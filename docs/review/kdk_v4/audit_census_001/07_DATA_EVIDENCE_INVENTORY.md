# 07 — Data & Evidence Inventory

**Audited commit:** `43d458f`.

## Live captured market data in the repo

The only real captured market data committed to the repo lives under
`docs/review/kdk_v4/wp_l1_rithmic/probe/` — sanitized per-probe result CSVs, transcripts,
capture manifests, and a handful of decoded-frame JSONs. **Raw captures remain in the quarantine
directory OUTSIDE the repo** (`stage2_closure/README.md` §handover). The committed evidence is the
*derived* record (results/manifests), not the raw firehose.

**Longest continuous live capture window that exists as evidence: 160 seconds**
(session `probe_20260731T102133Z`). Every readiness sub-test (E5/E6/E7) used windows of
25–90 s. There is **no** sustained-load capture. This directly substantiates **D1 = NOT_STARTED**.

## 15 EvidenceIds — re-hashed at HEAD

`stage2_closure/EVIDENCE_INDEX.csv`: **15 EvidenceIds, 15 match, 0 drift, 0 missing** at this
commit. The Stage 2 evidence chain is intact and independently re-verifiable.

## ADR status (read from the 7 files)

All seven ADRs in `stage2_closure/adr/` carry `**Status:** AWAITING_OWNER_APPROVAL` with the
line "none recorded ... NOT in force until the owner approves it." **No ADR is in force.**

## Acceptance records

Search over `docs/governance/`, `docs/review/`, `IMPLEMENTATION_STATUS.md` for a signed/approved
acceptance artifact: **none found.** The only `ACCEPTED` strings are (a) prose in `STAGE2_CHECKLIST`
describing the *pending* state, and (b) `AUTHORITY_ORDER.md:25` describing the reviewer's *role*.
**There is no artifact in which an owner or reviewer records acceptance of Stage 2, any ADR, any
parameter, or any requirement.** `ACCEPTED` axis = **NOT_EVIDENCED** across the board.

## Dataset qualification (`DATASET_CONTRACTS.md`, 6 requirements)

| # | requirement | status | evidence |
|---|---|---|---|
| 1 | point-in-time, no look-ahead | schema supports; **no dataset** | — |
| 2 | full provenance per record | **schema-enforced** | schema tests |
| 3 | `Ready` across calibration window | **UNMET** — no window measured | longest = 160 s |
| 4 | no unrecovered gap in window | schema-enforced | — |
| 5 | sufficient sample + warm-up | **UNMET** — parameter unbound | 0/134 approved |
| 6 | out-of-sample held back | **UNMET** — never run | — |

**Does any dataset meet the point-in-time provenance contract? NO.** Requirements 1, 3, 5, 6 are
unmet. **D3 = CONTRACT_ONLY / BLOCKED** confirmed.

## OptionFlow sidecar

`artifacts/optionflow/<PRODUCT>/levels.json` — the DLL reader (`OptionFlow/OptionFlowReader`)
treats a missing file as a first-class `null` (`GexContext=null`). Whether a current `levels.json`
exists on disk is environment-state, not repo-tracked evidence; the Options analytics that would
produce it are `IMPLEMENTED_BUT_INVALIDATED` (6 defects `OPT-001..006`,
`03_OPTIONS_DEFECT_REGISTER.md`). Options evidence axis = **INVALIDATED**, not `Ready`.
