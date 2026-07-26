# Review Checklist — Phase 1B Composite Profile Foundation + Phase 1C Structural References

## Prior baseline

- [x] Phase 1A PASS + locked (`gcae-p1a-primary-tpo-volume-profile-pass` @ `771095eaadba55729dcb569a0c96b5da1ab12ddc`)
- [x] Phase 1A Primary TPO/VP engines not redesigned
- [x] P0-07C3D Trade Recorder remains locked
- [x] Master spec v1.2 untouched
- [x] MBO not enabled in primary process

## Composite governance

- [x] No hard N-day / fixed rolling-window Production merge
- [x] OperatorAnchored confirmed path; no silent anchor selection
- [x] ShadowEvidence does not mutate confirmed composite
- [x] Uncalibrated thresholds → NotCalibrated / NOT CALIBRATED / NOT EVALUATED text
- [x] No StableBalance / Breaking / NewValue Production labels yet

## Implementation

- [x] CompositePolicyMode Disabled / OperatorAnchored / ShadowEvidence
- [x] CompositeAuctionContribution from immutable Phase 1A snapshots only
- [x] CompletedAuctionLedger replace-by-version; epoch/tick/policy fail-closed
- [x] CompositeAggregator tick-index TPO + exact volume sums
- [x] Reuses Phase 1A PocSelector + ValueAreaCalculator
- [x] ConfirmedCompositeProfileSnapshot + DevelopingCompositePreviewSnapshot
- [x] Deterministic CompositeId (identity|epoch|anchor|policy|ordered ids)
- [x] Merge-evidence metrics (overlap, POC/centroid displacement, outside share, etc.)
- [x] Runtime snapshot Composite fields; atomic publication
- [x] GPS composite rows (AwaitingAnchor / Ready / Partial / Evidence)
- [x] Optional Confirmed vs Preview overlay labels (no S/R / Entry language)
- [x] Bounded transition logging
- [x] Composite Ready does not force global Data Ready
- [x] Disabled composite does not degrade Primary Profile
- [x] Publish-path init when Current missing
- [x] Operator-configuration fingerprint rebuilds on settings change (not every trade)

## Explicitly deferred (still deferred after Phase 1C foundation)

- [x] No nPOC / HVN-LVN intelligence / weekly-monthly-IB-VWAP-swing fabrication
- [x] No Directional Auction Context / One-Time Framing / Episode
- [x] No Acceptance/Re-entry / FAR/AAC / Entry/Invalidation/Targets
- [x] No Orderflow / DOM / MBO / Telegram / Adaptive TPO
- [x] No automatic Production merge/close thresholds

## Live acceptance Phase 1B (operator anh Fen — GCQ6 / Rithmic Live)

- [x] Blank anchor → `COMPOSITE: AWAITING ANCHOR` / `COMPOSITE ANCHOR: —`
- [x] Valid loaded anchor `PI-2026-07-20` → `COMPOSITE: READY` (PARTIAL allowed)
- [x] Two-way blank ↔ nonblank propagation on same instance
- [x] Multiple completed historical auctions (3: `PI-2026-07-20`…`22`)
- [x] Correct range / count / TPO+Volume profile outputs observed
- [x] Developing auction (`PI-2026-07-23`) does not mutate Confirmed membership
- [x] Preview ON/OFF isolation (Confirmed Id/count/membership retained)
- [x] Remove/add lifecycle — one card, one overlay set
- [x] MBO remains OFF / BLOCKED
- [x] Global DATA DEGRADED independent of Composite Ready
- [x] Source/deployed DLL SHA match `CA2157509B140D0752FB4FCEF70E1FCD863553053D6E133566B848BBDDB02B87`
- [ ] Optional ATAS built-in multi-day profile equality — **not required** (benchmark only)

## Verify Phase 1B (closeout)

- [x] Release restore/build/test green (329 passed, 0 warnings)
- [x] Phase 1A + Trade Recorder regressions green
- [x] MBO remains off/blocked
- [x] Master specification untouched
- [x] Selective Phase 1B commit + tag (no OAC / no solution contamination)
- [x] No push (no upstream configured)

## Phase 1C Structural Reference Foundation — code/test

