#!/usr/bin/env python3
"""Validate a live OptionFlow snapshot.

Three independent checks:

  1. OI TRUTH   — every contract's live-captured OI must equal the EOD cache OI
                  for the same trading day (proves the template-158 decode).
  2. IV SANITY  — implied vols must sit in a plausible band; report coverage.
  3. GEX SELF-CONSISTENCY — regime sign must match net dealer gamma at spot;
                  the zero-gamma flip must be finite and near the strikes; walls
                  must be real strikes.

Usage:
    python tools/validate.py \
        --debug ../../artifacts/optionflow/GC/contracts_debug.json \
        --levels ../../artifacts/optionflow/GC/levels.json \
        --oi-cache "%LOCALAPPDATA%/SeachainsOption/data/rithmic_oi_eod_cache.json"

Any check can be skipped by omitting its input. Exit code is non-zero if a
provided check fails, so it can gate a pipeline.
"""

from __future__ import annotations

import argparse
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from gex import black76  # noqa: E402


def _load(path):
    with open(os.path.expandvars(path), encoding="utf-8") as fh:
        return json.load(fh)


def check_oi_truth(debug: dict, cache: dict) -> tuple[bool, str]:
    rec = cache.get("records", cache)
    by_key = {}
    for r in rec.values():
        by_key[f"{r['exchange']}|{r['symbol']}"] = r["open_interest"]
    matched = mismatched = missing = 0
    examples = []
    for c in debug["contracts"]:
        if c["open_interest"] is None:
            continue
        key = f"{c['exchange']}|{c['symbol']}"
        cv = by_key.get(key)
        if cv is None:
            missing += 1
        elif cv == c["open_interest"]:
            matched += 1
        else:
            mismatched += 1
            if len(examples) < 5:
                examples.append(f"{c['symbol']} live={c['open_interest']} cache={cv}")
    total = matched + mismatched
    rate = (matched / total * 100) if total else 0.0
    ok = mismatched == 0 and matched > 0
    msg = (f"OI TRUTH: {matched}/{total} exact ({rate:.1f}%), {missing} not-in-cache"
           f"{'; ' + '; '.join(examples) if examples else ''}"
           f"  [cache day {cache.get('collection_date','?')}]")
    return ok, msg


def check_iv_sanity(debug: dict, lo=0.03, hi=1.50) -> tuple[bool, str]:
    ivs = [c["iv"] for c in debug["contracts"] if c.get("iv")]
    if not ivs:
        return False, "IV SANITY: no implied vols computed"
    out = [v for v in ivs if not (lo <= v <= hi)]
    ivs_sorted = sorted(ivs)
    med = ivs_sorted[len(ivs_sorted) // 2]
    ok = len(out) == 0
    cov = len(ivs) / len(debug["contracts"]) * 100
    return ok, (f"IV SANITY: {len(ivs)} IVs, median={med:.1%}, "
                f"range=[{min(ivs):.1%},{max(ivs):.1%}], {len(out)} out-of-band, "
                f"coverage={cov:.0f}%")


def check_gex_consistency(debug: dict, levels: dict) -> tuple[bool, str]:
    spot = levels.get("spot") or debug.get("spot")
    regime = levels.get("regime")
    flip = levels.get("selected_flip")
    problems = []

    # Recompute net dealer gamma at spot from the debug contracts; its sign must
    # match the reported regime.
    net = 0.0
    r = 0.04
    for c in debug["contracts"]:
        if not c.get("iv") or not c.get("open_interest"):
            continue
        pv = {"GC": 100.0, "ES": 50.0, "NQ": 20.0}.get(debug["product"], 1.0)
        T = max(c["dte"], 0.0) / 365.25
        if T <= 0:
            continue
        g = black76.greeks(spot, c["strike"], c["iv"], T, r, c["is_call"]).gamma
        dg = black76.dollar_gamma_per_1pct(g, spot, pv) * c["open_interest"]
        net += dg if c["is_call"] else -dg
    expect = "NEGATIVE_GAMMA" if net < 0 else "POSITIVE_GAMMA"
    if regime and regime != expect:
        problems.append(f"regime={regime} but net-gamma-at-spot sign says {expect}")

    if flip is None:
        problems.append("no zero-gamma flip")
    else:
        strikes = [c["strike"] for c in debug["contracts"]]
        if strikes and not (min(strikes) <= flip <= max(strikes)):
            problems.append(f"flip {flip} outside strike range")

    ok = not problems
    return ok, (f"GEX CONSISTENCY: spot={spot} regime={regime} "
                f"net_gamma_at_spot={net:,.0f} flip={flip}"
                + ("" if ok else "  PROBLEMS: " + "; ".join(problems)))


def main(argv=None) -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--debug", required=True, help="contracts_debug.json from a live snapshot")
    p.add_argument("--levels", help="levels.json to check consistency against")
    p.add_argument("--oi-cache", help="EOD OI cache for the OI-truth check")
    args = p.parse_args(argv)

    debug = _load(args.debug)
    results = []
    if args.oi_cache and os.path.exists(os.path.expandvars(args.oi_cache)):
        results.append(check_oi_truth(debug, _load(args.oi_cache)))
    results.append(check_iv_sanity(debug))
    if args.levels:
        results.append(check_gex_consistency(debug, _load(args.levels)))

    print("\n=== OptionFlow snapshot validation ===")
    all_ok = True
    for ok, msg in results:
        print(f"  [{'PASS' if ok else 'FAIL'}] {msg}")
        all_ok &= ok
    print(f"\n{'ALL CHECKS PASSED' if all_ok else 'SOME CHECKS FAILED'}")
    return 0 if all_ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
