#!/usr/bin/env python3
"""GCAE OptionFlow sidecar CLI.

Usage
-----
See real option levels NOW from an OI cache on disk (no Rithmic needed):

    python run_snapshot.py --from-oi-cache "%LOCALAPPDATA%\\SeachainsOption\\data\\rithmic_oi_eod_cache.json"

Write those levels into the repo artifacts so the DLL can read them:

    python run_snapshot.py --from-oi-cache <cache.json> --write

Live pipeline (needs C:\\Users\\LOQ\\.gcae\\rithmic.env filled in):

    python run_snapshot.py --live --probe        # connect, subscribe, dump raw feed
    python run_snapshot.py --live --once          # one full snapshot -> levels.json
    python run_snapshot.py --list-systems         # discover your system_name

The --probe mode prints the raw tick keys we receive; that is how we confirm
which field carries open interest on your specific feed (the one thing that
cannot be verified offline).
"""

from __future__ import annotations

import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from gex.levels_writer import write_levels          # noqa: E402
from rithmic import collector                        # noqa: E402
from rithmic.oi_cache import load_oi_cache           # noqa: E402

DEFAULT_ARTIFACTS = os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "..", "..", "artifacts", "optionflow")

PRODUCT_EXCHANGE = {"GC": "COMEX", "ES": "CME", "NQ": "CME"}


def _print_levels(doc) -> None:
    print(f"\n=== {doc.product}  health={doc.data_health}  spot={doc.spot}"
          f"  regime={doc.regime}  flip={doc.selected_flip}")
    print(f"    coverage: {doc.coverage}")
    if not doc.levels:
        print("    (no VALID levels — nothing fabricated)")
        return
    for lv in sorted(doc.levels, key=lambda x: (x.scope, x.level_type)):
        price = f"{lv.price:>12.2f}" if lv.price is not None else "     n/a"
        print(f"    {lv.level_type:<24} {lv.scope:<18} {price}  [{lv.status}]")
    a = getattr(doc, "analytics", None)
    if a:
        print("    --- deep analytics ---")
        atm = a.get("atm")
        if atm:
            em = f"  straddle_EM=±{atm['straddle_move_to_expiry']:.1f}" if atm.get("straddle_move_to_expiry") else ""
            print(f"    ATM_STRIKE               {atm['strike']:>12.2f}  straddle={atm['straddle']:.2f}{em}"
                  + (f"  atm_iv={atm['atm_iv']:.1%}" if atm.get("atm_iv") else ""))
        if a.get("vanna_wall"):
            print(f"    VANNA_WALL               {a['vanna_wall']['strike']:>12.2f}")
        if a.get("charm_wall"):
            print(f"    CHARM_WALL               {a['charm_wall']['strike']:>12.2f}")
        em = a.get("expected_move")
        if em:
            print(f"    EXPECTED_MOVE  atm_iv={em['atm_iv']:.1%}  1d=±{em['one_day_move']:.1f}"
                  + (f"  to_exp=±{em['to_expiry_move']:.1f}" if em.get("to_expiry_move") else ""))
        sk = a.get("iv_skew")
        if sk:
            print(f"    IV_SKEW  put={sk['put_iv']:.1%} call={sk['call_iv']:.1%} skew={sk['skew']:+.1%}")
        ts = a.get("term_structure") or {}
        for name, b in ts.items():
            if b:
                print(f"    TERM[{name:<7}] net_gex={b['net_gex']:>16,.0f}  strikes={b['strikes']}")


def cmd_from_oi_cache(args) -> int:
    if not os.path.exists(args.from_oi_cache):
        print(f"cache not found: {args.from_oi_cache}", file=sys.stderr)
        return 2
    by_product = load_oi_cache(args.from_oi_cache)
    if not by_product:
        print("no parseable option records in cache", file=sys.stderr)
        return 3
    root = os.path.abspath(args.data_root)
    for product in sorted(by_product):
        quotes = by_product[product]
        doc = collector.compute_oi_only_levels(quotes, product=product)
        _print_levels(doc)
        if args.write:
            path = write_levels(doc, root)
            print(f"    -> {path}")
    if args.write:
        print(f"\nWrote OI-only levels under {root}")
    else:
        print("\n(dry run — add --write to publish levels.json for the DLL)")
    print("Note: OI walls + max-pain are real from OI alone. GEX walls / zero-gamma"
          " need live option IV (use --live).")
    return 0


