# Implementation Status

> **Spec backbone:** `docs/spec/GC_AuctionFlow_Engine_v1.2_Post_Cross_Review_Final_Spec_VI.md` (architecture/roadmap)
> **+ `docs/spec/GC_AuctionFlow_Engine_v1.3_Knowledge_Grounded_Spec_VI.md`** (semantics/discriminators/calibration contracts; source: KIM ĐẤU KINH; GEX out of scope). Precedence: v1.3 §0.2.

| Field | Value |
|-------|--------|
| Current phase | **SKELETON COMPLETE — FULL-CHAIN LIVE VALIDATION PASSED; PER-PHASE SEMANTIC ACCEPTANCE STILL PENDING** |
| Probe version | **0.0.6** (unchanged) |
| RawEventRecorderSchemaVersion | **1.2.0** |
| Runtime snapshot schema | **0.25.0** (module fault reporting) |
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
| PLAR policy | **PLAR_POLICY_V1** (Phase 3E) |
| Price Memory policy | **PRICE_MEMORY_POLICY_V1** (Phase 1I) |
| Imbalance policy | **IMBALANCE_POLICY_V1** (Phase 2H) |
| Day Structure policy | **DAY_STRUCTURE_POLICY_V1** (Phase 1H, RESEARCH_ONLY) |
| Entry policy | **ENTRY_POLICY_V1** (Phase 4B, ObserveOnly only) |
| CFD mapping policy | **CFD_MAPPING_POLICY_V1** (Phase 4C, INVALID — no CFD feed) |
| Risk policy | **RISK_POLICY_V1** (Phase 4A, no size emitted) |
| Phase 2G | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Old Value Reclaim Test (extends `ACCEPTANCE_REENTRY_RESOLUTION_POLICY_V1`) |
| Phase 2F-b | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Facilitation Structure + Maintenance components (extends `TRADE_FACILITATION_POLICY_V1`) |
| Phase 3D | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Location Gate (extends `SIGNAL_MATURITY_POLICY_V1`) |
| Phase 3E | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — PLAR / Target Engine (`PLAR_POLICY_V1`) |
| Phase 3E-b | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — PLAR published to runtime + GPS |
| Phase 1I | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Price Memory / Retest Ledger (`PRICE_MEMORY_POLICY_V1`) |
| Phase 2H | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Imbalance context + gate (`IMBALANCE_POLICY_V1`) |
| Phase 1H | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Day Structure Classifier, RESEARCH_ONLY (`DAY_STRUCTURE_POLICY_V1`) |
| Phase 4B | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Entry Policy Engine, ObserveOnly only (`ENTRY_POLICY_V1`) |
| Phase 4C | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — CFD Mapping, INVALID (`CFD_MAPPING_POLICY_V1`) |
| Phase 4A | **CODE/TEST PASS — LIVE ACCEPTANCE PENDING** — Position Sizing + Account Risk (`RISK_POLICY_V1`) |
| Test count | **1375** passed / 0 failed / 0 skipped |
| GPS diagnostic rows | **21** |
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

## First live run (2026-07-27) — GCQ6 / Rithmic

| Gate | Result |
|------|--------|
| Modules reporting real state | **21 / 21** |
| Modules confirmed behaving correctly | **21 / 21** |
| Integration defects found | **4 classes** — all invisible to a green unit suite |
| Final card | build `5FB94B79`, `FAULTS: none`, chain flowing end to end |
| Card at close | build `FF75B587`, schema `0.25.0`, `FAULTS: none` |
| Per-phase live acceptance | **STILL PENDING** — this proved the modules are alive, not that each phase's semantics are correct |

### What the run established

Foundation READY: Profile, Composite (5 auctions), References (16 confirmed / 8 developing
/ 22 confluence), Directional. The chain Episode -> Evidence -> Orderflow -> Cluster ->
Efficiency -> Resolution -> EffortResult -> FAR -> AAC runs PARTIAL on live data, where
PARTIAL means aggressor classification is absent rather than the module being broken.

Four modules report exactly what they were designed to and that is a pass, not a gap:
`DAY STRUCTURE: NOTCALIBRATED`, `ENTRY PLAN: OBSERVEONLY`, `CFD MAP: INVALID`,
`RISK: NOTCALIBRATED`.

### The three defects, and why the suite could not see them

| Defect | Why unit tests missed it |
|---|---|
| Trade Facilitation was reachable only through its `Ensure` guard, which early-returns once a snapshot exists, so it published `AwaitingEfficiency` once and froze | no test simulated more than one publish path |
| Seven modules were populated only in the `OnCalculate` chain, but `PublishRuntimeSnapshot` is reachable from **six** call sites and trade callbacks publish far more often than bars close, so the last publish saw nulls | tests drive hosts directly and never go through the indicator |
| PLAR held a full reference set but no price, because `NearestReferenceView` carries one only when the reference extraction produced it | tests always passed a price in |

All three are integration defects, and every one sat behind a green suite.

### Correction to an earlier claim

The `phase2f-b` fix commit called `Ensure*InitializedForPublish` "a first-publish safety
net" and said the per-bar chain was the real update path. That is backwards. `Ensure` is
how a module survives the five non-bar publish paths; the chain is the optional half.
Facilitation was broken in both directions at once, which is why fixing only the chain
changed nothing on the card.

### Diagnostics added because the failure was unreadable

- **`BUILD: <mvid8>`** on the card. Two consecutive screenshots were byte-identical
  including a row just fixed, and nothing distinguished "stale DLL" from "fix did not
  work". A screenshot now proves which binary produced it.
- **`FAULTS:`** row. Every `Process*` ended in an empty `catch`, so a module killed by an
  exception looked identical to one switched off.
- **`ENABLED BUT NO SNAPSHOT`**. `NOT AVAILABLE` conflated off / idle / silently-not-
  publishing. Separating the third case is what located the root cause.
- **GPS card compact mode**. With every module on, the card ran to several hundred lines
  and the status block sat below the bottom of any screen.


### Chain closure (final state)

```
EPISODE (3) -> EVIDENCE (3) -> EFFICIENCY (4) -> FAR (3) + AAC (3) -> MATURITY (6) -> THESIS CONTRACT
```