- [x] Previous Primary Confirmed references (High/Low/TPO/VA + exact volume when available)
- [x] Current Primary Developing references
- [x] Confirmed Composite references (READY/PARTIAL); Preview ignored
- [x] Deterministic ReferenceId stable when Developing zone moves
- [x] StateVersion increments on zone change; identical republish idempotent (registry-owned)
- [x] Confirmed immutable / stale revision / tick-epoch-policy fail-closed
- [x] Lifecycle Fresh/Active/Expired/Retired only (no Episode statuses)
- [x] Exact-tick confluence; adjacent ticks do not group; no strength score
- [x] Nearest above/below in ticks; no support/resistance
- [x] Input fingerprint reuse on ordinary trade-style publish
- [x] Module disabled does not degrade Primary/Composite (**automated coverage**)
- [x] Reference Ready does not clear global DATA DEGRADED
- [x] GPS READY/PARTIAL/AWAITING + diagnostics without Episode/Thesis/Long/Short
- [x] Overlay Confirmed/Developing/MIXED wording; one line per exact zone
- [x] MIXED confluence display (**automated coverage**)
- [x] Individual Developing StateVersion monotonicity (**automated coverage**)
- [x] Remove/add renderer lifecycle / no duplicate overlay set (**automated coverage**)
- [x] Source-scope: no Phase 1D Episode/Thesis/FAR/AAC/Entry wiring
- [x] GCAE tests green (372 passed / 0 failed / 0 skipped)
- [x] 0 build errors / 0 warnings
- [x] Runtime schema 0.4.0; REFERENCE_POLICY_V1; REFERENCE_OVERLAY_POLICY_V1; assembly 0.0.6

## Phase 1C live acceptance (operator — GCQ6 / Rithmic Live)

- [x] Gate 1: REFERENCES READY; policy V1; Confirmed 16 / Developing 8 / Confluence 19
- [x] Primary Profile READY; Composite READY; DATA DEGRADED independent; MBO BLOCKED
- [x] Gate 2: Developing movement (TPO VAH 4064.0→4064.1) remained READY; no STALE_REVISION
- [x] Gate 3: Restrained overlay — unique zones; CONFIRMED/DEVELOPING wording; dense labels suppressed
- [x] Gate 4: Reference overlay OFF — REF lines/labels gone; Primary/Composite visuals returned; engines READY
- [x] Gate 5: Reference overlay ON again — one REF set; no duplicate; no STALE_REVISION
- [x] Source/deployed DLL hashes match `9CAE06B9091D23854DA60A425E45884D2CC0D5C50E501A4E9EB1B12A58FAF3CA`
- [x] Documented live coverage limitation accepted (module disable / remove-add / MIXED live / per-id StateVersion visuals = automated)
- [x] FINAL PASS WITH DOCUMENTED LIVE COVERAGE LIMITATION

## Phase 1D Multi-Horizon Directional Context — code/test

- [x] Pairwise migration decision table (Discovery/Rotation/Balance/Conflicted/Transition/Unknown)
- [x] Up/Down Discovery
- [x] Up/Down Rotation
- [x] Balance
- [x] Transition
- [x] Conflicted
- [x] Unknown/fail-closed behavior
- [x] Exact volume Unavailable never fabricated as zero
- [x] Tick/epoch/timestamp compatibility fails closed
- [x] Completed-auction-only Structural context; no fixed five-day window
- [x] Insufficient history → Unknown + limitation
- [x] Developing Tactical context; idempotent republish; identity stable per auction
- [x] Completed-period-only OTF; DevelopingUp/Down/Broken/Mixed
- [x] No calibrated OTF Confirmed state (`OTF_CONFIRMATION_NOT_CALIBRATED`)
- [x] Exact-tick price location; no S/R wording
- [x] Missing-volume Partial handling
- [x] Module disabled does not degrade Profile/Composite/References
- [x] Directional Ready does not clear global DATA DEGRADED
- [x] Input fingerprint reuse on ordinary trade-style publish
- [x] GPS READY/PARTIAL/AWAITING + Developing label; no Long/Short/Buy/Sell/Thesis/Entry/Probability
- [x] No Episode/Acceptance/Orderflow production dependency
- [x] No directional chart arrows / duplicated reference lines
- [x] Source-scope: Phase 1E not started *(historical at 1D lock; superseded by Phase 1E)*
- [x] GCAE tests green (399 passed / 0 failed / 0 skipped)
- [x] 0 build errors / 0 warnings
- [x] Runtime schema 0.5.0; DIRECTIONAL_CONTEXT_POLICY_V1; assembly 0.0.6

## Phase 1D live acceptance (operator — focused first gate)

