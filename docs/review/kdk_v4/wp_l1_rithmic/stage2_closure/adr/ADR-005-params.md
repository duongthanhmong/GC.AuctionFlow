# ADR-005 — No seed parameter is hard-coded; the registry stays the single source

**Status:** AWAITING_OWNER_APPROVAL
**Approval:** none recorded. This ADR states a decision the evidence supports; it is NOT in force until the owner approves it.
**Date:** 2026-07-31
**Evidence:** `EV-PARAM-REGISTRY`
**MRBS anchor:** §2.2 VER-001/002/003; §24.3 conditions for keeping a module/threshold
**Project rule:** `G-CAL-001` — no self-set thresholds

## Context

The canonical registry contains **134 parameters**. Measured distribution of
`approval_status`:

| status | count |
|---|---|
| Proposed | 92 |
| UnderReview | 38 |
| Deferred | 4 |
| **Approved** | **0** |

`current_impl_member` is `NOT_PRESENT` for **132 of 134**; the two that exist are
`TpoPeriodMinutes` and `ValueAreaFraction`.

No calibration dataset exists. No walk-forward has been run. Nothing in the acquisition work
of docs 12–13 produces a calibrated threshold, and it was never intended to.

## Decision

1. **No value from the registry is compiled into the recorder or any engine.** Parameters
   are resolved at runtime from a versioned config, and a missing parameter is a
   `CONFIG_VERSION_MISSING` / record-invalid condition (VER-001), never a silent default.
2. Every persisted decision or record carries `AlgorithmVersion`, `ConfigVersion` and
   `DataSchemaVersion` as three independent fields (VER-001).
3. A parameter may move to `Approved` only with (a) a point-in-time dataset with provenance,
   (b) an out-of-sample result, and (c) a decision record. None of those exist today, so
   **the correct count of approved parameters after Stage 2 closure is still zero.**
4. Where this closure package needs a number to function — for example a gap tolerance —
   it **names the registry item and leaves it unbound** rather than inventing a value. The
   schemas mark such fields required-but-unset and the recorder fails closed if unset.

## Consequences

- The recorder cannot classify `Degraded` vs `Invalid` on trade-stream gaps until DQ-001's
  tolerance is approved. Until then it records the **measurement** (gap count, max gap,
  out-of-order count) and marks the interval `Invalid` on any connection-coincident
  discontinuity (ADR-003), which needs no threshold.
- This is deliberately conservative: it prefers an interval wrongly marked `Invalid` over an
  interval wrongly marked `Ready`.

## What is NOT claimed

That the 134 seeds are wrong. They are unproven, which is a different statement, and the
registry already records them as such.