`MATURITY: PARTIAL (6)` equals exactly the three FAR plus three AAC theses upstream. The
arithmetic matching is what makes this a confirmation rather than merely a different
result.

### Fourth defect class: publish guards that froze their module

`Ensure*InitializedForPublish` returned early whenever a snapshot already existed, so the
module never rebuilt after its first publish. Four were affected: TradeFacilitation,
SignalMaturity, ThesisContract, AcceptanceReentryResolution.

Trade Facilitation computed a fingerprint and then ignored it in the condition.

Signal Maturity is the instructive one. It froze at `AWAITINGTHESIS (0)` while FAR and AAC
each carried three theses, and because "awaiting thesis" reads as a legitimate waiting
state it survived several rounds of debugging unexamined. The symptom looked normal.

The guards were pointless as well as harmful: every host already gates its own rebuild on
an input fingerprint, so the redundant call costs nothing.

The fourth instance, AcceptanceReentryResolution, was found by the shape test, not by
reading the code — a hand review had already missed it.

### Why the scope counts mattered

`PARTIAL` alone cannot separate a module processing scopes from one running over nothing.
`AUCTION EFFICIENCY: PARTIAL (4)` beside `TRADE FACILITATION: AWAITINGEFFICIENCY` made the
contradiction visible in a single screenshot; without the count the two statements were
both true and jointly uninformative.

### Test-gap note

`PublishPathCoverageTests.A03` was written as protection against the second defect and was
vacuous — it accepted any host as driven because the publish body always contains the
substring `Ensure`. It was rewritten and both guard sets were then verified by mutation:
removing `ProcessTradeFacilitation` from the per-bar chain and `ProcessPlar` from the
publish path each turn the relevant assertions red. A green assertion that cannot fail is
worse than none.

### Open

- One test failed once immediately after a clean rebuild. **Not reproduced** in three
  further full clean rebuilds. Leading hypothesis is a source read racing the build. No
  fix attempted, because the cause is unobserved; the source readers now assert the file
  is non-empty and contains the indicator class, so a recurrence reports the real problem
  instead of failing confusingly downstream.

### Closed: the CRLF integrity finding, properly this time

Commit `5973489` recorded that `core.autocrlf = true` — with no `.gitattributes` — was
rewriting source on disk after the DLL had been built from it, so a source-vs-binary
SHA-256 check could disagree for reasons unrelated to the build. The response then was to
fix the *order of operations* (commit -> clean -> build -> test -> deploy -> hash). That
avoided the symptom; it did not remove the cause, and it left the order load-bearing.

`.gitattributes` now pins `* text=auto eol=lf`, with `.ps1`/`.cmd`/`.bat` kept CRLF because
Windows shells break on LF continuations.

Two things were wrong in the tree and are now fixed:

- 238 of 316 `.cs` files were CRLF on disk while the repository stored LF. The compiler
  reads bytes, so the built DLL depended on which files a checkout had touched.
- The first `--renormalize` was discarded by an operator error (`rm .git/index` to force a
  refresh; the following `git reset` rebuilt the index from HEAD). Redone and verified.

`git diff --ignore-cr-at-eol` across all 258 affected files returns nothing, so the change
was line endings and nothing else. The repository content itself needed no commit — only
`.gitattributes` did.

**Determinism confirmed rather than assumed.** Two consecutive clean rebuilds of the same
tree produce a byte-identical DLL, so a recorded hash is a real check. The build identity
changed across this work (`E7706FAC` -> `B0543F6E`) purely because the source bytes changed;
no code changed, and 1356 tests pass on both.

| | |
|---|---|
| Deployed SHA-256 | `BB9F2214079A44821E661E63AA87C9E14F35C03F7FAE3F145441B91A8997C4AE` |
| Build identity | `B0543F6E` |
| Source == deployed | yes |

### CORRECTION: the feed does carry aggressor side — the engine reads the wrong field

Recorded earlier in this document, and **wrong**: *"trades arrive in volume and none carry
an aggressor side"*. That was measured from `MarketDataArg.IsAsk` / `IsBid`, which Rithmic
never populates. Reading the recording back shows the side is present on every trade, in
`Direction`:

```
directionName across 24,214 trades      isAsk || isBid true
   Sell   12,300                            0
   Buy    11,914
```

No `Between`, no absent values, and a 50.8 / 49.2 split — the shape of a real aggressor
stream, not an artefact.

**Where it is lost.** `TradeStreamAtasMapper` captures `trade.Direction` into
`NewTradeObservation.Direction`, so the value reaches the engine intact. Then
`EpisodeTradeEvent.TryFromNewTrade` computes:

```csharp
var classified = obs.IsAsk || obs.IsBid;   // always false on this feed
```

and passes `obs.IsAsk, obs.IsBid` onward. The correct field is carried the whole way and
discarded at the last step — the same failure shape as the four display-layer defects, one
layer deeper.

**What this invalidates:**

| Reported as | Actually |
|---|---|
| `BID/ASK: UNAVAILABLE (FEED CARRIES NO SIDE)` on the card | wrong — the card states a measurement of the wrong field |
| `AggressorEvidenceMissing` on scanner rows is "this feed" | wrong — it is this bug |
| `CanonicalOutsideDelta` permanently null here | wrong — recoverable |
| Auction Efficiency ask/bid components unavailable | wrong — recoverable |
| Deriving side from the quote is the only path | wrong — no derivation needed, the feed states it |

**Fix scope, not yet done.** `EpisodeTradeEvent` is Phase 1E and LOCKED, and correcting it
changes delta, efficiency, imbalance and episode evidence simultaneously — every module
that consumes aggressor evidence would begin producing different values, and today's live
acceptance would need re-running for those modules. That is a deliberate, tested change
with a re-acceptance, not a patch to slip in.

Found only because the recording could be read back. Nothing in the live card or the test
suite could have surfaced it: every layer agreed, and all of them were reading the same
wrong field.

### The recording was read back for the first time

`SegmentReader` opens a real `.seg` and decodes it. The frame codec already had tests, but
every one of them decoded a buffer it had just encoded in memory — nothing had opened a
session file, so what the recorder wrote to disk was an assumption held up by SHA-256. A
hash proves a file is undamaged; it says nothing about whether the contents mean anything,
and a recorder writing garbage steadily would have passed every check the project had.

