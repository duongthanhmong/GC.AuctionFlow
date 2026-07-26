# Known Limitations

## P0-07B / P0-07C2 Raw Event Recorder

1. P0-07C2 has no ATAS callback adapters and no indicator recorder settings; Trade/DOM wiring is P0-07C3/C4.
2. Cumulative tick constituents are not recorded (`CumulativeTickConstituentsRecorded = false`).
3. Provider snapshot completion remains unknown (`ProviderSnapshotCompletionKnown = false`).
4. Writer-global sequence is process-local dequeue order only — not exchange causality.
5. Internal accounting reconciliation is not exchange-feed completeness.
6. MBO payload schema exists but primary-process recording is blocked (P0-06D lock).
7. Segments are authoritative evidence; manifest is a recoverable index only.
8. Footer `BytesBeforeFooter` excludes the footer frame itself; final `.seg` length is `CompletedSegmentInfo.ByteLength` / `BytesWritten`.
9. Periodic mid-segment `Flush(true)` during long open segments is not yet performed (completion flush only); subject to sensitivity test in later operator runs.
10. Shutdown-timeout undrained counting does not reclaim an in-flight draft already dequeued by a blocked worker (counted via later discard/fault paths).
11. **CallbackInvocationResultPayload** absence means callback completion is unknown; it is local metadata, not a market event.
12. **OnBestBidAskChanged** dual-sided payload shape remains **UNKNOWN**; no BestBidAsk recorder mapping in P0-07C2 (defer P0-07C4A).
13. Schema **1.0.0** is unsupported for live trust; use **1.2.0+**.
14. **Record-count taxonomy (P0-07C3BC):** footer/manifest carry MarketEventRecordCount, InvocationResultRecordCount, LifecycleIntegrityRecordCount; RawEventRecordCount/RecordCount remain the compatible total.
15. **Invocation-result queue policy (wired in C3BC):** same recorder queue as market drafts; separate InvocationResult* accounting; not counted as NormalizedObservations.
16. CallbackInvocationSequence exhausted at `long.MaxValue` throws (no wrap). CallbackItemOrdinal exhausted at `int.MaxValue` stops enumeration with `CallbackItemOrdinalExhausted`.
17. Recorder cumulative path never reads `CumulativeTrade.Ticks`; ReportedTickCountAvailable=false and ReportedTickCount=null (never zero-for-unknown).
18. DOM / BestBidAsk / MBO recorder integration deferred (P0-07C4+). P0-07C3D live Trade verification **PASS** — session `01f6650494194d3bacbe00062dede326`; see `docs/evidence/P0-07C3D_GCQ6_Rithmic_TradeRecorder_LiveVerification.md`.

## P0-06 / P0-06B / P0-06C / P0-06D

8. No MBO-specific unsubscribe; ProviderUnsubscribePerformed=false.
9. Raw enums ≠ exchange lifecycle; interpreted action Unknown.
10. captureSubscriptionEpoch stamps obs; finalClosedEpoch is post-stop only.
11. duplicateSubscribeSuppressed is genuine races only; SubscribeTriggerCheckCount is separate.
12. Initial time window 5s from FirstCallbackReceiveUtc (seed).
13. MboOrderObservationState capacity 65536 (seed); saturation without eviction.
14. **P0-06C Decision B:** no GCAE MBO→chart DataSeries write found.
15. **P0-06D:** Chart A/B showed abnormal bar on **both** GCAE and non-GCAE charts during fresh MBO snapshot — shared platform/provider interaction strongly supported; exact mechanism Unknown. Do not claim MBO→trade/candle conversion.
16. **Operational lock:** MBO must not run in the ATAS process used for primary GC analysis or trading; same-process separate chart is not proven isolation.
17. Historical/Replay MBO Unknown; Queue 32768 seed.

## P0-05 / P0-05B DOM (retained)

18. LiveDom Available with Partial fidelity — not fully Validated.
19. VolumeMeaning / ZeroVolumeMeaning Unknown; no stable DOM book.
20. Singular MarketDepthChanged NOT_OBSERVED_IN_TEST_WINDOW on GCQ6/Rithmic.

## P0-04 trade (retained)

21. Trade fingerprints diagnostic only; continuity ≠ exchange-feed completeness.
22. cumulativeNewObservationCount is observation count, not unique exchange executions.

## P0-08A Runtime Data Gate / Auction GPS Card

