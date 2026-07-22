# Decision Log

## D-P0-02-001 — Production and test TFM (superseded by D-P0-02A-001)

- **Decision (original):** Both projects target **net10.0**.
- **Date:** 2026-07-21

## D-P0-02-002 — One DLL / one indicator

- **Decision:** Single deployable `GC.AuctionFlow.dll`; visible name `GC AuctionFlow Engine`.
- **Basis:** Spec §2.6 / §49.2.
- **Date:** 2026-07-21

## D-P0-02-003 — ATAS_HOME reference resolution

- **Decision:** Resolve ATAS DLLs through `ATAS_HOME`; fail build if missing.
- **Date:** 2026-07-21

## D-P0-02-004 — Spool root (constant only)

- **Decision:** Spool root directory name `.gcae` under `%USERPROFILE%`. Writer not in P0-02/P0-03.
- **Date:** 2026-07-21

## D-P0-02-005 — CapabilityMatrix evidence identity

- **Decision:** Canonical workspace ES CapabilityMatrix SHA-256 is `471BDA044627624546709543740C59CD03EAE271D86ED93A16676F44115CFAC1`. Earlier uploaded ES matrix is a different artifact.
- **Date:** 2026-07-21

## D-P0-02-006 — Assembly version string

- **Decision:** NuGet `<Version>` uses SemVer without invalid prerelease numeric leading zeros.
- **Date:** 2026-07-21

## D-P0-02A-001 — TFM net10.0-windows + UseWPF

- **Decision:** Production/test TFM `net10.0-windows`; production `UseWPF=true` clears MSB3277.
- **Date:** 2026-07-21

## D-P0-02A-002 — Namespace GC.AuctionFlow.Atas

- **Decision:** Folder/namespace `Atas` to avoid shadowing `ATAS.Indicators.Indicator`.
- **Date:** 2026-07-21

## D-P0-02A-003 — Operator Indicators directory

- **Decision:** Deploy exactly one copy to `C:\Users\LOQ\AppData\Roaming\ATAS\Indicators`. Do not dual-install to Documents.
- **Smoke:** Operator confirmed P0-02 ATAS smoke PASS.
- **Date:** 2026-07-21

## D-P0-03-001 — Capability schema 1.0.0

- **Decision:** Capability / evidence contracts use SchemaVersion **1.0.0** and ProbeVersion placeholder **0.0.3** (superseded for probe runtime by D-P0-04-001). Axes kept separate (availability, coverage, computability, fidelity, sequence, MBO lifecycle, provenance). No confidence score. Unknown values remain explicit and serializable. ES evidence cannot be relabeled as GC via InstrumentId rewrite.
- **Date:** 2026-07-21

## D-P0-04A-001 — Trade Stream API audit PASS

- **Decision:** P0-04A PASS on ATAS **8.0.14.395**. Observed signatures on `ExtendedIndicator`: `OnNewTrade(MarketDataArg)`, `OnNewTrades(IEnumerable<MarketDataArg>)`, `OnCumulativeTrade(CumulativeTrade)`, `OnUpdateCumulativeTrade(CumulativeTrade)`. Payload types under `ATAS.Indicators`. `Lastprice` exact casing. No native trade/cumulative sequence ID.
- **Date:** 2026-07-21

## D-P0-04-001 — ProbeVersion 0.0.4 / artifact schema 1.0.0

- **Decision:** Runtime Trade Stream Probe uses ProbeVersion **0.0.4** and TradeStreamProbeSchemaVersion **1.0.0** (superseded schema by D-P0-04C-001).
- **Date:** 2026-07-21

## D-P0-04C-001 — cumulativeNewObservationCount / schema 1.0.1

- **Decision:** Rename serialized/runtime counter `newExecutionCount` → `cumulativeNewObservationCount`. Meaning: number of normalized observations from `OnCumulativeTrade` only — **not** unique or total exchange executions. TradeStreamProbeSchemaVersion **1.0.1**. ProbeVersion was **0.0.4** at that closeout. P0-04 GCQ6/Rithmic operator verification recorded PASS.
- **Date:** 2026-07-22

## D-P0-05A-001 — DOM API audit PASS

- **Decision:** P0-05A PASS on ATAS 8.0.14.395. Indicator depth surface: `MarketDepthChanged(MarketDataArg)`, `MarketDepthsChanged(IEnumerable<MarketDataArg>)`, `OnBestBidAskChanged(MarketDataArg)`, `GetMarketDepthSnapshot()` / `MarketDepthInfo.GetMarketDepthSnapshot()`. No action/sequence/level-index/reset on MarketDataArg.
- **Date:** 2026-07-22

## D-P0-05-001 — DOM semantics probe 0.0.5 / schema 1.0.0

