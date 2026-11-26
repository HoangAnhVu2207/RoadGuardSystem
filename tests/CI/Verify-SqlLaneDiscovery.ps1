param(
    [string]$ManifestPath = (Join-Path $PSScriptRoot 'sql-lanes.json'),
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '../RoadGuardSystem.IntegrationTests/bin/Debug/net8.0/RoadGuardSystem.IntegrationTests.dll')
)

$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
if ($manifest.localRealAcceptanceClass -cne 'RoadGuardSystem.IntegrationTests.Processing.AiRealVerticalTests') {
    throw 'Declare the opt-in REAL AI acceptance class separately from the SQL lanes.'
}
& (Join-Path $PSScriptRoot 'Verify-LaneDiscovery.ps1') -ManifestPath $ManifestPath -AssemblyPath $AssemblyPath
