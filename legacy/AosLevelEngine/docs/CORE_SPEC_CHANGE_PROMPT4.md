# Change record — Prompt 4 Level Engine PATCHES (VÁ 1–6)

## Status
Prior Prompt 4 implementation marked **DRAFT** and superseded by this patch set (2026-07-20).

## Patches applied
1. Scope: predeclared levels only (no VWAP / developing / intraday).
2. Three event-time batches: PRESESSION_SET / OPEN_SET / IB_SET.
3. A0 deterministic profile fields — FAIL STARTUP if missing.
4. StructuralGrade + FreshnessState + ConfluenceCount; EffectiveGrade pure function.
5. Interaction state machine ARMED|ACTIVE|RESET_PENDING|CLOSED; ConcurrentInteractionGroupId.
6. InteractionOutcomes append-only (separate from LevelInteractions).

## Sidecar
SchemaVersion 5 → `AOS_*_s5.sqlite` + `interaction_outcomes` UNIQUE(InteractionId, HorizonType, OutcomeVersion).

## Single-print
`SinglePrintStatus=CAN_XAC_MINH` — not implemented; omission recorded.
