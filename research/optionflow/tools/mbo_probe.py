#!/usr/bin/env python3
"""MBO / order-flow entitlement probe.

Question: does the fin_ Rithmic account get ORDER-LEVEL depth (Market-By-Order)?
That is the data ATAS cannot expose (it aggregates by price and drops order_id /
queue priority). If it flows here, GCAE's BLOCKED order flow could be fed direct.

What this does (read-only, no orders, ticker plant only):
  1. connect, resolve GC front-month future + its price,
  2. subscribe depth-by-order (RequestDepthByOrderUpdates, template 117) for it,
  3. collect a window, and separately capture any Rithmic Reject / "permission
     denied" the depth subscription triggers,
  4. print a verdict.

Verdict:
  * DepthByOrder events with exchange_order_id + depth_order_priority  -> MBO ENTITLED
  * a Reject / "permission denied" on the depth request                -> NOT ENTITLED
  * zero events, no reject                                             -> INCONCLUSIVE
                                                                          (quiet market — retry during RTH)

Usage:
    python tools/mbo_probe.py [--window 20]
"""

from __future__ import annotations

import argparse
import asyncio
import logging
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from rithmic.client import OptionFlowRithmic   # noqa: E402
from rithmic.config import load_config         # noqa: E402
from rithmic import live                        # noqa: E402

PRODUCT, EXCHANGE = "GC", "COMEX"


class _RejectCapture(logging.Handler):
    """Grab any Rithmic reject / permission-denied log the depth request causes."""
    def __init__(self):
        super().__init__(level=logging.WARNING)
        self.hits: list[str] = []

    def emit(self, record):
        msg = record.getMessage().lower()
        if any(k in msg for k in ("reject", "permission", "denied", "not entitled", "1011", "1034")):
            self.hits.append(record.getMessage())


async def run(window: float) -> int:
    cfg = load_config()
    if not cfg.is_complete:
        print("Fill C:\\Users\\LOQ\\.gcae\\rithmic.env first.", file=sys.stderr)
        return 2

    cap = _RejectCapture()
    logging.getLogger("rithmic").addHandler(cap)

    rc = OptionFlowRithmic(cfg)
    print(f"Connecting to {cfg.url} as {cfg.user} ...")
    await rc.connect()

    fut, F = await live._future_price(rc, PRODUCT, EXCHANGE, 4.0)
    print(f"{PRODUCT}: future {fut} price={F}")
    if not F:
        print("No future price (quiet market). Try during RTH.")
        await rc.disconnect()
        return 3

    print(f"Subscribing depth-by-order (MBO) for {fut} around {F} ...")
    try:
        await rc.subscribe_market_depth(fut, EXCHANGE, F)
    except Exception as e:
        print(f"subscribe_market_depth raised: {e!r}")

    print(f"Collecting {window}s ...")
    await asyncio.sleep(window)

    ev = rc.depth_events()
    books = rc.book_event_count()
    print(f"\n=== depth-by-order events: {len(ev)}   aggregated-book events: {books} ===")
    for e in ev[:12]:
        print(f"    upd={e['update_type']} txn={e['transaction_type']} "
              f"px={e['price']} sz={e['size']} prio={e['priority']} oid={e['order_id']}")

    order_level = any(e.get("order_id") not in (None, 0, "") or e.get("priority") not in (None, 0)
                      for e in ev)

    print("\n--- VERDICT ---")
    if order_level:
        print("MBO ENTITLED — order-level depth (order_id/priority) is flowing.")
        verdict = 0
    elif cap.hits:
        print("NOT ENTITLED — depth request was rejected:")
        for h in cap.hits[:5]:
            print("   ", h)
        verdict = 1
    elif ev or books:
        print("PARTIAL — depth events arrived but without order-level fields "
              "(likely aggregated book only; order-by-order may need higher entitlement).")
        verdict = 0
    else:
        print("INCONCLUSIVE — no depth events and no reject. Market likely quiet; "
              "re-run during RTH before concluding.")
        verdict = 4

    await rc.disconnect()
    return verdict


def main(argv=None) -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--window", type=float, default=20.0)
    args = p.parse_args(argv)
    try:
        return asyncio.run(run(args.window))
    except Exception as e:
        print(f"probe error: {e!r}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
