# Stage 2 — corrigendum 1

Continues from `9c093a84`. **Stage 2 not accepted; work package OPEN; Task D not started.**

---

## Item 3 first — the payload/source reconciliation, because it changes the readings below

**Answer: documentation/source mismatch, and worse than a mismatch — the two fields carry no
information at all.**

`MboToRawEventAdapter.cs:55` says *"The interpreted lifecycle action is deliberately not folded into the
payload."* The payload class contradicts it (`Payloads/RawEventPayload.cs:294-334`):

```csharp
InterpretedLifecycleAction = "Unknown";   // line 311 — hard-coded in the constructor
SnapshotCompletionKnown    = false;       // line 319 — hard-coded in the constructor
```

Both are public properties, so both **are** serialised — but as compile-time constants. Not a historical
producer revision, not a decoder error: the comment describes intent, the constructor hard-codes a sentinel,
and the serializer emits it.

Measured across the full decoded population (`raw/09_record_census.txt`): `interpretedLifecycleAction =
"Unknown"` on **24,413/24,413**; `snapshotCompletionKnown = false` on **24,413/24,413**. Exactly as the
constructor dictates.

**Consequence: Stage 2 listed these among "MBO payload fields observed" as if informative. They are not.
Withdrawn.** The lifecycle information that *does* exist lives in `rawTypeName` / `rawTypeNumeric`.

### `lifecycleIntegrityRecordCount` — interpretation corrected

It counts the recorder's **own** lifecycle/integrity record category (`RecorderLifecycle`,
`RecorderIntegrity` — `RawEventPayloadKind` 10 and 11), **not** MBO New/Change/Delete transitions. Zero is
therefore **not** a proxy for missing MBO reconciliation, and Stage 1, the corrigendum and Stage 2 all used
it loosely. Corrected everywhere.

### Producer-version provenance — a real gap

The manifest carries `recorderSchemaVersion` (`1.2.0`) and `containerVersion` (`1`) and **no producer
commit, build id or assembly version**. The strongest available producer evidence for each decoded session
is therefore only the schema version plus `recorderProcessInstanceId`. **Recorded as a provenance gap.** I
have not applied current-source semantics to historical bytes anywhere below except where the decoded
values themselves confirm the current source (which, for the two constants above, they do exactly).

## Item 2 — MBO evidence scope, measured over the full population

`tools/record_census.py`, every decoded record walked — no sampling. Four sessions, 75,904 records,
**24,413 MBO**.

| question | measurement |
|---|---|
| **1. schema / key presence** | `exchangeOrderId`, `priority`, `price`, `volume`, `rawTypeName`, `interpretedLifecycleAction`, `snapshotCompletionKnown` — key present in **24,413/24,413** each |
| **2. non-null value presence** | `exchangeOrderId`, `priority`, `price`, `volume` — non-null in **24,413/24,413** each |
| **3. usable order-level identity** | `exchangeOrderId`: **0 zero, 0 negative**, **8,740 distinct**. `priority`: **24,413 > 0**, 0 zero, 0 negative. `price`/`volume`: 0 null, 0 zero |
| **4. lifecycle completeness** | `rawTypeName`: **New 8,239 · Change 7,954 · Delete 8,220**, `rawTypeIsKnownEnumMember` **true 24,413/24,413**. But `interpretedLifecycleAction` **constant**, `snapshotCompletionKnown` **constant** |

So the Stage 2 claim **is** supported for identity — every record has a positive, non-sentinel order id and a
positive priority, and New/Change/Delete is present in the raw type. It was **not** supported at the time it
was made, because three sampled field shapes cannot establish a population property. The claim now rests on
a full count.

**Ordering and integrity, same population:**

