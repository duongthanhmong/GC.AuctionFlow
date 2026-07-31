# r1/06 — Parameter & Hard-Code Audit, corrected classification

**Supersedes census artifact 06.** R1 HEAD `323e36a`. Commands in `raw/R1_COMMANDS.txt`.

## `NotCalibrated` — classified (was: "383 runtime guards")

| classification | count | command basis |
|---|---|---|
| textual occurrences | **383** | `grep -rn NotCalibrated\|NOT_CALIBRATED` |
| unique files | **68** | `grep -rl` |
| enum/const declarations | **84** | `grep -E '(enum\|const\|= )[^;]*NotCalibrated'` |
| control-flow return/assignment | **10** | `grep -E 'return .*NotCalibrated\|= .*NotCalibrated'` |
| in comments | **45** | `grep -E '^\s*//.*NotCalibrated'` |
| reason-code string literal | **0** | `grep '"[^"]*[Nn]ot.?[Cc]alibrated'` |
| remainder | ~244 | enum-value references / comparisons / usages |

**Corrected statement:** "383 runtime guards" is withdrawn. The 383 are dominated by **enum-value
declarations (84)** — `NotCalibrated` is a *state value* in many per-module state enums — plus
**comments (45)** and **~10 actual conclusion-suppression returns**. The genuine
"refuses-to-conclude" control-flow sites number in the low tens, not 383. The overall pattern
(classifiers default to a not-calibrated state rather than emitting a value) still holds, but the
number was mislabeled.

## Numeric-literal / hard-code inspection (the scope census 06 admitted it skipped)

Inspected: classifier folders `EffortResult/`, `Imbalance/`, `Resolution/`, `Evidence/`; all
constructor default parameters across `src/GC.AuctionFlow`; `ratio/threshold/percentile/timeout/
window` literal contexts.

| finding | result |
|---|---|
| hard-coded `double`/`decimal` threshold in classifier folders (excl `0.0`/`1.0`/version) | **0** |
| constructor-default numeric literals (`= N.N` in ctor params) | **0** |
| `ratio/threshold/percentile = <number>` sites | **0** substantive (only enum ordinals `IbWidthPercentile = 0`) |
| the 2 registry-bound values | `tpo_bracket_minutes`→`TpoPeriodMinutes`, `value_area_percent`→`ValueAreaFraction` — **REGISTRY_BOUND** |

### Per-value classification

| value | classification |
|---|---|
| `TpoPeriodMinutes`, `ValueAreaFraction` | **REGISTRY_BOUND** (2 canonical ids in code) |
| enum ordinals (`= 0`, `= 3`, …) | **STRUCTURAL_CONSTANT** (enum member indices, not thresholds) |
| any classifier threshold | **not found in inspected scope** |
| the other 132 canonical parameters | **UNAPPROVED_PARAMETER_CANDIDATE** — `NOT_PRESENT` in code (correctly absent) |

## Corrected conclusion (scope-bounded, not global)

**Within the inspected scope** — classifier threshold folders, all ctor defaults, and
ratio/threshold/timeout literal contexts — **no unapproved numeric threshold is hard-coded.** The
classifiers defer via not-calibrated state rather than embedding a value, consistent with
`G-CAL-001` and ADR-005. **This is NOT a global "no violation" claim:** a line-by-line sweep of all
383 `NotCalibrated` sites and every numeric literal in all 248 files was not performed — recorded
as **UNKNOWN_REVIEW_REQUIRED** residual scope, not as a clean bill.
