param([ValidateSet('Build', 'Verify')][string]$Mode = 'Build')

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$zipPath = Join-Path $PSScriptRoot 'RF-10-refactor-checkpoint-05-handoff.zip'
$hashPath = "$zipPath.sha256"
$baselineRoot = Join-Path $env:TEMP 'roadguard-rf1002-c01-baseline-20261001'
$changed = @(
    'planning/refactor/10-identity-characterization-baseline.md',
    'planning/refactor/10-refactor-checklist.md',
    'planning/refactor/10-refactor-slices.md',
    'planning/refactor/tasks/RF-10-02-project-road-scope.md',
    'planning/refactor/reports/RF-10-refactor.md'
)
$new = @(
    'planning/refactor/10-project-gis-characterization-baseline.md',
    'planning/refactor/reports/RF-10-02-C01.md',
    'planning/refactor/reports/RF-10-refactor-checkpoint-05-README.md',
    'planning/refactor/reports/RF-10-refactor-checkpoint-05-pack.ps1',
    'tests/RoadGuardSystem.ApiTests/Projects/Rf1002ProjectGisCharacterizationTests.cs'
)
$context = @(
    'AGENTS.md', '.agents/manifest.json',
    'docs/product/confirmed-decisions.md',
    'planning/refactor/tasks/RF-06-isolated-characterization.md',
    'planning/refactor/tasks/RF-06A-current-erd-data-dictionary.md',
    'planning/refactor/tasks/RF-07-work-package-pilot.md',
    'planning/refactor/tasks/RF-08-shared-seams.md',
    'planning/refactor/tasks/RF-09-transition-mechanics.md',
    'RoadGuardSystem.API/Controllers/ProjectsController.cs',
    'RoadGuardSystem.API/Controllers/ProjectRoadSectionsController.cs',
    'RoadGuardSystem.API/Controllers/ProjectWarrantiesController.cs',
    'RoadGuardSystem.API/Authorization/ProjectAccessAuthorization.cs',
    'RoadGuardSystem.Services/Implementations/Authorization/ProjectScopeGuard.cs',
    'RoadGuardSystem.Services/Implementations/Projects/RoadSectionVersionService.cs',
    'RoadGuardSystem.Services/Implementations/Warranties/WarrantyCreationService.cs',
    'RoadGuardSystem.Repositories/Implementations/Projects/RoadSectionVersionPersistenceService.cs',
    'RoadGuardSystem.Repositories/Implementations/Warranties/WarrantyPersistenceService.cs',
    'RoadGuardSystem.BusinessObjects/Projects/ProjectMember.cs',
    'RoadGuardSystem.BusinessObjects/Projects/RoadSectionVersion.cs',
    'RoadGuardSystem.BusinessObjects/Warranties/Warranty.cs',
    'tests/RoadGuardSystem.ApiTests/Authorization/P112ProjectAuthorizationTests.cs',
    'tests/RoadGuardSystem.ApiTests/Warranties/P120WarrantyCreationTests.cs',
    'tests/RoadGuardSystem.ApiTests/Projects/P121RoadSectionVersionTests.cs',
    'tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationSqlServerFixture.cs',
    'tests/RoadGuardSystem.IntegrationTests/Projects/P211ProjectMembershipReadModelTests.cs',
    'tests/RoadGuardSystem.IntegrationTests/Infrastructure/ProjectMembershipSqlFixture.cs'
)

$expectedBaseline = @{
    'planning/refactor/10-identity-characterization-baseline.md' = '7469ECB8840833954BC4C2C0B5304A2DD181E5E800E61986DD83243FB1979A23'
    'planning/refactor/10-refactor-checklist.md' = '2D6D5763B8CE0D12C4612190C8AF9E4A3C1FE77E5BFA04A86D3C9F8729EB0AB1'
    'planning/refactor/10-refactor-slices.md' = 'ECAC6AE92CA6F8E08F767F50A607AEE595D61232D306A0B6DDF071E07DEE01E3'
    'planning/refactor/tasks/RF-10-02-project-road-scope.md' = '856C4ABCAC37AD652271540B89BF2AB6A6F2C627A6A56765BB53E94F72E19A31'
    'planning/refactor/reports/RF-10-refactor.md' = 'E02F9D89C0BB0C5243841CE927423213BCB08A3B0E428062E66E4FE969978CC8'
}

