# Live FIN/Rithmic acquisition — **DATA ACQUIRED**

**Outcome: `AUTHENTICATED / DATA_ACQUIRED`.** The owner replaced the FIN credential; the third run logged
in and returned real market data across every surface attempted.

**This is the first capture in the work package containing genuinely new FIN/Rithmic data.**

> **CORRECTED 2026-07-31.** Two headline numbers below are mislabelled and are corrected
> here rather than edited away. **`88,624` is not 88,624 market events** — 87,649 of them
> are symbol-catalog rows from `P7`; actual futures streaming was **951 events**, plus 10
> bars and 8 ticks from mis-selected instruments. **`187,220` is the size of the evidence
> JSON, not wire bytes** — the probe set `payload_bytes = len(serialized_json)`.
> `subscription_accepted` was a flag this probe set itself, **not** a server ACK.
> See `12_EXPLOITATION_PROBE_RESULTS.md` §1 for the measured replacements.

| | |
|---|---|
| session | `probe_20260731T102133Z` |
| UTC | `2026-07-31T10:21:33Z` → `10:24:13Z` (160 s) |
| local | `2026-07-31T17:21:33+07:00` |
| command | `python tools/rithmic_probe.py --out <quarantine> --window 25` |
| **records / callbacks** | **88,624** |
| **payload bytes** | **187,220** (raw on disk 242,277 across 13 files) |
| clean teardown | ticker plant disconnected cleanly; history plant disconnected |

Prior runs `probe_20260731T052809Z` and `probe_20260731T101559Z` both failed at login with
`rp_code 13 permission denied`. The only thing that changed is the credential. Everything else — gateway,
system name, app identity, code — is unchanged, which retrospectively confirms the diagnosis: the refusal
was account state, not configuration.

---

## 1. Entitlement — answered

`list_exchanges()` returned **4 exchanges, all fully entitled**:

| exchange | level 1 | level 2 | flag |
|---|---|---|---|
| **COMEX** | enabled | **enabled** | 1 |
| NYMEX | enabled | **enabled** | 1 |
| CBOT | enabled | **enabled** | 1 |
| CME | enabled | **enabled** | 1 |

**Level 2 is enabled on COMEX.** That is the entitlement MBO and depth work requires, and it is now
measured rather than assumed.

## 2. Per-probe results

| probe | surface | accepted | callbacks | bytes | outcome |
|---|---|---|---|---|---|
| P0 | authenticate, TICKER_PLANT | — | — | — | **AUTHENTICATED** |
| P1 | `list_exchanges()` | yes | 4 | 835 | 4 exchanges, L1+L2 enabled |
| P2 | `get_front_month_contract()` | yes | 1 | 43 | **`GCZ6`** |
| P3 | `get_reference_data()` | yes | 1 | 501 | **19 non-null fields** |
| P4 | L1 `LAST_TRADE\|BBO` | yes | **104** | 53,705 | bid 4110.0 / ask 4110.2 |
| P5 | 14 unrequested `UpdateBits` | yes | **34** | 18,965 | aggressor, vwap, volume, … |
| P6a | aggregated `ORDER_BOOK` | yes | **762** | 71,650 | `update_type` 3/4/6/7 |
| P6b | `DepthByOrder` / MBO | yes | **51** | 28,100 | **`sequence_number` on 51/51** |
| P7 | option chain discovery | yes | **87,649** | 2,921 | search returned the whole universe |
| P8 | option market data | yes | 8 | 3,740 | 8 ticks, 1 template-158 frame |
| P9 | disconnect ticker | — | — | — | clean |
| P10 | **HISTORY_PLANT** + bar replay | yes | **10** | 6,760 | 10 × 1-min bars |

## 3. The finding that matters most — MBO native sequence is real

`P6b` returned 51 `DepthByOrder` events with **`sequence_number` present on 51 of 51**, and this field set:

```
sequence_number, update_type, transaction_type,
depth_price, prev_depth_price, prev_depth_price_flag, depth_size,
depth_order_priority, exchange_order_id,
ssboe, usecs, source_ssboe, source_usecs, source_nsecs, jop_ssboe, jop_nsecs
```

`update_type` distribution: **`[1]` NEW ×11 · `[2]` CHANGE ×30 · `[3]` DELETE ×10**.

Every Stage 2 prediction about this surface is now **confirmed live**:

| predicted from schema | measured |
|---|---|
| `sequence_number` exists on the wire | **YES — 51/51** |
| per-level New/Change/Delete only on `DepthByOrder` | **YES — 1/2/3 observed** |
| three clock domains | **YES — `source_*`, `ssboe/usecs`, `jop_*` all populated** |
| order identity + queue priority | **YES — `exchange_order_id`, `depth_order_priority`** |

**The ATAS recorder path records `NativeSequenceAvailable=false` on 75,904/75,904 events. The direct path
delivers it on 100% of events.** The gap was the acquisition path, exactly as Stage 2 argued — and that is
no longer an inference.

**Caveat, stated:** `sequence_gaps=49` across 51 events. That is expected and **not** a defect — the probe
subscribed only 3 price levels, so it sees a sparse slice of a book-wide sequence. Gap analysis is only
meaningful over a full-book subscription, which this was not.

## 4. Fields the ATAS path loses, now observed on the wire