- [x] GCQ6 / Rithmic Live; Primary + Composite READY; References READY; Directional ON; OTF ON; Preview OFF; MBO OFF/BLOCKED
- [x] Card core: DIRECTIONAL CONTEXT READY; POLICY V1; STRUCTURAL CONFLICTED; TACTICAL DOWNDISCOVERY (DEVELOPING); OTF DEVELOPINGDOWN; PRICE LOCATION InsideValue
- [x] No Long/Short/Buy/Sell/Thesis/Entry/Target/Probability/Acceptance/Rejection/Episode wording
- [x] DATA DEGRADED independent of Directional READY; MBO BLOCKED
- [x] Source/deployed DLL hashes match `9976E848578B9057503EC0D8A866C1593CED189EDC5EC04563940F605F0C6AFB`
- [x] FINAL PASS WITH DOCUMENTED LIVE DIAGNOSTIC COVERAGE LIMITATION

### Automated-only (not manually observed live)

- [x] Transition count details (**automated coverage**)
- [x] Current/previous auction ID diagnostics (**automated coverage**)
- [x] Component migration diagnostics (TPO value/POC, VPOC) (**automated coverage**)
- [x] OTF streak/count diagnostics (**automated coverage**)
- [x] Individual Directional StateVersion values (**automated coverage**)
- [x] Input fingerprint/version diagnostics (**automated coverage**)

## Phase 1E Auction Episode Observation — code/test

- [x] Reference-role mapping (Upper/Lower/Centerline); Developing excluded
- [x] Exact touch → Interacting; boundary cross → OutsideAttempt
- [x] AttemptCount rules; continued outside does not increment
- [x] Geometric ReentryDeveloping; second outside AttemptCount=2; one EpisodeId
- [x] Duplicate trade / card republish idempotent
- [x] Centerline CrossCount; never OutsideAttempt/ReentryDeveloping
- [x] Reserved Acceptance/Unresolved states never emitted
- [x] Outside metrics (distance/volume/trades/duration); inside excluded
- [x] Aggressor unavailable remains unavailable (Partial), not zero
- [x] LocalPoc deterministic when exact volume; not acceptance
- [x] EpisodeId / StateVersion / EventRevision policies
- [x] Per-auction dedup ledger; auction transition clears ledger + Expired
- [x] LIVE_ONLY history mode; no candle fabrication
- [x] Module default OFF; Ready does not clear DATA DEGRADED; MBO irrelevant
- [x] GPS DISABLED/AWAITING/READY/PARTIAL; no Sweep/FAR/AAC/Long/Short/Acceptance wording
- [x] Source-scope: Phase 1F Acceptance not started; no Episode overlay/alerts
- [x] Prior Phase 1A–1D regressions green
- [x] GCAE tests green (439 passed / 0 failed / 0 skipped)
- [x] 0 build errors / 0 warnings
- [x] Runtime schema 0.6.0; AUCTION_EPISODE_POLICY_V1; assembly 0.0.6
- [x] OnNewTrades Episode admission independent of EnableTradeStreamProbe (D-P1E-002)
- [x] Trade callback admission (`OnNewTrade` / `OnNewTrades`)
- [x] Batch event forwarding
- [x] Eligible Confirmed references only
- [x] Deterministic EpisodeId; one active episode per key
- [x] Event ordering and dedup
- [x] Current Primary Auction reset
- [x] Registry revisions; metrics; LocalPoc
- [x] Runtime/card states; Data Gate independence; MBO independence

## Phase 1E live acceptance (operator — focused first gate)

- [x] GCQ6 / Rithmic Live; Primary + Composite READY; References READY; Directional ON; Auction Episodes ON; Episode Diagnostics ON; Preview OFF; MBO OFF
- [x] Card: EPISODES PARTIAL; POLICY V1; ELIGIBLE REFERENCES 16; ACTIVE EPISODES 4; LATEST DEVELOPING
- [x] After wiring fix: not AWAITING TRADES; live trade admission PASS
- [x] Centerline live: PreviousPrimaryVpoc @ 4051.2; ROLE CENTERLINE; AttemptCount 0; MAX EXCURSION 60 ticks; DIRECTION DOWN
- [x] History mode LIVE_ONLY; no Sweep/FAR/AAC/Acceptance/Long/Short; DATA DEGRADED independent; MBO BLOCKED
- [x] Source/deployed DLL hashes match `924DB65C4D719D831926C81392AF600A332CD6BFF81401C5B6FC9E30CDFACBC2`
- [x] FINAL PASS WITH DOCUMENTED LIVE STATE-MACHINE COVERAGE LIMITATION

