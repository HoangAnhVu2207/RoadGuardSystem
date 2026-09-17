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

# 1. SelfTestNegative mode: simulate broken/insecure CI workflow to verify gate blocks
if ($SelfTestNegative) {
    Write-Host "[SelfTestNegative] Simulating broken and insecure CI configurations..." -ForegroundColor Yellow
    $mockInsecureCi = @"
name: Insecure CI
on:
  push:
    branches: [ main ]
jobs:
  build:
    runs-on: ubuntu-latest
    services:
      mssql:
        image: mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04
        env:
          MSSQL_SA_PASSWORD: 'HardcodedPlainPassword123!'
        options: --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'HardcodedPlainPassword123!' -Q 'SELECT 1'"
    env:
      ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING: "Server=localhost,1433;Database=master;User Id=sa;Password=HardcodedPlainPassword123!;TrustServerCertificate=True;"
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Validate Seeder
        run: dotnet run --project tools/RoadGuardSystem.Seeder --no-build -- -c "Server=localhost,1433;Password=HardcodedPlainPassword123!;"
"@

    if ($mockInsecureCi -notmatch 'secrets\.ROADGUARD_CI_SQL_PASSWORD') {
        $errors += "[Simulated] Missing repository secret reference secrets.ROADGUARD_CI_SQL_PASSWORD."
    }
    if ($mockInsecureCi -match "MSSQL_SA_PASSWORD:\s*['`"][^$]") {
        $errors += "[Simulated] Hardcoded password literal in service MSSQL_SA_PASSWORD."
    }
    if ($mockInsecureCi -match 'Password=(?!(\$\{\{\s*secrets\.ROADGUARD_CI_SQL_PASSWORD\s*\}\}))[^;''"`\s]+') {
        $errors += "[Simulated] Hardcoded password literal in connection string."
    }
    if ($mockInsecureCi -match 'dotnet run.*--project.*Seeder.*--\s+-c') {
        $errors += "[Simulated] Seeder invoked with CLI connection string argument."
    }
    if ($mockInsecureCi -match '--health-cmd.*-P\s+[''"]?[^$]') {
        $errors += "[Simulated] Healthcheck specifies hardcoded password instead of container environment."
    }
    if ($mockInsecureCi -notmatch "dotnet-version:\s*['`"]?10\.0\.401['`"]?") {
        $errors += "[Simulated] Invalid or unpinned SDK version (must be 10.0.401)."
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

# Security rule: pull_request_target must NOT be used
if ($ciContent -match 'pull_request_target') {
    $errors += "CI workflow must not use 'pull_request_target' due to security elevation risk."
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

# 5. Service container verification: exact pinned SQL Server image, secret reference, container-env healthcheck
$expectedSqlImage = "mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04"
if ($ciContent -notmatch [regex]::Escape($expectedSqlImage)) {
    $errors += "CI workflow service container must use pinned SQL Server image: '$expectedSqlImage'"
}
if ($ciContent -notmatch "(?s)options:.*--health-cmd") {
    $errors += "CI workflow SQL Server service container must specify a healthcheck command."
}

# Finding 1 & 3: Secret reference and credential hardening
if ($ciContent -notmatch 'secrets\.ROADGUARD_CI_SQL_PASSWORD') {
    $errors += 'CI workflow must reference repository secret "${{ secrets.ROADGUARD_CI_SQL_PASSWORD }}".'
}

# MSSQL_SA_PASSWORD in service must use secret reference, NOT literal
if ($ciContent -match "MSSQL_SA_PASSWORD:\s*['`"][^$]") {
    $errors += "CI workflow contains hardcoded password literal in service container MSSQL_SA_PASSWORD."
}

# Connection string in env must use secret reference, NOT literal password
if ($ciContent -match 'Password=(?!(\$\{\{\s*secrets\.ROADGUARD_CI_SQL_PASSWORD\s*\}\}))[^;''"`\s]+') {
    $errors += "CI workflow contains hardcoded password literal in connection string."
}

# Healthcheck must use container environment variable ($$MSSQL_SA_PASSWORD), NOT hardcoded password
if ($ciContent -notmatch '--health-cmd.*-P\s+[''"]*\$\$MSSQL_SA_PASSWORD') {
    $errors += 'CI workflow healthcheck must pass container environment variable "$$MSSQL_SA_PASSWORD" to -P.'
}

# Seeder command must read from environment variable, NOT CLI argument (-c / --connection-string)
if ($ciContent -match 'dotnet run.*--project.*tools/RoadGuardSystem\.Seeder.*--\s+(-c|--connection-string)') {
    $errors += "CI workflow seeder command must not pass connection string or password via CLI argument (-c / --connection-string). Must read from environment."
}

# 6. Pipeline steps verification in expected order
$pipelineTokens = @(
    "actions/checkout@v4",
    "actions/setup-dotnet@v4",
    "Verify-P102Docs.ps1",
    "Verify-DockerCompose.ps1",
    "Verify-CiWorkflow.ps1",
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