> **CORRECTED 2026-07-31 by `12_EXPLOITATION_PROBE_RESULTS.md` §5.** This section attributed
> the fields below to the **session-stat bits**. That attribution was **wrong**. `P5`'s 34
> records are `data_type` 1 and 2 — `LAST_TRADE` and `BBO` — collected from a tick sink
> shared with `P4`, which had not been unsubscribed. Per-bit testing later showed that
> **none of the 14 session-stat bits produces a tick at all**; each returns a template
> `async_rithmic` 1.6.3 cannot parse and discards.
>
> **The fields are real and `aggressor` is genuinely a vendor field — it arrives on
> `LAST_TRADE` (template 150).** Only the attribution to `P5` was incorrect.

From `P5` (session-stat bits) and `P4`:

`aggressor` ×4 · `aggressor_exchange_order_id` ×4 · `exchange_order_id` ×5 · `trade_size` ×5 ·
`volume` ×5 · `vwap` ×5 · `net_change` ×5 · `percent_change` ×5 · `trade_price` ×5 ·
`lean_price` ×24 · `ask_orders` ×7 · `is_snapshot` · `presence_bits` / `clear_bits`

**`aggressor` is delivered by the vendor.** KDK's aggressor classification does not have to be inferred.

`P6a` aggregated book returned `bid_orders`, `ask_orders`, `impl_bid_size`, `impl_ask_size` — order counts
and implied size at each level, none of which the current path keeps.

## 5. `ORDER_BOOK.update_type` — corrigendum item 4 confirmed live

Observed distribution: **`7` SOLO ×365 · `4` BEGIN ×198 · `6` END ×198 · `3` SNAPSHOT_IMAGE ×1**.

Exactly batch/transaction framing, **not** per-level New/Change/Delete — as the withdrawal of `G-05`
stated. `SNAPSHOT_IMAGE` appearing once at the start also confirms snapshot-vs-incremental is a real,
observable distinction on this surface.

## 6. History plant — the "1011 permission denied" comment is now false

`client.py:97-98` says the non-ticker plants *"get permission denied (1011) and would abort the whole
connect"*. **`HISTORY_PLANT` authenticated on this account** and returned 10 one-minute bars for `GCZ6`.

So historical backfill is available. That comment describes the **old** account, not this one, and
`M-12`/the bar-replay rows move from "exposed but unused" to **obtainable**.

## 7. What did NOT work — my selection logic, not the API

`P7` searched `"OG"` and got **87,649 symbols** — the whole universe, including `Future` and
`Future Strategy` instruments such as `1OTG7-1OTJ7`. The first six taken were therefore futures spreads,
and their reference data correctly returned `underlying_symbol=None`, `strike_price=None`,
`put_call_indicator=None`.

**That is a defect in my instrument-selection rule, not evidence about option reference data.** The
required "2 calls + 2 puts nearest ATM at nearest expiry" selection was not achieved, so:

- exact option→futures `underlying_symbol` mapping: **still UNVERIFIED**
- option strike / expiry / put-call from the vendor: **still UNVERIFIED**
- option OI via template-158: 1 frame seen, but on the wrong instruments — **inconclusive**

`P8`'s 8 ticks are likewise from futures strategies. **No option-specific claim is made from this run.**
The reviewer's corrected selection rule (nearest expiry, 2 calls + 2 puts near ATM) is exactly what is
needed and is the first thing to run next.

## 8. Evidence

| artifact | bytes | sha-256 |
|---|---|---|
| `P1_exchange_permissions.json` | 835 | `2f30f8e346d90699…` |
| `P3_reference_data.json` | 501 | `da2fa2b8159e38c6…` |
| `P4_l1_trades_bbo.json` | 53,705 | `7764c3382210b036…` |
| `P5_session_stats.json` | 18,965 | `05d1c2761183c61a…` |
| `P6a_order_book.json` | 71,650 | `669f09d8dc26c25a…` |
| `P6b_depth_by_order.json` | 28,100 | `50dfc72138e29260…` |
| `P7_option_search.json` | 55,057 | `ea07c7e0bd3ea043…` |
| `P10_historical_time_bars.json` | 6,760 | `23044e8b180b4419…` |

Full manifest: `probe/probe_20260731T102133Z_capture_manifest.csv` (13 files). Sanitized transcript and
per-probe results committed. **Raw captures stay in the quarantine directory outside `.gcae` and outside
the repository.** No credential value was read, printed or committed; the handler-level redaction held on
this run.

## 9. Answering the question that was asked

*"Does it provide the data KDK requires?"*

| KDK layer-1 requirement | status after this run |
|---|---|
| executed trades, price, quantity | **YES** |
| aggressor classification | **YES — vendor field** |
| EventTime vs ReceiveTime | **YES — `source_*` vs `ssboe/usecs`** |
| sequence / unique key | **YES — `sequence_number` 51/51** |
| BBO + sizes | **YES**, plus order counts and implied size |
| Depth New/Change/Delete | **YES — on `DepthByOrder`** |
| MBO order identity + priority | **YES** |
| level-2 entitlement | **YES — COMEX, NYMEX, CBOT, CME** |
| historical backfill | **YES — history plant + bar replay** |
| futures reference data | **YES — 19 fields incl. `single_point_value` 100.0** |
| option underlying / strike / expiry / P-C | **NOT YET — selection defect, §7** |
| option OI point-in-time | **NOT YET — same cause** |
| vendor IV / Greeks | **NO — absent from the protocol; local calculation confirmed** |

Everything on the futures side that KDK Layer 1 asks for is **obtainable and measured**. The options side
is unproven only because the probe picked the wrong instruments, which is a one-line fix.

**Task D is still not started and Stage 2 is not declared accepted.**
