# ADR-002 — MBO is research telemetry, not a reconstructable book

**Status:** AWAITING_OWNER_APPROVAL
**Approval:** none recorded. This ADR states a decision the evidence supports; it is NOT in force until the owner approves it.
**Date:** 2026-07-31
**Evidence:** `EV-REBUILD-DIVERGES`
**MRBS anchor:** §18 DQ-002 (DOM/MBO not a core dependency), DQ-004
**KDK anchor:** layer 6 displayed microstructure — dependent on source and on
reconstructability

## Context

`DepthByOrder` (template 160) delivers everything a market-by-order recorder wants: native
`sequence_number` on every event, NEW/CHANGE/DELETE, `exchange_order_id`,
`depth_order_priority`, and three clock domains. That richness invites treating it as a
reconstructable full book.

It was tested rather than assumed. Fan-out over 21 prices at the vendor tick size
(`min_fprice_change` = 1.0, read from reference data), a template-116 snapshot at each price,
then 479 updates replayed in arrival order keyed on `exchange_order_id`:

| measurement | value |
|---|---|
| snapshots returned | 21 of 21 prices |
| **orders seeded per snapshot** | **exactly 1, every time** |
| CHANGE addressing an order never seeded | 75 |
| DELETE addressing an order never seeded | 16 |
| price levels matching the vendor's own aggregated book | **3 of 21** |

The rebuilt size is **below** the vendor size at nearly every level and never above — the
signature of an incomplete seed, not of mis-applied updates.

## Decision

**MBO is recorded as research telemetry and is never promoted to a reconstructed book.**

- `data_quality_role = RESEARCH_TELEMETRY` for every MBO surface in the manifest.
- The recorder persists MBO events **as received**, in arrival order, with the native
  sequence — it does not maintain, publish or persist a derived book state from them.
- No core analytic may take an MBO-derived book depth, queue position or order count as an
  input while this ADR stands.
- Subscription is **per price**: both `request_market_depth` and `subscribe_to_market_depth`
  take a single `depth_price`. A recorder wanting N levels issues N subscriptions.

## Consequences

- Sequence discontinuity on a per-price MBO subscription is **expected and is not loss**: a
  one-price subscription observes a sparse slice of a book-wide counter. Measured span 2,644
  against 17 distinct on one price. The recorder must record subscription scope alongside
  the sequence statistics, or the numbers are uninterpretable (see ADR-003).
- Reversing this ADR requires either a vendor whole-book snapshot request that this client
  does not expose, or a different bootstrap source. Both are outside the current evidence.

## What is NOT claimed

That the vendor cannot supply a full book. Only that **this client, on this path, cannot
bootstrap one** — 116 returns one order per price.