### Automated-only (not manually observed live)

- [x] UpperBoundary first cross → OutsideAttempt (**automated coverage**)
- [x] LowerBoundary first cross → OutsideAttempt (**automated coverage**)
- [x] Continued outside trades do not increment AttemptCount (**automated coverage**)
- [x] Geometric return → ReentryDeveloping (**automated coverage**)
- [x] Second outside excursion → AttemptCount 2 (**automated coverage**)
- [x] Repeated crossing retains same EpisodeId (**automated coverage**)
- [x] Duplicate callbacks idempotent (**automated coverage**)
- [x] Cumulative updates do not create attempts (**automated coverage**)
- [x] Auction transition expires old episodes (**automated coverage**)
- [x] Reference retirement expires episodes (**automated coverage**)
- [x] Disable/re-enable clean lifecycle (**automated coverage**)
- [x] True stale/out-of-order event rejection (**automated coverage**)
- [x] LocalPoc deterministic tie handling (**automated coverage**)
- [x] Individual StateVersion and EventRevision sequences (**automated coverage**)
- [x] History/live overlap dedup (**automated coverage**)
- [x] No-lookahead historical replay where exact events supported (**automated coverage**)

## Phase 1F Acceptance / Re-entry Evidence Measurement — code/test

- [x] EvidenceId stability
- [x] Phase 1E EpisodeMeasurementEvent feed consumption
- [x] UpperBoundary eligibility
- [x] LowerBoundary eligibility
- [x] Centerline not-applicable behavior
- [x] Acceptance observation mapping
- [x] Re-entry observation mapping
- [x] Outside time / volume / trade count
- [x] Descriptive ratios + mathematical safety (no NaN/Infinity; zero denom → unavailable)
- [x] Maximum outside excursion
- [x] Local POC displacement
- [x] Geometric re-entry / re-entry speed / maintained-inside / inside volume-trades
- [x] Revision ownership; duplicate idempotence; lifecycle freeze
- [x] Data Gate independence; MBO independence
- [x] Runtime schema 0.7.0; ACCEPTANCE_REENTRY_EVIDENCE_POLICY_V1; assembly 0.0.6
- [x] GPS/card integration; no Episode mutation; no prohibited wording
- [x] GCAE tests green (458 passed / 0 failed / 0 skipped)
- [x] 0 build errors / 0 warnings
- [x] Source/deployed DLL hash match `1FDEBBB3E4E94497157FF6FA7D621760028AA516497BAB8800D88CF5C9E07249`
- [x] Focused live acceptance gate PASS
- [x] FINAL PASS WITH DOCUMENTED LIVE EVIDENCE-LIFECYCLE COVERAGE LIMITATION — commit/tag

## Phase 1F live acceptance (operator — focused first gate) — PASS

- [x] GCQ6 / Rithmic Live; Primary + Composite READY; References READY; Directional READY; Episodes ON; Evidence ON; Evidence Diagnostics ON; Preview OFF; MBO OFF
- [x] Card: ACCEPTANCE/REENTRY EVIDENCE PARTIAL; POLICY V1; EVIDENCE HISTORY LIVE_ONLY; ACTIVE EVIDENCE SETS 2
- [x] UpperBoundary PreviousPrimaryTpoVah @ 4063.9; ACCEPTANCE OBS UNRESOLVED; REENTRY OBS GEOMETRICREENTRY
- [x] Outside ratios time 0.4242 / volume 0.4479 / trade 0.4404; Local POC displacement −8 ticks
- [x] Episode remained REENTRYDEVELOPING AttemptCount 1 max excursion 29 ticks — no Resolution mutation
- [x] DATA DEGRADED independent; MBO BLOCKED; no prohibited wording
- [x] Source/deployed DLL SHA match

### Automated-only (not manually observed live)

