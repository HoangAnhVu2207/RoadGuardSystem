param(
    [ValidateSet('Api', 'Sql', 'Unit')][string]$Lane = 'Api',
    [string]$Results = 'D:/RoadGuard-AI-LocalTest/develop-sync-20261123/results'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
# Process-local test isolation: no configured/shared SQL or opted-in AI worker.
Remove-Item Env:ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING -ErrorAction SilentlyContinue
Remove-Item Env:ROADGUARD_MINIO_SMOKE -ErrorAction SilentlyContinue
Remove-Item Env:ROADGUARD_RUN_REAL_AI -ErrorAction SilentlyContinue
$env:RG_MINIO_IMAGE = 'rg-low-resource-20261030-minio:latest'
$env:AiRealWorker__Enabled = 'false'
$env:DOTNET_PROCESSOR_COUNT = '2'
$env:TEMP = 'D:/RoadGuard-AI-LocalTest/tmp'
$env:TMP = $env:TEMP
$env:RG_TEST_UNICODE_FONT = 'D:/RoadGuard-AI-LocalTest/venv/Lib/site-packages/matplotlib/mpl-data/fonts/ttf/DejaVuSans.ttf'
New-Item -ItemType Directory -Force -Path $Results | Out-Null
switch ($Lane) {
    'Api' {
        $project = 'tests/RoadGuardSystem.ApiTests'
        $filter = 'FullyQualifiedName~.Projects.|FullyQualifiedName~.Reports.|FullyQualifiedName~.Cases.|FullyQualifiedName~.Surveys.|FullyQualifiedName~.Processing.|FullyQualifiedName~.Inspections.|FullyQualifiedName~.Repairs.|FullyQualifiedName~.Offline.|FullyQualifiedName~.Notifications.|FullyQualifiedName~.Reporting.|FullyQualifiedName~.Files.|FullyQualifiedName~.Startup.|FullyQualifiedName~.Defects.|FullyQualifiedName~.Exports.|FullyQualifiedName~.Retention.|FullyQualifiedName~.Authorization.|FullyQualifiedName~.Idempotency.|FullyQualifiedName~.Labels.|FullyQualifiedName~.Warranties.'
    }
    'Sql' {
        $project = 'tests/RoadGuardSystem.IntegrationTests'
        $filter = 'FullyQualifiedName~.Processing.|FullyQualifiedName~.Projects.|FullyQualifiedName~.Surveys.|FullyQualifiedName~.Inspections.|FullyQualifiedName~.Repairs.|FullyQualifiedName~.Offline.|FullyQualifiedName~.Notifications.|FullyQualifiedName~.Reporting.|FullyQualifiedName~.Retention.|FullyQualifiedName~.Defects.|FullyQualifiedName~.Files.|FullyQualifiedName~.Huy01.|FullyQualifiedName~.Exports.'
    }
    'Unit' {
        $project = 'tests/RoadGuardSystem.UnitTests'
        $filter = 'FullyQualifiedName~.Processing.'
    }
}
Push-Location -LiteralPath $repo
try {
    $log = Join-Path $Results "workflow-$($Lane.ToLowerInvariant()).log"
    if ($Lane -eq 'Sql') {
        # Match CI: process-crash recovery cases require a dedicated child host.
        $workerProject = Join-Path $repo 'tools/RoadGuardSystem.ExportTestWorker/RoadGuardSystem.ExportTestWorker.csproj'
        & dotnet build $workerProject --nologo --verbosity quiet -p:RunAnalyzers=false *> (Join-Path $Results 'export-test-worker-build.log')
        if ($LASTEXITCODE -ne 0) { throw 'Export test worker build failed; inspect export-test-worker-build.log.' }
        $env:RG_EXPORT_TEST_WORKER = Join-Path $repo 'tools/RoadGuardSystem.ExportTestWorker/bin/Debug/net8.0/RoadGuardSystem.ExportTestWorker.dll'
        if (-not (Test-Path -LiteralPath $env:RG_EXPORT_TEST_WORKER -PathType Leaf)) { throw 'Export test worker assembly missing.' }
    }
    & dotnet test $project --filter $filter --logger "trx;LogFileName=workflow-$($Lane.ToLowerInvariant()).trx" --results-directory $Results --verbosity quiet -p:RunAnalyzers=false *> $log
    $code = $LASTEXITCODE
    Get-Content -LiteralPath $log -Tail 35
    exit $code
}
finally { Pop-Location }