23. ~~Profile / TPO / Volume Profile not implemented~~ � **superseded by Phase 1A PASS** (Primary Intraday Classic TPO + VP present).
24. RollState remains **Unknown** without next-contract volume or external roll-calendar evidence; ActiveRoll is never inferred in this slice.
25. Bid/Ask classification is **Unknown** (not validated fidelity); DOM capability shown Unavailable; MBO remains **Blocked** / isolated-environment-only.
26. Live Trade Recorder success is not promoted to exchange-feed completeness, historical fidelity, replay fidelity, or validated Bid/Ask/DOM fidelity.
27. Auction GPS Card reserved rows (Structural/Tactical/Location/Episode/Thesis) are NOT AVAILABLE and hidden unless ShowAuctionGpsDiagnostics is enabled.
28. No Production Thesis, FAR/AAC, Entry/Target, CFD mapping, Telegram, or trading/order execution in P0-08A.
29. OnRender overlay uses OFT.Rendering; Exact Final vs LatestBar draw cadence is operator-confirmed on live ATAS (C?N X�C MINH TR�N ATAS TH?T for residual draw-cadence nuances).

## Phase 1A Primary Intraday Profile

30. ~~Unspecified Kind treated as America/New_York~~ � **superseded by D-P1A-002 / `ATAS_CANDLE_TIME_UTC_V1`** (LiveObserved UTC wall-clock).
31. Volume Profile Unavailable when GetAllPriceLevels is empty/fails ? ProfileState Partial; TPO may still be Ready.
32. ValueAreaFraction default 0.70 is a conventional configurable method default, not a calibrated GC edge or predictive threshold.
33. ~~Only current + immediately previous primary auctions~~ � **superseded in part by Phase 1B**: Primary display remains current/previous; completed-auction list feeds Composite ledger only when Composite enabled.
34. Overlay lines are informational price levels only � not Structural References, support/resistance claims, or Entry/Target semantics.
35. Global DataState may remain Degraded due BidAsk Unknown and Roll Unknown even when PROFILE READY or COMPOSITE READY.
36. **ATAS TPO methodology difference (Phase 1A closeout):** ATAS built-in TPO POC did not match GCAE Classic TPO POC in the observed live comparison. GCAE timestamp normalization, bar-ledger accounting, deterministic TPO distribution and independent oracle were verified. ATAS proprietary methodology or exact benchmark settings remain unconfirmed. No GCAE algorithm was changed to force parity.
37. TPO parity / forensic diagnostics (reference price, period coverage, ledger audit) are **disabled by default**, diagnostic-only, and must not influence TPO, POC, Value Area, or Production analysis.
38. Unknown timestamp semantics still refuse silent conversion. Candle Time = bar start, LastTime = bar end (ATAS API names).
39. Historical chart-add uses deferred profile rebuild (ingest-only until current bar) to avoid O(n�) load; final distribution equals eager rebuild.

## Phase 1B Composite Profile Foundation (LOCKED FINAL PASS)

40. **No hard N-day Production merge.** Confirmed composite requires OperatorAnchored + explicit anchor; missing anchor → AWAITING ANCHOR (no silent choice).
41. Merge/close thresholds (value-overlap, POC displacement, outside acceptance, etc.) are **uncalibrated**. ShadowEvidence defaults OFF; unset thresholds → NotCalibrated / NOT CALIBRATED / NOT EVALUATED. Shadow never mutates confirmed composite.
42. Developing composite preview is optional and separate; it must not alter confirmed CompositeId or membership.
43. Composite Partial when any included contribution lacks exact volume-by-price; VPOC/Volume VA may be unavailable — never infer Bid/Ask from totals.
44. History gaps between included local auction dates are reported; research history is not persisted in this phase.
45. StableBalance/Breaking/NewValue Production classification, Episode, FAR/AAC, Thesis remain **not implemented** (Phase 1D+). Phase 1C Structural References are profile-derived foundation only (see Phase 1C section).
46. ATAS built-in multi-day profile remains a **benchmark only**; methodology equality is not required for Phase 1B acceptance.
47. `EnableCompositeProfile` defaults **false**; enabling Composite must not redesign locked Phase 1A Primary engines or Trade Recorder.
48. **Composite Ready does not clear global DATA DEGRADED.** Bid/Ask Unknown/Partial (and Roll Unknown) may keep DataState Degraded while COMPOSITE READY is valid.
49. **MBO remains blocked** in the primary ATAS process (P0-06D operational lock).
50. Anchor supplied but not loaded may still use AWAITING / Building status wording under current semantics; requested id must remain visible in diagnostics when supplied.
51. Operator-configuration fingerprint rebuilds Composite on settings change only — ordinary trade/GPS publishes must not rebuild when fingerprint is unchanged.
52. **OAC / non-GCAE tree contamination** (solution OAC projects, `src/Oac.*`, OAC docs) remains **outside GCAE Phase 1B scope** and is not part of this lock.
53. Live-accepted closeout DLL SHA-256: `CA2157509B140D0752FB4FCEF70E1FCD863553053D6E133566B848BBDDB02B87`. Tag: `gcae-p1b-composite-profile-foundation-pass`.

