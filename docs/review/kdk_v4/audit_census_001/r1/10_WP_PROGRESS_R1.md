# r1/10 — WP Progress, corrected against the canonical WBS

**Supersedes census artifact 10.** R1 HEAD `323e36a`, canonical WBS read from `43d458f`.

## The correction

Census artifact 10 invented `WP-00…WP-10` slots and mapped them onto B0–B6. **Both were wrong.**
Verified at `43d458f`:

- `WP-00…WP-10` appear **only in the census's own artifact 10** — a repo-wide grep finds them
  nowhere else.
- `docs/BAN_THAO.md` and `docs/TIEN_DO.md` are **tracked at `43d458f`** and define the WBS as
  **"Chặng 0–5"**, not WP-00…WP-10.
- The parameter phase-ownership axis is **B0–B6/C/D** (R3 package), a *different* axis.

Per the correction spec ("do not invent a second mapping onto B0–B6 or legacy WPs"), the corrected
progress is reported against the **actual** WBS — Chặng 0–5 — on two independent axes.

## Canonical WBS: Chặng 0–5 (BAN_THAO), two axes

| Chặng | scope (BAN_THAO) | axis A: code/artifact | axis B: evidence/acceptance |
|---|---|---|---|
| **0** | Đóng nền dữ liệu (Stage 2 close, D1/D2) | writer `CODE_PRESENT`+`RUNTIME_WIRED`; Stage 2 pkg complete | D1 `NOT_STARTED`; D2 `UNVERIFIED`; Stage 2 `NOT_ACCEPTED` |
| **1** | Nền tất định (Session/TPO/VP/Zone/DQ/Version) | `CODE_PRESENT` (R3E PARTIAL; Session CONFLICT) | `TEST_EVIDENCED`; `NotCalibrated`; `NOT_ACCEPTED` |
| **2** | Trạng thái đấu giá (Episode/Acceptance/FAR/AAC/Rotation) | `CODE_PRESENT` (thresholds `NotCalibrated`) | `TEST_EVIDENCED`; **`BLOCKED`** on dataset |
| **3** | Trí tuệ M1 (M1/CVD/Effort-Result) | `CODE_PRESENT` (PARTIAL; M1 scope conflict) | `TEST_EVIDENCED`; `NotCalibrated` |
| **4** | Trụ Options (dựng lại, Phase C) | overlay `CODE_PRESENT` (`DISPLAY_ONLY`) | **`INVALIDATED`** (OPT-001..006); `AWAITING_DOMAIN_SPEC` |
| **5** | Cầu thực thi (CFD, Phase 4) | stub only | `NOT_EVIDENCED`; `CANDIDATE-DEPRECATION` |

## Real prior work packages (the ones that exist as identifiers)

| WP | axis A | axis B |
|---|---|---|
| **WP01A** (Effort/Result research bridge) | `CODE_PRESENT` (`EpisodeDatasetStore`, `EffortResultResearchCollector`) | `TEST_EVIDENCED`; `NOT_ACCEPTED` |
| **WP-L1-02** (test-flakiness repair) | committed | **`TECHNICAL_REVIEW_VERDICT` = accepted/closed** in the review narrative — a documented technical verdict, not an owner-signed formal acceptance |
| **WP-L1-RITHMIC** (data surface discovery) | Stage 2 package complete | `HANDOVER_COMPLETE`; overall **`NOT_ACCEPTED`** (D1–D4 open) |

## On the owner's WP-00…WP-10 premise

The correction task states BAN_THAO/TIEN_DO "define WP-00…WP-10." At `43d458f` they do **not** — they
define Chặng 0–5. If the owner intends a WP-00…WP-10 WBS, it is **not yet in the repo**; this is a
`SOURCE_AVAILABILITY_CONFLICT` parallel to the execution brief. The mapping above (Chặng ↔ progress)
is offered so the owner can align numbering without the audit inventing identifiers.

**No percentages** — 591/679 requirements `UNMAPPED_IN_02B` makes any percentage fabricated precision.
