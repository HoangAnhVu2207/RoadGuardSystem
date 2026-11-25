param(
    [Parameter(Mandatory)][int]$Lane,
    [Parameter(Mandatory)][string]$ResultsPath,
    [Parameter(Mandatory)][string]$Project,
    [Parameter(Mandatory)][string]$ManifestPath,
    [Parameter(Mandatory)][string]$Prefix,
    [switch]$CollectCoverage,
    [switch]$AllowMinioSmokeSkip
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$selected = @($manifest.lanes | Where-Object lane -eq $Lane)
if ($selected.Count -ne 1) { throw "No unique assignment for $Prefix lane $Lane." }
$clauses = @(
    $selected[0].classes | ForEach-Object { "FullyQualifiedName~$($_)." }
    $selected[0].methods | Where-Object { $_ } | ForEach-Object { "FullyQualifiedName=$_" }
)
if ($clauses.Count -eq 0) { throw "No assigned test selectors for $Prefix lane $Lane." }
$filter = $clauses -join '|'

$arguments = @(
    'test', $Project, '--no-build', '--nologo', '-v', 'q', '--filter', $filter,
    '--logger', "trx;LogFileName=$Prefix-lane-$Lane.trx",
    '--results-directory', $ResultsPath
)
if ($CollectCoverage) { $arguments += '--collect:XPlat Code Coverage' }

Push-Location $repo
try {
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & (Join-Path $PSScriptRoot 'Verify-TestResults.ps1') -ResultsPath $ResultsPath -AllowMinioSmokeSkip:$AllowMinioSmokeSkip
} finally {
    Pop-Location
}
