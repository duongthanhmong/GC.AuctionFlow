# Review Checklist — P0-07C3BC Trade Recorder Accounting + Integration

## Prior

- [x] P0-07C2 PASS / tag `gcae-p0-07c2-callback-grouping-pass`
- [x] P0-07C3A PASS

## P0-07C3BC must pass

- [x] RawEventRecorderSchemaVersion = 1.2.0; container version 1 unchanged
- [x] Probe schemas / ProbeVersion 0.0.6 unchanged
- [x] Footer/manifest category counts + recovery equation
- [x] InvocationResult* same-queue counters
- [x] TradeToRawEventAdapter with DirectionRaw/DataTypeRaw; no Ticks access
- [x] Trade callbacks dual-map; no base.OnNewTrades
- [x] Settings EnableRawEventRecorder / EnableTradeRecording only
- [x] Startup gate + diagnostics; master disabled = NotConfigured
- [x] Dispose: recorder → Trade → DOM → MBO → base.OnDispose finally
- [x] No DOM/BBA/MBO recorder / no master-spec changes
- [x] `dotnet clean/restore/build/test -c Release` — 0 errors, 0 warnings
- [x] P0-07C3D not started
