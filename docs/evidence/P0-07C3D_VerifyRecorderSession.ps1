# P0-07C3D post-session verifier (evidence tooling only — not a product feature).
# Validates session layout, SHA sidecars, manifest hash, and counter equations from manifest.json.
# Full GCAR frame CRC / RecoveryScanner validation is performed in the C# test/harness pass after live capture.
#
# Usage:
#   powershell -NoProfile -File docs\evidence\P0-07C3D_VerifyRecorderSession.ps1 -SessionId <32-hex-or-guid>

param(
    [Parameter(Mandatory = $true)]
    [string]$SessionId,
    [string]$UserProfile = $env:USERPROFILE
)

$ErrorActionPreference = "Stop"

function Normalize-SessionId([string]$id) {
    $t = $id.Trim().Replace("-", "")
    if ($t.Length -ne 32) { throw "SessionId must be Guid or 32-hex N format. Got: $id" }
    return [Guid]::ParseExact($t, "N")
}

function Assert-Eq([string]$name, $left, $right) {
    if ([long]$left -ne [long]$right) {
        throw ("ACCOUNTING_FAIL {0}: {1} != {2}" -f $name, $left, $right)
    }
    Write-Output ("ACCOUNTING_OK {0}: {1}" -f $name, $left)
}

$guid = Normalize-SessionId $SessionId
$sessionDir = Join-Path $UserProfile (".gcae\recorder\sessions\{0}" -f $guid.ToString("N"))
$segmentsDir = Join-Path $sessionDir "segments"
$manifestPath = Join-Path $sessionDir "manifest.json"
$manifestHashPath = Join-Path $sessionDir "manifest.json.sha256"

Write-Output "=== P0-07C3D VerifyRecorderSession ==="
Write-Output ("SessionId={0}" -f $guid)
Write-Output ("SessionDir={0}" -f $sessionDir)

if (-not (Test-Path -LiteralPath $sessionDir)) { throw "Session directory missing: $sessionDir" }
if (-not (Test-Path -LiteralPath $manifestPath)) { throw "manifest.json missing" }
if (-not (Test-Path -LiteralPath $manifestHashPath)) { throw "manifest.json.sha256 missing" }

$tmpLeft = @(Get-ChildItem -LiteralPath $segmentsDir -Filter "*.seg.tmp" -ErrorAction SilentlyContinue)
if ($tmpLeft.Count -gt 0) {
    Write-Output "FAIL: temporary segment(s) remain:"
    $tmpLeft | ForEach-Object { Write-Output $_.FullName }
    throw "Temporary segments remain after claimed clean shutdown"
}

$segFiles = @(Get-ChildItem -LiteralPath $segmentsDir -Filter "*.seg" -ErrorAction SilentlyContinue)
if ($segFiles.Count -eq 0) { throw "No completed .seg files found" }