## Phase 1C Structural Reference Foundation (LOCKED — FINAL PASS WITH DOCUMENTED LIVE COVERAGE LIMITATION)

54. **Profile-derived references only.** No weekly/monthly/IB/VWAP/swing/launch base/single prints/HVN/LVN/nPOC/poor high-low/origin/event/DOM-derived levels in Production.
55. **No approach/interacting/acceptance lifecycle** at runtime. Reserved enum members must not be emitted. TestCount = 0 / NotEvaluated; ReactionHistory / ExecutedActivity = NotEvaluated.
56. **Exact-tick confluence only** — no tolerance band, ATR buffer, percentage width, or fuzzy approach distance.
57. **No hierarchy score / probability / edge / expectancy** claims. Component metadata only.
58. Developing Composite Preview does **not** contribute Structural References.
59. **Reference Ready does not clear global DATA DEGRADED.** Bid/Ask may remain Unknown/Partial.
60. MBO remains blocked; OI is not a Production Core dependency.
61. Single-price references use `ZoneLow == ZoneHigh` on the instrument tick grid.
62. ~~**No Phase 1D behavior**~~ — **superseded by Phase 1D Directional Context (descriptive only; still no Episode/FAR/AAC/Thesis/Entry)**.
63. OAC contamination remains outside GCAE Phase 1C/1D scope.
64. **Overlay readability (`REFERENCE_OVERLAY_POLICY_V1`):** while Structural Reference Overlay is ON, Primary/Composite visual levels are suppressed to avoid duplicate constituent labels; dense labels may be collision-suppressed while lines remain. Engine registry membership is unchanged.
65. **Developing StateVersion:** registry-owned monotonic revision. Reconcile drafts use `RegistryAssignedStateVersion = 0`. Authoritative lower external revisions still fail closed (`STALE_REVISION`).
66. **Documented live coverage limitation:** complete module disable, remove/add after final revision fix, live MIXED confluence display, and per-reference StateVersion visuals were not separately repeated as final live manual gates (operator ended further toggle testing). Covered by automated tests; does not alter Reference semantics.
67. Live-accepted closeout DLL SHA-256: `9CAE06B9091D23854DA60A425E45884D2CC0D5C50E501A4E9EB1B12A58FAF3CA`. Tag: `gcae-p1c-structural-reference-foundation-pass`.

## Phase 1D Multi-Horizon Directional Context Foundation (LOCKED — FINAL PASS WITH DOCUMENTED LIVE DIAGNOSTIC COVERAGE LIMITATION)

68. Limited to Primary/Composite profile evidence (+ optional Reference snapshot identity). No weekly/monthly directional horizon.
69. No Acceptance/Episode inputs. No executed Orderflow inputs.
70. Execution Context Unavailable / NotAvailable in Phase 1D.
71. OTF confirmation policy **not calibrated** — ConfirmedUp/ConfirmedDown never emitted; `OTF_CONFIRMATION_NOT_CALIBRATED`.
72. No Thin Participation / Settlement tags yet.
73. No Day Structure Production classifier.
74. No score, probability, Long/Short signal, or automatic execution.
75. Tactical context may revise descriptively during the current Developing auction; must not rewrite Structural completed context.
76. Directional Ready does not clear global DATA DEGRADED; module default OFF.
77. Completed TPO period feed is read-only from ClassicTpoEngine — TPO math / 30m / 08:20 ET anchor unchanged.
78. **Documented live diagnostic coverage limitation:** expanded diagnostic fields (auction IDs, transition count, component migrations, OTF streaks/counts, StateVersions, fingerprint) were not manually observed live; covered by automated tests. Core READY gate was live-verified.
79. MBO remains blocked in the primary ATAS process.
80. OAC contamination remains outside GCAE Phase 1D scope.
81. ~~**No Phase 1E behavior**~~ — **superseded by Phase 1E Auction Episode Observation** (geometric only; still no Acceptance / Orderflow / FAR/AAC / Thesis / Entry / Risk).
82. Live-accepted closeout DLL SHA-256: `9976E848578B9057503EC0D8A866C1593CED189EDC5EC04563940F605F0C6AFB`. Tag: `gcae-p1d-multi-horizon-directional-context-pass`.

