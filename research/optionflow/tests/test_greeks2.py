"""Pin second-order greeks against finite differences of the first-order model.

If the closed form matches a bump-and-reprice to ~1e-4, the algebra is correct.
"""

import pytest

from gex import black76

F, K, SIGMA, T, R = 4080.0, 4100.0, 0.22, 5.0 / 365.0, 0.04


def _delta(F_, sig_, T_, is_call):
    return black76.greeks(F_, K, sig_, T_, R, is_call).delta


def _vega(sig_):
    return black76.greeks(F, K, sig_, T, R, True).vega


def _gamma(F_):
    return black76.greeks(F_, K, SIGMA, T, R, True).gamma


@pytest.mark.parametrize("is_call", [True, False])
def test_vanna_matches_ddelta_dsigma(is_call):
    h = 1e-5
    fd = (_delta(F, SIGMA + h, T, is_call) - _delta(F, SIGMA - h, T, is_call)) / (2 * h)
    g2 = black76.second_order_greeks(F, K, SIGMA, T, R, is_call)
    assert g2.vanna == pytest.approx(fd, abs=1e-4)


@pytest.mark.parametrize("is_call", [True, False])
def test_charm_matches_ddelta_dt(is_call):
    # charm = dDelta/dt = -dDelta/dT
    h = 1e-6
    dddT = (_delta(F, SIGMA, T + h, is_call) - _delta(F, SIGMA, T - h, is_call)) / (2 * h)
    g2 = black76.second_order_greeks(F, K, SIGMA, T, R, is_call)
    assert g2.charm == pytest.approx(-dddT, abs=1e-3)


def test_vomma_matches_dvega_dsigma():
    h = 1e-5
    fd = (_vega(SIGMA + h) - _vega(SIGMA - h)) / (2 * h)
    g2 = black76.second_order_greeks(F, K, SIGMA, T, R, True)
    assert g2.vomma == pytest.approx(fd, abs=1e-2)


def test_speed_matches_dgamma_dF():
    h = 1e-2
    fd = (_gamma(F + h) - _gamma(F - h)) / (2 * h)
    g2 = black76.second_order_greeks(F, K, SIGMA, T, R, True)
    assert g2.speed == pytest.approx(fd, rel=1e-3)


def test_vanna_call_put_equal():
    # vanna is sign-agnostic between call/put (same d1,d2)
    c = black76.second_order_greeks(F, K, SIGMA, T, R, True).vanna
    p = black76.second_order_greeks(F, K, SIGMA, T, R, False).vanna
    assert c == pytest.approx(p, abs=1e-12)