- **Decision:** ProbeVersion **0.0.5**; DomSemanticsProbeSchemaVersion **1.0.0**; TradeStreamProbeSchemaVersion remains **1.0.1**. IL confirms `MarketDepthsChanged` foreach → `MarketDepthChanged`; derived override must not call base. DepthUpdateAction=Unknown only. DepthLevelObservationState is diagnostic last-observation (not an order book). Deferred snapshot pull via OnCalculate only. LiveDom/HistoricalDom/ReplayDom claims forced false in artifact.
- **Date:** 2026-07-22

## D-P0-05B-001 — GCQ6/Rithmic DOM operator evidence PASS

- **Decision:** P0-05 and P0-05 live operator verification = **PASS** on GCQ6 / Rithmic (two sessions). Evidence: `docs/evidence/P0-05B_DomSemantics_GCQ6_Rithmic_OperatorEvidence.md`. Run A SHA-256 `176DC185…C336`; Run B SHA-256 `ED424D7A…F3B539`. Batch/BBA/Snapshot OBSERVED; singular MarketDepthChanged NOT_OBSERVED_IN_TEST_WINDOW.
- **LiveDom lock:** Availability=Available; Coverage includes Live; Runtime presence=Observed; Fidelity=Partial (not fully Validated); NativeSequence=Absent; StableBookReconstruction=false.
- **Unchanged:** HistoricalDom=Unknown; ReplayDom=Unknown; VolumeMeaning/ZeroVolumeMeaning/UpdateAction=Unknown; no Delete inference from zero volume.
- **Date:** 2026-07-22

## D-P0-06A-001 — MBO API audit PASS

- **Decision:** P0-06A PASS on ATAS 8.0.14.395. Indicator path: `protected Task SubscribeMarketByOrderData()` → `IOnlineDataProvider.SubscribeMarketByOrdersData()`; callback `protected virtual void OnMarketByOrdersChanged(IEnumerable<MarketByOrder>)` EMPTY_RET. Payload `ATAS.DataFeedsCore.MarketByOrder` with Type=`MarketByOrderUpdateTypes` {Snapshot=0,New=1,Change=2,Delete=3}, Side=`MarketDataType`, ExchangeOrderId/Priority `long`. No MBO-specific unsubscribe. Do not attach provider `MarketByOrdersChanged` event in P0-06.
- **Date:** 2026-07-22

## D-P0-06-001 — MBO lifecycle probe 0.0.6 / schema 1.0.0

- **Decision:** ProbeVersion **0.0.6**; MboLifecycleProbeSchemaVersion **1.0.0** (superseded by D-P0-06B-001); TradeStreamProbeSchemaVersion **1.0.1** and DomSemanticsProbeSchemaVersion **1.0.0** preserved. Subscribe-once with Task outcome observation (TaskCompleted ≠ Succeeded ≠ callback presence). MboInterpretedLifecycleAction=Unknown; SnapshotCompletionKnown=false; all MBO capability claims forced false. Queue seed capacity 32768.
- **Date:** 2026-07-22

## D-P0-06B-001 — MBO probe diagnostic hardening / schema 1.0.1

- **Decision:** Keep ProbeVersion **0.0.6**; bump MboLifecycleProbeSchemaVersion to **1.0.1**. Early-return subscribe trigger so later OnCalculate does not inflate `duplicateSubscribeSuppressed`. Artifact fields `captureSubscriptionEpoch` + `finalClosedEpoch`. Initial time window from FirstCallbackReceiveUtc (5s seed) with batch/window item counts. Bounded `MboOrderObservationState` with UntrackedNonzeroIdDueToStateCapacity / StateCapacityReached.
- **Date:** 2026-07-22

## D-P0-06C-001 — Chart side-effect audit Decision B

- **Decision:** P0-06C PASS with root-cause **Decision B**. GCAE has no `this[bar]` / DataSeries write that can receive MBO prices (source + OnCalculate IL). BaseIndicator still constructs a default `ValueDataSeries` (**OBSERVED_IL**). Visual isolation APIs (`IsHidden`, `ScaleIt`, …) exist (**OBSERVED_API**) but are **not** applied speculatively. Abnormal M1 bar correlates with fresh MBO snapshot; ATAS mechanism unproven.
- **Date:** 2026-07-22

## D-P0-06D-001 — Controlled Chart A/B reproduction PASS + operational lock

