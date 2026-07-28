# GCAE SUPERSESSION REGISTER

> Round 1B, 2026-07-28. Records every known conflict among CLAUDE.md, v1.2, v1.3,
> README, IMPLEMENTATION_STATUS, the current KDK, and current code behaviour. History
> is not rewritten — conflicts are recorded and resolved forward. Git tags are never
> moved or deleted; supersession is documented here instead.
>
> Fields: `conflict_id | documents | conflicting statements | KDK requirement |
> current resolution | implementation impact | status`.

## SUP-001 — Authority order (BLOCKER GOV-001)
- **documents:** CLAUDE.md (pre-1B) vs KDK vs AUTHORITY_ORDER.md
- **conflicting statements:** old CLAUDE.md — "KDK KHÔNG quyết định scope; khi KDK vs
  v1.3/STATUS thì v1.3 và STATUS thắng; source/tests LOCKED đứng số 1". KDK/AUTHORITY_ORDER
  — KDK is the highest **domain** authority; code/tests are evidence of behaviour, not
  authority over meaning.
- **KDK requirement:** KDK is the grounding domain source (whole document; hierarchy
  lines 323–341).
- **current resolution:** CLAUDE.md rewritten; `AUTHORITY_ORDER.md` issued. Order is
  PO → KDK → reviewer spec → v1.2/v1.3 (non-conflicting) → STATUS → code/tests.
- **implementation impact:** No domain code may be changed until the corrected order
  is in force (this closes the blocker). No behaviour change by itself.
- **status:** RESOLVED (docs only; awaiting reviewer sign-off).

## SUP-002 — GEX vs the Options pillar
- **documents:** CLAUDE.md/v1.3 (GEX-centric wording) vs KDK glossary (line 373)
- **conflicting statements:** older wording treated "GEX" as the Options feature. KDK —
  "GEX là một mô-đun exposure bên trong trụ Options"; the Options pillar also includes
  IV structure, activity, public positioning, scenario exposure (glossary line 372).
- **KDK requirement:** Ch44 (pillar role/architecture); glossary 372–373.
- **current resolution:** CLAUDE.md and roadmap phase C now say GEX is one module inside
  the Options pillar; the pillar is broader.
- **implementation impact:** Options rebuild (roadmap C) must model the full pillar, not
  just GEX. No production change under freeze.
- **status:** RESOLVED_IN_DOCS / IMPLEMENTATION_PENDING.

## SUP-003 — Ch 76/77/79 "forbidden/out-of-scope" vs "in-scope, awaiting spec"
- **documents:** CLAUDE.md (pre-1B "Vẫn cấm họ chiến lược Ch76/77/79") vs KDK Part VII vs reviewer
- **conflicting statements:** old CLAUDE.md framed them as forbidden extras. Reviewer/KDK —
  they are in the **target product scope** but `AWAITING_DOMAIN_SPEC`, unauthorized until a
  reviewer domain spec exists.
- **KDK requirement:** Ch76, Ch77, Ch79 (Options-conditioned strategy families).
- **current resolution:** CLAUDE.md + catalog (02A/02B) + roadmap now mark them
  `AWAITING_DOMAIN_SPEC`, in-scope, unauthorized.
- **implementation impact:** No implementation until spec; status is not "out of scope".
- **status:** RESOLVED_IN_DOCS.

## SUP-004 — Phase 5 "FINAL PASS / LOCKED" vs actual state
- **documents:** IMPLEMENTATION_STATUS (earlier "Phase 5 FINAL PASS/LOCKED") vs audit reality
- **conflicting statements:** STATUS claimed a locked, complete Options pillar. Reality —
  a **display-only** GEX overlay whose analytics are invalidated by OPT-001..006; the live
  check validated the §50 null-invariant and display, not a calibrated pillar.
- **KDK requirement:** Ch45–49 (data QC, per-expiry identity, scenario exposure).
- **current resolution:** Claim retracted during Round 1 (report 02C). "FINAL PASS" reserved
  for a calibrated + OOS + live-accepted pillar; current state is DISPLAY_ONLY /
  IMPLEMENTED_BUT_INVALIDATED.
- **implementation impact:** Options must not be treated as decision-grade; roadmap C rebuild.
- **status:** RESOLVED_IN_DOCS (STATUS wording correction pending under authorized doc scope).

