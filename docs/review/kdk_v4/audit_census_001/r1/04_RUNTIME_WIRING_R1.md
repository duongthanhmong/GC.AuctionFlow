# r1/04 — Runtime Wiring, corrected: snapshot field comparison

**Supersedes the census artifact 04 claim** that `GcaeRuntimeSnapshot` is a proven rename of
`StructuralAnalysisSnapshot`. R1 HEAD `323e36a`.

## `StructuralAnalysisSnapshot` (MRBS §41) — what it actually is

`MRBS_v1.1.md:1585-1645`. **Status line (verbatim):** *"REVIEW-REQUIRED. Đây là output mục tiêu
**nếu** chủ dự án quyết định tách CFD/execution khỏi core."* — a **proposed** Analysis-Only Output
Contract, **conditional on an owner decision that has not been made**. It is a flat JSON object with
scalar fields:

```
instrument, contract, analysis_time, session_id, data_quality, engine_state,
auction_regime{intraday,multi_session,relation},
reference_zone{zone_id,source,lower_ticks,upper_ticks,status},
episode{episode_id,state,attempt_number,max_excursion_ticks,outside_duration_seconds},
acceptance{state,local_poc_location,outside_volume_share,retest_count},
order_flow{effort_result,classified_volume_ratio,stacked_imbalance},
setup{state,direction,reason_codes[]},
structural_map{nearest_poc_ticks,value_center_ticks,opposite_edge_ticks},
options_context, config_version
```

## `GcaeRuntimeSnapshot` (`src/GC.AuctionFlow/Runtime/GcaeRuntimeSnapshot.cs`) — what exists

A **sealed UI-facing diagnostic aggregate** (`SnapshotVersion = "0.26.0"`), constructor takes
**~30 per-module set-snapshots + diagnostic flags**: `DataGateSnapshot`, `ContractSnapshot`,
`RuntimeCapabilitySnapshot`, `AuctionEpisodeSetSnapshot`, `AcceptanceReentryEvidenceSetSnapshot`,
`ExecutedOrderflowSetSnapshot`, `EffortResultClassificationSetSnapshot`, `FarThesisSetSnapshot`,
`AacThesisSetSnapshot`, `StructuralReferenceSetSnapshot`, `AuctionResolutionSetSnapshot`, etc.

## Field-by-field verdict

| §41 domain | present in `GcaeRuntimeSnapshot`? | as |
|---|---|---|
| data_quality / engine_state | yes | `DataGateSnapshot`, `RuntimeCapabilitySnapshot` |
| auction_regime | yes | `DirectionalContextSetSnapshot` / regime placeholders |
| reference_zone | yes | `StructuralReferenceSetSnapshot` |
| episode | yes | `AuctionEpisodeSetSnapshot` |
| acceptance | yes | `AcceptanceReentryEvidenceSetSnapshot` |
| order_flow / effort_result | yes | `ExecutedOrderflowSetSnapshot`, `EffortResultClassificationSetSnapshot` |
| setup (FAR/AAC) | yes | `FarThesisSetSnapshot`, `AacThesisSetSnapshot` |
| structural_map | partial | reference/plar snapshots |
| config_version / analysis_time | yes | `timestampUtc`, capability version |
| **shape** | **DIFFERENT** | flat scalar contract vs nested diagnostic set-snapshots with `show*Diagnostics` flags |
| **purpose** | **DIFFERENT** | §41 = analysis-only *output contract*; `GcaeRuntimeSnapshot` = *UI render* aggregate |

## Corrected verdict

`GcaeRuntimeSnapshot` **covers the same semantic domains** as the §41 proposal but is **not a
rename** and **not the §41 contract**: §41 is an unapproved, flat, analysis-only output contract
gated on an owner CFD-split decision; the implemented type is a nested UI-diagnostic snapshot. A
reviewer must not treat requirement `StructuralAnalysisSnapshot` (S41) as satisfied by
`GcaeRuntimeSnapshot`. The gap is: **no code emits the flat §41 output contract**, and §41 is not
yet approved to require one. Recorded as **UNMAPPED / CONTRACT_NOT_APPROVED**, not "rename done."

## Boundary trace (unchanged, confirmed)

Ingress `OnNewTrade` (`GcAuctionFlowIndicator.cs:780`) → recorder (`:796→:3236`,
`new SegmentWriter` `RawEventRecorderSession.cs:92`) + AMT (`_profileHost`) →
Reference/Composite (`:1476-1528`) → `new GcaeRuntimeEngine` (`:1345`) →
`runtime.Publish()` (`:2967`) → `GcaeRuntimeSnapshot` → ATAS render. **Break: NotCalibrated
acceptance gate**, by design; DLL ingress is ATAS, not direct Rithmic.
