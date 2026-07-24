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

## D-P0-07C1-001 — Integration audit Decision B

- **Decision:** P0-07C1 PASS / **Decision B**. Current P0-07B contracts lack honest callback batch grouping (`CallbackInvocationSequence`, `CallbackItemOrdinal`, shared per-callback receive stamps). Do not overload `StreamLocalCaptureSequence`. Narrow amendment required before Trade/DOM callback integration. BBA dual-sided `MarketDataArg` shape remains UNKNOWN pending P0-07C4A.
- **Date:** 2026-07-22

## D-P0-07C2-001 — Schema 1.1.0 callback grouping + fan-out foundation

- **Decision:** RawEventRecorderSchemaVersion **1.1.0**. Add callback grouping fields; `ReceiveUtc` compatibility alias equals `CallbackReceiveUtc`. Add `CallbackInvocationResultPayload`. Null raw items consume ordinals. `PrimitiveFanOutCoordinator` + `SinglePassEnumerationHelper` are callback-neutral test/infra only — **not** wired to indicator. No live recorder settings, no Trade/DOM adapters, no BBA mapping, no MBO enablement. Schema 1.0.0 classified unsupported for live trust (no live 1.0.0 evidence produced).
- **Date:** 2026-07-22

## D-P0-07C2-002 — Closeout audit corrections + C3 queue/taxonomy policy

- **Decision:** P0-07C2 closeout audit fixed proven defects only: CallbackInvocationSequence overflow does not wrap; single-pass uses explicit GetEnumerator/MoveNext/Current with stage-sanitized failures; sink rejection/fault separated from NormalizationFailures (`FanOutItemRejections`/`FanOutItemFaults`); ordinal exhaustion guarded. Approved for P0-07C3 (governance only, not implemented in C2): invocation-result records share the market draft queue/writer order (no control channel); separate InvocationResult* counters with Attempts = Accepted + QueueFull + Faults; footer/manifest must gain Market/InvocationResult/LifecycleIntegrity split counts (narrow amendment — no segment-frame redesign). BBA remains UNKNOWN until P0-07C4A. P0-07C3 not started.
- **Date:** 2026-07-22

## D-P0-07C3A-001 — Trade integration architecture PASS

- **Decision:** P0-07C3A PASS. Dual-map within single enumeration; schema 1.2.0 for category counts; same-queue invocation results; no CumulativeTrade.Ticks on recorder path; DirectionRaw/DataTypeRaw required; decompose as C3BC (accounting+Trade wire) then C3D live.
- **Date:** 2026-07-22

## D-P0-07C3BC-001 — Trade recorder accounting + callback integration

- **Decision:** Implement P0-07C3BC. RawEventRecorderSchemaVersion **1.2.0**. Footer/manifest category counts + InvocationResult* counters. TradeToRawEventAdapter + TradeRecorderHost wired into OnNewTrade/OnNewTrades/OnCumulativeTrade/OnUpdateCumulativeTrade. Dispose: recorder then probes then base.OnDispose. No DOM/BBA/MBO recorder work. No CumulativeTrade.Ticks access. Tagged `gcae-p0-07c3bc-trade-recorder-pass` @ `25bf03c`.
- **Date:** 2026-07-22

## D-P0-07C3D-001 — Live verification interim (precheck/deploy only)

- **Decision:** P0-07C3D precheck + single Indicators DLL deploy PASS on baseline `25bf03c` / tag `gcae-p0-07c3bc-trade-recorder-pass`. Controlled live GCQ6/Rithmic session **not executed** in agent turn (no recorder artifacts). Overall recommendation **FAIL — LIVE SESSION NOT EXECUTED**. Evidence: `docs/evidence/P0-07C3D_GCQ6_Rithmic_TradeRecorder_LiveVerification.md`. Fresh ATAS restart required before any live claim. No commit/tag for C3D.
- **Date:** 2026-07-22
- **Superseded by D-P0-07C3D-002.**

## D-P0-07C3D-002 — Live Trade recorder verification PASS + metadata closeout

