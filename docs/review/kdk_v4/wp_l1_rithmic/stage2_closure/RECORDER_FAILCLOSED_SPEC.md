# Recorder fail-closed specification

**Governs:** the direct-Rithmic acquisition path only. The ATAS path is a separate
`acquisitionPath` and is out of scope here.
**Schemas:** `schemas/recorder_segment.schema.json`, `market_event.schema.json`,
`raw_frame.schema.json`, `capability_snapshot.schema.json`
**Tests:** `tests/test_stage2_closure.py` — 47/47

Every rule below is enforced by a schema constraint with a corresponding **negative** test.
A rule that exists only here and not in the schema is marked `PROSE_ONLY` and is not claimed
as enforced.

---

## 1. The one principle

**The recorder never emits `Ready` for a condition it did not verify.**

The failure mode this guards against is specific and measured: the client library reconnects
after an outage, replays subscriptions, logs `Reconnection successful`, and continues at a
sequence **3,975 higher** without telling the caller anything happened (`EV-RECONNECT-GAP`).
A recorder that trusts the library produces a file that looks continuous and is not.

## 2. Segment lifecycle

```
                     ┌──────────────┐
  open ─────────────▶│  ACCUMULATING│
                     └──────┬───────┘
              connection    │    clean close / roll
                  event     │
                     ┌──────▼───────┐        ┌───────────┐
                     │ GAP_ASSESSING│───────▶│  SEALED   │
                     └──────┬───────┘        └───────────┘
                            │
             discontinuity coincident with the connection event
                            │
                     ┌──────▼───────────────┐
                     │ SEALED_INVALID       │
                     │ containsUnrecovered  │
                     │ Gap = true           │
                     └──────────────────────┘
```

A segment is **sealed on every connection event**, not only at end of session. Two segments
either side of an outage are two artifacts, never one.

## 3. Gap detection — enforced

| rule | enforcement |
|---|---|
| track `last_sequence` per (instrument, subscription scope) across reconnects | `PROSE_ONLY` — runtime behaviour |
| a connection-coincident discontinuity sets `containsUnrecoveredGap = true` | `PROSE_ONLY` — runtime behaviour |
| `containsUnrecoveredGap = true` ⇒ `dataQuality = Invalid` | **schema** — *"unrecovered gap + dataQuality Ready is rejected"* |
| ⇒ at least one `discontinuities` entry | **schema** — *"unrecovered gap with no discontinuity record is rejected"* |
| ⇒ reason code `DATA_INVALID` | **schema** — *"unrecovered gap without DATA_INVALID reason code is rejected"* |
| `recoveryAttempted` is always `false`; `recoveryMechanism` is `none_available` | **schema** — *"claiming a recovery mechanism is rejected"* |
| `dataQuality = Ready` ⇒ `containsUnrecoveredGap = false` | **schema** |

**Discontinuity is not the same as a gap.** A per-price MBO subscription is *expected* to skip
— measured span 2,644 against 17 distinct values on one price. The distinguishing fact is
**coincidence with a connection event**, not magnitude. That is why `subscriptionScope` is a
required field: without it the sequence statistics are uninterpretable, and
`missingWithinSpan` would read as loss when it is scope.

**No threshold is used.** DQ-001's tolerance is an unbound Parameter Registry item (ADR-005),
so the recorder classifies on the connection-event correlation, which needs none.

## 4. Raw-frame preservation

Templates the client cannot parse are captured at the socket and persisted verbatim
(`ADR-007`). Enforced:

- a generic wire-format walk can **never** yield `semanticStatus = SCHEMA_BOUND`
  — *"generic wire decode cannot be SCHEMA_BOUND"*
- `decodedFields` keys must be **numeric field numbers**; a named key is rejected
  — *"a decoded field carrying a NAME is rejected"*
- unparseable frames are **counted per segment**, not logged per frame

Measured volume is small — 11 unparseable frames in a 200-frame session — so preservation is
unconditional and not gated on a parameter.

## 5. Options

| rule | enforcement |
|---|---|
| identity comes from vendor reference data only | **schema** — `identitySource` is a single-value enum, so symbol-string parsing cannot be expressed |
| `underlyingSymbol` required per series | **schema** — *"an option with no underlyingSymbol is rejected"* |
| `rollState = InvalidMapping` ⇒ `optionsState = Invalid` + `CONTRACT_MAPPING_INVALID` | **schema** (ROLL-004) |
| an OI value may exist only when its frame is `SCHEMA_BOUND` | **schema** |
| `optionsState = Ready` requires schema-bound OI on every contract | **schema** — *currently unsatisfiable by design* |

That last row is deliberate. While template 158 is client-blocked, **"Options Ready" cannot be
asserted at all** — not by a careless writer, not by a future refactor. It becomes reachable
only when a vendor `.proto` binds template 158, and that is a separate authorised change.

## 6. Core independence

`DataQuality` for the authoritative core (ADR-001) is computed from the core surfaces alone.
An MBO, options or session-statistic failure downgrades **its own** surface row in the
capability snapshot and **must not** downgrade the core segment. MRBS DQ-002 is the authority;
the manifest test *"no MBO surface is AUTHORITATIVE_CORE"* enforces the classification.

## 7. Startup and restart

On start, the recorder has no `last_sequence`. Therefore:

- the first segment of a process is `containsUnrecoveredGap = true` **unless** the recorder
  can prove continuity from a prior sealed segment of the same process lineage;
- proving continuity requires the prior segment's `endUtc`, its final sequence, and an
  unbroken connection — absent any of them, the interval is `Invalid`.

This is the deliberate conservative choice: an interval wrongly marked `Invalid` costs
research data; an interval wrongly marked `Ready` costs a wrong conclusion.

## 8. What this spec does not cover

- **Throughput and sustained-load behaviour.** Untested. The longest measured window is 90 s
  and the largest single capture is 3,973 aggregated-book messages.
- **The writer path.** No persistence implementation is specified or built here.
- **Per-instrument fan-out at scale.** The 21-price fan-out is the largest measured.
- **ATAS-path recorder behaviour.** Different `acquisitionPath`, different evidence.

None of these are claimed as `READY`.
