# Implementation Status

| Field | Value |
|-------|--------|
| Current phase | **P0-07C3BC** — Trade recorder accounting + live Trade callback integration |
| Probe version | **0.0.6** (unchanged) |
| RawEventRecorderSchemaVersion | **1.2.0** |
| RawEventContainerVersion | **1** (unchanged) |
| Trade / Dom / Mbo probe schemas | **1.0.1** / **1.0.0** / **1.0.1** (unchanged) |
| P0-07B | **PASS** |
| P0-07C1 | **PASS / Decision B** |
| P0-07C2 | **PASS** (`gcae-p0-07c2-callback-grouping-pass`) |
| P0-07C3A | **PASS** |
| P0-07C3BC | **IMPLEMENTED — pending closeout review** |
| P0-07C3D | **NOT STARTED** |
| P0-07C4 | **NOT STARTED** |

## P0-07C3BC locks

- Schema **1.2.0**; container **1** unchanged (no GCAR/GCF1 redesign)
- Footer/manifest category counts: RawEventRecordCount = Market + InvocationResult + LifecycleIntegrity
- Invocation-result same queue; separate InvocationResult* counters; not NormalizedObservations
- Trade callbacks dual-map probe + recorder; never `base.OnNewTrades`
- Recorder never accesses `CumulativeTrade.Ticks`; ReportedTickCountAvailable=false / null
- Settings: EnableRawEventRecorder=false, EnableTradeRecording=true (master-gated)
- DOM / BBA / MBO recording deferred; MBO lock retained
- P0-07C3D live verification **not started**
