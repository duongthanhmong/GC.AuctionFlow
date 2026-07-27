"""Per-strike GEX/DEX aggregation and level extraction.

Methodology is the public one both reference tools state in their output:

    gex_unit               USD_PER_1_PERCENT_UNDERLYING_MOVE
    aggregation            SUM_SIGNED_BLACK76_GEX_BY_STRIKE
    dealer_sign_assumption CALL_POSITIVE_PUT_NEGATIVE
    normalization          NET_GEX / MAX_ABS_NET_GEX
    regime_rule            SIGN_OF_NET_STRUCTURAL_GEX_AT_SPOT (evaluated at spot)

Everything is derived; nothing is fabricated. A strike with no valid greek stays
absent from the GEX table rather than being invented, mirroring GCAE's data-gate
discipline (a level that never had a VALID value is never displayed).
"""

from __future__ import annotations

from dataclasses import dataclass, field

from . import black76


@dataclass(frozen=True)
class ContractQuote:
    """One option contract at snapshot time, ready for greeks."""
    strike: float
    is_call: bool
    open_interest: int
    iv: float | None          # implied vol; None -> excluded from GEX/DEX (still counts for OI/max-pain)
    T: float                  # years to expiry
    point_value: float
    volume: int | None = None
    dte: float | None = None  # days to expiry, for term-structure bucketing
    price: float | None = None  # option mid/last, for ATM straddle / market EM


@dataclass
class StrikeAgg:
    strike: float
    call_oi: int = 0
    put_oi: int = 0
    call_vol: int = 0
    put_vol: int = 0
    call_gex: float = 0.0     # signed dollar GEX per 1% move, calls (>=0)
    put_gex: float = 0.0      # signed dollar GEX per 1% move, puts (<=0)
    call_dex: float = 0.0
    put_dex: float = 0.0
    gamma_activity: float = 0.0   # sum |gamma*oi| dollarized — dealer-sign-agnostic
    vanna_exp: float = 0.0    # signed dealer vanna exposure (call+ put-)
    charm_exp: float = 0.0    # signed dealer charm exposure (call+ put-)
    call_contribs: int = 0
    put_contribs: int = 0

    @property
    def net_gex(self) -> float:
        return self.call_gex + self.put_gex

    @property
    def net_dex(self) -> float:
        return self.call_dex + self.put_dex

    @property
    def total_oi(self) -> int:
        return self.call_oi + self.put_oi


@dataclass
class Level:
    level_type: str
    price: float
    formula_version: str
    detail: dict = field(default_factory=dict)


# ---------------------------------------------------------------------------
# Aggregation
# ---------------------------------------------------------------------------

def build_strike_table(quotes: list[ContractQuote], F: float, r: float) -> dict[float, StrikeAgg]:
    table: dict[float, StrikeAgg] = {}
    for q in quotes:
        agg = table.setdefault(q.strike, StrikeAgg(strike=q.strike))
        if q.is_call:
            agg.call_oi += q.open_interest
            if q.volume:
                agg.call_vol += q.volume
        else:
            agg.put_oi += q.open_interest
            if q.volume:
                agg.put_vol += q.volume

        if q.iv is None or q.iv <= 0 or q.T <= 0:
            continue  # OI counted above, but no greeks -> no GEX/DEX contribution
        g = black76.greeks(F, q.strike, q.iv, q.T, r, q.is_call)
        g2 = black76.second_order_greeks(F, q.strike, q.iv, q.T, r, q.is_call)
        dollar_gamma = black76.dollar_gamma_per_1pct(g.gamma, F, q.point_value) * q.open_interest
        dollar_delta = black76.dollar_delta(g.delta, F, q.point_value) * q.open_interest
        # vanna/charm exposures share the CALL_POSITIVE/PUT_NEGATIVE dealer sign.
        vanna_e = g2.vanna * q.open_interest * q.point_value
        charm_e = g2.charm * q.open_interest * q.point_value
        sign = 1.0 if q.is_call else -1.0
        agg.gamma_activity += abs(dollar_gamma)
        agg.vanna_exp += sign * vanna_e
        agg.charm_exp += sign * charm_e
        if q.is_call:
            agg.call_gex += dollar_gamma          # CALL_POSITIVE
            agg.call_dex += dollar_delta
            agg.call_contribs += 1
        else:
            agg.put_gex -= dollar_gamma           # PUT_NEGATIVE
            agg.put_dex += dollar_delta
            agg.put_contribs += 1
    return table


