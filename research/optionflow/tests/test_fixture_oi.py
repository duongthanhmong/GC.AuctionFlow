"""Golden test against the real machine output.

Copy the live files in first (classifier blocks me from copying):

    copy "%LOCALAPPDATA%\\SeachainsOption\\data\\rithmic_oi_eod_cache.json" ^
         tests\\fixtures\\rithmic_oi_eod_cache.json

If the fixture is absent the test skips, so the suite stays green offline.
This proves symbol parsing + OI aggregation + OI walls + max-pain on ~800 real
COMEX/CME option records — the parts that need no live quote.
"""

import json
import os

import pytest

from gex import engine
from gex.contracts import parse_symbol
from gex.engine import ContractQuote

FIX = os.path.join(os.path.dirname(__file__), "fixtures", "rithmic_oi_eod_cache.json")


@pytest.fixture(scope="module")
def oi_records():
    if not os.path.exists(FIX):
        pytest.skip(f"fixture not present: {FIX}")
    with open(FIX, encoding="utf-8") as fh:
        return json.load(fh)["records"]


def _quotes_for(records, product):
    out = []
    for rec in records.values():
        c = parse_symbol(rec["exchange"], rec["symbol"])
        if c is None or c.product != product:
            continue
        out.append(ContractQuote(
            strike=c.strike, is_call=c.is_call,
            open_interest=int(rec["open_interest"]),
            iv=None, T=0.0, point_value=c.point_value_or_default,
        ))
    return out


def test_gc_records_parse_and_have_oi(oi_records):
    q = _quotes_for(oi_records, "GC")
    assert len(q) > 50                       # OGQ6 alone had ~180 in the sample
    assert all(x.open_interest >= 0 for x in q)
    assert {x.is_call for x in q} == {True, False}   # both sides present


def test_gc_oi_walls_land_on_real_strikes(oi_records):
    q = _quotes_for(oi_records, "GC")
    table = engine.build_strike_table(q, F=4082.0, r=0.04)  # F only matters for greeks (skipped)
    cwall, pwall = engine.oi_walls(table)
    assert cwall is not None and cwall.price in table
    assert pwall is not None and pwall.price in table
    assert table[cwall.price].call_oi == max(a.call_oi for a in table.values())


def test_gc_max_pain_is_within_strike_range(oi_records):
    q = _quotes_for(oi_records, "GC")
    table = engine.build_strike_table(q, F=4082.0, r=0.04)
    mp = engine.max_pain(table)
    assert mp is not None
    assert min(table) <= mp.price <= max(table)
