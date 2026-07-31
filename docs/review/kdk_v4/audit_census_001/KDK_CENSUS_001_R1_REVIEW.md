# KDK-CENSUS-001-R1 — Targeted Correction Review

**Corrects (does not overwrite) the census committed at `323e36a`, which audited HEAD `43d458f`.**
**R1 run HEAD:** `323e36a` · branch `wp-l1-rithmic-data-surface-discovery` · worktree 131 untracked,
0 tracked-modified — preserved. The historical census artifacts (`01`…`13`) are left intact; this
addendum and the `r1/` corrected artifacts supersede specific claims, cited below.

Every count here was re-measured directly at this HEAD. Raw commands: `raw/R1_COMMANDS.txt`.

---

## What survived (census conclusions confirmed at HEAD)

| conclusion | status |
|---|---|
| build: `Build succeeded` 0/0, exit 0 | **CONFIRMED** |
| tests: **1531 passed** / 0 failed / 0 skipped (claim of 1458 is stale) | **CONFIRMED** |
| 248 tracked `.cs` src, 79 tests, 563 tracked files; 23 untracked = legacy `Oac.*` | **CONFIRMED** |
| 679 requirements in 02A/02B; 134 parameters; **0 approved**; 2 in code | **CONFIRMED** |
| 15 EvidenceIds re-hash: 15 match, 0 drift | **CONFIRMED** |
| D2 writer code **exists and is wired** (`SegmentWriter` 411L @ `RawEventRecorderSession.cs:92`) — contradicts STAGE2_CHECKLIST "nothing writes" | **CONFIRMED** |
| `RecoveryScanner` (546L) has **no production caller** (tests only) | **CONFIRMED** |
| No signed owner acceptance record exists anywhere | **CONFIRMED** |
| Runtime chain is wired end-to-end; stops at `NotCalibrated` acceptance gate by design | **CONFIRMED** |
| DLL ingress is via ATAS, not direct Rithmic | **CONFIRMED** |

## What was RETRACTED or NARROWED

| # | census claim | correction |
|---|---|---|
| **D1** | "the domain is almost entirely unbuilt" (Exec, artifact 01) | **RETRACTED.** 02B's 591 `NOT_IMPLEMENTED` rows carry **zero** `source_files`/`source_symbols` — they are **`UNMAPPED_IN_02B`** (traceability gap), not proven code-absence. The code-focused R3E matrix maps **23 of 25 modules to real cited symbols** (22 PARTIAL + 1 CONFLICT). Corrected conclusion: **the domain is largely BUILT at module granularity, almost entirely UNTRACED at requirement granularity, and entirely UNCALIBRATED.** See `r1/03_CODE_TO_REQUIREMENT_R1.md`. |
| **D2** | invented `WP-00…WP-10` slots, then mapped them onto B0–B6 | **RETRACTED.** `WP-00…WP-10` exist **only in the census's own artifact 10** — not in BAN_THAO/TIEN_DO (tracked at `43d458f`), which use **"Chặng 0–5"**. The canonical WBS is **Chặng 0–5** (BAN_THAO) + phase-owner **B0–B6/C/D** (R3 package). No invented mapping. See `r1/10_WP_PROGRESS_R1.md`. |
| **D3** | brief "missing after hashing 409 archives" stated as fact | **NARROWED to `SOURCE_AVAILABILITY_CONFLICT`.** No repo progress document ever recorded it as `RECEIVED` (the only `26ad6e23` mentions are the census's own artifacts). The owner asserts it was received; this workspace has never contained a file with that hash across repeated searches. Neither side is dismissed. GOV-Q1 corrected below. |
| **D6** | "383 runtime guards" | **RETRACTED.** 383 = **textual** occurrences across **68 files**: **84** enum/const declarations + **45** comments + **10** control-flow returns/assignments + remaining references. Not 383 guards. Corrected classification in `r1/06_PARAMETER_HARDCODE_R1.md`. |
| **D7** | `GcaeRuntimeSnapshot` presented as a proven rename of `StructuralAnalysisSnapshot` | **RETRACTED.** MRBS §41 `StructuralAnalysisSnapshot` is a **REVIEW-REQUIRED proposed** Analysis-Only Output Contract, conditional on an owner decision to split CFD from core (**not made**). `GcaeRuntimeSnapshot` is a richer UI-diagnostic aggregate of per-module set-snapshots. Same domains, different shape/purpose — **not a rename**. Field comparison in `r1/04_RUNTIME_WIRING_R1.md`. |

## Defects that did NOT reproduce at HEAD

| # | premise | reality |
|---|---|---|
| **D8** | "STAGE2_CHECKLIST labels ADR-001…007 `ACCEPTED` while files say `AWAITING`" | **Does not reproduce.** At `43d458f` the checklist **already** says `AWAITING_OWNER_APPROVAL` for all 7 (fixed in `8160f2c`), matching the ADR files. The contradiction was resolved before this audit. Taxonomy still clarified in the Owner Pack. |

## Defects fixed by construction

| # | defect | fix |
|---|---|---|
| **D4** | manifest embeds its own mutable hash (`2412557a…`/1395 vs actual `4dbc8bc3…`/1838) | Replaced with detached **`SHA256SUMS.txt`** hashing all bundle files (including `13_MANIFEST.md`) after finalization. `13_MANIFEST.md` no longer claims its own hash. |
| **D5** | `12_RAW_TRANSCRIPT.md` was a summary | Full commands + exit codes + logs added to `raw/` (`BUILD.txt`, `TEST.txt`, `R1_COMMANDS.txt`); transcript points to them. |

## Corrected governance item

**GOV-Q1 (was: "brief never arrived"):** → **`SOURCE_AVAILABILITY_CONFLICT`.** The execution brief
(SHA-256 `26ad6e23…`, incl. §15) is asserted RECEIVED by the owner but has never been present in
this workspace across repeated content-hash searches (most recent: 409 archives, no match). It is
not committed to the repo at any reachable commit. **Resolution requires the owner to commit the
file to the repo** (as KDK/MRBS/Registry were), or to confirm it was delivered only to a different
environment. Until then §15's report format cannot be honored — stated as a conflict, not a denial.
