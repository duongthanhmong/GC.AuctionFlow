# 02_REPOSITORY_INVENTORY.md

**Audited commit:** `43d458fab164c35055bd5378c78c8be7c14cd369` (`43d458f`)  
**Branch:** `wp-l1-rithmic-data-surface-discovery`  
**Generated:** deterministic recount at audited HEAD. Numbers here supersede every earlier report.

## Tracked file inventory by extension

| ext | files |
|---|---|
| `.cs` | 355 |
| `.py` | 50 |
| `.md` | 50 |
| `.csv` | 33 |
| `.txt` | 27 |
| `.json` | 22 |
| `.csproj` | 5 |
| `.sh` | 4 |
| `.log` | 4 |
| `.gitkeep` | 4 |
| `.ps1` | 3 |
| `.zip` | 1 |
| `.sln` | 1 |
| `.ini` | 1 |
| `.gitignore` | 1 |
| `.gitattributes` | 1 |
| `.bundle` | 1 |

**Total tracked files: 563**

## Source .cs counts

| scope | count |
|---|---|
| tracked `src/GC.AuctionFlow/**/*.cs` | 248 |
| tracked `tests/**/*.cs` | 79 |
| on-disk `src/**/*.cs` excl bin+obj | 271 |
| **untracked legacy** `src/Oac.Core`+`src/Oac.Atas` | 23 |

> The 271 on-disk figure minus 248 tracked = 23 untracked legacy files under `src/Oac.Core` and
> `src/Oac.Atas`. `CLAUDE.md` `D-P0-02-002` forbids building either. They are NOT GCAE source.

## src/GC.AuctionFlow per-folder (tracked .cs)

| folder | .cs |
|---|---|
| `Probe/` | 39 |
| `Recorder/` | 28 |
| `Research/` | 18 |
| `Thesis/` | 12 |
| `Profile/` | 12 |
| `Runtime/` | 11 |
| `Episode/` | 11 |
| `Directional/` | 10 |
| `Evidence/` | 9 |
| `Reference/` | 8 |
| `Orderflow/` | 7 |
| `Composite/` | 7 |
| `OptionFlow/` | 6 |
| `Efficiency/` | 6 |
| `Data/` | 6 |
| `Cluster/` | 6 |
| `Resolution/` | 5 |
| `Participation/` | 5 |
| `EffortResult/` | 5 |
| `Core/` | 5 |
| `Atas/` | 5 |
| `UI/` | 4 |
| `Plar/` | 4 |
| `Memory/` | 4 |
| `Maturity/` | 4 |
| `Facilitation/` | 4 |
| `Imbalance/` | 2 |
| `Execution/` | 2 |
| `Logging/` | 1 |
| `Entry/` | 1 |
| `DayStructure/` | 1 |
