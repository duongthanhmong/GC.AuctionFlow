import math

import pytest

from gex import black76


def test_atm_call_equals_put_and_known_value():
    F = K = 100.0
    sigma, T, r = 0.20, 1.0, 0.0
    c = black76.price(F, K, sigma, T, r, is_call=True)
    p = black76.price(F, K, sigma, T, r, is_call=False)
    assert c == pytest.approx(p, abs=1e-12)          # ATM, r=0 -> call == put
    assert c == pytest.approx(7.9655674, abs=1e-4)   # closed-form reference


def test_put_call_parity():
    F, K, sigma, T, r = 105.0, 100.0, 0.25, 0.5, 0.03
    c = black76.price(F, K, sigma, T, r, is_call=True)
    p = black76.price(F, K, sigma, T, r, is_call=False)
    parity = math.exp(-r * T) * (F - K)
    assert (c - p) == pytest.approx(parity, abs=1e-10)


def test_gamma_symmetric_and_known():
    F, K, sigma, T, r = 100.0, 100.0, 0.20, 1.0, 0.0
    gc = black76.greeks(F, K, sigma, T, r, is_call=True).gamma
    gp = black76.greeks(F, K, sigma, T, r, is_call=False).gamma
    assert gc == pytest.approx(gp, abs=1e-15)        # gamma identical call/put
    assert gc == pytest.approx(0.0198476, abs=1e-6)


def test_delta_signs_and_bounds():
    F, K, sigma, T, r = 100.0, 100.0, 0.2, 0.5, 0.0
    dc = black76.greeks(F, K, sigma, T, r, is_call=True).delta
    dp = black76.greeks(F, K, sigma, T, r, is_call=False).delta
    assert 0.0 < dc < 1.0
    assert -1.0 < dp < 0.0
    assert dc - dp == pytest.approx(1.0, abs=1e-9)   # e^{-rT}=1 here


def test_implied_vol_round_trip():
    F, K, T, r = 4082.0, 4100.0, 3.0 / 365.0, 0.04
    true_sigma = 0.18
    px = black76.price(F, K, true_sigma, T, r, is_call=True)
    iv = black76.implied_vol(px, F, K, T, r, is_call=True)
    assert iv == pytest.approx(true_sigma, abs=1e-5)


def test_implied_vol_below_intrinsic_returns_none():
    F, K, T, r = 4200.0, 4000.0, 0.01, 0.0
    intrinsic = F - K
    assert black76.implied_vol(intrinsic - 5.0, F, K, T, r, is_call=True) is None


def test_dollar_gamma_unit_scales_with_point_value():
    F, K, sigma, T, r = 4082.0, 4082.0, 0.2, 0.02, 0.04
    g = black76.greeks(F, K, sigma, T, r, is_call=True).gamma
    dg_gc = black76.dollar_gamma_per_1pct(g, F, point_value=100.0)
    dg_es = black76.dollar_gamma_per_1pct(g, F, point_value=50.0)
    assert dg_gc == pytest.approx(2.0 * dg_es, rel=1e-12)
    assert dg_gc > 0.0
