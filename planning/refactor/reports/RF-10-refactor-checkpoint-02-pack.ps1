param([ValidateSet('Build', 'Verify')][string]$Mode = 'Build')

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$zipPath = Join-Path $PSScriptRoot 'RF-10-refactor-checkpoint-02-handoff.zip'
$hashPath = "$zipPath.sha256"
$baselineRoot = Join-Path $env:TEMP 'roadguard-rf1005-c01-baseline-20261001'

$changed = @(
    'planning/refactor/10-survey-coexistence-baseline.md',
    'planning/refactor/reports/RF-10-03-C01.md',
    'planning/refactor/10-refactor-checklist.md',
    'planning/refactor/10-refactor-slices.md',
    'planning/refactor/reports/RF-10-refactor.md',
    'planning/refactor/tasks/RF-10-05-processing-ai.md'
)
$new = @(
    'tests/RoadGuardSystem.IntegrationTests/Processing/Rf1005CallbackCharacterizationTests.cs',
    'planning/refactor/reports/RF-10-05-C01.md',
    'planning/refactor/reports/RF-10-refactor-checkpoint-02-README.md',
    'planning/refactor/reports/RF-10-refactor-checkpoint-02-pack.ps1'
)
$context = @(
    'RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs',
    'RoadGuardSystem.Services/Implementations/Processing/ProcessingV2Service.cs',
    'RoadGuardSystem.Repositories/Implementations/Processing/ProcessingV2PersistenceService.cs',
    'RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs',
    'RoadGuardSystem.Repositories/Interfaces/Processing/ProcessingAiResultRequest.cs',
    'RoadGuardSystem.Repositories/Interfaces/Processing/ProcessingJobCreateRequest.cs',
    'RoadGuardSystem.Repositories/Interfaces/Processing/ProcessingJobRetryRequest.cs',
    'RoadGuardSystem.Repositories/Interfaces/Processing/ProcessingJobPersistenceStatus.cs',
    'RoadGuardSystem.Repositories/RoadGuardDbContext.cs',
    'RoadGuardSystem.BusinessObjects/Processing/ProcessingJob.cs',
    'RoadGuardSystem.BusinessObjects/Processing/ProcessingAttempt.cs',
    'RoadGuardSystem.BusinessObjects/Processing/ProcessingBlock.cs',
    'RoadGuardSystem.BusinessObjects/Processing/AIModelVersion.cs',
    'RoadGuardSystem.BusinessObjects/Processing/AIDetection.cs',
    'RoadGuardSystem.BusinessObjects/Idempotency/IdempotencyRecord.cs',
    'RoadGuardSystem.BusinessObjects/Auditing/AuditLog.cs',
    'RoadGuardSystem.BusinessObjects/Messaging/OutboxMessage.cs',
    'tests/RoadGuardSystem.IntegrationTests/Processing/P231ProcessingPersistenceTests.cs',
    'tests/RoadGuardSystem.IntegrationTests/Infrastructure/IdentitySqlServerFixture.cs',
    'tests/RoadGuardSystem.IntegrationTests/Infrastructure/SqlServerTestFixture.cs',
    'planning/refactor/02-contract-gaps.md',
    'docs/product/confirmed-decisions.md'
)
$expectedBaseline = @{
    'planning/refactor/10-survey-coexistence-baseline.md' = '8D34946604944310DCD8EB0B553E31384D6827B2303BDA54F287329C375CC481'
    'planning/refactor/reports/RF-10-03-C01.md' = 'C2864CE267EC54F7D989AE6EB916F825BC4296DF7DF4B1928A28986FE4538BC5'
    'planning/refactor/10-refactor-checklist.md' = '27005EE6709DF0D38ADA33DCEA9152AA2053E16C8F5681C3FDB989669A197807'
    'planning/refactor/10-refactor-slices.md' = '5D0903115BA1746C0700643A04A766BC77283C9D52093A2529CAFF699CF15DAB'
    'planning/refactor/reports/RF-10-refactor.md' = 'D7A5215143213AE8B91085B88C88D8242041FBD7435FDA45423832AFC6363593'
    'planning/refactor/tasks/RF-10-05-processing-ai.md' = 'B2BD28ED91CEB36A784AA64DF554AFBC7C8AB201167E6798EA3C80FEEFA8C542'
}

