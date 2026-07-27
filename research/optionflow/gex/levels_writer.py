"""Emit the GCAE OptionFlow levels.json contract.

This is OUR schema (schema_version gcae-optionflow-v1), not any third party's.
Design goals borrowed from GCAE's data-gate discipline:

  * every level carries an explicit `status` (VALID / STALE / HELD / BLOCKED) and
    `production_eligible` / `display_eligible` flags — a consumer never has to
    guess whether a number is real.
  * a level that has never held a VALID value is omitted, never faked.
  * atomic write (tmp + os.replace) so a reader with a shared handle never sees
    a half-written file.
"""

from __future__ import annotations

import json
import os
import tempfile
from dataclasses import asdict, dataclass, field

SCHEMA_VERSION = "gcae-optionflow-v1"


@dataclass
class OutLevel:
    level_id: str
    level_type: str
    scope: str                 # 0DTE / SECONDARY_1_7_DTE
    price: float | None
    status: str                # VALID / STALE / HELD / BLOCKED / MISSING
    production_eligible: bool
    formula_version: str
    detail: dict = field(default_factory=dict)


@dataclass
class LevelsDoc:
    product: str
    underlying_symbol: str
    spot: float | None
    regime: str | None
    selected_flip: float | None
    primary_expiry: str | None
    published_at: str
    published_at_epoch: int
    data_health: str
    coverage: dict
    levels: list[OutLevel]
    analytics: dict | None = None   # deep metrics: vanna/charm walls, term structure, EM, skew
    schema_version: str = SCHEMA_VERSION

    def to_json(self) -> dict:
        d = asdict(self)
        return d


def write_levels(doc: LevelsDoc, data_root: str) -> str:
    """Atomically write artifacts/optionflow/<PRODUCT>/levels.json. Returns path."""
    out_dir = os.path.join(data_root, doc.product)
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, "levels.json")
    payload = json.dumps(doc.to_json(), indent=2, ensure_ascii=False)
    # Atomic replace so ATAS/Sierra readers never see a torn file.
    fd, tmp = tempfile.mkstemp(dir=out_dir, prefix=".levels.", suffix=".tmp")
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(payload)
        os.replace(tmp, path)
    finally:
        if os.path.exists(tmp):
            os.remove(tmp)
    return path
