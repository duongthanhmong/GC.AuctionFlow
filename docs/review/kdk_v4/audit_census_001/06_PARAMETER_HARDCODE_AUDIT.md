# 06 — Parameter & Hard-Code Audit

**Audited commit:** `43d458f`.

## Registry binding

| metric | value | evidence |
|---|---|---|
| canonical parameters | 134 | `KDK_PARAMETER_CANONICAL_REGISTRY_R3.csv` |
| `Approved` (research/production) | **0** | `approval_status` column |
| bound to a code member | **2** | `current_impl_member != NOT_PRESENT` |
| `NOT_PRESENT` | 132 | — |

The 2 bound: `tpo_bracket_minutes` (`ClassicTpoEngine.cs`), `value_area_percent`
(`ValueAreaAndPoc.cs`) — both structural TPO/VP constants, neither directional.

## Hard-code discipline markers in `src/GC.AuctionFlow`

| marker | count | meaning |
|---|---|---|
| `NotCalibrated` / `NOT_CALIBRATED` | **383** | classifiers deliberately refuse to conclude |
| `throw new *Exception` | 151 | guard/validation throws (not stubs) |
| `NotImplementedException` | **0** | no unimplemented-stub throws |
| `TODO` / `FIXME` / `HACK` | **0** | no acknowledged debt markers |

## Assessment

The dominant pattern is **not** hard-coded thresholds masquerading as working logic. It is the
opposite: **383 `NotCalibrated` guards** that make classifiers emit observations without
conclusions until a parameter is approved. This is consistent with `G-CAL-001` (no self-set
thresholds) and ADR-005.

**No canonical parameter was found hard-coded in `src/` in violation of its unapproved status.**
The two present members are structural constants, and the registry itself lists them (they are
not smuggled values). This is a **clean** result for the hard-code dimension — the risk here is
the reverse of the usual one: almost nothing is bound, so almost nothing runs to a conclusion.

**Caveat (scope-bounded):** this audit counted markers and checked the 134 canonical ids by
name. A line-by-line sweep of all 383 `NotCalibrated` sites to confirm each truly gates emission
(vs. being a dead label) was not performed in this run — recorded as a residual verification item,
not a finding of violation.
