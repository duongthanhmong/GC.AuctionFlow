# Implementation Status

| Field | Value |
|-------|--------|
| Current phase | **Phase 1B FINAL PASS — LOCKED** — Composite Profile Foundation |
| Probe version | **0.0.6** (unchanged) |
| RawEventRecorderSchemaVersion | **1.2.0** |
| Runtime snapshot schema | **0.3.0** (Composite fields) |
| Profile snapshot schema | **1.0.2** (PrimaryProfileSet includes CompletedAuctions for Composite feed) |
| Composite policy | **COMPOSITE_POLICY_V1** |
| P0-07C3D | **PASS + LOCKED** (`gcae-p0-07c3d-live-trade-recorder-pass`) |
| P0-08A | **PASS + LOCKED** (`gcae-p0-08a-runtime-data-gate-gps-card-pass` @ `01ebeee`) |
| Phase 1A | **PASS WITH DOCUMENTED ATAS METHODOLOGY DIFFERENCE** — tag `gcae-p1a-primary-tpo-volume-profile-pass` @ `771095e` |
| Phase 1B | **FINAL PASS — COMMITTED + TAGGED** (`gcae-p1b-composite-profile-foundation-pass`) |
| Phase 1C | **NOT STARTED** |
| P0-07C4 | **NOT STARTED** |

## Phase 1B final closeout (2026-07-24)

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 329 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **PASS** — GCQ6 / Rithmic Live (operator anh Fen) |
| Blank-anchor AWAITING | **PASS** |
| Anchor propagation (blank ↔ `PI-2026-07-20`) | **PASS** |
| Anchored Composite READY | **PASS** — 3 completed auctions |
| Multi-auction historical feed | **PASS** — `PI-2026-07-20`…`PI-2026-07-22`; developing `PI-2026-07-23` outside Confirmed |
| Confirmed / Preview isolation | **PASS** |
| Remove/add lifecycle | **PASS** — one card, one overlay set |
| MBO | **BLOCKED** |
| Global DATA DEGRADED | Remains independent (Bid/Ask Unknown/Partial); Composite Ready does **not** clear it |
| Final DLL SHA-256 | `CA2157509B140D0752FB4FCEF70E1FCD863553053D6E133566B848BBDDB02B87` (source = deployed) |

### Live accepted Confirmed Composite (final lifecycle)

- Status: READY · Anchor: `PI-2026-07-20` · Auctions: 3
- Membership: `PI-2026-07-20`, `PI-2026-07-21`, `PI-2026-07-22`
- Range: 4001.6–4171.4 · TPO POC: 4124.8 · VPOC: 4125.0
- TPO VA: VAL 4056.5 / VAH 4146.0 · Volume VA: VAL 4067.4 / VAH 4167.8
- Preview: OFF · Evidence: NOT EVALUATED · MBO: BLOCKED

### Wiring fixes accepted during live acceptance

1. Guarded one-shot Composite init when Current missing (blank-anchor AWAITING gate).
2. Normalized Composite operator-configuration fingerprint (anchor / include-through-latest / exclusions / preview / shadow / `COMPOSITE_POLICY_V1`) so setting changes rebuild once; ordinary trades reuse.

## Phase 1A locks (unchanged)

- Classic TPO period: **30 minutes** (configurable)
- Primary Intraday anchor: **08:20 America/New_York**
- Timestamp policy: **`ATAS_CANDLE_TIME_UTC_V1`**
- Exact price-volume only for VP — **no bar-total smearing**
- POC tie: **MIDPOINT_THEN_PREVPOC_THEN_LOWER_TICK_V1**
- Trade Recorder locked; MBO blocked; master spec v1.2 untouched
- ATAS is a benchmark, not an unquestionable oracle

## Phase 1B Composite Profile Foundation (locked content)

Present:

- `CompositePolicyMode`: Disabled / OperatorAnchored / ShadowEvidence
- OperatorAnchored confirmed composite (explicit anchor required; no silent default)
- Completed-auction ledger (replace-by-version; epoch/tick/policy fail-closed)
- Deterministic composite TPO + exact Volume aggregation (reuses Phase 1A POC/VA)
- Developing composite preview (separate; cannot mutate confirmed)
- Merge-evidence metrics (measurement only)
- ShadowEvidence states; **NotCalibrated** when thresholds unset — no Production merge/close
- Runtime snapshot Composite fields + GPS composite rows + optional overlay
- Operator-configuration fingerprint for publish-path rebuild (not per-trade)
- Bounded transition logging

Explicitly absent / deferred (Phase 1C+):

- Hard N-day production merge (forbidden)
- Automatic StableBalance / Breaking / NewValue Production classification
- Structural Reference Lifecycle / nPOC / HVN-LVN intelligence
- Directional Auction Context / One-Time Framing / Auction Episode
- Acceptance/Re-entry / FAR/AAC / Entry/Invalidation/Targets
- Orderflow / DOM / MBO / Telegram / Adaptive TPO
- Calibrated GC merge/close thresholds (shadow thresholds remain unset)

## Defaults

- `EnableCompositeProfile = false` (opt-in)
- Developing preview OFF unless enabled
- ShadowEvidence OFF; thresholds null → `COMPOSITE EVIDENCE: NOT CALIBRATED` / NOT EVALUATED
- MBO remains OFF / blocked
