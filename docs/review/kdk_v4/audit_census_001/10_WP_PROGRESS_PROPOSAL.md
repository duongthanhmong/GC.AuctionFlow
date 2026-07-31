# 10 — WP Progress Proposal (two independent axes)

**Audited commit:** `43d458f`. Two axes, never collapsed to one number:
**(A) Code/artifact presence** · **(B) Evidence/acceptance readiness**.

## Note on the WP-00…WP-10 numbering

**The identifiers `WP-00` … `WP-10` do not exist in this repository.** A search returns only
`WP01A` (22 files), `WP-L1-02` (24 files), and the `WP-L1-RITHMIC` arc. The table below maps the
requested WP-00…WP-10 slots onto the work packages and KDK/MRBS phases that actually exist, so the
reviewer gets a real answer rather than an invented one. Where a slot has no real work package it
is marked `NO_SUCH_WP`.

| slot | maps to (real) | axis A: code/artifact | axis B: evidence/acceptance |
|---|---|---|---|
| **WP-00** | Foundation / recorder + probes (B0) | `CODE_PRESENT` — recorder, `SegmentWriter`, 39 probe files | `TEST_EVIDENCED`; **`NOT_ACCEPTED`** (no acceptance record) |
| **WP-01** | AMT map/state (B1) — TPO/VP/Composite/Reference | `CODE_PRESENT` (PARTIAL per R3E) | `TEST_EVIDENCED`; `NotCalibrated`; `NOT_ACCEPTED` |
| **WP-02** | Order flow raw (B2) | `CODE_PRESENT` (PARTIAL) | `TEST_EVIDENCED`; `NotCalibrated` |
| **WP-03** | Episode (B3) | `CODE_PRESENT` (no timeout) | `TEST_EVIDENCED`; `NotCalibrated` |
| **WP-04** | Effort/Result (B4) | `CODE_PRESENT` | `TEST_EVIDENCED`; `candidate_only` |
| **WP-05** | Acceptance + FAR/AAC/Rotation (B5) | `CODE_PRESENT` (thresholds `NotCalibrated`) | `TEST_EVIDENCED`; **`BLOCKED`** on dataset |
| **WP-06** | DOM/MBO research telemetry (B6) | `CODE_PRESENT` | `LIVE_EVIDENCED` (direct probe); `RESEARCH_TELEMETRY`, never core |
| **WP-07** | Options context (Phase C) | `CODE_PRESENT` overlay (`DISPLAY_ONLY`) | **`INVALIDATED`** — 6 defects `OPT-001..006` |
| **WP-08** | Execution bridge / CFD (Phase 4) | `CODE_PRESENT` stub only | `NOT_EVIDENCED`; `CANDIDATE-DEPRECATION` |
| **WP-09** | WP-L1-RITHMIC acquisition | artifacts + Stage 2 package | **`LIVE_EVIDENCED`** (15 EvidenceIds); **`NOT_ACCEPTED`** (Stage 2 open) |
| **WP-10** | Sustained-load (D1) + writer proof (D2) | writer `CODE_PRESENT`+`RUNTIME_WIRED` | **D1 `NOT_STARTED`**; D2 `UNVERIFIED_FOR_D2` |

## Real work packages, stated plainly

| WP | axis A | axis B |
|---|---|---|
| **WP01A** (Effort/Result research bridge) | `CODE_PRESENT` (`EpisodeDatasetStore`, `EffortResultResearchCollector`) | `TEST_EVIDENCED`; `NOT_ACCEPTED` |
| **WP-L1-02** (test-flakiness repair) | committed | prior reviewer verdict `TECHNICAL_ACCEPTED / FORMALLY_CLOSED` (a documented acceptance — the ONE place acceptance was recorded, in the review narrative) |
| **WP-L1-RITHMIC** (data surface discovery) | Stage 2 package complete | `VERIFIED / HANDOVER_COMPLETE`; **overall `NOT_ACCEPTED`** (D1–D4 open) |

**No percentages are given** because no objective basis exists to derive them: 591/679
requirements are `NOT_IMPLEMENTED` and 0/134 parameters are approved, so any percentage would be
a fabricated precision. Categorical states are the honest instrument.
