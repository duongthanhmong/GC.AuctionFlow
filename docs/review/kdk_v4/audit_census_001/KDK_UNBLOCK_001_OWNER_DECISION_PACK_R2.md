# KDK-UNBLOCK-001 — Owner Decision Pack R2

**Supersedes the R1 pack (`19e7e362…`).** Decision-grade. Nothing self-approved. R2 HEAD `c94ed8b`.
Evidence for the ADRs is the **direct-Rithmic probe evidence (15 EvidenceIds)** + the **47/47 Stage 2
Python schema suite** — **not** the 1531 C# tests (those cover the ATAS-path recorder, a different
subsystem).

---

## 1. ADR decisions — independent per ADR

| ADR | authoritative artifact | direct evidence | gates which task | deferral blocks |
|---|---|---|---|---|
| **001** core = trades+BBO+aggr depth | `adr/ADR-001-core.md` | `EV-PROBE-L1-TRADES`, `EV-PROBE-ENTITLEMENT`, `EV-PROBE-AGG-BOOK` | D2 writer scope | direct, ATAS, hybrid |
| **002** MBO = telemetry | `adr/ADR-002-mbo.md` | `EV-REBUILD-DIVERGES` (3/21 levels) | MBO handling | only later Options/MBO calibration |
| **003** gap recovery absent → fail-closed | `adr/ADR-003-gaps.md` | `EV-RECONNECT-GAP` (Δ3975) + schema tests C5/C6 (Stage 2 suite) | **D2 fail-closed writer** | direct, ATAS, hybrid |
| **004** options OI client-blocked | `adr/ADR-004-options.md` | `EV-EXPLOIT-OPTION-*`, `EV-UNMAPPED-OI-155-157` | Options sidecar | **Options only** |
| **005** no seed hard-coded | `adr/ADR-005-params.md` | `EV-PARAM-REGISTRY`, r1/06 | all calibration | acceptance/calibration only |
| **006** hist/live separate capability | `adr/ADR-006-hist-live.md` | `EV-HIST-PARITY` (76/77) | dataset assembly | direct, hybrid |
| **007** preserve unparseable frames | `adr/ADR-007-raw-frames.md` | `EV-UNMAPPED-OI-155-157`, schema tests C8/C9 | raw retention | direct, hybrid (Options recovery) |

**Response — one per ADR (not a bundle):**
```
ADR-001: [ APPROVE / REJECT / DEFER ]
ADR-002: [ APPROVE / REJECT / DEFER ]
ADR-003: [ APPROVE / REJECT / DEFER ]     ← gates D2 fail-closed; blocks ALL paths if deferred
ADR-004: [ APPROVE / REJECT / DEFER ]     ← Options-only impact
ADR-005: [ APPROVE / REJECT / DEFER ]
ADR-006: [ APPROVE / REJECT / DEFER ]
ADR-007: [ APPROVE / REJECT / DEFER ]
```
Minimum for **any** D2 writer proof: **ADR-001 + ADR-003** approved. 004/007 may defer without
blocking the core recorder.

## 2. Authority & dependency matrix

Distinguishing: technical-review · proposal · ratification · owner-approval · formal-acceptance · handover.

| item | highest authority TODAY | artifact | note |
|---|---|---|---|
| ADR-001…007 | **proposal** (`AWAITING_OWNER_APPROVAL`) | `adr/*.md` | 7/7; no owner approval |
| 02A | **adopted-in-docs / catalog-PASS** | `ADOPTION_CLOSEOUT.md` | NOT a formal owner ratification artifact — "adopted" = validator PASS only |
| 02B | **provisional** | `SUPERSESSION_REGISTER.md:144` | not ratified |
| 02D | **coverage report exists** | `02D_*.csv` (93 rows) | NOT "complete"; a report, not an acceptance |
| 04A | **candidate, self-declares NOT authorized** | `04A:3` "Nothing here is authorized for implementation" | **its internal dependency statements are PROPOSALS, not binding gates, until 04A itself is authorized** |
| Stage 2 | **HANDOVER_COMPLETE** | `STAGE2_CHECKLIST.md` | overall `NOT_ACCEPTED` (D1–D4) |
| WP-L1-02 | **TECHNICAL_REVIEW_VERDICT** | review narrative | not owner-signed |
| control proposal v1.2 | **external / proposed** | not materialized | `PROPOSAL_PENDING_OWNER_ADOPTION` |

**Explicit:** 02A is not owner-ratified (only validator-PASS); 02D is a report, not "complete"; 04A's
dependency claims (e.g. "02B ratification gates Phase B") are **proposals inside an unauthorized
document**, binding only once 04A is authorized.

## 3. Recorder architecture — surface the control conflict

**Conflict:** `BAN_THAO` v1.2 defines `WP-04` as a **direct-Rithmic contract-compliant writer**
(Option A). The R1 Owner Pack recommended **Option C (hybrid)**. These disagree. **Do not silently
reinterpret WP-04.**

