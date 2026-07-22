# P0-07C3D — Controlled GCQ6 / Rithmic Live Trade Recorder Verification

**Recommendation:** **PASS**

Live Trade-only recorder verification completed on metadata-closeout DLL after operator GCQ6/Rithmic session with clean shutdown. Prior interim FAIL (NOT EXECUTED) is superseded by this evidence.

---

## 1. Exact baseline (code closed in this commit)

| Field | Value |
|-------|--------|
| Pre-closeout tag | `gcae-p0-07c3bc-trade-recorder-pass` @ `25bf03c034ae5aa2d507f314ce1f4804f5fe83ad` |
| Closing tag (this milestone) | `gcae-p0-07c3d-live-trade-recorder-pass` |
| Master spec | unchanged |
| Scope | Trade recorder live + metadata closeout only; no DOM/BBA/MBO; no framing redesign |

## 2. Build / test result (metadata closeout)

| Step | Result |
|------|--------|
| `dotnet clean -c Release` | OK |
| `dotnet restore` | OK |
| `dotnet build -c Release` | **0 errors, 0 warnings** |
| `dotnet test -c Release` | **218 passed**, 0 failed |

## 3. Deployment (metadata-closeout DLL)

| Field | Value |
|-------|--------|
| Destination | `C:\Users\LOQ\AppData\Roaming\ATAS\Indicators\GC.AuctionFlow.dll` |
| SHA-256 | `FADF6D5518E9D95F3D08B29D9A7917AFB64D8B5A557E47271A26DBD1EE6838CD` |
| Size | 568320 bytes |
| Duplicate Indicators copies | none |

## 4. Runtime environment

| Field | Value |
|-------|--------|
| Instrument | GCQ6 |
| Provider | Rithmic |
| Declared mode | Live / OperatorDeclared |
| ATAS Indicators SDK (prior validated) | 8.0.14.395 |
| MBO | disabled (operational lock retained) |
| DOM / BBA recording | not enabled |
| EnableRawEventRecorder | true |
| EnableTradeRecording | true |
| ExpectedInstrumentCode | GCQ6 |

## 5. Validated live session

| Field | Value |
|-------|--------|
| SessionId | `01f66504-9419-4d3b-acbe-00062dede326` (`01f6650494194d3bacbe00062dede326`) |
| Session directory | `%USERPROFILE%\.gcae\recorder\sessions\01f6650494194d3bacbe00062dede326\` |
| Start UTC | `2026-07-22T11:38:02.7693115Z` |
| Stop UTC | `2026-07-22T11:44:12.9609567Z` |
| Duration | ~6.2 minutes |
| Recorder schema | **1.2.0** |
| Container version | **1** (GCAR) |
| EnabledStreams | **`["Trade"]`** |
| AbnormalTermination | **false** |
| AbnormalTerminationReason | null / empty |

### Prior sessions (not the PASS claim)

| SessionId N | Note |
|-------------|------|
| `94ad02d363aa41a48014c2dccaa3b929` | First live capture (pre-metadata closeout DLL) |
| `737a08961394470b83f6070b684cf383` | Intermediate run (pre-metadata DLL) |

## 6. Artifacts

| Artifact | SHA-256 / note |
|----------|----------------|
| `manifest.json` | `532E92D3008917F5D3D34A9F6A5168340F7C30D49C6FBCBA2A71EE00D0B802C4` (sidecar match) |
| `645e82a54fe7439a9085b62c4578bc0c.seg` | `A553452478BB95250F8416E02B24D192A9BCF8D5E2B5BADA4E58BCC70CFC2387` — GCAR v1 flags=0 — 4520 records (M=2260 I=2260 L=0) seq 1..4520 |
| `4486efb4a557418a80bec8f3592a1cb9.seg` | `67FF5ED1C9B226022EC5EACB5F6ADF820FE24432B127106B1E61F93F76211D0C` — GCAR v1 flags=0 — 870 records (M=435 I=435 L=0) seq 4521..5390 |
| Temporary `.seg.tmp` after shutdown | **none** |

Category sum: RawEventRecordCount = 5390 = 2695 Market + 2695 InvocationResult + 0 Lifecycle.

## 7. Accounting (manifest counters)

| Equation | Result |
|----------|--------|
| WriterDequeuedTotal (5390) = Market(2695)+Inv(2695)+Lifecycle(0)+SerFail(0)+Discard(0) | **OK** |
| InvocationResultAttempts(2695) = Accepted(2695)+QueueFull(0)+Faults(0) | **OK** |
| Clean: InvocationResultsWritten(2695) = Accepted(2695) | **OK** |
| Market: Normalized(2695)=Accepted(2695)+Drops(0); RecordsWritten(2695)=Market+Lifecycle | **OK** |
| CallbackInvocations(2695) = Authorized(2695) = InvocationResultEmissionAttempts(2695) | **OK** |
| Undrained / SerFail / WriterDiscard / WorkerFault / Hash/Flush/Manifest failures | **0** |

## 8. Callback / Trade capture

| Item | Status |
|------|--------|
| Trade capture + grouping + payload path | **PASS** (live; prior validated session lineage retained) |
| DOM / BBA / MBO records | **none** |
| Queue / serialization / writer / drain / flush / hash / manifest failures | **none** |
| Zero-count callback sources (if any) | report as `NOT_OBSERVED_IN_TEST_WINDOW` (not Unavailable) |

## 9. Clean shutdown

| Check | Result |
|-------|--------|
| Stop accepting | yes |
| Bounded drain | yes (UndrainedAtShutdown=0) |
| Footer / durable flush / segment rename / SHA sidecar | yes |
| Manifest completed | yes |
| AbnormalTermination | **false** (IndicatorDispose classified normal) |
| Unhandled exception | none observed |

## 10. Platform safety

| Check | Result |
|-------|--------|
| No MBO subscription | yes (EnableMboLifecycleProbe false; MBO disabled in manifest) |
| Trade-only EnabledStreams | `["Trade"]` |
| Probe continues independently of recorder | design retained; live session clean |

## 11. Metadata closeout (included)

Fixes validated by unit tests + this live session:

1. EnabledStreams Trade-only  
2. Normal termination on successful IndicatorDispose  
3. CallbackInvocations wired (= emission attempts when all invocations emit)  
4. Evidence verifier GCAR UInt16 version/flags parse  

## 12. Known limitations

1. Cumulative tick constituents still not recorded.  
2. MBO primary-process operational lock unchanged.  
3. DOM/BBA recorder not in scope (P0-07C4+).  
4. Manifest is per-session; prior folders are not overwritten.  
5. Chart aggregation / exact ATAS UI version for this specific window not separately instrumented beyond provider/mode/instrument in manifest.

## 13. PASS / FAIL

| Gate | Result |
|------|--------|
| Baseline build/test + metadata closeout | **PASS** |
| Single Indicators DLL deploy | **PASS** |
| Live GCQ6/Rithmic Trade session + clean shutdown | **PASS** |
| Artifacts GCAR=1 / schema 1.2.0 / SHA | **PASS** |
| WriterDequeuedTotal + InvocationResult + callback accounting | **PASS** |
| EnabledStreams / normal termination | **PASS** |

### Overall recommendation

**PASS**
