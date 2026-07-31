# Stage 2 · Task B — end-to-end data lineage

> **SUPERSEDED IN PART by `04_STAGE2_CORRIGENDUM.md`.** Three statements below are
> withdrawn there: `interpretedLifecycleAction` and `snapshotCompletionKnown` are hard-coded
> constants and carry no information; `lifecycleIntegrityRecordCount` counts the recorder's own
> category and is not an MBO transition count; and the 24,413-record claim was made from three
> sampled field shapes and is now measured over the full population. The direct-Rithmic
> "not yet traced" list is also completed there.

Two independent branches. They share no code today, and the report keeps them separate because they have
different plants, different identities and different integrity models.

---

## Branch 1 — ATAS callbacks → recorder segments

```
ATAS platform (Rithmic feed)
  → GcAuctionFlowIndicator callbacks
  → TradeRecorderHost                        Recorder/TradeRecorderHost.cs
  → Trade/Depth/Mbo → RawEventAdapter        Recorder/{Trade,DepthTo,MboTo}RawEventAdapter.cs
  → RawEventEnvelope (JSON)                  Recorder/RawEventEnvelope.cs
  → SegmentWriter → FrameCodec               Recorder/SegmentWriter.cs, FrameCodec.cs
  → <sessionId>/segments/<segmentId>.seg     + .sha256 sidecar
  → ManifestWriter → manifest.json           Recorder/ManifestWriter.cs
  → RecoveryScanner (replay/repair)          Recorder/RecoveryScanner.cs
```

### Container format — transcribed and independently verified

| element | bytes | source |
|---|---|---|
| container header | 8 — magic `GCAR` `0x52414347`, uint16 version, uint16 flags | `FrameCodec.cs:10,19-25` |
| frame header | 16 — magic `GCF1` `0x31464347`, uint16 frameVersion, uint16 frameType, uint16 flags, uint16 reserved, uint32 payloadLength | `FrameCodec.cs:13,94-105` |
| payload | UTF-8 JSON | `SegmentWriter.cs:194,223,299` |
| trailer | uint32 CRC-32C over header+payload | `FrameCodec.cs:106-110` |
| CRC | reflected Castagnoli `0x82F63B78`, init/xorout `0xFFFFFFFF` | `Crc32C.cs:11,26-30` |

`frameType`: 1 `SegmentHeader`, 2 `RawEvent`, 3 `SegmentFooter` (`RecorderEnums.cs:67-72`).
`RawEventPayloadKind` 1–12 and `RecorderStreamKind` 1–5 (`RecorderEnums.cs:3-10,27-41`).

**Verification against the curated samples** (`raw/07_decode_samples.txt`): 3 segments, 0 container errors,
**0 CRC failures**, 0 decode errors, 1,078 `RawEvent` frames — and the decoded category split
(539 market / 539 invocation) reproduces the manifest's `marketEventRecordCount` and
`invocationResultRecordCount` **exactly**. An independent reimplementation landing on the producer's own
numbers is the strongest check available offline.

### Envelope fields (`RawEventEnvelope.cs:116-152`)

Identity and ordering: `RecorderSchemaVersion`, `SessionId`, `RecorderProcessInstanceId`, `SegmentId`,
`SegmentOrdinal`, `RecorderGlobalLocalSequence`, `StreamLocalCaptureSequence`,
`CallbackInvocationSequence`, `CallbackItemOrdinal`, `SubscriptionOrCaptureEpoch`, `ContractEpoch`.
Clock domains — **three, kept separate**: `SourceTimeTicks` + `SourceDateTimeKind` (venue/platform),
`CallbackReceiveUtc` (ingress), `WriterDequeuedUtc` (persistence), plus
`CallbackReceiveStopwatchTimestamp` and `CallbackManagedThreadId`.
Provenance: `DeclaredDataSourceMode` + `ModeProvenance`, `DeclaredProvider` + `ProviderProvenance`,
`Instrument`.
Integrity: `IntegrityFlags`, `NativeSequenceAvailable`.