function HashBytes([byte[]]$bytes) {
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function AddFile([System.Collections.IDictionary]$entries, [string]$zipName, [string]$source, [string]$role) {
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing package source: $source" }
    $entries[$zipName] = @{ bytes = [IO.File]::ReadAllBytes($source); role = $role; source = $source }
}

function VerifyArchive {
    if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) { throw "Missing ZIP: $zipPath" }
    $zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $manifestEntry = $zip.GetEntry('manifest.json')
        if ($null -eq $manifestEntry) { throw 'Missing manifest.json' }
        $reader = [IO.StreamReader]::new($manifestEntry.Open())
        try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
        if ($zip.Entries.Count -ne $manifest.files.Count + 1) { throw 'ZIP entry count differs from manifest' }
        foreach ($file in $manifest.files) {
            $entry = $zip.GetEntry($file.path)
            if ($null -eq $entry) { throw "Missing ZIP entry: $($file.path)" }
            $stream = [IO.MemoryStream]::new()
            try { $entry.Open().CopyTo($stream); $bytes = $stream.ToArray() } finally { $stream.Dispose() }
            if ($bytes.Length -ne $file.size -or (HashBytes $bytes) -ne $file.sha256) {
                throw "ZIP content mismatch: $($file.path)"
            }
            if ($file.path.StartsWith('current/') -or $file.path.StartsWith('context/')) {
                $relative = $file.path.Substring($file.path.IndexOf('/') + 1)
                $source = Join-Path $root $relative
                if ((HashBytes ([IO.File]::ReadAllBytes($source))) -ne $file.sha256) {
                    throw "Working tree drift: $relative"
                }
            }
            if ($file.path.StartsWith('baseline/')) {
                $relative = $file.path.Substring('baseline/'.Length)
                if ((HashBytes $bytes) -ne $expectedBaseline[$relative].ToLowerInvariant()) {
                    throw "Baseline hash mismatch: $relative"
                }
            }
            if ($file.path.StartsWith('evidence/') -and $file.path -ne 'evidence/task-specific-diff.patch') {
                $content = [Text.Encoding]::UTF8.GetString($bytes)
                if ($content -match '(?i)(?:password|pwd)\s*=\s*[^;\s"'']+' -or
                    $content -match '(?i)authorization:\s*bearer\s+[A-Za-z0-9._-]{20,}') {
                    throw "Potential credential in evidence: $($file.path)"
                }
            }
        }
        $zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
        if (Test-Path -LiteralPath $hashPath) {
            $sidecar = (Get-Content -LiteralPath $hashPath -Raw).Trim().Split(' ')[0]
            if ($sidecar -ne $zipHash) { throw 'External ZIP hash sidecar mismatch' }
        }
        Write-Output "PASS ZIP entries=$($manifest.files.Count)+manifest SHA256=$zipHash"
    }
    finally { $zip.Dispose() }
}

if ($Mode -eq 'Verify') { VerifyArchive; exit 0 }
if (Test-Path -LiteralPath $zipPath) { throw 'Refusing to overwrite an existing checkpoint ZIP' }

$entries = [ordered]@{}
foreach ($path in $changed) {
    $baseline = Join-Path $baselineRoot $path
    if ((Get-FileHash -LiteralPath $baseline -Algorithm SHA256).Hash -ne $expectedBaseline[$path]) {
        throw "Baseline bytes changed: $path"
    }
    AddFile $entries "baseline/$path" $baseline 'pre_task_dirty_bytes'
    AddFile $entries "current/$path" (Join-Path $root $path) 'task_changed_document'
}
foreach ($path in $new) { AddFile $entries "current/$path" (Join-Path $root $path) 'task_new_file' }
foreach ($path in $context) { AddFile $entries "context/$path" (Join-Path $root $path) 'read_only_source_context' }

$evidenceRoot = Join-Path $PSScriptRoot 'RF-10-05-C01-evidence'
foreach ($file in Get-ChildItem -LiteralPath $evidenceRoot -File | Sort-Object Name) {
    AddFile $entries "evidence/$($file.Name)" $file.FullName 'raw_run_evidence'
}

$empty = Join-Path $baselineRoot 'empty-before-new-files'
[IO.File]::WriteAllBytes($empty, [byte[]]@())
$diffLines = [Collections.Generic.List[string]]::new()
foreach ($path in $changed + $new) {
    $before = if ($changed -contains $path) { Join-Path $baselineRoot $path } else { $empty }
    $after = Join-Path $root $path
    $beforeLabel = if ($changed -contains $path) { 'pre-task dirty byte snapshot' } else { 'NOT_EXISTED' }
    $diffLines.Add("# TASK-SPECIFIC: $path; before=$beforeLabel")
    $diff = & git diff --no-index --binary -- $before $after 2>$null
    if ($LASTEXITCODE -notin @(0, 1)) { throw "Cannot generate diff for $path" }
    foreach ($line in $diff) { $diffLines.Add($line) }
}
$diffBytes = [Text.Encoding]::UTF8.GetBytes(($diffLines -join "`n") + "`n")
$entries['evidence/task-specific-diff.patch'] = @{ bytes = $diffBytes; role = 'task_specific_diff_not_apply_verified'; source = 'generated from byte snapshots' }

$manifestFiles = foreach ($key in $entries.Keys) {
    $value = $entries[$key]
    [ordered]@{ path = $key; role = $value.role; size = $value.bytes.Length; sha256 = HashBytes $value.bytes }
}
$manifest = [ordered]@{
    branch = 'anh'
    localHead = '2efc8a5775f834c7f0fe37cc0ce703011649e1f1'
    baseline = 'pre-task dirty byte snapshots, not HEAD'
    patchApplicability = 'NOT_VERIFIED'
    files = @($manifestFiles)
}
$manifestBytes = [Text.Encoding]::UTF8.GetBytes(($manifest | ConvertTo-Json -Depth 6) + "`n")
$zip = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($key in $entries.Keys) {
        $entry = $zip.CreateEntry($key, [IO.Compression.CompressionLevel]::Optimal)
        $stream = $entry.Open()
        try { $stream.Write($entries[$key].bytes) } finally { $stream.Dispose() }
    }
    $entry = $zip.CreateEntry('manifest.json', [IO.Compression.CompressionLevel]::Optimal)
    $stream = $entry.Open()
    try { $stream.Write($manifestBytes) } finally { $stream.Dispose() }
}
finally { $zip.Dispose() }
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($hashPath, "$zipHash  RF-10-refactor-checkpoint-02-handoff.zip`n", [Text.Encoding]::UTF8)
VerifyArchive
