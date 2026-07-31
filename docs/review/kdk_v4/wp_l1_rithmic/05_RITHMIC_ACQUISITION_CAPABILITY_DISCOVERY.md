# Stage 2 addendum — Rithmic acquisition-capability discovery

*(Requested as `04_…`; `04_STAGE2_CORRIGENDUM.md` was already taken by the previous instruction, so this
is `05_`. Same document, different prefix.)*

> **SUPERSEDED IN PART by `07_ACQUISITION_ADDENDUM_CORRIGENDUM.md`.** Withdrawn there: the
> "18 surfaces" count (17 non-zero, 14 unrequested); row **G-05** and every claim that aggregated
> `OrderBook.update_type` repairs the DOM `UpdateAction="Unknown"` defect (that enum is batch state,
> not per-level New/Change/Delete); the claim that `_on_tick` discards fields (it retains the full
> tick — the loss is at the persistence boundary); the reconnect classification; the reference-data
> conclusion (now a hypothesis); the IV/Greeks evidence basis (now a real all-module scan); and the
> P-6 MBO probe design. The capability matrix is superseded by
> `08_ACQUISITION_CAPABILITY_MATRIX.csv`, which separates nine layers.

**Status: offline enumeration COMPLETE. Live probe NOT RUN — blocked by `SEC-001`, see §6.**
Task D not started. No production source or tests changed.

---

## 0. The one thing I did not do, and why

The instruction authorises a bounded live market-data probe. **I have not run it**, because the same
reviewer set a hard gate that this instruction does not mention or lift:

> `ODR-L1-05` — **`CONFIRMED — HARD GATE` (2026-07-31)**. *"No live Rithmic connection, no collector run
> and no credential test until rotation is documented. Not a preference; a blocking prerequisite."*

Measured now, in the repository:

```
docs/review/evidence/SECURITY_INCIDENT_SEC001.md:51
    - Rithmic password rotated: **PENDING (operator)**
docs/review/evidence/SECURITY_INCIDENT_SEC001.md:52
    - README username removed/rotated: **PENDING (Phase A)**
```

The credential the probe would use is the one that incident report says is exposed and unrotated. I am not
going to walk through that gate on an instruction that appears not to have it in view — **one sentence from
you clears it** (§6). Everything that does not depend on connecting is finished below.

## 1. Acquisition path behind `.gcae`

`.gcae` holds recorder output only (Stage 1: zero code files). Its manifests, envelopes and payload schemas
trace back to producers in `GC.AuctionFlow`. **The two branches are not the same acquisition path, and the
difference turns out to be the most important finding in this document.**

```
BRANCH A — the path that produced .gcae
  ATAS platform (its own Rithmic connection, credentials NOT ours)
    → GcAuctionFlowIndicator callbacks
    → TradeRecorderHost                       Recorder/TradeRecorderHost.cs:140-180
    → {Trade,DepthTo,Mbo}ToRawEventAdapter
    → RawEventEnvelope (JSON)                 Recorder/RawEventEnvelope.cs
    → SegmentWriter/FrameCodec → .seg + .sha256
    → ManifestWriter → manifest.json

BRANCH B — the direct path, our FIN credential
  %USERPROFILE%\.gcae\rithmic.env             rithmic/config.py:12,55
    → OptionFlowRithmic.connect()             rithmic/client.py:79-100
    → async_rithmic, TICKER_PLANT only        rithmic/client.py:100
    → search_symbols / front_month_contract   rithmic/client.py:181-198
    → request_market_data_update              rithmic/client.py:200
    → _on_tick / _on_market_depth / _on_order_book
    → gex/engine → artifacts/optionflow/<P>/levels.json
```

**`.gcae` was produced entirely by Branch A.** No `.seg` in the corpus came from our FIN credential — the
recorder consumes ATAS callbacks. That matters for every "is it obtainable?" question below: what the ATAS
callback surface delivers and what the Rithmic wire protocol delivers are **not the same set**.

## 2. Enumerated surface of the pinned dependency

`tools/rithmic_surface_enum.py`, `raw/11_rithmic_surface_enum.txt`. Offline; no connection, no credential
read.

