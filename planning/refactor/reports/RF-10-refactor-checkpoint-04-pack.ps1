param([ValidateSet('Build', 'Verify')][string]$Mode = 'Build')

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$zipPath = Join-Path $PSScriptRoot 'RF-10-refactor-checkpoint-04-handoff.zip'
$hashPath = "$zipPath.sha256"
$baselineRoot = Join-Path $env:TEMP 'roadguard-rf1001-c01-baseline-20261001'
$changed = @(
    'planning/refactor/10-refactor-checklist.md',
    'planning/refactor/10-refactor-slices.md',
    'planning/refactor/tasks/RF-10-01-identity-access.md',
    'planning/refactor/reports/RF-10-refactor.md'
)
$new = @(
    'planning/refactor/10-identity-characterization-baseline.md',
    'planning/refactor/reports/RF-10-01-C01.md',
    'planning/refactor/reports/RF-10-refactor-checkpoint-03-correction.md',
    'tests/RoadGuardSystem.ApiTests/Identity/Rf1001IdentityCharacterizationTests.cs',
    'planning/refactor/reports/RF-10-refactor-checkpoint-04-README.md',
    'planning/refactor/reports/RF-10-refactor-checkpoint-04-pack.ps1'
)
$context = @(
    'AGENTS.md', '.agents/manifest.json',
    'docs/product/confirmed-decisions.md', 'planning/refactor/02-contract-gaps.md',
    'planning/refactor/02-decision-register.md', 'planning/refactor/09-consumer-registry.md',
    'planning/refactor/reports/RF-10-03-C01.md',
    'planning/refactor/reports/RF-10-refactor-checkpoint-03-README.md',
    'planning/refactor/reports/RF-10-03-C01-evidence/test-final3-run.txt',
    'RoadGuardSystem.API/Controllers/AuthController.cs',
    'RoadGuardSystem.API/Controllers/ProfileController.cs',
    'RoadGuardSystem.API/Controllers/MeController.cs',
    'RoadGuardSystem.API/Controllers/UsersController.cs',
    'RoadGuardSystem.API/Controllers/AdminUsersController.cs',
    'RoadGuardSystem.API/Controllers/ReporterRegistrationsController.cs',
    'RoadGuardSystem.API/Authentication/JwtBearerConfiguration.cs',
    'RoadGuardSystem.Services/Implementations/Identity/IdentityService.cs',
    'RoadGuardSystem.Services/Implementations/Identity/IdentityV2Service.cs',
    'RoadGuardSystem.Services/Implementations/Authentication/AuthService.cs',
    'RoadGuardSystem.Services/Implementations/Authentication/IdentityOnboardingService.cs',
    'RoadGuardSystem.Services/Options/IdentityOnboardingOptions.cs',
    'RoadGuardSystem.Services/Options/JwtOptions.cs',
    'RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.Profiles.cs',
    'RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.PasswordResets.cs',
    'RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.RefreshTokens.cs',
    'RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.SessionIssuance.cs',
    'RoadGuardSystem.Repositories/Implementations/Identity/IdentityOnboardingRepository.cs',
    'RoadGuardSystem.DTOs/Identity/ProfileResponseDto.cs',
    'RoadGuardSystem.DTOs/Identity/ProfileUpdateRequestDto.cs',
    'RoadGuardSystem.DTOs/Identity/AdminPasswordResetRequestDto.cs',
    'RoadGuardSystem.DTOs/Identity/AdminResetPasswordV2RequestDto.cs',
    'tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationSqlServerFixture.cs',
    'tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationWebApplicationFactory.cs',
    'tests/RoadGuardSystem.ApiTests/Authentication/AuthenticationFlowTests.cs',
    'tests/RoadGuardSystem.ApiTests/Authentication/AuthenticationSessionFlowTests.cs',
    'tests/RoadGuardSystem.ApiTests/Authentication/V2IdentityOnboardingFlowTests.cs',
    'tests/RoadGuardSystem.ApiTests/Identity/P111ProfileReadTests.cs',
    'tests/RoadGuardSystem.ApiTests/Identity/P111ProfileUpdateTests.cs',
    'tests/RoadGuardSystem.ApiTests/Identity/P111PasswordResetTests.cs',
    'tests/RoadGuardSystem.ApiTests/Identity/V2AccountFlowTests.cs'
)
$expectedBaseline = @{
    'planning/refactor/10-refactor-checklist.md' = '393EEA58E4A576DDA8AE61B0086072F9BAFB0500EA072A8452BB13E22AB3F027'
    'planning/refactor/10-refactor-slices.md' = 'CB84476FFFC5C4FE4D15470FBABC65021A1E0A93A156169E3552FD047B81D8AF'
    'planning/refactor/tasks/RF-10-01-identity-access.md' = '08D31C88F56D5085525BE970D72C4EB670C8F296EC232A8DA941B4529E77E3FA'
    'planning/refactor/reports/RF-10-refactor.md' = 'B65EC762FCFC4D323AF62B1B7A902C91676D8F7DE3D4507CFD7548F3E4CEB920'
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
        $baseline = Join-Path $baselineRoot $path
        $bytes = [IO.File]::ReadAllBytes($baseline)
        if ((HashBytes $bytes) -ne $expectedBaseline[$path].ToLowerInvariant()) { throw "Baseline drift: $path" }
        AddFile $entries "baseline/$path" $baseline 'pre-task dirty bytes'
        AddFile $entries "current/$path" (Join-Path $root $path) 'changed document'
    }
    foreach ($path in $new) { AddFile $entries "current/$path" (Join-Path $root $path) 'new task file' }
    foreach ($path in $context) { AddFile $entries "context/$path" (Join-Path $root $path) 'read-only source context' }
    $evidenceRoot = Join-Path $root 'planning/refactor/reports/RF-10-01-C01-evidence'
    foreach ($file in Get-ChildItem -LiteralPath $evidenceRoot -File | Sort-Object Name) {
        AddFile $entries "evidence/$($file.Name)" $file.FullName 'raw run evidence'
    }
    $entries['evidence/task-specific-diff.patch'] = @{ bytes = [byte[]](TaskDiff); role = 'generated repository-relative diff' }
    return $entries
}