- **Decision:** P0-07C3D **PASS**. Metadata closeout (EnabledStreams Trade-only; normal IndicatorDispose termination; callback counters; GCAR UInt16 verifier). Validated live session `01f6650494194d3bacbe00062dede326` (GCQ6/Rithmic, ~6 min, schema 1.2.0, GCAR v1, 5390 raw events, CallbackInvocations=InvocationResultEmissionAttempts=2695, AbnormalTermination=false). Tagged `gcae-p0-07c3d-live-trade-recorder-pass`. DOM/BBA/MBO recorder still deferred.
- **Evidence:** `docs/evidence/P0-07C3D_GCQ6_Rithmic_TradeRecorder_LiveVerification.md`
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

## D-P0-08A-001 � Runtime Data Gate + Auction GPS Card foundation

- **Decision:** P0-08A adds ContractSnapshot, RuntimeCapabilitySnapshot, deterministic DataGateEngine, immutable GcaeRuntimeSnapshot publication, AuctionGpsCardViewModel, and ATAS OnRender overlay. Profile remains NotReady ? DataState Degraded with primary reason PROFILE_NOT_READY is correct until a Profile slice exists. Roll stays Unknown without roll evidence (no invented next-contract volume / calendars). MBO displays BLOCKED and stays disabled in the primary ATAS process. Trade Recorder remains locked; UI never reaches mutable recorder/probe objects. EnableCustomDrawing=true is authorized only for the GPS card overlay (no DataSeries mutation).
- **Date:** 2026-07-22

## D-P1A-001 � Primary Intraday Classic TPO / Volume Profile vertical slice

- **Decision:** Phase 1A implements Classic 30-minute TPO anchored at 08:20 America/New_York (IANA with Windows Eastern fallback), exact ATAS per-price executed volume via IndicatorCandle.GetAllPriceLevels()/PriceVolumeInfo when available, deterministic POC/VA (0.70 conventional configurable default, not GC edge), current+previous auction only, GPS card profile rows, and minimal overlay. No volume smearing from bar totals. Profile Ready removes PROFILE_NOT_READY but BidAsk/Roll Unknown may keep global DATA Degraded. Composite, Structural References, Episode, FAR/AAC, Thesis, Adaptive TPO, DOM/MBO remain deferred.
- **Observed ATAS API:** GetCandle(int), IndicatorCandle.GetAllPriceLevels(), PriceVolumeInfo.{Price,Volume,Bid,Ask,Ticks}, IChart.PriceChartContainer.GetYByPrice/GetXByBar.
- **Date:** 2026-07-22

## D-P1A-002 ? ATAS candle timestamp is UTC (LiveObserved)

- **Decision:** IndicatorCandle.Time/LastTime contained wall-clock is UTC even when DateTime.Kind is Unspecified (LiveObserved 2026-07-23 GCQ6/Rithmic; period index 40 vs 32 = +4h NY mis-convert). Normalization policy `ATAS_CANDLE_TIME_UTC_V1`: normalize once to UTC DateTimeOffset, then PrimaryAuctionClock converts to America/New_York. Unknown semantics do not convert. Ledger rebuilds on policy mismatch. POC/VA/TPO/VP algorithms unchanged.
- **Date:** 2026-07-23

## D-P1A-003 ? Phase 1A PASS WITH DOCUMENTED ATAS METHODOLOGY DIFFERENCE

- **Decision:** Phase 1A Primary Intraday Classic TPO + Volume Profile is **PASS WITH DOCUMENTED ATAS METHODOLOGY DIFFERENCE**. Live-verified: UTC candle Time (bar start), 08:20 America/New_York clock, period index, current/previous rollover, Classic 30m TPO, exact volume-by-price VP, ledger accounting (no rejected bars in forensic capture), replace-by-bar-index, independent ClassicTpoOracle equals engine, developing/completed-only agree at selected POC, POC tie policy as specified. ATAS displayed TPO POC (e.g. 4130.2) did not match GCAE selected POC (e.g. 4124.8); forensic showed 4130.2 at count 14 / rank 57 / below max by 3 ? not in GCAE max-count tie set. Timestamp, developing policy, and tie policy are not the cause. No concrete GCAE distribution defect demonstrated. ATAS proprietary methodology / exact benchmark settings remain unconfirmed. **ATAS is not an unquestionable oracle.** No TPO/POC/VA algorithm was changed to force parity. Parity/forensic diagnostics remain default-off and diagnostic-only. Tag `gcae-p1a-primary-tpo-volume-profile-pass` @ `771095eaadba55729dcb569a0c96b5da1ab12ddc`. Locked baseline for Phase 1B.
- **Evidence:** `docs/evidence/Phase1A_Primary_TPO_Volume_Profile_LiveCloseout.md`
- **Date:** 2026-07-23

