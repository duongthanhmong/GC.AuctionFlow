# Implementation Status

> **Spec backbone:** `docs/spec/GC_AuctionFlow_Engine_v1.2_Post_Cross_Review_Final_Spec_VI.md` (architecture/roadmap)
> **+ `docs/spec/GC_AuctionFlow_Engine_v1.3_Knowledge_Grounded_Spec_VI.md`** (semantics/discriminators/calibration contracts; source: KIM ĐẤU KINH; GEX out of scope). Precedence: v1.3 §0.2.

| Field | Value |
|-------|--------|
| Current phase | **Phase 2G CODE/TEST PASS — LIVE ACCEPTANCE PENDING** |
| Probe version | **0.0.6** (unchanged) |
| RawEventRecorderSchemaVersion | **1.2.0** |
| Runtime snapshot schema | **0.18.0** (Phase 2G: Old Value Reclaim) |
| Profile snapshot schema | **1.0.2** (Completed TPO period feed) |
| Composite policy | **COMPOSITE_POLICY_V1** (unchanged) |
| Reference policy | **REFERENCE_POLICY_V1** (unchanged) |
| Overlay policy | **REFERENCE_OVERLAY_POLICY_V1** (unchanged) |
| Directional policy | **DIRECTIONAL_CONTEXT_POLICY_V1** (unchanged) |
| Episode policy | **AUCTION_EPISODE_POLICY_V1** (unchanged) |
| Evidence policy | **ACCEPTANCE_REENTRY_EVIDENCE_POLICY_V1** (unchanged) |
| Orderflow policy | **EXECUTED_ORDERFLOW_POLICY_V1** (unchanged) |
| Cluster Raw policy | **CLUSTER_RAW_FEATURE_POLICY_V1** (unchanged) |
| Auction Efficiency policy | **AUCTION_EFFICIENCY_EVIDENCE_POLICY_V1** |
| Resolution policy | **ACCEPTANCE_REENTRY_RESOLUTION_POLICY_V1** (Phase 2D) |
| Effort/Result policy | **EFFORT_RESULT_CLASSIFIER_POLICY_V1** (Phase 2E) |
| Trade Facilitation policy | **TRADE_FACILITATION_POLICY_V1** (Phase 2F) |
| FAR thesis policy | **FAR_THESIS_POLICY_V1** (Phase 3A) |
| AAC thesis policy | **AAC_THESIS_POLICY_V1** (Phase 3A) |
| Signal Maturity policy | **SIGNAL_MATURITY_POLICY_V1** (Phase 3B) |
| Thesis Contract policy | **THESIS_CONTRACT_POLICY_V1** (Phase 3C) |
| Phase 2G | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Old Value Reclaim Test (extends `ACCEPTANCE_REENTRY_RESOLUTION_POLICY_V1`) |
| Test count | **947** passed / 0 failed / 0 skipped |
| GPS diagnostic rows | **14** |
| Anti-pattern guards | **22 tests** covering AP-001..AP-028 (v1.3 §12) |
| P0-07C3D | **PASS + LOCKED** |
| P0-08A | **PASS + LOCKED** |
| Phase 1A | **LOCKED** — `gcae-p1a-primary-tpo-volume-profile-pass` |
| Phase 1B | **LOCKED FINAL PASS** — `gcae-p1b-composite-profile-foundation-pass` @ `787d0ba` |
| Phase 1C | **LOCKED FINAL PASS WITH DOCUMENTED LIVE COVERAGE LIMITATION** — `gcae-p1c-structural-reference-foundation-pass` @ `cdb2974` |
| Phase 1D | **LOCKED FINAL PASS WITH DOCUMENTED LIVE DIAGNOSTIC COVERAGE LIMITATION** — `gcae-p1d-multi-horizon-directional-context-pass` @ `dcfea72` |
| Phase 1E | **LOCKED FINAL PASS WITH DOCUMENTED LIVE STATE-MACHINE COVERAGE LIMITATION** — `gcae-p1e-auction-episode-observation-pass` @ `9772e48` |
| Phase 1F | **LOCKED FINAL PASS WITH DOCUMENTED LIVE EVIDENCE-LIFECYCLE COVERAGE LIMITATION** — `gcae-p1f-acceptance-reentry-evidence-measurement-pass` @ `bd892ea` |
| Phase 2A | **LOCKED FINAL PASS WITH DOCUMENTED LIVE RAW-FEATURE COVERAGE LIMITATION** — `gcae-p2a-executed-orderflow-raw-feature-foundation-pass` @ `1603dfa` |
| Phase 2B | **LOCKED FINAL PASS WITH DOCUMENTED LIVE CLASSIFIED-CLUSTER COVERAGE LIMITATION** — `gcae-p2b-cluster-raw-feature-measurement-pass` |
| Phase 2C | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Auction Efficiency Raw Evidence (`AUCTION_EFFICIENCY_EVIDENCE_POLICY_V1`) |
| Phase 2D | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Acceptance/Re-entry Resolution (`ACCEPTANCE_REENTRY_RESOLUTION_POLICY_V1`) |
| Phase 2E | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Effort vs Result Classifier (`EFFORT_RESULT_CLASSIFIER_POLICY_V1`) |
| Phase 1G | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Participation Regime: Settlement Proximity Tags + Thin Participation Classifier |
| Phase 2F | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Trade Facilitation Index (`TRADE_FACILITATION_POLICY_V1`) |
| Phase 3A | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — FAR + AAC Thesis State Machine Foundation (`FAR_THESIS_POLICY_V1`, `AAC_THESIS_POLICY_V1`) |
| Phase 3B | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Signal Maturity (`SIGNAL_MATURITY_POLICY_V1`) |
| Phase 3C | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Thesis Contract + 5-dimension Invalidation (`THESIS_CONTRACT_POLICY_V1`) |
| P0-07C4 | **NOT STARTED** |

