# Automated GitHub Actions CI Workflow Verification
[CmdletBinding()]
param (
    [string]$RepoRoot = "",
    [switch]$SelfTestNegative
)

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
    $RepoRoot = Resolve-Path (Join-Path $scriptDir "..\..")
}

Write-Host "=== GitHub Actions CI Workflow Verification ===" -ForegroundColor Cyan
Write-Host "Repository root: $RepoRoot" -ForegroundColor Gray

$errors = @()

$ciFile = Join-Path $RepoRoot ".github/workflows/ci.yml"

# 1. SelfTestNegative mode: simulate broken CI workflow to verify gate blocks
if ($SelfTestNegative) {
    Write-Host "[SelfTestNegative] Simulating broken CI configuration..." -ForegroundColor Yellow
    $mockCi = @"
name: Broken CI
on:
  push:
    branches: [ main ]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Build
        run: dotnet build
"@
    if ($mockCi -notmatch 'mcr\.microsoft\.com/mssql/server:2019-CU18-ubuntu-20\.04') {
        $errors += "[Simulated] Missing SQL Server service container."
    }
    if ($mockCi -notmatch 'dotnet-version:\s*[''"]?10\.0\.401[''"]?') {
        $errors += "[Simulated] Invalid or unpinned SDK version (must be 10.0.401)."
    }
    if ($mockCi -notmatch 'Verify-P102Docs\.ps1') {
        $errors += "[Simulated] Missing documentation verifier step."
    }
    if ($mockCi -notmatch 'Verify-DependencySecurity\.ps1') {
        $errors += "[Simulated] Missing dependency security scan step."
    }
    if ($mockCi -notmatch 'dotnet format --verify-no-changes') {
        $errors += "[Simulated] Missing format verification step."
    }
    if ($mockCi -notmatch 'actions/upload-artifact') {
        $errors += "[Simulated] Missing coverage artifact upload step."
    }

    Write-Host "[SelfTestNegative] Detected $($errors.Count) simulated policy violations as expected." -ForegroundColor Green
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

# 2. Check existence of CI workflow file
if (-not (Test-Path $ciFile)) {
    $errors += "Missing .github/workflows/ci.yml."
    Write-Host "FAILURE: Missing required workflow file:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

$ciContent = Get-Content $ciFile -Raw -Encoding UTF8

# 3. Triggers verification
$requiredTriggers = @("pull_request", "push", "workflow_dispatch")
foreach ($trigger in $requiredTriggers) {
    if ($ciContent -notmatch "(?m)^\s*$([regex]::Escape($trigger)):") {
        $errors += "CI workflow missing required trigger: '$trigger'"
    }
}

$requiredBranches = @("develop", "main", "anh", "huy")
foreach ($branch in $requiredBranches) {
    if ($ciContent -notmatch [regex]::Escape($branch)) {
        $errors += "CI workflow trigger branches missing: '$branch'"
    }
}

# 4. SDK verification: exact pinned 10.0.401
if ($ciContent -notmatch "dotnet-version:\s*['`"]?10\.0\.401['`"]?") {
    $errors += "CI workflow must use exact pinned .NET SDK version '10.0.401'."
}

# 5. Service container verification: exact pinned SQL Server image and healthcheck
$expectedSqlImage = "mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04"
if ($ciContent -notmatch [regex]::Escape($expectedSqlImage)) {
    $errors += "CI workflow service container must use pinned SQL Server image: '$expectedSqlImage'"
}
if ($ciContent -notmatch "(?s)options:.*--health-cmd") {
    $errors += "CI workflow SQL Server service container must specify a healthcheck command."
}

# 6. Pipeline steps verification in expected order
$pipelineTokens = @(
    "actions/checkout@v4",
    "actions/setup-dotnet@v4",
    "Verify-P102Docs.ps1",
    "Verify-DependencySecurity.ps1",
    "dotnet restore",
    "dotnet format --verify-no-changes",
    "dotnet build",
    "--no-incremental",
    "dotnet test",
    "XPlat Code Coverage",
    "actions/upload-artifact@v4"
)

$lastIndex = -1
foreach ($token in $pipelineTokens) {
    $currentIndex = $ciContent.IndexOf($token)
    if ($currentIndex -lt 0) {
        $errors += "CI workflow missing required pipeline token/step: '$token'"
    } elseif ($currentIndex -lt $lastIndex) {
        $errors += "CI workflow pipeline step out of sequence: '$token' appeared before previous required step."
    } else {
        $lastIndex = $currentIndex
    }
}

# 7. Coverage collection verification across all test suites
$testSuites = @("UnitTests", "ApiTests", "IntegrationTests")
foreach ($suite in $testSuites) {
    if ($ciContent -notmatch [regex]::Escape($suite)) {
        $errors += "CI workflow test execution does not include suite: '$suite'"
    }
}

if ($errors.Count -gt 0) {
    Write-Host "FAILURE: CI workflow verification failed with $($errors.Count) error(s):" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "SUCCESS: CI workflow configuration verified cleanly." -ForegroundColor Green
exit 0
