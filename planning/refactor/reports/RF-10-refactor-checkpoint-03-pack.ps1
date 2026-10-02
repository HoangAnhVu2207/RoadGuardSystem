param([ValidateSet('Build', 'Verify')][string]$Mode = 'Build')

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$zipPath = Join-Path $PSScriptRoot 'RF-10-refactor-checkpoint-03-handoff.zip'
$hashPath = "$zipPath.sha256"
$baselineRoot = Join-Path $env:TEMP 'roadguard-rf1003-c01-baseline-20261001'

$changed = @(
    'planning/refactor/10-survey-coexistence-baseline.md',
    'planning/refactor/reports/RF-10-03-C01.md',
    'planning/refactor/10-refactor-checklist.md',
    'planning/refactor/10-refactor-slices.md',
    'planning/refactor/tasks/RF-10-03-survey-dataset.md',
    'planning/refactor/reports/RF-10-refactor.md'
)
$new = @(
    'tests/RoadGuardSystem.ApiTests/Surveys/Rf1003SameRowSurveyTests.cs',
    'planning/refactor/reports/RF-10-refactor-checkpoint-03-README.md',
    'planning/refactor/reports/RF-10-refactor-checkpoint-03-pack.ps1'
)
$context = @(
    'AGENTS.md',
    '.agents/manifest.json',
    'RoadGuardSystem.API/Controllers/SurveyPlanningController.cs',
    'RoadGuardSystem.API/Controllers/SurveyV2Controller.cs',
    'RoadGuardSystem.Services/Implementations/Surveys/SurveyPlanningService.cs',
    'RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs',
    'RoadGuardSystem.Repositories/Implementations/Surveys/SurveyPlanningPersistenceService.cs',
    'RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs',
    'RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs',
    'RoadGuardSystem.Repositories/Configurations/SurveyPlanConfiguration.cs',
    'RoadGuardSystem.BusinessObjects/Surveys/SurveyPlan.cs',
    'RoadGuardSystem.BusinessObjects/Surveys/SurveyPlanPostponement.cs',
    'RoadGuardSystem.BusinessObjects/Surveys/SurveyPlanScope.cs',
    'RoadGuardSystem.BusinessObjects/Idempotency/IdempotencyRecord.cs',
    'tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationSqlServerFixture.cs',
    'tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationWebApplicationFactory.cs',
    'tests/RoadGuardSystem.ApiTests/Surveys/P122SurveyPlanningTests.cs',
    'tests/RoadGuardSystem.ApiTests/Surveys/P2SurveyV2ApiTests.cs',
    'planning/refactor/02-contract-gaps.md',
    'planning/refactor/02-decision-register.md'
)
$expectedBaseline = @{
    'planning/refactor/10-survey-coexistence-baseline.md' = 'CD30C451A944009C0AE9A4B1049B796BC1175438F99E48A9C5E65128B33C15FE'
    'planning/refactor/reports/RF-10-03-C01.md' = '0CCFE439FAFF971CA738706D59F8C0D95B1C7C01A8416CD30525C5EFD675239C'
    'planning/refactor/10-refactor-checklist.md' = '561EC0B2043247BF7174A43FD396154013BD0B7FE14DC059B72E101B9FBFCADA'
    'planning/refactor/10-refactor-slices.md' = 'CFB7DF54A7633DC7990329555C4489410FD226487C144B2AF9A39D1F4A1E0CD2'
    'planning/refactor/tasks/RF-10-03-survey-dataset.md' = '896E155CA95F3C64B7A2614F6FF179800711A4FC755DAD056A9174187BF6D62C'
    'planning/refactor/reports/RF-10-refactor.md' = 'B0A34D09FB1AC2F8D4B724D65CE38E1601FB6F054FCC80327035C1A167FDE724'
}

