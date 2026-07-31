# Acquisition addendum — corrigendum 1

Continues from `7555b87`. **Stage 2 remains OPEN and BLOCKED by `SEC-001`. No live probe was run.**
Task D not started. No production source or tests changed.

---

## 11. The gate first, because it governs everything else

**No probe ran — not P-1, not any other.** The reviewer's point is accepted without argument: `P-1
list_exchanges()` still requires a credential login, so "market-data only" does not exempt it from
`ODR-L1-05`. It was never a bypass and is not described as one anywhere now.

The gate clears only by (a) explicit confirmation that rotation is complete, or (b) explicit withdrawal of
`ODR-L1-05` by the owner. **Stage 2 does not close automatically without the probe** — if the probe is
held, Stage 2 stays `OPEN/BLOCKED` unless the owner changes the acceptance scope. I do not assert
otherwise.

*Owner action taken this session, separate from the probe:* the local recorder spool was pruned at the
owner's instruction — see §12. That is data-lifecycle housekeeping, not a probe.

## 1. `UpdateBits` — corrected count

Measured (`raw/11`, §1):

```
total members                : 18
members == 0 (UNSPECIFIED)   : 1
non-zero subscribable bits   : 17
requested by our code        : 3   (LAST_TRADE, BBO, OPEN_INTEREST)
non-zero and never requested : 14
```

The 14: `ORDER_BOOK, OPEN, OPENING_INDICATOR, HIGH_LOW, HIGH_BID_LOW_ASK, CLOSE, CLOSING_INDICATOR,
SETTLEMENT, MARKET_MODE, MARGIN_RATE, HIGH_PRICE_LIMIT, LOW_PRICE_LIMIT, PROJECTED_SETTLEMENT,
ADJUSTED_CLOSE`.

**"18 surfaces" is withdrawn. 17 subscribable, 14 unrequested.**

## 2. Enumerator replaced

The old tool reported empty sections because `async_rithmic` creates events in
`RithmicClient.__init__` (`client.py:35-56`) and reaches plant methods by instance delegation. The new
tool constructs a client with **placeholder credentials and never calls `connect()`**, so instance state is
visible without a login.

Now enumerated: `RithmicClient` + `TickerPlant` + `OrderPlant` + `PnlPlant` + `HistoryPlant` +
`RepositoryPlant` + `BasePlant`; **14 event handles**; **39 delegated instance callables**; **104 protobuf
modules, 104 messages, 1,080 fields**, each field printed; every enum with its values and an explicit
zero/non-zero split.

Selected delegated callables relevant here: `list_exchanges`, `get_reference_data`,
`get_front_month_contract`, `search_symbols`, `subscribe_to_market_data`, `subscribe_to_market_depth`,
`request_market_depth`, `unsubscribe_from_*`, `get_historical_tick_data`, `get_historical_time_bars`,
`subscribe_to_time_bar_data`, `get_system_info`.

## 3. Capability layers separated

`08_ACQUISITION_CAPABILITY_MATRIX.csv` now carries **nine** distinct columns:
`protocol_schema_present` · `public_callable_in_1_6_3` · `called_by_gc_auctionflow` ·
`server_request_accepted` · `fin_entitlement` · `callback_observed` · `persisted_by_current_path` ·
`field_value_completeness` · `kdk_usability`.

Real public method names are used throughout — `list_exchanges()`, `get_reference_data()`,
`request_market_depth()`, `subscribe_to_market_depth()` — not template module names. Columns 4, 5 and 6
read `NOT_PROBED` on every row.

## 4. `OrderBook.update_type` — G-05 WITHDRAWN

Measured from the installed package:

```
OrderBook.UpdateType    : UPDATETYPE_UNSPECIFIED=0, CLEAR_ORDER_BOOK=1, NO_BOOK=2,
                          SNAPSHOT_IMAGE=3, BEGIN=4, MIDDLE=5, END=6, SOLO=7
