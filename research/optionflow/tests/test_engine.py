import pytest

from gex import engine
from gex.engine import ContractQuote, StrikeAgg


# ---------------------------------------------------------------------------
# Sign convention: calls add POSITIVE gex, puts add NEGATIVE gex.
# ---------------------------------------------------------------------------

def test_build_strike_table_sign_convention():
    F, r = 4082.0, 0.04
    quotes = [
        ContractQuote(strike=4100, is_call=True,  open_interest=500, iv=0.2, T=0.02, point_value=100.0),
        ContractQuote(strike=4100, is_call=False, open_interest=500, iv=0.2, T=0.02, point_value=100.0),
    ]
    table = engine.build_strike_table(quotes, F, r)
    agg = table[4100]
    assert agg.call_gex > 0.0
    assert agg.put_gex < 0.0
    assert agg.call_oi == 500 and agg.put_oi == 500
    # gamma_activity is magnitude-summed, so it is strictly positive.
    assert agg.gamma_activity > 0.0


def test_contract_without_iv_still_counts_oi_but_no_gex():
    table = engine.build_strike_table(
        [ContractQuote(strike=4000, is_call=False, open_interest=800, iv=None, T=0.02, point_value=100.0)],
        F=4082.0, r=0.04,
    )
    agg = table[4000]
    assert agg.put_oi == 800      # OI still recorded
    assert agg.put_gex == 0.0     # but no greek contribution invented


# ---------------------------------------------------------------------------
# Level extractors on a hand-built table (fully deterministic).
# ---------------------------------------------------------------------------

def _table(rows):
    t = {}
    for r in rows:
        t[r.strike] = r
    return t


def test_oi_walls_pick_max_oi_strike():
    table = _table([
        StrikeAgg(strike=4000, call_oi=10, put_oi=900),
        StrikeAgg(strike=4100, call_oi=700, put_oi=50),
        StrikeAgg(strike=4200, call_oi=300, put_oi=20),
    ])
    cwall, pwall = engine.oi_walls(table)
    assert cwall.price == 4100 and cwall.level_type == "CALL_OI_WALL"
    assert pwall.price == 4000 and pwall.level_type == "PUT_OI_WALL"


def test_net_gex_peaks_and_walls():
    table = _table([
        StrikeAgg(strike=4000, call_gex=1e6, put_gex=-9e6),
        StrikeAgg(strike=4100, call_gex=8e6, put_gex=-1e6),
    ])
    cwall = engine.call_gex_wall(table)
    pwall = engine.put_gex_wall(table)
    pos, neg = engine.net_gex_peaks(table)
    assert cwall.price == 4100          # largest positive call gex
    assert pwall.price == 4000          # most negative put gex
    assert pos.price == 4100            # net +7e6
    assert neg.price == 4000            # net -8e6


def test_max_pain_matches_independent_brute_force():
    # Independent reference computed inline; asserts engine == brute force.
    import random
    rng = random.Random(42)
    rows = []
    for k in range(3950, 4210, 10):
        rows.append(StrikeAgg(strike=float(k),
                              call_oi=rng.randint(0, 500),
                              put_oi=rng.randint(0, 500)))
    table = _table(rows)

    strikes = sorted(table)
    def pain(settle):
        tot = 0.0
        for s in strikes:
            a = table[s]
            if settle > s:
                tot += (settle - s) * a.call_oi
            elif settle < s:
                tot += (s - settle) * a.put_oi
        return tot
    ref = min(strikes, key=pain)

    lvl = engine.max_pain(table)
    assert lvl is not None
    assert lvl.price == ref


def test_zero_gamma_flip_brackets_a_sign_change():
    # A put-heavy low strike and call-heavy high strike create a flip in between.
    F, r = 4050.0, 0.04
    quotes = [
        ContractQuote(strike=4000, is_call=False, open_interest=5000, iv=0.25, T=0.02, point_value=100.0),
        ContractQuote(strike=4100, is_call=True,  open_interest=5000, iv=0.25, T=0.02, point_value=100.0),
    ]
    flip, zlo, zhi = engine.zero_gamma_flip(quotes, F, r)
    assert flip is not None
    assert 4000.0 <= flip.price <= 4100.0
    assert zlo <= flip.price <= zhi


def test_regime_none_without_iv():
    quotes = [ContractQuote(strike=4000, is_call=True, open_interest=10, iv=None, T=0.02, point_value=100.0)]
    assert engine.regime_at_spot(quotes, F=4050.0, r=0.04) is None
