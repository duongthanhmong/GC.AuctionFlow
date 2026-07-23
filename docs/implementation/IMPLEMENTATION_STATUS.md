# Implementation Status

| Field | Value |
|-------|--------|
| Current phase | **Phase 1A CLOSED** — next authorized slice is Phase 1B Composite Profile (not started) |
| Probe version | **0.0.6** (unchanged) |
| RawEventRecorderSchemaVersion | **1.2.0** |
| Runtime snapshot schema | **0.2.0** |
| Profile snapshot schema | **1.0.1** (TPO / Volume / PrimaryAuction / PrimaryProfileSet) |
| P0-07C3D | **PASS + LOCKED** (`gcae-p0-07c3d-live-trade-recorder-pass`) |
| P0-08A | **PASS + LOCKED** (`gcae-p0-08a-runtime-data-gate-gps-card-pass` @ `01ebeee`) |
| Phase 1A | **PASS WITH DOCUMENTED ATAS METHODOLOGY DIFFERENCE** — tag `gcae-p1a-primary-tpo-volume-profile-pass` |
| P0-07C4 | **NOT STARTED** |
| Phase 1B | **NOT STARTED** |

## Phase 1A locks / semantics

- Classic TPO period: **30 minutes** (configurable)
- Primary Intraday anchor: **08:20 America/New_York** (IANA + Windows Eastern fallback)
- Timestamp policy: **`ATAS_CANDLE_TIME_UTC_V1`** (LiveObserved: Unspecified Kind, UTC wall-clock)
- Exact price-volume only for VP — **no bar-total smearing**
- Current + previous auction only — no Composite / Structural References
- Global DATA may remain **Degraded** due BidAsk/Roll Unknown even when PROFILE READY
- TPO parity / forensic diagnostics: **disabled by default**, diagnostic-only, cannot influence TPO/POC/VA
- Trade Recorder unchanged / locked; MBO remains blocked in primary process
- Master spec v1.2 untouched
- **ATAS built-in TPO POC is not treated as an unquestionable oracle**; GCAE follows its explicit deterministic algorithm

## Phase 1A scope (contained)

Present: PrimaryAuctionClock + timestamp normalization, PriceGrid, Classic TPO, exact Volume Profile, VA + deterministic POC, current/previous snapshots, runtime/DataGate, GPS profile rows, minimal overlay, bounded diagnostics, tests, governance.

Absent: Composite Profile, Structural Reference Engine, Directional Auction Context, Auction Episode, Acceptance/Re-entry, FAR/AAC, Entry/Invalidation/Targets, DOM/MBO implementation, master-spec changes.