## Phase 1D final closeout (2026-07-24)

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 399 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live core gate | **PASS** — GCQ6 / Rithmic Live |
| Final verdict | **FINAL PASS WITH DOCUMENTED LIVE DIAGNOSTIC COVERAGE LIMITATION** |
| Structural state live | **PASS** — CONFLICTED published |
| Tactical Developing live | **PASS** — DOWNDISCOVERY (DEVELOPING) |
| OTF descriptive live | **PASS** — DEVELOPINGDOWN |
| Price location live | **PASS** — InsideValue |
| Data Gate independence | **PASS** — DATA DEGRADED while Directional READY |
| MBO | **BLOCKED** |
| Runtime schema | `0.5.0` |
| Policy | `DIRECTIONAL_CONTEXT_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| OTF confirmation | **NOT CALIBRATED** — ConfirmedUp/ConfirmedDown reserved |
| Final DLL SHA-256 | `9976E848578B9057503EC0D8A866C1593CED189EDC5EC04563940F605F0C6AFB` (source = deployed) |
| Tag | `gcae-p1d-multi-horizon-directional-context-pass` |
| Phase 1E | **authorized separately — see Phase 1E section** |

## Phase 1E final closeout (2026-07-24) — LOCKED

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 439 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live trade admission | **PASS** — left AWAITING TRADES after wiring fix (D-P1E-002) |
| Live natural episodes | **PASS** — ACTIVE EPISODES: 4; LATEST DEVELOPING |
| Centerline live | **PASS** — PreviousPrimaryVpoc @ 4051.2; CENTERLINE; AttemptCount 0; max excursion 60 ticks; Direction DOWN |
| Final verdict | **FINAL PASS WITH DOCUMENTED LIVE STATE-MACHINE COVERAGE LIMITATION** |
| History mode | **LIVE_ONLY** — no candle reconstruction of pre-start activity |
| Runtime schema | `0.6.0` |
| Policy | `AUCTION_EPISODE_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Final DLL SHA-256 | `924DB65C4D719D831926C81392AF600A332CD6BFF81401C5B6FC9E30CDFACBC2` (source = deployed) |
| Tag | `gcae-p1e-auction-episode-observation-pass` |
| Phase 1F | **authorized separately — see Phase 1F section** |

### Documented live state-machine coverage limitation (accepted)

Focused live gate proved Episode PARTIAL publication, live trade admission, Confirmed-reference eligibility, natural active episodes, and Centerline observation (AttemptCount 0; side excursion tracked). Repeated-attempt / geometric re-entry / auction-expiry / retirement / disable-reenable / stale-order / exact revision / history-overlap / LocalPoc-tie transitions were **not** all manually observed live and remain covered by deterministic automated tests only. This does not alter Episode semantics and does not require further operator-manufactured crossings.

### Live AWAITING TRADES defect (fixed before closeout)

- **Observed:** `TRADES: OBSERVED` while `EPISODES: AWAITING TRADES` with 16 eligible references.
- **Root cause:** `OnNewTrades` gated Episode behind `EnableTradeStreamProbe` (default OFF).
- **Fix (D-P1E-002):** normalize once; Episode admission always; probe gates only for probe enqueue.

### Phase 1E present (locked)

- Event-driven Auction Episode Observation from normalized `NewTradeObservation` (`OnNewTrade` / `OnNewTrades`)
- Trade admission independent of Trade Stream Probe enablement
- Confirmed Previous Primary + Confirmed Composite references only (Developing excluded)
- One active episode per `(PrimaryAuctionId, ReferenceId)`; AttemptCount on boundary outside transitions
- Centerline: CrossCount + side excursions; never OutsideAttempt/ReentryDeveloping; AttemptCount stays 0
- Boundary geometric ReentryDeveloping only (no Acceptance)
- Per-auction dedup ledger; Primary Auction change → Expired + ledger clear
- GPS/card rows; diagnostics OFF by default; module default OFF
- No Episode overlay, alerts, Sweep/FAR/AAC/Long-Short/thesis

### Explicitly deferred (Phase 1F+)

- Acceptance / stable re-entry Resolution
- Approach distance / intra-auction calibrated reset
- FAR / AAC / Orderflow interpretation / Thesis / Entry / Risk
- Episode chart overlay / ATAS alerts
- Exact historical trade reconstruction (unavailable on chart load)

