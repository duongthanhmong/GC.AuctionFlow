# Review Checklist — Phase 1B Composite Profile Foundation

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
- [x] Runtime snapshot 0.3.0 Composite fields; atomic publication
- [x] GPS composite rows (AwaitingAnchor / Ready / Partial / Evidence)
- [x] Optional Confirmed vs Preview overlay labels (no S/R / Entry language)
- [x] Bounded transition logging
- [x] Composite Ready does not force global Data Ready
- [x] Disabled composite does not degrade Primary Profile
- [x] Publish-path init when Current missing
- [x] Operator-configuration fingerprint rebuilds on settings change (not every trade)

## Explicitly deferred

- [x] No Structural Reference Lifecycle / nPOC / HVN-LVN intelligence
- [x] No Directional Auction Context / One-Time Framing / Episode
- [x] No Acceptance/Re-entry / FAR/AAC / Entry/Invalidation/Targets
- [x] No Orderflow / DOM / MBO / Telegram / Adaptive TPO
- [x] No automatic Production merge/close thresholds

## Live acceptance (operator anh Fen — GCQ6 / Rithmic Live)

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

## Verify (closeout)

- [x] Release restore/build/test green (329 passed, 0 warnings)
- [x] Phase 1A + Trade Recorder regressions green
- [x] MBO remains off/blocked
- [x] Master specification untouched
- [x] Selective Phase 1B commit + tag (no OAC / no solution contamination)
- [x] No push (no upstream configured)