DepthByOrder.UpdateType : UPDATETYPE_UNSPECIFIED=0, NEW=1, CHANGE=2, DELETE=3
DepthByOrder.TransactionType : BUY=1, SELL=2
```

`OrderBook.update_type` is **book transaction/batch state**, not per-level New/Change/Delete. My claim that
it repairs the ATAS DOM `UpdateAction="Unknown"` defect was **semantically wrong and is withdrawn**, along
with row `G-05`.

**The sharper finding that replaces it:** per-level New/Change/Delete exists **only** on `DepthByOrder`,
i.e. at the *order* level. So aggregated-depth New/Change/Delete is not obtainable from Rithmic either —
the ATAS `UpdateAction="Unknown"` gap cannot be closed by subscribing aggregated `ORDER_BOOK`. It can only
be closed by reconstructing aggregated depth from per-order MBO events, which in turn needs the native
sequence and snapshot completion that Branch A does not carry.

## 5. Tick-field preservation — corrected, and the conclusion flips

`_on_tick` (`client.py:110-123`) appends the **complete tick dict** to `_ticks` and forwards it unchanged to
every registered handler. Only `_quotes` keeps the narrow `{bid, ask, last, ts}` view. My "discards the
rest" statement was wrong.

Traced to the persistence boundary, which is what actually matters:

| consumer | reads | in the production path? |
|---|---|---|
| `mid_or_last()` → `_quotes` | narrow view | **Yes** — `live.py:37,41,72`, `run_snapshot.py:141` |
| `collect_for()` → `list(self._ticks)` | **full ticks** | **No caller anywhere in the live path** |
| `depth_events()` | full depth events | only `tools/mbo_probe.py`, a tool |
| `write_levels()` → `levels.json` (`gcae-optionflow-v1`) | `LevelsDoc` only | **Yes — and it carries no raw tick field at all** |

**Correct statement:** the full tick is *retained in memory for the process lifetime* and **never
persisted**. `aggressor`, `trade_size`, `volume`, `vwap`, `exchange_order_id` and the `source_*` timestamps
are not discarded by the callback — they are captured, held, and then dropped at the persistence boundary
because nothing writes them.

That is a materially better position than "discarded": the data is already in hand, and persisting it is a
writer change, not a re-acquisition.

## 6. Reconnect / resubscription — reclassified on four axes

Measured from the installed package:

```
RithmicClient.__init__ (client.py:74,85) constructs DEFAULTS even when the caller passes nothing:
  reconnection_settings = ReconnectionSettings(max_retries=None, backoff_type='linear',
                                               interval=10, max_delay=120, jitter_range=(0.5, 2.5))
  retry_settings        = RetrySettings(max_retries=3, timeout=30.0, jitter_range=(0.5, 2.0))

TickerPlant tracks subscriptions   (ticker.py:66,85,145,166  _subscriptions["market_data"|"market_depth"])
TickerPlant._login() REPLAYS them  (ticker.py:11-17)
```

| axis | classification |
|---|---|
| explicit project configuration | **ABSENT** — neither settings object is constructed anywhere in `GC.AuctionFlow` |
| dependency implementation in installed 1.6.3 | **IMPLEMENTED** — defaults exist; subscriptions tracked and replayed on re-login |
| runtime behaviour on FIN | **NOT_PROBED** |
| reproducibility | **UNPINNED** — `requirements.txt` says `>=1.0`, so a fresh install may resolve elsewhere |

Composite label: `DEPENDENCY_OWNED_IMPLEMENTED_IN_1.6.3 / RUNTIME_UNPROVEN / REPRODUCIBILITY_UNPINNED`.
My earlier "available but unverified" understated the dependency and is corrected.

## 7. Reference data — scoped down to a hypothesis

`ResponseReferenceData.underlying_symbol` is a **candidate** vendor mapping field. Schema presence does not
prove it names the exact expiry-specific futures contract KDK Ch 82 requires — it could equally carry a
product root.

- Exact option→futures-contract mapping: **UNVERIFIED**, pending live samples.
- "`instrument_master_gcae.json` may be unnecessary": **HYPOTHESIS**, not a conclusion. The file stays
  `REJECT_PENDING_PROVENANCE` on its own merits (no producer, no sidecar, no consumer).

## 8. IV / Greeks negative finding — now reproducible

The tool scans **every message descriptor in all 104 modules** and prints its terms:

```
search terms            : implied, impl_vol, iv, vol, volatility, delta, gamma, vega, theta,
                          rho, greek, charm, vanna, moneyness
