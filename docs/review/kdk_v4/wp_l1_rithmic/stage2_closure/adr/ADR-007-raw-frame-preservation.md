# ADR-007 — Preserve frames the client cannot parse; preservation is not interpretation

**Status:** ACCEPTED
**Date:** 2026-07-31
**Evidence:** `EV-UNMAPPED-OI-155-157`, `EV-PERBIT-UNMAPPED`, `EV-PERBIT-ATTRIBUTION`
**MRBS anchor:** §23.1 event sourcing (state rebuildable from the log, not just the final
signal); §21 OPT-002; §18 DQ-003

## Context

The vendor sends at least eight streaming templates this client has no message class for.
Today they raise `Exception: Unknown template ID` inside the library's process loop and are
**destroyed**. Attribution measured per `UpdateBits`:

| bit | template | parsed |
|---|---|---|
| `OPEN`, `HIGH_LOW` | 152 | no |
| `HIGH_BID_LOW_ASK` | 153 | no |
| `OPENING_INDICATOR` | 154 | no |
| `CLOSE`, `SETTLEMENT`, `PROJECTED_SETTLEMENT` | 155 | no |
| `MARKET_MODE` | 157 | no |
| **`OPEN_INTEREST`** | **158** | no |
| `MARGIN_RATE` | 162 | no |
| `HIGH_PRICE_LIMIT`, `LOW_PRICE_LIMIT` | 163 | no |

Discarding them is an irreversible loss of entitled data. Interpreting them without a vendor
`.proto` would be inventing semantics.

## Decision

The recorder captures unparseable frames at the socket and persists them **verbatim**:

```
RawFrameRecord
  template_id        int        read from the Base message
  captured_utc       string     aware UTC
  framed_bytes       int        including the 4-byte length prefix
  payload_base64     string     the exact bytes, unmodified
  decoded_fields     object?    OPTIONAL generic wire-format walk
  decode_method      enum       "none" | "generic_wireformat" | "vendor_proto"
  semantic_status    enum       "UNINTERPRETED" | "FIELD_NUMBERS_ONLY" | "SCHEMA_BOUND"
```

Rules:

1. `payload_base64` is the record. Everything else is derived and may be recomputed.
2. `decoded_fields` from a generic wire-format walk yields **field numbers and wire types
   only**. Those are facts read off the bytes. A field *name* or *meaning* is never written
   unless `decode_method = "vendor_proto"`.
3. `semantic_status` must be `SCHEMA_BOUND` before any analytic may consume the value. A
   `FIELD_NUMBERS_ONLY` frame is archival, not input.
4. Raw frames never satisfy OPT-002. A point-in-time OI snapshot built from a
   `FIELD_NUMBERS_ONLY` frame is `Unusable`.
5. Preservation is unconditional and cheap — the measured volume is small (11 unparseable
   frames in a 200-frame session). It is not gated on a parameter.

## Consequences

- The day a vendor `.proto` or a maintained mapping arrives, the archive is **replayable**:
  every preserved frame can be re-decoded and promoted to `SCHEMA_BOUND` without a new live
  capture. That is the entire point.
- The recorder must not log the unparseable-frame exception per frame at ERROR; it counts
  them and emits one summary per segment, or a busy session drowns its own log.

## Why this is not "collecting data we do not understand"

The alternative is destroying entitled data that is provably present and provably useful —
`OGU6 C4120 open interest = 174` was read off these bytes. Preserving it costs a base64
string; recovering it later costs another live session that may not be reproducible.