| dimension | A: Direct-Rithmic | B: ATAS-callback | C: Hybrid |
|---|---|---|---|
| authoritative **research/calibration** source | **A** (native seq 51/51) | weak (no native seq) | **direct leg of C** |
| authoritative **live-analysis** source | separate process | **B** (in ATAS runtime) | **ATAS leg of C** |
| raw fields available | full | ATAS-limited (`NativeSequenceAvailable=false`) | both, source-tagged |
| **portable derived features** | **YES — parity-proven derived params/features can run in ATAS even though raw direct-only fields cannot** | native | tagged by capability manifest |
| source-specific features un-runnable in ATAS | raw native-seq fields | n/a | flagged |
| parity / capability-manifest requirement | must prove parity to port | n/a | **manifest mandatory** |
| security/credential | own Rithmic conn, SEC-001 | none extra | SEC-001 for direct leg |
| operational ownership | 2 processes | 1 (ATAS) | 2 + reconciliation |

**Correction to the R1 pack:** "direct-Rithmic means no features available in ATAS" was too strong.
**Raw direct-only fields** are unavailable in ATAS, **but derived parameters/features computed from
parity-proven inputs may be portable** to the ATAS runtime — subject to a capability manifest proving
the input exists on the ATAS path. This is exactly what separates A from C.

**If the owner chooses C:** `WP-04` (currently "direct writer" in v1.2) must be **amended** to a
two-leg spec: `WP-04a` direct-Rithmic research writer + `WP-04b` ATAS-path live writer + a capability
manifest contract. This is a **WBS + contract amendment**, recorded here, not applied.

## 4. D1 acceptance criteria — repaired, all still `PROPOSED`

| metric | PROPOSED | design fix (R2) |
|---|---|---|
| abrupt-termination test | process kill via **.NET/Windows** (`Process.Kill(entireProcessTree:true)`) or host power-loss sim — **NOT Linux `kill -9`** | matches actual ATAS/.NET/Windows deployment |
| `RecoveryScanner` role | **has NO production caller today** — wiring it is a D2 implementation task, not an assumption | corrected from R1 |
| peak-throughput baseline | measure peak events/s over a **defined RTH window** FIRST, publish it, THEN require headroom | defines "2× peak" |
| soak window | **full GC session (RTH+ETH)** — GC trades nearly 24h; RTH-only misses ETH gap/reconnect behaviour | stated why |
| CPU denominator | **% of one logical core** on a **named machine spec** (to be recorded at run) | removes ambiguity |
| disk medium / percentile n / queue cap | record disk type; **p99 over ≥ 10k samples**; queue cap = configured bound (value PROPOSED) | measurable |
| **implementation criteria vs owner acceptance thresholds** | **separated** — see below | — |

- **Implementation criteria** (I can verify): schema-valid segments, discontinuity flagged, recovery
  scan classifies a truncated segment, no silent drop.
- **Owner acceptance thresholds** (owner sets): soak hours, throughput multiple, latency p99, CPU %,
  RAM ceiling. All `PROPOSED`.

## 5. Minimum-authority table — replaces "1+9 unblocks D2" (deleted)

The R1 claim *"Answering 1+9 alone unblocks KDK-D2-001"* is **false and removed.** Real per-task
minimum authority (stable IDs):

| task ID | task | minimum effective authority | status |
|---|---|---|---|
| **T-DOC** | documentation/governance correction (this R2) | none | **DONE** |
| **T-HARNESS** | test-only contract harness (static field-fit) | none | **DONE** (`r2/contract_compat_harness.py`) |
| **T-D2-SRC** | production D2 source changes (extend header, wire RecoveryScanner) | **ADR-001 + ADR-003 APPROVED** + **DEC-ARCH** (A/B/C) | **BLOCKED** |
| **T-D2-DIRECT** | direct-Rithmic live connection | DEC-ARCH ∈ {A,C} + **SEC-001 resolved** + entitlement | **BLOCKED** |
| **T-D2-ATAS** | ATAS deployment of new recorder | DEC-ARCH ∈ {B,C} + ATAS deploy authority | **BLOCKED** |
| **T-D2-LIVE** | live evidence run (D1) | T-D2-SRC done + a live session authorization | **BLOCKED** |
| **T-D2-ACCEPT** | formal D2 acceptance | T-D2-LIVE evidence + owner acceptance | **BLOCKED** |

**Production D2 is NOT unblocked by any two answers.** The smallest real unblock of `T-D2-SRC` is
**ADR-001 + ADR-003 approved AND DEC-ARCH chosen** — three decisions, not two, and even then only the
*source* step opens; live evidence and acceptance need more.

---

## Smallest owner response required next (stable IDs)

```
ADR-001: [ APPROVE / REJECT / DEFER ]
ADR-003: [ APPROVE / REJECT / DEFER ]
DEC-ARCH (recorder path): [ A direct / B ATAS / C hybrid ]
DEC-WP04 (if C): [ amend WP-04 into WP-04a direct + WP-04b ATAS + manifest — YES / NO ]
DEC-DELIVER (materialize into repo): [ execution brief 26ad6e23 / BAN_THAO(1) v1.2 / TIEN_DO(1) v1.2 ]
```
`ADR-001 + ADR-003 + DEC-ARCH` is the minimum to open **T-D2-SRC** only. Everything downstream
(T-D2-LIVE, T-D2-ACCEPT) needs its own authority per the table above.
