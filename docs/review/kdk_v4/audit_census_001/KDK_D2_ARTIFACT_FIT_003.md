# KDK-D2-ARTIFACT-FIT-003 — corrected from source semantics

**Supersedes `_002` classification** (retained as historical). `AUDITED_SOURCE_HEAD 43d458f`,
`R3_DELIVERABLE_COMMIT e751f6e`. No `.cs` changed. Stable IDs only.

## Why `_002` was wrong

`_002`'s table and summary disagreed on counts, and two fields were classified from field NAMES, not
SOURCE SEMANTICS. Both are corrected here from the actual source.

### `sequenceStats` — reclassified `ABSENT_REQUIRES_NEW_PRODUCER_STATE`

`SegmentFooterRecord.FirstWriterSequence`/`LastWriterSequence` are assigned from
`RawEventEnvelope.RecorderGlobalLocalSequence` (`SegmentWriter.cs:268-269`). That value is defined at
the source:

> `RawEventEnvelope.cs:8` — *"RecorderGlobalLocalSequence is writer **dequeue order only** — not
> callback total order, ..."*

Writer dequeue order **cannot** produce the Stage 2 native-sequence statistics
(`distinct`, `arrivalInversions`, `sortedDiscontinuities`, `span`, `missingWithinSpan`), which require
an exchange/native sequence. Distinguish:

| leg | native sequence | sequenceStats derivable? |
|---|---|---|
| **direct-Rithmic** | present (measured 51/51) | **yes — from native sequence, NOT the current footer** |
| **ATAS callback** | `NativeSequenceAbsent` (`DepthToRawEventAdapter.cs:134`) | **no — unavailable on this path** |
| current binary footer | writer dequeue order only | **no** |

So `sequenceStats` is **not derivable from the current footer** on any path. It is
`ABSENT_REQUIRES_NEW_PRODUCER_STATE` (a native-sequence capture, available only on the direct leg).

### `versions` — reclassified `ABSENT_REQUIRES_NEW_PRODUCER_STATE`

Stage 2 `versionBlock` requires **three independent SemVer** fields: `algorithmVersion`,
`configVersion`, `dataSchemaVersion`. The recorder exposes `RecorderSchemaVersion` (string) +
`ContainerVersion` (int). These are **one recorder-format pair**, not the algorithm/config/data-schema
triplet — `algorithmVersion` and `configVersion` are **not tracked by the recorder at all**. Not
derivable; requires new producer state.

## Corrected 12-field classification (totals exactly 12)

**Boundary note (required):** the recorder produces **no `recorder_segment` JSON artifact**. Field
data below lives in the **binary `.gcae` header/footer**, a *different* artifact. No field is
`EXACTLY_EMITTED` in the JSON contract because that JSON contract is not emitted; where a value is
carried identically in the binary artifact and would serialize 1:1, it is `SEMANTICALLY_EQUIVALENT`
across the binary→JSON boundary.

| # | schema field | classification | basis |
|---|---|---|---|
| 1 | `segmentId` | **SEMANTICALLY_EQUIVALENT** | binary header `.SegmentId` (Guid → JSON string), cross-boundary |
| 2 | `instrumentId` | **SEMANTICALLY_EQUIVALENT** | binary header `.Instrument` identity |
| 3 | `startUtc` | **SEMANTICALLY_EQUIVALENT** | binary header `.StartedUtc` |
| 4 | `endUtc` | **SEMANTICALLY_EQUIVALENT** | binary footer `.EndedUtc` |
| 5 | `dataSourceId` | **PARTIAL_DIFFERENT_SEMANTICS** | `.DeclaredDataSourceMode` + `.DeclaredProvider` are two provenance strings; no canonical `dataSourceId` mapping defined |
| 6 | `versions` | **ABSENT_REQUIRES_NEW_PRODUCER_STATE** | recorder has schema-version+int container-version, not the algorithm/config/data-schema SemVer triplet |
| 7 | `sequenceStats` | **ABSENT_REQUIRES_NEW_PRODUCER_STATE** | footer sequence is writer dequeue order, not native sequence (`RawEventEnvelope.cs:8`) |
| 8 | `sourceMix` | **ABSENT_REQUIRES_NEW_PRODUCER_STATE** | derivable only if single-source-per-segment is enforced AND a canonical `dataSourceId` mapping exists — neither does |
| 9 | `capabilitySnapshotId` | **ABSENT_REQUIRES_NEW_PRODUCER_STATE** | no capability-snapshot linkage (DQ-003) |
| 10 | `dataQuality` | **ABSENT_REQUIRES_NEW_PRODUCER_STATE** | no fail-closed quality verdict (ADR-003) |
| 11 | `subscriptionScope` | **ABSENT_REQUIRES_NEW_PRODUCER_STATE** | not modelled |
| 12 | `containsUnrecoveredGap` | **ABSENT_REQUIRES_NEW_PRODUCER_STATE** | no reconnect-correlated gap detector (ADR-003) |

**Arithmetically consistent summary (totals 12):**

| class | count |
|---|---|
| `EXACTLY_EMITTED` | **0** (no JSON artifact exists) |
| `SEMANTICALLY_EQUIVALENT` | **4** |
| `DERIVABLE_AT_FINALIZATION` | **0** (the two prior "derivable" claims were wrong — see above) |
| `PARTIAL_DIFFERENT_SEMANTICS` | **1** |
| `ABSENT_REQUIRES_NEW_PRODUCER_STATE` | **7** |
| **total** | **12** |

## Binary integrity — kept separate, relabeled accurately

`dotnet test --filter ~Recorder` → **131 passed / 0 failed / 0 skipped, exit 0**
(`raw/R3_RECORDER_TESTS.txt`). Correct label:
**`TEST_EVIDENCED_WITH_DETERMINISTIC_SYNTHETIC_FIXTURES / NOT_LIVE_EVIDENCED`**. Fixture kinds:

- **handcrafted byte fixtures** — `SegmentReaderTests.BuildSegment(...)` constructs container bytes via
  `ContainerFormat.WriteContainerHeader` (`SegmentReaderTests.cs:23-27`);
- **artifacts through the real `SegmentWriter`** — `RecorderCoreTests`, `TradeRecorderC3BcTests`
  (production-code-path evidence, not live market);
- **RecoveryScanner truncation/corruption fixtures** — `P007C3BcCloseoutCorrectionTests`,
  `RecorderCloseoutAuditTests`;
- **optional local-spool tests** — **0 skipped** in this run.

**Correction to `_002`:** using the real writer makes these **production-code-path** fixtures, not
"not synthetic." They are deterministic synthetic fixtures; **none is live-market evidence.**

## Topology recommendation (unchanged, still not chosen)

`PROPOSED_PENDING_DEC-ARCH = T2 (contract-compliant metadata sidecar/manifest)` — keeps the
131-fixture-proven binary integrity stable and adds the fail-closed contract additively. Not applied.
