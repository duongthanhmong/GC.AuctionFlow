# TIEN_DO — R2 proposed progress delta (NOT owner-adopted)

**Status: `PROPOSAL_PENDING_OWNER_ADOPTION`.** R2 HEAD `c94ed8b`.

> **Base-availability note.** The R2 instruction says to base this on the supplied `TIEN_DO(1).md`
> v1.2 (`d24ec88b…`). That file is **not materialized** in this workspace (Provenance Matrix) — its
> exact content is in no commit and not on disk. Therefore this is an **evidence-backed progress
> delta** using the `WP-00…WP-10` semantics **stated in the R2 task instruction**, NOT a merge of
> v1.2 body text I have never seen. It does **not** overwrite the tracked `docs/TIEN_DO.md`.

## Two axes preserved

- **Delivery WBS:** `WP-00…WP-10` (supplied v1.2 control proposal — external, not materialized).
- **Execution lanes:** Chặng 0–5 + phase-owner B0–B6/C/D. Options runs as a parallel lane (KDK 2447).

## Evidence-backed WP progress (only what is proven at HEAD)

| WP | code/artifact axis | evidence/acceptance axis | evidence |
|---|---|---|---|
| **WP-02** (census/audit) | census + R1 + R2 complete | **independent review PENDING** | `audit_census_001/`, SHA256SUMS 25/25 clean-checkout |
| **WP-03** (Stage 2 package) | package present, 47/47 schema tests | **verified, NOT owner-accepted** | `stage2_closure/`, 15 EvidenceIds 15/15 |
| **WP-04** (writer) | writer foundation present (`SegmentWriter` 411L, wired); contract-fit done | **architecture + production proof PENDING** | `r2/contract_compat_report.json` (5 fail-closed fields ABSENT) |
| **WP-05** (acceptance/FAR/AAC) | classifiers `CODE_PRESENT_PARTIAL`, `NotCalibrated` | **acceptance criteria PROPOSED; sustained-load evidence ABSENT** | Owner Pack R2 §4; longest window 160 s |
| **WP-06** (dataset) | contract/QA prep only | **no accepted point-in-time dataset** | `DATASET_CONTRACTS.md` 4/6 unmet |
| **WP-07** (Options) | code candidates/partial wiring | **requirement traceability + calibration INCOMPLETE; analytics INVALIDATED** | `OPT-001..006`; overlay `DISPLAY_ONLY` |

## Not changed

Decision queue Q1–Q10 (tracked `TIEN_DO.md`) and the module tower table stand. This delta adds only
the WP axis and the R2-verified progress states. **No parameter bound; 0/134 approved.**
