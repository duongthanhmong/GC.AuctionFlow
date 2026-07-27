"""Option-contract identity.

In LIVE mode every field (product, strike, call/put, expiry, underlying, point
value) comes straight from Rithmic `ResponseReferenceData` — authoritative, no
guessing. This module's string parser exists only for OFFLINE fixtures such as
`rithmic_oi_eod_cache.json`, whose records carry just the raw symbol + OI.

Rithmic CME/COMEX option symbols look like:  "OGQ6 C4080"  "E4AN6 P7435"
    <root><monthcode><yeardigit> <C|P><strike>
"""

from __future__ import annotations

import re
from dataclasses import dataclass

# COMEX gold option roots all map to GC. CME index-option roots start with 'E'
# (S&P / ES family) or 'Q' (Nasdaq / NQ family). This table is only consulted by
# the offline string parser; the live collector never needs it.
_COMEX_GOLD_ROOTS_PREFIX = ("OG", "G")  # OGxx monthly, G5W/G5R weekly, etc.

# Futures contract multipliers ("single_point_value"). Live mode reads this from
# reference data; kept here so the fixture path can compute dollar greeks too.
POINT_VALUE = {"GC": 100.0, "ES": 50.0, "NQ": 20.0}

_SYMBOL_RE = re.compile(r"^(?P<root>[A-Z0-9]+)\s+(?P<cp>[CP])(?P<strike>\d+(?:\.\d+)?)$")


@dataclass(frozen=True)
class OptionContract:
    product: str          # GC / ES / NQ
    exchange: str         # COMEX / CME
    symbol: str           # raw Rithmic trading symbol
    is_call: bool
    strike: float
    open_interest: int | None = None
    point_value: float | None = None

    @property
    def point_value_or_default(self) -> float:
        return self.point_value if self.point_value is not None else POINT_VALUE.get(self.product, 1.0)


def product_from_root(exchange: str, root: str) -> str | None:
    ex = exchange.upper()
    if ex == "COMEX":
        return "GC"
    if ex == "CME":
        if root.startswith("E"):
            return "ES"
        if root.startswith("Q"):
            return "NQ"
    # Fall back to explicit prefix check regardless of exchange field quality.
    if any(root.startswith(p) for p in _COMEX_GOLD_ROOTS_PREFIX):
        return "GC"
    return None


def parse_symbol(exchange: str, symbol: str) -> OptionContract | None:
    """Parse an offline-cache option symbol. Returns None if it is not an option
    (e.g. a future) or the root cannot be mapped to a tracked product."""
    m = _SYMBOL_RE.match(symbol.strip())
    if not m:
        return None
    root = m.group("root")
    product = product_from_root(exchange, root)
    if product is None:
        return None
    return OptionContract(
        product=product,
        exchange=exchange.upper(),
        symbol=symbol,
        is_call=(m.group("cp") == "C"),
        strike=float(m.group("strike")),
    )
