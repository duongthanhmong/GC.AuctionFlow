# P0-06B — MBO Operator Evidence + Diagnostic Hardening Notes (GCQ6 / Rithmic)

**Status:** MBO runtime event presence **PASS**; P0-06B diagnostic hardening applied (schema **1.0.1**).  
**Instrument:** GCQ6 · **Provider:** Rithmic · **Provenance:** OperatorDeclared  

Artifacts under `%USERPROFILE%\.gcae\capability\`.

---

## RUN A

| Field | Value |
|-------|--------|
| SessionId | `bded768b-27d3-46af-ba8a-aeffa3801bb2` |
| Artifact | `MboLifecycleProbe_GCQ6_20260721T195540Z_bded768b.json` |
| SHA-256 | `1E6559C94838F4FCC16C7A8FA913B310FF4956E1A734A5996ADF88D26C1EDF79` |

Validated runtime notes:

- Initial subscription delivered **2176** raw Snapshot observations.
- Full snapshot price range included **10.0** through **5826.1**.
- Depth Of Market visual bar interaction is strongly supported but **not** claimed as proven platform internals.

## RUN B

| Field | Value |
|-------|--------|
| SessionId | `7ab4b3ef-7fa1-4129-ad17-203bdea3383c` |
| Artifact | `MboLifecycleProbe_GCQ6_20260721T195758Z_7ab4b3ef.json` |
| SHA-256 | `A07B80DD2481C164C6C0BA494BF8E42D50AFD43C7481A7B8A549640FE345CF28` |

Validated runtime notes:

- Remove/re-add delivered **no new** Snapshot observations.
- Provider subscription persistence is **suspected but not proven**.

---

## Locked interpretation (unchanged by P0-06B)

- MBO runtime event presence: **PASS** on GCQ6/Rithmic.
- LiveMboCapabilityClaim / MboLifecycleCompletenessClaim / StableMboBookReconstruction remain **false**.
- SnapshotCompletionKnown remains **false**.
- Historical MBO / Replay MBO remain **Unknown**.

## P0-06B diagnostic fixes (schema 1.0.1)

- `duplicateSubscribeSuppressed` no longer inflated by later OnCalculate checks.
- Artifact uses `captureSubscriptionEpoch` + `finalClosedEpoch` (not ambiguous `subscriptionEpoch`).
- Initial time window anchored to `FirstCallbackReceiveUtc` (5s seed).
- `MboOrderObservationState` capacity saturation recorded without eviction.