## D-P1B-001 — Composite Profile Foundation (OperatorAnchored; no hard N-day merge)

- **Decision:** Phase 1B implements Composite Profile Foundation + merge-evidence measurement only. Confirmed composite is **OperatorAnchored** (explicit AuctionId required; `COMPOSITE: AWAITING ANCHOR` if missing). No fixed N-day / rolling-window Production merge. Completed-auction ledger keys by AuctionId, replace-by-higher ContributionVersion (Ordinal), fail-closed on contract-epoch / tick-size / timestamp-policy mismatch. Aggregation sums Phase 1A TPO counts and exact volumes by tick index; reuses Phase 1A POC tie + Value Area. Developing preview is separate and must not mutate confirmed CompositeId/membership. Merge-evidence metrics are computed; ShadowEvidence never creates/closes/alters confirmed composite. Unset shadow thresholds → **NotCalibrated** (no invented GC defaults). Composite Ready does not force global Data Ready. Structural References / Episode / Thesis / FAR/AAC / automatic StableBalance classification remain deferred. Master specification untouched. Trade Recorder unchanged. MBO remains blocked.
- **Date:** 2026-07-23

## D-P1B-002 — Phase 1B FINAL PASS + lock (live acceptance complete)

- **Decision:** Phase 1B Composite Profile Foundation is **FINAL PASS** and locked. Operator live acceptance on GCQ6 / Rithmic Live verified: blank-anchor AWAITING; two-way anchor propagation on the same instance; Confirmed READY with three historical completed auctions (`PI-2026-07-20`…`22`); developing auction excluded from Confirmed; Preview ON/OFF isolation; remove/add lifecycle (one card / one overlay); MBO BLOCKED; global DATA DEGRADED (Bid/Ask Unknown/Partial) remains independent of Composite Ready. Explicit operator anchor retained — no silent selection and no fixed N-day merge. Runtime **operator-configuration fingerprint** is the accepted publish-path wiring mechanism (rebuild once on settings change; ordinary trade publishes reuse). ATAS built-in multi-day profile remains optional benchmark, not oracle. Tests **329** passed; DLL SHA-256 `CA2157509B140D0752FB4FCEF70E1FCD863553053D6E133566B848BBDDB02B87` (source = deployed). Tag `gcae-p1b-composite-profile-foundation-pass`. Phase 1C requires a separate plan and explicit authorization — **NOT STARTED**.
- **Date:** 2026-07-24

## D-P1C-001 — Structural Reference Foundation (exact-tick profile-derived; no score/Episode)

- **Decision:** Phase 1C implements Structural Reference Foundation only (`REFERENCE_POLICY_V1`). References are objects derived from locked Phase 1A Primary and Phase 1B Confirmed Composite snapshots. Exact tick zones (`ZoneLow == ZoneHigh` for single-price levels); no ATR/percent/fuzzy tolerance. Deterministic `ReferenceId` excludes zone so Developing levels may migrate without identity churn; Confirmed payloads are immutable. Confirmed vs Developing sets remain separate; Developing Composite Preview does not contribute references. Exact-tick confluence only — no hierarchy score / probability. Runtime lifecycle emits only Fresh/Active/Expired/Retired; Approaching/Interacting/OutsideAttempt/AcceptedThrough/Reaccepted/Exhausted remain reserved and are not emitted. TestCount stays 0 / NotEvaluated. Module Ready does not clear global DATA DEGRADED. Runtime snapshot schema bumped to **0.4.0** for Structural Reference fields; assembly **0.0.6** and `COMPOSITE_POLICY_V1` unchanged. Episode / Acceptance / Orderflow / FAR/AAC / Thesis / Entry / Risk remain deferred (Phase 1D+). Master specification untouched. Live acceptance required before commit/tag.
- **Date:** 2026-07-24

## D-P1C-002 — Phase 1C FINAL PASS WITH DOCUMENTED LIVE COVERAGE LIMITATION + lock

