# KDK-CENSUS-001-R2 — Control-file & claim reconciliation

**Corrects R1 (`368d168`/`c94ed8b`) without overwriting it.** R2 HEAD `c94ed8b`, base `43d458f`,
worktree 131 untracked / 0 tracked-modified preserved. Commands: `raw/R2_COMMANDS.txt`.

---

## 1. WBS conclusion — repaired

**Retract:** "WP-00…WP-10 do not exist" (census 10) **and** its R1 replacement "the canonical WBS is
Chặng 0–5."

**Evidence-bounded truth:**

| question | answer |
|---|---|
| existed at `43d458f`? | **No** — the tracked `TIEN_DO/BAN_THAO` at `43d458f` used Chặng 0–5; `WP-00…WP-10` appear only in the census's own artifact 10 |
| exist in the supplied v1.2 control files? | **Yes, per owner** — `BAN_THAO(1).md`/`TIEN_DO(1).md` v1.2 define them |
| are those v1.2 files committed? | **No** — not in any commit; not on disk (Provenance Matrix) |
| owner-adopted? | **No** — `PROPOSAL_PENDING_OWNER_ADOPTION` |

**Corrected label:** `WP-00…WP-10` is the **current supplied control proposal** — *not* an invented
WBS, *not* an owner-approved canonical WBS. **Both axes are preserved:**

- **delivery WBS:** `WP-00…WP-10` (supplied v1.2 control proposal, external, not materialized)
- **execution stages/lanes:** Chặng 0–5 (tracked) and phase-owner B0–B6/C/D (R3 package)

## 2. Execution-brief status — repaired

Taxonomy applied. The brief (`26ad6e23…`, incl. §15) is recorded RECEIVED by the current control
files; the actual artifact is not in this workspace (409 archives hashed across two runs, no match;
no repo doc records RECEIVED except the census's own).

**Current correct state: `RECEIVED_RECORDED_SOURCE_FILE_UNAVAILABLE_AT_AUDITED_HEAD`.**

Not reopened as "never arrived." No owner *decision* is requested; the request is a **document
delivery** — materialize the file into the repo if reproducibility requires §15. Same class applies
to the v1.2 control files themselves.

## 3. Code-completeness — narrowed

**Retract:** "the domain is largely BUILT at module granularity."

A cited symbol proves a **code candidate / partial presence**, not functional completeness. Per-module
highest **independently-proven** state at HEAD (source file exists + R3E alignment; runtime/test not
independently established per-module in this pass):

| state | modules |
|---|---|
| `CODE_PRESENT_PARTIAL` | **23** |
| `CODE_CANDIDATE_FOUND` | **2** (ValueRotation, EngineLifecycleWarmup — R3E `NOT_IMPLEMENTED`) |
| `RUNTIME_WIRED` (per-module semantic) | **0 independently proven** — composition root builds `GcaeRuntimeEngine` and calls `Publish()` (r1/04), but per-module semantic invocation with valid domain output was not proven |
| `TEST_EVIDENCED` (per-module) | not established in this pass; 1531 suite is aggregate |
| `LIVE_EVIDENCED` / `ACCEPTED` | **0** |

**Requirement-level traceability (separate axis):** 591 `UNMAPPED_IN_02B`, 59 `NOT_APPLICABLE`,
28 `AWAITING_DOMAIN_SPEC`, 1 `CODE_TESTED`. Module-level partial presence ≠ requirement-level coverage.

**Corrected statement:** 23/25 modules have partial code present; **no module is independently proven
built, wired-with-valid-output, or accepted.** "Built" is withdrawn.

## 4. Composition wiring vs semantic readiness — separated

| layer | proven? | evidence |
|---|---|---|
| object/composition wiring exists | **YES** | `new GcaeRuntimeEngine` `GcAuctionFlowIndicator.cs:1345`; `runtime.Publish()` `:2967` |
| a runtime caller invokes the boundary | **YES** (Publish is called) | `:2967` |
| valid domain output emitted | **NOT PROVEN** | classifiers gated `NotCalibrated` |
| output suppressed by `NotCalibrated` | **YES** | 84 enum decls, 10 control-flow returns (r1/06) |
| downstream receives diagnostic/unknown only | **YES** | Entry emits `ObserveOnly` |
| live / acceptance evidence | **NONE** | — |

**Corrected statement:** a connected call graph exists; an operational
Episode → Acceptance → Order Flow → FAR/AAC **conclusion** chain is **not** proven. The chain composes
and runs; it does not conclude.

## 5. Parameter & hard-code scope — corrected

- **"2 parameters in code"** means exactly: **2 canonical Registry IDs** (`tpo_bracket_minutes`,
  `value_area_percent`) were found by matching `current_impl_member` in the R3 CSV. It does **not**
  imply only two configurable behaviours exist.
- **Hard-code scope (exact):** inspected paths = classifier folders `EffortResult/`, `Imbalance/`,
  `Resolution/`, `Evidence/`; literal classes = `double`/`decimal` (excl `0.0`/`1.0`/version) and
  constructor-default numeric literals across `src/GC.AuctionFlow`. Result in **that scope**: 0
  hard-coded thresholds. **NOT inspected in this pass:** `int` caps, `TimeSpan`/duration
  construction, `static`/`readonly` defaults, config fallbacks, window sizes, retry counts, timeouts
  across all 248 files. Those are **UNKNOWN_REVIEW_REQUIRED**, not "zero." No global no-violation
  claim is made.

## 6. Handover independent-verifiability — confirmed

All R1-referenced files (`r1/*`, `raw/R1_COMMANDS.txt`, `raw/BUILD.txt`, `raw/TEST.txt`,
`SHA256SUMS.txt`, `KDK_D2_CONTRACT_FIT_001.md`) **exist in the pushed commit `c94ed8b`** and
`sha256sum -c SHA256SUMS.txt` returns **25/25 OK from a clean `git archive` checkout** (not the
working tree). **No file omitted.** The defect was packaging (a summary delivered without the
referenced files), not repo-completeness — the git bundle is complete and clone-verifiable.

## Retained / narrowed / retracted (summary)

| R1 conclusion | R2 disposition |
|---|---|
| build 0/0; 1531 tests; 248/79; 679 reqs; 134 params 0 approved; 15 EvidenceIds 15/15 | **RETAINED** |
| writer exists+wired; RecoveryScanner un-wired; ingress via ATAS | **RETAINED** |
| "WP-00…WP-10 do not exist" / "canonical WBS is Chặng 0–5" | **RETRACTED** → current supplied control proposal; both axes preserved |
| "largely BUILT at module granularity" | **RETRACTED** → 23 CODE_PRESENT_PARTIAL, 0 proven built |
| "runtime chain wired end-to-end" | **NARROWED** → composes+runs, does not conclude |
| "0 hard-coded thresholds" | **NARROWED** → 0 in named scope; rest UNKNOWN_REVIEW_REQUIRED |
| brief "missing / SOURCE_AVAILABILITY_CONFLICT" | **REFINED** → `RECEIVED_RECORDED_SOURCE_FILE_UNAVAILABLE_AT_AUDITED_HEAD` |
