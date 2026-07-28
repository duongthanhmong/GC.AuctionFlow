<#
Phase A / A4 - atomic, provenance-bound deployment of the single GC.AuctionFlow.dll.
PREPARE ONLY during A-IMPLEMENT (use -DryRun). Never writes credentials.

A-DEP-002: the PRODUCTION entry point has NO flag that can override HEAD, the clean-tree
requirement, hash verification, or rollback. All deployment mechanics live in the internal
function Invoke-GcaeDeployCore, which is parameterised by a repo root so tests can drive it
with a TEMPORARY Git repository - without any production bypass. A real deploy refuses unless:
  - the build manifest has ALL required fields
  - manifest.working_tree_clean == true AND manifest.gate_verdict == 'PASS'
  - the working tree at RepoRoot is clean
  - manifest.commit == HEAD at RepoRoot
  - the supplied build-manifest file's SHA == the -BuildManifestSha passed in (recorded)
  - the input manifest re-verifies (recompute == manifest.pre_input_manifest_sha256)
  - sha256(supplied DLL) == manifest.dll_sha256
Then: backup -> temp copy -> verify -> atomic move -> verify -> write deploy manifest file.
On ANY failure after replacement: restore backup, or (no previous target) REMOVE the new
target and verify its absence.
#>
param(
  [string]$ManifestPath = "docs/review/phaseA/A_BUILD_MANIFEST.json",
  [Parameter(Mandatory=$true)][string]$BuildManifestSha,   # must match sha256(ManifestPath); recorded
  [string]$InputManifestPath = "docs/review/phaseA/A_INPUT_MANIFEST.sha256",
  [string]$DllPath = "",
  [string]$Target  = "$env:APPDATA\ATAS\Indicators\GC.AuctionFlow.dll",
  [string]$DeployManifestOut = "docs/review/phaseA/A_DEPLOY_MANIFEST.json",
  [string]$RepoRoot = ".",     # tests pass a temporary git repo; production uses the real repo
  [switch]$DryRun
)
$ErrorActionPreference = "Stop"

function Sha256File($p){ if(Test-Path $p){ (Get-FileHash -Algorithm SHA256 $p).Hash.ToLower() } else { "<absent>" } }
function Sha256Str($s){ $b=[Text.Encoding]::UTF8.GetBytes($s); ((New-Object Security.Cryptography.SHA256Managed).ComputeHash($b) | ForEach-Object { $_.ToString("x2") }) -join "" }
function GitAt($root, [string[]]$a){ & git -C $root @a }
function IsSha($h){ $h -is [string] -and $h -match '^[0-9a-f]{64}$' }
function AtasHome(){ if($env:ATAS_HOME){ $env:ATAS_HOME } else { "C:\Program Files (x86)\ATAS Platform" } }
$ATAS_MAP = @{ "ATAS.Indicators.dll"="atas_indicators_sha256"; "ATAS.DataFeedsCore.dll"="atas_datafeeds_sha256"; "OFT.Rendering.dll"="atas_oft_rendering_sha256" }

function Recompute-InputManifestSha($root, $inputManifestPath){
  # recompute each 'sha  path' line's file hash under $root and re-hash the reconstructed manifest,
  # matching clean_build.sh's format exactly (git repo-relative paths).
  if(-not (Test-Path $inputManifestPath)){ throw "input manifest missing: $inputManifestPath" }
  $lines = Get-Content $inputManifestPath
  $rebuilt = New-Object System.Collections.Generic.List[string]
  foreach($ln in $lines){
    if($ln -match '^[0-9a-f]{64}\s+\*?(.+)$'){
      $rel = $Matches[1].TrimStart('*')
      $fp = Join-Path $root $rel
      if(-not (Test-Path $fp)){ throw "input file missing during re-verify: $rel" }
      $h = Sha256File $fp
      $rebuilt.Add("$h  $rel")
    }
    elseif($ln -match '^(ATAS\.\S+|OFT\.\S+)\s+[0-9a-f]{64}$'){
      # A-DEP-003: re-hash the installed ATAS assembly (do NOT trust the stored hash)
      $name = ($ln -split '\s+')[0]; $fp = Join-Path (AtasHome) $name
      if(-not (Test-Path $fp)){ throw "ATAS assembly missing during re-verify: $name" }
      $rebuilt.Add("$name  $(Sha256File $fp)")
    }
    else { $rebuilt.Add($ln) }     # comment lines pass through
  }
  return Sha256Str (($rebuilt -join "`n") + "`n")
}