## Phase 1E Auction Episode Observation Foundation (LOCKED — FINAL PASS WITH DOCUMENTED LIVE STATE-MACHINE COVERAGE LIMITATION)

83. **Confirmed references only.** Developing Current Primary, Developing Composite Preview, and moving Current Primary POC/VA/H/L do not create episodes (`DEVELOPING_REFERENCE_EPISODES_NOT_AUTHORIZED`).
84. **No approach state emission.** ApproachingReference reserved; `APPROACH_DISTANCE_NOT_CALIBRATED`.
85. **No intra-auction calibrated reset.** `INTRA_AUCTION_EPISODE_RESET_NOT_CALIBRATED` — same auction/reference remains one episode across re-entries.
86. **No Acceptance / stable re-entry Resolution** (Phase 1F). Emitted resolutions: None / Expired / InvalidData only.
87. **No FAR/AAC**, Orderflow interpretation, Long/Short, SweepDetector, thesis, entry/target/probability.
88. **No Episode chart overlay or ATAS alerts** in Phase 1E.
89. **Historical reconstruction is LIVE_ONLY** (`EPISODE_HISTORY: LIVE_ONLY`) — candle OHLC is never used to fabricate pre-start interactions.
90. Bid/Ask / delta fields may be **unavailable** (Partial) without invalidating distance/time/volume/trade-count geometry; never fabricate zero aggressor evidence.
91. **Local Value** (LocalValueLow/High) may remain unavailable (`EPISODE_LOCAL_VALUE_POLICY_NOT_AUTHORIZED`); LocalPoc when exact volume is measurement only — not acceptance.
92. **Centerline has no canonical inside/outside semantics** — CrossCount and side excursions only; AttemptCount remains 0; never OutsideAttempt/ReentryDeveloping.
93. **No MBO/DOM/OI dependency** for Episode creation; MBO remains blocked in the primary process.
94. **DATA DEGRADED remains independent** of Episode Ready/Partial. No edge/probability claim.
95. OAC contamination remains outside GCAE Phase 1E scope.
96. **Live AWAITING TRADES wiring defect (fixed, D-P1E-002):** Episode admission must not depend on Trade Stream Probe enablement.
97. **Documented live state-machine coverage limitation:** repeated-attempt / geometric re-entry / auction expiry / reference retirement / disable-reenable / stale ordering / exact revision sequences / history-live overlap / LocalPoc ties were not all manually observed live; covered by automated tests. Core live gate (PARTIAL, natural ACTIVE episodes, Centerline) was live-verified.
98. ~~**No Phase 1F behavior**~~ — **superseded by Phase 1F Acceptance / Re-entry Evidence Measurement** (measurement only; still no resolution conclusions / FAR/AAC / Thesis / Entry / Risk).
99. Live-accepted closeout DLL SHA-256: `924DB65C4D719D831926C81392AF600A332CD6BFF81401C5B6FC9E30CDFACBC2`. Tag: `gcae-p1e-auction-episode-observation-pass`.

## Phase 1F Acceptance / Re-entry Evidence Measurement Foundation (LOCKED — FINAL PASS WITH DOCUMENTED LIVE EVIDENCE-LIFECYCLE COVERAGE LIMITATION)

