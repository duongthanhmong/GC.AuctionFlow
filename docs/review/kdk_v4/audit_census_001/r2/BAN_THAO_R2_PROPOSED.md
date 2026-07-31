# BAN_THAO — R2 proposed delta (NOT owner-adopted)

**Status: `PROPOSAL_PENDING_OWNER_ADOPTION`.** R2 HEAD `c94ed8b`.

> **Base-availability note.** Same as the TIEN_DO R2 delta: the supplied `BAN_THAO(1).md` v1.2
> (`0e24349b…`) is **not materialized** here, so this is an evidence-backed delta using the WP
> semantics from the R2 instruction, not a merge of unseen v1.2 text. Does not overwrite tracked
> `docs/BAN_THAO.md`.

## WBS ↔ execution-lane alignment (both axes, no collapse)

| delivery WP (v1.2 proposal) | execution lane (tracked) | phase-owner |
|---|---|---|
| WP-00/01 foundation + AMT | Chặng 0–1 | B0, B1 |
| WP-02 census | — (governance) | — |
| WP-03 Stage 2 | Chặng 0 (data) | B0 |
| **WP-04 writer** | Chặng 0 (D2) | B0 |
| WP-05 acceptance/FAR-AAC | Chặng 2 | B3, B5 |
| WP-06 dataset | cross-cutting | Phase D/governance |
| WP-07 Options | Chặng 4 (parallel lane) | Phase C |

## The WP-04 architecture conflict (must reach the owner)

`BAN_THAO` v1.2 defines **WP-04 = direct-Rithmic contract-compliant writer (Option A)**. The Owner
Pack recommends **Option C (hybrid)**. **Unresolved control conflict** — surfaced, not reinterpreted.
If the owner picks C, WP-04 must be amended (`DEC-WP04`): `WP-04a` direct research writer + `WP-04b`
ATAS live writer + capability manifest. See Owner Pack R2 §3.

## Options parallel-lane policy — preserved

Per KDK 2447, AMT and Options prepare in **parallel** (different questions). No sequential dependency
is imposed. Options (WP-07 / Chặng 4 / Phase C) may proceed on its own lane subject to Q5 (`.proto`)
and Q6/Q10 (Ch76/77/79 + 04A), independently of the core recorder lane.

## Binding-law reminders (unchanged, from tracked BAN_THAO §0)

1. Tower authority (KDK 359) — evidence authority, not build order.
2. No self-set thresholds (MRBS §44 + §24.3; 0/134 approved).
3. SPEC_CONFLICT ⇒ stop, don't self-resolve (MRBS line 62).
4. No 14 setups as 14 independent logics **at Phase 1** (MRBS 850).

**This delta binds nothing.** It is a proposal; owner adoption is required to make WP-00…WP-10 canonical.
