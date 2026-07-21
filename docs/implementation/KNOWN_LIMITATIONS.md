# Known Limitations

## P0-04 / P0-04B Trade Stream Probe

1. Callback threading remains **Unknown** until operator measurement on ATAS/GC.
2. Clock Kind / exchange-clock semantics remain **Unknown**.
3. No trade stream is authoritative; volumes are not merged; no total-executed-volume field.
4. Fingerprints are diagnostic only — not trade IDs, not native sequence, never used for deletion.
5. Process-local cumulative instance IDs are valid only within one indicator instance and are not exchange IDs.
6. Internal capture continuity does not establish exchange-feed completeness.
7. Bounded queue/samples may drop observations (`QueueFullDrops`); not a completeness proof.
8. No GC runtime capability is claimed before operator verification on GC/Rithmic.
9. Zero callback count = `NOT_OBSERVED_IN_TEST_WINDOW` (not auto-Unavailable). All four callbacks are not required.
10. P0-04B: `OnNewTrades` does **not** call `base.OnNewTrades` (avoids synthetic singular callbacks).
11. Identity-discovery / mismatch runs export diagnostics with `CaptureAuthorized=false` — never LIVE PASS.
12. Four ATAS override adapters require operator smoke inside ATAS.
13. `cumulativeNewObservationCount` is observation count from OnCumulativeTrade only — not unique/total exchange executions (schema 1.0.1).

## P0-03 capability schema (retained)

10. Schema defines contracts; ES CapabilityMatrix evidence remains ES-only.
11. No ContractIdentity / roll resolution implementation.
12. No Status panel rendering changes (P0-09 still deferred).

## Skeleton / environment (retained)

13. InstrumentProfile for GC is not present; operator must enter ExpectedInstrumentCode explicitly.
14. ATAS product **8.0.14.395** / host net10; production **net10.0-windows** + `UseWPF=true`.
15. `UseWPF=true` is not authorization for UI rendering.

## Evidence

16. Workspace CapabilityMatrix SHA-256 `471BDA044627624546709543740C59CD03EAE271D86ED93A16676F44115CFAC1` is **ES-only**.
17. ES risks remain historical notes — not GC claims.
