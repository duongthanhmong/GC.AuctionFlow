# WP-L1-RITHMIC-DATA-SURFACE-DISCOVERY — Stage 1 (corrigendum 1) · HISTORICAL

> **STAGE 2 UPDATE.** Stage 1 was independently accepted at `5074f13`. Stage 2 decoded the recorder
> container and traced the producer, and **two conclusions in this document are now superseded** — see
> `01_TASK_B_LINEAGE.md`:
>
> * **MBO is `PRESENT_AND_SUBSTANTIAL / COMPLETENESS_UNPROVEN`**, not `UNVERIFIED`. 24,413 `Mbo` records
>   with `exchangeOrderId` and `priority` were decoded, CRC-verified, 0 errors.
> * **The `enabledStreams` / `disabledStreams` conflict is RESOLVED, and it was never a data
>   inconsistency.** `disabledStreams` is hard-coded to emit `Mbo` unconditionally on every session
>   (`RawEventRecorderSession.cs:728`); `enabledStreams` is built from runtime flags. `enabledStreams` is
>   authoritative.
>
> Everything else below stands as accepted.

**HISTORICAL STAGE 1 RECORD.** The status line below was true when this document was written and was
accepted at `5074f13`. It is **not** the current status. Current status: `04_STAGE2_CORRIGENDUM.md`.

> *Status as of Stage 1:* `STAGE_1_DISCOVERY / CORRIGENDUM_1`. Not accepted, not closed. Stage 2 not
> started. Task B, Task D, `.seg` payload decoding and the direct-Rithmic lineage not begun, by
> instruction.

| | |
|---|---|
| branch | `wp-l1-rithmic-data-surface-discovery`, based on `282b8a2a6944dc84954fb42c67fecb45912462f7` |
| accepted WP-L1-02 boundary | **`efd12135afad24c8fe5ce1410c5e8bd2a2b82221`** |
| `GC.AuctionFlow` master | `8af7f49f6c765c8cf5b1bfd2a600a6f661474a04` |
| `DATA_rithmic` main | `18c81bbec8fadab27f7d9b31a20373c577d2d680` |

**This commit adds Python discovery tools and documentation only. No production source and no tests
changed.**

**Three Python discovery tools were added** — `inventory.py`, `manifest_census.py`,
`episode_dataset_audit.py`. One was retired: `recorder_survey.py`, which was the source of the
double-count. Two are retained unchanged: `sidecar_audit.py`, `crlf.py`. Five tools ship in total, and
every figure below is reproducible by running them.

---

## 0. What Stage 1 got wrong

| Stage 1 claim | corrected |
|---|---|
| 1,026 non-`.git` files | **1,027** — the histogram summed to 1,027 and the prose did not |
| "15 extensions searched" | **16** |
| "62 session dirs" | **60** under `recorder/sessions` + **2** under `recorder/samples` |
| 56 manifests / 293 segments / 8,595,119 records | **physical 56, unique 54** — the two curated samples duplicate two session IDs and were counted twice |
| MBO `UNAVAILABLE` | **`UNVERIFIED / NOT_ADMISSIBLE_FOR_USE`** — `lifecycleIntegrityRecordCount` is not an MBO event count |
| "no source anywhere in DATA_rithmic history" | now **evidenced**, was asserted |
| inventory and episode-audit figures | tools were run inline and not committed; now committed |

## 1. Recursive inventory — recomputed

`raw/01_gcae_inventory.txt`, from `tools/inventory.py`.

```
files including .git : 1079
files excluding .git : 1027      <- the figure used throughout
```

Histogram (basename suffix, `.git` pruned) sums to exactly 1,027:

```
489 .sha256   293 .seg   197 .json   44 .tmp   1 (no extension)   1 .env   1 .md   1 .jsonl
```

The `(no extension)` entry is `.gitignore`; Stage 1's shell histogram binned it as an extension named
`gitignore`, which is why the two lists differ by one label but not by one file.

**Directories:** `recorder/sessions` **60** · `recorder/samples` **2**.

### Source/config search — 16 extensions, exact predicate

```
.py .cs .ps1 .sh .bat .cmd .ipynb .sql .yaml .yml .toml .ini .cfg .proto .js .ts
matches: 0
```

```bash
find <root> -type f -not -path '*/.git/*' \
  \( -name '*.py' -o -name '*.cs' -o -name '*.ps1' -o -name '*.sh' -o -name '*.bat' \
     -o -name '*.cmd' -o -name '*.ipynb' -o -name '*.sql' -o -name '*.yaml' -o -name '*.yml' \
     -o -name '*.toml' -o -name '*.ini' -o -name '*.cfg' -o -name '*.proto' -o -name '*.js' \
     -o -name '*.ts' \)
```

