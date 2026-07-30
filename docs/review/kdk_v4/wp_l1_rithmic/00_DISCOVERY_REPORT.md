# WP-L1-RITHMIC-DATA-SURFACE-DISCOVERY — discovery report (stage 1)

**Status: `DISCOVERY_IN_PROGRESS`. Not closed, not claimed closed.** This covers the mandatory sequence
steps 1–3 (baseline · inventory · discovery report) for the parts reached so far. Tasks B and D are
**not** done, and §7 says exactly what is missing.

## Baselines

| repo | branch | commit |
|---|---|---|
| `GC.AuctionFlow` | `wp-l1-02-closure` | `efd12135afad24c8fe5ce1410c5e8bd2a2b82221` |
| `GC.AuctionFlow` | `master` / `origin/master` | `8af7f49f6c765c8cf5b1bfd2a600a6f661474a04` |
| `DATA_rithmic` | `main` | `18c81bbec8fadab27f7d9b31a20373c577d2d680` |

---

## 1. The premise needs correcting first

The directive says the extraction logic lives in `.gcae` and *"chưa được đưa đầy đủ vào `GC.AuctionFlow`"*.
**Measured: `.gcae` contains no code at all.**

Searched the whole tree for `.py .cs .ps1 .sh .bat .cmd .ipynb .sql .yaml .yml .toml .ini .cfg .proto .js .ts`
— **0 hits**. Every one of the 1,026 non-`.git` files is a data artifact:

```
489 .sha256   293 .seg   197 .json   44 .tmp   1 .md   1 .jsonl   1 .gitignore   1 .env
```

**`C:\Users\LOQ\.gcae` is the working tree of the `DATA_rithmic` repo** — `.gcae/.git` exists, remote
`KIMDAUKINH.git` (GitHub has since renamed it `DATA_rithmic`), `HEAD 18c81bb` on `main`, working tree clean.
So `.gcae` is not a legacy codebase to port; it is the **output** of producers that already live in
`GC.AuctionFlow` (`src/GC.AuctionFlow/Recorder`, `src/GC.AuctionFlow/Probe`, `research/optionflow`), plus a
runtime data root (`SpoolRoot` → `%USERPROFILE%\.gcae`).

That reshapes Task D: there is **no legacy code migration to perform from `.gcae`**. What is real is the
gap between what the producers *record* and what `GC.AuctionFlow` can *validate and consume* — §5, §6.

I state this as a correction, not a refusal: if code once existed there it is gone from the tree, and
nothing in git history under `.gcae/.git` contains source either (294 tracked files, all artifacts).

## 2. What `DATA_rithmic` actually publishes

294 tracked files. `.gitignore` excludes `rithmic.env`, `*.env`, `.env*` and `recorder/sessions/`.

| path | tracked | on disk | note |
|---|---|---|---|
| `capability/` | 280 | 57 MB | probe artifacts + `.sha256` sidecars |
| `recorder/samples/` | 11 | ~1.7 MB | 2 curated sessions |
| `recorder/sessions/` | **0** | **13 GB** | gitignored, local only — 62 session dirs |
| `research/episode-dataset.jsonl` | 1 | 2.9 MB | **contaminated, §4** |
| `instrument_master_gcae.json` | 1 | 9.9 MB | **57,242 option definitions, §3** |
| `rithmic.env` | 0 | 1 KB | gitignored, correct |

**Total local `.gcae`: 13 GB.** The public repo carries ~70 MB of it.

## 3. Capability inventory — evidence-backed

Full field lists: `raw/04_recorder_survey.txt`. Extract:

### Recorder (ATAS → Rithmic feed), 56 manifests, schema `1.2.0`

| measure | value |
|---|---|
| segments | 293 |
| records | **8,595,119** |
| — market events | 6,266,413 |
| — invocation results | 2,328,706 |
| — lifecycle-integrity records | **0** |
| writer-sequence discontinuities between consecutive segments | **0** |
| instruments | `GCQ6@COMEX` ×55, `GCZ6@COMEX` ×1 |
| session dates (UTC) | 2026-07-22, -23, -27, -28, -30 |
| mode / provider | `Live` / `Rithmic`, both `OperatorDeclared` |

