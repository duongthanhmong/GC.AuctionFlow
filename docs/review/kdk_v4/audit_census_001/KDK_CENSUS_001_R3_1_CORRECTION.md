# KDK-CENSUS-001-R3.1 — correction receipt (narrow)

Fixes the four residual inconsistencies the reviewer flagged after accepting R3.1's technical
corrections. **No R3.2 census, no test rerun** (the Python harness was re-run to regenerate its
machine report; `dotnet` was not). `AUDITED_SOURCE_HEAD 43d458f`, `R3_DELIVERABLE_COMMIT e751f6e`.
Worktree 131 untracked / 0 tracked-modified preserved. No `.cs` changed.

## C1 — machine report reconciled to `_003` (was stale R3)

`r2/contract_compat_report.json` carried the old R3 heuristic: `sequenceStats=PRESENT_PARTIAL`,
`versions=PRESENT_PARTIAL`, and *"not synthetic"* — contradicting `KDK_D2_ARTIFACT_FIT_003.md`. The
harness now **derives its classification directly from `_003`** (`classification_authority:
KDK_D2_ARTIFACT_FIT_003.md`), so the machine report can no longer disagree with the doc. Re-run
output:

```
classification (per _003): EXACT=0 SE=4 DERIV=0 PDS=1 ABSENT=7 total=12
sequenceStats / versions : ABSENT_REQUIRES_NEW_PRODUCER_STATE / ABSENT_REQUIRES_NEW_PRODUCER_STATE
binary integrity label   : TEST_EVIDENCED_WITH_DETERMINISTIC_SYNTHETIC_FIXTURES / NOT_LIVE_EVIDENCED
```

The old `RECORDER_TO_SCHEMA` heuristic (which produced the wrong `PRESENT_PARTIAL`) was removed. The
JSON no longer contains the string "not synthetic".

## C2 — R3.1 commit-SHA pointer corrected

`KDK_CENSUS_001_R3_1_REVIEW.md` said the R3.1 commit SHA is in `…HANDOVER.zip.sha256`. That file
holds **only the archive hash**. The R3.1 deliverable commit
`6ef076896a0996adaef330499e4d78218e9e8d7c` is recorded in
**`handover_r3_1/POST_COMMIT_RECEIPT.md`**. The review line is corrected to point there.

## C3 — `SEC-001` dependency reconciled (was a regression)

The R3.1 Owner Pack listed `SEC-001` as a dependency of `AUTH-D2-DIRECT-SRC` (source changes). That
was wrong per the agreed authority split: **`SEC-001` (credential rotation) gates the direct
connection / live session, not source editing.** Editing/reviewing the direct recorder source needs
no rotated credential. Corrected: `SEC-001` moved off `AUTH-D2-DIRECT-SRC` onto `AUTH-LIVE-SESSION`;
`AUTH-D2-DIRECT-SRC` now requires only `ADR-001` + `ADR-003-DIRECT` + `DEC-ARCH ∈ {A,C}`.

## C4 — clean-checkout 46/46 is repository-verifiable, receipt-attested this round

The `46/46` clean git-checkout result is reproducible by anyone with repository access
(`git archive <commit> … | sha256sum -c SHA256SUMS.txt`). It was **not independently reproducible in
the reviewer's round because the repository was not delivered there** — only the archive was. This is
a *delivery scope* note, not a correctness gap: the **archive extraction 45/45** was independently
reproduced by the reviewer. To make the clean-checkout independently checkable without repo access,
the corrected archive below is the authoritative self-contained package.

## Corrected archive

`handover_r3_1/GCAE_CENSUS_R3_1_CORR_HANDOVER.zip` supersedes the first R3.1 archive
(`GCAE_CENSUS_R3_1_HANDOVER.zip`, sha `14d7cbbf…`, which is retained but carries the stale JSON). The
corrected archive contains the reconciled `contract_compat_report.json`, the corrected review and
Owner Pack, this correction receipt, the correct `13_MANIFEST.md`, and the bannered historical
`KDK_D2_CONTRACT_FIT_001.md` under its correct unsuffixed name. Its own hash is in an **external**
`.sha256`; the R3.1-correction commit SHA is in the post-commit receipt.

## Unchanged (reviewer-confirmed, retained)

23 CODE_PRESENT_PARTIAL + 2 CODE_CANDIDATE; 12-field 0/4/0/1/7=12; sequence semantics (dequeue order);
131 recorder tests pass; ADR-003 direct/ATAS split; CSV 25 unique / 0 malformed. **No owner decision
changes** — the recommended response stands, and `AUTH-D2-DIRECT-SRC` remains `HOLD` pending this
reconciliation (now delivered).
