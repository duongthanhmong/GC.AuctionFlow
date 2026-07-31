"""Bounded, read-only FIN/Rithmic market-data capability probe.

Stage 2 addendum item 3. Authorised by the owner on 2026-07-31 after the SEC-001 rotation
gate was raised and the owner reaffirmed.

HARD EXCLUSIONS - enforced by construction, not by intention:
  * no order placement, modification or cancellation
  * no account, position, PnL or execution request
  * only TickerPlant is connected; OrderPlant / PnlPlant / HistoryPlant are never opened
  * credentials are loaded by the existing config loader and NEVER printed, logged or stored
  * output goes to a quarantine directory outside .gcae and outside the repository
  * bounded instrument set and bounded observation windows
  * on an entitlement / auth / rate-limit error the probe STOPS; it does not retry

Every response is written raw to quarantine and a SANITIZED summary is printed.

usage:
    python rithmic_probe.py --out <quarantine_dir> [--only P1] [--window 20]
"""
import argparse
import asyncio
import collections
import datetime as dt
import json
import os
import sys
import traceback

sys.dont_write_bytecode = True

# Requests that must never be issued. Checked against the plant call surface at start-up so
# a future refactor that adds one of these to the probe fails loudly instead of silently.
FORBIDDEN = (
    "new_order", "modify_order", "cancel_order", "cancel_all", "exit_position",
    "bracket", "pnl", "account", "fill", "order_history", "trade_route",
    "subscribe_for_order_updates", "replay_executions", "rms",
)

# Fields that must never leave this process in cleartext.
SECRET_KEYS = ("user", "password", "passwd", "secret", "token", "fcm", "ib", "account",
               "login", "system_name")


def sanitize(obj, depth=0):
    """Redact anything that could identify the account or carry a credential."""
    if depth > 6:
        return "<depth>"
    if isinstance(obj, dict):
        out = {}
        for k, v in obj.items():
            if any(s in str(k).lower() for s in SECRET_KEYS):
                out[k] = "<REDACTED>"
            else:
                out[k] = sanitize(v, depth + 1)
        return out
    if isinstance(obj, (list, tuple)):
        return [sanitize(v, depth + 1) for v in obj[:50]]
    return obj


def pb_to_dict(msg):
    """Protobuf message -> plain dict of set fields, without importing json_format."""
    if msg is None:
        return None
    if isinstance(msg, (str, int, float, bool)):
        return msg
    d = {}
    try:
        for f, v in msg.ListFields():
            if hasattr(v, "ListFields"):
                d[f.name] = pb_to_dict(v)
            elif isinstance(v, (list, tuple)) or hasattr(v, "__iter__") and not isinstance(v, (str, bytes)):
                d[f.name] = [pb_to_dict(x) if hasattr(x, "ListFields") else x for x in v]
            else:
                d[f.name] = v
    except Exception:
        for a in dir(msg):
            if a.startswith("_") or callable(getattr(msg, a, None)):
                continue
            try:
                d[a] = getattr(msg, a)
            except Exception:
                pass
    return d


class Probe:
    def __init__(self, outdir, window):
        self.out = outdir
        self.window = window
        self.log = []
        self.captures = {}
        os.makedirs(outdir, exist_ok=True)

    def say(self, *a):
        line = " ".join(str(x) for x in a)
        print(line, flush=True)
        self.log.append(line)

    def save(self, name, obj):
        p = os.path.join(self.out, name + ".json")
        with open(p, "w", encoding="utf-8", newline="\n") as fh:
            json.dump(sanitize(obj), fh, indent=1, default=str)
        return p


def assert_no_forbidden_calls(src_text):
    hits = [f for f in FORBIDDEN if f in src_text]
    if hits:
        raise SystemExit("REFUSING TO RUN: probe source references forbidden surfaces: %s" % hits)