### The git-history claim, now with evidence

Stage 1 asserted that no source exists anywhere in `DATA_rithmic` history. Measured:

```
commits examined       : 2
source paths in history: 0   (none)
command: git rev-list --all | xargs -n1 git ls-tree -r --name-only
```

**Scope of that claim, stated honestly:** it covers the 16 extensions above across both commits reachable
from `--all` in the local clone. It does not prove nothing executable ever existed under some other name,
and the history is only two commits deep, both created 2026-07-31 — so it says little about anything older
than this repository.

`.gcae` remains the working tree of `DATA_rithmic` (`.gcae/.git`, remote `KIMDAUKINH.git`, since renamed)
and the runtime data root reached by `SpoolRoot`.

## 2. Recorder census — double-count eliminated

`raw/06_manifest_census.txt`, from `tools/manifest_census.py`. Every physical manifest path is enumerated
there.

```
physical manifests : 56
unparseable        : 0
unique sessionId   : 54
ids appearing twice: 2
```

**The two same-ID pairs were not assumed duplicate — they were hashed.**

| sessionId | manifest sha256 | segment hashes | verdict |
|---|---|---|---|
| `737a0896-…-070b684cf383` | `8ccf56f6a5b128ad` on both copies | 1 segment, `c521fc1ba53810d3` on both | **IDENTICAL** |
| `94ad02d3-…-c2dccaa3b929` | `6d824326d86c2fb9` on both copies | 2 segments, `9ccd22b47d014600` + `c53b87f8c8b62a17` on both | **IDENTICAL** |

### Corrected totals

| | unique sessions (canonical) | curated sample copies |
|---|---|---|
| manifests | **54** | 2 |
| segments | **290** | 3 |
| records | **8,594,041** | 1,078 |
| — market events | **6,265,874** | 539 |
| — invocation results | **2,328,167** | 539 |
| — lifecycle-integrity records | **0** | 0 |

Stage 1's 8,595,119 minus the curated 1,078 gives 8,594,041 exactly. Curated totals are reported here and
**never added** to the canonical figures.

### Posture, unique sessions only

`enabledStreams`: `Trade,Dom,Mbo` ×28 · `Trade` ×15 · `Trade,Dom` ×11.
`disabledStreams`: `Mbo` on **all 54**.
MBO posture: `recording=true, isolation=OperatorDecision` ×28 · `recording=false,
isolation=IsolatedEnvironmentOnly` ×25 · `recording=false, isolation=OperatorDecision` ×1.
Instrument: `GCQ6@COMEX` ×53, `GCZ6@COMEX` ×1. Dates: 2026-07-22 ×5, -23 ×6, -27 ×35, -28 ×7, -30 ×1.
`abnormalTermination=true` on **2** sessions.

### Six session directories have no manifest

`5eeafc13…`, `71cc9a3c…`, `753b7dc3…`, `97673894…`, `c237940a…`, `d3d89f53…`. Each holds exactly one file
under `segments/` and an empty `recovery/`; in each case that single file is the session's `.seg.tmp`. No
loose files. These sessions were never sealed.

### The 44 `.tmp` files — classified, not decoded, not modified

All 44 are `X.seg.tmp`, **exactly one per session directory**, across 44 distinct sessions.

| property | value |
|---|---|
| zero-byte | **0** |
| a corresponding sealed `X.seg` exists | **0 of 44** |
| claimed by any manifest `segments[]` entry | **0 of 44** |
| size range | 122,880 → 64,598,016 bytes |

The shape is consistent with an in-flight segment that was never sealed and renamed. **Recoverability is
not asserted**: deciding whether these hold usable records requires decoding `.seg` payloads, which Stage 1
does not do.

## 3. Integrity — sidecars

`raw/03_sidecar_audit.txt`: **489/489 verify.** Zero mismatches, zero missing targets, zero unparseable.
`autocrlf` did not corrupt the binaries.

Five files have no sidecar; two matter: `research/episode-dataset.jsonl` and `instrument_master_gcae.json`.
(`.gitignore`, `recorder/samples/README.md`, `rithmic.env` legitimately have none.)

On this evidence the sidecar mechanism is **not** broken. That is a measurement, not a ruling on the
producer's design, which Task B has not yet traced.

## 4. `research/episode-dataset.jsonl`