First read of the live spool, three most recent sessions:

| Session | Segments | Frames |
|---|---|---|
| `1b544b32` | 13 | 192,175 |
| `bb623d6c` | 4 | 131,346 |
| `896e846d` | 2 | 8,905 |

Every sealed segment ended **exactly on a frame boundary** — zero trailing bytes, zero CRC
failures. Payloads are genuine events carrying session id, segment ordinal, a monotonic
`recorderGlobalLocalSequence`, `writerDequeuedUtc`, stream kind and callback source. The
recorder is doing what it claimed.

**Two findings that only reading could produce:**

1. **`Depth` frames: zero. `BestBidAsk`: 67,867.** `MarketDepthChanged` never yielded a
   recordable frame on this feed — every depth-side event came from `OnBestBidAskChanged`.
   So `DOM: PARTIAL` means *top-of-book quotes only*, not book depth. The distinction
   matters: pulling, stacking and queue position need the book, and none of that is being
   captured because none of it is arriving.
2. **`CallbackInvocationResult`: 62,142 — about a third of all frames.** That is recorder
   bookkeeping rather than market data, and it is a third of the disk cost.

Session `bb623d6c` also confirms the manifest defect independently: it declares
`enabledStreams: ["Trade"]` and contains 9,228 `BestBidAsk` frames — exactly the two-call-
site bug, visible in the data rather than inferred.

### Phase 5A — LIVE ACCEPTANCE PASS (2026-07-27, 19:20 VN / 08:20 ET rollover)

Observed on build `CDA0F51E` at the primary auction anchor:

```
EPISODE:  PARTIAL (3 ACTIVE / 6 CLOSED)
SCANNER:  6 EP / 2947 BAR | CAL STEP 2/6 STRATIFIEDDISTRIBUTION
```

Three things confirmed, the third for the first time in the project:

1. **Episodes close only at rollover.** `ExpireAllActive` closed six at the anchor. This
   settles the earlier `0 EP` reading as correct behaviour rather than a defect — the
   analysis of the three `CloseEpisode` call sites is now confirmed empirically.
2. **6 closed, 6 folded.** Exact match, no rows dropped between the registry and the
   dataset.
3. **The calibration protocol advanced on its own: step 1 -> step 2.** `CollectRawFeatures`
   is satisfied; the block moved to `StratifiedDistribution`, which is precisely where it
   was predicted to stop, because ParticipationRegime and VolatilityRegime have no
   registered boundaries.

The new auction rolled correctly alongside it: `TACTICAL CONTEXT` flipped
`UPDISCOVERY -> DOWNDISCOVERY`, and references re-prefixed `CUR -> PREV`.

Recorder ran throughout with `FAULTS: none`, writing both streams —
`enabledStreams: ["Trade","Dom"]`, depth climbing past 10,000 frames.

**This closes Phase 5A.** CODE PASS, TEST PASS and LIVE ACCEPTANCE all hold, so it is a
FINAL PASS — the first in the project. `[C]` states remain locked and the gate is
unchanged; what moved is the protocol's position, not its verdict.

**Next block is a research decision, not code:** registering volatility and participation
boundaries in the DECISION_LOG. The DLL cannot take that step and should not.

### Measured: what the Rithmic feed actually carries

Three capability values were hard-coded, so the card reported policy as observation. They
are now measured, and the first live run answered a question that had been open for the
life of the project.

| | Before | Measured | Meaning |
|---|---|---|---|
| Bid/ask | `Unknown` (pinned) | **`Unavailable`** | trades arrive in volume and **none** carry an aggressor side |
| DOM | `Unavailable` (pinned) | **`Partial`** | depth callbacks **do** arrive |
| MBO | `Blocked` (pinned) | `Blocked` (real) | operational lock, not an absence |

The classification is threshold-free — none / some / all — so no number was chosen. The
limitation strings follow the measurement, so `BIDASK_NOT_VALIDATED` clears itself if a
feed ever proves otherwise.

**Consequences, now settled rather than suspected:**

- `AggressorEvidenceMissing` on scanner rows is **not a defect**. It is this feed.
- `CanonicalOutsideDelta` on episodes is permanently null here.
- Auction Efficiency's ask/bid-dependent components stay unavailable for the same reason.
- Depth recording has real data to capture, which is why 5A-b's adapter was worth building.

**Deriving aggressor side from the quote was considered and rejected.** Comparing a trade
price to the prevailing best bid/ask is a standard technique, but it needs the trade and
the quote to be orderable against each other, and depth callbacks here carry no native
sequence — `DepthToRawEventAdapter` flags `NativeSequenceAbsent` on every frame. Aligning
them would be guesswork, and a derived side stored beside an observed one is exactly the
confident-but-wrong evidence the engine exists to avoid. The raw depth is recorded instead,
so the alignment question can be studied offline from data rather than assumed at runtime —
which is step 1 of `G-CAL-002`, not a shortcut past it.

### Phase 5A-d — participation regime axis, and step 2 is now unblockable

The last of the three axes. Unlike volatility, v1.2 §10.4 **does** specify this one, and
the specification carries a constraint that shaped the module: **volume percentile by
clock bucket**. An overnight period and a cash-session period are not comparable on volume,
so a single global boundary set would produce a distribution whose shape is mostly the
session calendar. Registration is therefore per clock bucket, keyed on the engine's own TPO
period index, and the axis stays unusable until **every observed bucket** has one.

Partial registration is explicitly refused. Stratifying some periods and leaving the rest
`Unavailable` produces a distribution over a silently self-selected subset — worse than no
distribution, because it looks like one. `UnregisteredClockBuckets` names exactly which
slots still need a decision, so the remaining work is visible and incremental rather than a
flat "not calibrated".

Boundaries are registered, not derived, for the reasons set out on 5A-c. The production
limitation already said so: `THIN_PARTICIPATION_PERCENTILE_THRESHOLDS_NOT_CALIBRATED`.

Two features of §10.4 are honestly unavailable and stay null:

