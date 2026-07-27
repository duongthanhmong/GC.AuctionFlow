"""Black-76 pricing and greeks for options on futures.

Public-domain mathematics. Nothing here derives from any third-party tool; it is
the standard Black (1976) model for European options whose underlying is a
futures price F (as opposed to Black-Scholes on a spot price S).

Under Black-76 with futures F, strike K, vol sigma, time-to-expiry T (years),
risk-free rate r:

    d1 = (ln(F/K) + 0.5*sigma^2*T) / (sigma*sqrt(T))
    d2 = d1 - sigma*sqrt(T)
    call = e^{-rT} [ F*N(d1) - K*N(d2) ]
    put  = e^{-rT} [ K*N(-d2) - F*N(-d1) ]

Greeks used for GEX/DEX:
    gamma = e^{-rT} * phi(d1) / (F * sigma * sqrt(T))     # same for call and put
    delta_call =  e^{-rT} * N(d1)
    delta_put  = -e^{-rT} * N(-d1)

`gamma` here is dGamma per 1.0 change in F. Dollar-gamma per 1% underlying move
(the "USD_PER_1_PERCENT_UNDERLYING_MOVE" unit both reference tools report) is:

    dollar_gamma_1pct = gamma * (F*0.01)**2 * point_value

which is what engine.py multiplies by open interest.
"""

from __future__ import annotations

import math
from dataclasses import dataclass

SQRT_2 = math.sqrt(2.0)
SQRT_2PI = math.sqrt(2.0 * math.pi)


def norm_pdf(x: float) -> float:
    return math.exp(-0.5 * x * x) / SQRT_2PI


def norm_cdf(x: float) -> float:
    # erf-based standard normal CDF; matches scipy.stats.norm.cdf to ~1e-15.
    return 0.5 * (1.0 + math.erf(x / SQRT_2))


@dataclass(frozen=True)
class Greeks:
    price: float
    delta: float
    gamma: float
    vega: float


def _d1_d2(F: float, K: float, sigma: float, T: float) -> tuple[float, float]:
    if F <= 0.0 or K <= 0.0 or sigma <= 0.0 or T <= 0.0:
        raise ValueError("black76 requires F,K,sigma,T all > 0")
    v = sigma * math.sqrt(T)
    d1 = (math.log(F / K) + 0.5 * sigma * sigma * T) / v
    return d1, d1 - v


def price(F: float, K: float, sigma: float, T: float, r: float, is_call: bool) -> float:
    d1, d2 = _d1_d2(F, K, sigma, T)
    disc = math.exp(-r * T)
    if is_call:
        return disc * (F * norm_cdf(d1) - K * norm_cdf(d2))
    return disc * (K * norm_cdf(-d2) - F * norm_cdf(-d1))


def greeks(F: float, K: float, sigma: float, T: float, r: float, is_call: bool) -> Greeks:
    d1, d2 = _d1_d2(F, K, sigma, T)
    disc = math.exp(-r * T)
    v = sigma * math.sqrt(T)
    gamma = disc * norm_pdf(d1) / (F * v)
    vega = disc * F * norm_pdf(d1) * math.sqrt(T)  # per 1.00 change in sigma
    if is_call:
        delta = disc * norm_cdf(d1)
        px = disc * (F * norm_cdf(d1) - K * norm_cdf(d2))
    else:
        delta = -disc * norm_cdf(-d1)
        px = disc * (K * norm_cdf(-d2) - F * norm_cdf(-d1))
    return Greeks(price=px, delta=delta, gamma=gamma, vega=vega)


def implied_vol(
    target_price: float,
    F: float,
    K: float,
    T: float,
    r: float,
    is_call: bool,
    *,
    lo: float = 1e-4,
    hi: float = 5.0,
    tol: float = 1e-8,
    max_iter: int = 100,
) -> float | None:
    """Recover sigma from a market option price by bisection.

    Returns None when the target is below intrinsic or otherwise not invertible,
    so callers can mark the contract MISSING_IV rather than fabricate a vol.
    """
    disc = math.exp(-r * T)
    intrinsic = disc * (max(F - K, 0.0) if is_call else max(K - F, 0.0))
    if target_price < intrinsic - 1e-9:
        return None
    # Price is monotone increasing in sigma; bracket then bisect.
    p_lo = price(F, K, lo, T, r, is_call)
    p_hi = price(F, K, hi, T, r, is_call)
    if not (p_lo - 1e-12 <= target_price <= p_hi + 1e-12):
        return None
    a, b = lo, hi
    for _ in range(max_iter):
        mid = 0.5 * (a + b)
        pm = price(F, K, mid, T, r, is_call)
        if abs(pm - target_price) < tol:
            return mid
        if pm < target_price:
            a = mid
        else:
            b = mid
    return 0.5 * (a + b)


@dataclass(frozen=True)
class Greeks2:
    """Second-order greeks used by professional GEX/flow desks.

    vanna = dDelta/dSigma  (also dVega/dF): drives 'vanna flows' as IV shifts.
    charm = dDelta/dt      (delta decay): drives end-of-day / expiry pinning.
    vomma = dVega/dSigma   (vol convexity).
    speed = dGamma/dF      (gamma slope).

    All are per-contract, per-unit; the engine multiplies by OI, point value and
    the dealer sign to get exposures. Formulas are Black-76 (b=0 cost of carry);
    tests pin every one against a finite-difference of price/delta/gamma/vega.
    """
    vanna: float
    charm: float
    vomma: float
    speed: float


def second_order_greeks(F: float, K: float, sigma: float, T: float, r: float, is_call: bool) -> Greeks2:
    d1, d2 = _d1_d2(F, K, sigma, T)
    disc = math.exp(-r * T)
    v = sigma * math.sqrt(T)
    phi = norm_pdf(d1)
    gamma = disc * phi / (F * v)

    vanna = -disc * phi * d2 / sigma
    vomma = disc * F * phi * math.sqrt(T) * d1 * d2 / sigma   # vega * d1*d2/sigma
    speed = -gamma / F * (d1 / v + 1.0)

    # charm = dDelta/dt = -dDelta/dT. For Black-76 (b=0), with
    # d(d1)/dT = -d1/(2T) + sigma/(2 sqrt(T))  ->  phi*d2/(2T) common term.
    common = phi * d2 / (2.0 * T)
    if is_call:
        charm = disc * (r * norm_cdf(d1) + common)
    else:
        charm = disc * (-r * norm_cdf(-d1) + common)
    return Greeks2(vanna=vanna, charm=charm, vomma=vomma, speed=speed)


def dollar_gamma_per_1pct(gamma: float, F: float, point_value: float) -> float:
    """Convert raw dGamma/dF into USD per 1% underlying move (per contract)."""
    move = F * 0.01
    return gamma * move * move * point_value


def dollar_delta(delta: float, F: float, point_value: float) -> float:
    """Signed dollar delta per contract (delta notional at F)."""
    return delta * F * point_value