`RecorderIntegrityFlags` (`RecorderEnums.cs:75-85`) already names the honest gaps:
`NativeSequenceAbsent`, `SourceTimeKindUnspecified`, `ProviderSnapshotCompletionUnknown`,
`MboRecordingBlocked`, `ContractIdentityChanged`, `DiskSafetyStop`, `WorkerFault`.

### Consumers

`RecoveryScanner` and `SegmentReader` are the only readers in-repo. **Nothing in the KDK analysis path
consumes `.seg` at all** — the engine's research corpus is the separate `episode-dataset.jsonl`. That is
the single largest gap this work package exposes: 13 GB of captured market data with no consumer.

---

## Branch 2 — direct Rithmic (Python sidecar), static analysis only

**No live connection was made.**

```
%USERPROFILE%\.gcae\rithmic.env  (or GCAE_RITHMIC_ENV)     rithmic/config.py:12,55
  → OptionFlowRithmic.connect()                            rithmic/client.py:79-100
  → async_rithmic, TICKER_PLANT only                       rithmic/client.py:100
  → search_option_symbols / front_month_future             rithmic/client.py:181-198
  → subscribe(symbol, exchange, quotes, open_interest)     rithmic/client.py:200
  → _on_tick / _on_market_depth / _on_order_book           rithmic/client.py:110,135,147
  → ContractQuote → gex/engine.py → analytics/black76
  → levels_writer → artifacts/optionflow/<PRODUCT>/levels.json
```

Findings from the code, not from the artifacts:

- **One plant only.** `connect(plants=[SysInfraType.TICKER_PLANT])` — the comment records that the other
  plants return permission-denied (1011) and would abort the whole connect. So order-plant and history-plant
  capabilities are **not** available on this entitlement as configured.
- **The OI bit is resolved from the generated proto enum, never guessed** (`client.py:32-40`):
  `RequestMarketDataUpdate.UpdateBits.OPEN_INTEREST`, returning `None` if the member is absent. Subscription
  bits are `LAST_TRADE | BBO | OPEN_INTEREST` (`client.py:55-66`).
- **Tick field mapping is narrow** (`client.py:110-123`): `bid_price`, `ask_price`, `trade_price`, `ssboe`
  timestamp. Bid and ask **are** captured on the wire here — the earlier finding that they were not
  *persisted* is a `levels_writer` limitation, not a collection one.
- **Template-158 frames are handled by a patch** (`rithmic/patch158.py`) and the plant's per-frame warning
  is filtered out (`client.py:42-52`). That is where OI actually arrives.
- Credentials are read from outside the repository and are never written into artifacts. **I read key
  names only; no value was opened.**

**Not yet traced:** reconnect/resubscribe, deduplication, ordering guarantees, backpressure, and the
snapshot/bootstrap procedure for the option chain. Those need a deeper read of `collector.py` and
`live.py` than this pass performed, and they are listed as remaining work rather than guessed at.

---

## The `enabledStreams` / `disabledStreams` question — RESOLVED

Stage 1 recorded an "unresolved semantic inconsistency": `Mbo` appears in `enabledStreams` on 28 sessions
**and** in `disabledStreams` on all 54. The producer code settles it.

`enabledStreams` is built from runtime state (`TradeRecorderHost.cs:158-162`):

```csharp
var streams = new List<string>(2);
if (pending.EnableTradeRecording) streams.Add("Trade");
if (pending.EnableDepthRecording) streams.Add("Dom");
if (pending.EnableMboRecording)   streams.Add("Mbo");
```

`disabledStreams` is **hard-coded and unconditional** (`RawEventRecorderSession.cs:728`):

```csharp
new[] { new DisabledStreamRecord("Mbo", MboOperationalLock.MboOperationalBlockReason) },
```

It emits exactly one entry, always `Mbo`, on every session regardless of configuration. It is not a
per-session state — it is a **standing advisory carrying a constant string**.

**Verdict: `enabledStreams` is authoritative. `disabledStreams` is a constant and must not be read as
runtime state.** There is no data inconsistency; there is a misleading field name.