## Phase 1F final closeout (2026-07-24) — LOCKED

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 458 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Focused live gate | **PASS** — GCQ6 / Rithmic Live |
| UpperBoundary evidence live | **PASS** — PreviousPrimaryTpoVah @ 4063.9; UPPER BOUNDARY; REENTRYDEVELOPING AttemptCount 1; max excursion 29 ticks |
| Finite ratios live | **PASS** — time 0.4242 / volume 0.4479 / trade 0.4404 (all in [0,1]) |
| Geometric re-entry live | **PASS** — REENTRY OBS GEOMETRICREENTRY; ACCEPTANCE OBS UNRESOLVED |
| Local POC displacement live | **PASS** — −8 ticks |
| Observer-only | **PASS** — Episode remained REENTRYDEVELOPING; no Stable Reacceptance; no Resolution mutation |
| Final verdict | **FINAL PASS WITH DOCUMENTED LIVE EVIDENCE-LIFECYCLE COVERAGE LIMITATION** |
| History mode | **LIVE_ONLY** (inherited from Episode) — no candle reconstruction |
| Runtime schema | `0.7.0` |
| Policy | `ACCEPTANCE_REENTRY_EVIDENCE_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Final DLL SHA-256 | `1FDEBBB3E4E94497157FF6FA7D621760028AA516497BAB8800D88CF5C9E07249` (source = deployed) |
| Tag | `gcae-p1f-acceptance-reentry-evidence-measurement-pass` |
| Acceptance/Re-entry Resolution | **NOT STARTED** |
| FAR/AAC | **NOT STARTED** |
| Phase 2 Executed Orderflow | **authorized separately — see Phase 2A section** |

## Phase 2A final closeout (2026-07-24) — LOCKED

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 469 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Focused live gate | **PASS** — GCQ6 / Rithmic Live |
| Ordinary trade admission | **PASS** — Trade Stream Probe OFF; Recorder OFF permitted |
| Raw volume / trade count | **PASS** — EXECUTED VOLUME 113; TRADES 103 |
| Unknown-aggressor accounting | **PASS** — Unknown 113; Ask/Bid unavailable; no fabricated Ask/Bid |
| Classified Delta/CVD safety | **PASS** — CLASSIFIED DELTA 0; CLASSIFIED CVD 0 (classified subset empty) |
| Aggressor coverage | **PASS** — 0 finite and in [0,1] |
| Coverage mode live | **PASS** — LIVEONLYMIDAUCTION |
| Episode Orderflow | **PASS** — EPISODE ORDERFLOW PARTIAL observed |
| Data Gate independence | **PASS** — DATA DEGRADED remains independent |
| MBO | **BLOCKED** |
| Final verdict | **FINAL PASS WITH DOCUMENTED LIVE RAW-FEATURE COVERAGE LIMITATION** |
| History mode | **LIVE_ONLY** — no candle / ATAS visual reconstruction |
| Runtime schema | `0.8.0` |
| Policy | `EXECUTED_ORDERFLOW_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Final DLL SHA-256 | `F92538852AD2478002F6FCB89B052FC9ACBD3773746F733C31548BF77B5356D2` (source = deployed) |
| Tag | `gcae-p2a-executed-orderflow-raw-feature-foundation-pass` |
| Phase 2B Trade Facilitation | **NOT STARTED** |
| Effort vs Result | **NOT STARTED** |
| Acceptance/Re-entry Resolution | **NOT STARTED** |
| FAR/AAC | **NOT STARTED** |

## Phase 2C code/test (2026-07-25) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 497 passed ×2 / 0 failed / 0 skipped; Probe/Recorder 183 green; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Commit/tag | **HOLD** until live acceptance |
| Runtime schema | `0.10.0` |
| Policy | `AUCTION_EFFICIENCY_EVIDENCE_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Source/deployed DLL SHA-256 | `A22FFA7250AC89A26CEC4C92AF5FA81897B4B2ACAC7C3AE58BC03733439EFAA2` (exact match) |
| Effort / Result | **raw evidence vectors only** — classification NOT CALIBRATED |
| History | **LIVE_ONLY** |
| Aggressor | may remain Unknown-only (Partial) |
| Effort vs Result classifier | **NOT STARTED** |
| Trade Facilitation | **NOT STARTED** |
| Absorption / Exhaustion | **NOT STARTED** |
| Resolution / FAR / AAC | **NOT STARTED** |

### Phase 2C present (code/test)

- Immutable Effort + Result vectors from Phase 2A/2B/1E/1F/Profile
- Descriptive raw progress-per-unit relationships (null-safe; no EfficiencyScore)
- Current-auction / active / closed Episode scopes; fingerprint-gated rebuild
- Module default OFF; GPS rows; diagnostics OFF by default; no overlay/alerts
- Does not mutate Orderflow/Cluster/Profile/Composite/Reference/Directional/Episode/Evidence

### Explicitly deferred after Phase 2C

- EffortResultBalanced / AggressionEffective/Ineffective
- PotentialPassiveAbsorption / PotentialExhaustion
- TradeFacilitationHealthy / TradeFacilitationFailing
- Acceptance/Re-entry Resolution / FAR / AAC / Thesis / Entry / Risk

## Phase 2D code/test (2026-07-26) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 544 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Commit/tag | **HOLD** until live acceptance |
| Runtime schema | `0.11.0` |
| Policy | `ACCEPTANCE_REENTRY_RESOLUTION_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Source/deployed DLL SHA-256 | `981F5177721926D83BFFB228D542A0494E7536864D0C69670D6D07DED0DFAE48` (exact match) |
| AcceptanceResolution | Early/Developing pass-through; Unresolved/Established/Failed → **NOT CALIBRATED** |
| ReentryResolution | GeometricReentry/Developing pass-through; Unresolved/Stable/Failed → **NOT CALIBRATED** |
| Overall conclusion | Always **NOT CALIBRATED** — FAR/AAC calibrated thresholds NOT AUTHORIZED |
| History | **LIVE_ONLY** (inherited) |
| New Phase 2D tests | 47 tests (A01–K04); total 544 |

### Phase 2D present (code/test)

- `AuctionResolutionHost` — fingerprint-gated rebuild from `AcceptanceReentryEvidenceSetSnapshot`
- `AuctionResolutionSnapshot` / `AuctionResolutionSetSnapshot` — immutable versioned snapshots
- `ResolutionIdentity.BuildFromEvidenceId()` — `ARES|{sanitized}|{policyVersion}` format
- `ResolutionInputFingerprint` — IEquatable struct gating rebuild on evidence revision change
- `AuctionResolutionPolicyConfig` — `ACCEPTANCE_REENTRY_RESOLUTION_POLICY_V1`; 15 limitation constants
- Resolution enums: `ResolutionModuleState`, `AcceptanceResolutionState`, `ReentryResolutionState`, `AuctionResolutionConclusion`, `ResolutionDataQuality`
- GPS card rows via `AuctionGpsCardMapper.BuildAuctionResolutionLines()`; showDiagnostics-gated ID/version rows
- RuntimeSnapshot schema bumped `0.10.0` → `0.11.0`; `AuctionResolution` property on `GcaeRuntimeSnapshot`
- Indicator: `EnableAcceptanceReentryResolution` / `ShowAuctionResolutionDiagnostics` settings; module default OFF
- GPS diagnostics list: `RESOLUTION:` status row added (9 rows total; was 8)

