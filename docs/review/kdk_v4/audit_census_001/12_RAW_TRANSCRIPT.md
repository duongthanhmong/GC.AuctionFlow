# 12 — Raw Command Transcript (KDK-CENSUS-001)

Audited commit: 43d458fab164c35055bd5378c78c8be7c14cd369
All commands run read-only at this HEAD by the auditor. Reproducible by another reviewer.

## Baseline
```
$ git rev-parse HEAD
43d458fab164c35055bd5378c78c8be7c14cd369
$ git rev-parse --abbrev-ref HEAD
wp-l1-rithmic-data-surface-discovery
$ git status --porcelain | wc -l           # 131, all untracked (??)
$ git status --porcelain | grep -cE "^ ?M"  # 0 tracked-modified
$ git stash list | wc -l                    # 0
```

## Source-document hashes (sha256)
```
4cf22c028d682a997e73571462b3579aab64f996cfdc0d00f886a47e529f8c5c  docs/spec/KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS.md
e55f690cacd89c7978e68c10489697f2927309704b188ca1db01b0b015411438  docs/review/kdk_v4/mrbs_adoption_r1/inputs/KDK_MRBS_v1.1.md
ca6ddcaebff4cf22194bf3ffe7b1e68864c6bedc469507b5cd98f2236a36249c  docs/review/kdk_v4/mrbs_param_recon_r3/inputs/KDK_Parameter_Registry_v1.0.md
44c446c7280a0a3c8059bd57f6b3fa0ce5381652d41b22f4b084b8ce3679b756  CLAUDE.md
```

## Inventory
```
$ git ls-files 'src/GC.AuctionFlow/**/*.cs' | wc -l   -> 248
$ git ls-files 'tests/**/*.cs' | wc -l                -> 79
$ find src -name '*.cs' -not -path '*/bin/*' -not -path '*/obj/*' | wc -l -> 271 (23 untracked Oac.*)
$ git ls-files | wc -l                                -> 563
```

## Build + Test (exit codes captured)
```
$ dotnet build src/GC.AuctionFlow/GC.AuctionFlow.csproj -c Release --nologo
Build succeeded. 0 Warning(s) 0 Error(s)   EXIT_CODE=0
$ dotnet test  tests/GC.AuctionFlow.Tests/GC.AuctionFlow.Tests.csproj -c Release --nologo
Passed!  Failed: 0, Passed: 1531, Skipped: 0, Total: 1531, Duration: 24s   EXIT_CODE=0
```
(Full output: raw/BUILD.txt, raw/TEST.txt)

## Requirements + parameters (from CSV)
```
02A rows: 679   02B rows: 679   02D rows: 93
02B implementation_status: NOT_IMPLEMENTED 591 | NOT_APPLICABLE 59 | AWAITING_DOMAIN_SPEC 28 | CODE_TESTED 1
Registry: 134 params | Proposed 92 | UnderReview 38 | Deferred 4 | Approved 0 | in-code 2
```

## Writer symbols (D2 verification)
```
src/GC.AuctionFlow/Recorder/SegmentWriter.cs   : class SegmentWriter (411 lines), tracked
src/GC.AuctionFlow/Recorder/ManifestWriter.cs  : class ManifestWriter (35), tracked
src/GC.AuctionFlow/Recorder/RecoveryScanner.cs : class RecoveryScanner (546), tracked, NO src caller
wired: RawEventRecorderSession.cs:92  new SegmentWriter(...)
```

## Evidence + brief
```
15 EvidenceIds re-hashed at HEAD: 15 match, 0 drift, 0 missing
Execution brief 26ad6e23...: 409 zip/archives hashed across Downloads/Desktop/Documents/repo -> NO MATCH
Acceptance record (owner/reviewer signed ACCEPTED): NONE FOUND
```

## Marker counts (src/GC.AuctionFlow)
```
NotCalibrated: 383 | throw new *Exception: 151 | NotImplementedException: 0 | TODO/FIXME/HACK: 0
```
