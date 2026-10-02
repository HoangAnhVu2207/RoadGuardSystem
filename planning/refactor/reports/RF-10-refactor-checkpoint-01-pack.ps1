[CmdletBinding()]
param([ValidateSet('Build', 'Verify')][string]$Mode = 'Build')

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$zipPath = Join-Path $PSScriptRoot 'RF-10-refactor-checkpoint-01-handoff.zip'
$items = [System.Collections.Generic.List[object]]::new()

function Add-Paths([string]$Role, [string[]]$Paths) {
    foreach ($path in $Paths) {
        $script:items.Add([pscustomobject]@{ path = $path.Replace('\', '/'); source = $path.Replace('\', '/'); role = $Role })
    }
}

Add-Paths 'changed_checkpoint' @(
    '.agents/modules/README.md',
    'planning/refactor/README.md',
    'planning/refactor/03-master-plan.md',
    'planning/refactor/03-two-developer-plan.md',
    'planning/refactor/04-delivery-slices.md',
    'planning/refactor/04-operation-crosswalk.json',
    'planning/refactor/04-operation-crosswalk.md',
    'planning/refactor/09-change-gates.md',
    'planning/refactor/09-cg17-package.md',
    'planning/refactor/10-refactor-slices.md',
    'planning/refactor/10-refactor-checklist.md',
    'planning/refactor/10-survey-coexistence-baseline.md',
    'planning/refactor/11-development-plan.md',
    'planning/refactor/11-handoff-baseline.md',
    'planning/refactor/reports/RF-10-refactor.md',
    'planning/refactor/reports/RF-10-03-C00.md',
    'planning/refactor/reports/RF-10-03-C01.md',
    'planning/refactor/reports/RF-10-04-C01.md',
    'planning/refactor/reports/RF-11-C01.md',
    'planning/refactor/tasks/RF-10-01-identity-access.md',
    'planning/refactor/tasks/RF-10-02-project-road-scope.md',
    'planning/refactor/tasks/RF-10-03-survey-dataset.md',
    'planning/refactor/tasks/RF-10-04-file-upload-storage.md',
    'planning/refactor/tasks/RF-10-05-processing-ai.md',
    'planning/refactor/tasks/RF-10-06-reporter-defect.md',
    'planning/refactor/tasks/RF-10-07-inspection-fast-track-repair.md',
    'planning/refactor/tasks/RF-10-08-offline-sync.md',
    'planning/refactor/tasks/RF-10-09-messaging-reporting-retention.md',
    'planning/refactor/tasks/RF-11-release-retirement.md'
)

Add-Paths 'mixed_prior_and_checkpoint' @(
    'tests/RoadGuardSystem.ApiTests/Files/UploadApiTests.cs'
)

Add-Paths 'preexisting_dirty_context' @(
    'AGENTS.md',
    '.agents/manifest.json',
    'docs/product/confirmed-decisions.md',
    'planning/refactor/01-endpoint-inventory.md',
    'planning/refactor/reports/RF-09.md',
    'planning/refactor/09-consumer-registry.md',
    'RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs',
    'RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.Dataset.cs',
    'tests/RoadGuardSystem.ApiTests/Surveys/P2SurveyV2ApiTests.cs',
    'tests/RoadGuardSystem.IntegrationTests/Surveys/P2V2SurveyScopeConcurrencyTests.cs',
    'planning/V2/Execution/ANH-02-project-survey.md',
    'RoadGuardSystem.API/Controllers/ProjectRoadSectionsController.cs',
    'tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationSqlServerFixture.cs',
    'tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationWebApplicationFactory.cs',
    'tests/RoadGuardSystem.IntegrationTests/Infrastructure/SqlServerTestFixture.cs',
    'tests/Tooling/rf09_transition_guard.py',
    'tests/Tooling/test_rf09_transition_guard.py'
)

Add-Paths 'read_only_context' @(
    'planning/refactor/02-contract-gaps.md',
    'docs/backend/data/current-schema.inventory.json',
    'docs/diagram/V2/05_Technical/openapi.yaml',
    'docs/postman/RoadGuardSystem-V2.postman_collection.json',
    'RoadGuardSystem.API/RoadGuardSystem.API.http',
    'planning/refactor/tools/build_rf04_crosswalk.py',
    'planning/refactor/tools/verify_rf04.py',
    'planning/refactor/tools/rf04_ownership.py',
    'RoadGuardSystem.DTOs/Files/UploadCreateRequestDto.cs',
    'RoadGuardSystem.Services/Implementations/Files/UploadService.cs',
    'RoadGuardSystem.API/Controllers/UploadsController.cs',
    'RoadGuardSystem.API/Program.cs',
    'RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.cs',
    'RoadGuardSystem.BusinessObjects/Files/StoredFile.cs',
    'RoadGuardSystem.API/Constants/ApiErrorCodes.cs',
    'RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs',
    'RoadGuardSystem.API/Middlewares/CorrelationIdMiddleware.cs',
    'RoadGuardSystem.API/Controllers/SurveyPlanningController.cs',
    'RoadGuardSystem.API/Controllers/SurveyV2Controller.cs',
    'RoadGuardSystem.Services/Implementations/Surveys/SurveyPlanningService.cs',
    'RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs',
    'RoadGuardSystem.Repositories/Implementations/Surveys/SurveyPlanningPersistenceService.cs',
    'RoadGuardSystem.BusinessObjects/Surveys/SurveyPlan.cs',
    'RoadGuardSystem.BusinessObjects/Surveys/SurveyRequest.cs',
    'RoadGuardSystem.Repositories/RoadGuardDbContext.cs',
    'tests/RoadGuardSystem.ApiTests/Surveys/P122SurveyPlanningTests.cs',
    'tests/RoadGuardSystem.IntegrationTests/Infrastructure/Rf09TransitionRehearsalTests.cs'
)

Add-Paths 'packaging_tool' @(
    'planning/refactor/reports/RF-10-refactor-checkpoint-01-pack.ps1'
)

$items.Add([pscustomobject]@{
    path = 'README.md'
    source = 'planning/refactor/reports/RF-10-refactor-checkpoint-01-README.md'
    role = 'evidence'
})
$items.Add([pscustomobject]@{
    path = 'evidence/commands.md'
    source = 'planning/refactor/reports/RF-10-refactor-checkpoint-01-commands.md'
    role = 'evidence'
})
$items.Add([pscustomobject]@{
    path = 'evidence/overflow-stack-trace.txt'
    source = 'planning/refactor/reports/RF-10-refactor-checkpoint-01-overflow-stack-trace.txt'
    role = 'evidence'
})

function Get-Sha256([byte[]]$Bytes) {
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($Bytes)).ToLowerInvariant()
}

function Add-ZipBytes($Archive, [string]$Name, [byte[]]$Bytes, [string]$Role, [string]$Source, $Records) {
    $entry = $Archive.CreateEntry($Name, [IO.Compression.CompressionLevel]::Optimal)
    $stream = $entry.Open()
    try { $stream.Write($Bytes, 0, $Bytes.Length) } finally { $stream.Dispose() }
    $Records.Add([pscustomobject]@{
        path = $Name
        role = $Role
        source = $Source
        size_bytes = $Bytes.Length
        sha256 = Get-Sha256 $Bytes
    })
}

if ($Mode -eq 'Build') {
    if (Test-Path -LiteralPath $zipPath) { throw "Package already exists; refusing to overwrite: $zipPath" }
    $duplicate = $items | Group-Object path | Where-Object Count -gt 1
    if ($duplicate) { throw "Duplicate package path: $($duplicate.Name -join ', ')" }
    foreach ($item in $items) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $item.source) -PathType Leaf)) {
            throw "Missing source $($item.source)"
        }
    }
    if ((& git -C $root rev-parse HEAD).Trim() -ne '2efc8a5775f834c7f0fe37cc0ce703011649e1f1') {
        throw 'HEAD changed since checkpoint'
    }
    $records = [System.Collections.Generic.List[object]]::new()
    $status = (& git -C $root status --short --branch) -join "`n"
    if ($LASTEXITCODE -ne 0) { throw 'git status failed' }
    $diff = (& git -C $root diff -- tests/RoadGuardSystem.ApiTests/Files/UploadApiTests.cs) -join "`n"
    if ($LASTEXITCODE -ne 0 -or -not $diff.StartsWith('diff --git')) { throw 'git cumulative diff failed' }

    $fileStream = [IO.File]::Open($zipPath, [IO.FileMode]::CreateNew)
    $archive = [IO.Compression.ZipArchive]::new($fileStream, [IO.Compression.ZipArchiveMode]::Create, $false)
    try {
        foreach ($item in $items) {
            $absolute = Join-Path $root $item.source
            if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { throw "Missing source $($item.source)" }
            $bytes = [IO.File]::ReadAllBytes($absolute)
            Add-ZipBytes $archive $item.path $bytes $item.role $item.source $records
        }
        Add-ZipBytes $archive 'evidence/git-status-at-packaging.txt' ([Text.Encoding]::UTF8.GetBytes($status + "`n")) 'evidence' $null $records
        Add-ZipBytes $archive 'evidence/upload-test-cumulative-from-HEAD.patch' ([Text.Encoding]::UTF8.GetBytes($diff + "`n")) 'cumulative_diff_not_task_only' $null $records

        $manifest = [ordered]@{
            package = 'RF-10-refactor-checkpoint-01'
            branch = 'anh'
            head = '2efc8a5775f834c7f0fe37cc0ce703011649e1f1'
            baseline = 'BASELINE_NOT_CAPTURED'
            patch_applicability = 'NOT_VERIFIED'
            created_local = (Get-Date).ToString('o')
            files = $records
        }
        $manifestBytes = [Text.Encoding]::UTF8.GetBytes(($manifest | ConvertTo-Json -Depth 8) + "`n")
        $entry = $archive.CreateEntry('manifest.json', [IO.Compression.CompressionLevel]::Optimal)
        $stream = $entry.Open()
        try { $stream.Write($manifestBytes, 0, $manifestBytes.Length) } finally { $stream.Dispose() }
    } finally { $archive.Dispose() }
}

$zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entries = @($zip.Entries)
    $manifestEntry = $entries | Where-Object FullName -eq 'manifest.json'
    if (@($manifestEntry).Count -ne 1) { throw 'Missing or duplicate manifest' }
    $reader = [IO.StreamReader]::new($manifestEntry.Open(), [Text.Encoding]::UTF8)
    try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    if ($manifest.files.Count -ne $entries.Count - 1) { throw 'Manifest entry count differs from ZIP' }
    if ($manifest.head -ne (& git -C $root rev-parse HEAD).Trim()) { throw 'HEAD differs from package' }
    $names = @($entries | ForEach-Object FullName)
    if (@($names | Select-Object -Unique).Count -ne $names.Count) { throw 'Duplicate ZIP path' }
    foreach ($file in $manifest.files) {
        $entry = $zip.GetEntry($file.path)
        if ($null -eq $entry) { throw "Manifest path absent: $($file.path)" }
        $stream = $entry.Open()
        $memory = [IO.MemoryStream]::new()
        try { $stream.CopyTo($memory); $bytes = $memory.ToArray() }
        finally { $stream.Dispose(); $memory.Dispose() }
        if ($bytes.Length -ne $file.size_bytes -or (Get-Sha256 $bytes) -ne $file.sha256) {
            throw "Bad ZIP size/hash: $($file.path)"
        }
        if ($file.source) {
            $absolute = Join-Path $root $file.source
            if (-not (Test-Path -LiteralPath $absolute -PathType Leaf) -or
                (Get-Sha256 ([IO.File]::ReadAllBytes($absolute))) -ne $file.sha256) {
                throw "Working-tree mismatch: $($file.source)"
            }
        }
        $text = [Text.Encoding]::UTF8.GetString($bytes)
        if ($text -match '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----' -or
            $text -match '(?im)(?:server|data source)=[^\r\n;]+;[^\r\n]*(?:password|pwd)=' -or
            $text -match '(?im)^Authorization:\s*Bearer\s+[A-Za-z0-9._-]{20,}') {
            throw "Possible credential in ZIP entry: $($file.path)"
        }
    }
    if ($zip.GetEntry('README.md') -eq $null -or $zip.GetEntry('evidence/upload-test-cumulative-from-HEAD.patch') -eq $null) {
        throw 'Required package evidence absent'
    }
    Write-Output "PASS ZIP decompression/hash/worktree/credential check: $($manifest.files.Count) files + manifest"
} finally { $zip.Dispose() }
Get-FileHash -LiteralPath $zipPath -Algorithm SHA256 | ForEach-Object { "ZIP SHA-256 $($_.Hash)" }