function HashBytes([byte[]]$bytes) { [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant() }
function SnapshotPath([string]$path) { Join-Path $baselineRoot ($path -replace '[\\/]', '__') }
function AddFile([System.Collections.IDictionary]$entries, [string]$name, [string]$source, [string]$role) {
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing source: $source" }
    $entries[$name] = @{ bytes = [IO.File]::ReadAllBytes($source); role = $role }
}
function TaskDiff {
    $empty = Join-Path $baselineRoot 'empty-before-new-files'
    if (-not (Test-Path -LiteralPath $empty)) { [IO.File]::WriteAllBytes($empty, [byte[]]@()) }
    $lines = [Collections.Generic.List[string]]::new()
    foreach ($path in $changed + $new) {
        $old = if ($changed -contains $path) { SnapshotPath $path } else { $empty }
        $current = Join-Path $root $path
        $kind = if ($changed -contains $path) { 'pre-task dirty byte snapshot' } else { 'NOT_EXISTED' }
        $lines.Add("# TASK-SPECIFIC: $path; before=$kind")
        $raw = & git diff --no-index --binary -- $old $current 2>$null
        if ($LASTEXITCODE -notin @(0, 1)) { throw "Cannot diff $path" }
        foreach ($line in $raw) {
            if ($line.StartsWith('diff --git ')) { $lines.Add("diff --git a/$path b/$path") }
            elseif ($line.StartsWith('--- ')) { $lines.Add($(if ($changed -contains $path) { "--- a/$path" } else { '--- /dev/null' })) }
            elseif ($line.StartsWith('+++ ')) { $lines.Add("+++ b/$path") }
            else { $lines.Add($line) }
        }
    }
    [Text.Encoding]::UTF8.GetBytes(($lines -join "`n") + "`n")
}
function CollectEntries {
    $entries = [ordered]@{}
    foreach ($path in $changed) {
        $baseline = SnapshotPath $path
        $bytes = [IO.File]::ReadAllBytes($baseline)
        if ((HashBytes $bytes) -ne $expectedBaseline[$path].ToLowerInvariant()) { throw "Baseline drift: $path" }
        AddFile $entries "baseline/$path" $baseline 'pre-task dirty bytes'
        AddFile $entries "current/$path" (Join-Path $root $path) 'changed document'
    }
    foreach ($path in $new) { AddFile $entries "current/$path" (Join-Path $root $path) 'new task file' }
    foreach ($path in $context) { AddFile $entries "context/$path" (Join-Path $root $path) 'read-only source context' }
    $evidenceRoot = Join-Path $root 'planning/refactor/reports/RF-10-02-C01-evidence'
    foreach ($file in Get-ChildItem -LiteralPath $evidenceRoot -File | Sort-Object Name) { AddFile $entries "evidence/$($file.Name)" $file.FullName 'raw run evidence' }
    $entries['evidence/task-specific-diff.patch'] = @{ bytes = [byte[]](TaskDiff); role = 'generated repository-relative diff' }
    $entries
}
function VerifyArchive([System.Collections.IDictionary]$expected) {
    if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) { throw "Missing ZIP: $zipPath" }
    $zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $manifestEntry = $zip.GetEntry('manifest.json'); if ($null -eq $manifestEntry) { throw 'Missing manifest.json' }
        $reader = [IO.StreamReader]::new($manifestEntry.Open()); try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
        if ($zip.Entries.Count -ne $manifest.files.Count + 1) { throw 'ZIP entry count differs from manifest' }
        $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($file in $manifest.files) {
            if (-not $seen.Add($file.path)) { throw "Duplicate manifest path: $($file.path)" }
            $entry = $zip.GetEntry($file.path); if ($null -eq $entry) { throw "Missing entry: $($file.path)" }
            $stream = [IO.MemoryStream]::new(); try { $source = $entry.Open(); try { $source.CopyTo($stream) } finally { $source.Dispose() }; $bytes = $stream.ToArray() } finally { $stream.Dispose() }
            if ($bytes.Length -ne $file.size -or (HashBytes $bytes) -ne $file.sha256) { throw "Corrupt entry: $($file.path)" }
            if (-not $expected.Contains($file.path) -or (HashBytes $expected[$file.path].bytes) -ne $file.sha256) { throw "Archive source drift: $($file.path)" }
        }
        if ($seen.Count -ne $expected.Count) { throw 'Manifest omits expected entry' }
    } finally { $zip.Dispose() }
    $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath).Hash.ToLowerInvariant()
    $sidecar = (Get-Content -LiteralPath $hashPath -Raw).Trim().Split(' ')[0].ToLowerInvariant()
    if ($actualHash -ne $sidecar) { throw 'External ZIP SHA-256 mismatch' }
    "PASS entries=$($expected.Count) zipSha256=$actualHash"
}
if ((& git -C $root rev-parse HEAD).Trim() -ne '2efc8a5775f834c7f0fe37cc0ce703011649e1f1') { throw 'HEAD differs from surveyed local commit' }
$entries = CollectEntries
if ($Mode -eq 'Build') {
    if (Test-Path -LiteralPath $zipPath) { throw 'Checkpoint 05 ZIP already exists; verify before rebuild' }
    $manifest = @{ branch = 'anh'; localHead = '2efc8a5775f834c7f0fe37cc0ce703011649e1f1'; dirtyBaseline = 'five byte snapshots; new files NOT_EXISTED'; patchApplicability = 'NOT_VERIFIED'; files = @($entries.Keys | Sort-Object | ForEach-Object { @{ path = $_; role = $entries[$_].role; size = $entries[$_].bytes.Length; sha256 = HashBytes $entries[$_].bytes } }) }
    $zip = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
    try { foreach ($name in $entries.Keys) { $entry = $zip.CreateEntry($name, [IO.Compression.CompressionLevel]::Optimal); $stream = $entry.Open(); try { $stream.Write($entries[$name].bytes) } finally { $stream.Dispose() } }; $json = [Text.Encoding]::UTF8.GetBytes(($manifest | ConvertTo-Json -Depth 8)); $entry = $zip.CreateEntry('manifest.json', [IO.Compression.CompressionLevel]::Optimal); $stream = $entry.Open(); try { $stream.Write($json) } finally { $stream.Dispose() } } finally { $zip.Dispose() }
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath).Hash.ToLowerInvariant(); "$hash  RF-10-refactor-checkpoint-05-handoff.zip" | Set-Content -LiteralPath $hashPath -NoNewline
}
VerifyArchive $entries
