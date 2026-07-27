"""Option-contract directory.

Loads an instrument-master JSON (CME contract specs: symbol, strike, put/call,
expiry) and selects the contracts worth subscribing for a snapshot: near the
money, within a DTE window. Days-to-expiry is recomputed from expiration_datetime
against "now" — never trusted from a stored field — so a slightly stale master
still yields correct scopes.

Default master: C:\\Users\\LOQ\\.gcae\\instrument_master_gcae.json (override with
GCAE_INSTRUMENT_MASTER). It is a static snapshot; refresh it when new expiries are
needed. A future step can regenerate it from Rithmic search instead.
"""

from __future__ import annotations

import datetime as _dt
import json
import os
from dataclasses import dataclass

DEFAULT_MASTER = os.path.join(os.path.expanduser("~"), ".gcae", "instrument_master_gcae.json")


@dataclass(frozen=True)
class OptionSpec:
    symbol: str
    product: str
    exchange: str
    is_call: bool
    strike: float
    expiry: _dt.datetime
    dte: float
    T: float          # years to expiry
    family: str | None


def load_master(path: str | None = None) -> dict:
    path = path or os.environ.get("GCAE_INSTRUMENT_MASTER", DEFAULT_MASTER)
    with open(path, encoding="utf-8") as fh:
        return json.load(fh)["options"]


def _parse_dt(s: str) -> _dt.datetime:
    # "2026-07-28 21:00:00+00:00"
    return _dt.datetime.fromisoformat(s)


def select_contracts(master: dict, product: str, now: _dt.datetime, spot: float,
                     *, max_dte: float = 7.0, strike_band_pct: float = 0.10,
                     limit: int | None = None) -> list[OptionSpec]:
    lo, hi = spot * (1 - strike_band_pct), spot * (1 + strike_band_pct)
    out: list[OptionSpec] = []
    for v in master.values():
        if v.get("product") != product:
            continue
        strike = float(v["strike"])
        if not (lo <= strike <= hi):
            continue
        exp = _parse_dt(v["expiration_datetime"])
        dte = (exp - now).total_seconds() / 86400.0
        if dte < 0 or dte > max_dte:
            continue
        T = max(dte, 0.0) / 365.25
        out.append(OptionSpec(
            symbol=v["raw_symbol"], product=product, exchange=v["exchange"],
            is_call=(v["option_type"] == "C"), strike=strike,
            expiry=exp, dte=dte, T=T, family=v.get("option_family"),
        ))
    # nearest-to-money first, so a limit keeps the most relevant strikes
    out.sort(key=lambda s: (abs(s.strike - spot), s.dte))
    if limit:
        out = out[:limit]
    return out


def scope_for(dte: float) -> str:
    return "0DTE" if dte < 1.0 else "SECONDARY_1_7_DTE"
