# Dataset contracts

What a consumer may assume about a dataset produced by the direct-Rithmic recorder, and what
it must check. Each contract names its enforcing schema and test.

**No dataset described here exists yet.** These are the contracts a dataset must satisfy
before it may be used. Stating them now is what prevents a future capture from being
retro-fitted into a calibration set it does not qualify for.

---

## DS-1 — Core trade/quote dataset

**Contains:** executed trades, BBO, aggregated book (ADR-001)
**Schema:** `market_event.schema.json`, `recorder_segment.schema.json`

A consumer may assume:

- prices are **integer ticks**, with `tickSize` from vendor reference data — never a float
  and never an assumed tick size;
- `eventTimeUtc`, `receiveTimeUtc` and `processingTimeUtc` are three distinguishable values,
  each timezone-aware;
- `aggressorSide` came from the vendor when `aggressorProvenance = vendor_field`;
- every record resolves a `capabilitySnapshotId` (DQ-003) and carries three version fields
  (VER-001);
- a segment marked `Ready` contains **no** connection-coincident sequence gap.

A consumer must check:

- `dataQuality` per segment, not per file;
- `sourceMix` — reject mixed-source segments if the analysis requires a single source;
- `sequenceProvenance` before treating `sequence` as an exchange sequence. Templates 150/151
  carry no native sequence; that field is recorder-assigned there.

## DS-2 — MBO research telemetry

**Schema:** `recorder_segment.schema.json` with `subscriptionScope`
**Status:** `RESEARCH_TELEMETRY` — **never** a core input (ADR-002, MRBS DQ-002)

A consumer may assume events are as-received in arrival order with native sequence.

A consumer must **not** assume:

- that a book can be reconstructed. Measured: template 116 seeds **exactly one order per
  price**, leaving 75 CHANGE and 16 DELETE addressing orders never seeded, and 3 of 21 price
  levels matching the vendor's own book (`EV-REBUILD-DIVERGES`);
- that `missingWithinSpan` is loss. On a per-price subscription it is scope.

**Prohibited use:** any queue-position, book-depth or order-count feature derived from MBO
entering a core analytic while ADR-002 stands.

## DS-3 — Options context dataset

**Schema:** `options_snapshot.schema.json`
**Status:** `OPTIONS_CONTEXT`, `optionsState` at best `ContextOnly` today

Assumable: identity, strike, expiry, put/call, point value and quotes — all vendor-verified
(`EV-EXPLOIT-OPTION-REFERENCE`, `EV-EXPLOIT-OPTION-QUOTES`).

Not assumable, and structurally unrepresentable:

- open interest and settlement values — client-blocked, `openInterest` must be `null`;
- any GEX/dealer/regime figure — `derivedExposure` requires formula, sign and assumption
  (OPT-004), and without OI none can be stated honestly;
- **that options belong to the futures front month.** Measured `OGU6 → GCV6` against a front
  month of `GCZ6`. `underlyingSymbol` is read per series.

## DS-4 — Historical replay dataset

**Status:** `ReplayCompatible`, published as a **separate capability** from live (DQ-004)

- Records arrive on template 207 as **tick bars**. In the measured sample **180 of 180** had
  `num_trades = 1` and `open == high == low == close` — trade-by-trade inside a bar envelope.
  Describe it that way; it is not a raw tick feed.
- Parity against live is **76 of 77** over one 90-second window (`EV-HIST-PARITY`). That is
  evidence, not equivalence, and it does not merge the two sources.
- **Request windows must be timezone-aware UTC.** A naive datetime is localised to the system
  timezone by `plants/base.py:567-577`; on this machine that silently shifted a window by
  7 hours and returned 85 plausible-looking wrong records.

## DS-5 — Raw frame archive

**Schema:** `raw_frame.schema.json` (ADR-007)

- `payloadBase64` is the record; everything else is derived.
- `semanticStatus = FIELD_NUMBERS_ONLY` is **archival**. Only `SCHEMA_BOUND` may feed an
  analytic, and only `decodeMethod = vendor_proto` can produce it.
- The archive is **replayable**: when a vendor schema arrives, every preserved frame can be
  re-decoded without a new live session.

---

## Qualification for calibration — none of these datasets qualifies today

A dataset may be used for calibration or walk-forward only when **all** hold:

| # | requirement | status |
|---|---|---|
| 1 | point-in-time, no look-ahead (MRBS §24.1) | schema supports it; **no dataset exists** |
| 2 | full provenance: `dataSourceId`, `capabilitySnapshotId`, three versions per record | enforced by schema |
| 3 | segment-level `dataQuality = Ready` across the calibration window | enforceable; **unmeasured over any real window** |
| 4 | no unrecovered gap in the window | enforced by schema |
| 5 | sufficient sample size and warm-up | **parameter unbound** (ADR-005) |
| 6 | out-of-sample period held back | **not run** |

**Requirements 1, 3, 5 and 6 are unmet.** No fixture, synthetic series or
provenance-less capture may substitute for any of them. The longest real capture in evidence
is 160 seconds.