# ---------------------------------------------------------------------------
# Level extraction (each returns None when the input can't support it)
# ---------------------------------------------------------------------------

def _argmax(items, key):
    best = None
    for it in items:
        v = key(it)
        if v is None:
            continue
        if best is None or v > best[1]:
            best = (it, v)
    return best[0] if best else None


def call_gex_wall(table: dict[float, StrikeAgg]) -> Level | None:
    a = _argmax(table.values(), lambda s: s.call_gex if s.call_gex > 0 else None)
    return Level("CALL_GEX_WALL", a.strike, "GEX_WALLS_1.0.0", {"call_gex": a.call_gex}) if a else None


def put_gex_wall(table: dict[float, StrikeAgg]) -> Level | None:
    # Most negative put GEX == largest magnitude.
    a = _argmax(table.values(), lambda s: -s.put_gex if s.put_gex < 0 else None)
    return Level("PUT_GEX_WALL", a.strike, "GEX_WALLS_1.0.0", {"put_gex": a.put_gex}) if a else None


def net_gex_peaks(table: dict[float, StrikeAgg]) -> tuple[Level | None, Level | None]:
    pos = _argmax(table.values(), lambda s: s.net_gex if s.net_gex > 0 else None)
    neg = _argmax(table.values(), lambda s: -s.net_gex if s.net_gex < 0 else None)
    pl = Level("POSITIVE_NET_GEX_PEAK", pos.strike, "GEX_WALLS_1.0.0", {"net_gex": pos.net_gex}) if pos else None
    nl = Level("NEGATIVE_NET_GEX_PEAK", neg.strike, "GEX_WALLS_1.0.0", {"net_gex": neg.net_gex}) if neg else None
    return pl, nl


def oi_walls(table: dict[float, StrikeAgg]) -> tuple[Level | None, Level | None]:
    c = _argmax(table.values(), lambda s: s.call_oi if s.call_oi > 0 else None)
    p = _argmax(table.values(), lambda s: s.put_oi if s.put_oi > 0 else None)
    cl = Level("CALL_OI_WALL", c.strike, "OI_WALLS_1.0.0", {"call_oi": c.call_oi}) if c else None
    pl = Level("PUT_OI_WALL", p.strike, "OI_WALLS_1.0.0", {"put_oi": p.put_oi}) if p else None
    return cl, pl


def volume_walls(table: dict[float, StrikeAgg]) -> tuple[Level | None, Level | None]:
    c = _argmax(table.values(), lambda s: s.call_vol if s.call_vol > 0 else None)
    p = _argmax(table.values(), lambda s: s.put_vol if s.put_vol > 0 else None)
    cl = Level("CALL_VOLUME_WALL", c.strike, "VOLUME_WALLS_1.0.0", {"call_vol": c.call_vol}) if c else None
    pl = Level("PUT_VOLUME_WALL", p.strike, "VOLUME_WALLS_1.0.0", {"put_vol": p.put_vol}) if p else None
    return cl, pl


def gamma_activity_peak(table: dict[float, StrikeAgg]) -> Level | None:
    a = _argmax(table.values(), lambda s: s.gamma_activity if s.gamma_activity > 0 else None)
    return Level("GAMMA_ACTIVITY_PEAK", a.strike, "GAMMA_ACTIVITY_1.0.0", {"gamma_activity": a.gamma_activity}) if a else None