| Feature | Why |
|---|---|
| trade count | the volume profile keys volume by tick and carries no per-level count |
| depth / spread | needs a bid/ask feed; this one reports `BID/ASK: UNKNOWN` |

`Dislocated` is kept as its own regime rather than the top of `Normal`, per §10.4 — an
event-driven period is not a busy normal one, and a thin overnight auction can still carry
real information after a shock.

**All three axes now exist.** With all three registered the protocol leaves step 2 and stops
at step 3, which is where it should stop: sample criteria are pre-registered by a research
process, and choosing them in the DLL after seeing rows is what `G-FAST-001` forbids. `D03`
asserts that no combination of inputs to `CalibrationProtocol.Evaluate` ever permits an
unlock — steps 4 to 6 are outside this build entirely.

Verified by mutation: accepting partial registration fails `C06`; letting a bucket borrow
another bucket's boundaries fails `B02`.

### Phase 5A-c — volatility regime axis

**Neither v1.2 nor v1.3 defines this axis.** It is named as mandatory five times
(`G-DISC-008`, `G-TF-004`, `G-CAL-002` step 2, §29.5, `G-FAST-003`) and nowhere is it said
what the buckets are or where their boundaries come from. That gap had to be closed by a
decision, and the decision matters more than the code.

**Rejected: boundaries derived from the observed sample.** It was the obvious way to break
the deadlock — no human input needed, the axis becomes usable immediately. It breaks the
protocol in three places at once:

| | |
|---|---|
| `G-FAST-001` | bins rebuilt as the sample grows re-choose the sample criteria after seeing results, continuously |
| reproducibility | the same episode changes stratum retroactively, so no distribution built on it can be reproduced or refuted |
| `G-CAL-002` step 4 | out-of-sample validation is impossible when the out-of-sample data helped build the bins judging it |

The third is structural: the protocol would stop being blocked at step 2 and start being
broken at step 4, silently. And "terciles, nearest-rank" is itself a coder choice that
moves the boundaries — the numbers come from data, the method does not, which is what
`G-CAL-001` actually forbids.

**Chosen: registered boundaries.** The DLL collects realized range per completed period —
observable, complete, no threshold involved — and applies boundaries decided outside. That
is the six-step protocol working as designed: DLL does step 1, a research process decides,
the DECISION_LOG records it, the decision comes back in frozen. Frozen bins make step 4
possible again.

`VolatilityRegimeBoundaries.Register` **refuses** rather than repairs: no DECISION_LOG
reference, or a mis-ordered pair, yields `Unregistered`. A boundary without provenance is
indistinguishable from one somebody typed in.

The axis therefore moves `NotImplemented` -> `ImplementedButNotCalibrated`. The protocol is
still blocked at step 2, but now on a decision somebody can make rather than on a
classifier that does not exist. Three new operator inputs under **Research**, all
deliberately unusable by default.

Verified by mutation: adding a `SuggestedLowerBoundaryTicks` that ranks the sample fails
`A01`; dropping the DECISION_LOG requirement fails `B02`.

**Remaining blocker for step 2: ParticipationRegime**, which rests on `ThinParticipationLabel`
— itself `THIN_PARTICIPATION_NOT_CALIBRATED`.

### Phase 5A-b — bar replay, because the live path cannot feed the scanner

First live run of 5A reported `SCANNER: 0 ROWS` and stayed there. Not a defect. Every
`CloseEpisode` call site was traced:

| Close path | Real frequency |
|---|---|
| `OnPrimaryAuctionChanged` | once per day, at the 08:20 ET anchor |
| `SyncEligibleReferences` — reference leaves the eligible set | rare |
| `InvalidateAll` — data fault | rare |

There is no time-based expiry, and `ReferenceIdentity` states that zone is not part of
identity, so a Developing level migrating from 4094.1 to 4093.2 keeps its `ReferenceId` and
retires nothing. Intra-auction episode reset is itself gated —
`INTRA_AUCTION_EPISODE_RESET_NOT_CALIBRATED`.

That closes a circle:

```
scanner rows  <-  episodes must close
episodes close intra-auction  <-  INTRA_AUCTION_EPISODE_RESET must be calibrated
calibrating it  <-  needs scanner rows
```

So the live path yields roughly one batch of rows per trading day — precisely the
multi-month passive wait v1.2 §46 exists to avoid.

**What was built.** `HistoricalBarReplayHost` walks the bars already loaded on the chart
against the confirmed reference set, buffering them from the existing per-bar pass so there
is no second walk. Previous-auction levels are fixed for the whole session, so measuring
this session's bars against them is exact; Developing references are excluded because
measuring an old bar against a level that did not yet hold that price is reconstruction,
not observation.

**It does not feed the episode registry.** `EpisodeTradeEvent` is documented as built from
trade prints and *never from candles* — a locked-phase invariant that nothing enforced.
Synthesising episode events from bar geometry was the obvious way to build this phase and
would have silently corrupted every acceptance conclusion downstream, because the sequence
would have been invented. `D03` now enforces it.

**The limit that shapes the design:** a bar has no intra-bar ordering. It cannot say whether
price crossed a level once or six times, nor which side it approached from. So bar rows are
a separate dataset, counted separately, never pooled with live rows, and unable to advance
the calibration protocol. The card shows a pair — `0 EP / 4 BAR` — not a total.

Both guards verified by mutation: adding a `CrossCount` to the bar record fails `A05`;
letting bar rows satisfy protocol step one fails `D02`.

What a bar *can* honestly give, and does: zone touch, close location, max excursion above
and below in ticks, volume at price inside the zone, and delta when both sides are
reported. Unavailable measurements are null, never zero.

### Phase 5A — Historical Scanner (step 1 of 6 only)

`src/GC.AuctionFlow/Research/`. Folds closed episodes into a raw-feature dataset so that
rule versions can be re-run later without re-collecting data (v1.2 §46.3). It performs
step one of the `G-CAL-002` unlock protocol and nothing else.

**Finding: the protocol is blocked at step two, not step three.**

`G-CAL-002` step 2 requires a distribution stratified by
`ReferenceType x ParticipationRegime x VolatilityRegime`. Only one of those three axes is
usable:

