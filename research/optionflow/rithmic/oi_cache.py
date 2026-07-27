"""Load an open-interest cache (the KEY|CONTRACT -> {open_interest} JSON shape)
into ContractQuotes grouped by product. Works fully offline.
"""

from __future__ import annotations

import json

from gex.contracts import parse_symbol
from gex.engine import ContractQuote


def load_oi_cache(path: str) -> dict[str, list[ContractQuote]]:
    with open(path, encoding="utf-8") as fh:
        doc = json.load(fh)
    records = doc.get("records", doc)  # tolerate either wrapped or bare
    by_product: dict[str, list[ContractQuote]] = {}
    for rec in records.values():
        exch = rec.get("exchange", "")
        sym = rec.get("symbol", "")
        c = parse_symbol(exch, sym)
        if c is None:
            continue
        oi = rec.get("open_interest")
        if oi is None:
            continue
        by_product.setdefault(c.product, []).append(ContractQuote(
            strike=c.strike, is_call=c.is_call, open_interest=int(oi),
            iv=None, T=0.0, point_value=c.point_value_or_default,
        ))
    return by_product