false-positive filters  : volume, vol_, _vol, total_vol
modules scanned: 104   messages: 104   fields compared: 1080
MATCHES: 0
```

Only now is it fair to cite `raw/11` as an all-module field scan. Conclusion unchanged — **IV and Greeks
must be calculated locally** — but the evidence now matches the claim.

## 9. MBO probe design — corrected

The 1.6.3 public API is `request_market_depth(symbol, exchange, depth_price)` and
`subscribe_to_market_depth(symbol, exchange, depth_price)`. **Both require a `depth_price`.** A response is
scoped to a price level and side; it is **not** a full-book snapshot, and my P-6 description of "confirms
`sequence_number` on the wire" via one call overstated what one call returns.

Corrected distinctions, to be probed separately when the gate clears:

| concept | how it is obtained | what one call proves |
|---|---|---|
| one-price snapshot | `request_market_depth(sym, exch, price)` | the state at **that price/side** only |
| per-price updates | `subscribe_to_market_depth(sym, exch, price)` | incremental `DepthByOrder` at that price |
| aggregated book | `subscribe_to_market_data(..., ORDER_BOOK)` | `OrderBook` batch-state events, **not** per-level NCD |
| complete-book bootstrap | repeated per-price snapshots across a band, or a vendor full-book facility if one exists | **not established** — no full-book request was found in the 104 modules |
| snapshot completion | `OrderBook.update_type ∈ {SNAPSHOT_IMAGE, BEGIN, MIDDLE, END, SOLO}` | batch framing, still not per-level NCD |
| sequence continuity | `DepthByOrder.sequence_number` across events | continuity **within** the observed price level |

Price-level selection, when authorised: take the front-month GC BBO from P-4, then probe a small
symmetric band (for example 3 ticks either side of the touch) so both sides are represented — documented
per level, not a single opportunistic price.

## 10. Path sanitised

`raw/11` now prints `<site-packages>/async_rithmic`. Verified: **0 occurrences** of the local home path in
the regenerated output.

## 12. Recorder spool pruned — owner-authorised, recorded here for completeness

Not part of the corrigendum, but it changed the local corpus and must be on the record.

The owner asked to reduce `.gcae`. I first established the fact that mattered: **`recorder/sessions/` is
gitignored — 0 files on GitHub — so the 13 GB was the only copy**, while `recorder/samples/` (11 files,
1.7 MB) is the part that is genuinely backed up. Presented with that, the owner chose representative
retention over deletion of everything.

Applied via `tools/prune_recorder_sessions.py`, deterministic rule: keep the 4 sessions already decoded in
Stage 2 evidence, plus the smallest session in every (date × `enabledStreams`) group.

| | |
|---|---|
| sessions before / after | 60 → **10** |
| size before / after | 13.63 GB → **1.38 GB** (freed 12.25 GB) |
| records removed | 7,719,458 |
| full manifest of what was removed | `12_RECORDER_PRUNE_MANIFEST.csv` — every deleted session with id, bytes, records, streams, instrument, start/stop and segment hash prefixes |
| untouched | `recorder/samples/` (11 tracked), `capability/` (280), `research/`, `instrument_master_gcae.json`, `rithmic.env` |
| `.gcae` git state after | clean, `HEAD 18c81bb` unchanged |
| sidecars after | **182/182 verify** |

Retained coverage: at least one session for every date (2026-07-22, -23, -27, -28, -30) and every posture
(`Trade`, `Trade,Dom`, `Trade,Dom,Mbo`), plus 3 MBO sessions on 2026-07-27.

**Cost, stated plainly:** the other 24 MBO sessions are gone. Any future question needing more MBO volume
than the retained 4 sessions must be answered from newly captured data, not from this corpus.

## Claims that survive unchanged

Dependency unpinned · Rithmic schema carries `DepthByOrder.sequence_number` and three clock domains
(`source_*`, `ssboe/usecs`, `jop_*`) · reference-data, bar-replay and exchange-permission schemas exist ·
the ATAS recorder path loses fields the wire protocol carries · IV/Greeks are local-only.

**None of these is promoted from "schema exists" to "FIN can obtain it".** That promotion requires the
live probe, which has not run.
