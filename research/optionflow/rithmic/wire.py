"""Minimal protobuf wire-format decoder.

Rithmic's open-interest message (template 158) is not in async_rithmic's proto
set, so we cannot parse it with a generated class. We do not need to: protobuf
is self-describing enough at the wire level to pull out the fields we want
(symbol string, open-interest varint) by field number. This decodes a message
into {field_number: [values]} without any .proto.

Wire types handled: 0 varint, 1 fixed64, 2 length-delimited (str/bytes/nested),
5 fixed32. That covers every field Rithmic uses in these messages.
"""

from __future__ import annotations

import struct


def _read_varint(buf: bytes, i: int) -> tuple[int, int]:
    shift = 0
    result = 0
    while True:
        b = buf[i]
        i += 1
        result |= (b & 0x7F) << shift
        if not (b & 0x80):
            return result, i
        shift += 7


def decode(buf: bytes) -> dict[int, list]:
    """Return {field_number: [value, ...]}. Length-delimited values are returned
    as raw bytes; the caller decides whether they are utf-8 strings or nested."""
    out: dict[int, list] = {}
    i, n = 0, len(buf)
    while i < n:
        tag, i = _read_varint(buf, i)
        field = tag >> 3
        wt = tag & 0x07
        if wt == 0:
            val, i = _read_varint(buf, i)
        elif wt == 2:
            ln, i = _read_varint(buf, i)
            val = buf[i:i + ln]
            i += ln
        elif wt == 5:
            val = struct.unpack_from("<I", buf, i)[0]
            i += 4
        elif wt == 1:
            val = struct.unpack_from("<Q", buf, i)[0]
            i += 8
        else:
            # unknown wire type -> stop rather than misread
            break
        out.setdefault(field, []).append(val)
    return out


def as_str(v) -> str | None:
    if isinstance(v, (bytes, bytearray)):
        try:
            return v.decode("utf-8")
        except UnicodeDecodeError:
            return None
    return None
