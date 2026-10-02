param([ValidateSet('Baseline','Capture','Build','Test','Package')][string]$Phase, [string]$Label)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path "$PSScriptRoot/../../../..").Path
$evidence = $PSScriptRoot
$testPath = 'tests/RoadGuardSystem.ApiTests/Processing/A0801CallbackIdentityCharacterizationTests.cs'
$docs = @('planning/refactor/reports/RF-10-07-C01.md', 'planning/refactor/evidence/post-checkpoint13-assessment/findings-ledger.md')
$payload = Join-Path $evidence 'payload'
function WriteJson($path, $value) {
    New-Item -ItemType Directory -Force -Path (Split-Path $path) | Out-Null
    $value | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $path -Encoding utf8
}
function SourcePaths {
    @(& rg --files --hidden -g '!**/bin/**' -g '!**/obj/**' -g '!**/.git/**' -g '!planning/refactor/evidence/**' -g '*.cs' -g '*.csproj' -g '*.props' -g '*.targets' -g 'global.json' -g 'NuGet.Config' -g 'appsettings*.json' -g 'xunit.runner.json' | Sort-Object)
}
function HashPaths($paths) {
    @($paths | ForEach-Object { $file = Get-Item -LiteralPath (Join-Path $repo $_); [pscustomobject]@{ path = $_.Replace('\','/'); size = $file.Length; sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() } })
}
function CopyMapped($relative, $prefix) {
    $dest = Join-Path $payload "$prefix/$relative"
    New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
    Copy-Item -LiteralPath (Join-Path $repo $relative) -Destination $dest
}
Push-Location $repo
try {
    if ($Phase -eq 'Baseline') {
        if (Test-Path "$payload/checks/baseline.json") { throw 'Baseline already exists; refusing overwrite.' }
        WriteJson "$payload/checks/baseline.json" @{ utc = [DateTimeOffset]::UtcNow; branch = (& git branch --show-current); head = (& git rev-parse HEAD); dirty = @(& git status --short); sources = @(HashPaths (SourcePaths)); allowlist = @($testPath, $docs, 'planning/refactor/tasks/RF-10-A08-01-characterization.md', 'planning/refactor/reports/RF-10-A08-01-characterization.md', 'planning/refactor/evidence/A08-01-characterization/**') }
        foreach ($doc in $docs) { CopyMapped $doc 'before' }
        WriteJson "$payload/checks/prior-handoff.json" (HashPaths @('planning/refactor/evidence/post-checkpoint13-assessment/RF-10-post-checkpoint-13-assessment-handoff-v2.zip','planning/refactor/evidence/RF-10-refactor-checkpoint-12-correction-01-handoff.tar.gz'))
    }
    if ($Phase -eq 'Capture') {
        if (!$Label) { throw 'Capture needs Label' }
        $assemblies = @(& rg --files --no-ignore tests/RoadGuardSystem.ApiTests/bin/Debug/net8.0 -g 'RoadGuardSystem*.dll' -g 'RoadGuardSystem*.pdb' | Sort-Object)
        WriteJson "$payload/checks/$Label.json" @{ utc = [DateTimeOffset]::UtcNow; sources = @(HashPaths (SourcePaths)); assemblies = @(HashPaths $assemblies) }
    }
    if ($Phase -in @('Build','Test')) {
        if (!$Label) { throw 'Run needs unique Label' }
        $logs = "$payload/runtime/$Label"
        if (Test-Path $logs) { throw 'Run label exists; preserve failed runs.' }
        New-Item -ItemType Directory -Force -Path $logs | Out-Null
        $args = if ($Phase -eq 'Build') { @('build','tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj','--no-incremental','--nologo') } else { @('test','tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj','--no-build','--no-restore','--filter','FullyQualifiedName~A0801CallbackIdentityCharacterizationTests','--logger','trx;LogFileName=A08-01.trx','--results-directory',('"' + $logs + '"'),'--nologo') }
        $start = [DateTimeOffset]::UtcNow
        $process = Start-Process -FilePath 'dotnet' -ArgumentList $args -WorkingDirectory $repo -RedirectStandardOutput "$logs/stdout.txt" -RedirectStandardError "$logs/stderr.txt" -WindowStyle Hidden -PassThru
        $process.WaitForExit()
        WriteJson "$logs/command.json" @{ executable = 'dotnet'; arguments = $args; cwd = $repo; startUtc = $start; endUtc = [DateTimeOffset]::UtcNow; exitCode = $process.ExitCode }
        Get-Content "$logs/stdout.txt" -Tail 6 | ForEach-Object { if ($_.Length -gt 500) { $_.Substring(0,500) + ' [full line retained in log]' } else { $_ } }
        Get-Content "$logs/stderr.txt" -Tail 6
        if ($process.ExitCode -ne 0) { throw "$Phase failed ($($process.ExitCode)); logs retained." }
    }
    if ($Phase -eq 'Package') {
        $pre = Get-Content "$payload/checks/pre-build-02.json" -Raw | ConvertFrom-Json
        $built = Get-Content "$payload/checks/post-build-02.json" -Raw | ConvertFrom-Json
        $tested = Get-Content "$payload/checks/post-test-02.json" -Raw | ConvertFrom-Json
        $sourceDiffBuild = @(Compare-Object @($pre.sources | ForEach-Object { $_.path + ':' + $_.sha256 }) @($built.sources | ForEach-Object { $_.path + ':' + $_.sha256 }))
        $sourceDiffTest = @(Compare-Object @($built.sources | ForEach-Object { $_.path + ':' + $_.sha256 }) @($tested.sources | ForEach-Object { $_.path + ':' + $_.sha256 }))
        $assemblyDiff = @(Compare-Object @($built.assemblies | ForEach-Object { $_.path + ':' + $_.sha256 }) @($tested.assemblies | ForEach-Object { $_.path + ':' + $_.sha256 }))
        $runs = @()
        foreach ($run in @('test-01','test-02')) {
            [xml]$trx = Get-Content "$payload/runtime/$run/A08-01.trx" -Raw
            $counters = @{}; foreach ($attribute in $trx.TestRun.ResultSummary.Counters.Attributes) { $counters[$attribute.Name] = $attribute.Value }
            $times = @{}; foreach ($attribute in $trx.TestRun.Times.Attributes) { $times[$attribute.Name] = $attribute.Value }
            $runs += @{ run = $run; counters = $counters; times = $times; cases = @($trx.TestRun.Results.UnitTestResult | ForEach-Object { @{ name = $_.testName; outcome = $_.outcome; duration = $_.duration; error = $_.Output.ErrorInfo.Message } }) }
        }
        $finalCounters = $runs[-1].counters
        $runCommands = @('build-01','test-01','build-02','test-02') | ForEach-Object { Get-Content "$payload/runtime/$_/command.json" -Raw | ConvertFrom-Json }
        if ($sourceDiffBuild.Count + $sourceDiffTest.Count + $assemblyDiff.Count -gt 0 -or $finalCounters.passed -ne '13' -or $finalCounters.failed -ne '0' -or $runCommands[-1].exitCode -ne 0 -or $runCommands[-2].exitCode -ne 0) { throw 'Final verification gate failed.' }
        WriteJson "$payload/checks/verification-summary.json" @{ sourceCount = $pre.sources.Count; sourceDiffBuild = $sourceDiffBuild; sourceDiffTest = $sourceDiffTest; assemblyDiff = $assemblyDiff; assemblies = $tested.assemblies; commands = @($runCommands); runs = $runs }
        $baseline = Get-Content "$payload/checks/baseline.json" -Raw | ConvertFrom-Json
        $current = HashPaths (SourcePaths)
        $sourceDiffPackage = @(Compare-Object @($tested.sources | ForEach-Object { $_.path + ':' + $_.sha256 }) @($current | ForEach-Object { $_.path + ':' + $_.sha256 }))
        if ($sourceDiffPackage.Count -gt 0) { throw 'Executable source changed after final verification.' }
        $drift = @($baseline.sources | Where-Object { $old = $_; !($current | Where-Object { $_.path -eq $old.path -and $_.sha256 -eq $old.sha256 }) })
        if ($drift.Count -gt 0) { throw 'Preexisting source changed outside allowlist.' }
        $added = @($current | Where-Object { $_.path -notin $baseline.sources.path })
        if (@($added | Where-Object { $_.path -ne $testPath }).Count -gt 0) { throw 'Unexpected new source.' }
        foreach ($doc in $docs) {
            CopyMapped $doc 'current'
            $diff = & git diff --no-index -- "$payload/before/$doc" "$repo/$doc" 2>&1
            if ($LASTEXITCODE -gt 1) { throw 'Diff failed.' }
            $dest = "$payload/diffs/$doc.diff"
            New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
            $diff | Set-Content $dest -Encoding utf8
        }
        $references = @('AGENTS.md','.agents/manifest.json','.agents/rules/evidence.md','.agents/rules/delivery.md','.agents/rules/safety.md','.agents/rules/review-and-coordination.md','.agents/modules/README.md','planning/refactor/templates/task-report.md','planning/refactor/tasks/RF-10-05-processing-ai.md','planning/refactor/tasks/RF-10-08-offline-sync.md','planning/refactor/tasks/RF-10-A08-01-characterization.md','planning/refactor/reports/RF-10-A08-01-characterization.md','planning/refactor/reports/RF-10-08-assessment.md','planning/refactor/evidence/post-checkpoint13-assessment/idempotency-matrix.md','docs/product/confirmed-decisions.md','docs/diagram/V2/AI_Integration/README.md','contracts/events/README.md')
        foreach ($path in $references) { CopyMapped $path 'current' }
        $context = @(& rg --files RoadGuardSystem.BusinessObjects/Processing RoadGuardSystem.BusinessObjects/Idempotency RoadGuardSystem.Repositories/Idempotency RoadGuardSystem.Repositories/Interfaces/Processing RoadGuardSystem.Services/Interfaces/Processing RoadGuardSystem.DTOs/Processing RoadGuardSystem.API/Authentication -g '*.cs')
        $context += @($testPath,'tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationSqlServerFixture.cs','tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationWebApplicationFactory.cs','tests/RoadGuardSystem.IntegrationTests/Processing/Rf1005CallbackCharacterizationTests.cs','RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs','RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs','RoadGuardSystem.API/Program.cs','RoadGuardSystem.Services/Implementations/Processing/ProcessingV2Service.cs','RoadGuardSystem.Repositories/Implementations/Processing/ProcessingV2PersistenceService.cs','RoadGuardSystem.Repositories/RoadGuardDbContext.cs')
        $context += @('tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationApiCollection.cs','RoadGuardSystem.API/Constants/ApiErrorCodes.cs','RoadGuardSystem.BusinessObjects/Catalogs/DefectType.cs','RoadGuardSystem.BusinessObjects/Identity/ApplicationUser.cs','RoadGuardSystem.BusinessObjects/Auditing/AuditLog.cs','RoadGuardSystem.BusinessObjects/Messaging/OutboxMessage.cs','RoadGuardSystem.BusinessObjects/Common/Enums.cs','RoadGuardSystem.BusinessObjects/Common/Extensions/UserRoleCodeExtensions.cs','RoadGuardSystem.Repositories/Implementations/Seeding/IdentityRoleSeedStep.cs')
        $context += @('RoadGuardSystem.API/BackgroundServices/ValidationRunWorker.cs','RoadGuardSystem.BusinessObjects/Identity/ApplicationRole.cs','RoadGuardSystem.Repositories/Configurations/ApplicationRoleConfiguration.cs')
        $context += @(& rg --files RoadGuardSystem.Services/Options RoadGuardSystem.API/Middlewares -g '*Jwt*' -g '*Correlation*')
        $context += @(& rg --files RoadGuardSystem.Repositories/Configurations -g '*ProjectConfiguration*' -g '*RoadSection*' -g '*SurveyConfiguration*' -g '*SurveyDataVersion*' -g '*AIModelVersion*' -g '*DefectType*' -g '*ApplicationUser*' -g '*AuditLog*' -g '*OutboxMessage*')
        $context += @(& rg --files RoadGuardSystem.Repositories/Configurations -g '*Processing*' -g '*AIDetection*' -g '*Idempotency*' -g '*StoredFile*' -g '*UploadSession*' -g '*FileScope*')
        $context += @(& rg --files RoadGuardSystem.BusinessObjects/Files RoadGuardSystem.BusinessObjects/Projects RoadGuardSystem.BusinessObjects/Surveys -g '*.cs')
        $context += @($current | Where-Object { $_.path -match '\.(csproj|props|targets)$|global.json$|xunit.runner.json$' } | ForEach-Object path)
        $context = @($context | Sort-Object -Unique)
        foreach ($path in $context) { CopyMapped $path 'source' }
        foreach ($path in @('pack-handoff.ps1','scope-matrix.md','compatibility.md','provenance.md','review.md','README.md')) { CopyMapped "planning/refactor/evidence/A08-01-characterization/$path" 'current' }
        CopyMapped 'planning/refactor/evidence/RF-10-refactor-checkpoint-12-correction-01-handoff.tar.gz.sha256' 'historical'
        $searchArgs = @('-n','ReceiveResultAsync|processing_job.dispatch','RoadGuardSystem.API','RoadGuardSystem.Services','RoadGuardSystem.Repositories','-g','*.cs','-g','!**/Migrations/**','-g','!**/bin/**','-g','!**/obj/**')
        $searchOutput = @(& rg @searchArgs)
        $searchExit = $LASTEXITCODE
        WriteJson "$payload/checks/callback-reachability.json" @{ executable = 'rg'; arguments = $searchArgs; exitCode = $searchExit; output = $searchOutput; scope = 'Production C# in API/Services/Repositories excluding migrations/bin/obj, normal rg ignore rules; no external provider repository searched' }
        if ($searchExit -ne 0) { throw 'Callback source search failed.' }
        $reportPath = 'planning/refactor/reports/RF-10-A08-01-characterization.md'
        $sectionCount = @(Select-String -Path $reportPath -Pattern '^## [1-7]\.').Count
        if ($sectionCount -ne 7) { throw 'Report must have seven sections.' }
        $parseTokens = $null; $parseErrors = $null
        [System.Management.Automation.Language.Parser]::ParseFile($PSCommandPath,[ref]$parseTokens,[ref]$parseErrors) | Out-Null
        if ($parseErrors.Count -gt 0) { throw 'Packaging script syntax errors.' }
        $patchCheck = @(& git diff --check 2>&1 | ForEach-Object { [string]$_ })
        if ($LASTEXITCODE -ne 0) { throw 'git diff --check failed.' }
        WriteJson "$payload/checks/static-review.json" @{ reportSectionCount = $sectionCount; scriptParseErrors = $parseErrors.Count; gitDiffCheck = $patchCheck; inspectedDocs = @($docs); sourceScope = 'baseline hash preservation + final source hash equality; only new A08-01 test allowed' }
        WriteJson "$payload/checks/final-state.json" @{ utc = [DateTimeOffset]::UtcNow; branch = (& git branch --show-current); head = (& git rev-parse HEAD); dirty = @(& git status --short); preservedSourceCount = $baseline.sources.Count; drift = $drift; added = $added; sourceContext = $context }
        $files = @(Get-ChildItem $payload -File -Recurse | Where-Object { $_.FullName -ne "$payload\MANIFEST.json" } | Sort-Object FullName)
        $entries = @($files | ForEach-Object { @{ path = [IO.Path]::GetRelativePath($payload,$_.FullName).Replace('\','/'); size = $_.Length; sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() } })
        WriteJson "$payload/MANIFEST.json" @{ excludedOnly = 'MANIFEST.json'; entries = $entries }
        $zip = Join-Path $evidence 'RF-10-A08-01-characterization-handoff.zip'
        if (Test-Path $zip) { throw 'Archive exists; do not overwrite.' }
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::CreateFromDirectory($payload,$zip)
        $extract = Join-Path $evidence 'extract-verification'
        [IO.Compression.ZipFile]::ExtractToDirectory($zip,$extract)
        $manifest = Get-Content "$extract/MANIFEST.json" -Raw | ConvertFrom-Json
        $missing = @(); $mismatch = @()
        foreach ($entry in $manifest.entries) {
            $path = Join-Path $extract $entry.path
            if (!(Test-Path -LiteralPath $path)) { $missing += $entry.path; continue }
            if ((Get-Item $path).Length -ne $entry.size -or (Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.sha256) { $mismatch += $entry.path }
        }
        $actual = @(Get-ChildItem $extract -File -Recurse | ForEach-Object { [IO.Path]::GetRelativePath($extract,$_.FullName).Replace('\','/') })
        $unlisted = @($actual | Where-Object { $_ -ne 'MANIFEST.json' -and $_ -notin $manifest.entries.path })
        $archive = [IO.Compression.ZipFile]::OpenRead($zip)
        try { $duplicates = @($archive.Entries | Group-Object FullName | Where-Object Count -gt 1).Count } finally { $archive.Dispose() }
        $manifestDuplicates = @($manifest.entries | Group-Object path | Where-Object Count -gt 1).Count
        $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
        WriteJson "$evidence/archive-verification.json" @{ zip = $zip; sha256 = $hash; payloadCount = $manifest.entries.Count; extractedCount = $actual.Count; missing = $missing; mismatch = $mismatch; unlisted = $unlisted; duplicates = $duplicates; manifestDuplicates = $manifestDuplicates }
        if ($missing.Count + $mismatch.Count + $unlisted.Count + $duplicates + $manifestDuplicates -gt 0) { throw 'Archive verification failed.' }
        "$hash  RF-10-A08-01-characterization-handoff.zip" | Set-Content "$zip.sha256" -Encoding ascii
        Get-Content "$evidence/archive-verification.json"
    }
} finally { Pop-Location }
