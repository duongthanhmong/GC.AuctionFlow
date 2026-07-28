# OptionFlow test fixtures

## `rithmic_oi_eod_cache.json`

**SYNTHETIC TEST DATA.** This file is **not** sourced from Rithmic, from any live
account, session, or feed. It contains no credentials, no account identifier, no
session timestamps, and no live-feed metadata. It was generated deterministically
(fixed COMEX gold `OGQ6` strikes 3900–4300 step 10, one Call and one Put per strike,
with a fixed open-interest curve) purely to exercise the **offline** OptionFlow paths.

**Purpose:** regression tests for symbol parsing, open-interest aggregation, OI walls,
and Max Pain (`tests/test_fixture_oi.py`) — the parts that need no live quote.

**Not valid for any market inference or calibration.** It proves the parser/aggregation
plumbing, nothing about real gold options positioning.

It is committed (via a narrow `.gitignore` exception for exactly this filename) so a
clean checkout carries its own test data and the suite runs 37/37 with 0 skipped.
Live OptionFlow cache files remain gitignored.