Manifest carries per session: `sessionId`, `recorderProcessInstanceId`, `startUtc`/`stopUtc`,
`contractEpochs`, `lastInstrument` (securityCode, securityId, exchange, contractExpiration, tickSize,
identityKey), `enabledStreams`, `disabledStreams`, MBO posture, `abnormalTermination(+Reason)`,
`knownLimitations`, `capabilityClaimsForcedFalse`, `continuityDisclaimer`, and per segment: `segmentId`,
`segmentOrdinal`, `fileName`, `sha256Hex`, `recordCount`, `marketEventRecordCount`,
`invocationResultRecordCount`, `lifecycleIntegrityRecordCount`, `byteLength`, `firstWriterSequence`,
`lastWriterSequence`, `contractEpoch`, `categoryCountsKnown`.

**This is a strong provenance model already** — mode, provider, both provenances, instrument identity,
contract epoch, per-segment hash and sequence range. Task D's provenance requirements are largely *already
met by the recorder*; what is missing is a **validator** in `GC.AuctionFlow` that reads and checks it.

### Capability probes, 280 artifacts

`TradeStreamProbe` (49 with parsed fields), `DomSemanticsProbe`, `MboLifecycleProbe`. Each carries
`schemaVersion`, `probeVersion`, `sessionId`, `createdUtc`, `continuityDisclaimer`,
`declaredDataSourceMode`, `dataSourceModeProvenance`, `expectedInstrumentCode`, `observedInstrument`,
`gateReason`, `captureAuthorized`, `liveTradeCapabilityClaim`, `callbackObservations`, `counters`,
`overlap`, samples, `knownLimitations`, `integrityEvents`, `callbackThreading`, `clockSemantics`,
`authoritativeStream`.

### Instrument master

`instrument_master_gcae.json`, 10,350,790 bytes: `{source, built_from, options}` where `options` holds
**57,242 entries**. This is an option-universe definition cache and is **not referenced anywhere in
`GC.AuctionFlow`** — see §6.

## 4. Data quality — `research/episode-dataset.jsonl`

Audit: `raw/02_episode_dataset_audit.txt`.

| measure | value |
|---|---|
| lines | 5,143 |
| **malformed** | **1** (line 2,673) |
| distinct `EpisodeId` | 2,113 |
| duplicate ids | 2,098 ids, **3,029 surplus rows** |
| rows under `EPISODE_POLICY_V1`, auction `PI-2026-07-26` | **5,127** |
| rows under `AUCTION_EPISODE_POLICY_V1` (production identity) | **15** |

The 15 production rows carry a full composite identity
(`EP|GCZ6|GCZ6_tick=0.1|PI-2026-07-29|REF_…|SEQ=8_FP=…|AUCTION_EPISODE_POLICY_V1`) and span GCQ6→GCZ6 on
2026-07-27…-30. The other 5,127 carry fixture-shaped ids (`EP-1`, `EP-2`, …, 23 copies of `EP-1`).

**This corpus is ~99.7% test fixture and is published publicly in `DATA_rithmic`.** It must not be used
for calibration or backtest in its current form.

**A correction to my own first reading.** The file's mtime is today 03:32, and I initially inferred that
the test suite had appended fixtures during my WP-L1-02 runs. That is **wrong**. `git status` is clean and
the content matches `HEAD` once line endings are normalised — `.gcae` has `core.autocrlf=true`, so the
working copy is CRLF (2,927,958 bytes) and the blob is LF (2,922,815 bytes). Raw hashes differ; content
does not (`raw/05_crlf_check.txt`). The fixtures were committed, not written today. I also checked every
test construction site: no current test writes to the default `%USERPROFILE%\.gcae` store with folding
enabled — the two default-store sites (`Phase5ABBarReplayTests.cs:307,329`) pass `episodes: null`, and the
two in `Phase5AHistoricalScannerTests` are `enabled: false`.