**Version: `async_rithmic` 1.6.3 installed. `requirements.txt` says `async_rithmic>=1.0` — NOT pinned.**
Every statement below is true of 1.6.3 and is not guaranteed for any other resolved version. That alone
keeps the dependency at `DEPENDENCY_OWNED_UNVERIFIED` for reproducibility purposes.

### 2.1 `UpdateBits` — 18 subscribable surfaces; the code requests 3

```
LAST_TRADE 1 · BBO 2 · ORDER_BOOK 4 · OPEN 8 · OPENING_INDICATOR 16 · HIGH_LOW 32 ·
HIGH_BID_LOW_ASK 64 · CLOSE 128 · CLOSING_INDICATOR 256 · SETTLEMENT 512 · MARKET_MODE 1024 ·
OPEN_INTEREST 2048 · MARGIN_RATE 4096 · HIGH_PRICE_LIMIT 8192 · LOW_PRICE_LIMIT 16384 ·
PROJECTED_SETTLEMENT 32768 · ADJUSTED_CLOSE 65536
```

`client.py:55-66` builds `LAST_TRADE | BBO | OPEN_INTEREST`. **Never requested, though exposed:**
`SETTLEMENT`, `PROJECTED_SETTLEMENT`, `MARKET_MODE` (trading status), `OPEN`/`CLOSE`, `HIGH_LOW`,
`HIGH_BID_LOW_ASK`, `OPENING_INDICATOR`/`CLOSING_INDICATOR`, `MARGIN_RATE`, `HIGH_PRICE_LIMIT`,
`LOW_PRICE_LIMIT`, `ADJUSTED_CLOSE`, and `ORDER_BOOK` on the option path.

KDK wants settlement and session statistics for layer 1/2. **They are one subscription bit away and have
never been asked for.**

### 2.2 Request templates exposed (43), market/reference subset

`request_market_data_update` · `request_depth_by_order_snapshot` · `request_depth_by_order_updates` ·
`request_reference_data` · `request_search_symbols` · `request_front_month_contract` ·
`request_tick_bar_replay` · `request_tick_bar_update` · `request_time_bar_replay` ·
`request_time_bar_update` · `request_list_exchange_permissions` · `request_trade_routes`

Used by current code: `market_data_update`, `search_symbols`, `front_month_contract`. **Unused:** MBO
snapshot **and** updates as first-class requests, reference data, both bar-replay families (historical
data), and exchange-permission listing.

### 2.3 The decisive schema — `DepthByOrder` (MBO)

```
template_id, symbol, exchange, sequence_number, update_type, transaction_type,
depth_price, prev_depth_price, prev_depth_price_flag, depth_size, depth_order_priority,
exchange_order_id, ssboe, usecs, source_ssboe, source_usecs, source_nsecs, jop_ssboe, jop_nsecs
```

**The Rithmic wire protocol carries `sequence_number`.** It also carries three separate clock domains:
`source_*` (exchange), `ssboe/usecs` (receive), `jop_*`.

The recorder records `NativeSequenceAvailable = false` on **75,904 of 75,904** decoded records
(`raw/09_record_census.txt`) and sets `NativeSequenceAbsent`. **That absence is a property of Branch A, not
of Rithmic.** The native sequence exists on the wire and is lost because the data arrives through ATAS
callbacks instead.

This is the single most consequential finding of Stage 2: **the reason MBO is not reconstructable is the
acquisition path, not the vendor.** `ResponseDepthByOrderSnapshot` additionally carries `depth_side` and a
full snapshot, so snapshot-vs-incremental is a first-class protocol feature too.

### 2.4 `LastTrade` — fields the current tick handler discards

```
trade_price, trade_size, aggressor, exchange_order_id, aggressor_exchange_order_id,
net_change, percent_change, volume, vwap, trade_time, is_snapshot, presence_bits, clear_bits,
ssboe, usecs, source_ssboe, source_usecs, source_nsecs, jop_ssboe, jop_nsecs
```

