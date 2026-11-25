param(
    [Parameter(Mandatory)][ValidateRange(1, 20)][int]$Lane,
    [Parameter(Mandatory)][string]$ResultsPath,
    [switch]$CollectCoverage
)

$ErrorActionPreference = 'Stop'
$manifest = Join-Path $PSScriptRoot 'sql-lanes.json'
& (Join-Path $PSScriptRoot 'Verify-SqlLaneDiscovery.ps1') -ManifestPath $manifest
$arguments = @{
    Lane = $Lane
    ResultsPath = $ResultsPath
    ManifestPath = $manifest
    Project = 'tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj'
    Prefix = 'integration'
    CollectCoverage = $CollectCoverage
}
& (Join-Path $PSScriptRoot 'Invoke-TestLane.ps1') @arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
