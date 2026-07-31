# 04 — Runtime Wiring Map

**Audited commit:** `43d458f`. Traced from the composition root by reading source, not architecture docs.

**Composition root:** `src/GC.AuctionFlow/Atas/GcAuctionFlowIndicator.cs` (an ATAS `Indicator`).
This is the ONLY entry point; there is no standalone host. Everything runs inside ATAS's
`OnCalculate` / `OnNewTrade` / `OnNewTrades` callbacks.

## The chain, boundary by boundary

Intended: `Rithmic → recorder → AMT → Episode → Acceptance → Order Flow → FAR/AAC → AnalysisSnapshot → ATAS`

| # | Boundary | Producer → Consumer | Wiring (file:line) | State |
|---|---|---|---|---|
| B0 | **Rithmic → DLL** | ATAS platform → `OnNewTrade(MarketDataArg)` | `GcAuctionFlowIndicator.cs:780` | `RUNTIME_WIRED` — but see note A |
| B1 | **ingress → recorder** | `OnNewTrade` → `TryRecordNewTrade` → `SegmentWriter` | `:796` → `:3236`; `new SegmentWriter` at `RawEventRecorderSession.cs:92` | `RUNTIME_WIRED`, `TEST_EVIDENCED` |
| B2 | **ingress → AMT** | `MapNewTrade` → `_profileHost` (TPO/VP) | `:794`; read back at `:1443-1444` | `RUNTIME_WIRED` |
| B3 | **AMT → Reference/Composite** | `_profileHost.Current` → `_referenceHost`, `_compositeHost` | `:1476, :1496, :1522, :1528` | `RUNTIME_WIRED` |
| B4 | **→ Runtime engine** | hosts → `new GcaeRuntimeEngine(...)` | `:1345`; ctor `GcaeRuntimeEngine.cs:41` | `RUNTIME_WIRED` |
| B5 | **Episode/Acceptance/EffortResult → snapshot** | `runtime.Publish(...)` → `GcaeRuntimeSnapshot` | call `:2967`; producer `GcaeRuntimeEngine.cs:51,276` | `RUNTIME_WIRED`, `NotCalibrated` — note B |
| B6 | **snapshot → ATAS render** | `GcaeRuntimeSnapshot` / host `.Current` → GPS card + overlays | `:1443-1528` reads; renderers in `Atas/` | `RUNTIME_WIRED` (`DISPLAY_ONLY`) |

## Notes — where reality diverges from the intended architecture

**Note A — Rithmic ingress is INDIRECT.** The DLL does **not** open its own Rithmic connection.
It receives `MarketDataArg` from the **ATAS platform**, which owns the Rithmic feed. The
`Recorder/` and `Probe/` subsystems record what ATAS hands them. The direct-Rithmic acquisition
proven in the Stage 2 package (`probe/` evidence) is a **separate Python path** (`async_rithmic`),
NOT the DLL's ingress. Consequence: the ATAS path loses fields the direct path carries
(`NativeSequenceAvailable=false` on the ATAS path — the entire premise of WP-L1-RITHMIC).

**Note B — the chain runs end-to-end but CONCLUDES nothing.** `runtime.Publish()` executes and
produces a `GcaeRuntimeSnapshot`, but every downstream classifier is gated `NotCalibrated`
(383 occurrences in `src/`). Entry emits `ObserveOnly` only. So the break point is **not** a
missing consumer — it is a **calibration gate**, by design (no parameter is `Approved`, ADR-005).

**Naming gap.** The requirement/MRBS term is **`StructuralAnalysisSnapshot`** (MRBS S41,
02B). It does **not exist** in `src/` (0 files). The implemented type is **`GcaeRuntimeSnapshot`**
(`GcaeRuntimeEngine.cs:276`). A reviewer matching requirement names to symbols will find a false
"missing"; it is a rename, not an absence. **RECORD as contradiction CTR-06.**

## Dead / partially-wired outputs

| symbol | producer | consumer in `src/` | verdict |
|---|---|---|---|
| `RecoveryScanner` (546 lines) | `Recorder/RecoveryScanner.cs:43` | **NONE** — only `tests/Unit/Recorder/*` call it | `TEST_EVIDENCED`, **NOT `RUNTIME_WIRED`** — dead in production path |
| `SegmentWriter` (411 lines) | `Recorder/SegmentWriter.cs:92` | `RawEventRecorderSession.cs:92` | `RUNTIME_WIRED` — reachable from ATAS recorder startup dispatch (`GcAuctionFlowIndicator.cs:3166`) |
| `ManifestWriter` (35 lines) | `Recorder/ManifestWriter.cs:7` | recorder session | `CODE_PRESENT` |

## WIRE-BREAK — the exact stop point

**The runtime chain does not break on wiring. It "breaks" (stops producing actionable output) at
B5: the Acceptance / FAR-AAC classifiers are `NotCalibrated`, so the published snapshot carries
observations, not conclusions.** Operational consequence: the DLL renders profiles, zones and
observe-only state to ATAS, and records raw events, but **emits no trade signal** — which is the
intended, disciplined behaviour, not a defect. Reaching `Ready` requires (a) approved parameters
(0/134 today) and (b) a calibration dataset (none exists). Both are owner/dataset-gated, not code
gaps.
