# r1/03 — Corrected Code-to-Requirement Matrix

**Supersedes census artifact 03.** R1 HEAD `323e36a`, audited base `43d458f`.

## The correction

Census artifact 03 read 02B's 591 `NOT_IMPLEMENTED` as near-proof the domain is unbuilt.
**02B is provisional and its 591 `NOT_IMPLEMENTED` rows carry ZERO source_files/source_symbols**
— they are **UNMAPPED_IN_02B** (requirement→code traceability not yet populated), which is NOT
the same as code absence. Verified: `grep` of the 591 rows' source columns = 0 populated.

Independent code evidence (R3E alignment matrix, module granularity) contradicts 'unbuilt':

| module | alignment | cited symbol (src) |
|---|---|---|
| MarketDataNormalization | PARTIAL | class GcaeRuntimeSnapshot |
| DataQualityCapability | PARTIAL | enum DataState |
| SessionTradingDate | CONFLICT | class PrimaryAuctionClock |
| TPO | PARTIAL | class ClassicTpoEngine |
| SessionVolumeProfile | PARTIAL | class VolumeProfileEngine |
| CompositeProfile | PARTIAL | enum CompositeStatus |
| AuctionRegime | PARTIAL | enum DirectionalAuctionState |
| OneTimeFraming | PARTIAL | class OneTimeFramingTracker |
| ReferenceZone | PARTIAL | enum ReferenceType |
| PriceMemory | PARTIAL | class PriceMemoryHost |
| AuctionEpisode | PARTIAL | enum EpisodeState |
| IndependentAttemptRearm | PARTIAL | AttemptCount |
| AcceptanceReacceptance | PARTIAL | AcceptanceEvidenceVector |
| M1OrderFlow | PARTIAL | class ExecutedOrderflowHost |
| Imbalance | PARTIAL | enum ImbalanceQualification |
| CVD | PARTIAL | ClassifiedCvd |
| EffortResult | PARTIAL | AuctionEffortEvidenceVector |
| FAR | PARTIAL | class FarThesisHost |
| AAC | PARTIAL | class AacThesisHost |
| ValueRotation | NOT_IMPLEMENTED | enum MissingEvidenceKind |
| StructuralAnalysisSnapshot | PARTIAL | class GcaeRuntimeSnapshot |
| OptionsContext | PARTIAL | enum OptionFlowLineKind |
| EngineLifecycleWarmup | NOT_IMPLEMENTED | class GcaeRuntimeSnapshot |
| EventOrderingBarFinalizati | PARTIAL | Validate |
| DeterministicReplayAudit | PARTIAL | DataGateReasonCodes |

## Two-granularity truth (the corrected conclusion)

| granularity | measure | state |
|---|---|---|
| **module** (R3E, 25 modules) | 22 PARTIAL + 1 CONFLICT have cited code symbols; 2 NOT_IMPLEMENTED | **CODE_PRESENT** for 23/25 |
| **requirement** (02B, 679) | 591 UNMAPPED_IN_02B, 59 NOT_APPLICABLE, 28 AWAITING_DOMAIN_SPEC, 1 CODE_TESTED | **traceability almost entirely absent** |
| **calibration** (Registry, 134) | 0 Approved, 2 in code | **entirely UNCALIBRATED** |

**Corrected statement:** the GCAE analysis core is *largely built at module granularity*
(23/25 modules carry real code, 1531 tests pass), *almost entirely untraced at requirement
granularity* (02B provisional, 591 rows unmapped to code), and *entirely uncalibrated* (0/134
parameters approved). 'Almost entirely unbuilt' is **withdrawn**.

## State legend applied per the correction spec

- **UNMAPPED_IN_02B** — 591 rows: 02B has no source symbol; code may or may not exist. NOT converted to NOT_IMPLEMENTED.
- **CODE_PRESENT / RUNTIME_WIRED** — 23/25 R3E modules with cited symbols reachable from the ATAS composition root (see r1/04).
- **TEST_EVIDENCED** — 1531 unit tests; the single 02B CODE_TESTED row (KDK-CH21-REQ-003) plus module-level tests.
- **LIVE_EVIDENCED** — only the WP-L1-RITHMIC probe surfaces (15 EvidenceIds); NO analysis-core module is LIVE_EVIDENCED.
- **ACCEPTED** — none.
- **NOT_APPLICABLE** (59) / **AWAITING_DOMAIN_SPEC** (28, Ch76/77/79) — unchanged from 02B.

**Scope honesty:** this correction establishes module-level CODE_PRESENT from R3E's cited symbols.
It does NOT re-map all 591 requirements to symbols (that is the 02B ratification work, GOV-Q5).
