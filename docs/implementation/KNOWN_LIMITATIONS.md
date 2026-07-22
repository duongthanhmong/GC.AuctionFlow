# Known Limitations

## P0-06 / P0-06B / P0-06C / P0-06D

1. No MBO-specific unsubscribe; ProviderUnsubscribePerformed=false.
2. Raw enums ≠ exchange lifecycle; interpreted action Unknown.
3. captureSubscriptionEpoch stamps obs; finalClosedEpoch is post-stop only.
4. duplicateSubscribeSuppressed is genuine races only; SubscribeTriggerCheckCount is separate.
5. Initial time window 5s from FirstCallbackReceiveUtc (seed).
6. MboOrderObservationState capacity 65536 (seed); saturation without eviction.
7. **P0-06C Decision B:** no GCAE MBO→chart DataSeries write found.
8. **P0-06D:** Chart A/B showed abnormal bar on **both** GCAE and non-GCAE charts during fresh MBO snapshot — shared platform/provider interaction strongly supported; exact mechanism Unknown. Do not claim MBO→trade/candle conversion.
9. **Operational lock:** MBO must not run in the ATAS process used for primary GC analysis or trading; same-process separate chart is not proven isolation.
10. Historical/Replay MBO Unknown; Queue 32768 seed.

## P0-05 / P0-05B DOM (retained)

11. LiveDom Available with Partial fidelity — not fully Validated.
12. VolumeMeaning / ZeroVolumeMeaning Unknown; no stable DOM book.
13. Singular MarketDepthChanged NOT_OBSERVED_IN_TEST_WINDOW on GCQ6/Rithmic.

## P0-04 trade (retained)

14. Trade fingerprints diagnostic only; continuity ≠ exchange-feed completeness.
15. cumulativeNewObservationCount is observation count, not unique exchange executions.
