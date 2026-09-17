[CmdletBinding()]
param (
    [string]$ProjectPath = "tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj",
    [string[]]$BlockedSeverities = @("High", "Critical")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ProjectPath)) {
    Write-Error "Project file not found: $ProjectPath"
    exit 2
}

Write-Host "Running dependency vulnerability scan on: $ProjectPath" -ForegroundColor Cyan
$scanOutput = & dotnet list $ProjectPath package --vulnerable --include-transitive 2>&1

$hasVulnerabilities = $false
$vulnerabilityFindings = @()

$lines = $scanOutput -split "`r?`n"
$parsingTable = $false

foreach ($line in $lines) {
    if ($line -match "Transitive Package|Top-level Package") {
        $parsingTable = $true
        continue
    }

    if ($parsingTable) {
        if ($line -match '^\s*>\s*([^\s]+)\s+([^\s]+)\s+([^\s]+)\s+(https?://[^\s]+)') {
            $pkgName = $matches[1]
            $resolvedVer = $matches[2]
            $severity = $matches[3]
            $advisoryUrl = $matches[4]

            if ($BlockedSeverities -contains $severity) {
                $hasVulnerabilities = $true
                $vulnerabilityFindings += [PSCustomObject]@{
                    Package     = $pkgName
                    Resolved    = $resolvedVer
                    Severity    = $severity
                    AdvisoryUrl = $advisoryUrl
                }
            }
        }
        elseif ($line -match '^\s*The given project .* has no vulnerable packages') {
            $parsingTable = $false
        }
    }
}

if ($hasVulnerabilities) {
    Write-Host "FAILURE: Detected $($vulnerabilityFindings.Count) vulnerable package(s) matching severities ($($BlockedSeverities -join ', ')):" -ForegroundColor Red
    foreach ($v in $vulnerabilityFindings) {
        Write-Host "  - Package: $($v.Package) (Resolved: $($v.Resolved)) | Severity: $($v.Severity) | Advisory: $($v.AdvisoryUrl)" -ForegroundColor Red
    }
    exit 1
}

Write-Host "SUCCESS: No vulnerable dependencies with severity ($($BlockedSeverities -join ', ')) detected in $ProjectPath." -ForegroundColor Green
exit 0