| Axis | Status | Why |
|---|---|---|
| ReferenceType | Available | emitted on every episode |
| ParticipationRegime | ImplementedButNotCalibrated | `ThinParticipationLabel` is itself `THIN_PARTICIPATION_NOT_CALIBRATED`; keying on it would calibrate one gate with another |
| VolatilityRegime | NotImplemented | no classifier exists in this build |

Pooling across the two missing axes would produce a distribution that looks complete and
is not, and a threshold derived from it would be exactly the coder-chosen number
`G-CAL-001` bans. So the gate reports `CAL STEP 2/6 STRATIFIEDDISTRIBUTION` rather than
advancing to the sample-criteria wall.

**Consequence for the roadmap:** unlocking `[C]` needs a volatility regime classifier and
a calibrated participation regime *first*. Those were not previously on the critical path
for 5A. The scanner is useful now — it accumulates rows either way — but the unlock is two
modules further away than the v1.3 roadmap implies.

Of the nine §46.7 first-workload studies, seven are `AwaitingPrerequisite` and each names
its own blocker; two (Day Structure distribution, pre-settlement episode outcome) reach
`AwaitingSampleCriteria`, which is the honest terminus — `G-FAST-001` forbids choosing
sample criteria after seeing rows, and that decision belongs outside this build.

What the module deliberately cannot do, each verified by mutation:

- compute any statistic (`A03` — adding a `double MedianRowsPerReferenceType` fails it)
- introduce a calibration-gated state (`F04`/`C02` — adding `Calibrated = 100` fails both)
- report `UnlockPermitted` as true under any input (`B01`)
- progress a study past `AwaitingSampleCriteria` (`C03`)
- label a dataset row with a verdict (`D06`)

Retention is bounded at 2048 rows, and `RowsCollected` is tracked independently of what is
retained — same reasoning as the price memory ledger's `TotalTests`. A dataset that saw
3000 episodes and reports 2048 is lying about sample size, and sample size is exactly what
a calibration decision would rest on.

Schema `0.25.0` -> `0.26.0`. GPS rows 22 -> 23.

### Closed: the module chain is declared once

Every integration defect found during live acceptance was a symptom of one thing: the
chain order lived in **two hand-maintained call lists**, one in `OnCalculate` and one in
`PublishRuntimeSnapshot`. They had already diverged — Resolution and Trade Facilitation
sat at different points in each — and the divergence was invisible, because each list
looked complete on its own.

`ModuleDriveSchedule` (`src/GC.AuctionFlow/Runtime/ModuleDriveSchedule.cs`) replaces both.
One declaration of 19 steps, each carrying a bar drive, a publish drive and its
dependencies. Both handlers now call `RunBar()` / `RunPublish()` and keep no order of
their own.

What this makes structurally impossible rather than merely caught:

| Defect class from the live run | Why it cannot recur |
|---|---|
| Module reachable only via its publish guard | A guard is only ever a step's publish delegate; the bar drive is a separate required argument |
| Module driven on one path but not the other | A step has no way to opt out of a pass — publish defaults to the bar drive |
| Two paths in different orders | There is one order |
| Module reads an input driven after it | Construction throws |

The schedule is a plain type with no ATAS dependency, so `ModuleDriveScheduleTests` runs
it for real — 15 behavioural tests rather than source-text inspection. What still cannot
be instantiated is the indicator's *declaration* of the chain, so
`IndicatorProcessChainTests` parses that single list. All five of its assertions were
verified by mutation, listed in the file header; each mutation fails exactly the tests it
should and no others.

Coordinator extraction is **partial by intent**. The orchestration that caused real
defects is out; the ~2400 lines of ATAS-coupled module bodies are not, because moving
them requires abstracting `GetCandle`, `CurrentBar`, `InstrumentInfo` and ~40 operator
properties, and there is no test that could protect that move today.

### Closed: `COMPLETED PERIODS: 17` vs `TPO PERIOD INDEX: 36` — correct, not a defect

Auction `PI-2026-07-26` is anchored 08:20 ET on a **Sunday**, and COMEX gold does not
reopen until 18:00 ET.

| | |
|---|---|
| 08:20 -> 18:00 | 19.33 periods, market closed |
| 18:00 -> 02:45 | 17.5 periods with data |
| total elapsed | 36.83 -> index 36 |

`36 - 19 = 17`. The engine correctly excludes periods with no trading. Reporting 36 would
have been the defect, because it would mean fabricating structure for hours when the
market was shut.

This took a round trip and manual arithmetic only because the card never showed
`HistoricalInitializationState`. That row now exists, so the same question is answerable
from a screenshot.

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

## DLL integrity note (2026-07-27) — CORRECTED

A progress audit found the deployed DLL did not match a fresh build of the committed
source. An initial explanation blamed incremental build state. **That was wrong**, and
the real cause matters more.

### Root cause: git line-ending normalisation

`core.autocrlf = true` and the repository has no `.gitattributes`. Source files are
written with LF, then `git add` / `git commit` rewrites them to CRLF **in the working
tree**. The source bytes therefore change after the build that was deployed.

Verified rather than assumed:

- Three consecutive clean-from-scratch rebuilds of the committed source all produce the
  identical hash, so the build **is** reproducible
- Working tree clean at the time of measurement
- Files committed this session are CRLF on disk despite being authored LF

### Consequence for every phase in this session

The closeout order used was **build -> deploy -> record hash -> commit**. Because the
commit mutated the source afterwards, the recorded hash certified an artifact built from
a source state that no longer existed on disk. The DLLs were functionally correct — line
endings do not change C# semantics — but the SHA-256 gate proved nothing.

The check was also weak in a second way: comparing `source` against `deployed`
immediately after copying compares a file with the copy just made of it, which always
passes.

### Corrected procedure

```
commit  ->  rm -rf bin obj  ->  build  ->  run tests  ->  deploy  ->  record hash
```

The hash must be taken from a clean build of **already-committed** source. Under that
order the current DLL verifies:

| | |
|---|---|
| Commit | `b2c1709` |
| Clean-rebuild SHA-256 | `BECEA77113AF134292804EB3F18D9FC61F551C48C8D6EF45EC49E8D94CF1EEE4` |
| Reproducible | 3/3 identical clean rebuilds |
| Tests on that build | 1226 passed |