100. **Measurement ≠ resolution.** No Established/Failed Acceptance; no StableReaccepted/ReentryFailed.
101. **LIVE_ONLY** evidence history inherited from Episode — no candle OHLC backfill / reconstruction.
102. **Boundary episodes only** for canonical Acceptance Outside / Re-entry geometry.
103. **Centerline** marked `CENTERLINE_ACCEPTANCE_GEOMETRY_NOT_APPLICABLE` — no canonical outside ratios or reentry conclusions.
104. **No OutsideCloseRatio / TPO outside count / Local Value rebuild / OldValueReclaimFailure / RetestHoldQuality** unless exact feed/policy later authorized.
105. **No opposite-aggression effectiveness** / Orderflow interpretation (`OPPOSITE_AGGRESSION_INTERPRETATION_NOT_AUTHORIZED`).
106. Bid/Ask / delta may be **unavailable** (Partial) — never fabricate zero aggressor evidence.
107. **No FAR/AAC**, Thesis, Entry/Risk, Long/Short, probability/confidence, alerts, or Phase 1F chart overlay.
108. **No invented** acceptance / maintenance / stable-reentry / reclaim / retest thresholds.
109. Phase 1F does **not mutate** Episode State, Resolution, or AttemptCount.
110. Module Ready/Partial does **not clear** global DATA DEGRADED. MBO/DOM/OI not required; MBO remains blocked.
111. OAC contamination remains outside GCAE Phase 1F scope.
112. ~~**No Phase 2 Executed Orderflow**~~ — **superseded by Phase 2A raw feature foundation** (still no interpretation / Trade Facilitation / Resolution / FAR/AAC).
113. **Documented live evidence-lifecycle coverage limitation:** full outside/inside segmentation, LowerBoundary symmetry, zero-denominator ratios, repeated attempts, exact revision sequences, lifecycle freeze/reset, Centerline not-applicable, bid/ask variants, compatibility fail-closed, and deterministic replay were not all manually observed live; covered by automated tests. Core live gate (PARTIAL UpperBoundary evidence, finite ratios, geometric re-entry, Local POC displacement, observer-only) was live-verified.
114. Live-accepted closeout DLL SHA-256: `1FDEBBB3E4E94497157FF6FA7D621760028AA516497BAB8800D88CF5C9E07249`. Tag: `gcae-p1f-acceptance-reentry-evidence-measurement-pass`.
115. Acceptance/Re-entry Resolution, FAR/AAC remain **NOT STARTED**. Phase 2A raw Orderflow is separate (see Phase 2A section).

## Phase 2A Executed Orderflow Raw Feature Foundation (LOCKED — FINAL PASS WITH DOCUMENTED LIVE RAW-FEATURE COVERAGE LIMITATION)

116. **Raw ≠ interpretation.** No imbalance / stacked imbalance / Big Trade / extreme Delta/Volume / tape-speed labels.
117. **LIVE_ONLY** history when exact chart-load prints unavailable; no candle or ATAS visual reconstruction. Current live scope began **LIVEONLYMIDAUCTION**.
118. Bid/Ask classification may be **Partial/Unavailable**; Unknown aggressor volume remains explicit; Ask/Bid are never fabricated.
119. Classified Delta/CVD cover the classified subset only — never invent tick-rule aggressor. CVD is not position data; no OI position inference.
120. Cumulative callbacks are **not** authoritative for executed totals (`CUMULATIVE_CALLBACKS_NOT_AUTHORITATIVE_FOR_EXECUTED_TOTALS`).
121. **No absorption/exhaustion/Effort vs Result/Trade Facilitation.**
122. **No Resolution/FAR/AAC**, Thesis, Long/Short, probability/confidence, alerts, or Orderflow overlay.
123. No MBO/DOM/OI dependency; MBO remains blocked. Module Ready/Partial does **not clear** global DATA DEGRADED.
124. OAC contamination remains outside GCAE Phase 2A scope.
125. **Documented live raw-feature coverage limitation:** Ask/Bid classified paths, complete READY, mixed-side reconciliation, nonzero Delta/CVD, full per-price ledger, complete Episode aggregates, timing metrics, cumulative revision replacement, auction/epoch/disable–re-enable resets, LiveOnlyFromAuctionStart, revision sequences, out-of-order rejection, and deterministic replay were not all manually observed live; covered by automated tests. Core live gate (PARTIAL MidAuction Unknown-only accounting, volume/trades, coverage safety, Probe independence) was live-verified.
126. Live-accepted closeout DLL SHA-256: `F92538852AD2478002F6FCB89B052FC9ACBD3773746F733C31548BF77B5356D2`. Tag: `gcae-p2a-executed-orderflow-raw-feature-foundation-pass`.
127. Effort vs Result, Trade Facilitation, Acceptance/Re-entry Resolution, FAR/AAC remain **NOT STARTED**. Phase 2B Cluster Raw is separate (see Phase 2B section).

