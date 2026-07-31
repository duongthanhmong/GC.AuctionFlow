# 08 — Contradiction Register

**Audited commit:** `43d458f`. Every row: a claim in a project document that repo state at this
HEAD contradicts. Rejected/superseded matrices are treated as void.

| id | claim | claim ref | verified reality | reality ref | severity |
|---|---|---|---|---|---|
| **CTR-01** | "no persistence implementation exists; the schemas define the contract, **nothing writes to it**" (D2) | `STAGE2_CHECKLIST.md:69` | `SegmentWriter.cs` (411 lines) + `ManifestWriter.cs` (35) + `RecoveryScanner.cs` (546) exist and are tracked; `SegmentWriter` is constructed and wired | `Recorder/SegmentWriter.cs:92`, `RawEventRecorderSession.cs:92`, `GcAuctionFlowIndicator.cs:3166` | **HIGH** |
| **CTR-02** | "NOT_IMPLEMENTED 589 · PARTIAL 2 · DISPLAY_ONLY 1" | `ADOPTION_CLOSEOUT.md:38-39` | 02B measured: NOT_IMPLEMENTED **591**, PARTIAL **0**, DISPLAY_ONLY **0**, CODE_TESTED **1** | `02B_KDK_REQUIREMENT_TRACEABILITY_V4.csv` (679 rows) | MEDIUM |
| **CTR-03** | "1458 passed / 0 failed" (also "1461") | `IMPLEMENTATION_STATUS.md:47` | **1531 passed / 0 failed / 0 skipped** | `raw/TEST.txt`, exit 0 | MEDIUM (stale) |
| **CTR-04** | header "(GEX out of scope)" | `IMPLEMENTATION_STATUS.md:4` | line ~69 marks Phase 5 OptionFlow "AUTHORIZED (operator 2026-07-28)"; CLAUDE.md GOV-002 supersedes the LOCK language | `IMPLEMENTATION_STATUS.md:69`, `CLAUDE.md` GOV-002 | MEDIUM (internal) |
| **CTR-05** | Phase 5 "5E GC live accept — FINAL PASS / LOCKED" | `IMPLEMENTATION_STATUS.md` | `CLAUDE.md` GOV-002: "HISTORICAL / SUPERSEDED ... Options pillar not complete; do not present Phase 5 as a finished Options pillar" | `CLAUDE.md` (Phase 5 section) | MEDIUM |
| **CTR-06** | requirement/MRBS term `StructuralAnalysisSnapshot` (S41) | `MRBS_v1.1` S41, 02B | symbol does **not exist** in `src/` (0 files); implemented type is `GcaeRuntimeSnapshot` | `grep src/ = 0`; `GcaeRuntimeEngine.cs:276` | LOW (rename, not absence) |
| **CTR-07** | `IMPLEMENTATION_STATUS.md` current-phase narrative | doc `:8` | doc **stops 2026-07-28**; no entry for KDK v4 adoption, MRBS work, or the entire WP-L1-RITHMIC branch — stale relative to HEAD | `IMPLEMENTATION_STATUS.md:1752-1778` (ends at Phase A) | MEDIUM |
| **CTR-08** | earlier drafts cite alignment-matrix R1 line numbers | (superseded TIEN_DO/BAN_THAO drafts) | R1 is REJECTED; current is R3E | `mrbs_adoption_r2/README_R2.md:3` | LOW (already corrected in `43d458f`) |

## Must-reconcile (not contradictions, but numeric drift to fix in trackers)

- Test count 1458/1461 → **1531** (CTR-03).
- 02B distribution in closeout → measured (CTR-02).

## Note on CTR-01 (the load-bearing one)

The Stage 2 checklist's D2 line was written when the writer did not exist, or was written without
re-checking `src/`. At `43d458f` the writer code is **present and runtime-wired**. This does **not**
make D2 "done": `CODE_PRESENT + RUNTIME_WIRED` ≠ `LIVE_EVIDENCED` under sustained load. The correct
D2 state is **`CODE_PRESENT / RUNTIME_WIRED / D1-UNPROVEN`**, not "nothing writes." The checklist
understates the code and overstates the openness in the same line.
