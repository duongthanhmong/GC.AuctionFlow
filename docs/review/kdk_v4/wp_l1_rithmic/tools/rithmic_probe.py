"""Bounded, read-only FIN/Rithmic acquisition probe. LIVE.

Owner explicitly withdrew ODR-L1-05 as a blocking gate for this probe on 2026-07-31.

HARD EXCLUSIONS, by construction:
  * no order submit / modify / cancel / exit
  * no account, position, execution or PnL request
  * only TICKER_PLANT (and, in an isolated second connection, HISTORY_PLANT)
  * credentials come from the existing loader and are never printed, logged or serialised
  * raw capture goes to a quarantine directory outside .gcae and outside the repository
  * bounded windows, clean unsubscribe + disconnect per group
  * an entitlement denial on one surface is recorded and does not stop the others

usage:
  python rithmic_probe.py --out <quarantine_dir> [--window 25] [--skip-history]
"""
import argparse
import asyncio
import collections
import datetime as dt
import hashlib
import json
import os
import sys
import time

sys.dont_write_bytecode = True

BANNED = ("submit_order", "modify_order", "cancel_order", "cancel_all_orders",
          "exit_position", "list_positions", "list_accounts", "subscribe_to_pnl_updates",
          "get_account_rms", "replay_executions", "get_fill_history")

SECRET_HINT = ("user", "password", "passwd", "secret", "token", "fcm", "ib_id",
               "account", "login", "system_name", "url", "gateway")


def install_log_redaction(user, password):
    """async_rithmic echoes the ENTIRE login request - including the cleartext password -
    into its ERROR log when the server denies the login. Observed 2026-07-31 at
    plants/base.py:550 via RithmicErrorResponse. Nothing downstream of that is safe until
    it is filtered, so every logger gets a filter that rewrites the record before a handler
    can emit it."""
    import logging

    secrets = [s for s in (user, password) if s]

    class _Redact(logging.Filter):
        def filter(self, record):
            try:
                msg = record.getMessage()
            except Exception:
                return True
            hit = any(s in msg for s in secrets)
            if hit:
                for s in secrets:
                    msg = msg.replace(s, "<REDACTED>")
                record.msg = msg
                record.args = ()
            if record.exc_info:
                # A traceback can carry the same repr; drop it rather than risk emitting it.
                txt = logging.Formatter().formatException(record.exc_info)
                if any(s in txt for s in secrets):
                    record.exc_info = None
                    record.msg = str(record.msg) + " [traceback suppressed: contained credential]"
            return True

    f = _Redact()
    logging.getLogger().addFilter(f)
    for name in list(logging.root.manager.loggerDict):
        logging.getLogger(name).addFilter(f)
    logging.getLogger().handlers and [h.addFilter(f) for h in logging.getLogger().handlers]
    return f


def redact(o, d=0):
    if d > 8:
        return "<deep>"
    if isinstance(o, dict):
        return {k: ("<REDACTED>" if any(s in str(k).lower() for s in SECRET_HINT)
                    else redact(v, d + 1)) for k, v in o.items()}
    if isinstance(o, (list, tuple)):
        return [redact(v, d + 1) for v in list(o)[:200]]
    return o


def pb(msg):
    if msg is None or isinstance(msg, (str, int, float, bool)):
        return msg
    if isinstance(msg, dict):
        return {k: pb(v) for k, v in msg.items()}
    if isinstance(msg, (list, tuple)):
        return [pb(x) for x in msg]
    out = {}
    lf = getattr(msg, "ListFields", None)
    if callable(lf):
        try:
            for f, v in lf():
                if hasattr(v, "ListFields"):
                    out[f.name] = pb(v)
                elif hasattr(v, "__iter__") and not isinstance(v, (str, bytes)):
                    out[f.name] = [pb(x) for x in v]
                else:
                    out[f.name] = v
            return out
        except Exception:
            pass
    for a in dir(msg):
        if a.startswith("_"):
            continue
        try:
            v = getattr(msg, a)
        except Exception:
            continue
        if callable(v):
            continue
        out[a] = v if isinstance(v, (str, int, float, bool, type(None))) else str(v)
    return out