def max_pain(table: dict[float, StrikeAgg]) -> Level | None:
    """Strike minimizing total intrinsic payout to option holders at expiry,
    weighted by open interest. Needs only strikes + OI (works offline)."""
    strikes = sorted(s for s, agg in table.items() if agg.total_oi > 0)
    if len(strikes) < 2:
        return None
    best = None
    for settle in strikes:
        pain = 0.0
        for s in strikes:
            agg = table[s]
            if settle > s:      # calls at strike s are ITM by (settle - s)
                pain += (settle - s) * agg.call_oi
            elif settle < s:    # puts at strike s are ITM by (s - settle)
                pain += (s - settle) * agg.put_oi
        if best is None or pain < best[1]:
            best = (settle, pain)
    return Level("MAX_PAIN", best[0], "MAX_PAIN_1.0.0", {"total_pain": best[1]})


def zero_gamma_flip(
    quotes: list[ContractQuote],
    F: float,
    r: float,
    *,
    grid_lo: float | None = None,
    grid_hi: float | None = None,
    steps: int = 400,
) -> tuple[Level | None, float | None, float | None]:
    """Scan a hypothetical-spot grid and find where TOTAL dealer net dollar-gamma
    changes sign (the gamma flip / true zero gamma). Returns (flip level, zone_low,
    zone_high) using the crossings that bracket the flip. Live-only: needs IV.

    Dealer net gamma at candidate spot X = sum over contracts of signed dollar
    gamma (calls +, puts -) with each contract's own IV, re-evaluated at X.
    """
    priced = [q for q in quotes if q.iv and q.iv > 0 and q.T > 0]
    if not priced:
        return None, None, None
    ks = [q.strike for q in priced]
    lo = grid_lo if grid_lo is not None else min(min(ks), F) * 0.97
    hi = grid_hi if grid_hi is not None else max(max(ks), F) * 1.03
    if hi <= lo:
        return None, None, None

    def net_gamma_at(X: float) -> float:
        tot = 0.0
        for q in priced:
            g = black76.greeks(X, q.strike, q.iv, q.T, r, q.is_call)
            dg = black76.dollar_gamma_per_1pct(g.gamma, X, q.point_value) * q.open_interest
            tot += dg if q.is_call else -dg
        return tot

    prev_x = lo
    prev_v = net_gamma_at(lo)
    crossings: list[float] = []
    for i in range(1, steps + 1):
        x = lo + (hi - lo) * i / steps
        v = net_gamma_at(x)
        if prev_v == 0.0:
            crossings.append(prev_x)
        elif (prev_v < 0.0) != (v < 0.0):
            # linear interpolation of the zero crossing
            t = prev_v / (prev_v - v)
            crossings.append(prev_x + (x - prev_x) * t)
        prev_x, prev_v = x, v
    if not crossings:
        return None, None, None
    # Flip nearest to spot; zone spans the bracketing crossings if several.
    flip = min(crossings, key=lambda c: abs(c - F))
    zone_low = min(crossings)
    zone_high = max(crossings)
    return (
        Level("TRUE_ZERO_GAMMA", flip, "ZERO_GAMMA_1.0.0",
              {"crossings": crossings, "regime_at_spot":
               "NEGATIVE_GAMMA" if net_gamma_at(F) < 0 else "POSITIVE_GAMMA"}),
        zone_low,
        zone_high,
    )


def regime_at_spot(quotes: list[ContractQuote], F: float, r: float) -> str | None:
    priced = [q for q in quotes if q.iv and q.iv > 0 and q.T > 0]
    if not priced:
        return None
    tot = 0.0
    for q in priced:
        g = black76.greeks(F, q.strike, q.iv, q.T, r, q.is_call)
        dg = black76.dollar_gamma_per_1pct(g.gamma, F, q.point_value) * q.open_interest
        tot += dg if q.is_call else -dg
    return "NEGATIVE_GAMMA" if tot < 0 else "POSITIVE_GAMMA"