| measure | value |
|---|---|
| `NativeSequenceAvailable` | **false on 75,904/75,904** |
| `IntegrityFlags` on MBO records | `SourceTimeKindUnspecified, ProviderSnapshotCompletionUnknown` — **24,413** |
| `IntegrityFlags` on Trade/Dom | `NativeSequenceAbsent, SourceTimeKindUnspecified` — 48,190 |
| `RecorderGlobalLocalSequence` gaps | **0** |
| `RecorderGlobalLocalSequence` backwards | 3 — **artefact of aggregating four sessions**, one per session boundary |
| `StreamLocalCaptureSequence` gaps | Mbo 3, Dom 3, Trade 3 — same boundary artefact; Integrity 3,300 is expected, that stream is per-invocation |
| `CallbackInvocationSequence` backwards | 34,862 — expected where streams interleave; **not** interpreted as an anomaly without producer semantics |

**Classification retained but qualified: `PRESENT_AND_SUBSTANTIAL / COMPLETENESS_UNPROVEN`.**

Usable order-level identity: **YES, on the decoded population.**
Stable book reconstructability: **NO** — no provider sequence (`NativeSequenceAvailable` false everywhere),
snapshot completion explicitly unknown (`ProviderSnapshotCompletionUnknown` on every MBO record), and the
two interpretive fields are constants.

**No production-readiness claim for MBO reconstruction, queue position, spoofing or iceberg. The absence of
a native sequence alone forecloses it.**

## Item 1 — direct-Rithmic lineage, completed

Read in full: `config.py` (70), `client.py` (211), `collector.py` (133), `live.py` (102), `patch158.py`
(84), `instruments.py` (77), `oi_cache.py` (31), `wire.py` (64), `levels_writer.py` (73),
`run_snapshot.py` (231). **No live connection.**

```
run_snapshot.cmd_live                             run_snapshot.py:103
  → load_config()  %USERPROFILE%\.gcae\rithmic.env  config.py:12,55
  → OptionFlowRithmic(cfg).connect()               client.py:79-100
      plants=[SysInfraType.TICKER_PLANT] only      client.py:100
  → instruments.load_master()                      instruments.py
  → per product: patch158.clear() then snapshot_one
      → live._future_price → front_month_future → subscribe(quotes)   live.py:29-46
      → poll mid_or_last up to max_wait=20s
      → instruments.select_contracts(master, …)    live.py:56
      → subscribe(each option, quotes+open_interest)
      → asyncio.sleep(window_secs)   default 12s   live.py:63
      → open_interest_by_symbol()  ← template-158 frames
      → black76.implied_vol per contract
      → collector.compute_full_levels → LevelsDoc
  → write_levels → artifacts/optionflow/<PRODUCT>/levels.json
  → rc.disconnect()
```

| mechanism | classification | evidence |
|---|---|---|
| reconnect | **ABSENT** | no reconnect path in any of the ten files; `connect()` is called once per run (`run_snapshot.py:126`) |
| resubscription after reconnect | **ABSENT** | follows from the above; no subscription replay exists |
| subscription-state tracking | **ABSENT** | `subscribe()` (`client.py:200`) awaits the dependency and records nothing locally |
| deduplication | **ABSENT** | `_on_tick` appends to `_ticks` and last-write-wins into `_quotes` (`client.py:110-123`) |
| event ordering / sequence validation | **ABSENT** | no sequence field is read; ordering is arrival order |
| out-of-order handling | **ABSENT** | same |
| gap detection | **ABSENT** | same |
| queue / buffer bounds | **ABSENT** | `_ticks`, `_depth_events`, `_book_events` are plain lists, no `maxlen` (`client.py:71-75`) |
| backpressure / drop accounting | **ABSENT** | nothing counts or drops; memory grows with the run |
| clearing `_ticks` / `_quotes` / `_depth_events` / `_book_events` between products | **ABSENT** | none is cleared or partitioned by product; only `patch158.OI_FRAMES` is cleared, per product (`run_snapshot.py:150`, `patch158.py:63`) |
| template-158 state partitioning | **IMPLEMENTED** | `patch158.clear()` called per product before each snapshot |
| option-chain bootstrap | **IMPLEMENTED (weak)** | fixed `asyncio.sleep(window_secs)`, default 12 s (`live.py:63`) |
| bootstrap completion criteria | **ABSENT** | a wall-clock window, not a completeness test |
| snapshot completion | **ABSENT** | no snapshot-complete marker is requested or consumed |
| timestamp domains | **PARTIAL** | one field, `ssboe`, kept as `q["ts"]` (`client.py:117`); no ingress/processing separation |
| failure propagation | **PARTIAL** | `snapshot_one` isolates per-product failures by design; `client.py:35-38`, `104`, `130-132` swallow broadly into `None` |
| dependency behaviour (`async_rithmic`) | **DEPENDENCY_OWNED_UNVERIFIED** | reconnect/ordering may exist inside the library; **no pinned version is recorded in-repo** and none was verified from primary source |