function Invoke-GcaeDeployCore {
  param($ManifestPath,$BuildManifestSha,$InputManifestPath,$DllPath,$Target,$DeployManifestOut,$RepoRoot,[switch]$DryRun)

  if(-not (Test-Path $ManifestPath)){ throw "build manifest missing: $ManifestPath" }
  # build-manifest SHA must be supplied and match (recorded)
  $actualManSha = Sha256File $ManifestPath
  if($BuildManifestSha -ne $actualManSha){ throw "supplied BuildManifestSha $BuildManifestSha != actual $actualManSha" }
  $m = Get-Content $ManifestPath -Raw | ConvertFrom-Json

  # all required fields present (A-DEP-003)
  $req = @("schema","commit","working_tree_clean","gate_verdict","configuration","dll_path","dll_sha256",
           "sdk_pinned","sdk_resolved_root","pre_input_manifest_sha256","post_input_manifest_sha256",
           "dotnet_total","dotnet_failed","python_total","python_failed",
           "dotnet_test_trx_sha256","python_test_junit_sha256",
           "atas_indicators_sha256","atas_datafeeds_sha256","atas_oft_rendering_sha256")
  foreach($f in $req){ if($null -eq $m.$f){ throw "manifest missing required field: $f" } }

  if($m.schema -ne "gcae-build-manifest-v2"){ throw "unexpected manifest schema '$($m.schema)'" }
  if($m.gate_verdict -ne "PASS"){ throw "gate_verdict '$($m.gate_verdict)' is not deployable (need PASS)" }
  if(-not [bool]$m.working_tree_clean){ throw "manifest.working_tree_clean is false - not deployable" }
  if($m.configuration -ne "Release"){ throw "configuration '$($m.configuration)' != Release" }
  if($m.sdk_pinned -ne $m.sdk_resolved_root){ throw "SDK pin '$($m.sdk_pinned)' != resolved '$($m.sdk_resolved_root)'" }
  if($m.pre_input_manifest_sha256 -ne $m.post_input_manifest_sha256){ throw "pre != post input manifest sha" }
  if([int]$m.dotnet_failed -ne 0 -or [int]$m.dotnet_total -lt 1461){ throw "invalid .NET test counts" }
  if([int]$m.python_failed -ne 0 -or [int]$m.python_total -ne 37){ throw "invalid Python test counts" }
  foreach($f in @("dll_sha256","dotnet_test_trx_sha256","python_test_junit_sha256",
                  "atas_indicators_sha256","atas_datafeeds_sha256","atas_oft_rendering_sha256")){
    if(-not (IsSha $m.$f)){ throw "field $f is not a valid sha256" } }
  # re-hash the three INSTALLED ATAS reference assemblies and require they match the manifest
  foreach($name in $ATAS_MAP.Keys){
    $fp = Join-Path (AtasHome) $name
    if(-not (Test-Path $fp)){ throw "installed ATAS assembly missing: $name" }
    if((Sha256File $fp) -ne $m.($ATAS_MAP[$name])){ throw "installed ATAS assembly changed: $name" } }

  $head = (GitAt $RepoRoot @("rev-parse","HEAD")).Trim()
  if($m.commit -ne $head){ throw "manifest.commit $($m.commit) != HEAD $head" }
  $dirty = GitAt $RepoRoot @("status","--porcelain")
  if($dirty){ throw "working tree not clean at $RepoRoot" }

  # input manifest re-verification
  $recomputed = Recompute-InputManifestSha $RepoRoot $InputManifestPath
  if($recomputed -ne $m.pre_input_manifest_sha256){ throw "input manifest re-verify mismatch ($recomputed != $($m.pre_input_manifest_sha256))" }

  if(-not $DllPath){ $DllPath = Join-Path $RepoRoot $m.dll_path }
  if(-not (Test-Path $DllPath)){ throw "DLL not found: $DllPath" }
  $srcHash = Sha256File $DllPath
  if($srcHash -ne $m.dll_sha256){ throw "DLL hash $srcHash != manifest.dll_sha256 $($m.dll_sha256)" }

  $prevHash = Sha256File $Target
  $hadTarget = Test-Path $Target
  $backup = "$Target.bak-$head"
  $deploy = [ordered]@{ utc=(Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"); commit=$head
    source_dll=$DllPath; source_sha256=$srcHash; target=$Target; prev_sha256=$prevHash
    had_previous_target=$hadTarget; backup=$backup; build_manifest_sha256=$actualManSha
    gate_verdict=$m.gate_verdict; dry_run=[bool]$DryRun }

  if($DryRun){
    Write-Host "DRY RUN - all provenance checks passed; no file copied."
    $deploy | ConvertTo-Json | Set-Content -Encoding UTF8 $DeployManifestOut
    $deploy | ConvertTo-Json | Write-Output
    return
  }

  $dir = Split-Path $Target; if(-not (Test-Path $dir)){ New-Item -ItemType Directory -Force $dir | Out-Null }
  try {
    if($hadTarget){ Copy-Item $Target $backup -Force }
    $tmp = "$Target.tmp-$([guid]::NewGuid().ToString('N'))"
    Copy-Item $DllPath $tmp -Force
    if((Sha256File $tmp) -ne $srcHash){ throw "temp copy hash mismatch before move" }
    Move-Item $tmp $Target -Force
    $depHash = Sha256File $Target
    if($env:GCAE_FORCE_VERIFY_FAIL -eq "1"){ throw "forced post-copy failure (test env)" }
    if($depHash -ne $srcHash){ throw "deployed hash $depHash != source $srcHash" }
    $deploy.deployed_sha256 = $depHash
    $deploy | ConvertTo-Json | Set-Content -Encoding UTF8 $DeployManifestOut
    $deploy | ConvertTo-Json | Write-Output
    Write-Host "DEPLOY OK: deployed == source ($depHash)"
  }
  catch {
    Write-Warning "DEPLOY FAILED: $_"
    if($hadTarget -and (Test-Path $backup)){
      Copy-Item $backup $Target -Force
      if((Sha256File $Target) -eq $prevHash){ Write-Host "ROLLBACK OK: restored previous" }
      else { throw "ROLLBACK VERIFY FAILED" }
    } elseif(-not $hadTarget) {
      if(Test-Path $Target){ Remove-Item $Target -Force }
      if(Test-Path $Target){ throw "ROLLBACK FAILED: new target still present" }
      Write-Host "ROLLBACK OK: removed newly installed target (no previous existed)"
    }
    throw
  }
}

try {
  Invoke-GcaeDeployCore -ManifestPath $ManifestPath -BuildManifestSha $BuildManifestSha `
    -InputManifestPath $InputManifestPath -DllPath $DllPath -Target $Target `
    -DeployManifestOut $DeployManifestOut -RepoRoot $RepoRoot -DryRun:$DryRun
  exit 0
} catch { Write-Error $_; exit 1 }
