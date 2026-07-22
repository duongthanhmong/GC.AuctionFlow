# P0-06C — Chart Side-Effect Audit (ATAS 8.0.14.395)

**Status:** Decision **B** — no GCAE chart/DataSeries write path found that can receive MBO prices.  
**Date:** 2026-07-22  
**No speculative visual fix implemented.**

---

## 1. GCAE chart write inventory

| Location | Finding | Label |
|----------|---------|--------|
| `GcAuctionFlowIndicator.OnCalculate` | Calls EnsureProbesStarted / TryCaptureInstrument / TrySubscribeMboOnce / deferred DOM snapshot only. **No** `this[bar] = …` | **OBSERVED_SOURCE** |
| Entire `src/GC.AuctionFlow` | No matches for `this[`, `ValueDataSeries`, `CandleDataSeries`, `DataSeries.Add`, `AddSeries`, `OnRender`, OHLC assignment into series | **OBSERVED_SOURCE** |
| `MboAtasMapper` / `MboObservation` | Copies primitives into immutable sealed observation; no ATAS series types | **OBSERVED_SOURCE** |
| Probe workers | No chart/candle/series references | **OBSERVED_SOURCE** |
| Constructor | `DenyToChangePanel = true`; `EnableCustomDrawing = false` | **OBSERVED_SOURCE** |

Compiled IL check: `OnCalculate` does **not** call `set_Item` (**OBSERVED_IL** via PEReader tests).

---

## 2. Default ATAS Indicator series behavior

| Finding | Label |
|---------|--------|
| `BaseIndicator` ctor creates `ValueDataSeries`, sets `VisualType`, `List.Add` to `DataSeries` | **OBSERVED_IL** |
| `BaseIndicator.set_Item(int, decimal)` / `get_Item(int)` exist (indexer) | **OBSERVED_API** |
| `BaseIndicator.OnCalculate` abstract/virtual with empty declared body in metadata | **OBSERVED_API** |
| Platform may still host the default series even if GCAE never writes it | **INFERRED** |

GCAE does not assign into the default series. Whether an empty/default series still participates in autoscale is **UNKNOWN** (platform behavior).

---

## 3. Exact visual-isolation APIs found (do not invent others)

| Member | Declaring type | Label |
|--------|----------------|--------|
| `IsHidden` | `IDataSeries` / `BaseDataSeries` / `ValueDataSeries` | **OBSERVED_API** |
| `IsVisible` | same | **OBSERVED_API** |
| `ScaleIt` | `ValueDataSeries`, `CandleDataSeries`, … | **OBSERVED_API** |
| `VisualType` | `ValueDataSeries` | **OBSERVED_API** |
| `ShowCurrentValue` | `ValueDataSeries` | **OBSERVED_API** |
| `DrawAbovePrice` | series + `ExtendedIndicator` | **OBSERVED_API** |
| `IgnoreHistoryScale` | `Indicator` | **OBSERVED_API** |
| `Panel` | `BaseIndicator` | **OBSERVED_API** |
| `Visible` | `ChartObject` | **OBSERVED_API** |
| `EnableCustomDrawing` / `OnRender` | `ExtendedIndicator` | **OBSERVED_API** (GCAE leaves drawing disabled) |

No member named `ExcludeFromAutoscale` or `IgnoredByScale` was observed on these assemblies.

Making the default series inert would be a **separate authorized change** after proving it is necessary — **not** done in P0-06C under Decision B.

---

## 4. Operator evidence correlation

| Session | Shape | Chart note |
|---------|--------|------------|
| `142cbe8d-…` | Fresh MBO snapshot (rawSnapshot=2138) | Abnormal M1 vertical bar after first add; remained after GCAE remove |
| `5fdd910f-…` | Remove/re-add (rawSnapshot=0) | No second abnormal bar |
| `38a1fb31-…` | Trade+DOM+MBO concurrent | Zero queue drops / zero norm failures; rawSnapshot=0 |

**Interpretation:** abnormal bar **correlates** with fresh initial MBO snapshot. Internal ATAS mechanism **unproven**. Do not claim GCAE wrote the bar OHLC.

---

## 5. Root-cause decision: **B**

- GCAE writes only probe bootstrap logic in `OnCalculate`; no MBO/depth price reaches a chart DataSeries.
- No speculative visual fix.
- Suspected platform/provider shared-pipeline interaction remains **unproven**.
- MBO must not be enabled on the operator’s primary analysis chart until a controlled reproduction (below) completes.

---

## 6. Minimal reproduction plan (later)

**Chart A (control):** clean GCQ6 M1, no indicators; record current-bar OHLC before/after window.  
**Chart B:** clean GCQ6 M1; GCAE with **only** MBO probe enabled; no DOM display indicator; record add UTC, first MBO callback UTC, OHLC before/after first callback; screenshots.  
**Optional Chart C:** only if Decision A later requires proven isolation (`IsHidden`/`ScaleIt` on default `ValueDataSeries`).  

No order placement. Do not use the main analysis chart.