## SUP-005 — Options analytics rendered vs KDK correctness
- **documents:** code behaviour (research/optionflow/*, DLL overlay) vs KDK Ch45–49
- **conflicting statements:** the sidecar computes and the DLL renders GEX/flip/regime/EM as
  if valid. KDK requires per-(expiry,strike,underlying) identity, per-expiry IV/skew, and
  quote-quality gating — all violated by OPT-001..006 (report 03).
- **KDK requirement:** Ch45 (mapping+QC), Ch46 (per-expiry structure), Ch48 (scenario exposure).
- **current resolution:** Options analytics classified `IMPLEMENTED_BUT_INVALIDATED` /
  `BLOCKED_BY_DEFECT`; display retained, conclusions not trusted.
- **implementation impact:** Roadmap C (schema v2, identity+QC) before any Options conclusion.
- **status:** OPEN (defects logged, fix deferred to authorized phase).

## SUP-006 — "dealer_positioning" asserted as fact vs no-participant-identity
- **documents:** code (analytics.py dealer_positioning) vs KDK Ch4/Ch5/Ch48
- **conflicting statements:** the sidecar labels a computed signed exposure as observed
  "dealer positioning". KDK — do not assert participant identity as fact; signed exposure must
  carry {scenario, assumption, formula, unit, confidence}.
- **KDK requirement:** Ch4 (participants), Ch5 (evidence provenance), Ch48 (scenario exposure).
- **current resolution:** logged as OPT-005 (MAJOR); relabel to scenario exposure in roadmap C.
- **implementation impact:** naming + schema change under authorized Options phase.
- **status:** OPEN.

## SUP-007 — Aggressor-side (earlier claim vs actual code)
- **documents:** an earlier audit note ("still reads IsAsk||IsBid") vs code EpisodeTradeEvent.cs:75
- **conflicting statements:** the note implied an assumed aggressor. Code actually uses
  `TradeAggressorSide.Resolve(obs.Direction, …)` — a measured resolution.
- **KDK requirement:** Ch21 (aggressor must be measured, never assumed).
- **current resolution:** corrected in report 02; code **matches** KDK here.
- **implementation impact:** none (alignment confirmed).
- **status:** RESOLVED_NO_CONFLICT.

## SUP-008 — TPO parity DISAGREED vs measurement-correct claim
- **documents:** parity artifact (DISAGREED) vs any "profile measurement correct" claim
- **conflicting statements:** a TPO parity comparison item is still open/DISAGREED while
  profiles are presented as measurement-grade.
- **KDK requirement:** Ch7 (TPO by a stated measure).
- **current resolution:** Ch7 rows carry the known-defect tag; status PARTIAL for exact POC/VA.
- **implementation impact:** resolve parity before calling TPO measurement-locked (roadmap B).
- **status:** OPEN.

## SUP-009 — Build provenance (deployed vs clean-build vs LOCK artifact)
- **documents:** IMPLEMENTATION_STATUS/LOCK record vs artifact hashes
- **conflicting statements:** deployed DLL `4c7b5f09…` ≠ clean-build `1526e42f…` ≠ LOCK
  artifact `7d203115…`. Provenance of the running binary is not reproducibly established.
- **KDK requirement:** Ch5 (data/evidence integrity — provenance).
- **current resolution:** Recorded as a provenance BLOCKER (report 01/02C). Archive-binary
  equality (archive Release DLL == deployed) is proven; clean reproducibility is not.
- **implementation impact:** Roadmap A (Foundation & Release Integrity) — deterministic build.
- **status:** OPEN.

## SUP-010 — "Đọc ba tài liệu" omits KDK as primary
- **documents:** CLAUDE.md header ("read v1.2, v1.3, STATUS before every task")
- **conflicting statements:** header listed three implementation docs as the pre-task read,
  with KDK as secondary "background".
- **KDK requirement:** KDK is the domain grounding source.
- **current resolution:** CLAUDE.md now places KDK as the highest domain authority; header
  retains the three docs as the implementation-state read, KDK as domain authority.
- **implementation impact:** none behavioural.
- **status:** RESOLVED_IN_DOCS.

---

### Alignments (recorded, no conflict)
- **v1.3 §50 ↔ KDK Ch50 / hierarchy line 339:** both say Options never vetoes clear price
  acceptance and GEX is never a necessary condition. **ALIGNED.**
- **G-CAL-001 ↔ KDK "không tự đặt threshold":** both forbid self-set thresholds. **ALIGNED.**
- **One-DLL (D-P0-02-002) ↔ KDK:** no domain conflict. **ALIGNED.**

### Maintenance
New conflicts are appended with the next `SUP-NNN`. A conflict is only closed to
`RESOLVED_*` when either the docs are corrected (doc-only) or the authorized phase lands the
fix with tests + reviewer sign-off. `OPEN` items are the domain-correctness backlog.