`raw/02_episode_dataset_audit.txt`, from `tools/episode_dataset_audit.py`.

| measure | value |
|---|---|
| non-blank lines | 5,143 |
| **malformed** | **1** — line **2,673** |
| distinct `EpisodeId` | 2,113 |
| duplicate ids | 2,098 (surplus 3,029) |
| rows with fixture id `EP-<n>` | **5,127** — all `EPISODE_POLICY_V1` / `PI-2026-07-26` |
| rows with composite production identity | **15** — all `AUCTION_EPISODE_POLICY_V1`, `PI-2026-07-27`…`-30` |

Classification is by id **shape**, not by policy string. It must not be used for calibration or backtest as
it stands, and it is published publicly in `DATA_rithmic`.

**Correction carried forward from Stage 1.** The mtime is 2026-07-31 03:32 and I first inferred the test
suite had appended fixtures during the WP-L1-02 runs. Wrong: `.gcae` sets `core.autocrlf=true`, so the
checkout is CRLF (2,927,958 B, sha `79b97b03…`) and the blob is LF (2,922,815 B, sha `ee69cf0e…`). The
audit tool now prints both hashes so the comparison cannot be botched again. No current test writes to the
default store with folding enabled.

## 5. MBO — reclassified in Stage 1, SUPERSEDED by Stage 2 (see `01_TASK_B_LINEAGE.md`)

**`UNVERIFIED / NOT_ADMISSIBLE_FOR_USE`.**

Stage 1 wrote `UNAVAILABLE`. That was wrong, and the reason matters: **`lifecycleIntegrityRecordCount` is
not an MBO event count.** It counts one record category the recorder emits; MBO market events, if any, would
sit inside `marketEventRecordCount` and be distinguishable only by decoding `.seg` payloads. Zero lifecycle
records is therefore not evidence of zero MBO data.

`UNAVAILABLE` is reserved for a later finding supported by decoded payloads and traced producer semantics.

The posture contradiction — `Mbo` in `enabledStreams` on 28 sessions and in `disabledStreams` on all 54,
with `mboRecordingEnabled=true` on those same 28 — is recorded as an **unresolved semantic inconsistency**.
Which field the producer treats as authoritative is a Task B question and is not answered here.

Nothing in this report supports MBO reconstruction, queue position, spoofing or iceberg detection, and no
such claim is made.

## 6. Other capabilities noted

- **Instrument master** — `instrument_master_gcae.json`, 10,350,790 B, `{source, built_from, options}`
  with **57,242** option entries. No sidecar, no provenance beyond two strings, and no consumer found in
  `GC.AuctionFlow`.
- **Capability probes** — 280 artifacts: `TradeStreamProbe`, `DomSemanticsProbe`, `MboLifecycleProbe`, each
  carrying schema/probe version, session, declared mode + provenance, expected vs observed instrument, gate
  reason, counters, overlap statistics, samples, known limitations, integrity events, callback threading and
  clock semantics.
- **Recorder provenance model** — mode, provider, both provenances, instrument identity, contract epoch,
  per-segment sha256 and writer-sequence range are already recorded per session. What `GC.AuctionFlow` lacks
  is a **validator** that reads and checks them.

## 7. Not done

1. `.seg` payload decoding — **every count in this report is manifest-level**.
2. Direct Rithmic / Python branch (`research/optionflow/rithmic`).
3. Task B end-to-end lineage.
4. Task D porting.
5. Migration matrix — depends on 2 and 3.
6. Producer of `instrument_master_gcae.json`.

## 8. Observations

- `OBS-R1` — published `episode-dataset.jsonl` is 5,127 fixture rows vs 15 production rows, 1 malformed, no
  sidecar.
- `OBS-R2` — MBO stream posture self-inconsistent across all 54 sessions; unresolved pending Task B.
- `OBS-R3` — `instrument_master_gcae.json`: 57,242 option definitions, no provenance, no consumer.
- `OBS-R4` — `.gcae` uses `core.autocrlf=true` while holding binary `.seg`. No damage measured, but a
  `.gitattributes` pinning `-text` would remove the class of risk.
- `OBS-R5` — `DATA_rithmic` is public and carries the contaminated corpus and the option universe.
- `OBS-R6` — 6 sessions unsealed with no manifest; 44 unsealed `.seg.tmp` totalling ~0.9 GB.
- `OBS-R7` — 2 sessions report `abnormalTermination=true`.

**No live Rithmic connection. No credential value read, printed or copied. No order placed. ATAS, OAC,
installed DLLs and both data corpora unmodified.**
