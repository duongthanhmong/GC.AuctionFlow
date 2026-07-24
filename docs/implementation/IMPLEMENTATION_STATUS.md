# Implementation Status

| Field | Value |
|-------|--------|
| Current phase | **Phase 1C FINAL PASS WITH DOCUMENTED LIVE COVERAGE LIMITATION — LOCKED** |
| Probe version | **0.0.6** (unchanged) |
| RawEventRecorderSchemaVersion | **1.2.0** |
| Runtime snapshot schema | **0.4.0** (Structural Reference fields) |
| Profile snapshot schema | **1.0.2** |
| Composite policy | **COMPOSITE_POLICY_V1** (unchanged) |
| Reference policy | **REFERENCE_POLICY_V1** |
| Overlay policy | **REFERENCE_OVERLAY_POLICY_V1** |
| P0-07C3D | **PASS + LOCKED** |
| P0-08A | **PASS + LOCKED** |
| Phase 1A | **LOCKED** — `gcae-p1a-primary-tpo-volume-profile-pass` |
| Phase 1B | **LOCKED FINAL PASS** — `gcae-p1b-composite-profile-foundation-pass` @ `787d0ba` |
| Phase 1C | **FINAL PASS WITH DOCUMENTED LIVE COVERAGE LIMITATION — COMMITTED + TAGGED** |
| Phase 1D | **NOT STARTED** |
| P0-07C4 | **NOT STARTED** |

## Phase 1C final closeout (2026-07-24)

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 372 passed / 0 failed / 0 skipped; 0 errors / 0 warnings |
| Live acceptance | **PASS WITH DOCUMENTED LIVE COVERAGE LIMITATION** — GCQ6 / Rithmic Live |
| Engine READY | **PASS** — Confirmed 16 / Developing 8 / Confluence 19 |
| Developing revision under live movement | **PASS** — TPO VAH 4064.0→4064.1; remained READY; no STALE_REVISION |
| Restrained overlay | **PASS** — unique zones; CONFIRMED/DEVELOPING wording; collision suppression |
| Overlay OFF / ON again | **PASS** — one REF set; Primary/Composite visual restored when OFF |
| Runtime schema | `0.4.0` |
| Policies | `REFERENCE_POLICY_V1` + `REFERENCE_OVERLAY_POLICY_V1` |
| Final DLL SHA-256 | `9CAE06B9091D23854DA60A425E45884D2CC0D5C50E501A4E9EB1B12A58FAF3CA` (source = deployed) |
| Tag | `gcae-p1c-structural-reference-foundation-pass` |
| Phase 1D | **NOT STARTED** |

### Documented live coverage limitation (accepted)

Operator ended further repetitive toggle testing. Not separately repeated as final live manual gates:

- complete Structural References module disable
- remove/add indicator lifecycle after the final revision fix
- live observation of a MIXED confluence group
- direct visual observation of each individual Developing StateVersion

These remain covered by deterministic automated tests. Live evidence proved legitimate Developing movement kept the module READY.

### Live defects fixed and accepted

1. Overlay duplication → `REFERENCE_OVERLAY_POLICY_V1` (unique zones; label collision; Primary/Composite visuals suppressed while REF overlay ON).
2. Developing `STALE_REVISION` → registry-owned monotonic `StateVersion`; drafts use `RegistryAssignedStateVersion = 0`.

### Phase 1C present (locked)

- Profile-derived Structural References (Previous/Current Primary + Confirmed Composite)
- Deterministic `ReferenceId` (zone excluded); Developing zone migration keeps identity
- Confirmed immutable; Developing revisable via registry-owned `StateVersion`
- Exact-tick confluence only; nearest above/below (no S/R)
- Source lifecycle: Fresh / Active / Expired / Retired
- Input fingerprint publish guard; restrained overlay
- MBO blocked; Reference Ready does not clear global DATA DEGRADED

### Explicitly deferred (Phase 1D+)

- Weekly/monthly/IB/VWAP/swing/launch/HVN/LVN/nPOC/DOM levels
- Approach / Interacting / OutsideAttempt / Acceptance runtime emission
- TestCount / reaction history / executed activity evaluation
- Hierarchy score / probability
- Directional Auction / Episode / FAR/AAC / Thesis / Entry / Risk / Telegram

## Phase 1B final closeout (2026-07-24) — LOCKED

| Gate | Result |
|------|--------|
| Code/test | **PASS** — 329 passed at lock |
| Live acceptance | **PASS** |
| Final DLL SHA-256 | `CA2157509B140D0752FB4FCEF70E1FCD863553053D6E133566B848BBDDB02B87` |
| Tag | `gcae-p1b-composite-profile-foundation-pass` |

## Phase 1A locks (unchanged)

- Classic TPO 30m; anchor 08:20 America/New_York; `ATAS_CANDLE_TIME_UTC_V1`
- Exact price-volume only; Trade Recorder locked; MBO blocked; master spec v1.2 untouched