class Rec:
    """One probe step's measured result."""
    def __init__(self, pid, desc):
        self.id = pid
        self.desc = desc
        self.t0 = dt.datetime.now(dt.timezone.utc)
        self.t1 = None
        self.authenticated = None
        self.request_accepted = None
        self.subscription_accepted = None
        self.callbacks = 0
        self.callback_types = collections.Counter()
        self.payload_bytes = 0
        self.nonnull = collections.Counter()
        self.first_evt = None
        self.last_evt = None
        self.seq_present = 0
        self.seq_values = []
        self.error = None
        self.outfile = None
        self.outsize = 0
        self.outsha = None
        self.clean_teardown = None
        self.notes = []

    def row(self):
        gaps = 0
        s = sorted(v for v in self.seq_values if isinstance(v, int))
        for a, b in zip(s, s[1:]):
            if b - a > 1:
                gaps += 1
        return {
            "probe": self.id, "description": self.desc,
            "utc_start": self.t0.isoformat(timespec="seconds"),
            "utc_stop": (self.t1 or dt.datetime.now(dt.timezone.utc)).isoformat(timespec="seconds"),
            "local_start": self.t0.astimezone().isoformat(timespec="seconds"),
            "authenticated": self.authenticated,
            "request_accepted": self.request_accepted,
            "subscription_accepted": self.subscription_accepted,
            "callback_observed": self.callbacks > 0,
            "callback_count": self.callbacks,
            "callback_types": ";".join("%s=%d" % kv for kv in self.callback_types.most_common()),
            "payload_bytes": self.payload_bytes,
            "nonempty_values_observed": bool(self.nonnull),
            "nonnull_fields": ";".join("%s=%d" % kv for kv in self.nonnull.most_common(40)),
            "first_event_utc": self.first_evt, "last_event_utc": self.last_evt,
            "sequence_present": self.seq_present,
            "sequence_min": min(s) if s else "", "sequence_max": max(s) if s else "",
            "sequence_gaps": gaps,
            "error": self.error or "",
            "capture_file": self.outfile or "", "capture_bytes": self.outsize,
            "capture_sha256": self.outsha or "",
            "clean_teardown": self.clean_teardown,
            "notes": " | ".join(self.notes),
        }


class Probe:
    def __init__(self, outdir, window):
        self.out = outdir
        self.window = window
        self.lines = []
        self.recs = []
        os.makedirs(outdir, exist_ok=True)

    def say(self, *a):
        s = " ".join(str(x) for x in a)
        print(s, flush=True)
        self.lines.append(s)

    def dump(self, rec, name, payload):
        p = os.path.join(self.out, name + ".json")
        data = json.dumps(redact(payload), indent=1, default=str).encode("utf-8")
        with open(p, "wb") as fh:
            fh.write(data)
        rec.outfile = name + ".json"
        rec.outsize = len(data)
        rec.outsha = hashlib.sha256(data).hexdigest()
        rec.payload_bytes = len(data)
        self.say("     capture %s  %d bytes  sha256=%s" % (rec.outfile, rec.outsize, rec.outsha[:16]))

    def tally(self, rec, events):
        rec.callbacks = len(events)
        for e in events:
            d = pb(e)
            rec.callback_types[type(e).__name__] += 1
            if isinstance(d, dict):
                for k, v in d.items():
                    if v not in (None, "", 0):
                        rec.nonnull[k] += 1
                for tk in ("ssboe", "trade_time", "bid_time"):
                    if d.get(tk):
                        rec.first_evt = rec.first_evt or d[tk]
                        rec.last_evt = d[tk]
                if d.get("sequence_number") is not None:
                    rec.seq_present += 1
                    rec.seq_values.append(d["sequence_number"])