### Explicitly deferred after Phase 2D

- Established acceptance / Failed acceptance calibration (gated: NOT CALIBRATED)
- Stable re-acceptance / Re-entry failed calibration (gated: NOT CALIBRATED)
- FAR / AAC overall conclusion (NOT CALIBRATED)
- Thesis / Entry / Risk phases
- Overlay alerts

## Phase 2E code/test (2026-07-26) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 592 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Commit/tag | **HOLD** until live acceptance |
| Runtime schema | `0.12.0` |
| Policy | `EFFORT_RESULT_CLASSIFIER_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Source/deployed DLL SHA-256 | `848E0D9417E65051ED70889E9E36CFB1F6326FAE830CF5357CCB6F2708991BB4` (exact match) |
| All classification states | **NOT CALIBRATED** — EffortResultBalanced/AggressionEffective/Ineffective/Absorption/Exhaustion/TradeFacilitationHealthy/Failing reserved |
| FAR/AAC/Thesis/Entry | **NOT AUTHORIZED** in Phase 2E |
| History | **LIVE_ONLY** |
| GPS rows | 10 (was 9); "EFFORT RESULT:" row added |
| New Phase 2E tests | 48 tests (A01–K05); total 592 |

### Phase 2E present (code/test)

- `EffortResultClassifierHost` — fingerprint-gated rebuild from `AuctionEfficiencyEvidenceSetSnapshot`
- `EffortResultClassificationSnapshot` / `EffortResultClassificationSetSnapshot` — immutable versioned snapshots
- `EffortResultIdentity.BuildFromEfficiencyId()` — `ERCL|{sanitized}|{policyVersion}` format
- `EffortResultInputFingerprint` — IEquatable struct gating rebuild on efficiency InputFingerprint change
- `EffortResultClassifierPolicyConfig` — `EFFORT_RESULT_CLASSIFIER_POLICY_V1`; 12 limitation constants
- Effort/Result enums: `EffortResultModuleState`, `EffortResultClassificationState`, `EffortResultDataQuality`
- Supports: CurrentAuction scope + ActiveEpisode scopes + RecentlyClosed (cap 64)
- GPS card rows via `AuctionGpsCardMapper.BuildEffortResultLines()`; showDiagnostics-gated ID/version rows
- RuntimeSnapshot schema bumped `0.11.0` → `0.12.0`; `EffortResult` property on `GcaeRuntimeSnapshot`
- Indicator: `EnableEffortResultClassifier` / `ShowEffortResultDiagnostics` settings; module default OFF
- GPS diagnostics list: `EFFORT RESULT:` status row added (10 rows total; was 9)

### Explicitly deferred after Phase 2E

- EffortResultBalanced / AggressionEffective/Ineffective calibration (gated: NOT CALIBRATED)
- PotentialPassiveAbsorption / PotentialExhaustion calibration (gated: NOT CALIBRATED)
- TradeFacilitationHealthy / TradeFacilitationFailing calibration (gated: NOT CALIBRATED)
- FAR / AAC overall conclusion (NOT AUTHORIZED in Phase 2E)
- Thesis / Entry / Risk phases
- Overlay alerts

## Phase 2F code/test (2026-07-27) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 758 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| SHA-256 source==deployed | `619CB118B2CB44A46E695F27D4FB2EBFDD42FC5A8CBC6D6FF6E8AC0932312FE0` |
| Schema version | `0.15.0` |
| GPS rows | 12 (TRADE FACILITATION added as 12th row) |

### Phase 2F present (code/test)

- `TradeFacilitationPolicyConfig` (`TRADE_FACILITATION_POLICY_V1`): `LimitationNotCalibrated`, `LimitationHealthyNotCalibrated`, `LimitationFailingNotCalibrated`, `LimitationLiveOnly`, `LimitationNoFarAac`
- `TradeFacilitationSnapshot` / `TradeFacilitationSetSnapshot` v1.0.0 — raw index components stored for calibration
- `TradeFacilitationHost`: fingerprint-gated Rebuild from `AuctionEfficiencyEvidenceSetSnapshot?`; direction-consistent effort (AskVol if Up, BidVol if Down, null if Unknown); `FavorableProgressPerDirectionUnit` raw research value
- Classification always `NotCalibrated` — calibration NOT authorized in Phase 2F
- GPS card: 12th diagnostic row `TRADE FACILITATION:` added
- Runtime schema: `0.14.0` → `0.15.0`

### Phase 2F NOT present (code/test)

- Calibrated Healthy/Failing thresholds (spec §23.4 calibration gate: NOT_CALIBRATED enforced)
- FAR/AAC thesis signals in facilitation (no `LimitationNoFarAac` bypass)
- Historical reconstruction (LIVE_ONLY enforced)

## Phase 2G code/test (2026-07-27) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 947 passed x2 / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Runtime schema | `0.17.0` -> `0.18.0` |
| Policy | `ACCEPTANCE_REENTRY_RESOLUTION_POLICY_V1` (extended; version unchanged) |
| Source/deployed DLL SHA-256 | `0B1DCD8D6D5BCCB47FC12541B874051AA1A3A4D826AF9E9896F95094A8C3ABB9` (exact match) |
| Reclaim outcome | **NOT CALIBRATED** — AttemptedAndHeld / AttemptedAndFailed reserved |
| GPS rows | 14 (unchanged — reclaim rows nest inside the existing RESOLUTION block) |
| New Phase 2G tests | 27 tests (A01-G02); total 947 |
| Spec source | v1.3 §6.2 `G-ACC-005`; KDK Ch 18 |

### Why this phase exists

`OldValueReclaim` is the FAR-vs-AAC decision axis (KDK Ch 18): a reclaim that is
attempted **and held** supports FAR; one that **fails** supports AAC; **no attempt**
leaves the auction Unresolved. Without this field neither thesis family can ever leave
`NotCalibrated`, even after the Historical Scanner runs.

### Audit finding that motivated the shape

- `AcceptanceEvidenceVector.OldValueReclaimFailure` is a `bool?` that **no producer ever
  set** — a dead field. It also cannot distinguish "not attempted" from "attempted,
  outcome unknown", which is exactly why `G-ACC-005` requires three states.
- `AuctionResolutionHost` **never read any evidence vector** before this phase; it only
  mapped observation-state enums. The reclaim axis was absent from the pipeline entirely.

### Phase 2G present (code/test)

- `OldValueReclaimState`: `Unknown=0`, `NotAttempted=1`, `AttemptedOutcomeNotCalibrated=2`;
  `AttemptedAndHeld=100` / `AttemptedAndFailed=101` reserved
- `OldValueReclaimObservation` — raw research measurements: attempt count, dwell inside,
  max/current depth returned, local value rebuild, inside volume after re-entry, and the
  elapsed-since-inside **time bound** required by `G-ACC-005`
- `AuctionResolutionHost.DeriveOldValueReclaim` — first consumer of the Phase 1F evidence
  vectors; derives the three observable facts only
- Measurements are `null` for `NotAttempted` / `Unknown` — reporting them would imply an
  attempt that never happened (`G-ACC-003`)
- GPS: reclaim rows nested inside the existing RESOLUTION block; diagnostics show raw
  measurements and report `unavailable`, never `0`

### Phase 2G NOT present (code/test)

- Held-vs-failed verdict (calibration gate); the deciding window LENGTH is not calibrated
- Any boolean "reclaimed" shortcut — asserted absent by test G01
- Changes to the Evidence module: **Phase 1F is LOCKED**, so the dead
  `OldValueReclaimFailure` field is left in place and documented as superseded rather
  than removed

## Phase 3C code/test (2026-07-27) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 920 passed x2 / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Commit/tag | **HOLD** closeout tag until live acceptance |
| Runtime schema | `0.16.0` -> `0.17.0` |
| Policy | `THESIS_CONTRACT_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Source/deployed DLL SHA-256 | `53DC99E39CC4203FC2DD205B21D18C8D1E0DA2B5E4E046E32778257F1218EBCB` (exact match) |
| Contract state | **NOT CALIBRATED** — Complete/Executable reserved |
| All 5 invalidation dimensions | **NOT CALIBRATED** — Triggered/Cleared reserved |
| Protective stop | **NOT AUTHORIZED** — v1.2 §32.4 needs volatility/MAE, deferred |
| GPS rows | 14 (was 13); `CONTRACT:` row added |
| New Phase 3C tests | 45 tests (A01-M03); total 920 |
| Spec source | v1.2 §32 + §11.1-11.3 + v1.3 §11 |

