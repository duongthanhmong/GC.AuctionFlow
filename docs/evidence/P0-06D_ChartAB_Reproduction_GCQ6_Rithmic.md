# P0-06D — Controlled Chart A/B Reproduction Closeout (GCQ6 / Rithmic)

**Status:** PASS  
**Closeout type:** Documentation only  
**Date (UTC evidence):** 2026-07-22  

---

## Session

| Field | Value |
|-------|--------|
| SessionId | `ced0cc72-dad1-431b-b542-e2d30611fa35` |
| Artifact | `MboLifecycleProbe_GCQ6_20260722T081336Z_ced0cc72.json` |
| SHA-256 | `AD5D12D778174FFD0D9CCF34E0959510DB878F86A20B13CEC609A65C033EC6D8` |
| Companion SHA-256 | Independently verified (match confirmed) |

Artifact path: `%USERPROFILE%\.gcae\capability\`

---

## Artifact identity / gates

| Field | Value |
|-------|--------|
| SchemaVersion | 1.0.1 |
| ProbeVersion | 0.0.6 |
| Instrument | GCQ6 |
| SecurityId | GCQ6@COMEX |
| Mode | Live / OperatorDeclared |
| Provider | Rithmic / OperatorDeclared |
| CaptureAuthorized | true |
| captureSubscriptionEpoch | 1 |
| finalClosedEpoch | 2 |
| subscriptionAttemptCount | 1 |
| duplicateSubscribeSuppressed | 0 |
| subscribeTriggerCheckCount | 75041 |
| Callback status | OBSERVED |
| MBO runtime event presence | true |

## Integrity

| Counter | Value |
|---------|------:|
| callbackInvocations | 4376 |
| batchItemsEnumerated | 6530 |
| acceptedToQueue | 6530 |
| processedByWorker | 6530 |
| queueFullDrops | 0 |
| normalizationFailures | 0 |
| batchEnumerationFailures | 0 |
| gate / post-dispose rejects | 0 |

## Raw types

| Type | Count |
|------|------:|
| Snapshot | 2131 |
| New | 1353 |
| Change | 1677 |
| Delete | 1369 |
| Unknown | 0 |
| zero ExchangeOrderId | 0 |

## Initial burst

| Field | Value |
|-------|--------|
| firstCallbackBatchItemCount | 2131 |
| first callback batch | raw Snapshot records |
| snapshotObservationCount | 2131 |
| distinctNonzeroIdsWithSnapshot | 2131 |
| firstSnapshotReceiveUtc == lastSnapshotReceiveUtc | yes |
| initialTimeWindowDurationSeconds | 5 |
| initialTimeWindowItemCount | 2487 |
| snapshotSeenAfterInitialTimeWindow | false |
| SnapshotCompletionKnown | false |

## Bounded state

| Field | Value |
|-------|--------|
| maxTrackedNonzeroIds | 65536 |
| trackedNonzeroIdCount | 3484 |
| stateCapacityReached | false |
| untrackedNonzeroIdDueToStateCapacity | 0 |

---

## Controlled Chart A/B result

Operator opened **two separate GCQ6 charts** in the **same ATAS session**.  
GCAE with **only MBO probe** enabled was attached to **one** chart.

**Observed:**

- An abnormal vertical bar appeared on the chart containing GCAE.
- The **same** abnormal bar appeared on the separate GCQ6 chart that did **not** contain GCAE.
- The event occurred during the fresh initial MBO snapshot sequence.

**Interpretation:**

- Chart-local GCAE DataSeries writing is **not** supported by evidence (reinforces P0-06C Decision B).
- A shared ATAS/Rithmic instrument/provider/chart-data interaction is **strongly supported**.
- Exact platform mechanism remains **Unknown**.
- Do **not** state that MBO was converted into a trade or candle.
- Do **not** claim internal ATAS causality beyond the observed correlation.

---

## Operational lock

**MBO subscription must not be enabled in the ATAS process used for primary GC analysis or trading.**

A separate chart or workspace **inside the same process** is **not** considered proven isolation.

Future MBO testing requires an isolated test environment — preferably a **separate ATAS process**, virtual machine, or machine.

---

## Status rollup

| Item | Status |
|------|--------|
| P0-06B diagnostic hardening | **PASS** |
| P0-06C chart side-effect audit | **PASS / Decision B** |
| P0-06D controlled Chart A/B reproduction | **PASS** |
| P0-06 overall | **PASS WITH PLATFORM-SIDE OPERATIONAL LIMITATION** |
| P0-07 | **NOT STARTED** |
