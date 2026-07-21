# P0-05B — DOM Event Semantics Operator Evidence (GCQ6 / Rithmic)

**Status:** PASS  
**Date (UTC evidence):** 2026-07-21  
**Closeout:** P0-05B Operator Evidence Closeout  
**Instrument:** GCQ6  
**SecurityId:** GCQ6@COMEX  
**Exchange:** COMEX  
**Declared mode:** Live  
**Declared provider:** Rithmic  
**Provider provenance:** OperatorDeclared  
**CaptureAuthorized:** true (both runs)  
**Companion SHA-256:** independently verified for both runs  

Artifacts live under `%USERPROFILE%\.gcae\capability\`.

---

## RUN A

| Field | Value |
|-------|--------|
| SessionId | `e82c53a9-c92a-4cf6-859c-f077ca8af384` |
| Artifact | `DomSemanticsProbe_GCQ6_20260721T185932Z_e82c53a9.json` |
| SHA-256 | `176DC185BC57AB9D4127718ABC6A897057AEDD6762FEBED812CDBD0A8852C336` |

| Metric | Value |
|--------|-------|
| MarketDepth batch observations | 17162 |
| BestBidAsk observations | 4208 |
| Snapshot items | 1247 |
| AcceptedToQueue | 21370 |
| ProcessedByWorker | 21370 |
| QueueFullDrops | 0 |
| NormalizationFailures | 0 |
| ZeroVolumeObservations | 1048 |
| Managed callback thread IDs | 30 |
| Source-time decreasing transitions | 792 |

## RUN B

| Field | Value |
|-------|--------|
| SessionId | `319fd10c-f61a-4d23-9138-7d6b79fa670e` |
| Artifact | `DomSemanticsProbe_GCQ6_20260721T190403Z_319fd10c.json` |
| SHA-256 | `ED424D7AE4117D1C80BFC67C5022CAE4BB7D6518D91B5FC7A37363B171F3B539` |

| Metric | Value |
|--------|-------|
| MarketDepth callback invocations | 9428 |
| MarketDepth batch observations | 9430 |
| BestBidAsk observations | 2118 |
| Snapshot items | 1251 |
| AcceptedToQueue | 11548 |
| ProcessedByWorker | 11548 |
| QueueFullDrops | 0 |
| NormalizationFailures | 0 |
| ZeroVolumeObservations | 644 |
| Managed callback thread IDs | 20 |
| Source-time decreasing transitions | 521 |

---

## Callback observation status (both runs)

| Source | Status |
|--------|--------|
| MarketDepthsBatch | OBSERVED |
| BestBidAskChanged | OBSERVED |
| SnapshotPull | OBSERVED |
| MarketDepthChanged (singular) | NOT_OBSERVED_IN_TEST_WINDOW |

---

## Locked capability interpretation

### LiveDom

| Axis | Locked value |
|------|----------------|
| Availability | **Available** |
| Coverage | includes **Live** |
| Runtime presence | **Observed** |
| Fidelity | **Partial** / not fully validated |
| NativeSequence | **Absent** |
| StableBookReconstruction | **false** |

Do **not** mark LiveDom as fully Validated.

### HistoricalDom

**Unknown** (unchanged).

### ReplayDom

**Unknown** (unchanged).

Probe JSON boolean claims (`LiveDomCapabilityClaim` / Historical / Replay) remain **false** in exported artifacts — those flags do not assert full capability validation. Governance Availability=Available above is the locked matrix interpretation from operator evidence.

---

## Locked semantic findings

- DataType and IsBid/IsAsk were fully consistent in both runs.
- Zero-volume depth observations exist.
- Zero volume must not yet be classified as Delete.
- Volume meaning remains **Unknown**.
- Update action remains **Unknown**.
- All observed source `DateTime.Kind` values were **Unspecified**.
- Source timestamps were **not** monotonic.
- DOM callbacks were delivered across many managed threads.
- No exchange-level ordering guarantee is established.
- Internal continuity does not establish exchange-feed completeness.

---

## Snapshot findings

- Snapshot pull executed and was non-empty in both runs.
- Snapshot sizes: **1247** (Run A) and **1251** (Run B).
- Bid minimum **10.0** and Ask maximum **5826.1** were observed.
- Snapshot coverage is provider-defined and very broad.
- Snapshot completion and production-book suitability remain **Unknown**.
- Smart DOM visible depth must not be assumed equal to API snapshot depth.

---

## Reset / reconnect

- Normal attach/remove: **passed**
- Remove/re-add: **passed** as two separate operator runs (A then B)
- Native reset marker: **not observed**
- Controlled reconnect: **not tested** → reconnect behavior remains **Unknown**
