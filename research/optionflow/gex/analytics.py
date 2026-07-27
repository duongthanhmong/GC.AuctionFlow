"""Deep options analytics on top of the per-strike GEX table.

These are the metrics a professional gamma/flow desk watches beyond raw walls:

  * Vanna / Charm walls — where dealer vanna and charm exposure concentrate.
    Charm drives end-of-day / into-expiry drift; vanna drives moves as IV shifts.
  * Term structure — net GEX split by expiry bucket (0DTE vs 1-7DTE). 0DTE gamma
    pins intraday; longer-dated builds the structural walls.
  * Expected move — ATM implied vol projected to a 1-day / to-expiry range.
  * IV skew — OTM put IV minus OTM call IV; positive = downside demand.

Everything is derived from data already captured (OI + IV). Nothing fabricated;
a metric that lacks inputs returns None rather than a guess.
"""

from __future__ import annotations

import math
from dataclasses import dataclass

from . import black76, engine
from .engine import ContractQuote, StrikeAgg


def _argmax_abs(items, key):
    best = None
    for it in items:
        v = key(it)
        if v is None:
            continue
        if best is None or abs(v) > abs(best[1]):
            best = (it, v)
    return best if best else None


def vanna_wall(table: dict[float, StrikeAgg]):
    a = _argmax_abs(table.values(), lambda s: s.vanna_exp or None)
    return (a[0].strike, a[1]) if a else None


def charm_wall(table: dict[float, StrikeAgg]):
    a = _argmax_abs(table.values(), lambda s: s.charm_exp or None)
    return (a[0].strike, a[1]) if a else None


def term_structure(quotes: list[ContractQuote], F: float, r: float) -> dict:
    """Net dealer GEX split into 0DTE and 1-7DTE buckets."""
    buckets = {"0DTE": [], "1_7DTE": []}
    for q in quotes:
        if q.dte is None:
            continue
        (buckets["0DTE"] if q.dte < 1.0 else buckets["1_7DTE"]).append(q)
    out = {}
    for name, qs in buckets.items():
        if not qs:
            out[name] = None
            continue
        table = engine.build_strike_table(qs, F, r)
        net = sum(s.net_gex for s in table.values())
        out[name] = {"net_gex": net, "strikes": len(table),
                     "contracts": sum(1 for q in qs if q.iv)}
    return out


def _atm_iv(quotes: list[ContractQuote], F: float) -> float | None:
    priced = [(abs(q.strike - F), q.iv) for q in quotes if q.iv and q.iv > 0]
    if not priced:
        return None
    priced.sort(key=lambda x: x[0])
    near = [iv for _, iv in priced[:4]]   # average a few nearest-the-money IVs
    return sum(near) / len(near)


def expected_move(quotes: list[ContractQuote], F: float) -> dict | None:
    """Project ATM IV to a 1-day and to-nearest-expiry move (1-sigma)."""
    atm = _atm_iv(quotes, F)
    if atm is None:
        return None
    dtes = [q.dte for q in quotes if q.dte is not None and q.dte >= 0]
    near_dte = min(dtes) if dtes else None
    one_day = F * atm * math.sqrt(1.0 / 365.25)
    to_exp = F * atm * math.sqrt(max(near_dte, 0.0) / 365.25) if near_dte is not None else None
    return {"atm_iv": atm, "one_day_move": one_day,
            "to_expiry_move": to_exp, "expiry_dte": near_dte}


def iv_skew(quotes: list[ContractQuote], F: float, wing_pct: float = 0.03) -> dict | None:
    """OTM put IV minus OTM call IV around +/- wing_pct of spot."""
    put_k = F * (1 - wing_pct)
    call_k = F * (1 + wing_pct)

    def nearest_iv(target, is_call):
        cands = [(abs(q.strike - target), q.iv) for q in quotes
                 if q.is_call == is_call and q.iv and q.iv > 0]
        if not cands:
            return None
        cands.sort(key=lambda x: x[0])
        return cands[0][1]

    put_iv = nearest_iv(put_k, is_call=False)
    call_iv = nearest_iv(call_k, is_call=True)
    if put_iv is None or call_iv is None:
        return None
    return {"put_iv": put_iv, "call_iv": call_iv, "skew": put_iv - call_iv,
            "put_strike": round(put_k), "call_strike": round(call_k)}


