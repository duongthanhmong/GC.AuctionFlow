# KDK-D2-ARTIFACT-FIT-002 — three-artifact fit (corrected, stable IDs)

**Supersedes the Q1/Q9 language in `KDK_D2_CONTRACT_FIT_001.md`** (which is retained as historical).
R3 HEAD `e373486`. No production `.cs` changed. Stable IDs only — no positional `Q#` labels.

**Stable IDs used here:** `ADR-001`, `ADR-003`, `DEC-ARCH`, `DEC-WP04`, `SEC-001`,
`AUTH-ATAS-DEPLOY`, `AUTH-LIVE-SESSION`, `DEC-D2-ACCEPT`.

## Correction of the boundary framing

`recorder_segment.schema.json` defines a **JSON segment-metadata contract** for the direct path. The
existing `.gcae` object is a **binary event container**. **A JSON schema field need not be embedded
in `SegmentHeaderRecord`/`Footer`** — some are derivable at finalization or belong in a separate
metadata artifact. Three distinct artifacts are compared, not conflated:

1. **binary `.gcae`** — header + framed raw-event records + footer (`FrameCodec`: `ContainerMagic
   0x52414347`, `FrameMagic 0x31464347`, CRC-32C Castagnoli).
2. **existing detached hash/manifest** — `ManifestWriter.WriteAtomic` → `sha  filename`.
3. **required JSON `recorder_segment` metadata** — the schema object (does not exist yet).

## Per-schema-field classification (recorder_segment.required)

| schema field | classification | basis |
|---|---|---|
| `segmentId` | **EXACTLY_EMITTED** | `SegmentHeaderRecord.SegmentId` |
| `instrumentId` | **SEMANTICALLY_EQUIVALENT** | `.Instrument` (`ObservedInstrumentIdentity`) |
| `startUtc` | **EXACTLY_EMITTED** | `.StartedUtc` |
| `endUtc` | **EXACTLY_EMITTED** | footer `.EndedUtc` |
| `dataSourceId` | **PARTIAL_DIFFERENT_SEMANTICS** | `.DeclaredDataSourceMode`+`.DeclaredProvider` (two fields, not one id) |
| `versions` | **DERIVABLE_AT_FINALIZATION** | `.RecorderSchemaVersion`+`.ContainerVersion` → 3-field block derivable |
| `sequenceStats` | **DERIVABLE_AT_FINALIZATION** | `.FirstWriterSequence`/`.LastWriterSequence`/`.RawEventRecordCount` → arrival-inversions/span computable |
| `sourceMix` | **ABSENT_REQUIRES_NEW_PRODUCER_STATE** | recorder tracks one mode; multi-source mix not modelled |
| `capabilitySnapshotId` | **ABSENT_REQUIRES_NEW_PRODUCER_STATE** | no capability snapshot linkage (DQ-003) |
| `dataQuality` | **ABSENT_REQUIRES_NEW_PRODUCER_STATE** | no fail-closed quality verdict (ADR-003) |
| `subscriptionScope` | **ABSENT_REQUIRES_NEW_PRODUCER_STATE** | not modelled |
| `containsUnrecoveredGap` | **ABSENT_REQUIRES_NEW_PRODUCER_STATE** | no reconnect-correlated gap detector (ADR-003) |

**Summary:** 4 exactly emitted, 1 semantically equivalent, 3 derivable at finalization, **1 partial /
different semantics, 5 absent-requires-new-producer-state.** The 5 absent are the ADR-003 fail-closed
contract — the load-bearing gap.

## Two implementation topologies (neither chosen)

| | **T1: extend binary container + emit JSON** | **T2: keep binary stable + contract-compliant metadata sidecar** |
|---|---|---|
| compatibility | breaks `.gcae` container version; readers must upgrade | binary readers unchanged; new sidecar file per segment |
| migration | container-version bump + reader migration | additive; no migration of existing tooling |
| atomicity | one atomic finalize covers both | two artifacts → need joint atomic finalize or a link record |
| recovery | `RecoveryScanner` extended to validate embedded JSON | `RecoveryScanner` unchanged for binary; sidecar validated separately |
| consumer | consumers parse one richer container | consumers read sidecar for metadata, container for events |

**Recommendation: `PROPOSED_PENDING_DEC-ARCH` = T2 (sidecar/manifest).** Rationale: it keeps the
131-test-proven binary integrity **stable** (no container-version churn, no reader migration) and
adds the fail-closed contract as an additive, independently-validated metadata artifact — which also
matches `DEC-ARCH: C hybrid` where the ATAS leg keeps its binary recorder and a metadata sidecar
carries capability/provenance. **Not applied; awaits `DEC-ARCH` + `DEC-WP04`.**

## Dependency (stable IDs, no positional labels)

Production writer change (`T-D2-SRC`) requires **`ADR-001` + `ADR-003` APPROVED** and **`DEC-ARCH`**
chosen. Direct leg additionally needs `SEC-001`; ATAS leg needs `AUTH-ATAS-DEPLOY`; a live evidence
run needs `AUTH-LIVE-SESSION`; formal sign-off needs `DEC-D2-ACCEPT`. **No live-session or acceptance
authority is requested now.**

## Binary integrity vs schema compatibility (harness, separated)

- **BINARY_INTEGRITY = TEST_EVIDENCED** — `dotnet test --filter ~Recorder` → **131 passed / 0 failed,
  exit 0** (`raw/R3_RECORDER_TESTS.txt`): `SegmentReaderTests` (parse), RecoveryScanner via
  `P007C3BcCloseoutCorrectionTests`+`RecorderCloseoutAuditTests` (truncated/corrupt classification),
  `RecorderCoreTests`/`TradeRecorderC3BcTests` (framing/CRC/atomic finalize). Real fixtures, not
  synthetic, not live.
- **SCHEMA_COMPATIBILITY = STATIC_FIELD_FIT** — `r2/contract_compat_harness.py` → 7 present, 5 absent.
  Labeled static; it is **not** a live contract-validation harness. No `recorder_segment` JSON
  metadata artifact is produced today.
