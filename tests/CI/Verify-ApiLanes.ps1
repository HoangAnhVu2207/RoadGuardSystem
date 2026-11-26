param(
    [Parameter(Mandatory)][string]$ResultsPath,
    [string]$ManifestPath = (Join-Path $PSScriptRoot 'api-lanes.json'),
    [string]$MatrixResult = 'success'
)

& (Join-Path $PSScriptRoot 'Verify-TestLanes.ps1') -ResultsPath $ResultsPath -ManifestPath $ManifestPath -Prefix api -LaneCount 9 -MatrixResult $MatrixResult -AllowMinioSmokeSkip
