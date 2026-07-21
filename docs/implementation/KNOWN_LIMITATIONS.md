# Known Limitations

## P0-05 / P0-05B DOM semantics (post operator PASS)

1. No explicit depth action / level index / native sequence / Indicator reset marker.
2. VolumeMeaning = Unknown; ZeroVolumeMeaning = Unknown; zero volume must not be classified as Delete.
3. UpdateAction remains Unknown; StableBookReconstruction = false.
4. LiveDom Availability = Available with Partial fidelity — **not** fully Validated. HistoricalDom / ReplayDom remain Unknown.
5. NativeSequence = Absent; no exchange-level ordering guarantee.
6. Source timestamps were not monotonic; all observed source DateTime.Kind = Unspecified.
7. DOM callbacks delivered across many managed threads.
8. Snapshot pull non-empty in operator runs, but snapshot completion and production-book suitability remain Unknown.
9. Snapshot coverage is provider-defined and very broad (Bid min 10.0 / Ask max 5826.1 observed); Smart DOM visible depth ≠ API snapshot depth.
10. Controlled reconnect not tested; reconnect behavior Unknown. No native reset marker observed.
11. Internal capture continuity does not establish exchange-feed completeness.
12. Queue capacity 16384 is a seed value (subject to sensitivity test); bursts may drop.
13. Singular `MarketDepthChanged` was NOT_OBSERVED_IN_TEST_WINDOW on GCQ6/Rithmic (batch path observed).

## P0-04 trade (retained)

14. Trade fingerprints diagnostic only; continuity ≠ exchange-feed completeness.
15. cumulativeNewObservationCount is observation count, not unique exchange executions.
