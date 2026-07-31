# Recorder production-readiness — the four items the ruling still held open

The ruling of 2026-07-31 accepted futures **acquisition feasibility** and withheld futures
**recorder production-readiness**, naming reconnect, sequence recovery, deterministic book
rebuild, and historical/live parity as unproven. Those four were run.

**Two pass. One fails, and the failure is structural. One passes with a caveat that
matters more than the pass.**

| item | result |
|---|---|
| reconnect after a mid-stream socket loss | **PASS** — 12.4 s, automatic |
| subscription replay after reconnect | **PASS** — resumed with no re-subscribe |
| **sequence / gap recovery** | **FAIL — the mechanism does not exist** |
| **deterministic book rebuild from 116 + 160** | **FAIL — bootstrap is structurally incomplete** |
| historical vs live parity | **PASS** — 76 of 77 live trades present historically |

Sessions `exploit_20260731T112315Z`, `…112650Z`, `…112911Z`, `…113104Z`.

---

## 1. Reconnect and subscription replay — pass

The websocket was closed from the client side mid-stream. Measured:

```
20 depth events, last sequence 115,664,320
closing the websocket from the client side ...
  WARNING  WebSocket connection closed - signalling reconnect
  INFO     Waiting 10.8s before reconnect attempt #1
  INFO     Reconnection attempt #1
reconnected: True after 12.4s
  INFO     Reconnection successful.
18 depth events WITHOUT re-subscribing, first sequence 115,668,295
116 snapshot after reconnect: returned
```

`TickerPlant._login` (`plants/ticker.py:11-17`) replays both `market_data` and
`market_depth` subscriptions, and that is confirmed observationally: data resumed with no
re-subscription call from the probe. A post-reconnect template-116 snapshot was also
obtainable, so a re-bootstrap attempt is at least possible.

## 2. Sequence recovery — **the mechanism does not exist**

Across a **12.4-second** outage the MBO sequence moved:

```
115,664,320  →  115,668,295        delta 3,975
```

**Those 3,975 sequence numbers are gone.** Nothing in the vendor protocol as exposed by this
library replays them: there is no gap-fill request, no "resume from sequence N", and the
reconnect path re-subscribes from *now*. The library does not surface the discontinuity to
the caller either — it logs `Reconnection successful` and the stream simply carries on at a
new sequence.

This is the single most important recorder-design consequence in this document. **A recorder
built on this path must detect the discontinuity itself** — by tracking the last sequence
per instrument across a reconnect — **and must mark the affected interval as incomplete.**
Silently concatenating pre- and post-outage events produces a stream that looks continuous
and is not.

Nothing here says data was lost by the vendor. It says this client cannot recover it, and
does not tell you it failed to.

## 3. Deterministic book rebuild — **fails, and the cause is the bootstrap**

Method: fan out over **21 prices** at the vendor tick size (`min_fprice_change` = **1.0**,
read from reference data, not assumed), request a template-116 snapshot at each, subscribe
template-117 at each, then replay every template-160 update in **arrival order** keyed on
`exchange_order_id` — NEW inserts, CHANGE updates, DELETE removes — and compare the
aggregate against the vendor's own aggregated book (template 156).

| | |
|---|---|
| snapshots returned | 21 of 21 prices |
| **orders seeded per snapshot** | **exactly 1 — every time** |
| updates applied | 479 — NEW 151, CHANGE 172, DELETE 156 |
| **CHANGE for an order never seeded** | **75** |
| **DELETE for an order never seeded** | **16** |
| vendor book assembled | 1,089 price levels from 3,963 incremental messages |
| price levels compared | 21 |
| **exact size match** | **3** (4107.0, 4111.0, 4121.0) |

Sample of the divergence:

| price | rebuilt | vendor |
|---|---|---|
| 4102.0 | 70 | 74 |
| 4103.0 | 1 | 6 |
| 4104.0 | 3 | 6 |
| 4105.0 | 1 | 8 |
| 4106.0 | 2 | 5 |
| **4107.0** | **2** | **2** |

**The rebuilt size is below the vendor size at nearly every level, never above.** That is the
signature of an incomplete seed, not of mis-applied updates — and the seed is provably
incomplete: `entries per snapshot: {1}`. Template 116 returns **one order for the one price
requested**, so the 91 updates addressing orders that were never seeded had nothing to
modify.

**Conclusion: full-book MBO reconstruction is not achievable from template 116 + 160 as
exposed by `async_rithmic` 1.6.3.** Either a whole-book snapshot request exists in the
vendor protocol and this library does not expose it, or bootstrap must be sourced elsewhere.
This is an architectural blocker for an MBO recorder and belongs in the recorder design
decision, not in a probe result.

