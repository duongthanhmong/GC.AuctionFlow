# AosLevelEngine — Prompt 4 (patched VÁ 1–6)

ES only. Measurement device. No Acceptance / scoring / alerts.

**Prior unpatched Prompt 4 code = DRAFT — superseded by this revision.**

## Batches (event-time ET, DST via America/New_York)

| Batch | Freeze trigger | Data window |
|-------|----------------|-------------|
| PRESESSION_SET | ≤ 09:29:59 ET | prior RTH / weekly / composite / nPOC / VA |
| OPEN_SET | first event ≥ 09:30 | overnight H/L from exchange time **&lt; 09:30 only** |
| IB_SET | first event ≥ 10:30 | trades in **[09:30, 10:30)** only |

## Grades

- `StructuralGrade` immutable from level type
- `FreshnessState` from completed interaction count only
- `ConfluenceCount` = distinct `IndependentSourceFamily`
- `EffectiveGrade` = pure function — see `FrozenZone.ComputeEffective`

## Interactions

State: `ARMED → ACTIVE → CLOSED → RESET_PENDING → ARMED`  
Reset = continuous outside **and** ≥ ResetTicks from edge **and** ≥ ResetTime on **monotonic** clock; any violation clears timer.  
Outcomes live in `InteractionOutcomeStore` / SQLite `interaction_outcomes` (append versions, never UPDATE).

## ATAS shell

See [`AosLevelEngine.Atas/README.md`](AosLevelEngine.Atas/README.md) — **net10.0** indicator + Historical Capability Probe.
Core remains **net8.0** (loads into net10 host; verified by successful `dotnet build`).

## Build / test

```powershell
cd "C:\Users\LOQ\Downloads\New folder (2)\AosLevelEngine"
dotnet test tests\AosLevelEngine.Tests.csproj -c Release
dotnet build AosLevelEngine.Atas\AosLevelEngine.Atas.csproj -c Release
```

## LevelManifest

`LevelSetEngine.WriteLevelManifestJson()` after freezes.

## CẦN XÁC MINH

Single-print TPO source; ATAS TimestampExchange Kind; contract code ESU6.
