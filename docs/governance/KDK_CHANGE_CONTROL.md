# KDK CHANGE CONTROL

> Round 1B, 2026-07-28. KDK (`docs/spec/KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS.md`) is the
> highest domain authority (see `AUTHORITY_ORDER.md`). A domain *meaning* may be changed
> only through this controlled process. Nobody — Product Owner, Claude, reviewer, code,
> tests, or an older document — may silently redefine an AMT / Order Flow / Options concept.

## What this governs
A "KDK change" is any alteration to the **meaning** of a domain concept: an invariant, an
evidence-tier placement, a classification definition, a prohibition, the `*` convention, or
a measurement contract's semantics. It is **not**:
- choosing priorities or product scope (that is the PO's, per AUTHORITY_ORDER §1);
- marking a feature `NOT_IMPLEMENTED` / `BLOCKED` / `AWAITING_DOMAIN_SPEC` because of an
  implementation constraint (that is expected and requires no KDK change);
- a reviewer spec that *translates* KDK into implementation work without contradicting it.

## The rule when code and KDK disagree
Code and tests are **evidence of current behaviour**, never authority over meaning. If the
code does something KDK does not sanction:
1. The code is treated as **wrong or incomplete**, not KDK.
2. Log the conflict in `SUPERSESSION_REGISTER.md` (`SUP-NNN`).
3. Mark the affected requirement `BLOCKED_BY_DEFECT` / `IMPLEMENTED_BUT_INVALIDATED` /
   `NOT_IMPLEMENTED` in `02B_KDK_REQUIREMENT_TRACEABILITY.csv`.
4. Fix under an authorized phase — do **not** edit KDK to match the code.

## Process to change a KDK meaning (rare)
1. **Proposal** — written change request: the exact KDK text/line range, the proposed new
   meaning, and the domain rationale grounded in market-auction / options theory.
2. **Impact analysis** — list every `KDK-CHxx-REQ-yyy` affected, every SUP conflict touched,
   and every module/schema impacted.
3. **Reviewer domain review** — the independent reviewer checks domain correctness against
   the evidence hierarchy and the `*` convention. Options changes additionally check the
   tier-7 non-veto invariant.
4. **Product Owner ratification** — the PO accepts or rejects scope/priority implications.
5. **Record** — on acceptance: amend KDK with a dated change note, add a `SUP-NNN` supersession
   entry, bump the catalog rows, and note the change in `CLAUDE.md` if it affects an invariant.
   The old KDK text is preserved in history (never silently overwritten).

## Hard constraints (cannot be waived by this process)
- The 9-tier evidence hierarchy ordering (KDK v4 344–363) and "lower never vetoes higher".
- Options (tier 7) never vetoes clear price acceptance (tier 4); GEX is a module inside Options.
- `*` content never creates entry/size/direction and never vetoes price acceptance pre-validation.
- "Never trade one signal" (evidence + invalidation + target space + reliable data).
- No self-set thresholds (`G-CAL-001`); nothing is `CALIBRATED`/`OOS_VERIFIED` without an artifact.

## Audit trail
Every KDK change and every code-vs-KDK conflict resolution is traceable through:
`SUPERSESSION_REGISTER.md` (the conflict), the catalog CSVs (the requirement status), and the
git history of KDK itself (the text change). No branch of this trail may be skipped.

---

## KDK v4 canonical adoption record (Product Owner, supersedes v3)

- **Supplied canonical (PO):** `KIM_DAU_KINH_CHUYEN_SAU_OPTIONS.md`
  SHA-256 `51cbf1087b252fdaf1d5db83784de25b100b677b59b54775618396a7d2547c76`, 6309 lines.
- **Repository canonical path (stable):** `docs/spec/KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS.md`
  (no `(3)`/`(4)` in the filename).
- **Two authorized editorial corrections applied:**
  1. Ch51 new block — removed 18 accidental Markdown escapes (`\####`, `\###`, `\-`, `\*\*`),
     local only, no global reformat.
  2. Ch50 — inserted the approved section **"Giao dịch phản ứng tại vùng Options"** (73 lines)
     between "Options trong sự kiện" and "Khi Options xung đột với giá".
- **Repository canonical after corrections:**
  SHA-256 `4cf22c028d682a997e73571462b3579aab64f996cfdc0d00f886a47e529f8c5c`, 6382 lines.
- **Change size (for the record):** the two corrections together are **91 additions, 18 deletions,
  net +73 lines** (196 diff-content lines): the 73-line Ch50 section added, plus the 18 escaped Ch51
  lines replaced by their 18 unescaped forms. (Do not describe this as "71 added / 18 removed".)
- **Permanent provenance (canonical, does not depend on any review artifact):** the PO-supplied hash
  `51cbf108…` (6309 lines), the corrected repository hash `4cf22c02…` (6382 lines), the two-correction
  description above, and the Git history of `docs/spec/KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS.md` together
  form the immutable provenance record. The focused editorial diff (`docs/review/kdk_v4/KDK_V4_EDITORIAL.diff`)
  is **review evidence only** and is **not** a canonical dependency; `docs/review/` is not committed source
  and this record must remain valid without it.
- **Why the hashes differ:** the PO hash is the raw supplied file; the repo hash reflects the two
  authorized editorial corrections above (unescape + Ch50 section), and nothing else.
- **Pre-existing note (NOT fixed, out of authorized scope):** line 367 (front-matter star-convention)
  carries a similar `\*\*…\*\*` escape; flagged for a separate decision, not touched here.
- The previous KDK requirement count (669) and its line references are **no longer assumed valid**;
  the catalog is regenerated from this canonical file before any Phase B/C work.