`_on_tick` (`client.py:110-123`) keeps `bid_price`, `ask_price`, `trade_price`, `ssboe` — **four fields**.
Discarded on the floor: `aggressor` (KDK's aggressor classification), `trade_size`, `volume`, `vwap`,
`exchange_order_id`, `is_snapshot`, and the entire `source_*` exchange-time domain.

### 2.5 `BestBidOffer` and `OrderBook`

`BestBidOffer`: `bid_price, bid_size, bid_orders, bid_implicit_size, bid_time, ask_*…, lean_price,
is_snapshot, presence_bits, clear_bits, ssboe, usecs`. **`bid_orders`/`ask_orders` are order counts at the
touch** and `*_implicit_size` separates implied liquidity — neither is captured today.

`OrderBook`: `update_type`, `bid/ask price/size/orders`, `impl_*_size`. **`update_type` exists** — which is
directly relevant to the `Depth UpdateAction = "Unknown"` defect on 35,025 recorded rows.

### 2.6 `ResponseReferenceData` — option identity, first-class

```
symbol, exchange, exchange_symbol, symbol_name, trading_symbol, trading_exchange, product_code,
instrument_type, underlying_symbol, expiration_date, currency, put_call_indicator, tick_size_type,
price_display_format, is_tradable, strike_price, ftoq_price, qtof_price,
min_qprice_change, min_fprice_change, single_point_value
```

`underlying_symbol`, `expiration_date`, `put_call_indicator`, `strike_price`, `single_point_value` are
**served by the vendor**. KDK Ch 82's "options must map to the correct underlying futures contract" is
answerable from `underlying_symbol` directly, instead of being inferred — and `instrument_master_gcae.json`
(57,242 orphaned entries, no provenance) may be unnecessary entirely.

### 2.7 IV and Greeks — vendor vs local

**No IV or Greek field appears in any of the 104 generated modules.** `implied` matches nothing in the
market-data schemas. So IV/Greeks are **locally calculated only** (`gex/black76.py`) — correctly, and this
now has evidence rather than assumption.

### 2.8 Entitlement

`request_list_exchange_permissions` → `ResponseListExchangePermissions{exchange, level_1_market_data,
level_2_market_data, entitlement_flag}`. **This answers "what is FIN entitled to?" with no market-data
subscription at all** — the smallest, safest possible probe, and the one I would run first.

### 2.9 Reconnect — correcting my own Stage 2 classification

Top-level `ReconnectionSettings` and `RetrySettings` exist in 1.6.3. My Stage 2 corrigendum classified
reconnect as `ABSENT`. **That was correct about our code and incomplete about the dependency:** the library
exposes reconnection configuration and our code never constructs either object. Reclassified: **absent in
our code, available in the dependency, unverified in behaviour.**

## 3. Recorder-gap list — obtainable from Rithmic, discarded or never requested

| # | Surface | Exposed by 1.6.3 | Requested by our code | In `.gcae` | Consequence |
|---|---|---|---|---|---|
| G-01 | MBO `sequence_number` | **Yes** (`DepthByOrder`) | No | **No** — `NativeSequenceAvailable=false` 75,904/75,904 | Book reconstruction foreclosed; **recoverable via Branch B** |
| G-02 | Exchange time `source_ssboe/usecs/nsecs` | **Yes** | No | No | Only receive-time survives; EventTime unavailable |
| G-03 | `aggressor` on LastTrade | **Yes** | No | Derived indirectly | KDK aggressor classification done by inference, not vendor field |
| G-04 | `is_snapshot` | **Yes** | No | No — `snapshotCompletionKnown` hard-coded `false` | Snapshot vs incremental unresolvable |
| G-05 | `OrderBook.update_type` | **Yes** | No | No — `UpdateAction="Unknown"` ×35,025 | DOM New/Change/Delete unrecoverable |
| G-06 | `SETTLEMENT`, `PROJECTED_SETTLEMENT` | **Yes** (bits 512, 32768) | No | No | Settlement/reference values absent |
| G-07 | `MARKET_MODE` | **Yes** (1024) | No | No | Trading status / session state absent |
| G-08 | `OPEN`/`CLOSE`/`HIGH_LOW`/`HIGH_BID_LOW_ASK` | **Yes** | No | No | Session OHLC absent |
| G-09 | `bid_orders`/`ask_orders`, `*_implicit_size` | **Yes** | No | No | Touch order-count and implied size absent |
| G-10 | `vwap`, `volume`, `trade_size` on LastTrade | **Yes** | No | Partly | Vendor VWAP/volume discarded |
| G-11 | `request_reference_data` | **Yes** | **No** | No | Option identity inferred instead of served |
| G-12 | Tick/time bar **replay** | **Yes** (4 templates) | No | No | No historical backfill path exists |
| G-13 | `request_list_exchange_permissions` | **Yes** | No | No | Entitlement never enumerated |
| G-14 | MBO snapshot/updates as first-class requests | **Yes** | No | Via ATAS only | Branch B never subscribes MBO |

## 4. Capability matrix

Machine-readable: `06_ACQUISITION_CAPABILITY_MATRIX.csv`, with the eight dimensions kept separate.
**Every "Request accepted by server", "FIN account entitled" and "Callback observed" cell reads
`NOT_PROBED`**, because no live probe was run. API exposure is recorded as exposure and nothing more.

## 5. Mapping to KDK requirements

| KDK requirement | Obtainable from Rithmic (1.6.3 schema) | Present in `.gcae` today |
|---|---|---|
| Executed trade price/qty | Yes — `trade_price`, `trade_size` | Yes |
| Aggressor classification | **Yes — vendor `aggressor` field** | Inferred only |
| EventTime vs ReceiveTime | **Yes — `source_*` vs `ssboe/usecs`** | Receive only |
| Sequence / unique key | **Yes — `sequence_number` (MBO)** | **No** |
| BBO + sizes | Yes, plus order counts and implicit size | Price/size only |
| Depth New/Change/Delete | **Yes — `update_type`** | **No** (`"Unknown"`) |
| MBO identity/priority/lifecycle | Yes — `exchange_order_id`, `depth_order_priority`, `update_type`, `transaction_type` | Identity+priority yes; lifecycle raw only |
| Futures/option underlying mapping | **Yes — `underlying_symbol`** | Inferred from master |
| Option strike/expiry/put-call | **Yes — vendor-served** | From master |
| Option OI point-in-time | Yes — `OPEN_INTEREST` bit | Requested; 0/300 populated |
| Publication lag | Derivable from `source_*` vs `ssboe` | Not derivable |
| IV / Greeks | **No vendor field — local calculation only** | Local only |

## 6. The live probe — designed, bounded, NOT executed

Ready to run the moment SEC-001 is cleared. Ordered least-invasive first, and I would stop at the first
entitlement error rather than retry:

| # | Probe | Request | Why it is safe |
|---|---|---|---|
| P-1 | Entitlement | `request_list_exchange_permissions` | No market-data subscription at all |
| P-2 | Front month | `request_front_month_contract` (GC/COMEX) | One reference lookup |
| P-3 | Reference data | `request_reference_data` on that one symbol | One reference lookup |
| P-4 | L1 | `market_data_update` `LAST_TRADE\|BBO`, one symbol, 30 s | Bounded window, one instrument |
| P-5 | Session stats | add `SETTLEMENT\|MARKET_MODE\|OPEN\|HIGH_LOW`, 30 s | Same symbol, additive bits |
| P-6 | MBO | `depth_by_order_snapshot` then `_updates`, 30 s | Confirms `sequence_number` on the wire |
| P-7 | Options | `search_symbols` + `reference_data` on **4** contracts (2 expiries × call/put) + `OPEN_INTEREST` | Minimal set, no full chain |

All output would go to a **new quarantine directory outside both corpora**, sanitized, with no credential
value ever printed. No order, position, PnL or account request appears anywhere above — `request_new_order`,
`request_modify_order`, `request_cancel_*`, `request_exit_position`, `request_pnl_*`, `request_account_*`
are excluded by construction.

**What I need from you — one of:**

1. **"SEC-001 rotation is done"** (or a note that you accept the risk) → I run P-1…P-7 and report.
2. **"Run P-1 only"** → entitlement enumeration alone; it needs no market-data subscription and is the
   smallest possible exposure.
3. **"Hold the probe"** → this document stands as the offline deliverable. **Stage 2 then remains
   `OPEN/BLOCKED`** — it does not close automatically without the probe unless you explicitly change
   the acceptance scope.

I am not asking you to re-authorise the probe — you already did. I am asking whether the credential-rotation
gate you set is satisfied, because I cannot verify that from the repository.