### Correction made during this work

The first comparison reported `vendor=None` at every level and scored 0/20. That was **my
bug, not a divergence**: I read the vendor book from `book_evts[-1]`, but template 156 is
**incremental** with BEGIN/MIDDLE/END/SOLO framing, so the last message is a delta, not a
book. Folding all 3,963 messages in arrival order produced the 1,089-level book above. The
3/21 result is the one measured after that fix.

## 4. Historical vs live parity — pass

Live `LAST_TRADE` was recorded over a 90-second window, then the same wall-clock window was
requested from `HISTORY_PLANT` and compared on `(second, price)`.

| | |
|---|---|
| live trades | 77 |
| historical records for the same window | 80 |
| **present in both** | **76** |
| live only | 1 |
| historical only | 4 |

**Historical replay covers 76 of 77 live trades.** The residual is consistent with boundary
effects at the window edges and with the live capture starting mid-second. This is real
parity evidence, and it means historical backfill is a usable source rather than a separate
universe.

### A trap worth recording

The first parity run returned **zero** overlap. Cause: `plants/base.py:567-577` localises a
**naive** datetime to the **system timezone**. This machine is UTC+7, so passing naive UTC
silently requested a window **7 hours off** — 25,201 seconds of offset, with no error, no
warning, and a perfectly plausible-looking 85 records returned.

**Historical requests must pass timezone-aware UTC datetimes.** A recorder that passes naive
timestamps will backfill the wrong window and have no indication that it did.

## 5. A defect class that defeated three attempts

`exchange_order_id`, `depth_price`, `update_type` and every other repeated protobuf field
arrive as `RepeatedScalarContainer`. It:

- has a `repr` **identical to a list**, so it reads correctly in every log and dump;
- is **not** a `list`, so `isinstance(v, list)` passes it through;
- has **no `__iter__` attribute** — it iterates via the legacy `__getitem__`/`__len__`
  protocol — so `hasattr(v, "__iter__")` **also** passes it through;
- is **unhashable**, so it explodes only when used as a dict key.

`pb()` in `rithmic_probe.py` tests `hasattr(v, "__iter__")` and therefore returns these
fields still wrapped. Two of my unwrap attempts used exactly the same two failing tests. The
working test is indexability (`__len__` plus `v[0]`).

This is recorded because a C# or Python recorder consuming these messages will meet the same
class of field, and because it is a case where the wrong value **prints correctly**.

## 6. Status

| ruling item | status now |
|---|---|
| reconnect | **PROVEN** — 12.4 s, automatic |
| resubscribe after reconnect | **PROVEN** — automatic, observed |
| sequence / gap recovery | **PROVEN ABSENT** — 3,975 lost, no recovery path, no caller-visible signal |
| deterministic book rebuild | **PROVEN NOT POSSIBLE** from 116+160 via 1.6.3 — 3/21 levels, seed is 1 order per price |
| historical / live parity | **PROVEN** — 76/77 |
| wire metrics | **measured at the socket** in every session |

**Still open, and not claimed:** sustained-load stability beyond these windows; throughput
under a full-book fan-out; the writer path; per-instrument fan-out at scale; whether the
vendor protocol offers a whole-book snapshot that this library does not expose.

**The ruling's verdict is unchanged by this document in one direction and strengthened in
another: futures acquisition feasibility stands as proven, and futures recorder
production-readiness is now not merely unproven but has two identified structural blockers —
absent gap recovery and an insufficient MBO bootstrap.** Stage 2 remains not accepted and
Task D remains not started.

---

## 7. On the three documents under review

`01-KDK_Parameter_Registry…`, `02-KDK_Machine_Readable_Binding_Specification_v1.1` and
`03-KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS` were assessed in the ruling but are **not present
in this workspace** — the `sandbox:/workspace/...` links do not resolve here. This is the
fourth time reviewer material has arrived as a path or a hash rather than as content
(previously `rithmic_probe.py` `b642b9fc…` then `c9b8340b…`, and
`test_rithmic_probe_offline.py` `8f459cd1…` then `8eca708b…`).

Consequently ruling steps 1–3 — the `SPEC_CONFLICT` decision record, converting MRBS into a
validatable schema, and the per-field capability/evidence manifest — cannot be started here.
Step 4 is this document. Step 5 was completed in `12_EXPLOITATION_PROBE_RESULTS.md`.
