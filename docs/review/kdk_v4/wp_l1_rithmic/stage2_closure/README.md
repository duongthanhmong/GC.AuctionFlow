# KDK Recorder Architecture & Stage 2 Closure Package v1.0

Built on the evidence in commits `9902ff0` and `e9b263a`. Every `PROVEN`, `VERIFIED` or
`ENFORCED` claim in this package resolves to an EvidenceId that points at a file in this
repository and re-hashes, or to a named test that passes.

**Run this first:**

```bash
python docs/review/kdk_v4/wp_l1_rithmic/stage2_closure/tests/test_stage2_closure.py
```

47/47 at the time of writing. It re-validates every schema, exercises 21 fail-closed rules as
**negative** cases, re-resolves and re-hashes all 15 EvidenceIds, and checks the parameter
registry still has zero approved entries.

## Contents

| file | what |
|---|---|
| `STAGE2_CHECKLIST.md` | the closure checklist — start here |
| `adr/ADR-001..007` | seven architecture decisions, each citing its evidence — all `AWAITING_OWNER_APPROVAL`, none in force |
| `schemas/*.json` | six executable JSON Schemas, draft 2020-12 |
| `CAPABILITY_EVIDENCE_MANIFEST.csv` | 32 surfaces → capability state → EvidenceIds |
| `EVIDENCE_INDEX.csv` | 15 EvidenceIds → artifact path → sha256 |
| `RECORDER_FAILCLOSED_SPEC.md` | the recorder's integrity rules, marked schema-enforced vs prose-only |
| `DATASET_CONTRACTS.md` | what a consumer may assume; why no dataset qualifies for calibration |
| `SPEC_CONFLICT_REGISTER.md` | five conflicts recorded, **none resolved** — MRBS forbids it |
| `build_evidence_manifest.py` | regenerates the manifest; fails on a dangling EvidenceId |
| `tests/test_stage2_closure.py` | the suite |

## The four load-bearing findings

1. **Gap recovery does not exist.** Reconnect works and subscriptions replay, but 3,975
   sequence numbers were lost across a 12.4 s outage with no recovery path and **no
   caller-visible signal**. The recorder must detect this itself and mark the interval
   `Invalid` — enforced by schema, not by prose.
2. **MBO cannot bootstrap a book.** Template 116 seeds exactly one order per price; 3 of 21
   levels matched the vendor's own book. MBO is therefore research telemetry, never core.
3. **Options identity is verified; OI is client-blocked.** Four contracts fully vendor-verified
   with live quotes. Open interest, settlement and market mode are on the wire and decoded —
   `async_rithmic` 1.6.3 has no message class and destroys them. The schema makes
   `optionsState = Ready` **unsatisfiable** while that holds.
4. **The option→futures mapping is not the identity.** `OGU6 → GCV6` while the front month is
   `GCZ6`. Read per series, never inferred.

## Approval status

**No ADR is in force.** All seven are `AWAITING_OWNER_APPROVAL`; no owner
approval has been recorded. They state decisions the evidence supports, not decisions taken.

## What this package does not do

No trading logic. No calibration. No parameter is bound — the registry still has **0 of 134
approved**, and the schemas reject an unapproved parameter that carries a value. No dataset
qualifies for calibration and none is claimed to.

## Missing input

`KDK_Claude_Continuation_Package_v1.0.zip` — including the **execution brief** — is not
present in this workspace. MRBS v1.1, the Parameter Registry v1.0 and the KDK corpus were
found already in the repository and were used. The brief was not, so the report format
specified in its §15 could not be followed; `STAGE2_CHECKLIST.md` and the session report
use the deliverable list given in the request instead.