## 5. Integrity — the sidecar mechanism is sound

`raw/03_sidecar_audit.txt`: **489/489 sidecars verify.** Zero mismatches, zero missing targets, zero
unparseable. `autocrlf` did **not** corrupt the binary `.seg` files.

This contradicts the standing assumption of a "broken hash sidecar mechanism" that Task D asks to fix.
**On the evidence available, it is not broken.** Five files have no sidecar, and only two of them matter:

- `research/episode-dataset.jsonl` — **no integrity sidecar** on the one dataset KDK actually consumes;
- `instrument_master_gcae.json` — no sidecar, no provenance beyond two string fields.

(`.gitignore`, `README.md`, `rithmic.env` legitimately have none.)

## 6. The contradiction that matters most — MBO

| posture | sessions |
|---|---|
| `enabledStreams` contains `Mbo` | **28** |
| `disabledStreams` contains `Mbo` | **56 — all of them** |
| `mboSchemaSupported=true, mboRecordingEnabled=true, isolation=OperatorDecision` | 28 |
| `mboSchemaSupported=true, mboRecordingEnabled=false, isolation=IsolatedEnvironmentOnly` | 27 |
| `…recording=false, isolation=OperatorDecision` | 1 |
| **`lifecycleIntegrityRecordCount` across all 293 segments** | **0** |

28 sessions declare MBO both **enabled and disabled**, and every session recorded **zero**
lifecycle-integrity records. The documented block reason is
*"Same-process GC chart-data side effect observed during fresh MBO subscription."*

**Consequence, stated conservatively:** there is no evidence of usable MBO/order-level data in this corpus.
MBO reconstruction, queue position, spoofing and iceberg detection are **`UNAVAILABLE`** on this evidence —
not "partial". Whether the 28 sessions contain MBO market events inside `.seg` payloads is **not yet
verified**; that requires decoding segment records, which is §7 work.

## 7. What is NOT done

Named so the gap is not mistaken for a finding of absence:

1. **`.seg` payload decoding.** Every count above comes from manifests, not from the 13 GB of segment
   bytes. Trade/DOM/MBO event counts *inside* segments, snapshot-vs-incremental, New/Change/Delete
   semantics, order id/priority/side, native sequence and snapshot-complete markers are **unverified**.
2. **The direct Rithmic (Python) branch.** `research/optionflow/rithmic` was not analysed in this pass.
   Option chain, strike/expiry/call-put, Open Interest, IV/Greeks inputs, GEX/DEX and settlement remain at
   the status established in the earlier legacy-recovery report, not re-verified here.
3. **Task B end-to-end lineage** — not written.
4. **Task D porting** — not started, correctly: the directive requires lineage first.
5. **`instrument_master_gcae.json`** — 57,242 option definitions, producer unidentified, consumer none.
6. **44 `.tmp` files** in the recorder tree — not yet explained (interrupted writes?).

## 8. Observations logged, not acted on

- `OBS-R1` — the published `episode-dataset.jsonl` is ~99.7% fixture with 1 malformed line, and has no
  integrity sidecar.
- `OBS-R2` — MBO declared enabled and disabled in the same 56 manifests; zero lifecycle records.
- `OBS-R3` — `instrument_master_gcae.json` (9.9 MB, 57,242 options) has no provenance and no consumer.
- `OBS-R4` — `.gcae` uses `core.autocrlf=true` while holding binary `.seg` files. It has not caused damage
  (489/489 verify), but a text-classified binary would be silently rewritten on checkout; a `.gitattributes`
  pinning `-text` would remove the risk.
- `OBS-R5` — `DATA_rithmic` is **public** and carries the contaminated corpus and 57k option definitions.

**No live Rithmic connection was made. No credential value was read, printed or copied. No order was
placed. OAC, the installed DLLs and ATAS configuration were not touched.**
