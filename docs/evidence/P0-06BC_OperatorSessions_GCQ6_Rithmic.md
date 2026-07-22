# P0-06B/C — Extended Operator Evidence (GCQ6 / Rithmic)

Companion to `P0-06B_MboLifecycle_GCQ6_Rithmic_OperatorEvidence.md`.

## Fresh MBO snapshot run

| Field | Value |
|-------|--------|
| SessionId | `142cbe8d-32cf-49df-8931-2933e96f952b` |
| subscriptionAttemptCount | 1 |
| Callback presence | OBSERVED |
| BatchItemsEnumerated | 7594 |
| rawSnapshot / New / Change / Delete | 2138 / 1733 / 2008 / 1715 |
| zeroExchangeOrderId | 0 |
| QueueFullDrops / NormalizationFailures | 0 / 0 |
| Snapshot price extremes | includes 10.0 and 5826.1 |
| Chart | Abnormal M1 vertical bar after first add; remained after removing GCAE |

## Remove / re-add run

| Field | Value |
|-------|--------|
| SessionId | `5fdd910f-ce6a-4799-8e35-0d564ad034d9` |
| subscriptionAttemptCount | 1 (new instance) |
| BatchItemsEnumerated | 5347 |
| rawSnapshot | **0** |
| rawNew / Change / Delete | 1778 / 1770 / 1799 |
| QueueFullDrops / NormalizationFailures | 0 / 0 |
| Chart | No second abnormal bar observed |

## Three-probe concurrent run

| Field | Value |
|-------|--------|
| SessionId | `38a1fb31-6da6-43f1-a7fa-f5da5870b35b` |
| Trade Accepted/Processed/Drops/NormFail | 1628 / 1628 / 0 / 0 |
| DOM Accepted/Processed/Drops/NormFail | 27699 / 27699 / 0 / 0 |
| MBO Accepted/Processed/Drops/NormFail | 15546 / 15546 / 0 / 0 |
| MBO rawSnapshot | 0 |

**Interpretation:** concurrent probes PASS in observed window. Abnormal M1 bar correlates strongly with fresh initial MBO snapshot; ATAS internal mechanism unproven — do not claim causality beyond evidence.