## Phase 2B Cluster Raw Feature Measurement Foundation (LOCKED — FINAL PASS WITH DOCUMENTED LIVE CLASSIFIED-CLUSTER COVERAGE LIMITATION)

128. **Raw measurements only.** No Bid/Ask Imbalance, Stacked Imbalance, Extreme Delta/Volume, Big Trade, or Repeated Extreme Tests classification.
129. Same-price / diagonal ratios are descriptive; unavailable Ask/Bid / opposing tick / zero denom → unavailable (never fabricated zero). Absolute-Delta rank may be unavailable.
130. RawDominantSide and consecutive dominance are not imbalance / stacked-imbalance / buyer-seller-control labels.
131. EMPIRICAL_MIDRANK_V1 ranks/percentiles are descriptive — not Extreme labels or probabilities; no percentile threshold.
132. Visits/revisits are not Repeated Extreme Tests; no time-gap threshold.
133. **CLOSE_POSITION_INPUT_UNAVAILABLE** — no Close Position without exact authorized bar-cluster input.
134. Unknown-only Phase 2A levels remain valid Cluster Raw **Partial**; CLUSTER CLASSIFICATION **NOT CALIBRATED**.
135. LIVE_ONLY history; no candle / chart-color / ATAS footprint / Cluster Search reconstruction. Coverage inherited from Phase 2A (live: LIVEONLYFROMAUCTIONSTART).
136. **No Absorption/Exhaustion / Effort vs Result / Trade Facilitation / Resolution / FAR / AAC**, Thesis, Long/Short, probability/edge, alerts, or cluster overlay.
137. Module Ready/Partial does **not clear** global DATA DEGRADED. MBO/DOM/OI not required; MBO remains blocked.
138. OAC contamination remains outside GCAE Phase 2B scope.
139. **Documented live classified-cluster coverage limitation:** classified Ask/Bid ratios, diagonal classified paths, classified dominant sides/runs, absolute-Delta rank, percentile ties, multi-visit lifecycle, classified Episode aggregates, Centerline, auction/epoch/disable resets, revision sequences, stale rejection, and deterministic replay were not all manually observed live; covered by automated tests. Core live gate (two-snapshot Unknown-only Partial update, Volume Rank, NOT CALIBRATED) was live-verified.
140. Live-accepted closeout DLL SHA-256: `A15CC6A85AA9562E59CA8B66140AAD017E957F96E96C7BBF47E620E6E0E71A39`. Tag: `gcae-p2b-cluster-raw-feature-measurement-pass`.
141. Imbalance / Stacked Imbalance / Extreme Delta/Volume / Big Trade / Effort vs Result / Trade Facilitation / Acceptance/Re-entry Resolution / FAR / AAC remain **NOT STARTED**.

## Phase 2C Auction Efficiency Raw Evidence Measurement Foundation (CODE/TEST PASS — LIVE ACCEPTANCE PENDING)

142. **Raw Effort and Result only.** No EffortResultBalanced, AggressionEffective/Ineffective, Absorption, Exhaustion, or Trade Facilitation.
143. No one-dimensional efficiency formula / EfficiencyScore; no threshold or weighting policy.
144. Ask/Bid may remain unavailable; Classified Delta may cover classified subset only; Absolute classified Delta may be unavailable for Unknown-only.
145. Imbalance / Big Trade / MBO Sweep / Stop / Iceberg effort components remain unavailable (research-only limitations; never substitute zero).
146. Start anchors begin within LIVE_ONLY observation scope; developing Profile revisions must not rewrite published start anchors.
147. Profile / Acceptance Evidence components may be Partial/unavailable.
148. No candle reconstruction; no overlay/alerts; no Resolution/FAR/AAC; no Long/Short.
149. Module Ready/Partial does **not clear** global DATA DEGRADED. MBO remains blocked.
150. OAC contamination remains outside GCAE Phase 2C scope.
151. EFFICIENCY CLASSIFICATION **NOT CALIBRATED** — do not use a fake Neutral state.
152. Live acceptance pending; commit/tag HOLD.
153. Effort vs Result classifier / Trade Facilitation / Absorption/Exhaustion / Resolution/FAR/AAC remain **NOT STARTED**.
