# KDK — Control-File Provenance Matrix R3

**Supersedes the R2 matrix (retained).** R3 HEAD `e373486`. Hashes re-checked directly.

| file / version | supplied hash | on disk here | repo path | first commit w/ exact content | authority | classification (R3) |
|---|---|---|---|---|---|---|
| `KDK_CENSUS_001_R1_REVIEW.md` | `ba404d93…` | YES | `…/audit_census_001/` | `368d168` | audit output (mine) | current |
| `KDK_UNBLOCK_001_OWNER_DECISION_PACK.md` | `19e7e362…` | YES | same | `368d168` | proposal (mine) | superseded by R2→R3 |
| **`BAN_THAO(1).md` v1.2** | `0e24349b…` | **NO** (searched all `.md` under Downloads/Desktop/Documents/repo) | not materialized | **none** | external control input | **`RECEIVED_HASH_VERIFIED_EXTERNAL_INPUT` (by reviewer) / `NOT_REPO_MATERIALIZED` (bytes unavailable in this workspace) / `NOT_OWNER_ADOPTED`** |
| **`TIEN_DO(1).md` v1.2** | `d24ec88b…` | **NO** | not materialized | **none** | external control input | same as above |
| execution brief | `26ad6e23…` | **NO** | not materialized | **none** | external | **`RECEIVED_RECORDED_SOURCE_FILE_UNAVAILABLE_AT_AUDITED_HEAD`** (unchanged) |
| `docs/BAN_THAO.md` (tracked) | Chặng-based | YES | `docs/BAN_THAO.md` | `43d458f` | proposal (mine) | superseded by v1.2 external |
| `docs/TIEN_DO.md` (tracked) | — | YES | `docs/TIEN_DO.md` | `43d458f` | proposal (mine) | superseded by v1.2 external |

## Materialization outcome (R3 step 1)

**The two v1.2 control files were NOT materialized into the repo — because their exact bytes are not
present in this workspace.** They were supplied to the **reviewer** and hash-verified **there**; they
did not reach this environment. Confirmed by content-hash search: no `.md` under Downloads / Desktop /
Documents / repo matches `0e24349b…` or `d24ec88b…`.

**Consequence for R3 step 1:** the instruction was conditional — *"If available in your workspace,
copy them."* They are **not** available here, so:

- I cannot preserve their exact bytes (I don't have them).
- I cannot produce an R3 proposed derivative that changes the v1.2 heading *"Work Breakdown Structure
  canonical" → "Proposed canonical WBS"*, because I have never seen the v1.2 body to derive from.
- The `r2/{TIEN_DO,BAN_THAO}_R2_PROPOSED.md` deltas stand as the best available — they use the WP
  semantics stated in the task instructions, flagged, without fabricating v1.2 text.

## Required delivery action (not a decision)

To materialize the v1.2 inputs (and the execution brief), the owner/reviewer must **commit the actual
files into the repo** or place them in this workspace. This is `DEC-DELIVER` in the owner pack — a
document-delivery action, explicitly separate from `NOT_OWNER_ADOPTED`. Until delivered, their status
remains `RECEIVED_HASH_VERIFIED_EXTERNAL_INPUT / NOT_REPO_MATERIALIZED / NOT_OWNER_ADOPTED`.

**No file was dismissed. None was upgraded to canonical or owner-adopted.**