- **Decision:** Phase 1C Structural Reference Foundation is **FINAL PASS WITH DOCUMENTED LIVE COVERAGE LIMITATION** and locked. Structural References accepted from profile-derived sources only (previous completed Primary, current Developing Primary, Confirmed Composite). Exact-tick zones and exact-tick confluence only — no tolerance band and no hierarchy score/probability. Confirmed and Developing remain distinct. Registry owns monotonic `StateVersion` (drafts use `RegistryAssignedStateVersion = 0`; true external stale revisions still fail closed). Renderer/`REFERENCE_OVERLAY_POLICY_V1` may suppress labels for readability but never mutates engine/registry data; while REF overlay is ON, Primary/Composite visual levels are suppressed though Primary/Composite engines remain the source of truth. No Episode/Acceptance/Orderflow/FAR/AAC/Thesis semantics. Live gates verified on GCQ6 / Rithmic Live: engine READY (16/8/19); Developing movement remained READY without STALE_REVISION; restrained overlay; overlay OFF restores Primary/Composite visuals and ON restores one REF set. Documented manual coverage limitation accepted (complete module disable, remove/add after final fix, live MIXED confluence, per-reference StateVersion visuals) — covered by automated tests; does not alter Reference semantics and does not block Phase 1D planning. Tests **372**; DLL SHA-256 `9CAE06B9091D23854DA60A425E45884D2CC0D5C50E501A4E9EB1B12A58FAF3CA`. Tag `gcae-p1c-structural-reference-foundation-pass`. Phase 1D requires its own implementation boundary and authorization — **NOT STARTED**.
- **Date:** 2026-07-24

## D-P1D-001 — Multi-Horizon Directional Context Foundation (categorical; no signal)

- **Decision:** Phase 1D implements multi-horizon descriptive Directional Context (`DIRECTIONAL_CONTEXT_POLICY_V1`) consuming Primary / Confirmed Composite / Structural Reference snapshots and completed TPO period feed only. Structural (completed auctions) and Tactical (Developing current Primary) states remain separate. Execution context is Unavailable (requires Episode + Orderflow — not authorized). State is produced only from deterministic categorical pairwise evidence (exact-tick; no ATR/tolerance/score/probability). OTF uses completed TPO periods only; ConfirmedUp/ConfirmedDown reserved until a calibrated confirmation policy is authorized (`OTF_CONFIRMATION_NOT_CALIBRATED`). Direction is not a hard veto, Long/Short signal, thesis, or trade recommendation. Runtime schema bumped to **0.5.0**; assembly **0.0.6** and Reference/Composite policies unchanged. Profile/Composite/Reference engines must not consume Directional Context. Live acceptance required before commit/tag.
- **Date:** 2026-07-24

## D-P1D-002 — Phase 1D FINAL PASS WITH DOCUMENTED LIVE DIAGNOSTIC COVERAGE LIMITATION + lock

- **Decision:** Phase 1D Multi-Horizon Directional Context Foundation is **FINAL PASS WITH DOCUMENTED LIVE DIAGNOSTIC COVERAGE LIMITATION** and locked. Multi-horizon Directional Context accepted (`DIRECTIONAL_CONTEXT_POLICY_V1`). Structural and Tactical states remain separate; Tactical maturity is Developing; Execution Context remains unavailable. Deterministic categorical evidence only — direction is not a hard veto or trading signal. OTF uses completed TPO periods only; ConfirmedUp/ConfirmedDown remain reserved until calibration. Unknown, Transition, and Conflicted are valid Production outputs. No fixed N-day structural aggregation. Live core gate on GCQ6 / Rithmic Live verified READY publication with STRUCTURAL CONFLICTED, TACTICAL DOWNDISCOVERY (DEVELOPING), OTF DEVELOPINGDOWN, PRICE LOCATION InsideValue; Primary/Composite/References READY; DATA DEGRADED independent; MBO BLOCKED; no Long/Short/Buy/Sell/Thesis/Entry/Target/Probability/Acceptance/Rejection/Episode wording. Expanded diagnostic fields (auction IDs, transition count, component migrations, OTF streaks/counts, StateVersions, fingerprint) have automated coverage but were not manually observed live — documented limitation accepted; no further operator toggle testing required. Tests **399**; DLL SHA-256 `9976E848578B9057503EC0D8A866C1593CED189EDC5EC04563940F605F0C6AFB`. Tag `gcae-p1d-multi-horizon-directional-context-pass`. Phase 1E requires a separate implementation boundary and authorization — **NOT STARTED**.
- **Date:** 2026-07-24
