param(
    [Parameter(Mandatory = $true)][ValidateRange(1, 4)][int]$Lane,
    [Parameter(Mandatory = $true)][string]$ResultsPath,
    [switch]$CollectCoverage
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'sql-lanes.json') -Raw | ConvertFrom-Json
& (Join-Path $PSScriptRoot 'Verify-SqlLaneDiscovery.ps1')
$selected = @($manifest.lanes | Where-Object { $_.lane -eq $Lane })
if ($selected.Count -ne 1 -or @($selected[0].classes).Count -eq 0) {
    throw "SQL lane $Lane has no unique class assignment."
}

$filter = (@($selected[0].classes | ForEach-Object { "FullyQualifiedName~${_}." }) -join '|')
$arguments = @(
    'test', 'tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj',
    '--no-build', '--nologo', '-v', 'n', '--filter', $filter,
    '--logger', "trx;LogFileName=integration-lane-$Lane.trx",
    '--results-directory', $ResultsPath
)
if ($CollectCoverage) {
    $arguments += '--collect:XPlat Code Coverage'
}
Push-Location $repo
try {
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & (Join-Path $PSScriptRoot 'Verify-TestResults.ps1') -ResultsPath $ResultsPath
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