foreach ($seg in $segFiles) {
    $shaPath = $seg.FullName + ".sha256"
    if (-not (Test-Path -LiteralPath $shaPath)) { throw "Missing SHA sidecar: $shaPath" }
    $expected = ((Get-Content -LiteralPath $shaPath -Raw).Trim() -split "\s+")[0].ToUpperInvariant()
    $actual = (Get-FileHash -LiteralPath $seg.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($expected -ne $actual) {
        throw ("SHA mismatch for {0}: sidecar={1} actual={2}" -f $seg.Name, $expected, $actual)
    }

    # GCAR magic + ContainerVersion (UInt16 LE @4) + ContainerFlags (UInt16 LE @6)
    $fs = [IO.File]::OpenRead($seg.FullName)
    try {
        $hdr = New-Object byte[] 8
        if ($fs.Read($hdr, 0, 8) -ne 8) { throw "Segment too short for GCAR header: $($seg.Name)" }
    }
    finally { $fs.Dispose() }
    $magic = [Text.Encoding]::ASCII.GetString($hdr, 0, 4)
    $ver = [BitConverter]::ToUInt16($hdr, 4)
    $flags = [BitConverter]::ToUInt16($hdr, 6)
    if ($magic -ne "GCAR") { throw ("Bad container magic {0} in {1}" -f $magic, $seg.Name) }
    if ($ver -ne 1) { throw ("Expected container version 1, got {0} in {1}" -f $ver, $seg.Name) }

    Write-Output ("SEG_OK {0} SHA256={1} BYTES={2} GCAR ContainerVersion={3} Flags={4}" -f $seg.Name, $actual, $seg.Length, $ver, $flags)
}

$manifestActual = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToUpperInvariant()
$manifestExpected = ((Get-Content -LiteralPath $manifestHashPath -Raw).Trim() -split "\s+")[0].ToUpperInvariant()
if ($manifestActual -ne $manifestExpected) {
    throw ("Manifest SHA mismatch: sidecar={0} actual={1}" -f $manifestExpected, $manifestActual)
}
Write-Output ("MANIFEST_OK SHA256={0}" -f $manifestActual)

$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
Write-Output ("RecorderSchemaVersion={0}" -f $manifest.recorderSchemaVersion)
Write-Output ("ContainerVersion={0}" -f $manifest.containerVersion)
Write-Output ("AbnormalTermination={0}" -f $manifest.abnormalTermination)

if ($manifest.recorderSchemaVersion -ne "1.2.0") {
    throw ("Expected recorderSchemaVersion 1.2.0, got {0}" -f $manifest.recorderSchemaVersion)
}
if ([int]$manifest.containerVersion -ne 1) {
    throw ("Expected containerVersion 1, got {0}" -f $manifest.containerVersion)
}

# Per-segment category sum from manifest segment index (schema 1.2.0 claims)
if ($null -ne $manifest.segments) {
    foreach ($s in $manifest.segments) {
        if ($null -ne $s.categoryCountsKnown -and -not [bool]$s.categoryCountsKnown) { continue }
        $sum = [long]$s.marketEventRecordCount + [long]$s.invocationResultRecordCount + [long]$s.lifecycleIntegrityRecordCount
        Assert-Eq ("ManifestSegmentCategorySum {0}" -f $s.fileName) ([long]$s.recordCount) $sum
    }
}

$c = $manifest.counters
if ($null -eq $c) { throw "manifest.counters missing" }

Assert-Eq "WriterDequeuedTotal" ([long]$c.writerDequeuedTotal) (
    [long]$c.marketEventsWritten + [long]$c.invocationResultsWritten + [long]$c.lifecycleIntegrityRecordsWritten +
    [long]$c.serializationFailures + [long]$c.writerDiscardedAfterFatalFault)

Assert-Eq "InvocationResultAttempts" ([long]$c.invocationResultEmissionAttempts) (
    [long]$c.invocationResultAcceptedToQueue + [long]$c.invocationResultQueueFullDrops + [long]$c.invocationResultFaults)

Assert-Eq "Normalized==Accepted+Drops" ([long]$c.normalizedObservations) ([long]$c.acceptedToQueue + [long]$c.queueFullDrops)
Assert-Eq "Accepted==Dequeued+Undrained" ([long]$c.acceptedToQueue) ([long]$c.writerDequeued + [long]$c.undrainedAtShutdown)
Assert-Eq "RecordsWritten==Market+Lifecycle" ([long]$c.recordsWritten) ([long]$c.marketEventsWritten + [long]$c.lifecycleIntegrityRecordsWritten)

if (-not [bool]$manifest.abnormalTermination) {
    Assert-Eq "CleanShutdown InvocationResultsWritten" ([long]$c.invocationResultsWritten) ([long]$c.invocationResultAcceptedToQueue)
    Assert-Eq "CleanShutdown RecordsWritten==AcceptedToQueue" ([long]$c.recordsWritten) ([long]$c.acceptedToQueue)
    if ([long]$c.writerDiscardedAfterFatalFault -ne 0) { throw "CleanShutdown WriterDiscardedAfterFatalFault != 0" }
    if ([long]$c.undrainedAtShutdown -ne 0) { throw "CleanShutdown UndrainedAtShutdown != 0" }
}

Write-Output "NOTE: Full per-frame CRC32C / RecoveryScanner scan still required in agent C# pass after live capture."
Write-Output "VERDICT=PASS_CORE_ARTIFACT_ACCOUNTING"
