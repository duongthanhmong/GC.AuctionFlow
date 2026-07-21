# AosLevelEngine.Atas — ATAS shell + Historical Capability Probe

**TFM: net10.0** (required — ATAS DLLs compile against `System.Runtime 10.0.0.0`; Phase 0 CS1705).

Bridges `AosLevelEngine` core (net8.0) into the ATAS host. This build measures historical capability only — it does **not** compute levels yet.

**ShellBuild: LE-ATAS-H03** (TradingDate + FixedProfile session alignment guard)

## Session resolution (v0.3)

- `TradingDate` = single definition (rollover 18:00 ET from Overnight.Start)
- JSON: `sessionResolution.resolvedTradingDate`, `priorTradingDate`, `sessionRolloverHourEt`, `sessionAlignment`
- FixedProfile LastDay vs prior → `ALIGNED` / `MISALIGNED` + `SESSION_DEFINITION_MISMATCH` if needed
- Each level source has `sourceTradingDate`

## Build

```powershell
cd "C:\Users\LOQ\Downloads\New folder (2)\AosLevelEngine"
dotnet build AosLevelEngine.Atas\AosLevelEngine.Atas.csproj -c Release
```

Output:

- `AosLevelEngine.Atas\bin\Release\net10.0\AosLevelEngine.Atas.dll`
- dependency: `AosLevelEngine.dll` (net8) copied beside it

## Deploy into ATAS

Exit ATAS fully before copy (DLL lock). Folders:

`%APPDATA%\ATAS\Indicators` and/or `%USERPROFILE%\Documents\ATAS\Indicators`

```powershell
$src = "C:\Users\LOQ\Downloads\New folder (2)\AosLevelEngine\AosLevelEngine.Atas\bin\Release\net10.0"
$destDocs = Join-Path $env:USERPROFILE "Documents\ATAS\Indicators"
$destApp = Join-Path $env:APPDATA "ATAS\Indicators"
New-Item -ItemType Directory -Force -Path $destDocs,$destApp | Out-Null
Copy-Item "$src\AosLevelEngine.Atas.dll","$src\AosLevelEngine.dll" $destDocs -Force
Copy-Item "$src\AosLevelEngine.Atas.dll","$src\AosLevelEngine.dll" $destApp -Force
```

Add indicator: **AOS → AOS Level Engine — Historical Capability Probe**. Chart **must be M1** for RotationR (`NO_SECONDARY_SERIES_API`).

## How to re-export (H02)

| Trigger | Behavior |
|---------|----------|
| **Export Nonce** | Increment (1→2→3…) + Apply → new export every time |
| **Export HistoricalCapability** | Tick + Apply → one export, checkbox clears; tick again for another run |
| **OnInitialize** | Auto-bumps nonce and exports when bars ready |
| **Bar count Δ ≥ 500** | Auto-export (seed threshold) |

`Status` updates every run with UTC time + OK/FAIL reason. Writes:

- `%USERPROFILE%\.aos\HistoricalCapability.json` (overwrite)
- `%USERPROFILE%\.aos\HistoricalCapability_yyyyMMdd_HHmmss.json` (archive)
- `%USERPROFILE%\.aos\level_engine_atas.log`

Include **Include RequestFixedProfile(LastDay)** (default true) embeds live FixedProfile result into the JSON.

## Probe findings locked into report

1. **secondarySeries.verdict** = `NO_SECONDARY_SERIES_API` → chart must be M1  
2. **contractCodeCandidates** from `InstrumentInfo.*` + `TradingManager.Security.Code/Expiration/SecurityId` (DLL-verified)  
3. **fixedProfileRuntime** — latency, levels, VAH/VAL/POC or fail reason  
4. **timestampExchangeKind** — `SpecifyKind(Utc)`; unexpected Kind → `dataQualityEvents`  
5. Export trigger no longer blocked by a sticky `_probeRan` flag
