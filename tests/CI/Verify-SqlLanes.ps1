param(
    [Parameter(Mandatory)][string]$ResultsPath,
    [string]$ManifestPath = (Join-Path $PSScriptRoot 'sql-lanes.json'),
    [string]$MatrixResult = 'success'
)

& (Join-Path $PSScriptRoot 'Verify-TestLanes.ps1') -ResultsPath $ResultsPath -ManifestPath $ManifestPath -Prefix integration -LaneCount 10 -MatrixResult $MatrixResult
