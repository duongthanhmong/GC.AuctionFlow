# KDK-CENSUS-001-R3.1 — bounded documentation & handover correction

**Corrects R3 (`e751f6e`) without overwriting R1/R2/R3.** Commit terminology:

| term | value |
|---|---|
| `AUDITED_SOURCE_HEAD` | `43d458f` |
| `R2_DELIVERABLE_COMMIT` | `e373486` |
| `R3_START_HEAD` | `e373486` (the R2 deliverable; **not** "R3 HEAD") |
| `R3_DELIVERABLE_COMMIT` | `e751f6e` |
| `CURRENT_HEAD` | `e751f6e` at R3.1 start → the R3.1 commit SHA is in **`handover_r3_1/POST_COMMIT_RECEIPT.md`** (the `.zip.sha256` receipt holds only the archive hash, NOT the commit SHA). Corrected by `KDK_CENSUS_001_R3_1_CORRECTION.md`. |

Worktree 131 untracked / 0 tracked-modified preserved. No `.cs` changed.

## 1. Commit terminology — corrected
`e373486` is the **R2 deliverable and R3 starting point**, never "R3 HEAD". The R3.1 commit SHA is
recorded externally (post-commit receipt), avoiding a self-referential SHA.

## 2. 25-module CSV — repaired
`KDK_25_MODULE_HIGHEST_PROVEN_STATE_R3_1.csv`:
- Restored full `class Symbol@path` for the 6 truncated rows (`MOD-04, 09, 11, 14, 17, 25`) from the
  R3E source; every path after `@` now equals one of that row's full `source_paths`.
- `MOD-03` reason reconciled to `R3E=CONFLICT` (was mistakenly "PARTIAL"); taxonomy unchanged.
- **Validation transcript** (`raw/R3_1_CSV_VALIDATION.txt`): 25 rows, `MOD-01…MOD-25` unique,
  **0 truncated, 0 malformed, 0 unmatched**, RESULT PASS.
- Counts unchanged, no upgrade from aggregate tests: **23 `CODE_PRESENT_PARTIAL` + 2
  `CODE_CANDIDATE_FOUND`**.

## 3. Artifact-fit — rebuilt from source semantics (`KDK_D2_ARTIFACT_FIT_003.md`)
Two `_002` classifications were technically wrong and are corrected:
- **`sequenceStats` → `ABSENT_REQUIRES_NEW_PRODUCER_STATE`.** Footer `FirstWriterSequence`/
  `LastWriterSequence` come from `RecorderGlobalLocalSequence` = **writer dequeue order only**
  (`RawEventEnvelope.cs:8`), which cannot produce native-sequence stats. Native sequence exists only
  on the direct-Rithmic leg; the ATAS leg is `NativeSequenceAbsent`.
- **`versions` → `ABSENT_REQUIRES_NEW_PRODUCER_STATE`.** `RecorderSchemaVersion`+int `ContainerVersion`
  ≠ the required `algorithmVersion`/`configVersion`/`dataSchemaVersion` triplet.

**Arithmetically consistent 12-field total:** `EXACTLY_EMITTED 0 · SEMANTICALLY_EQUIVALENT 4 ·
DERIVABLE_AT_FINALIZATION 0 · PARTIAL_DIFFERENT_SEMANTICS 1 · ABSENT_REQUIRES_NEW_PRODUCER_STATE 7 =
12`. Boundary explained: no `recorder_segment` JSON artifact exists; the data lives in the binary
`.gcae` header/footer (a different artifact), so nothing is `EXACTLY_EMITTED` in the JSON contract.

## 4. Binary-test label — corrected
**`TEST_EVIDENCED_WITH_DETERMINISTIC_SYNTHETIC_FIXTURES / NOT_LIVE_EVIDENCED`** (was wrongly "not
synthetic"). 131 passed / 0 failed / **0 skipped** (`raw/R3_RECORDER_TESTS.txt`). Fixtures:
handcrafted bytes (`SegmentReaderTests.BuildSegment`), real-`SegmentWriter` artifacts
(`RecorderCoreTests`, `TradeRecorderC3BcTests` — production-code-path), RecoveryScanner
truncation/corrupt (`P007C3BcCloseoutCorrectionTests`, `RecorderCloseoutAuditTests`); 0 spool-skips.
Using the real writer = **production-code-path evidence**, not live-market evidence.

## 5. ADR-003 direct vs ATAS — split
ADR-003 as written governs the **direct-Rithmic** recorder (native sequence). It is **not
automatically implementable on ATAS** (no native sequence; recorder-local sequence is dequeue order).
The ATAS leg needs an explicit integrity addendum (reconnect/restart/unknown-continuity/callback-loss/
queue-loss/missing-native-sequence → fail closed or `Unknown/Invalid`; never fabricate exchange
continuity from recorder-local sequence). Production authority split: **`AUTH-D2-DIRECT-SRC`** vs
**`AUTH-D2-ATAS-SRC` (HOLD until the addendum exists)**. Approving direct-path ADR-003 does not
authorize ATAS writer changes.

## 6. Handover — repository vs reviewer package (separated)
- **Repository at `e751f6e`:** `sha256sum -c SHA256SUMS.txt` = **40/40 OK** from a clean `git archive`
  checkout. The repo is **not** corrupt.
- **Reviewer's mixed attachment** (22 match / 2 mismatch / 16 missing) is a **stale partial package**:
  (a) stale `13_MANIFEST.md` `4dbc8bc3…` vs expected `263fccbc…`; (b) basename collision —
  `KDK_D2_CONTRACT_FIT_001(1).md` arrived while the unsuffixed attachment was stale.
- **Fix:** a uniquely-named R3.1 archive (`GCAE_CENSUS_R3_1_HANDOVER.zip`) containing every payload in
  its internal detached checksum, the **bannered** historical contract-fit under its correct unsuffixed
  name, the correct `13_MANIFEST.md`, raw recorder-test output, harness+report, corrected CSV, and the
  R3.1 review/artifact-fit/provenance/Owner-Pack + internal `RECEIPT.md`. The internal `SHA256SUMS.txt`
  does not hash itself; the archive's own hash is in an **external** `.sha256` receipt generated after
  the archive.

## Retained / narrowed / retracted (R3.1)
| item | disposition |
|---|---|
| 23 CODE_PRESENT_PARTIAL + 2 CODE_CANDIDATE; 0 built/wired/tested/live/accepted | **RETAINED** |
| 6 truncated CSV rows; MOD-03 reason | **REPAIRED** (validation PASS) |
| `sequenceStats`/`versions` "derivable" | **RETRACTED** → `ABSENT_REQUIRES_NEW_PRODUCER_STATE` |
| 12-field counts inconsistent across `_002`/R3 | **CORRECTED** → 0/4/0/1/7 = 12 |
| binary test "not synthetic" | **CORRECTED** → deterministic synthetic fixtures, NOT_LIVE_EVIDENCED |
| ADR-003 applied to both legs | **SPLIT** → direct-only; ATAS needs addendum; `AUTH-D2-ATAS-SRC = HOLD` |
| "R3 HEAD = e373486" | **CORRECTED** → R2 deliverable / R3 start |
