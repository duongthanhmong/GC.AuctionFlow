# KDK-D2-CONTRACT-FIT-001 — Writer ↔ Schema field-fit (analysis only)

**Why this and not KDK-D2-001:** at HEAD `323e36a` **no signed owner authority exists** — all 7
ADRs are `AWAITING_OWNER_APPROVAL` and no acceptance record exists. Therefore **no production wiring
was changed.** This is the non-production preparation the instruction requires. It does **not**
encode the recorder-architecture decision (owner Q9, Decision Pack §5) — it maps both paths.

## The two layers (they are not the same artifact)

| | existing recorder | Stage 2 schema |
|---|---|---|
| what | binary `.gcae` container: header + framed raw-event records + footer + detached sha256 | JSON **segment-metadata** object |
| producer | `SegmentWriter` (`Recorder/SegmentWriter.cs:92`), `RawEventRecorderSession.cs:92` | `recorder_segment.schema.json` |
| path | **ATAS-callback** | **direct-Rithmic** |
| per-record payload | JSON (`RecorderJson.SerializeEnvelope`) | n/a (metadata only) |

## Field-by-field: `SegmentHeaderRecord`/`Footer` vs `recorder_segment.schema.json` (required)

| schema required field | in binary recorder? | source |
|---|---|---|
| `segmentId` | **YES** | `SegmentHeaderRecord.SegmentId` |
| `instrumentId` | **YES** | `.Instrument` (`ObservedInstrumentIdentity`) |
| `startUtc` | **YES** | `.StartedUtc` |
| `endUtc` | **YES** | footer `.EndedUtc` |
| `dataSourceId` | **PARTIAL** | `.DeclaredDataSourceMode` + `.DeclaredProvider` (+provenance) — not a single id |
| `sourceMix` | **NO** | — |
| `capabilitySnapshotId` | **NO** | — |
| `dataQuality` | **NO** | — |
| `subscriptionScope` | **NO** | — |
| `sequenceStats` | **PARTIAL** | footer has `RawEventRecordCount`/`MarketEventRecordCount`; **no** arrival-inversions/span/distinct |
| `containsUnrecoveredGap` | **NO** | — |
| `versions` (3-field block) | **PARTIAL** | `RecorderSchemaVersion`+`ContainerVersion` — not algorithm/config/dataSchema triple |

## Verdict

| dimension | finding |
|---|---|
| **reusable** | container framing, atomic tmp→rename, per-file **detached sha256** (`ManifestWriter.WriteAtomic` already writes `sha  filename`), sequence tracking (`FirstWriterSequence`/`LastWriterSequence`), instrument identity + provenance |
| **mismatch** | the schema is a **fail-closed integrity contract** (ADR-003: `dataQuality`, `containsUnrecoveredGap`, `discontinuities`, `sequenceStats`, `capabilitySnapshotId`, `subscriptionScope`) — **none of which the binary recorder emits.** These are exactly the fields that make a segment safe to consume. |
| **missing provenance** | `capabilitySnapshotId` (DQ-003) and `sourceMix` (ADR-006) absent — a consumer cannot resolve acquisition conditions |
| **dead recovery path** | `RecoveryScanner.ValidateSegmentFile`/`ScanSessionDirectory` exist and are **tested** but have **no production caller** — the fail-closed recovery the schema assumes is not invoked at runtime |

## Exact implementation plan for KDK-D2-001 (runs ONLY after owner Q1 + Q9)

1. **Owner decides recorder path** (Decision Pack §5). If **B/C (ATAS or hybrid)**: extend
   `SegmentHeaderRecord`/`Footer` with the 7 missing schema fields, deriving `sequenceStats` from
   the existing writer-sequence tracking and `dataQuality`/`containsUnrecoveredGap` from a
   reconnect-correlated gap detector. If **A (direct-Rithmic)**: build a new direct-path segment
   emitter against the schema, reusing `SegmentWriter`'s framing/sha256.
2. Wire `RecoveryScanner` into the recorder startup path (currently dead) so a partial segment is
   classified on restart.
3. Add deterministic **positive** tests (schema-valid segment validates) and **negative** tests
   (unrecovered gap ⇒ `dataQuality=Invalid`+`DATA_INVALID`+discontinuity; mirrors the 47/47 Stage 2
   suite's fail-closed cases).
4. Preserve raw/unparseable frames (ADR-007) — already the container's job; verify.
5. `dotnet build` + `dotnet test`; then ONE real recorded session; validate emitted segment(s)
   against the 6 schemas.
6. **Do not** claim live/production readiness without that real session (D1).

## Live-run command for KDK-D2-001 (for the owner to authorize, not to run now)

```
# after ADRs approved + recorder path chosen:
dotnet build src/GC.AuctionFlow/GC.AuctionFlow.csproj -c Release --nologo
dotnet test  tests/GC.AuctionFlow.Tests/GC.AuctionFlow.Tests.csproj -c Release --nologo
# then one real session in ATAS (path B/C) or direct probe (path A), producing segment files,
# then: python docs/review/kdk_v4/wp_l1_rithmic/stage2_closure/tests/validate_segments.py <dir>
```

**No production wiring was modified by this document.** `RecoveryScanner` remains un-wired; that is
a deliberate hold pending authority, not an oversight.