### Follow-up worth doing

Adding a `.gitattributes` pinning `*.cs text eol=lf` would remove the mutation entirely
and make the build reproducible across machines. Not done yet — it rewrites every tracked
file and deserves its own change.

## Phase 1H code/test (2026-07-27) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 1174 passed x2 / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Runtime schema | `0.23.0` (unchanged — module is RESEARCH_ONLY, not published) |
| Policy | `DAY_STRUCTURE_POLICY_V1` |
| Source/deployed DLL SHA-256 | `B8873E0E5219B6003BB969924CDBFFC1D14398DD752938BC610E1134932D9319` (clean-rebuild canonical; see integrity note) |
| Status | **RESEARCH_ONLY** per v1.2 §18.4 |
| Every shape label | **RESERVED** — no day type is ever named |
| New Phase 1H tests | 37 tests (A01-H02); total 1174 |
| Spec source | v1.2 §18.4 + §18.4.1; KDK Ch 11 |

### The look-ahead split (v1.2 §18.4.1)

The highest-value correctness item in this phase. Two layers are kept strictly apart:

- **Live feature** — `CurrentDayExtremeStillEqualsIbHigh` / `...IbLow`. Safe to read at
  any time during the session.
- **End-of-day labels** — `IbHighWasDayHigh` / `IbLowWasDayLow`. **Null until the session
  completes.** Producing them intra-session would leak the session outcome into a live
  decision, which is exactly the look-ahead v1.2 §18.4.1 exists to prevent.

Test F01 asserts the EOD labels are withheld intra-session; F04 asserts they appear only
after completion.

### Phase 1H present (code/test)

- `DayStructureState` carries every v1.2 §18.4 shape name, and **all of them are
  reserved at 100+**. The module reports `NotCalibrated` while developing and
  `SessionComplete` after close — never a type
- Initial balance from the first two completed TPO periods. Two is the conventional
  reading, not a calibrated finding, and is declared as such
- **A partial initial balance is not an initial balance** — one period yields
  `Unavailable`, not a half-formed IB (test C02)
- Periods ordered by index, not arrival, so a later period cannot contaminate the IB
  (test C04)
- Range extension direction, extension ticks per side (never negative), session range,
  close-location band
- `RevisionCount` per session, reset on auction change

### Phase 1H NOT present (code/test)

- Any day-type label (all shape rules calibrated)
- `IbWidthPercentile` — needs a historical IB-width distribution (Phase 5A)
- POC / value migration series — Phase 1A does not expose a developing-profile time
  series, so both report null rather than a guess
- Double-distribution evidence (rule calibrated)
- Entry, veto or score surface — asserted absent by test H01

Phase 1A Profile is LOCKED; git diff confirms it was not touched.

## Phase 2H code/test (2026-07-27) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 1137 passed x2 / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Runtime schema | `0.22.0` -> `0.23.0` |
| Policy | `IMBALANCE_POLICY_V1` |
| Source/deployed DLL SHA-256 | `0C2F2D1258C00451EF34BA60D9DC6C5D9B7BE80410E0D929FA462173CC305CF5` (exact match) |
| Qualification / Stacked | **NOT CALIBRATED** — both rules reserved |
| GPS rows | **17** (was 16); `IMBALANCE:` row added |
| New Phase 2H tests | 34 tests (A01-F04); total 1137 |
| Spec source | KDK Ch 25; v1.3 AP-018 |

### Scope note — read before expecting new measurement

Phase 2B already computes **every ratio KDK Ch 25 needs**: same-price both directions,
diagonal both directions, raw dominant side, consecutive dominance run.

The rest of Ch 25 — the ratio rule and the minimum-volume rule — is calibrated. So this
phase adds **one new measurement** (location context) and is otherwise a **gate and
context module**, not a measurement module. That is stated plainly rather than dressed up.

### The one new measurement

`ImbalanceLocationContext` (KDK Ch 25 "Vị trí"): mid-value imbalance may be nothing more
than part of a rotation; imbalance at a boundary inside an episode is more notable.
`MidValue` / `ValueBoundary` / `OutsideValue` / `Unavailable`.

Location is resolved **before** any early return, because knowing where price sits does
not require a populated ladder. Reporting `Unavailable` while the location is actually
known would be a lie — a test caught this during implementation.

### Phase 2H present (code/test)

- Both comparison modes named (`SamePrice`, `Diagonal`) — KDK ranks neither as
  universally correct, so both ratios travel together
- Ratio **never carried without the volume behind it**: `ClassifiedVolume`,
  `UnknownAggressorVolume` and `AggressorCoverageRatio` are on every level, because a
  huge ratio on tiny volume is meaningless
- Unknown-aggressor volume is **disclosed, not dropped** (KDK Ch 25 mistake list)
- `LevelsWithoutRatioCount` — levels where aggressor classification produced no ratio at
  all; `Unavailable`, never zero
- AP-018 enforced structurally: test B05 asserts the snapshots carry **no acceptance,
  re-entry, resolution or thesis surface**, so acceptance cannot be read out of an
  imbalance
- `IMBALANCE_IS_NOT_PERMANENT_SUPPORT_RESISTANCE` — the KDK Ch 25 mistake of treating a
  stack as permanent support or resistance

### Phase 2H NOT present (code/test)

- Ask/Bid imbalance qualification (ratio rule calibrated)
- Stacked imbalance labelling (stack size calibrated)
- Any threshold constant — test D04 asserts the policy exposes none

## Phase 1I code/test (2026-07-27) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 1103 passed x2 / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Runtime schema | `0.21.0` -> `0.22.0` |
| Policy | `PRICE_MEMORY_POLICY_V1` |
| Source/deployed DLL SHA-256 | `8125C05F93E0B36BAFCDCB4CFEAAED6DD584230E6427F79B49DA68AD77266082` (exact match) |
| Reference strength | **NOT CALIBRATED** — Strengthening / Weakening / Stable reserved |
| GPS rows | **16** (was 15); `MEMORY:` row added |
| New Phase 1I tests | 44 tests (A01-H06); total 1103 |
| Spec source | v1.3 §13 `G-REF-001` / `G-REF-002`; KDK Ch 27 |