def cmd_live(args) -> int:
    import asyncio
    import datetime as dt
    from rithmic.config import load_config
    from rithmic.client import OptionFlowRithmic
    from rithmic import instruments, live, patch158
    from gex.levels_writer import write_levels

    cfg = load_config()
    if not cfg.is_complete:
        print("Incomplete config. Fill C:\\Users\\LOQ\\.gcae\\rithmic.env with "
              "RITHMIC_USER, RITHMIC_PASSWORD (system/gateway default to Rithmic Paper Trading).",
              file=sys.stderr)
        return 2

    products = list(PRODUCT_EXCHANGE) if args.product == "all" else [args.product]

    async def run_probe():
        rc = OptionFlowRithmic(cfg)
        print(f"Connecting to {cfg.url} as {cfg.user} on system '{cfg.system_name}' ...")
        await rc.connect()
        print("Connected. OPEN_INTEREST bit supported:", rc.open_interest_supported)
        master = instruments.load_master()
        now = dt.datetime.now(dt.timezone.utc)
        for product in products:
            exch = PRODUCT_EXCHANGE[product]
            fut, F = await live._future_price(rc, product, exch, 4.0)
            print(f"{product}: future {fut} price={F}")
            if not F:
                continue
            specs = instruments.select_contracts(master, product, now, F, max_dte=7, strike_band_pct=0.05, limit=30)
            print(f"{product}: subscribing {len(specs)} near-ATM options")
            for s in specs:
                await rc.subscribe(s.symbol, s.exchange, quotes=True, open_interest=True)
        await asyncio.sleep(args.window)
        oi = rc.open_interest_by_symbol()
        print(f"\n=== OI captured for {len(oi)} symbols (template 158, field 100064) ===")
        for sym in sorted(oi)[:20]:
            print(f"    {sym:<18} OI={oi[sym]}  quote={rc.mid_or_last(sym)}")
        await rc.disconnect()

    async def snapshot_one(rc, master, root, product):
        """One product snapshot, fully isolated: a failure here never aborts the
        other products or the loop."""
        exch = PRODUCT_EXCHANGE[product]
        now = dt.datetime.now(dt.timezone.utc)
        try:
            patch158.clear()
            doc, stats = await live.snapshot_product(
                cfg, rc, product, exch, master=master, now=now,
                max_dte=args.max_dte, max_contracts=args.max_contracts,
                strike_band_pct=args.strike_band, window_secs=args.window)
        except Exception as e:
            print(f"\n[{product}] snapshot failed: {e!r}")
            return
        contracts = stats.pop("contracts", [])
        print(f"\n[{product}] stats: {stats}")
        if doc is None:
            return
        _print_levels(doc)
        if args.write:
            print(f"    -> {write_levels(doc, root)}")
            import json as _json
            dbg_dir = os.path.join(root, product)
            os.makedirs(dbg_dir, exist_ok=True)
            dbg = os.path.join(dbg_dir, "contracts_debug.json")
            with open(dbg, "w", encoding="utf-8", newline="\n") as fh:
                _json.dump({"product": product, "spot": stats["spot"],
                            "contracts": contracts}, fh, indent=2)
            print(f"    -> {dbg}")

    async def run_once():
        rc = OptionFlowRithmic(cfg)
        print(f"Connecting to {cfg.url} as {cfg.user} ...")
        await rc.connect()
        master = instruments.load_master()
        root = os.path.abspath(args.data_root)
        cycle = 0
        try:
            while True:
                cycle += 1
                if args.loop:
                    print(f"\n===== cycle {cycle} =====")
                for product in products:
                    await snapshot_one(rc, master, root, product)
                if not args.loop:
                    break
                print(f"\n(sleeping {args.loop}s — Ctrl+C to stop)")
                await asyncio.sleep(args.loop)
        finally:
            await rc.disconnect()

    try:
        asyncio.run(run_once() if args.once else run_probe())
        return 0
    except KeyboardInterrupt:
        print("\nstopped.")
        return 0
    except Exception as e:  # surface connection/auth errors plainly
        print(f"live error: {e!r}", file=sys.stderr)
        return 1


def main(argv=None) -> int:
    p = argparse.ArgumentParser(description="GCAE OptionFlow sidecar")
    p.add_argument("--from-oi-cache", metavar="PATH", help="compute levels from an OI cache json")
    p.add_argument("--write", action="store_true", help="publish levels.json into artifacts")
    p.add_argument("--data-root", default=DEFAULT_ARTIFACTS, help="artifacts output root")
    p.add_argument("--live", action="store_true", help="connect to Rithmic")
    p.add_argument("--probe", action="store_true", help="(with --live) dump raw feed to learn OI field")
    p.add_argument("--once", action="store_true", help="(with --live) one snapshot then exit")
    p.add_argument("--loop", type=float, default=0.0, help="(with --live --once) re-snapshot every N seconds, keeping the connection")
    p.add_argument("--product", default="GC", help="GC/ES/NQ or 'all' (live)")
    p.add_argument("--max-dte", type=float, default=7.0, help="max days-to-expiry to include")
    p.add_argument("--window", type=float, default=12.0, help="live collection window seconds")
    p.add_argument("--max-contracts", type=int, default=300, help="cap option subscriptions per product")
    p.add_argument("--strike-band", type=float, default=0.15, help="strike window as fraction of spot (live)")
    args = p.parse_args(argv)

    if args.from_oi_cache:
        return cmd_from_oi_cache(args)
    if args.live:
        return cmd_live(args)
    p.print_help()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
