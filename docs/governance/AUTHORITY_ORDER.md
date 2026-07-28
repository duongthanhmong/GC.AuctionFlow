# GCAE AUTHORITY ORDER (BINDING)

> Issued Round 1B, 2026-07-28. Closes reviewer BLOCKER **GOV-001**. This document,
> together with `CLAUDE.md`, is the authoritative statement of which source wins a
> conflict. It supersedes every earlier "priority order" wording, including the
> previous CLAUDE.md list that placed LOCKED source/tests and v1.3/STATUS above KDK.

## The order (highest first)

1. **Product Owner — current explicit objective and constraints.**
   The PO selects priorities and product scope (e.g. "GC first, ES/NQ later").
   The PO's directive governs *what to work on and in what order*.
   The PO may **not** silently redefine an AMT / Order Flow / Options concept in a
   way that contradicts KDK; a change to a domain meaning goes through
   `KDK_CHANGE_CONTROL.md`.

2. **KIM ĐẤU KINH (KDK) — highest domain authority.**
   `docs/spec/KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS.md` defines the *meaning* of every
   concept: the 9-tier evidence hierarchy (KDK v4 lines 344–363), the `*` convention
   (365–384), "never trade one signal" (389), and all domain invariants. When any
   other document conflicts with KDK on a domain meaning, **KDK wins** and the
   conflict is recorded in `SUPERSESSION_REGISTER.md`.

3. **Reviewer-issued specifications.**
   The reviewer translates KDK into implementation work: phase specs, acceptance
   gates, calibration/OOS requirements. This is the **only** legitimate way to turn
   a KDK concept into authorized scope. A reviewer spec may not contradict KDK; if
   it must, that is a KDK change-control event, not a silent override.

4. **Current architecture specifications (v1.2 / v1.3).**
   Binding **only where they do not conflict with KDK**. v1.2 governs architecture,
   module boundaries, the one-DLL rule, build/deploy discipline. v1.3 governs
   measurement contracts and anti-pattern guards. Where either conflicts with KDK on
   a domain meaning, KDK wins and the conflict is logged. These are *implementation*
   documents dependent on KDK, not domain authorities.

5. **IMPLEMENTATION_STATUS and historical phase records.**
   State of what is built and what phase is active. Evidence of progress, **not**
   domain authority. A phase marked LOCKED records that a gate passed at a point in
   time; it does not make the underlying behaviour immune from correction.

6. **Existing source code and tests.**
   **Evidence of current behaviour.** Enum names, policy versions, schemas and
   proven runtime behaviour are facts about *what the system does today* — they are
   not authority to change what a KDK concept *means*. A green test proves the code
   does what the test says; it does not prove the code matches KDK.

## Consequences that change prior practice

- **A LOCK tag is not immunity.** Historical git tags are preserved as history and
  are never moved, deleted or rewritten. But a domain error inside a LOCKED phase is
  still an error; supersession is documented, the tag is left intact.
- **Implementation constraint ≠ redefinition.** When a constraint prevents a KDK
  feature, mark it `NOT_IMPLEMENTED`, `BLOCKED`, or `AWAITING_DOMAIN_SPEC`. Never
  redefine the concept to fit the code.
- **Options is tier 7; GEX is a module inside Options.** Options never vetoes clear
  price acceptance (tier 4). GEX is one exposure module within the Options pillar,
  not the pillar itself (KDK v4 glossary line 396).
- **Ch 76 / 77 / 79** (Options-conditioned strategy families) are **in the target
  product scope** but `AWAITING_DOMAIN_SPEC` — not "out of scope", and not authorized
  for implementation until the reviewer issues a domain spec.

## What did NOT change

- The one installed artifact rule (`D-P0-02-002`): only `GC.AuctionFlow.dll`.
- No self-set thresholds (`G-CAL-001`).
- No auto order placement in current program scope.
- The task process (plan → audit → minimal implement → targeted + full tests →
  semantics check → scope check → status update → commit only at the right gate).

## Pointer

`CLAUDE.md` carries the short form of this order. On any discrepancy between a
transient instruction and this file, this file plus KDK govern; escalate to the PO.

## KDK v4 canonical update (2026-07-28)
Authority order §2 now points at the v4 canonical `docs/spec/KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS.md`
(repo SHA-256 `4cf22c02…`, 6382 lines; PO-supplied `51cbf108…`, 6309 lines, plus 2 authorized editorial
corrections — see `KDK_CHANGE_CONTROL.md`). Prior KDK line references (e.g. "hierarchy lines 323–341")
are superseded; the v4 evidence hierarchy is at lines 344–363. The v4 operating flow and ownership
boundaries (AMT = location/state/acceptance; Order Flow = executed effort/result; Options = risk
pricing/horizon/sensitive zones; Governance = permission; Execution = GC→CFD) are binding. "AMT and
Options prepare in parallel" is never a score-voting model.
