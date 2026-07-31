# ADR-006 — Historical and live publish capability separately, despite measured parity

**Status:** AWAITING_OWNER_APPROVAL
**Approval:** none recorded. This ADR states a decision the evidence supports; it is NOT in force until the owner approves it.
**Date:** 2026-07-31
**Evidence:** `EV-HIST-PARITY`
**MRBS anchor:** §18 DQ-004 (*"Historical và live phải công bố capability riêng — không giả
định parity"*); §24.1 no-look-ahead

## Context

Parity was measured, not assumed. Live `LAST_TRADE` over a 90-second window against the same
wall-clock window requested from `HISTORY_PLANT`:

| | |
|---|---|
| live trades | 77 |
| historical records, same window | 80 |
| **present in both** | **76** |
| live only | 1 |
| historical only | 4 |

Historical replay arrives on template **207** as tick bars, but **180 of 180** in a separate
measurement carried `num_trades = 1` with `open == high == low == close` — each bar is one
trade, so the granularity is trade-by-trade inside a bar envelope.

## Decision

1. Historical and live are **two capability rows** in the manifest, each with its own
   `CapabilityState`. Parity evidence is attached; it does not merge them.
2. A dataset assembled from both sources records `DataSourceId` per record and the recorder
   emits `source_mix` on the segment. A consumer may reject mixed segments.
3. Historical records are labelled `ReplayCompatible`, never `LiveOnly`, and carry the fact
   that they are **bar-enveloped single trades**, not raw ticks. Calling them a raw tick feed
   would misstate the shape.
4. Historical backfill may sit **beside** an unrecovered live gap but does not clear it
   (ADR-003 §4). The gap flag describes the live stream's integrity.

## The trap this ADR exists to prevent

`plants/base.py:567-577` localises a **naive** datetime to the **system timezone**. On a
UTC+7 machine, passing naive UTC silently requested a window **7 hours off** — 25,201 seconds
— and returned a perfectly plausible 85 records. The first parity run scored **zero** overlap
for this reason and nothing errored.

**All historical requests must pass timezone-aware UTC.** The schema marks historical window
bounds as requiring an explicit UTC offset, and the test suite rejects a naive timestamp.

## Consequences

- No-look-ahead (§24.1) is unaffected: historical requests are bounded by an explicit window
  and the recorder stamps `ProcessingTime` separately from `EventTime`.
- The 1 live-only and 4 historical-only records are **not** explained here. They are
  consistent with window-boundary effects; that is a hypothesis, not a finding, and the
  manifest records parity as `76/77` rather than as "equivalent".
