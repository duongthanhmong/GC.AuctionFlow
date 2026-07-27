"""Teach async_rithmic to survive (and capture) template 158 — the open-interest
message it does not know about.

async_rithmic.plants.base.BasePlant._convert_bytes_to_response raises
`Exception("Unknown template ID: 158")` because template 158 is absent from its
TEMPLATES_MAP. We wrap that method: when template_id == 158 we decode the frame
generically (rithmic.wire) into {field_number: [values]}, push it onto a sink,
and return the already-parsed Base message so the normal receive loop keeps going
instead of crashing. All other templates fall through to the original code.

Rithmic convention: template_id lives at field 154467. Symbol/exchange are utf-8
string fields; open interest is a small varint. We keep every field so the probe
can print them and we can lock the exact field numbers.
"""

from __future__ import annotations

from . import wire

TEMPLATE_OPEN_INTEREST = 158
TEMPLATE_ID_FIELD = 154467

# Field numbers locked from a live template-158 capture on rsc-jp / Rithmic Paper
# Trading (2026-07-28). Verified: GCQ6 future OI=139192, ESU6=1901727, NQU6=288907.
FIELD_SYMBOL = 110100
FIELD_EXCHANGE = 110101
FIELD_OPEN_INTEREST = 100064
FIELD_SSBOE = 150100

# Every captured template-158 frame, decoded to {field_number: [values]}.
OI_FRAMES: list[dict[int, list]] = []

_installed = False


def install() -> None:
    global _installed
    if _installed:
        return
    from async_rithmic.plants import base as base_mod

    orig = base_mod.BasePlant._convert_bytes_to_response

    def patched(self, buffer):
        raw = buffer[4:]
        b = base_mod.pb.base_pb2.Base()
        try:
            b.ParseFromString(raw)
        except Exception:
            return orig(self, buffer)
        if b.template_id == TEMPLATE_OPEN_INTEREST:
            try:
                OI_FRAMES.append(wire.decode(raw))
            except Exception:
                pass
            return b  # valid message; downstream has no handler and ignores it
        return orig(self, buffer)

    base_mod.BasePlant._convert_bytes_to_response = patched
    _installed = True


def clear() -> None:
    OI_FRAMES.clear()


def extract_open_interest(frame: dict[int, list], *,
                          symbol_field: int = FIELD_SYMBOL,
                          exchange_field: int = FIELD_EXCHANGE,
                          oi_field: int = FIELD_OPEN_INTEREST) -> dict:
    """Interpret one template-158 frame using the locked field numbers."""
    def first(fn):
        vals = frame.get(fn)
        return vals[0] if vals else None

    sym = wire.as_str(first(symbol_field)) if first(symbol_field) is not None else None
    exch = wire.as_str(first(exchange_field)) if first(exchange_field) is not None else None
    oi = first(oi_field)
    return {
        "symbol": sym,
        "exchange": exch,
        "open_interest": int(oi) if isinstance(oi, int) else None,
        "ssboe": first(FIELD_SSBOE),
    }
