# Phase 1A — Primary Intraday Classic TPO / Volume Profile Live Closeout

| Field | Value |
|-------|--------|
| Verdict | **PHASE 1A PASS WITH DOCUMENTED ATAS METHODOLOGY DIFFERENCE** |
| Date | 2026-07-23 |
| Baseline prior tag | `gcae-p0-08a-runtime-data-gate-gps-card-pass` @ `01ebeeec7a7253ebd6cd465d7bfa213c21a1028b` |
| Closeout tag | `gcae-p1a-primary-tpo-volume-profile-pass` |
| Instrument / feed | GCQ6 M5 / Rithmic Live |
| Anchor | 08:20 America/New_York |
| TPO period | 30 minutes |
| Tick | 0.1 |

## Accepted evidence

- Timestamp normalization live verified (`ATAS_CANDLE_TIME_UTC_V1`)
- ATAS candle Time is bar-start UTC wall-clock with Unspecified Kind
- 08:20 America/New_York auction clock verified
- Period index verified live
- Deterministic current/previous auction rollover
- Classic 30-minute TPO mechanism tested
- Exact volume-by-price Volume Profile operational
- Bar ledger accounting reconciles; no rejected bars in forensic capture
- Replace-by-bar-index recalculation strategy
- Independent `ClassicTpoOracle` matches engine distribution
- Developing-period and completed-only diagnostics agree at the selected POC
- POC tie policy behaves as specified (`MIDPOINT_THEN_PREVPOC_THEN_LOWER_TICK_V1`)
- No TPO/POC/VA algorithm was changed to force ATAS parity
- Release builds and test suite green
- MBO remains blocked/off
- Master specification untouched

## Live methodology difference (inspected auction)

| Source | TPO POC |
|--------|---------|
| GCAE selected | **4124.8** (max count 17; tie set includes 4124.8–4125.1; midpoint → previous POC → lower tick) |
| ATAS displayed | **4130.2** |

GCAE forensic at reference **4130.2**:

- TPO count: **14**
- Rank: **57**
- Below maximum by: **3**
- Developing contribution: **no**
- Inside TPO Value Area: **yes**

### Conclusions

- Timestamp is not the cause
- Developing-period policy is not the cause
- Tie policy is not the cause
- No concrete GCAE distribution defect was demonstrated
- ATAS benchmark settings/methodology remain unconfirmed
- ATAS is not treated as an unquestionable oracle
- GCAE remains governed by its explicit deterministic algorithm

## Known limitation (canonical)

ATAS built-in TPO POC did not match GCAE Classic TPO POC in the
observed live comparison. GCAE timestamp normalization, bar-ledger
accounting, deterministic TPO distribution and independent oracle were
verified. ATAS proprietary methodology or exact benchmark settings
remain unconfirmed. No GCAE algorithm was changed to force parity.

## Diagnostics policy

TPO parity and forensic diagnostics remain:

- disabled by default
- diagnostic-only
- unable to influence TPO, POC, Value Area, or Production analysis

## Scope boundary

Phase 1A does **not** include Composite Profile, Structural References,
Directional Auction Context, Auction Episode, Acceptance/Re-entry,
FAR/AAC, Entry/Invalidation/Targets, DOM/MBO implementation, or
master-spec changes.