### The invariant this phase exists to protect

`G-REF-001`: there is **no one-directional rule** that a level tested many times becomes
weaker, or stronger. KDK Ch 27 gives the reason — passive liquidity can be replenished
between tests, so a raw count carries no directional meaning.

Test B01 folds fifty tests against one reference and asserts the strength state is
identical to a reference tested once. Both report `NotCalibrated`.

### Phase 1I present (code/test)

- `PriceMemoryHost` — folds `AuctionEpisodeSetSnapshot` into a per-reference ledger.
  Every episode against a reference is one test; the first is the first test, the rest
  are retests
- Deliberately **stateful across rebuilds**: memory that forgets between snapshots is not
  memory. Idempotent per `EpisodeId`, so refolding an evolving episode updates the record
  in place and never inflates the count (tests C04, C05)
- `ReferenceTestRecord` — episode id, timestamps, final state, resolution, attempt count,
  outcome
- Outcomes are mechanical only: `InProgress` / `Expired` / `InvalidData` /
  `NotCalibrated`. Held-vs-broken needs calibrated acceptance and re-entry resolution and
  stays reserved (test D03)
- `LiquidityReplenishmentObservability` always `Unavailable` — Tier-3 data, MBO BLOCKED,
  never inferred from price
- LIVE_ONLY: the ledger starts at indicator start; pre-start tests are unknown, not
  absent, and the card says so

### Phase 1I NOT present (code/test)

- Any reference-strength verdict (calibration gate)
- Held / Broken test outcomes (need Phase 2D resolution calibrated)
- Liquidity replenishment observation (MBO BLOCKED)
- Barrier permeability: Phase 3E can now be given reaction history, but a **count is not
  a verdict** — permeability stays `NotCalibrated` until 5A

### Bug caught by its own test

`TestCount` was derived from the retained record list, so once a reference exceeded the
64-record cap the reported count silently dropped back to 64. A reference tested 74 times
would report 74 tests before truncation and 64 after — history rewritten by a storage
limit. `TotalTests` is now tracked independently of retained records (test G08).

## Phase 3E-b code/test (2026-07-27) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 1059 passed x2 / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Runtime schema | `0.20.0` -> `0.21.0` |
| Source/deployed DLL SHA-256 | `F064322B55075DF738CEDD55ADB2BCF58D43B45D7CE294327AED4FA848B5E62F` (exact match) |
| GPS rows | **15** (was 14); `PATH:` row added |
| New tests | 7 (H01-H07); total 1059 |

### Gap this closes

Phase 3E built the PLAR module and correctly fed it into the location gate, but never
published `PlarSetSnapshot`. The corridor, barriers and remaining target space existed
only inside the engine — the operator could see a candidate vetoed for "no room" without
being able to see what room was measured. That is now on the card.

### Present

- `GcaeRuntimeSnapshot.Plar` + `ShowPlarDiagnostics`; `PLAR_STATUS=` in profile extras;
  PLAR limitations merged into `KnownLimitations`
- GPS diagnostic row 15: `PATH:`
- `BuildPlarLines` — per-direction target space, next barrier, next target; diagnostics
  add the full corridor with each obstacle's role and the final target
- Target space rendering keeps the three cases visually distinct:
  `40 ticks` / `NONE AHEAD (VETO)` / `NOT MEASURABLE`. A measured veto and an
  unmeasurable space must never look alike (test H05)
- Indicator: `ShowPlarDiagnostics`

## Phase 3E code/test (2026-07-27) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 1052 passed x2 / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Runtime schema | `0.20.0` (unchanged — PLAR is not yet on the runtime snapshot) |
| Policy | `PLAR_POLICY_V1` |
| Source/deployed DLL SHA-256 | `3C196B9E23E47BBCE98A2F46930FCE7E5614F6D6C02DABF50E17EF053320A7B3` (exact match) |
| Barrier permeability | **NOT CALIBRATED** — LowFriction / ModerateFriction / HighFriction reserved |
| New Phase 3E tests | 44 tests (A01-G06); total 1052 |
| Spec source | v1.2 §33; v1.3 §7.4 `G-FAR-006` |

### Closes G-LOC-002

Phase 3D could enforce `G-LOC-001` and `G-LOC-003` but not `G-LOC-002`, because
`RemainingTargetSpace` did not exist. Phase 3E supplies it and the veto is now live.

`TargetSpaceAvailability` keeps three cases apart, which is the whole point:

| Case | Meaning | Gate effect |
|---|---|---|
| `Available` | target ahead, distance known | veto not triggered |
| `NoTargetAhead` | references exist, none ahead — **measured** no room | `BlockedNoTargetSpace` |
| `Unavailable` | no references or no price — **cannot measure** | veto reported unmeasurable, never treated as passed |

Blocking reasons are separated to match: `NoRemainingTargetSpace` (measured) versus
`TargetSpaceUnavailable` (unmeasurable). The veto outranks position quality — a perfect
location with nowhere to go is still not tradeable (test G03).

### POC is a barrier, not just a target (G-FAR-006)

Every POC reference type maps to `PathObstacleRole.TargetAndBarrier`. KDK Ch 64 lists POC
as a valid FAR target, and KDK Ch 19 Rule 4 warns it can end a rotation before the
opposite edge is reached. Modelling it only as a target is the mistake the guard names.

### Phase 3E present (code/test)

- `PlarHost` — projects the active reference set forward from the current price in both
  directions; fingerprint-gated on price and reference-set revision
- `PathObstacleSnapshot` — distance to the **near edge**, always non-negative, ordered
  nearest-first with a corridor index
- Price inside a zone is interacting with it, not travelling toward it, so it is not
  "ahead" (test C06)
- `Corridor` — Barrier 1..3 per v1.2 §33.2
- Retired references are excluded

### Phase 3E NOT present (code/test)

- Barrier permeability / expected friction — needs reaction history (Phase 1I) and the
  adjacent-build research of v1.2 §33.2.1, which is research-only by spec
- TP1/TP2/TP3 ladder (v1.2 §33.4) — requires an entry price, which remains out of scope
- Progress-milestone verdicts (SlowProgress, EarlyWeakness, TargetDowngradeCandidate)
- Entry / stop / size / score fields — asserted absent by test E04