- [x] OutsideAttempt → AcceptanceObservation Early (**automated**)
- [x] Continued outside → Developing (**automated**)
- [x] Outside/inside time and volume segmentation (**automated**)
- [x] Zero denominator → unavailable; no NaN/Infinity (**automated**)
- [x] LowerBoundary symmetry (**automated**)
- [x] First geometric re-entry timestamp / exact ReentrySpeed (**automated**)
- [x] Returned-inside distance / TimeMaintainedInside / inside volume-trades (**automated**)
- [x] Second outside attempt / reattempt; same EvidenceId (**automated**)
- [x] Duplicate event idempotence; StateVersion/EventRevision sequences (**automated**)
- [x] Episode expiry freeze; Primary Auction close; disable/re-enable (**automated**)
- [x] Stale revision rejection; deterministic replay (**automated**)
- [x] Centerline not-applicable; bid/ask variants; invalid epoch/tick/timestamp fail-closed (**automated**)

## Phase 2A Executed Orderflow Raw Feature — code/test (LOCKED)

- [x] Authoritative trade normalization (`TradeStreamAtasMapper.MapNewTrade`)
- [x] OnNewTrade admission; OnNewTrades batch admission
- [x] Callback-overlap deduplication; cumulative not authoritative for totals
- [x] Probe independence; Recorder independence; MBO independence
- [x] Total executed volume; unique trade count
- [x] Ask/Bid/Unknown accounting; Classified Delta; Classified CVD
- [x] Aggressor coverage ratio; ratio mathematical safety
- [x] Current-auction aggregate; per-price raw ledger; Episode raw aggregate
- [x] Identity stability; revision ownership; auction/epoch reset
- [x] Timing raw only; LIVE_ONLY coverage modes
- [x] Runtime schema 0.8.0; EXECUTED_ORDERFLOW_POLICY_V1; assembly 0.0.6
- [x] GPS/card integration; Data Gate independence
- [x] No imbalance/absorption/Trade Facilitation/FAR/AAC/Long-Short wording
- [x] GCAE tests green (469 passed / 0 failed / 0 skipped)
- [x] 0 build errors / 0 warnings
- [x] Source/deployed DLL SHA-256 exact match

## Phase 2A live acceptance (LOCKED — focused core gate PASS)

- [x] GCQ6 / Rithmic Live; prior modules READY; Episodes ON; Evidence ON; Executed Orderflow ON; Orderflow Diagnostics ON; Preview OFF; MBO OFF; Probe/Recorder OFF permitted
- [x] Card: ORDERFLOW PARTIAL; POLICY EXECUTED_ORDERFLOW_POLICY_V1; COVERAGE LIVEONLYMIDAUCTION; HISTORY LIVE_ONLY
- [x] Executed volume 113 / trades 103; Unknown 113; Ask/Bid unavailable; CLASSIFIED DELTA 0 / CVD 0; coverage 0
- [x] EPISODE ORDERFLOW PARTIAL; no absorption/exhaustion/trapped/sweep; DATA DEGRADED independent; MBO BLOCKED
- [x] Final verdict: FINAL PASS WITH DOCUMENTED LIVE RAW-FEATURE COVERAGE LIMITATION
- [x] Tag: `gcae-p2a-executed-orderflow-raw-feature-foundation-pass`

### Phase 2A automated-only (not all live-observed)

- [x] Nonzero Ask/Bid classified paths; complete classification READY; mixed-side reconciliation (**automated**)
- [x] Nonzero Classified Delta/CVD; zero-denominator coverage unavailable (**automated**)
- [x] Full per-price ledger; complete Episode raw aggregates; Centerline Episode input (**automated**)
- [x] Timing min/max/mean/latest; duplicate timing idempotence; out-of-order rejection (**automated**)
- [x] Cumulative revision replacement; auction/epoch/disable–re-enable; LiveOnlyFromAuctionStart (**automated**)
- [x] StateVersion/EventRevision sequences; stale revision rejection; deterministic replay (**automated**)

## Phase 2B Cluster Raw Feature Measurement — code/test (LOCKED)

- [x] Phase 2A remains authoritative; no second trade normalization
- [x] Version-gated rebuild; changed-tick downstream path
- [x] Module default OFF; Awaiting/Ready/Partial/Invalid
- [x] Deterministic auction/level/Episode snapshot identities
- [x] Same-price / diagonal raw ratios; null-safe denominator; no NaN/Infinity
- [x] RawDominantSide Ask/Bid/Equal/Unknown; neighboring dominance; no missing-tick bridging
- [x] EMPIRICAL_MIDRANK_V1; Volume / Trade Count / Absolute Delta ranks
- [x] VisitCount/RevisitCount; auction + Episode aggregation
- [x] Availability policy; Unknown-only Partial truthful
- [x] Revisions/lifecycle; runtime/card; Data Gate independence; MBO independence
- [x] CLUSTER CLASSIFICATION NOT CALIBRATED; no thresholds; no prohibited wording
- [x] Runtime schema 0.9.0; CLUSTER_RAW_FEATURE_POLICY_V1; assembly 0.0.6
- [x] GCAE tests green (482 passed ×2 / 0 failed / 0 skipped); Probe/Recorder 139 green
- [x] 0 build errors / 0 warnings
- [x] Source/deployed DLL SHA-256 exact match

