<#
Filesystem + temp-Git-repo tests for deploy.ps1 (A-DEP-002 / A-DEP-003). No ATAS, no real
deploy. Builds a throwaway git repo, a fake ATAS_HOME, a matching input manifest (incl. ATAS
lines) and a full v2 build manifest, then drives the production deploy.ps1 (which has NO
provenance-bypass flags). Verifies provenance, working_tree_clean, input re-verification,
schema/SDK/config/counts/hashes, ATAS re-hash, and both rollback paths.
#>
param([string]$LogPath = "")
$ErrorActionPreference = "Stop"
$here   = Split-Path -Parent $MyInvocation.MyCommand.Path
$deploy = Join-Path $here "..\deploy.ps1"
function Sha256File($p){ (Get-FileHash -Algorithm SHA256 $p).Hash.ToLower() }
function Sha256Str($s){ $b=[Text.Encoding]::UTF8.GetBytes($s); ((New-Object Security.Cryptography.SHA256Managed).ComputeHash($b) | ForEach-Object { $_.ToString("x2") }) -join "" }
function WriteText($p,$s){ [IO.File]::WriteAllText($p, $s) }
$pass=0;$fail=0;$lines=@()
function Assert($d,$c){ if($c){$script:pass++;$script:lines+="PASS  $d"}else{$script:fail++;$script:lines+="FAIL  $d"} }

$work = Join-Path ([IO.Path]::GetTempPath()) ("gcae_dep_" + [guid]::NewGuid().ToString("N"))
$repo = Join-Path $work "repo"; New-Item -ItemType Directory -Force (Join-Path $repo "src") | Out-Null
WriteText (Join-Path $repo "global.json") '{ "sdk": { "version": "10.0.302" } }'
WriteText (Join-Path $repo "src\app.dll") "ARTIFACT-V1"
& git init -q $repo; & git -C $repo config user.email "t@t"; & git -C $repo config user.name "t"
& git -C $repo add -A; & git -C $repo commit -q -m init
$commit = (& git -C $repo rev-parse HEAD).Trim()

# fake ATAS_HOME with the three reference assemblies
$atas = Join-Path $work "atas"; New-Item -ItemType Directory -Force $atas | Out-Null
WriteText (Join-Path $atas "ATAS.Indicators.dll") "IND"; WriteText (Join-Path $atas "ATAS.DataFeedsCore.dll") "DF"; WriteText (Join-Path $atas "OFT.Rendering.dll") "OFT"
$ai=Sha256File (Join-Path $atas "ATAS.Indicators.dll"); $ad=Sha256File (Join-Path $atas "ATAS.DataFeedsCore.dll"); $ao=Sha256File (Join-Path $atas "OFT.Rendering.dll")
$env:ATAS_HOME = $atas

$gj=Sha256File (Join-Path $repo "global.json"); $dll=Sha256File (Join-Path $repo "src\app.dll")
$imPath = Join-Path $work "A_INPUT_MANIFEST.sha256"
$imLines = @("# test input manifest","$gj  global.json","$dll  src/app.dll","ATAS.Indicators.dll  $ai","ATAS.DataFeedsCore.dll  $ad","OFT.Rendering.dll  $ao")
WriteText $imPath ($imLines -join "`n"); $preSha = Sha256Str ((($imLines) -join "`n") + "`n")

function BaseM(){ [ordered]@{ schema="gcae-build-manifest-v2"; commit=$commit; working_tree_clean=$true; gate_verdict="PASS";
  configuration="Release"; dll_path="src/app.dll"; dll_sha256=$dll; sdk_pinned="10.0.302"; sdk_resolved_root="10.0.302";
  pre_input_manifest_sha256=$preSha; post_input_manifest_sha256=$preSha; dotnet_total=1461; dotnet_failed=0;
  python_total=37; python_failed=0; dotnet_test_trx_sha256=("a"*64); python_test_junit_sha256=("b"*64);
  atas_indicators_sha256=$ai; atas_datafeeds_sha256=$ad; atas_oft_rendering_sha256=$ao } }
function WriteM($name,$over){ $o=BaseM; foreach($k in $over.Keys){ if($over[$k] -eq "__REMOVE__"){$o.Remove($k)}else{$o[$k]=$over[$k]} }; $p=Join-Path $work $name; WriteText $p ($o|ConvertTo-Json); return $p }
$tgt = Join-Path $work "deployed.dll"; $dman = Join-Path $work "deploy_manifest.json"
function Run($manPath,$dllOverride,$extra){
  $ms = Sha256File $manPath; $dp = if($dllOverride){$dllOverride}else{(Join-Path $repo "src\app.dll")}
  $a = @("-ManifestPath",$manPath,"-BuildManifestSha",$ms,"-InputManifestPath",$imPath,"-DllPath",$dp,
         "-Target",$tgt,"-DeployManifestOut",$dman,"-RepoRoot",$repo) + $extra
  $prev=$ErrorActionPreference; $ErrorActionPreference="SilentlyContinue"
  & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $deploy @a 2>$null 1>$null
  $code=$LASTEXITCODE; $ErrorActionPreference=$prev; return $code
}