### Stale invariant corrected

`MaturityBlockingReason.TargetSpaceUnavailable` was unconditional in Phase 3B, when the
space genuinely could never be measured. It is now conditional; leaving it unconditional
would have made a measured, real target space still report as unmeasurable.

## Phase 3D code/test (2026-07-27) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 1008 passed x2 / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Runtime schema | `0.19.0` -> `0.20.0` |
| Policy | `SIGNAL_MATURITY_POLICY_V1` (extended; version unchanged) |
| Source/deployed DLL SHA-256 | `A87C2528B3425AA19215CFB117929517E17AF2A4B12AEB019D955D8C6FF6B4D4` (exact match) |
| GPS rows | 14 (unchanged — gate rows nest inside the MATURITY block) |
| New Phase 3D tests | 26 tests (A01-G03); total 1008 |
| Spec source | v1.3 §10 `G-LOC-001..003`; v1.2 §2.3 |

### Why this phase exists

v1.2 §2.3 asserts that orderflow only has meaning in Context and Location, but no
module enforced the Location half. `PriceValueLocation` has existed since Phase 1D and
was never read by the thesis path.

### What is enforced, and what is not

| Guard | Status |
|---|---|
| `G-LOC-003` — location Unavailable => no candidate | **ENFORCED**. Pure availability check. |
| `G-LOC-001` — mid-value / at-POC => low quality, never Confirmed | **ENFORCED**. Pure position check. |
| `G-LOC-002` — no target space => hard veto | **NOT ENFORCEABLE**. `RemainingTargetSpace` arrives with the Target Engine (Phase 3E). Declared unavailable via `REMAINING_TARGET_SPACE_NOT_AVAILABLE` + `MaturityBlockingReason.TargetSpaceUnavailable`; `LocationGateOutcome.BlockedNoTargetSpace` is reserved at 100 and unreachable (test E02). |

Treating "cannot evaluate" as "passed" would be the dangerous failure here, so the veto
is reported as unavailable rather than silently satisfied (test E01).

### Phase 3D present (code/test)

- `LocationGateOutcome`: `BlockedLocationUnavailable` / `AllowedLowQuality` /
  `AllowedBoundary` / `AllowedOutside`; `BlockedNoTargetSpace` reserved
- `MaturityBlockingReason.LowQualityLocation` added
- Gate evaluated in `SignalMaturityHost` **before** the lifecycle is finalised; a blocked
  scope is capped at `EpisodeActive` instead of reaching `Candidate`
- A blocked scope is still **reported**, not dropped — dropping it would hide the block
  from the operator (test C02)
- Terminal states (Invalidated / Expired / Completed) are never disturbed by the gate
  (test C05)
- Location resolution prefers current-primary **volume** profile over TPO, same rationale
  as Phase 2F-b: executed activity over time distribution
- Gate is positional only — independent of thesis direction (test F01) and never unlocks
  a maturity level (test F02)
- Location participates in the maturity fingerprint, so a location change rebuilds (F03)

### API change

`SignalMaturityHost.Rebuild` gained a `location` parameter before `nowUtc`. Phase 3B and
3C test call sites were updated to pass a resolvable location, so they keep testing
lifecycle mapping and contract construction rather than the gate. Gate behaviour is owned
solely by `Phase3DLocationGateTests`.

## Phase 2F-b code/test (2026-07-27) — LIVE ACCEPTANCE PENDING

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 975 passed x2 / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **REQUIRED** — focused gate pending |
| Runtime schema | `0.18.0` -> `0.19.0` |
| Policy | `TRADE_FACILITATION_POLICY_V1` (extended; version unchanged) |
| Source/deployed DLL SHA-256 | `7163C06ECB1E0971DCF393333D068922976E3F1D29980E699F6FEE0F286F3FEC` (exact match) |
| Facilitation classification | **NOT CALIBRATED** — unchanged; completeness is a precondition, not a verdict |
| GPS rows | 14 (unchanged — component rows nest inside the TRADE FACILITATION block) |
| New Phase 2F-b tests | 28 tests (A01-E04); total 975 |
| Spec source | v1.3 §5.2 `G-TF-002` / `G-TF-004`; KDK Ch 31 |

### Why this phase exists

KDK Ch 31 defines facilitation as a convergent conclusion from **four** components:
Activity, Progress, Structure and Maintenance. Phase 2F shipped only the first two, so
`G-TF-002` ("all four present or explicitly unavailable before Healthy/Failing") could
not be satisfied even in principle.

### Audit finding

Every measurement needed already existed on `AuctionResultEvidenceVector` from Phase 2C —
`VolumePocMigrationTicks`, `TpoPocMigrationTicks`, the value-centroid migrations,
`ProgressRetainedTicks`, `ProgressRetentionRatio`, `TimeAtMaximumExcursion`. None of them
had ever been surfaced to the facilitation module. No new measurement was required.

### Phase 2F-b present (code/test)

- `FacilitationComponentAlignment`: `Unavailable` / `Unknown` / `Aligned` / `Opposed` / `Flat`
- `FacilitationComponent`: the four KDK Ch 31 components, named explicitly
- **Structure**: signed POC migration (volume POC preferred over TPO POC — it reflects
  executed activity rather than time distribution) plus value-centroid migration, compared
  by SIGN against the attempted direction
- **Maintenance**: `ProgressRetainedTicks`, `ProgressRetentionRatio`,
  `TimeAtMaximumExcursion` — raw, unjudged
- `AvailableComponentCount` (0..4) and `ComponentsComplete`, making `G-TF-002` assertable
- Alignment is deliberately **magnitude-free** (test B11): whether a migration is large
  enough to matter is regime-dependent and calibrated (`G-TF-004`)
- Missing migration reports `Unavailable`, never `Flat` (test B07) — absent data must not
  be read as "no migration"

### Phase 2F-b NOT present (code/test)

- Healthy / Failing verdict — still gated. Test D06 asserts that having all four
  components does NOT unlock a classification: completeness is a precondition only.
- Magnitude thresholds for migration or retention (`G-TF-004`, needs regime stratification)

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
