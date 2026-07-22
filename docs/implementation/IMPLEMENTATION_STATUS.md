# Implementation Status

| Field | Value |
|-------|--------|
| Current phase | **P0-07C2 CLOSEOUT AUDIT** — Callback grouping + fan-out foundation |
| Probe version | **0.0.6** (unchanged) |
| RawEventRecorderSchemaVersion | **1.1.0** |
| RawEventContainerVersion | **1** (unchanged) |
| Trade / Dom / Mbo probe schemas | **1.0.1** / **1.0.0** / **1.0.1** (unchanged) |
| P0-07A | **PASS WITH LOCKED AMENDMENTS** |
| P0-07B | **PASS** (`gcae-p0-07b-recorder-core-pass`) |
| P0-07C1 | **PASS / Decision B** |
| P0-07C2 | **CLOSEOUT AUDIT COMPLETE — pending review** |
| P0-07C3 / P0-07C4 | **NOT STARTED** |

## Phase checklist

| Phase | Status |
|-------|--------|
| P0-07B recorder core | **PASS** |
| P0-07C1 integration audit | **PASS / Decision B** |
| P0-07C2 callback grouping + fan-out foundation | **READY FOR CLOSEOUT REVIEW** |
| P0-07C3 Trade callback integration | **NOT STARTED** |
| P0-07C4 DOM callback integration | **NOT STARTED** |

## P0-07C2 locks

- Schema **1.1.0**; schema **1.0.0** unsupported for live trust
- CallbackInvocationSequence per CallbackSource (Interlocked CAS); first value **1**; no silent wrap at `long.MaxValue` (throws `CallbackInvocationSequenceExhausted`)
- CallbackItemOrdinal starts at 0; null raw items and mapper-failure positions consume ordinals; stream-local sequence is separate
- Callback receive stamps captured once per invocation; `ReceiveUtc` / `ReceiveStopwatchTimestamp` are `[JsonIgnore]` aliases of Callback* fields
- CallbackInvocationResultPayload is recorder metadata (not a market event); absence ⇒ completion unknown
- Null singular: PayloadItemsEnumerated=1, NullItemObservations=1, EnumerationCompleted=true, FinalItemCountKnown=true; no market event
- Sink rejection/fault ≠ NormalizationFailures (`FanOutItemRejections` / `FanOutItemFaults`)
- PrimitiveFanOutCoordinator: capability then recorder (infra order ≠ market order); independent sinks
- **Approved P0-07C3 queue policy (governance only, not wired):** CallbackInvocationResult uses the same recorder queue/writer ordering as market drafts; no separate control channel; not counted as NormalizedObservations; requires separate counters:
  - InvocationResultEmissionAttempts = AcceptedToQueue + QueueFullDrops + Faults
  - plus InvocationResultsWritten
- **Record-count taxonomy gap (required narrow amendment at start of P0-07C3):** footer/manifest today expose only `RawEventRecordCount`. Preferred future invariant: RawEventRecordCount = MarketEventRecordCount + InvocationResultRecordCount + LifecycleIntegrityRecordCount. PayloadKind/`RawEventRecordCategory` already discriminate in JSON; do not redesign segment framing in C2.
- BBA dual-sided MarketDataArg shape remains **UNKNOWN** (defer to P0-07C4A); no BestBidAsk mapper
- MBO: SchemaSupported=true, RecordingEnabled=false, IsolatedEnvironmentOnly; no recorder SubscribeMarketByOrderData
- No indicator wiring, no recorder operator settings, no live enablement
- P0-07C3 **not started**

## Operational lock (P0-06D, retained)

**MBO subscription must not be enabled in the ATAS process used for primary GC analysis or trading.**