Assert "happy path deploys (0)" ((Run (WriteM "ok.json" @{}) $null @()) -eq 0)
Assert "target == source hash" ((Test-Path $tgt) -and ((Sha256File $tgt) -eq $dll))
Assert "deploy manifest written" (Test-Path $dman)
Assert "working_tree_clean=false refused" ((Run (WriteM "wtcf.json" @{working_tree_clean=$false}) $null @()) -eq 1)
Assert "non-deployable verdict refused" ((Run (WriteM "verdict.json" @{gate_verdict="PASS_FOR_REVIEW_NOT_DEPLOYABLE"}) $null @()) -eq 1)
Assert "stale commit refused" ((Run (WriteM "stale.json" @{commit=("f"*40)}) $null @()) -eq 1)
Assert "wrong dll hash refused" ((Run (WriteM "wrong.json" @{dll_sha256=("0"*64)}) $null @()) -eq 1)
Assert "missing field refused" ((Run (WriteM "miss.json" @{pre_input_manifest_sha256="__REMOVE__"}) $null @()) -eq 1)
Assert "wrong schema refused" ((Run (WriteM "schema.json" @{schema="bad"}) $null @()) -eq 1)
Assert "wrong SDK refused" ((Run (WriteM "sdk.json" @{sdk_resolved_root="9.9.9"}) $null @()) -eq 1)
Assert "pre/post input mismatch refused" ((Run (WriteM "prepost.json" @{post_input_manifest_sha256=("c"*64)}) $null @()) -eq 1)
Assert "invalid dotnet counts refused" ((Run (WriteM "counts.json" @{dotnet_failed=3}) $null @()) -eq 1)
Assert "invalid ATAS hash field refused" ((Run (WriteM "atasbad.json" @{atas_indicators_sha256="nothex"}) $null @()) -eq 1)
# wrong build-manifest sha
$mOK = WriteM "ok2.json" @{}; $prev=$ErrorActionPreference; $ErrorActionPreference="SilentlyContinue"
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $deploy -ManifestPath $mOK -BuildManifestSha ("a"*64) -InputManifestPath $imPath -DllPath (Join-Path $repo "src\app.dll") -Target $tgt -DeployManifestOut $dman -RepoRoot $repo 2>$null 1>$null
$c=$LASTEXITCODE; $ErrorActionPreference=$prev; Assert "wrong build-manifest sha refused" ($c -eq 1)
# changed ATAS assembly (mutate installed file after manifest)
WriteText (Join-Path $atas "ATAS.Indicators.dll") "TAMPERED"
Assert "changed ATAS assembly refused" ((Run (WriteM "atas.json" @{}) $null @()) -eq 1)
WriteText (Join-Path $atas "ATAS.Indicators.dll") "IND"   # restore
# input re-verify mismatch (mutate source after manifest), then restore+recommit
WriteText (Join-Path $repo "global.json") '{ "sdk": { "version": "9.9.9" } }'
Assert "input re-verify mismatch refused" ((Run (WriteM "iv.json" @{}) $null @()) -eq 1)
WriteText (Join-Path $repo "global.json") '{ "sdk": { "version": "10.0.302" } }'
& git -C $repo add -A; & git -C $repo commit -q -m fix | Out-Null; $commit2=(& git -C $repo rev-parse HEAD).Trim()
$gj2=Sha256File (Join-Path $repo "global.json")
$imLines2=@("# test input manifest","$gj2  global.json","$dll  src/app.dll","ATAS.Indicators.dll  $ai","ATAS.DataFeedsCore.dll  $ad","OFT.Rendering.dll  $ao")
WriteText $imPath ($imLines2 -join "`n"); $preSha=Sha256Str ((($imLines2) -join "`n") + "`n"); $commit=$commit2
# dirty tree refused
WriteText (Join-Path $repo "junk.txt") "x"
Assert "dirty tree refused" ((Run (WriteM "ok3.json" @{}) $null @()) -eq 1)
Remove-Item (Join-Path $repo "junk.txt") -Force
# rollback WITH previous
$mOK3 = WriteM "ok4.json" @{}; Run $mOK3 $null @() | Out-Null; $hashV1=Sha256File $tgt
$env:GCAE_FORCE_VERIFY_FAIL="1"; $c10=Run $mOK3 $null @(); $env:GCAE_FORCE_VERIFY_FAIL=""
Assert "forced-fail returns non-zero" ($c10 -ne 0)
Assert "rollback restored previous" ((Sha256File $tgt) -eq $hashV1)
# rollback NO previous
$tgt2=Join-Path $work "fresh.dll"; $prev=$ErrorActionPreference; $ErrorActionPreference="SilentlyContinue"; $env:GCAE_FORCE_VERIFY_FAIL="1"; $ms=Sha256File $mOK3
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $deploy -ManifestPath $mOK3 -BuildManifestSha $ms -InputManifestPath $imPath -DllPath (Join-Path $repo "src\app.dll") -Target $tgt2 -DeployManifestOut $dman -RepoRoot $repo 2>$null 1>$null
$c11=$LASTEXITCODE; $env:GCAE_FORCE_VERIFY_FAIL=""; $ErrorActionPreference=$prev
Assert "no-previous forced-fail non-zero" ($c11 -ne 0)
Assert "no-previous rollback removed new target" (-not (Test-Path $tgt2))
# bypass params absent
$src = Get-Content $deploy -Raw
Assert "no -CommitOverride" (-not ($src -match '\[string\]\$CommitOverride'))
Assert "no -AllowDirty" (-not ($src -match '\[switch\]\$AllowDirty'))
Assert "no -ForceVerifyFail param" (-not ($src -match '\[switch\]\$ForceVerifyFail'))

$env:ATAS_HOME=""
$lines += ("-"*56); $lines += "passed=$pass failed=$fail"; $lines += ("RESULT: " + ($(if($fail -eq 0){"PASS"}else{"FAIL"})))
$out = (@("A_DEPLOY_TEST - deploy.ps1 provenance + env verification + rollback","-"*56)+$lines) -join "`n"
Write-Output $out
if($LogPath){ WriteText $LogPath $out }
Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
if($fail -ne 0){ exit 1 } else { exit 0 }