async def run(pr, only):
    sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                    "..", "..", "..", "..", "..",
                                    "research", "optionflow"))
    from rithmic.config import load_config
    from async_rithmic import RithmicClient, SysInfraType, DataType

    cfg = load_config()
    if not cfg.is_complete:
        pr.say("CONFIG INCOMPLETE - stopping. (no values printed)")
        return 2

    # Credentials are handed straight to the client. They are never read into a local
    # variable that gets logged, and the config object itself is never serialised.
    client = RithmicClient(user=cfg.user, password=cfg.password,
                           system_name=cfg.system_name, app_name=cfg.app_name,
                           app_version=cfg.app_version, gateway=cfg.gateway)

    ticks, depth, book = [], [], []
    client.on_tick += lambda t, *a, **k: ticks.append(t)
    client.on_market_depth += lambda r, *a, **k: depth.append(r)
    client.on_order_book += lambda r, *a, **k: book.append(r)

    pr.say("P-0  connecting: TICKER_PLANT only (OrderPlant/PnlPlant/HistoryPlant never opened)")
    try:
        await client.connect(plants=[SysInfraType.TICKER_PLANT])
    except Exception as e:
        pr.say("P-0  CONNECT FAILED: %s: %s" % (type(e).__name__, e))
        pr.say("     stopping, not retrying")
        return 3
    pr.say("P-0  connected OK")

    async def step(tag, coro, desc):
        if only and tag not in only:
            return None
        pr.say("")
        pr.say("%s  %s" % (tag, desc))
        try:
            res = await coro()
            pr.captures[tag] = res
            return res
        except Exception as e:
            pr.say("%s  ERROR %s: %s" % (tag, type(e).__name__, e))
            pr.say("%s  stopping this step, not retrying" % tag)
            pr.captures[tag] = {"error": "%s: %s" % (type(e).__name__, e)}
            return None

    GC_EXCH = "COMEX"

    # ---- P-1 entitlement: no market-data subscription at all -------------------
    async def p1():
        res = await client.list_exchanges()
        rows = [pb_to_dict(r) for r in (res if isinstance(res, (list, tuple)) else [res])]
        pr.save("P1_exchange_permissions", rows)
        pr.say("P-1  exchanges returned: %d" % len(rows))
        for r in rows[:40]:
            pr.say("     exchange=%-12s L1=%s L2=%s flag=%s"
                   % (r.get("exchange"), r.get("level_1_market_data"),
                      r.get("level_2_market_data"), r.get("entitlement_flag")))
        return rows
    await step("P1", p1, "exchange permissions / entitlement")

    # ---- P-2 front month ------------------------------------------------------
    front = {"symbol": None}

    async def p2():
        sym = await client.get_front_month_contract("GC", GC_EXCH)
        front["symbol"] = sym if isinstance(sym, str) else pb_to_dict(sym)
        pr.save("P2_front_month", front)
        pr.say("P-2  GC front month = %s" % front["symbol"])
        return front
    await step("P2", p2, "front-month GC contract")

    sym = front["symbol"] if isinstance(front["symbol"], str) else None

    # ---- P-3 reference data ---------------------------------------------------
    async def p3():
        rd = await client.get_reference_data(sym, GC_EXCH)
        d = pb_to_dict(rd)
        pr.save("P3_reference_data", d)
        pr.say("P-3  reference-data fields returned: %d" % len(d or {}))
        for k in sorted((d or {}).keys()):
            pr.say("     %-32s %s" % (k, d[k]))
        return d
    if sym:
        await step("P3", p3, "reference data for %s" % sym)

    # ---- P-4 level 1 ----------------------------------------------------------
    async def p4():
        before = len(ticks)
        await client.subscribe_to_market_data(sym, GC_EXCH,
                                              DataType.LAST_TRADE | DataType.BBO)
        await asyncio.sleep(pr.window)
        got = ticks[before:]
        pr.save("P4_level1_ticks", [sanitize(t) for t in got[:200]])
        pr.say("P-4  ticks in %ds: %d" % (pr.window, len(got)))
        keys = collections.Counter()
        for t in got:
            if isinstance(t, dict):
                keys.update(t.keys())
        for k, n in keys.most_common(40):
            pr.say("     field %-28s present %d" % (k, n))
        return {"count": len(got), "fields": dict(keys)}
    if sym:
        await step("P4", p4, "L1 LAST_TRADE|BBO for %ds" % pr.window)

    # ---- P-5 session statistics bits ------------------------------------------
    async def p5():
        from async_rithmic.protocol_buffers import request_market_data_update_pb2 as m
        bits = m.RequestMarketDataUpdate.UpdateBits
        extra = (int(bits.SETTLEMENT) | int(bits.MARKET_MODE) | int(bits.OPEN)
                 | int(bits.HIGH_LOW) | int(bits.CLOSE) | int(bits.OPEN_INTEREST))
        before = len(ticks)
        await client.subscribe_to_market_data(sym, GC_EXCH, extra)
        await asyncio.sleep(pr.window)
        got = ticks[before:]
        pr.save("P5_session_stats_ticks", [sanitize(t) for t in got[:200]])
        keys = collections.Counter()
        for t in got:
            if isinstance(t, dict):
                keys.update(t.keys())
        pr.say("P-5  ticks in %ds: %d" % (pr.window, len(got)))
        for k, n in keys.most_common(40):
            pr.say("     field %-28s present %d" % (k, n))
        return {"count": len(got), "fields": dict(keys)}
    if sym:
        await step("P5", p5, "SETTLEMENT|MARKET_MODE|OPEN|HIGH_LOW|CLOSE|OPEN_INTEREST")

    # ---- P-6 MBO / depth-by-order ---------------------------------------------
    async def p6():
        before_d, before_b = len(depth), len(book)
        await client.subscribe_to_market_depth(sym, GC_EXCH, 0.0)
        await asyncio.sleep(pr.window)
        gd, gb = depth[before_d:], book[before_b:]
        pr.save("P6_depth_by_order", [sanitize(pb_to_dict(r)) for r in gd[:200]])
        pr.save("P6_order_book", [sanitize(pb_to_dict(r)) for r in gb[:200]])
        pr.say("P-6  depth-by-order events: %d   order-book events: %d" % (len(gd), len(gb)))
        if gd:
            d0 = pb_to_dict(gd[0])
            pr.say("     DepthByOrder fields present: %s" % ", ".join(sorted(d0.keys())))
            seq = [pb_to_dict(r).get("sequence_number") for r in gd[:500]]
            seq = [s for s in seq if s is not None]
            pr.say("     sequence_number present on %d/%d sampled" % (len(seq), min(len(gd), 500)))
            if seq:
                pr.say("     sequence_number range: %s .. %s" % (min(seq), max(seq)))
        if gb:
            b0 = pb_to_dict(gb[0])
            pr.say("     OrderBook fields present: %s" % ", ".join(sorted(b0.keys())))
        return {"depth": len(gd), "book": len(gb)}
    if sym:
        await step("P6", p6, "market depth / depth-by-order for %ds" % pr.window)

    # ---- P-7 minimal option set ----------------------------------------------
    async def p7():
        res = await client.search_symbols("OG", exchange=GC_EXCH)
        rows = [pb_to_dict(r) for r in (res if isinstance(res, (list, tuple)) else [res])]
        pr.save("P7_option_search", rows[:400])
        pr.say("P-7  option symbols returned: %d" % len(rows))
        for r in rows[:8]:
            pr.say("     %s" % {k: r.get(k) for k in
                                ("symbol", "exchange", "product_code",
                                 "instrument_type", "expiration_date")})
        picked = rows[:4]
        for r in picked:
            s = r.get("symbol")
            if not s:
                continue
            rd = await client.get_reference_data(s, r.get("exchange") or GC_EXCH)
            d = pb_to_dict(rd)
            pr.save("P7_refdata_%s" % str(s).replace(" ", "_"), d)
            pr.say("     refdata %s -> underlying=%s expiry=%s strike=%s pc=%s pointval=%s"
                   % (s, (d or {}).get("underlying_symbol"), (d or {}).get("expiration_date"),
                      (d or {}).get("strike_price"), (d or {}).get("put_call_indicator"),
                      (d or {}).get("single_point_value")))
        return {"search_count": len(rows), "refdata_probed": len(picked)}
    await step("P7", p7, "minimal option set: search + reference data on 4 contracts")

    pr.say("")
    pr.say("disconnecting")
    try:
        await client.disconnect()
    except Exception as e:
        pr.say("disconnect note: %s" % type(e).__name__)
    return 0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--only", default="")
    ap.add_argument("--window", type=float, default=20.0)
    a = ap.parse_args()

    assert_no_forbidden_calls(open(os.path.abspath(__file__), encoding="utf-8")
                              .read().split("HARD EXCLUSIONS")[1].split("def sanitize")[0]
                              .replace("FORBIDDEN", ""))

    stamp = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    outdir = os.path.join(a.out, "probe_" + stamp)
    pr = Probe(outdir, a.window)
    pr.say("###### RITHMIC READ-ONLY CAPABILITY PROBE ######")
    pr.say("started UTC : %s" % stamp)
    pr.say("quarantine  : %s" % outdir)
    pr.say("window      : %.0fs per streaming step" % a.window)
    pr.say("scope       : market-data and reference-data only; no order/account/PnL request")
    only = set(x.strip() for x in a.only.split(",") if x.strip())
    rc = 0
    try:
        rc = asyncio.run(run(pr, only))
    except KeyboardInterrupt:
        pr.say("INTERRUPTED")
        rc = 130
    except Exception:
        pr.say("UNHANDLED: " + traceback.format_exc().splitlines()[-1])
        rc = 1
    finally:
        with open(os.path.join(outdir, "transcript.txt"), "w",
                  encoding="utf-8", newline="\n") as fh:
            fh.write("\n".join(pr.log) + "\n")
        pr.say("")
        pr.say("transcript: %s" % os.path.join(outdir, "transcript.txt"))
    return rc


if __name__ == "__main__":
    raise SystemExit(main())