The 28/25/1 split is explained by the same file. `MboOperationalLock.MboOperationalBlockReason` now reads
**"WITHDRAWN 2026-07-27: same-process chart-data side effect was this project's own fsync on the ATAS
thread, not MBO subscription."** and `MboIsolationRequirement` moved `IsolatedEnvironmentOnly` →
`OperatorDecision`. Measured correlation is exact:

| sessions | isolation | recording | reason text |
|---|---|---|---|
| 28 | `OperatorDecision` | `true` | WITHDRAWN 2026-07-27… |
| 25 | `IsolatedEnvironmentOnly` | `false` | Same-process GC chart-data side effect… |
| 1 | `OperatorDecision` | `false` | WITHDRAWN 2026-07-27… |

The split is simply before/after the 2026-07-27 withdrawal, plus one session where the operator left MBO
off afterwards.

## MBO — reclassified again, this time on decoded payloads

**`PRESENT_AND_SUBSTANTIAL / COMPLETENESS_UNPROVEN`.**

Stage 1 said `UNAVAILABLE` (wrong). The corrigendum said `UNVERIFIED / NOT_ADMISSIBLE_FOR_USE` (right at
the time). Decoding now shows MBO data **exists in quantity**. From four MBO-declaring sessions,
CRC-verified, 0 decode errors (`raw/08_decode_mbo_sessions.txt`):

| payload kind | records |
|---|---|
| `Depth` | 35,025 |
| **`Mbo`** | **24,413** |
| `BestBidAsk` | 9,862 |
| `CallbackInvocationResult` | 3,301 |
| `CumulativeTradeUpdate` | 1,986 |
| `NewTrade` | 854 |
| `CumulativeTradeNew` | 463 |

MBO payload fields observed: `exchangeOrderId`, `priority`, `price`, `volume`, `derivedSide`,
`rawSideName`, `rawSideNumeric`, `rawTypeName`, `rawTypeNumeric`, `rawTypeIsKnownEnumMember`,
`interpretedLifecycleAction`, `snapshotCompletionKnown`.

**Order id and queue priority are both present.** That is the raw material for order-level work.

**What is still not established, and why the classification stops short of admissible:**

1. **`lifecycleIntegrityRecordCount` is 0 on every segment** — the category that would carry MBO lifecycle
   integrity accounting is empty, so New/Change/Delete transitions are recorded per-event but not
   reconciled.
2. **Snapshot completeness** — `snapshotCompletionKnown` exists per record; its distribution across the
   corpus has not been tabulated, and `ProviderSnapshotCompletionUnknown` is a defined integrity flag.
3. **Native sequence** — `NativeSequenceAbsent` is a defined flag; whether the feed supplies a native
   sequence is not yet measured.
4. **Coverage** — 4 of 28 MBO sessions decoded (~105 MB of ~8.9 GB).

**No claim of production-readiness for MBO reconstruction, queue position, spoofing or iceberg detection
is made, and none is supported by this evidence.**

## DOM — a real semantic gap

All 35,025 decoded `Depth` records carry `UpdateAction = "Unknown"`. The field exists and is populated with
a literal `Unknown`, so DOM **New/Change/Delete semantics are not recoverable from the capture as it
stands**. Separately, the two curated sample sessions declare `enabledStreams: [Trade, Dom]` yet contain
**zero** `Depth` records — declared-but-absent, exactly the case Stage 1 flagged as needing verification.

## Coverage and limits of this pass

| | |
|---|---|
| segments decoded | 7 of 290 sealed (3 curated + 4 MBO sessions) |
| bytes decoded | ~105 MB of ~13 GB |
| CRC verified | **100% of what was decoded** |
| decode errors | 0 |
| corpus-wide decode | **not performed** — pure-Python CRC-32C runs at a few MB/s |

Coverage was chosen to answer the MBO and DOM questions, not to characterise the whole corpus. Every
number above is scoped to what was decoded and says so.
