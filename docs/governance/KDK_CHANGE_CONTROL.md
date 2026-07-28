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
- The 9-tier evidence hierarchy ordering (KDK 323–341) and "lower never vetoes higher".
- Options (tier 7) never vetoes clear price acceptance (tier 4); GEX is a module inside Options.
- `*` content never creates entry/size/direction and never vetoes price acceptance pre-validation.
- "Never trade one signal" (evidence + invalidation + target space + reliable data).
- No self-set thresholds (`G-CAL-001`); nothing is `CALIBRATED`/`OOS_VERIFIED` without an artifact.

## Audit trail
Every KDK change and every code-vs-KDK conflict resolution is traceable through:
`SUPERSESSION_REGISTER.md` (the conflict), the catalog CSVs (the requirement status), and the
git history of KDK itself (the text change). No branch of this trail may be skipped.