## Phase 2B live acceptance (LOCKED — focused core gate PASS)

- [x] GCQ6 / Rithmic Live; Orderflow ON; Cluster Raw ON; Cluster Diagnostics ON; Probe/Recorder OFF permitted; MBO OFF
- [x] Two-snapshot update: volume 318→324; trades 258→264; levels 5→6; Unknown-only; CLASSIFICATION NOT CALIBRATED
- [x] Ask/Bid ratios unavailable (not zero); Volume Rank 4/5→6/6; Abs Delta rank unavailable; Visits 1/0
- [x] Episode Cluster Raw PARTIAL; HISTORY LIVE_ONLY; COVERAGE LIVEONLYFROMAUCTIONSTART
- [x] No imbalance/stacked/extreme/Big Trade/absorption/EffortResult/Trade Facilitation; DATA DEGRADED independent; MBO BLOCKED
- [x] Final verdict: FINAL PASS WITH DOCUMENTED LIVE CLASSIFIED-CLUSTER COVERAGE LIMITATION
- [x] Tag: `gcae-p2b-cluster-raw-feature-measurement-pass`

### Phase 2B automated-only (not all live-observed)

- [x] Classified Ask/Bid same-price and diagonal ratios; numerator-zero / denom-zero / missing adjacent (**automated**)
- [x] Classified Ask/Bid/Equal RawDominantSide; adjacent runs; Unknown/Equal/gap termination (**automated**)
- [x] Absolute-Delta rank; EMPIRICAL_MIDRANK_V1 ties; percentile bounds; collection-order independence (**automated**)
- [x] Multi-visit/revisit lifecycle; duplicate/revision/stale visit idempotence (**automated**)
- [x] Classified Episode aggregates; max abs-Delta tick; Centerline Episode input (**automated**)
- [x] Auction/epoch/disable–re-enable; StateVersion/EventRevision; stale Phase 2A rejection; replay (**automated**)

## Phase 2C Auction Efficiency Raw Evidence — code/test

- [x] Phase 2A/2B remain authoritative; no second trade/Cluster ledger
- [x] Module default OFF; AwaitingOrderflow / AwaitingEpisode / Ready / Partial / Invalid
- [x] Deterministic AEFF / AEFFEP identities; fingerprint-gated rebuild
- [x] Effort vector copied from 2A/2B; Ask/Bid nullable; Unknown explicit; no fabricated imbalance/Big Trade/MBO
- [x] Result geometry; favorable/adverse by Episode direction; retention; null-safe ratios
- [x] Immutable Profile start anchors; migration exact; Evidence outside ratios / geometric reentry copied
- [x] Raw relationships descriptive only; zero denom → null; no EfficiencyScore / Effective/Ineffective
- [x] Revisions/lifecycle; runtime schema 0.10.0; GPS NOT CALIBRATED; Data Gate independence
- [x] GCAE tests green (497 ×2); Probe/Recorder 183 green; 0 errors / 0 warnings
- [x] Source/deployed DLL SHA-256 exact match
- [ ] Focused live acceptance

## Phase 2C live acceptance (PENDING)

- [ ] GCQ6 / Rithmic Live; Episodes + Evidence + Orderflow + Cluster + Efficiency ON; Efficiency Diagnostics ON; other diagnostics OFF; Probe/Recorder OFF permitted; MBO OFF
- [ ] Card: AUCTION EFFICIENCY READY or PARTIAL; POLICY AUCTION_EFFICIENCY_EVIDENCE_POLICY_V1; HISTORY LIVE_ONLY; CLASSIFICATION NOT CALIBRATED
- [ ] Effort volume/trades positive; Classified Delta value or unavailable; Result net/range finite; favorable/adverse/retained finite or unavailable
- [ ] Progress per contract/trade finite or unavailable; no Effective/Ineffective/Absorption/Trade Facilitation wording
- [ ] DATA DEGRADED independent; MBO BLOCKED; two screenshots a few seconds apart when practical
