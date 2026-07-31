# ADR-001 — The authoritative core is executed trades, BBO and aggregated depth

**Status:** AWAITING_OWNER_APPROVAL
**Approval:** none recorded. This ADR states a decision the evidence supports; it is NOT in force until the owner approves it.
**Date:** 2026-07-31
**Evidence:** `EV-PROBE-L1-TRADES`, `EV-PROBE-ENTITLEMENT`, `EV-PROBE-AGG-BOOK`
**MRBS anchor:** §18 DQ-002; §2.1 `MarketEvent`
**KDK anchor:** evidence tower layers 1, 3, 5, 6 — data integrity outranks everything; matched
order flow sits at layer 5, displayed microstructure at layer 6

## Context

Four surfaces are entitled and deliver on this account: executed trades with a **vendor**
aggressor field, best bid/offer with sizes and order counts, aggregated order book depth,
and both clock domains. MBO also delivers, but see ADR-002.

MRBS §18 already settles the architectural question: *"Production core v1 chỉ phụ thuộc
executed trades, Bid/Ask classification, volume, trade count, local profile và phản ứng
giá"*, and DQ-002 states DOM/MBO/iceberg are **not** a core dependency.

## Decision

The recorder's **authoritative core** is exactly:

| surface | template | evidence |
|---|---|---|
| executed trades (price, size, volume, vwap) | 150 | `EV-PROBE-L1-TRADES` |
| **vendor aggressor** | 150 | `EV-PROBE-L1-TRADES` |
| best bid/offer + sizes + order counts | 151 | `EV-PROBE-L1-TRADES` |
| aggregated order book depth | 156 | `EV-PROBE-AGG-BOOK` |
| EventTime (`source_*`) vs ReceiveTime (`ssboe/usecs`) | all | `EV-REBUILD-DIVERGES` |

Everything else is support, telemetry or context. **A recorder run that captures the core
above with `DataQuality = Ready` is a complete run**, regardless of what the non-core
surfaces did.

## Consequences

- Core capture must not be blocked, degraded or delayed by an MBO, options or
  session-statistic failure. Those failures downgrade their own surface only.
- `aggressor` is taken from the vendor field. The recorder **must not** infer aggressor from
  trade-vs-quote comparison while the vendor field is present, and must record which of the
  two produced each value if a fallback is ever added.
- Template 156 is **incremental** with BEGIN/MIDDLE/END/SOLO framing. A single message is a
  delta, not a book. Any consumer folding it must apply messages in arrival order — this
  cost one wrong measurement during the readiness work (`EV-DOC-13` §3).

## Rejected alternative

*Treat MBO as core because it is richer.* Rejected on evidence, not preference: MBO cannot be
bootstrapped (ADR-002), so a core that depended on it would be unable to reach `Ready`.