function VerifyArchive([System.Collections.IDictionary]$expected) {
    if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) { throw "Missing ZIP: $zipPath" }
    $zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $manifestEntry = $zip.GetEntry('manifest.json')
        if ($null -eq $manifestEntry) { throw 'Missing manifest.json' }
        $reader = [IO.StreamReader]::new($manifestEntry.Open())
        try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
        if ($zip.Entries.Count -ne $manifest.files.Count + 1) { throw 'ZIP entry count differs from manifest' }
        $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($file in $manifest.files) {
            if (-not $seen.Add($file.path)) { throw "Duplicate manifest path: $($file.path)" }
            $entry = $zip.GetEntry($file.path)
            if ($null -eq $entry) { throw "Missing entry: $($file.path)" }
            $stream = [IO.MemoryStream]::new()
            try {
                $source = $entry.Open()
                try { $source.CopyTo($stream) } finally { $source.Dispose() }
                $bytes = $stream.ToArray()
            } finally { $stream.Dispose() }
            if ($bytes.Length -ne $file.size -or (HashBytes $bytes) -ne $file.sha256) { throw "Corrupt entry: $($file.path)" }
            if (-not $expected.Contains($file.path) -or (HashBytes $expected[$file.path].bytes) -ne $file.sha256) {
                throw "Current/baseline/diff drift: $($file.path)"
            }
        }
        if ($seen.Count -ne $expected.Count) { throw 'Manifest omits an expected entry' }
    } finally { $zip.Dispose() }
    $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath).Hash.ToLowerInvariant()
    $sidecar = (Get-Content -LiteralPath $hashPath -Raw).Trim().Split(' ')[0].ToLowerInvariant()
    if ($actualHash -ne $sidecar) { throw 'External ZIP SHA-256 mismatch' }
    "PASS entries=$($expected.Count) zipSha256=$actualHash"
}

if ((& git -C $root rev-parse HEAD).Trim() -ne '2efc8a5775f834c7f0fe37cc0ce703011649e1f1') {
    throw 'HEAD differs from surveyed local commit'
}
$entries = CollectEntries
if ($Mode -eq 'Build') {
    if (Test-Path -LiteralPath $zipPath) { throw 'Checkpoint 04 ZIP already exists; verify before any rebuild' }
    $manifest = @{
        branch = 'anh'
        localHead = '2efc8a5775f834c7f0fe37cc0ce703011649e1f1'
        dirtyBaseline = 'four byte snapshots; new files NOT_EXISTED'
        patchApplicability = 'NOT_VERIFIED'
        files = @($entries.Keys | Sort-Object | ForEach-Object {
            @{ path = $_; role = $entries[$_].role; size = $entries[$_].bytes.Length; sha256 = HashBytes $entries[$_].bytes }
        })
    }
    $zip = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($name in $entries.Keys) {
            $entry = $zip.CreateEntry($name, [IO.Compression.CompressionLevel]::Optimal)
            $stream = $entry.Open()
            try { $stream.Write($entries[$name].bytes) } finally { $stream.Dispose() }
        }
        $json = [Text.Encoding]::UTF8.GetBytes(($manifest | ConvertTo-Json -Depth 8))
        $entry = $zip.CreateEntry('manifest.json', [IO.Compression.CompressionLevel]::Optimal)
        $stream = $entry.Open()
        try { $stream.Write($json) } finally { $stream.Dispose() }
    } finally { $zip.Dispose() }
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath).Hash.ToLowerInvariant()
    "$hash  RF-10-refactor-checkpoint-04-handoff.zip" | Set-Content -LiteralPath $hashPath -NoNewline
}
VerifyArchive $entries