### Phase 3C present (code/test)

- `ThesisContractHost` — fingerprint-gated rebuild from Phase 3B `SignalMaturitySetSnapshot`
- `ThesisContractSnapshot` / `ThesisContractSetSnapshot` v1.0.0; `RecentlyClosedCapacity = 64`
- **Five thesis horizons** (v1.2 §11.2): Context / Thesis / Trigger / Management / Target.
  Always all five declared; every source reports `Unavailable` because the multi-horizon
  map is not authorized — roles exist so a contract can never silently omit one.
- **Five invalidation dimensions** (v1.2 §32.2 four + v1.3 §11.3 Evidence).
  Evidence is an independent dimension with its own limitation string: it fires earlier
  than Auction invalidation and does not require acceptance to have formed (`G-INV-001`).
- `MissingEvidenceKind[]` — structured, never free text (`G-THE-001`), never empty
- `ThesisConsistencyGate` — four separate booleans, deliberately not one score (`G-THE-006`)
- `ExpiresAtUtc` always `null` — expiry duration NOT CALIBRATED, never fabricated
- `SourceOfMove` always `Unknown` — needs the horizon map (v1.2 §11.3)
- Indicator: `EnableThesisContract` / `ShowThesisContractDiagnostics`; module default OFF
- GPS diagnostics list: `CONTRACT:` status row added (14 rows total; was 13)

### Phase 3C NOT present (code/test)

- `Complete` / `Executable` contract state (calibration gate)
- Protective hard stop and stop calculation (v1.2 §32.3-32.4) — needs tick volatility,
  MAE distribution, participation regime, spread, basis; asserted absent by test M01
- Target Engine / PLAR (v1.2 §33), RR + CFD mapping (§34), position sizing (§35)
- Entry price, stop price, target price, size — no such field exists (M01)
- Score or probability fields (M02); GEX surface (M03)
- Horizon map resolution and source-of-move attribution
- Historical reconstruction (LIVE_ONLY enforced)

### Legacy gate assertions tightened (not loosened)

Six pre-Phase-3 tests asserted `DoesNotContain("EnableThesis", indicatorSource)`, whose
intent is "there is no generic monolithic thesis toggle; theses are per-family flags".
The new `EnableThesisContract` property tripped them by prefix coincidence only.

The assertions were made **more precise** rather than relaxed: they now match
`"EnableThesis "` with a trailing space, which still catches a real generic
`EnableThesis { get; set; }` or `EnableThesis = false;` but not `EnableThesisContract`.
Files: Phase1A, Phase1C, Phase1D, Phase1E, Phase1F, Phase2A.

