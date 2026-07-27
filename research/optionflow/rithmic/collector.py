"""Assemble ContractQuotes into a levels document.

Two entry points:

  * compute_oi_only_levels() — needs only open interest + strikes. Produces the
    OI walls and max-pain, which are REAL with no implied vol required. This is
    what lets you see option-derived levels immediately from an OI cache.

  * compute_full_levels() — needs implied vol per contract (from live option
    prices). Adds GEX/DEX walls, net-gex peaks, gamma-activity, and the zero-gamma
    flip / regime.

Both return a LevelsDoc ready for levels_writer.write_levels().
"""

from __future__ import annotations

import datetime as _dt

from gex import analytics, engine
from gex.engine import ContractQuote, StrikeAgg
from gex.levels_writer import LevelsDoc, OutLevel

RISK_FREE_RATE = 0.04   # override per environment if needed


def _now():
    now = _dt.datetime.now(_dt.timezone.utc)
    return now.isoformat(), int(now.timestamp())


def _out(level, scope, status="VALID"):
    if level is None:
        return None
    return OutLevel(
        level_id=f"{scope}_{level.level_type}",
        level_type=level.level_type,
        scope=scope,
        price=level.price,
        status=status,
        production_eligible=(status == "VALID"),
        formula_version=level.formula_version,
        detail=level.detail,
    )


def _coverage(table: dict[float, StrikeAgg]) -> dict:
    strikes = list(table.values())
    with_gex = sum(1 for s in strikes if s.call_contribs or s.put_contribs)
    with_oi = sum(1 for s in strikes if s.total_oi > 0)
    n = max(len(strikes), 1)
    return {
        "strike_count": len(strikes),
        "oi_coverage_pct": round(100.0 * with_oi / n, 2),
        "gex_contract_coverage_pct": round(100.0 * with_gex / n, 2),
    }


def compute_oi_only_levels(quotes: list[ContractQuote], product: str,
                           underlying_symbol: str = "", spot: float | None = None,
                           scope: str = "0DTE") -> LevelsDoc:
    table = engine.build_strike_table(quotes, F=(spot or 1.0), r=RISK_FREE_RATE)
    cwall, pwall = engine.oi_walls(table)
    max_pain = engine.max_pain(table)

    levels = [lv for lv in (
        _out(cwall, scope), _out(pwall, scope), _out(max_pain, scope),
    ) if lv]

    iso, epoch = _now()
    health = "LIVE" if levels else "BLOCKED"
    return LevelsDoc(
        product=product, underlying_symbol=underlying_symbol, spot=spot,
        regime=None, selected_flip=None, primary_expiry=None,
        published_at=iso, published_at_epoch=epoch, data_health=health,
        coverage=_coverage(table), levels=levels,
    )


def compute_full_levels(quotes: list[ContractQuote], F: float, product: str,
                        underlying_symbol: str = "", primary_expiry: str | None = None,
                        scope: str = "0DTE") -> LevelsDoc:
    table = engine.build_strike_table(quotes, F=F, r=RISK_FREE_RATE)

    cgex = engine.call_gex_wall(table)
    pgex = engine.put_gex_wall(table)
    pos_peak, neg_peak = engine.net_gex_peaks(table)
    coi, poi = engine.oi_walls(table)
    cvol, pvol = engine.volume_walls(table)
    gact = engine.gamma_activity_peak(table)
    max_pain = engine.max_pain(table)
    flip, zlo, zhi = engine.zero_gamma_flip(quotes, F, RISK_FREE_RATE)
    regime = engine.regime_at_spot(quotes, F, RISK_FREE_RATE)

    raw = [cgex, pgex, pos_peak, neg_peak, coi, poi, cvol, pvol, gact, max_pain, flip]
    levels = [lv for lv in (_out(x, scope) for x in raw) if lv]

    # ATM reference level (nearest strike with a two-sided market).
    atm_block = analytics.atm(quotes, F)
    if atm_block:
        levels.append(OutLevel(
            level_id=f"{scope}_ATM_STRIKE", level_type="ATM_STRIKE", scope=scope,
            price=atm_block["strike"], status="VALID", production_eligible=True,
            formula_version="ATM_1.0.0",
            detail={"straddle": atm_block["straddle"],
                    "straddle_move_to_expiry": atm_block["straddle_move_to_expiry"],
                    "atm_iv": atm_block["atm_iv"]}))

    flip_price = flip.price if flip else None
    if flip is not None:
        levels_by_type = {l.level_type: l for l in levels}
        if "TRUE_ZERO_GAMMA" in levels_by_type:
            levels_by_type["TRUE_ZERO_GAMMA"].detail.update(
                {"flip_zone_low": zlo, "flip_zone_high": zhi})

    iso, epoch = _now()
    return LevelsDoc(
        product=product, underlying_symbol=underlying_symbol, spot=F,
        regime=regime, selected_flip=flip_price, primary_expiry=primary_expiry,
        published_at=iso, published_at_epoch=epoch,
        data_health="LIVE" if levels else "BLOCKED",
        coverage=_coverage(table), levels=levels,
        analytics=analytics.build_analytics(quotes, F, RISK_FREE_RATE),
    )
