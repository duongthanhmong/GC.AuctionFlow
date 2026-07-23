# Review Checklist — Phase 1A Primary Intraday Classic TPO / Volume Profile

## Prior baseline

- [x] P0-08A PASS + locked (`gcae-p0-08a-runtime-data-gate-gps-card-pass` @ `01ebeee`)
- [x] Trade Recorder not redesigned
- [x] Master spec v1.2 untouched
- [x] MBO not enabled in primary process

## Implementation

- [x] ProfileBarObservation / PriceVolumeObservation boundary (no ATAS types in Profile Core)
- [x] PriceGrid tick-index keys (GC 0.1)
- [x] PrimaryAuctionClock 08:20 America/New_York + DST
- [x] AtasTimestampNormalizer `ATAS_CANDLE_TIME_UTC_V1` (LiveObserved)
- [x] ClassicTpoEngine 30m letters, developing/completed distinction
- [x] VolumeProfileEngine exact price-volume only
- [x] POC tie policy MIDPOINT_THEN_PREVPOC_THEN_LOWER_TICK_V1
- [x] ValueAreaCalculator adjacent expand (0.70 conventional default)
- [x] Current/previous PrimaryProfileHost bar-ledger replace-by-index
- [x] Deferred historical rebuild (chart-add load path)
- [x] Runtime/DataGate/GPS card profile rows + overlay
- [x] ATAS mapper uses observed GetCandle / GetAllPriceLevels / PriceVolumeInfo
- [x] Bounded TPO parity / forensic diagnostics (default OFF; diagnostic-only)
- [x] Independent ClassicTpoOracle matches engine distribution

## Explicitly deferred

- [x] No Composite / HVN-LVN / nPOC / Excess / Single Prints / Poor High-Low
- [x] No Structural Reference / Episode / FAR/AAC / Entry/Target / Thesis
- [x] No Adaptive TPO / DOM / MBO / Telegram

## Live closeout

- [x] Timestamp normalization live verified (UTC Unspecified → period index correct)
- [x] Auction clock / period index / current-previous rollover live verified
- [x] Bar ledger forensic: no rejected bars in capture; replace-by-index strategy
- [x] Developing vs completed-only diagnostics agree at selected POC (methodology check)
- [x] ATAS TPO POC mismatch documented as methodology/settings difference — **no forced parity**
- [x] Final verdict: **PHASE 1A PASS WITH DOCUMENTED ATAS METHODOLOGY DIFFERENCE**
- [x] Evidence: `docs/evidence/Phase1A_Primary_TPO_Volume_Profile_LiveCloseout.md`
- [x] Tag: `gcae-p1a-primary-tpo-volume-profile-pass`

## Verify

- [x] Release build/test green at closeout
- [x] Trade Recorder regression tests pass
- [x] MBO remains off/blocked
- [x] Master specification untouched
