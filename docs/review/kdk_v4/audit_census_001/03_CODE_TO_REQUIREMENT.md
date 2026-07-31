# 03 — Code-to-Requirement & Parameter Matrix

**Audited commit:** `43d458f`. All counts re-derived from the CSVs at this HEAD.

## Requirements (02A / 02B / 02D)

| file | rows |
|---|---|
| `02A_KDK_REQUIREMENT_CATALOG_V4.csv` | **679** (668 LINE_SHIFT_ONLY + 10 NEW + 1 BASELINE_ANCHOR_CORRECTION) |
| `02B_KDK_REQUIREMENT_TRACEABILITY_V4.csv` | **679** |
| `02D_KDK_COVERAGE_REPORT_V4.csv` | 93 |

**02B `implementation_status` distribution (measured):**

| status | count |
|---|---|
| `NOT_IMPLEMENTED` | **591** |
| `NOT_APPLICABLE` | 59 |
| `AWAITING_DOMAIN_SPEC` | 28 |
| **`CODE_TESTED`** | **1** |

**One (1) requirement of 679 has code+test evidence:**

```
KDK-CH21-REQ-003  "No tick-rule assumption prohibition"
  source_files : src/GC.AuctionFlow/Probe/TradeAggressorSide.cs
  source_symbol: GC.AuctionFlow.Probe.TradeAggressorSide.Resolve
  test         : tests/.../Unit/Probe/TradeAggressorSideTests.cs
                 B01_An_unmapped_direction_stays_unknown
  live_evidence_id     : NONE
  calibration_artifact : NONE
  oos_artifact         : NONE
```

Even this one has `live_evidence_id = NONE`. It asserts a **refusal** (`Resolve()` returns
`Unknown` on insufficient data), not a positive analytic capability.

**02B is provisional** — `SUPERSESSION_REGISTER.md:141,144`, `ADOPTION_CLOSEOUT.md:37`.

**Contradiction with the closeout narrative (CTR-02):** `ADOPTION_CLOSEOUT.md:38-39` states
"NOT_IMPLEMENTED **589** · PARTIAL **2** · DISPLAY_ONLY **1**". The CSV it points at has
`NOT_IMPLEMENTED` **591**, **0** PARTIAL, **0** DISPLAY_ONLY, **1** CODE_TESTED. The narrative
describes a distribution the data does not contain.

## Alignment matrix — use the CURRENT revision only

**Current:** `mrbs_param_recon_r3e/MRBS_V1_1_CODE_ALIGNMENT_MATRIX_R3E.csv` (25 module rows).
**Rejected / do not cite:** `mrbs_adoption_r1/MRBS_V1_1_CODE_ALIGNMENT_MATRIX.csv`
(`mrbs_adoption_r2/README_R2.md:3` "Supersedes R1 (rejected)"). Earlier project docs (TIEN_DO,
BAN_THAO first drafts) cited R1 line numbers; those are void — this census uses R3E.

## Parameters (canonical registry R3)

| metric | value |
|---|---|
| total canonical parameters | **134** |
| `approval_status = Proposed` | 92 |
| `approval_status = UnderReview` | 38 |
| `approval_status = Deferred` | 4 |
| **`Approved` (research or production)** | **0** |
| have a `current_impl_member` in code | **2** |
| `NOT_PRESENT` in code | **132** |

The only 2 bound to code:
- `tpo_bracket_minutes` → `TpoPeriodMinutes@src/GC.AuctionFlow/Profile/ClassicTpoEngine.cs`
- `value_area_percent` → `ValueAreaFraction@src/GC.AuctionFlow/Profile/ValueAreaAndPoc.cs`

Both are structural TPO/VP constants (period length, value-area fraction), not
directional/threshold parameters. Promotion to `Approved` requires a point-in-time dataset +
out-of-sample result + Decision Record (MRBS §44 + §24.3, Registry §1). **None exists.**

## Reconciliation of totals against source documents

| total | source doc | census | verdict |
|---|---|---|---|
| 679 requirements | KDK v4 catalog regen | **679** | ✓ |
| 134 parameters | Registry v1.0 + 13 addendum | **134** | ✓ |
| 0 approved | Registry promotion pipeline | **0** | ✓ |