## Phase 3B code/test (2026-07-27) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 863 passed x2 / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Commit/tag | **HOLD** closeout tag until live acceptance |
| Runtime schema | `0.15.0` -> `0.16.0` |
| Policy | `SIGNAL_MATURITY_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Source/deployed DLL SHA-256 | `1109AC3C4EE67670673BE164044D0DDF4CD2E214E7686A94E0C14AD14FB57BA7` (exact match) |
| All maturity levels | **NOT CALIBRATED** — Fast/Standard/Confirmed reserved |
| FAST mode | **SHADOW ONLY** — v1.2 §29.5 guardrail enforced in policy |
| GPS rows | 13 (was 12); `MATURITY:` row added |
| New Phase 3B tests | 48 tests (A01-L03) + 22 anti-pattern guards + 1 AAC regression; total 863 |
| Spec source | v1.2 §29 + v1.3 §9 |

### Phase 3B present (code/test)

- `SignalMaturityHost` — fingerprint-gated rebuild from Phase 3A `FarThesisSetSnapshot` + `AacThesisSetSnapshot`
- `SignalMaturitySnapshot` / `SignalMaturitySetSnapshot` v1.0.0 — immutable, `RecentlyClosedCapacity = 64`
- `SignalMaturityLevel` — always `NotCalibrated`; Fast/Standard/Confirmed reserved at 100+
- `AnalysisLifecycleState` — v1.2 §29.1 lifecycle; capped at `Candidate`; Armed/Executable/Managing reserved at 100+
- `ExpectedBehaviorContractKind` — six KDK Ch 63 scenarios (v1.3 §9.3): FarReentry, FarRetest, AacEarly, AacRetest, NewValueContinuation, Rotation
- `ExpectedBehaviorDeadlineUtc` — always `null`; duration NOT CALIBRATED (never fabricated)
- `RetestObservationState` — always `NotCalibrated`; Micro/Structural/SecondAttempt reserved
- `MaturityBlockingReason[]` — never empty; explains why no level can be emitted
- `SignalMaturityPolicyConfig` — 12 limitation constants; `FastShadowOnlyDefault = true`
- Indicator: `EnableSignalMaturity` / `ShowSignalMaturityDiagnostics`; module default OFF
- GPS diagnostics list: `MATURITY:` status row added (13 rows total; was 12)

### Phase 3B NOT present (code/test)

- Fast / Standard / Confirmed level emission (calibration gate enforced)
- Entry Policy, Entry Zone, position sizing (no Entry/Stop/Target/Size fields — asserted by test L01)
- Score or probability fields (asserted by test L03)
- Retest micro-vs-structural discrimination (NOT CALIBRATED)
- Expected-behaviour deadline enforcement / Time Invalidation (Phase 3C)
- Historical reconstruction (LIVE_ONLY enforced)


## v1.3 remediation (2026-07-27) — bundled with Phase 3B

| Item | Result |
|------|--------|
| v1.3 §15.1(3) AAC premature invalidation | **FIXED** |
| v1.3 §15.1(4) Anti-Pattern Guard Registry | **DONE** — 22 tests covering AP-001..AP-028 |
| Total tests | 863 (was 840) |

### AAC re-entry mapping corrected

`AacThesisHost.DeriveAacState` mapped `EpisodeState.ReentryDeveloping` to
`AacState.Invalidated` with `NotCalibrated = false`, asserting the thesis could be
definitively closed on geometry alone.

Per KDK Ch 65 ("một bóng nến quay vào chưa đủ") and v1.3 `G-DISC-002` / `G-AAC-001`,
geometric re-entry is not reacceptance. AAC invalidation requires
`ReentryResolutionState.StableReacceptance`, which is calibration-gated.

Now maps to `AacState.Pullback` with `NotCalibrated = true`. New regression
`J04b_AacHost_never_invalidates_on_geometry_alone` asserts no evidence-observable
episode state can produce `Invalidated` or `ReacceptedOldValue`.

### Anti-Pattern Guard Registry (`tests/.../Unit/Guards/AntiPatternGuardTests.cs`)

22 behaviour/type-surface tests covering all 28 registry entries, plus four
cross-cutting registry guards: no GEX surface, no probability or order-placement
surface, every gated verdict enum offers `NotCalibrated`, and every state-machine
snapshot carries a `NotCalibrated` flag.

Documented exemptions (verified, not silently skipped):

- `CapabilityKind.MboSweeps` — the capability matrix must be able to NAME a feed
  capability in order to report it BLOCKED. Naming a capability is not emitting a
  Tier-4 label. The gate is verified separately: `default(MboSubscriptionState)`
  is `NotAttempted`, so MBO is never assumed live.
- `FarState` / `AacState` / `EpisodeState` / observation-state enums have no
  `NotCalibrated` member because they carry the gate as a `NotCalibrated` bool on
  their snapshot; asserted by `Registry_State_machine_snapshots_carry_a_NotCalibrated_flag`.

Guard matching is PascalCase token-aware: naive substring matching produced false
positives ("PeriodIn**dex**Compressor" for "Dex", "Containin**gEx**act" for "Gex").

## Phase 3A code/test (2026-07-26) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 651 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Commit/tag | **HOLD** until live acceptance |
| Runtime schema | `0.13.0` |
| FAR policy | `FAR_THESIS_POLICY_V1` |
| AAC policy | `AAC_THESIS_POLICY_V1` |
| Assembly | `0.0.6` (unchanged) |
| Source/deployed DLL SHA-256 | `237C699C4FBB8E1E326425B3695173F8A8DC52F3911E4F6C083280CA07CAFF9B` (exact match) |
| All calibrated states | **NOT CALIBRATED** — Armed/Executable/Managing/Completed reserved |
| GPS rows | 11 (was 10); "THESIS: NOT AVAILABLE" replaced by "FAR:" + "AAC:" rows |
| New Phase 3A tests | 59 tests (A01–R02); total 651 |

### Phase 3A present (code/test)

- `FarThesisHost` — FAR (Failed Auction Re-entry) state machine; fingerprint-gated rebuild from evidence
- `AacThesisHost` — AAC (Acceptance-Continuation) state machine; fingerprint-gated rebuild from evidence
- `FarState` enum: 15 states (observable 0–5, calibrated 100–105, terminal 200–202)
- `AacState` enum: 14 states (observable 0–6, calibrated 100–102, terminal 200–203)
- FAR direction: `CanonicalOutsideDirection==Below`→Long; `Above`→Short
- AAC direction: `Above`→Long; `Below`→Short (opposite of FAR)
- `ThesisDirection`: Unknown=0, Long=1, Short=2
- Observable state mappings: `Interacting`→EpisodeActive; reentry obs→ReentryDeveloping; `ReentryDeveloping` episode→NOT CALIBRATED
- AAC specific: `ReentryDeveloping` episode→`Invalidated` (re-entry negates continuation)
- GPS diagnostics: "FAR: {state}" and "AAC: {state}" rows
- Indicator settings: `EnableFarThesis`, `ShowFarThesisDiagnostics`, `EnableAacThesis`, `ShowAacThesisDiagnostics`
- RuntimeSnapshot schema bumped `0.12.0` → `0.13.0`; `FarThesis` + `AacThesis` properties added
- RecentlyClosedCapacity=64; ArmableCount/ExecutableCount always 0 (NOT CALIBRATED)

### Explicitly deferred after Phase 3A

- Armed / Executable / Managing / Completed states (gated: NOT CALIBRATED)
- Thesis Signal Maturity, Entry Policy, Invalidation triggers
- PLAR Targets, CFD Mapping, Risk phases
- Overlay alerts / Telegram integration

## Phase 2B final closeout (2026-07-25) — LOCKED

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 482 passed / 0 failed / 0 skipped (×2 full runs); Probe/Recorder 139 green; 0 errors / 0 warnings |
| Focused live gate | **PASS** — GCQ6 / Rithmic Live (two snapshots ~2s apart) |
| Phase 2A authoritative | **PASS** — no second trade normalization |
| Live update propagation | **PASS** — volume 318→324; trades 258→264; levels 5→6; Unknown 318→324 |
| Unknown-only Partial | **PASS** — Classified levels 0; Ask/Bid ratios unavailable (not zero) |
| Empirical Volume Rank | **PASS** — 4/5 → 6/6; latest tick 4071.0 → 4070.7 |
| ClassificationState | **PASS** — NOT CALIBRATED |
| Coverage live | **PASS** — LIVEONLYFROMAUCTIONSTART |
| History | **PASS** — LIVE_ONLY |
| Data Gate / MBO | **PASS** — DATA DEGRADED independent; MBO BLOCKED |
| Final verdict | **FINAL PASS WITH DOCUMENTED LIVE CLASSIFIED-CLUSTER COVERAGE LIMITATION** |
| Runtime schema | `0.9.0` |
| Policy | `CLUSTER_RAW_FEATURE_POLICY_V1` |
| Rank method | `EMPIRICAL_MIDRANK_V1` |
| Assembly | `0.0.6` (unchanged) |
| Final DLL SHA-256 | `A15CC6A85AA9562E59CA8B66140AAD017E957F96E96C7BBF47E620E6E0E71A39` (source = deployed) |
| Tag | `gcae-p2b-cluster-raw-feature-measurement-pass` |
| Imbalance / Stacked Imbalance | **NOT STARTED** |
| Extreme Delta / Extreme Volume | **NOT STARTED** |
| Big Trade | **NOT STARTED** |
| Effort vs Result classifier | **NOT STARTED** (Phase 2C is raw evidence only) |
| Trade Facilitation | **NOT STARTED** |
| Resolution/FAR/AAC | **NOT STARTED** |

### Documented live classified-cluster coverage limitation (accepted)

Focused live gate proved Cluster Raw PARTIAL publication, Phase 2A→2B revision propagation across two live snapshots, Unknown-only level growth, truthful unavailable Ask/Bid ratios, empirical Volume Rank updates, ClassificationState NOT CALIBRATED, Episode Cluster Raw PARTIAL, DATA DEGRADED independence, and MBO BLOCKED. LEVEL TRADES 90→84 is not a decrement — latest displayed tick changed (4071.0→4070.7). Classified Ask/Bid ratios, diagonal classified paths, classified dominant sides/runs, absolute-Delta rank, percentile ties, multi-visit lifecycle, classified Episode aggregates, Centerline, auction/epoch/disable resets, revision sequences, stale rejection, and deterministic replay remain **automated-only** coverage. This does not alter Cluster Raw semantics and does not require further Ask/Bid event hunting.

### Phase 2B present (locked)

- Cluster Raw host over Phase 2A snapshots; version-gated rebuild; visit tracking via changed tick
- Same-price / diagonal raw ratios; RawDominantSide; consecutive dominance (no stacked label)
- EMPIRICAL_MIDRANK_V1 ranks/percentiles; visits/revisits; Episode/auction cluster summaries
- Module default OFF; GPS rows; diagnostics OFF by default; no overlay/alerts/thresholds
- Does not mutate Orderflow/Profile/Composite/Reference/Directional/Episode/Evidence

### Explicitly deferred after Phase 2B

- Bid/Ask / stacked imbalance classification; Extreme Delta/Volume; Big Trade; tape-speed
- Absorption / exhaustion / Effort vs Result / Trade Facilitation
- Acceptance/Re-entry Resolution / FAR / AAC / Thesis / Entry / Risk

### Documented live raw-feature coverage limitation (accepted)

Focused live gate proved Orderflow PARTIAL publication, ordinary trade admission with Probe OFF, truthful Unknown-aggressor accounting (Ask/Bid unavailable), mathematically safe Classified Delta/CVD/coverage, LIVEONLYMIDAUCTION coverage, Episode Orderflow PARTIAL, DATA DEGRADED independence, and MBO BLOCKED. Ask/Bid classified paths, complete classification READY, mixed-side reconciliation, nonzero Delta/CVD, full per-price ledger, complete Episode aggregates, timing metrics, cumulative revision replacement, auction/epoch/disable–re-enable resets, LiveOnlyFromAuctionStart transition, revision sequences, out-of-order rejection, and deterministic replay remain **automated-only** coverage. This does not alter raw Orderflow semantics and does not require further operator-manufactured aggressor events.

### Phase 2A present (locked)

- One authoritative `TradeStreamAtasMapper.MapNewTrade` → `ExecutedTradeEvent` path
- Cumulative callbacks not authoritative for executed totals (`CUMULATIVE_CALLBACKS_NOT_AUTHORITATIVE_FOR_EXECUTED_TOTALS`)
- Current-auction aggregate + per-price ledger + Episode raw aggregate
- Classified Delta/CVD; Unknown aggressor explicit; timing raw only
- Module default OFF; GPS rows; diagnostics OFF by default; no overlay/alerts
- Does not mutate Profile/Composite/Reference/Directional/Episode/Evidence

### Explicitly deferred (Phase 2B+)

- ~~Cluster Raw Feature Measurement~~ — **Phase 2B LOCKED**
- Imbalance / stacked imbalance / Big Trade classification
- Absorption / exhaustion / Effort vs Result / Trade Facilitation
- Acceptance/Re-entry Resolution / FAR / AAC / Thesis / Entry / Risk

### Documented live evidence-lifecycle coverage limitation (accepted)

Focused live gate proved Evidence PARTIAL publication, natural UpperBoundary evidence set, finite descriptive ratios, geometric re-entry observation, Local POC displacement, observer-only behavior (Episode REENTRYDEVELOPING unchanged), DATA DEGRADED independence, and MBO BLOCKED. Full outside/inside segmentation, LowerBoundary symmetry, zero-denominator ratios, repeated attempts, exact revision sequences, lifecycle freeze/reset, Centerline not-applicable, bid/ask variants, compatibility fail-closed, and deterministic replay remain **automated-only** coverage. This does not alter Evidence/Episode semantics and does not require further operator-manufactured crossings.

### Phase 1F present (locked)

- Read-only `EpisodeMeasurementEvent` feed from Phase 1E admission
- Immutable Acceptance/Re-entry evidence vectors (measurement only)
- Observation states: Acceptance Early/Developing/Unresolved; Reentry GeometricReentry/Developing/Unresolved
- Centerline: `CENTERLINE_ACCEPTANCE_GEOMETRY_NOT_APPLICABLE` — no canonical outside ratios
- EvidenceId `AREV|{EpisodeId}|ACCEPTANCE_REENTRY_EVIDENCE_POLICY_V1` stable per EpisodeId
- LIVE_ONLY history; unavailable fields explicit (no fabricated zeros)
- Module default OFF; GPS rows; diagnostics OFF by default; no overlay/alerts
- Does not mutate Episode State / Resolution / AttemptCount

### Explicitly deferred (resolution / later phases)

- Acceptance Established / Failed
- Stable Reacceptance / Reentry Failed
- FAR / AAC / Thesis / Entry / Risk / Long-Short
- OutsideCloseRatio / TPO outside / Local Value rebuild / OldValueReclaimFailure / RetestHoldQuality
- Opposite-aggression effectiveness
- Invented acceptance/maintenance/stable-reentry thresholds

### Phase 1D present (locked)

- Multi-horizon descriptive Directional Context (Structural / Tactical / OTF / Execution=Unavailable)
- Deterministic pairwise categorical migration + conservative state table
- Completed-auction structural aggregation (no fixed N-day window)
- Developing tactical context vs previous completed Primary
- One-Time Framing from completed TPO periods only (DevelopingUp/Down/Broken/Mixed)
- Narrow completed TPO period production feed from ClassicTpoEngine
- Input fingerprint publish reuse; price-only location refresh when evidence unchanged
- GPS card rows; diagnostics OFF by default; no Long/Short/Buy/Sell/Thesis/score
- Module default OFF; Directional Ready does not clear global DATA DEGRADED

### Explicitly deferred after Phase 1D (partially superseded)

- ~~Auction Episode~~ — **Phase 1E LOCKED**
- Acceptance / Re-entry Resolution — **Phase 1F LOCKED (measurement only; Resolution still NOT STARTED)**
- ~~Orderflow raw features~~ — **Phase 2A LOCKED**
- Orderflow interpretation / FAR/AAC / Thesis / Entry / Risk
- Execution directional horizon
- Calibrated OTF ConfirmedUp/ConfirmedDown threshold
- Weekly/monthly profile horizons
- Thin Participation / Settlement / Day Structure production classifier
- Score / probability / automatic execution

## Phase 1C final closeout (2026-07-24) — LOCKED

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 372 passed at lock |
| Live acceptance | **PASS WITH DOCUMENTED LIVE COVERAGE LIMITATION** |
| Final DLL SHA-256 | `9CAE06B9091D23854DA60A425E45884D2CC0D5C50E501A4E9EB1B12A58FAF3CA` |
| Tag | `gcae-p1c-structural-reference-foundation-pass` |

## Phase 1B / 1A locks (unchanged)

See prior closeout sections; tags and hashes unchanged.
