# Known Limitations

## P0-07B Raw Event Recorder

1. P0-07B has no ATAS callback adapters; live capture wiring is P0-07C.
2. Cumulative tick constituents are not recorded (`CumulativeTickConstituentsRecorded = false`).
3. Provider snapshot completion remains unknown (`ProviderSnapshotCompletionKnown = false`).
4. Writer-global sequence is process-local dequeue order only — not exchange causality.
5. Internal accounting reconciliation is not exchange-feed completeness.
6. MBO payload schema exists but primary-process recording is blocked (P0-06D lock).
7. Segments are authoritative evidence; manifest is a recoverable index only.
8. Footer `BytesBeforeFooter` excludes the footer frame itself; final `.seg` length is `CompletedSegmentInfo.ByteLength` / `BytesWritten`.
9. Periodic mid-segment `Flush(true)` during long open segments is not yet performed (completion flush only); subject to sensitivity test in P0-07C.
10. Shutdown-timeout undrained counting does not reclaim an in-flight draft already dequeued by a blocked worker (counted via later discard/fault paths).

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
