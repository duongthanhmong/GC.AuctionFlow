# Review Checklist — P0-07C2 Callback Grouping Amendment

## Prior

- [x] P0-07B PASS / tag `gcae-p0-07b-recorder-core-pass`
- [x] P0-07C1 PASS / Decision B

## P0-07C2 must pass

- [x] RawEventRecorderSchemaVersion = 1.1.0; container version 1 unchanged
- [x] Probe schemas / ProbeVersion 0.0.6 unchanged
- [x] CallbackCaptureContext + per-source Interlocked sequences; first value 1; overflow no wrap
- [x] Draft/Envelope grouping fields; StreamLocal vs writer dequeue distinct
- [x] CallbackInvocationResultPayload semantics; FanOutItemRejections/Faults ≠ NormalizationFailures
- [x] Null raw items consume ordinals; null singular accounting
- [x] ReceiveUtc alias exactly CallbackReceiveUtc; aliases JsonIgnore / absent from JSON
- [x] PrimitiveFanOutCoordinator independent sink outcomes; capability-then-recorder order documented
- [x] SinglePassEnumerationHelper single-pass; stage-distinguished GetEnumerator/MoveNext/Current; no Count/ToList/ToArray
- [x] Same-queue invocation-result policy + separate counters recorded for P0-07C3 (not wired)
- [x] Record-count taxonomy gap documented as required C3 amendment
- [x] No GcAuctionFlowIndicator changes
- [x] No recorder operator settings / Trade/DOM adapters / live enablement
- [x] BBA UNKNOWN lock recorded; no MBO recording enablement / no recorder SubscribeMarketByOrderData
- [x] Master specification untouched
- [x] `dotnet clean/restore/build/test -c Release` — 0 errors, 0 warnings
- [x] P0-07C3 / P0-07C4 not started