def atm(quotes: list[ContractQuote], F: float) -> dict | None:
    """At-the-money reference: nearest strike to spot that has BOTH a call and a
    put priced. Returns the ATM strike, the straddle price (call+put), the
    straddle-implied expected move to expiry (~= the straddle, the market's own
    priced move), and ATM IV. Distinct from the IV-based expected_move()."""
    calls = {q.strike: q for q in quotes if q.is_call and q.price and q.price > 0}
    puts = {q.strike: q for q in quotes if not q.is_call and q.price and q.price > 0}
    common = sorted(set(calls) & set(puts), key=lambda k: abs(k - F))
    if not common:
        return None
    k = common[0]
    c, p = calls[k], puts[k]
    straddle = c.price + p.price
    ivs = [x.iv for x in (c, p) if x.iv]
    atm_iv = sum(ivs) / len(ivs) if ivs else None
    return {
        "strike": k,
        "call_price": c.price,
        "put_price": p.price,
        "straddle": straddle,
        # Market-implied expected move to expiry: the ATM straddle is ~E|move|.
        "straddle_move_to_expiry": straddle,
        "atm_iv": atm_iv,
        "dte": c.dte,
        "distance_from_spot": k - F,
    }


def gamma_profile_curve(quotes: list[ContractQuote], F: float, r: float,
                        *, span_pct: float = 0.06, points: int = 25) -> dict | None:
    """Total dealer net dollar-gamma re-evaluated across a grid of hypothetical
    spot prices. This is the CURVE (not just the point at spot): it shows where
    dealer hedging flips from suppressing to amplifying moves. Also reports the
    slope at spot (negative slope near spot = unstable/trend-prone)."""
    priced = [q for q in quotes if q.iv and q.iv > 0 and q.T > 0]
    if not priced:
        return None
    lo, hi = F * (1 - span_pct), F * (1 + span_pct)

    def net_gamma_at(X: float) -> float:
        tot = 0.0
        for q in priced:
            g = black76.greeks(X, q.strike, q.iv, q.T, r, q.is_call).gamma
            dg = black76.dollar_gamma_per_1pct(g, X, q.point_value) * q.open_interest
            tot += dg if q.is_call else -dg
        return tot

    curve = []
    for i in range(points):
        x = lo + (hi - lo) * i / (points - 1)
        curve.append({"price": round(x, 2), "net_gamma": net_gamma_at(x)})
    # local slope at spot via central difference
    h = (hi - lo) / (points - 1)
    slope = (net_gamma_at(F + h) - net_gamma_at(F - h)) / (2 * h)
    return {"curve": curve, "slope_at_spot": slope,
            "net_gamma_at_spot": net_gamma_at(F)}


def dealer_positioning(quotes: list[ContractQuote], F: float, r: float) -> dict:
    """Aggregate dealer book snapshot: total signed GEX/DEX and gross gamma."""
    table = engine.build_strike_table(quotes, F, r)
    total_gex = sum(s.net_gex for s in table.values())
    total_dex = sum(s.net_dex for s in table.values())
    gross_gamma = sum(s.gamma_activity for s in table.values())
    return {"total_net_gex": total_gex, "total_net_dex": total_dex,
            "gross_gamma": gross_gamma,
            "posture": "SHORT_GAMMA" if total_gex < 0 else "LONG_GAMMA"}


def build_analytics(quotes: list[ContractQuote], F: float, r: float) -> dict:
    table = engine.build_strike_table(quotes, F, r)
    vw = vanna_wall(table)
    cw = charm_wall(table)
    return {
        "atm": atm(quotes, F),
        "vanna_wall": {"strike": vw[0], "exposure": vw[1]} if vw else None,
        "charm_wall": {"strike": cw[0], "exposure": cw[1]} if cw else None,
        "term_structure": term_structure(quotes, F, r),
        "expected_move": expected_move(quotes, F),
        "iv_skew": iv_skew(quotes, F),
        "gamma_profile_curve": gamma_profile_curve(quotes, F, r),
        "dealer_positioning": dealer_positioning(quotes, F, r),
    }
