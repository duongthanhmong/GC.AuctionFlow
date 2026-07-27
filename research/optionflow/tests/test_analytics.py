import pytest

from gex import analytics
from gex.engine import ContractQuote


def _chain(F=4080.0):
    # A small synthetic chain across two expiry buckets with IV + OI + price.
    qs = []
    for k in range(4000, 4161, 20):
        for is_call in (True, False):
            px = max(F - k, 0) + 20 if is_call else max(k - F, 0) + 20
            qs.append(ContractQuote(strike=float(k), is_call=is_call, open_interest=100,
                                    iv=0.22, T=0.5 / 365.25, point_value=100.0, dte=0.5, price=px))
            qs.append(ContractQuote(strike=float(k), is_call=is_call, open_interest=50,
                                    iv=0.20, T=5.0 / 365.25, point_value=100.0, dte=5.0, price=px + 5))
    return qs


def test_atm_picks_nearest_two_sided_strike():
    a = analytics.atm(_chain(), F=4080.0)
    assert a is not None
    assert a["strike"] == 4080.0            # nearest to spot with call+put priced
    assert a["straddle"] == pytest.approx(a["call_price"] + a["put_price"])
    assert a["straddle_move_to_expiry"] == a["straddle"]


def test_atm_none_when_one_sided():
    calls_only = [ContractQuote(strike=4080.0, is_call=True, open_interest=1,
                                iv=0.2, T=0.01, point_value=100.0, price=15.0)]
    assert analytics.atm(calls_only, F=4080.0) is None


def test_term_structure_splits_buckets():
    ts = analytics.term_structure(_chain(), F=4080.0, r=0.04)
    assert ts["0DTE"] is not None and ts["1_7DTE"] is not None
    assert ts["0DTE"]["strikes"] > 0
    assert ts["1_7DTE"]["strikes"] > 0


def test_expected_move_positive_and_scales_with_iv():
    em = analytics.expected_move(_chain(), F=4080.0)
    assert em is not None
    assert em["atm_iv"] == pytest.approx(0.22, abs=0.02)
    assert em["one_day_move"] > 0
    # to-expiry move for the nearest (0.5 dte) should be smaller than a 1-day move
    assert em["to_expiry_move"] < em["one_day_move"]


def test_iv_skew_zero_when_symmetric():
    sk = analytics.iv_skew(_chain(), F=4080.0)
    assert sk is not None
    # equal IVs on both wings at the same dte -> ~0 skew
    assert abs(sk["skew"]) < 0.05


def test_build_analytics_shape():
    a = analytics.build_analytics(_chain(), F=4080.0, r=0.04)
    assert set(a) >= {"vanna_wall", "charm_wall", "term_structure", "expected_move", "iv_skew"}


def test_empty_chain_returns_none_not_crash():
    a = analytics.build_analytics([], F=4080.0, r=0.04)
    assert a["expected_move"] is None
    assert a["iv_skew"] is None
    assert a["gamma_profile_curve"] is None


def test_gamma_profile_curve_shape_and_slope():
    c = analytics.gamma_profile_curve(_chain(), F=4080.0, r=0.04, points=11)
    assert c is not None
    assert len(c["curve"]) == 11
    assert c["curve"][0]["price"] < c["curve"][-1]["price"]  # ascending grid
    assert "slope_at_spot" in c and "net_gamma_at_spot" in c


def test_dealer_positioning_posture_matches_sign():
    dp = analytics.dealer_positioning(_chain(), F=4080.0, r=0.04)
    expected = "SHORT_GAMMA" if dp["total_net_gex"] < 0 else "LONG_GAMMA"
    assert dp["posture"] == expected
    assert dp["gross_gamma"] >= 0