### A defect this trace exposes

`live.py:96` builds the quote with `open_interest=int(oi) if oi else 0`, while the debug record on the next
line keeps `None`. **A missing OI and a genuine zero OI become the same value for the engine**, and only the
debug artifact preserves the difference. That is `G-ACC-003` (null must not be fabricated as zero) violated
at the point of construction.

## Item 5 — reproducibility

**`raw/07_decode_samples.txt` regenerated** with the final committed decoder; it now carries the
`CRC-32C verified : 3 of 3 segments` scope line.

**Exact commands:**

```bash
python tools/segment_decoder.py --samples
python tools/segment_decoder.py --sessions 7cc5b6551f794a6094aee403ec20a408 \
    4b0078a775764849acf44fc47f30fd68 b46a5c0d0f6443eaa231d59f9fb9fa33 \
    27ca68e6537740838ab00eb825c185ef
python tools/record_census.py --sessions <same four ids>
python tools/test_segment_decoder.py
```

**Session-selection rule, stated so it can be judged:** of the 28 sessions whose `enabledStreams` contains
`Mbo`, the four **smallest by sealed-segment bytes** were chosen — 3.5 MB, 15.2 MB, 19.9 MB, 67.1 MB, about
105 MB of 8.86 GB. The rule is cost, not representativeness. All four are `GCQ6@COMEX` on 2026-07-27 and all
four are post-withdrawal (`mboRecordingEnabled=true`).

**What that leaves unmeasured, explicitly:** the other 24 MBO sessions (~8.75 GB); every `Trade`-only and
`Trade,Dom` session; the 2026-07-22/23/28/30 dates; the single `GCZ6@COMEX` session; the 2 sessions with
`abnormalTermination=true`; and all 44 unsealed `.seg.tmp`. **No date or posture other than the one above is
covered by decoded evidence.**

**Deterministic offline self-tests** — `tools/test_segment_decoder.py`, `raw/10_decoder_selftest.txt`,
**16/16 PASS**, no `.gcae` access:

`CRC-32C("123456789") == 0xE3069283` · `CRC-32C(b"") == 0` · well-formed container · bad container magic ·
unsupported container version · unsupported frame version · truncated frame (and earlier frames preserved) ·
CRC mismatch caught · CRC mismatch **not** caught with `verify_crc=False` (proving why scope must be stated) ·
invalid JSON · empty file.

## Item 6 — document state

`00_DISCOVERY_REPORT.md` is retitled and its status block now reads as the **historical Stage 1 record**.
Current status lives here and in `01_TASK_B_LINEAGE.md`.

## Corrections to my own Stage 2 statements

1. **"MBO payload fields observed … `interpretedLifecycleAction`, `snapshotCompletionKnown`"** — withdrawn;
   both are hard-coded constants.
2. **"24,413 records carrying `exchangeOrderId` and `priority`"** — the claim happens to hold, but it was
   made from three sampled shapes. Now measured over the full population.
3. **`lifecycleIntegrityRecordCount = 0` used as an MBO reconciliation proxy** — withdrawn; wrong category.
4. **`M-14 REUSE_AS_IS`, `M-19 DEFERRED`, `M-03` provenance claim** — all corrected in the matrices.
5. **"Bid and ask ARE captured on the wire"** — still true (`client.py:113-116`), but it is a *code path*;
   no artifact evidence is cited for it and it is now labelled as such.