- **Decision:** P0-06D PASS. Session `ced0cc72-dad1-431b-b542-e2d30611fa35`, artifact SHA-256 `AD5D12D7…C6D8` (companion verified). Fresh snapshot sequence (2131 Snapshot) with integrity zeros. Two GCQ6 charts in one ATAS session: abnormal vertical bar appeared on **both** the GCAE chart and the non-GCAE chart. Chart-local GCAE DataSeries writing not supported; shared ATAS/Rithmic instrument/provider/chart-data interaction strongly supported; exact mechanism Unknown. **Operational lock:** do not enable MBO subscription in the ATAS process used for primary GC analysis or trading; same-process separate chart is not proven isolation. P0-06 overall = **PASS WITH PLATFORM-SIDE OPERATIONAL LIMITATION**.
- **Evidence:** `docs/evidence/P0-06D_ChartAB_Reproduction_GCQ6_Rithmic.md`
- **Date:** 2026-07-22

## D-P0-07A-001 — Raw Event Recorder architecture PASS WITH LOCKED AMENDMENTS

- **Decision:** P0-07A architecture plan accepted with locked amendments: callback `RawEventDraft` vs writer `RawEventEnvelope`; writer-dequeue global sequence only; mandatory CRC32C length-framed container carrying UTF-8 JSON; exact multi-level accounting; segments authoritative / manifest recoverable index; MBO schema-capable but primary-process blocked; no exchange-feed completeness claims.
- **Date:** 2026-07-22

## D-P0-07B-001 — Recorder contracts, framing, segment, manifest, recovery

- **Decision:** Implement P0-07B only. RawEventRecorderSchemaVersion **1.0.0**; RawEventContainerVersion **1**. ProbeVersion **0.0.6** and probe schemas unchanged. No ATAS callback adapters, no indicator recorder settings, no MBO subscribe/record, no P0-07C. Storage under `%USERPROFILE%\.gcae\recorder\sessions\{SessionId}\`. Cumulative constituents not recorded. Capability claims forced false in manifests.
- **Date:** 2026-07-22

## D-P0-07B-002 — Closeout audit corrections

- **Decision:** P0-07B closeout audit fixed proven defects only: footer byte reservation in rotation; RawEventRecordCount / BytesBeforeFooter footer fields; RecordsWritten excludes header/footer; identity lifecycle carries previous/new identity tuples and is first RawEvent of new epoch segment; write-fault outcome accounting; recovery continues after valid-CRC malformed JSON but does not trust; CRC/length faults stop; quarantine copy failure reported; companion SHA uses uppercase hex + LF; invalid MaxSegmentBytes fails before recording.
- **Date:** 2026-07-22

## D-P0-04-002 — Base invocation from IL evidence

- **Decision (original P0-04):** IL on ATAS.Indicators 8.0.14.395: `OnNewTrade` / `OnCumulativeTrade` / `OnUpdateCumulativeTrade` / `BaseIndicator.OnDispose` = empty `ret`. `OnNewTrades` = non-trivial (foreach → `OnNewTrade`). Originally called `base.OnNewTrades`.
- **Superseded by D-P0-04B-001.**
- **Date:** 2026-07-21

## D-P0-04B-001 — Do not call base.OnNewTrades

- **Decision:** Because base `OnNewTrades` invokes virtual `OnNewTrade` per item, the derived override must enumerate the batch as `OnNewTradesBatch` and **must not** call `base.OnNewTrades`. Singular `OnNewTrade` represents only platform-direct singular callbacks. Overlap remains via fingerprint comparison without deduplication. Always `base.OnDispose()` in finally.
- **Date:** 2026-07-22

## D-P0-04B-002 — Identity bootstrap before ExpectedInstrumentCode gate

- **Decision:** Capture observed instrument snapshot before applying ExpectedInstrumentCode. Missing expected → `ExpectedInstrumentMissing`; mismatch → `InstrumentMismatch`; both increment `RejectedByInstrumentGate`, preserve observed identity, export diagnostic artifact with `CaptureAuthorized=false`. Never a LIVE capability PASS.
- **Date:** 2026-07-22

## D-P0-04B-003 — Callback observation terminology

- **Decision:** Zero callback count in a run → `NOT_OBSERVED_IN_TEST_WINDOW` (not Unavailable). Nonzero → `OBSERVED`. All four callbacks are not required for a successful operator run.
- **Date:** 2026-07-22

## D-P0-04-003 — Fingerprints and streams

- **Decision:** Fingerprints are diagnostic overlap evidence only (invariant CultureInfo pipe-joined fields). No fingerprint-based deletion; not trade IDs; not native sequence. No stream authoritative; no merged total volume.
- **Date:** 2026-07-21

## D-P0-04-004 — Artifact location

- **Decision:** Trade stream probe artifacts under `%USERPROFILE%\.gcae\capability\` with atomic rename + companion `.sha256` (hash not embedded in hashed JSON).
- **Date:** 2026-07-21
