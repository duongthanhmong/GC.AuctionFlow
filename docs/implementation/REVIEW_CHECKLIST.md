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
- [x] Source-scope: Phase 1E not started
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
