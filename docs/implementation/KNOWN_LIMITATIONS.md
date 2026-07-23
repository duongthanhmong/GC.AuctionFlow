# Known Limitations

## P0-07B / P0-07C2 Raw Event Recorder

1. P0-07C2 has no ATAS callback adapters and no indicator recorder settings; Trade/DOM wiring is P0-07C3/C4.
2. Cumulative tick constituents are not recorded (`CumulativeTickConstituentsRecorded = false`).
3. Provider snapshot completion remains unknown (`ProviderSnapshotCompletionKnown = false`).
4. Writer-global sequence is process-local dequeue order only â€” not exchange causality.
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
18. DOM / BestBidAsk / MBO recorder integration deferred (P0-07C4+). P0-07C3D live Trade verification **PASS** â€” session `01f6650494194d3bacbe00062dede326`; see `docs/evidence/P0-07C3D_GCQ6_Rithmic_TradeRecorder_LiveVerification.md`.

## P0-06 / P0-06B / P0-06C / P0-06D

8. No MBO-specific unsubscribe; ProviderUnsubscribePerformed=false.
9. Raw enums â‰  exchange lifecycle; interpreted action Unknown.
10. captureSubscriptionEpoch stamps obs; finalClosedEpoch is post-stop only.
11. duplicateSubscribeSuppressed is genuine races only; SubscribeTriggerCheckCount is separate.
12. Initial time window 5s from FirstCallbackReceiveUtc (seed).
13. MboOrderObservationState capacity 65536 (seed); saturation without eviction.
14. **P0-06C Decision B:** no GCAE MBOâ†’chart DataSeries write found.
15. **P0-06D:** Chart A/B showed abnormal bar on **both** GCAE and non-GCAE charts during fresh MBO snapshot â€” shared platform/provider interaction strongly supported; exact mechanism Unknown. Do not claim MBOâ†’trade/candle conversion.
16. **Operational lock:** MBO must not run in the ATAS process used for primary GC analysis or trading; same-process separate chart is not proven isolation.
17. Historical/Replay MBO Unknown; Queue 32768 seed.

## P0-05 / P0-05B DOM (retained)

18. LiveDom Available with Partial fidelity â€” not fully Validated.
19. VolumeMeaning / ZeroVolumeMeaning Unknown; no stable DOM book.
20. Singular MarketDepthChanged NOT_OBSERVED_IN_TEST_WINDOW on GCQ6/Rithmic.

## P0-04 trade (retained)

21. Trade fingerprints diagnostic only; continuity â‰  exchange-feed completeness.
22. cumulativeNewObservationCount is observation count, not unique exchange executions.

## P0-08A Runtime Data Gate / Auction GPS Card

23. ~~Profile / TPO / Volume Profile not implemented~~ — **superseded by Phase 1A PASS** (Primary Intraday Classic TPO + VP present; Composite still deferred).
24. RollState remains **Unknown** without next-contract volume or external roll-calendar evidence; ActiveRoll is never inferred in this slice.
25. Bid/Ask classification is **Unknown** (not validated fidelity); DOM capability shown Unavailable; MBO remains **Blocked** / isolated-environment-only.
26. Live Trade Recorder success is not promoted to exchange-feed completeness, historical fidelity, replay fidelity, or validated Bid/Ask/DOM fidelity.
27. Auction GPS Card reserved rows (Structural/Tactical/Location/Episode/Thesis) are NOT AVAILABLE and hidden unless ShowAuctionGpsDiagnostics is enabled.
28. No Production Thesis, FAR/AAC, Entry/Target, CFD mapping, Telegram, or trading/order execution in P0-08A.
29. OnRender overlay uses OFT.Rendering; Exact Final vs LatestBar draw cadence is operator-confirmed on live ATAS (C?N XÁC MINH TRÊN ATAS TH?T for residual draw-cadence nuances).

## Phase 1A Primary Intraday Profile

30. ~~Unspecified Kind treated as America/New_York~~ — **superseded by D-P1A-002 / `ATAS_CANDLE_TIME_UTC_V1`** (LiveObserved UTC wall-clock).
31. Volume Profile Unavailable when GetAllPriceLevels is empty/fails ? ProfileState Partial; TPO may still be Ready.
32. ValueAreaFraction default 0.70 is a conventional configurable method default, not a calibrated GC edge or predictive threshold.
33. Only current + immediately previous primary auctions are retained in runtime; no Composite / weekly / monthly profiles.
34. Overlay lines are informational price levels only — not Structural References, support/resistance claims, or Entry/Target semantics.
35. Global DataState may remain Degraded due BidAsk Unknown and Roll Unknown even when PROFILE READY.
36. **ATAS TPO methodology difference (Phase 1A closeout):** ATAS built-in TPO POC did not match GCAE Classic TPO POC in the observed live comparison. GCAE timestamp normalization, bar-ledger accounting, deterministic TPO distribution and independent oracle were verified. ATAS proprietary methodology or exact benchmark settings remain unconfirmed. No GCAE algorithm was changed to force parity.
37. TPO parity / forensic diagnostics (reference price, period coverage, ledger audit) are **disabled by default**, diagnostic-only, and must not influence TPO, POC, Value Area, or Production analysis.
38. Unknown timestamp semantics still refuse silent conversion. Candle Time = bar start, LastTime = bar end (ATAS API names).
39. Historical chart-add uses deferred profile rebuild (ingest-only until current bar) to avoid O(n²) load; final distribution equals eager rebuild.
