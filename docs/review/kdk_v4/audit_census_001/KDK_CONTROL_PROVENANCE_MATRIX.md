# KDK — Control-File Provenance Matrix

**R2 HEAD `c94ed8b`.** Resolves which control/review files are real, where, at what authority.
Hashes verified directly. "First commit with exact content" via `git log`.

| file / version | supplied hash | on disk (workspace path) | repo path | first commit w/ exact content | status @ `43d458f` | status @ HEAD `c94ed8b` | authority | classification |
|---|---|---|---|---|---|---|---|---|
| `KDK_CENSUS_001_R1_REVIEW.md` | `ba404d93…` | **YES** — `…/audit_census_001/KDK_CENSUS_001_R1_REVIEW.md` | same | `368d168` | not present | **present, hash-matches supplied** | audit output (mine) | **current** |
| `KDK_UNBLOCK_001_OWNER_DECISION_PACK.md` | `19e7e362…` | **YES** — `…/audit_census_001/` | same | `368d168` | not present | **present, hash-matches supplied** | proposal (mine) | **current, superseded by R2 pack** |
| `BAN_THAO(1).md` v1.2 | `0e24349b…` | **NO** — not found on disk (hashed all `.md` under Downloads/Desktop/Documents/repo) | `docs/BAN_THAO.md` holds a **different** v (Chặng-based) | **none** — this exact content is in no commit | v1.1-equiv (Chặng 0–5) tracked, **different hash** | **not materialized** | external control proposal | **external / proposed, NOT owner-adopted** |
| `TIEN_DO(1).md` v1.2 | `d24ec88b…` | **NO** — not found on disk | `docs/TIEN_DO.md` holds a **different** v | **none** | tracked, different hash | **not materialized** | external control proposal | **external / proposed, NOT owner-adopted** |
| `docs/BAN_THAO.md` (tracked) | n/a (Chặng-based) | YES | `docs/BAN_THAO.md` | `43d458f` | present | present | proposal (mine) | superseded by v1.2 external |
| `docs/TIEN_DO.md` (tracked) | n/a | YES | `docs/TIEN_DO.md` | `43d458f` | present | present | proposal (mine) | superseded by v1.2 external |

## Resolution statements

1. **The two R1 review artifacts the owner supplied ARE the files I committed** — hashes match
   `ba404d93…` / `19e7e362…` exactly. My R1 output is what is under review. Confirmed.

2. **`BAN_THAO(1).md` v1.2 and `TIEN_DO(1).md` v1.2 are NOT in this workspace.** Searched by exact
   SHA-256 across every `.md` in Downloads / Desktop / Documents / repo — **no match**. Their exact
   content is in **no commit**. Per the R2 instruction they are **not dismissed**: they are recorded
   as the **current external control proposal** whose file has not been materialized here
   (`SOURCE_AVAILABILITY_CONFLICT`, same class as the execution brief).

3. **Neither v1.2 file is owner-adopted.** `BAN_THAO` remains `PROPOSAL_PENDING_OWNER_ADOPTION`;
   detailed progress remains provisional pending review. This matrix does **not** upgrade them.

4. **Authority ceiling:** the highest authority any of these files carries today is *proposal /
   external review input*. None is `OWNER_APPROVED` or `FORMALLY_ACCEPTED`.

## Action required for reproducibility (not a decision — a delivery)

To use the v1.2 content as the base for the proposed `TIEN_DO`/`BAN_THAO` updates, the owner must
**materialize `BAN_THAO(1).md` and `TIEN_DO(1).md` into the repo** (commit them), exactly as
KDK/MRBS/Registry were delivered. Until then the R2 proposed-update documents (`r2/…`) use the WP
semantics **stated in the R2 task instruction**, flagged as such, and cannot merge v1.2 body text
that has never been present.
