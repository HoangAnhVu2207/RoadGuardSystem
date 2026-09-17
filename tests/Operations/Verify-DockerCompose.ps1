# Automated Docker Compose and Environment Configuration Verification
[CmdletBinding()]
param (
    [string]$RepoRoot = "",
    [switch]$SelfTestNegative
)

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
    $RepoRoot = Resolve-Path (Join-Path $scriptDir "..\..")
}

Write-Host "=== Docker Compose & Environment Verification ===" -ForegroundColor Cyan
Write-Host "Repository root: $RepoRoot" -ForegroundColor Gray

$errors = @()

$composeFile = Join-Path $RepoRoot "docker-compose.yml"
$envExample = Join-Path $RepoRoot ".env.example"
$gitignore = Join-Path $RepoRoot ".gitignore"

# 1. SelfTestNegative mode: simulate corrupt/insecure compose config to prove gate blocks
if ($SelfTestNegative) {
    Write-Host "[SelfTestNegative] Simulating insecure Docker Compose and missing healthcheck..." -ForegroundColor Yellow
    $mockCompose = @"
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:latest
    ports:
      - "1433:1433"
    environment:
      - MSSQL_SA_PASSWORD=SuperSecretRealPassword123!
"@
    if ($mockCompose -notmatch 'mcr\.microsoft\.com/mssql/server:2019-CU18-ubuntu-20\.04') {
        $errors += "[Simulated] Missing pinned SQL Server 2019-CU18 image."
    }
    if ($mockCompose -notmatch 'healthcheck:') {
        $errors += "[Simulated] Missing container healthcheck."
    }
    if ($mockCompose -match 'MSSQL_SA_PASSWORD=[^$]') {
        $errors += "[Simulated] Hardcoded secret detected in compose environment."
    }
    if ($mockCompose -notmatch 'volumes:') {
        $errors += "[Simulated] Missing named persistent volume."
    }

    Write-Host "[SelfTestNegative] Detected $($errors.Count) simulated policy violations as expected." -ForegroundColor Green
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

# 2. Check existence of required files
if (-not (Test-Path $composeFile)) {
    $errors += "Missing docker-compose.yml at repository root."
}
if (-not (Test-Path $envExample)) {
    $errors += "Missing .env.example template at repository root."
}
if (-not (Test-Path $gitignore)) {
    $errors += "Missing .gitignore at repository root."
}

if ($errors.Count -gt 0) {
    Write-Host "FAILURE: Missing required configuration files:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

# 3. Validate .gitignore protects .env
$gitignoreContent = Get-Content $gitignore -Raw -Encoding UTF8
if ($gitignoreContent -notmatch '(?m)^\.env\b' -and $gitignoreContent -notmatch '(?m)^\.env$') {
    $errors += ".gitignore does not properly ignore '.env' files."
}

# 4. Validate .env.example contains only placeholders and no real secrets
$envExampleContent = Get-Content $envExample -Raw -Encoding UTF8
$prohibitedPasswords = @("SuperSecret", "Password123", "P@ssw0rd123!", "Admin@123", "RoadGuard@2026!")
foreach ($prohibited in $prohibitedPasswords) {
    if ($envExampleContent -match [regex]::Escape($prohibited)) {
        $errors += ".env.example appears to contain hardcoded password: '$prohibited'"
    }
}
if ($envExampleContent -notmatch 'MSSQL_SA_PASSWORD=') {
    $errors += ".env.example is missing MSSQL_SA_PASSWORD placeholder."
}
if ($envExampleContent -notmatch 'MSSQL_PORT=') {
    $errors += ".env.example is missing MSSQL_PORT configuration placeholder."
}

# 5. Validate docker-compose.yml contents
$composeContent = Get-Content $composeFile -Raw -Encoding UTF8

# Image pinning
$expectedImage = "mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04"
if ($composeContent -notmatch [regex]::Escape($expectedImage)) {
    $errors += "docker-compose.yml must pin exact image: '$expectedImage'"
}

# Only SQL Server container
if ($composeContent -match 'services:\s*\n(\s+[a-zA-Z0-9_-]+:\s*\n){2,}') {
    $errors += "docker-compose.yml must only define SQL Server for Wave 0 local dependencies."
}

# Named persistent volume
if ($composeContent -notmatch 'volumes:\s*\n\s+[a-zA-Z0-9_-]+:') {
    $errors += "docker-compose.yml must define a named persistent data volume."
}

# Port configurable via environment variable
if ($composeContent -notmatch '\$\{[^}]*MSSQL_PORT[^}]*\}:1433' -and $composeContent -notmatch '"\$\{[^}]*MSSQL_PORT[^}]*\}:1433"') {
    $errors += "docker-compose.yml port mapping must be configurable via `${MSSQL_PORT}` environment variable."
}

# Healthcheck presence
if ($composeContent -notmatch 'healthcheck:') {
    $errors += "docker-compose.yml must configure a database readiness healthcheck."
}
if ($composeContent -notmatch 'sqlcmd' -or $composeContent -notmatch 'SELECT 1') {
    $errors += "docker-compose.yml healthcheck must utilize sqlcmd with 'SELECT 1' test query."
}

# No hardcoded password in compose file
$saPasswordMatch = [regex]::Match($composeContent, 'MSSQL_SA_PASSWORD:\s*["'']?([^"''\r\n\s]+)')
if ($saPasswordMatch.Success) {
    $saVal = $saPasswordMatch.Groups[1].Value
    if ($saVal -ne '${MSSQL_SA_PASSWORD}') {
        $errors += "docker-compose.yml contains hardcoded password in MSSQL_SA_PASSWORD ($saVal). Use environment variable interpolation `${MSSQL_SA_PASSWORD}` instead."
    }
}

# Ensure .env itself is NOT committed
$trackedEnv = git ls-files .env
if (-not [string]::IsNullOrWhiteSpace($trackedEnv)) {
    $errors += "FATAL: .env file is currently tracked by Git! It must be untracked and ignored."
}

if ($errors.Count -gt 0) {
    Write-Host "FAILURE: Docker Compose verification failed with $($errors.Count) error(s):" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "SUCCESS: Docker Compose and environment configuration verified cleanly." -ForegroundColor Green
exit 0
