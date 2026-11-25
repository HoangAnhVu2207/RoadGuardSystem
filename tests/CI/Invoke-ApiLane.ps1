param(
    [Parameter(Mandatory)][ValidateRange(1, 20)][int]$Lane,
    [Parameter(Mandatory)][string]$ResultsPath,
    [switch]$CollectCoverage
)

$ErrorActionPreference = 'Stop'
$manifest = Join-Path $PSScriptRoot 'api-lanes.json'
$assembly = Join-Path $PSScriptRoot '../RoadGuardSystem.ApiTests/bin/Debug/net8.0/RoadGuardSystem.ApiTests.dll'
& (Join-Path $PSScriptRoot 'Verify-LaneDiscovery.ps1') -ManifestPath $manifest -AssemblyPath $assembly
$arguments = @{
    Lane = $Lane
    ResultsPath = $ResultsPath
    ManifestPath = $manifest
    Project = 'tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj'
    Prefix = 'api'
    CollectCoverage = $CollectCoverage
    AllowMinioSmokeSkip = $true
}
& (Join-Path $PSScriptRoot 'Invoke-TestLane.ps1') @arguments
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