function HashBytes([byte[]]$bytes) {
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function AddFile([System.Collections.IDictionary]$entries, [string]$name, [string]$source, [string]$role) {
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing source: $source" }
    $entries[$name] = @{ bytes = [IO.File]::ReadAllBytes($source); role = $role }
}

function TaskDiff {
    $empty = Join-Path $baselineRoot 'empty-before-new-files'
    if (-not (Test-Path -LiteralPath $empty)) { [IO.File]::WriteAllBytes($empty, [byte[]]@()) }
    $lines = [Collections.Generic.List[string]]::new()
    foreach ($path in $changed + $new) {
        $old = if ($changed -contains $path) { Join-Path $baselineRoot $path } else { $empty }
        $current = Join-Path $root $path
        $kind = if ($changed -contains $path) { 'pre-task dirty byte snapshot' } else { 'NOT_EXISTED' }
        $lines.Add("# TASK-SPECIFIC: $path; before=$kind")
        $raw = & git diff --no-index --binary -- $old $current 2>$null
        if ($LASTEXITCODE -notin @(0, 1)) { throw "Cannot diff $path" }
        foreach ($line in $raw) {
            if ($line.StartsWith('diff --git ')) { $lines.Add("diff --git a/$path b/$path") }
            elseif ($line.StartsWith('--- ')) {
                $lines.Add($(if ($changed -contains $path) { "--- a/$path" } else { '--- /dev/null' }))
            }
            elseif ($line.StartsWith('+++ ')) { $lines.Add("+++ b/$path") }
            else { $lines.Add($line) }
        }
    }
    [Text.Encoding]::UTF8.GetBytes(($lines -join "`n") + "`n")
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
        $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($file in $manifest.files) {
            if (-not $names.Add($file.path)) { throw "Duplicate manifest entry: $($file.path)" }
            $entry = $zip.GetEntry($file.path)
            if ($null -eq $entry) { throw "Missing ZIP entry: $($file.path)" }
            $stream = [IO.MemoryStream]::new()
            try { $entry.Open().CopyTo($stream); $bytes = $stream.ToArray() } finally { $stream.Dispose() }
            if ($bytes.Length -ne $file.size -or (HashBytes $bytes) -ne $file.sha256) { throw "ZIP mismatch: $($file.path)" }
            if ($file.path.StartsWith('current/') -or $file.path.StartsWith('context/')) {
                $relative = $file.path.Substring($file.path.IndexOf('/') + 1)
                if ((HashBytes ([IO.File]::ReadAllBytes((Join-Path $root $relative)))) -ne $file.sha256) { throw "Working tree drift: $relative" }
            }
            if ($file.path.StartsWith('baseline/')) {
                $relative = $file.path.Substring('baseline/'.Length)
                if ((HashBytes $bytes) -ne $expectedBaseline[$relative].ToLowerInvariant()) { throw "Baseline mismatch: $relative" }
            }
            if ($file.path -eq 'evidence/task-specific-diff.patch' -and (HashBytes (TaskDiff)) -ne $file.sha256) {
                throw 'Task diff no longer matches baseline/current bytes'
            }
            if ($file.path.StartsWith('evidence/') -and $file.path -ne 'evidence/task-specific-diff.patch') {
                $content = [Text.Encoding]::UTF8.GetString($bytes)
                if ($content -match '(?i)(?:password|pwd)\s*=\s*[^;\s"'']+' -or
                    $content -match '(?i)authorization:\s*bearer\s+[A-Za-z0-9._-]{20,}') {
                    throw "Potential credential in evidence: $($file.path)"
                }
            }
        }
        $hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
        if (Test-Path -LiteralPath $hashPath) {
            $sidecar = (Get-Content -LiteralPath $hashPath -Raw).Trim().Split(' ')[0]
            if ($sidecar -ne $hash) { throw 'External ZIP hash mismatch' }
        }
        Write-Output "PASS ZIP entries=$($manifest.files.Count)+manifest SHA256=$hash"
    }
    finally { $zip.Dispose() }
}

if ($Mode -eq 'Verify') { VerifyArchive; exit 0 }
if (Test-Path -LiteralPath $zipPath) { throw 'Refusing to overwrite existing checkpoint ZIP' }

$entries = [ordered]@{}
foreach ($path in $changed) {
    $baseline = Join-Path $baselineRoot $path
    if ((Get-FileHash -LiteralPath $baseline -Algorithm SHA256).Hash -ne $expectedBaseline[$path]) { throw "Baseline changed: $path" }
    AddFile $entries "baseline/$path" $baseline 'pre_task_dirty_bytes'
    AddFile $entries "current/$path" (Join-Path $root $path) 'task_changed_document'
}
foreach ($path in $new) { AddFile $entries "current/$path" (Join-Path $root $path) 'task_new_file' }
foreach ($path in $context) { AddFile $entries "context/$path" (Join-Path $root $path) 'read_only_source_context' }
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'RF-10-03-C01-evidence') -File | Sort-Object Name) {
    AddFile $entries "evidence/$($file.Name)" $file.FullName 'raw_run_evidence'
}
$entries['evidence/task-specific-diff.patch'] = @{ bytes = [byte[]](TaskDiff); role = 'task_specific_diff_not_apply_verified' }

$files = foreach ($name in $entries.Keys) {
    $entry = $entries[$name]
    [ordered]@{ path = $name; role = $entry.role; size = $entry.bytes.Length; sha256 = HashBytes $entry.bytes }
}
$manifest = [ordered]@{
    branch = 'anh'
    localHead = '2efc8a5775f834c7f0fe37cc0ce703011649e1f1'
    baseline = 'pre-task dirty byte snapshots, not HEAD'
    patchApplicability = 'NOT_VERIFIED'
    files = @($files)
}
$manifestBytes = [Text.Encoding]::UTF8.GetBytes(($manifest | ConvertTo-Json -Depth 6) + "`n")
$zip = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($name in $entries.Keys) {
        $entry = $zip.CreateEntry($name, [IO.Compression.CompressionLevel]::Optimal)
        $stream = $entry.Open()
        try { $stream.Write($entries[$name].bytes) } finally { $stream.Dispose() }
    }
    $entry = $zip.CreateEntry('manifest.json', [IO.Compression.CompressionLevel]::Optimal)
    $stream = $entry.Open()
    try { $stream.Write($manifestBytes) } finally { $stream.Dispose() }
}
finally { $zip.Dispose() }
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($hashPath, "$hash  RF-10-refactor-checkpoint-03-handoff.zip`n", [Text.Encoding]::UTF8)
VerifyArchive