async def main_async(pr, args):
    here = os.path.dirname(os.path.abspath(__file__))
    sys.path.insert(0, os.path.abspath(os.path.join(
        here, "..", "..", "..", "..", "..", "research", "optionflow")))
    from rithmic.config import load_config
    from rithmic import patch158
    from async_rithmic import RithmicClient, SysInfraType, DataType
    from async_rithmic.protocol_buffers import request_market_data_update_pb2 as mdpb
    BITS = mdpb.RequestMarketDataUpdate.UpdateBits

    cfg = load_config()
    if not cfg.is_complete:
        pr.say("CONFIG INCOMPLETE - stopping (no values shown)")
        return 2
    # Install BEFORE any client call. See install_log_redaction for why.
    install_log_redaction(cfg.user, cfg.password)
    import logging
    logging.getLogger("rithmic").setLevel(logging.CRITICAL)
    logging.getLogger("rithmic.plant.ticker").setLevel(logging.CRITICAL)
    patch158.install()
    patch158.clear()

    ticks, depth, book, tbars, htick, hbar = [], [], [], [], [], []
    client = RithmicClient(user=cfg.user, password=cfg.password,
                           system_name=cfg.system_name, app_name=cfg.app_name,
                           app_version=cfg.app_version, url=cfg.url)
    client.on_tick += lambda x, *a, **k: ticks.append(x)
    client.on_market_depth += lambda x, *a, **k: depth.append(x)
    client.on_order_book += lambda x, *a, **k: book.append(x)
    client.on_time_bar += lambda x, *a, **k: tbars.append(x)

    # ---------------------------------------------------------------- AUTH
    r = Rec("P0", "authenticate, TICKER_PLANT only")
    pr.say("P0  connecting (TICKER_PLANT only)")
    try:
        await client.connect(plants=[SysInfraType.TICKER_PLANT])
        r.authenticated = True
        pr.say("P0  AUTHENTICATED")
    except Exception as e:
        r.authenticated = False
        msg = str(e)
        for sec in (cfg.user, cfg.password):
            if sec:
                msg = msg.replace(sec, "<REDACTED>")
        r.error = "%s: %s" % (type(e).__name__, msg[:300])
        pr.say("P0  AUTH FAILED: %s" % r.error)
        r.t1 = dt.datetime.now(dt.timezone.utc)
        pr.recs.append(r)
        return 3
    r.t1 = dt.datetime.now(dt.timezone.utc)
    pr.recs.append(r)

    EXCH = "COMEX"
    state = {"gc": None, "bid": None, "ask": None}

    async def step(pid, desc, fn):
        rec = Rec(pid, desc)
        rec.authenticated = True
        pr.say("")
        pr.say("%s  %s" % (pid, desc))
        try:
            await fn(rec)
            rec.request_accepted = True if rec.request_accepted is None else rec.request_accepted
        except Exception as e:
            rec.request_accepted = False
            rec.error = "%s: %s" % (type(e).__name__, str(e)[:300])
            pr.say("%s  REJECTED/ERROR: %s" % (pid, rec.error))
            pr.say("%s  recorded; continuing with other surfaces" % pid)
        rec.t1 = dt.datetime.now(dt.timezone.utc)
        pr.recs.append(rec)
        return rec

    # ---------------------------------------------------------------- P1
    async def p1(rec):
        res = await client.list_exchanges()
        rows = [pb(x) for x in (res if isinstance(res, (list, tuple)) else [res])]
        rec.callbacks = len(rows)
        for row in rows:
            for k, v in (row or {}).items():
                if v not in (None, "", 0):
                    rec.nonnull[k] += 1
        pr.say("     exchanges: %d" % len(rows))
        for row in rows[:30]:
            pr.say("       %s" % {k: row.get(k) for k in
                                  ("exchange", "level_1_market_data",
                                   "level_2_market_data", "entitlement_flag")})
        pr.dump(rec, "P1_exchange_permissions", rows)
    await step("P1", "exchange + entitlement enumeration -> list_exchanges()", p1)

    # ---------------------------------------------------------------- P2
    async def p2(rec):
        sym = await client.get_front_month_contract("GC", EXCH)
        state["gc"] = sym if isinstance(sym, str) else (pb(sym) or {}).get("symbol")
        rec.callbacks = 1
        rec.nonnull["symbol"] += 1
        pr.say("     GC front month = %s" % state["gc"])
        pr.dump(rec, "P2_front_month", {"symbol": state["gc"], "exchange": EXCH})
    await step("P2", "front-month GC contract -> get_front_month_contract()", p2)

    # ---------------------------------------------------------------- P3
    async def p3(rec):
        d = pb(await client.get_reference_data(state["gc"], EXCH))
        rec.callbacks = 1
        for k, v in (d or {}).items():
            if v not in (None, "", 0):
                rec.nonnull[k] += 1
        pr.say("     reference-data non-null fields: %d" % len(rec.nonnull))
        for k in sorted(d or {}):
            pr.say("       %-30s %s" % (k, d[k]))
        pr.dump(rec, "P3_reference_data", d)
    if state["gc"]:
        await step("P3", "reference data for %s" % state["gc"], p3)

    # ---------------------------------------------------------------- P4
    async def p4(rec):
        n0 = len(ticks)
        await client.subscribe_to_market_data(state["gc"], EXCH,
                                              DataType.LAST_TRADE | DataType.BBO)
        rec.subscription_accepted = True
        await asyncio.sleep(pr.window)
        got = ticks[n0:]
        pr.tally(rec, got)
        for t in got:
            d = pb(t)
            if d.get("bid_price"):
                state["bid"] = d["bid_price"]
            if d.get("ask_price"):
                state["ask"] = d["ask_price"]
        pr.say("     ticks: %d   bid=%s ask=%s" % (len(got), state["bid"], state["ask"]))
        pr.dump(rec, "P4_l1_trades_bbo", [pb(t) for t in got[:500]])
        try:
            await client.unsubscribe_from_market_data(
                state["gc"], EXCH, DataType.LAST_TRADE | DataType.BBO)
            rec.clean_teardown = True
        except Exception as e:
            rec.clean_teardown = False
            rec.notes.append("unsub: %s" % type(e).__name__)
    if state["gc"]:
        await step("P4", "L1 trades + BBO, %ds" % pr.window, p4)

    # ---------------------------------------------------------------- P5
    async def p5(rec):
        bits = (int(BITS.SETTLEMENT) | int(BITS.PROJECTED_SETTLEMENT) | int(BITS.MARKET_MODE)
                | int(BITS.OPEN) | int(BITS.CLOSE) | int(BITS.HIGH_LOW)
                | int(BITS.HIGH_BID_LOW_ASK) | int(BITS.OPENING_INDICATOR)
                | int(BITS.CLOSING_INDICATOR) | int(BITS.MARGIN_RATE)
                | int(BITS.HIGH_PRICE_LIMIT) | int(BITS.LOW_PRICE_LIMIT)
                | int(BITS.ADJUSTED_CLOSE) | int(BITS.OPEN_INTEREST))
        rec.notes.append("update_bits=%d" % bits)
        n0 = len(ticks)
        await client.subscribe_to_market_data(state["gc"], EXCH, bits)
        rec.subscription_accepted = True
        await asyncio.sleep(pr.window)
        got = ticks[n0:]
        pr.tally(rec, got)
        pr.say("     session-stat ticks: %d" % len(got))
        for k, n in rec.nonnull.most_common(30):
            pr.say("       %-30s %d" % (k, n))
        pr.dump(rec, "P5_session_stats", [pb(t) for t in got[:500]])
        try:
            await client.unsubscribe_from_market_data(state["gc"], EXCH, bits)
            rec.clean_teardown = True
        except Exception as e:
            rec.clean_teardown = False
            rec.notes.append("unsub: %s" % type(e).__name__)
    if state["gc"]:
        await step("P5", "14 unrequested UpdateBits surfaces, %ds" % pr.window, p5)

    # ---------------------------------------------------------------- P6a
    async def p6a(rec):
        n0 = len(book)
        await client.subscribe_to_market_data(state["gc"], EXCH, DataType.ORDER_BOOK)
        rec.subscription_accepted = True
        await asyncio.sleep(pr.window)
        got = book[n0:]
        pr.tally(rec, got)
        pr.say("     aggregated order-book events: %d" % len(got))
        if got:
            pr.say("     fields: %s" % ", ".join(sorted((pb(got[0]) or {}).keys())))
            ut = collections.Counter(str((pb(g) or {}).get("update_type")) for g in got)
            for k, n in ut.most_common():
                pr.say("       update_type %-24s %d" % (k, n))
                rec.notes.append("update_type %s=%d" % (k, n))
        pr.dump(rec, "P6a_order_book", [pb(g) for g in got[:500]])
        try:
            await client.unsubscribe_from_market_data(state["gc"], EXCH, DataType.ORDER_BOOK)
            rec.clean_teardown = True
        except Exception as e:
            rec.clean_teardown = False
    if state["gc"]:
        await step("P6a", "aggregated ORDER_BOOK, %ds" % pr.window, p6a)

    # ---------------------------------------------------------------- P6b
    async def p6b(rec):
        base = state["bid"] or state["ask"]
        if not base:
            rec.notes.append("no BBO from P4; using 0.0")
            levels = [0.0]
        else:
            levels = [round(base + i * 0.1, 1) for i in (-1, 0, 1)]
        rec.notes.append("depth_price levels=%s" % levels)
        n0 = len(depth)
        for lv in levels:
            await client.subscribe_to_market_depth(state["gc"], EXCH, lv)
        rec.subscription_accepted = True
        await asyncio.sleep(pr.window)
        got = depth[n0:]
        pr.tally(rec, got)
        pr.say("     depth-by-order events: %d   sequence_number present: %d"
               % (len(got), rec.seq_present))
        if got:
            d0 = pb(got[0])
            pr.say("     fields: %s" % ", ".join(sorted((d0 or {}).keys())))
            ut = collections.Counter(str((pb(g) or {}).get("update_type")) for g in got)
            for k, n in ut.most_common():
                pr.say("       update_type %-24s %d" % (k, n))
                rec.notes.append("update_type %s=%d" % (k, n))
        pr.dump(rec, "P6b_depth_by_order", [pb(g) for g in got[:800]])
        ok = True
        for lv in levels:
            try:
                await client.unsubscribe_from_market_depth(state["gc"], EXCH, lv)
            except Exception:
                ok = False
        rec.clean_teardown = ok
    if state["gc"]:
        await step("P6b", "DepthByOrder / MBO at representative price levels, %ds" % pr.window, p6b)

    # ---------------------------------------------------------------- P7
    picked = []

    async def p7(rec):
        res = await client.search_symbols("OG", exchange=EXCH)
        rows = [pb(x) for x in (res if isinstance(res, (list, tuple)) else [res])]
        rec.callbacks = len(rows)
        pr.say("     option symbols found: %d" % len(rows))
        for row in rows[:6]:
            pr.say("       %s" % {k: row.get(k) for k in
                                  ("symbol", "product_code", "instrument_type",
                                   "expiration_date")})
        pr.dump(rec, "P7_option_search", rows[:800])
        refs = []
        for row in rows[:6]:
            s = row.get("symbol")
            if not s:
                continue
            try:
                d = pb(await client.get_reference_data(s, row.get("exchange") or EXCH))
            except Exception as e:
                refs.append({"symbol": s, "error": type(e).__name__})
                continue
            refs.append(d)
            picked.append((s, row.get("exchange") or EXCH))
            pr.say("       ref %s underlying=%s expiry=%s strike=%s pc=%s pointval=%s"
                   % (s, (d or {}).get("underlying_symbol"), (d or {}).get("expiration_date"),
                      (d or {}).get("strike_price"), (d or {}).get("put_call_indicator"),
                      (d or {}).get("single_point_value")))
            for k, v in (d or {}).items():
                if v not in (None, "", 0):
                    rec.nonnull[k] += 1
        pr.dump(rec, "P7_option_reference_data", refs)
    await step("P7", "option chain discovery + reference data", p7)

    # ---------------------------------------------------------------- P8
    async def p8(rec):
        bits = (int(BITS.LAST_TRADE) | int(BITS.BBO) | int(BITS.OPEN_INTEREST)
                | int(BITS.SETTLEMENT) | int(BITS.HIGH_LOW))
        rec.notes.append("update_bits=%d on %d option symbols" % (bits, len(picked[:4])))
        n0 = len(ticks)
        for s, ex in picked[:4]:
            await client.subscribe_to_market_data(s, ex, bits)
        rec.subscription_accepted = True
        await asyncio.sleep(pr.window)
        got = ticks[n0:]
        pr.tally(rec, got)
        oi = patch158.OI_FRAMES
        rec.notes.append("template158_frames=%d" % len(oi))
        pr.say("     option ticks: %d   template-158 frames: %d" % (len(got), len(oi)))
        for k, n in rec.nonnull.most_common(25):
            pr.say("       %-30s %d" % (k, n))
        pr.dump(rec, "P8_option_market_data",
                {"ticks": [pb(t) for t in got[:500]],
                 "template158_frame_count": len(oi)})
        ok = True
        for s, ex in picked[:4]:
            try:
                await client.unsubscribe_from_market_data(s, ex, bits)
            except Exception:
                ok = False
        rec.clean_teardown = ok
    if picked:
        await step("P8", "option market data: trades, BBO, OI, settlement, %ds" % pr.window, p8)

    pr.say("")
    pr.say("disconnecting ticker plant")
    tear = Rec("P9", "clean disconnect, ticker plant")
    tear.authenticated = True
    try:
        await client.disconnect()
        tear.clean_teardown = True
        pr.say("     disconnected cleanly")
    except Exception as e:
        tear.clean_teardown = False
        tear.error = type(e).__name__
    tear.t1 = dt.datetime.now(dt.timezone.utc)
    pr.recs.append(tear)

    # ------------------------------------------------- P10 history, isolated
    if not args.skip_history:
        rec = Rec("P10", "historical bar replay, ISOLATED connection (HISTORY_PLANT)")
        pr.say("")
        pr.say("P10  historical bars - separate connection so a denial cannot kill the ticker run")
        c2 = RithmicClient(user=cfg.user, password=cfg.password,
                           system_name=cfg.system_name, app_name=cfg.app_name,
                           app_version=cfg.app_version, url=cfg.url)
        c2.on_historical_time_bar += lambda x, *a, **k: hbar.append(x)
        c2.on_historical_tick += lambda x, *a, **k: htick.append(x)
        try:
            await c2.connect(plants=[SysInfraType.HISTORY_PLANT])
            rec.authenticated = True
            pr.say("P10  history plant AUTHENTICATED")
            end = dt.datetime.now(dt.timezone.utc)
            start = end - dt.timedelta(minutes=10)
            try:
                bars = await c2.get_historical_time_bars(
                    state["gc"] or "GCZ6", EXCH, start, end,
                    __import__("async_rithmic").TimeBarType.MINUTE_BAR, 1)
                rec.request_accepted = True
                rows = [pb(b) for b in (bars or [])]
                rec.callbacks = len(rows)
                for row in rows:
                    for k, v in (row or {}).items():
                        if v not in (None, "", 0):
                            rec.nonnull[k] += 1
                pr.say("     historical 1-min bars returned: %d" % len(rows))
                pr.dump(rec, "P10_historical_time_bars", rows[:500])
            except Exception as e:
                rec.request_accepted = False
                rec.error = "%s: %s" % (type(e).__name__, str(e)[:300])
                pr.say("     bar request rejected: %s" % rec.error)
            try:
                await c2.disconnect()
                rec.clean_teardown = True
            except Exception:
                rec.clean_teardown = False
        except Exception as e:
            rec.authenticated = False
            rec.error = "%s: %s" % (type(e).__name__, str(e)[:300])
            pr.say("P10  history plant DENIED/FAILED: %s" % rec.error)
            pr.say("     recorded as a negative result; ticker results stand")
        rec.t1 = dt.datetime.now(dt.timezone.utc)
        pr.recs.append(rec)
    return 0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--window", type=float, default=25.0)
    ap.add_argument("--skip-history", action="store_true")
    a = ap.parse_args()

    src = open(os.path.abspath(__file__), encoding="utf-8").read()
    body = src.split("async def main_async")[1]
    bad = [b for b in BANNED if ("client." + b) in body or ("c2." + b) in body]
    if bad:
        raise SystemExit("REFUSING TO RUN: probe body calls forbidden surfaces: %s" % bad)

    stamp = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    outdir = os.path.join(a.out, "probe_" + stamp)
    pr = Probe(outdir, a.window)
    pr.say("###### FIN/RITHMIC LIVE READ-ONLY ACQUISITION PROBE ######")
    pr.say("session id  : probe_%s" % stamp)
    pr.say("utc start   : %s" % dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds"))
    pr.say("local start : %s" % dt.datetime.now().astimezone().isoformat(timespec="seconds"))
    pr.say("quarantine  : %s" % outdir)
    pr.say("window      : %.0fs per streaming group" % a.window)
    pr.say("scope       : market data + reference data only; no order/account/PnL request")
    rc = 1
    t0 = time.time()
    try:
        rc = asyncio.run(main_async(pr, a))
    except KeyboardInterrupt:
        pr.say("INTERRUPTED")
        rc = 130
    except Exception as e:
        pr.say("UNHANDLED %s: %s" % (type(e).__name__, str(e)[:300]))
        rc = 1
    finally:
        pr.say("")
        pr.say("elapsed %.1fs" % (time.time() - t0))
        rows = [r.row() for r in pr.recs]
        import csv
        cp = os.path.join(outdir, "probe_results.csv")
        if rows:
            with open(cp, "w", encoding="utf-8", newline="") as fh:
                w = csv.DictWriter(fh, fieldnames=list(rows[0].keys()), lineterminator="\n")
                w.writeheader()
                w.writerows(rows)
        with open(os.path.join(outdir, "transcript.txt"), "w",
                  encoding="utf-8", newline="\n") as fh:
            fh.write("\n".join(pr.lines) + "\n")
        print("\nresults csv : %s" % cp)
        print("transcript  : %s" % os.path.join(outdir, "transcript.txt"))
    return rc


if __name__ == "__main__":
    raise SystemExit(main())
