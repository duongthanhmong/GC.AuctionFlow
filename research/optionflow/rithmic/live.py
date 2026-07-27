"""Live snapshot orchestration: Rithmic -> ContractQuotes -> full GEX levels.

For one product:
  1. subscribe the front-month future, read its price F (Black-76 underlying),
  2. pick near-the-money option contracts within a DTE window from the master,
  3. subscribe them (quotes + open interest),
  4. collect a window of ticks + template-158 OI frames,
  5. imply vol per contract from its option mid/last, build ContractQuotes,
  6. compute full GEX levels and return the doc (+ per-contract coverage stats).
"""

from __future__ import annotations

import asyncio
import datetime as _dt

from gex import black76
from gex.engine import ContractQuote
from gex.levels_writer import LevelsDoc
from . import collector, instruments
from .client import OptionFlowRithmic
from .config import RithmicConfig


async def _future_price(rc: OptionFlowRithmic, product: str, exchange: str,
                        settle_secs: float, *, max_wait: float = 20.0) -> tuple[str, float | None]:
    """Subscribe the front-month future and poll for a price. A quiet product
    (e.g. GC in an off-hour) may not tick within the initial settle window, so we
    keep polling up to max_wait before giving up instead of failing fast."""
    fut = await rc.front_month_future(product, exchange)
    await rc.subscribe(fut, exchange, quotes=True, open_interest=False)
    waited = 0.0
    step = max(settle_secs, 1.0)
    while waited < max_wait:
        await asyncio.sleep(step)
        waited += step
        px = rc.mid_or_last(fut)
        if px:
            return fut, px
        step = 2.0
    return fut, rc.mid_or_last(fut)


async def snapshot_product(cfg: RithmicConfig, rc: OptionFlowRithmic, product: str, exchange: str,
                           *, master: dict, now: _dt.datetime,
                           max_dte: float = 7.0, strike_band_pct: float = 0.15,
                           max_contracts: int = 300, settle_secs: float = 4.0,
                           window_secs: float = 12.0) -> tuple[LevelsDoc | None, dict]:
    fut, F = await _future_price(rc, product, exchange, settle_secs)
    if not F:
        return None, {"error": f"no future price for {fut}"}

    specs = instruments.select_contracts(master, product, now, F,
                                          max_dte=max_dte, strike_band_pct=strike_band_pct,
                                          limit=max_contracts)
    if not specs:
        return None, {"error": f"no option contracts near {F} for {product}"}

    for s in specs:
        await rc.subscribe(s.symbol, s.exchange, quotes=True, open_interest=True)

    await asyncio.sleep(window_secs)

    oi_by_symbol = rc.open_interest_by_symbol()
    quotes: list[ContractQuote] = []
    debug: list[dict] = []   # per-contract record for the validator
    stats = {"future": fut, "spot": F, "selected": len(specs),
             "with_oi": 0, "with_iv": 0, "priced": 0}

    for s in specs:
        oi = oi_by_symbol.get(s.symbol)
        px = rc.mid_or_last(s.symbol)
        if oi:
            stats["with_oi"] += 1
        iv = None
        if px and px > 0 and s.T > 0:
            stats["priced"] += 1
            iv = black76.implied_vol(px, F, s.strike, s.T, collector.RISK_FREE_RATE, s.is_call)
            if iv:
                stats["with_iv"] += 1
        quotes.append(ContractQuote(
            strike=s.strike, is_call=s.is_call,
            open_interest=int(oi) if oi else 0,
            iv=iv, T=s.T, point_value=instruments_point_value(product),
            dte=s.dte, price=px,
        ))
        debug.append({"symbol": s.symbol, "exchange": s.exchange,
                      "strike": s.strike, "is_call": s.is_call,
                      "open_interest": int(oi) if oi else None,
                      "price": px, "iv": iv, "dte": round(s.dte, 3)})
    stats["contracts"] = debug

    doc = collector.compute_full_levels(
        quotes, F=F, product=product, underlying_symbol=fut,
        primary_expiry=min((s.expiry for s in specs), default=None).isoformat()
        if specs else None,
    )
    return doc, stats


def instruments_point_value(product: str) -> float:
    return {"GC": 100.0, "ES": 50.0, "NQ": 20.0}.get(product, 1.0)
