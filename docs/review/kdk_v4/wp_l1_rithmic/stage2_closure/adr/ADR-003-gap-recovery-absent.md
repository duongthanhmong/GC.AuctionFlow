# ADR-003 — Gap recovery does not exist; the recorder must fail closed on its own

**Status:** ACCEPTED
**Date:** 2026-07-31
**Evidence:** `EV-RECONNECT-GAP`
**MRBS anchor:** §18 DQ-001 (gap/duplicate/out-of-order measured; over tolerance → `Invalid`);
§23.2 reason code `DATA_INVALID`
**KDK anchor:** layer 1 data integrity — the layer nothing else may override

## Context

A client-side socket close was forced mid-stream. Measured:

```
last sequence before : 115,664,320
reconnected          : True, after 12.4 s (library backed off 10.8 s, one attempt)
subscriptions        : replayed automatically (TickerPlant._login, ticker.py:11-17)
first sequence after : 115,668,295
delta                : 3,975
```

Reconnect works. Subscription replay works. A post-reconnect template-116 snapshot is
obtainable. **The 3,975 sequence numbers are gone**, and there is no gap-fill request, no
resume-from-sequence, and no caller-visible signal — the library logs
`Reconnection successful` and the stream continues at a new sequence.

This is the most consequential finding for recorder design in the whole work package: a
naive recorder concatenating pre- and post-outage events produces a file that **looks
continuous and is not**.

## Decision

The recorder owns gap detection. It **must**:

1. Track `last_sequence` **per (instrument, subscription scope)** across the whole session,
   including across reconnects.
2. On reconnect, compare the first post-reconnect sequence with the last pre-reconnect
   sequence and emit a `SequenceDiscontinuity` record carrying
   `last_sequence_before`, `first_sequence_after`, `delta`, `outage_start_utc`,
   `outage_end_utc`, and `recovery_attempted = false`.
3. Mark the covering interval `DataQuality = Invalid` for the affected surface, and mark the
   segment `contains_unrecovered_gap = true`.
4. **Never** interpolate, backfill from an adjacent surface, or silently join the two
   halves. Historical backfill may be attached as a *separate*, separately-labelled source
   (ADR-006) but does not clear the flag.
5. Emit reason code `DATA_INVALID` on any consumer request that spans an unrecovered gap.

**Fail closed:** when discontinuity state cannot be determined — for example the recorder
restarted and has no `last_sequence` — the interval is `Invalid`, not `Ready`.

## Consequences

- A recorder run containing an unrecovered gap is still a **valid artifact**; it is simply
  not `Ready` over that interval. Discarding the whole run would lose good data either side.
- Because per-price MBO sequence is *expected* to be discontinuous (ADR-002), discontinuity
  alone is not a gap. Only a discontinuity **coincident with a connection event** is a gap.
  The recorder must distinguish them by correlating with connection state, not by threshold.
- No tolerance threshold is set here. DQ-001 says gaps are measured against a tolerance; that
  tolerance is a Parameter Registry item and is **not** invented in this ADR (ADR-005).

## What is NOT claimed

That the vendor lost data. Only that **this client cannot recover it and does not report
that it failed to**.
