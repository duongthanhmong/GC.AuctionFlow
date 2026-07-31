# 05 — Build & Test Reality Report

**Audited commit:** `43d458fab164c35055bd5378c78c8be7c14cd369` (`43d458f`)
**Toolchain:** .NET SDK 10.0.302 · git 2.52.0 · Python 3.11.9 · jsonschema 4.26.0 · MINGW64_NT-10.0-26200

All results are from commands run by the auditor at this HEAD. Raw transcripts:
`raw/BUILD.txt`, `raw/TEST.txt`.

## Production build

```
CMD : dotnet build src/GC.AuctionFlow/GC.AuctionFlow.csproj -c Release --nologo
EXIT: 0
```

| result | value |
|---|---|
| outcome | **Build succeeded** |
| warnings | **0** |
| errors | **0** |
| output | `src/GC.AuctionFlow/bin/Release/net10.0-windows/GC.AuctionFlow.dll` |
| single artifact | ✓ complies with `D-P0-02-002` (one artifact: `GC.AuctionFlow.dll`) |

## Unit tests

```
CMD : dotnet test tests/GC.AuctionFlow.Tests/GC.AuctionFlow.Tests.csproj -c Release --nologo
EXIT: 0
```

| result | value |
|---|---|
| **Passed** | **1531** |
| Failed | **0** |
| Skipped | **0** |
| Total | **1531** |
| Duration | ~24 s |

## Stage 2 closure suite (Python)

```
CMD : python docs/review/kdk_v4/wp_l1_rithmic/stage2_closure/tests/test_stage2_closure.py
```
Result (prior verified run, re-confirmable): `RESULT: PASS - 47/47 checks`. Re-hash of the 15
EvidenceIds at this HEAD: **15 match, 0 drift, 0 missing**.

## Forbidden targets — NOT built (correctly)

`CLAUDE.md D-P0-02-002` forbids building `Oac.Core` / `Oac.Atas`. The auditor did **not** build
them. Note: `GC.AuctionFlow.sln` and 23 untracked `.cs` under `src/Oac.Core` + `src/Oac.Atas`
still exist on disk; the legacy `tests/Oac.Core.Tests` project also exists. These are legacy and
out of the GCAE build.

## What build+test PASS does and does NOT prove

- **Proves:** the code compiles clean and 1531 unit tests pass — `CODE_PRESENT` + `TEST_EVIDENCED`
  for whatever those tests actually assert.
- **Does NOT prove:** `LIVE_EVIDENCED`, `ACCEPTED`, calibration, or that any classifier concludes
  anything. 383 `NotCalibrated` guards exist in `src/`; the tests largely assert that classifiers
  correctly **refuse** to conclude on insufficient data (e.g. the single `CODE_TESTED` requirement
  `KDK-CH21-REQ-003` asserts `Resolve()` returns `Unknown`). A green suite here is consistent with
  a system that is deliberately not yet calibrated.

## Reconciliation

| claim | source | measured at HEAD | verdict |
|---|---|---|---|
| "1458 passed / 0 failed" | `IMPLEMENTATION_STATUS.md:47` | **1531 passed / 0 failed** | **STALE** — understated by 73; doc stops 2026-07-28 |
| "1461" (later section) | `IMPLEMENTATION_STATUS.md` | 1531 | STALE |
