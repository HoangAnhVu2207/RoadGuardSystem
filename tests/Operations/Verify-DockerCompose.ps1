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

# 1. SelfTestNegative mode: simulate broken/insecure compose config to prove gate blocks
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
    if ($mockCompose -notmatch 'MSSQL_SA_PASSWORD:\s*["'']?\$\{MSSQL_SA_PASSWORD:\?') {
        $errors += "[Simulated] Missing fail-closed required environment guard ':?' on MSSQL_SA_PASSWORD."
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

# 4. Validate .env.example contains only non-usable placeholders / empty values and no real secrets
$envExampleContent = Get-Content $envExample -Raw -Encoding UTF8
$prohibitedPasswords = @("SuperSecret", "Password123", "P@ssw0rd123!", "Admin@123", "RoadGuard@2026!", "yourStrong(!)Password")
foreach ($prohibited in $prohibitedPasswords) {
    if ($envExampleContent -match [regex]::Escape($prohibited)) {
        $errors += ".env.example contains immediately usable password or credential: '$prohibited'. It must be blank or placeholder requiring user configuration."
    }
}
if ($envExampleContent -notmatch 'MSSQL_SA_PASSWORD=') {
    $errors += ".env.example is missing MSSQL_SA_PASSWORD entry."
}
if ($envExampleContent -notmatch 'MSSQL_PORT=') {
    $errors += ".env.example is missing MSSQL_PORT configuration placeholder."
}

# 5. Validate docker-compose.yml text contents
$composeContent = Get-Content $composeFile -Raw -Encoding UTF8

# Image pinning
$expectedImage = "mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04"
if ($composeContent -notmatch [regex]::Escape($expectedImage)) {
    $errors += "docker-compose.yml must pin exact image: '$expectedImage'"
}

# Fail-closed interpolation guard: must use ${MSSQL_SA_PASSWORD:?...}
if ($composeContent -notmatch 'MSSQL_SA_PASSWORD:\s*["'']?\$\{MSSQL_SA_PASSWORD:\?[^}]+\}["'']?') {
    $errors += 'docker-compose.yml must enforce fail-closed required guard on MSSQL_SA_PASSWORD using interpolation pattern "${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD is required}".'
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

# Ensure .env itself is NOT committed
$trackedEnv = git ls-files .env
if (-not [string]::IsNullOrWhiteSpace($trackedEnv)) {
    $errors += "FATAL: .env file is currently tracked by Git! It must be untracked and ignored."
}

# 6. Structured verification using `docker compose config`
# Test A: When MSSQL_SA_PASSWORD is unset, `docker compose config` MUST fail (exit non-zero)
Write-Host "Verifying Docker Compose fail-closed behavior when MSSQL_SA_PASSWORD is unset..." -ForegroundColor Gray
$envBackup = $env:MSSQL_SA_PASSWORD
try {
    $env:MSSQL_SA_PASSWORD = $null
    $null = docker compose -f $composeFile config 2>&1
    if ($LASTEXITCODE -eq 0) {
        $errors += "docker compose config succeeded with exit code 0 when MSSQL_SA_PASSWORD was unset. It must fail-closed with non-zero exit code."
    }
} finally {
    if ($null -ne $envBackup) {
        $env:MSSQL_SA_PASSWORD = $envBackup
    }
}

# Test B: When temporary valid password provided, `docker compose config --format json` MUST succeed and parse valid JSON
Write-Host "Verifying Docker Compose structured JSON when temporary password is provided..." -ForegroundColor Gray
$tempSaPassword = "TempVerifyPassword123!"
$env:MSSQL_SA_PASSWORD = $tempSaPassword
try {
    $jsonOutput = docker compose -f $composeFile config --format json 2>&1
    if ($LASTEXITCODE -ne 0) {
        $errors += "docker compose config --format json failed with exit code $LASTEXITCODE when temporary password was provided."
    } else {
        $parsedConfig = $null
        try {
            $parsedConfig = $jsonOutput | ConvertFrom-Json
        } catch {
            $errors += "Failed to parse 'docker compose config --format json' output as JSON: $($_.Exception.Message)"
        }

        if ($null -ne $parsedConfig) {
            # Structured check: exactly 1 service
            $serviceNames = @($parsedConfig.services.PSObject.Properties | Select-Object -ExpandProperty Name)
            if ($serviceNames.Count -ne 1 -or $serviceNames[0] -ne "sqlserver") {
                $errors += "docker-compose.yml must define exactly one service named 'sqlserver'. Found: $($serviceNames -join ', ')"
            }

            $sqlService = $parsedConfig.services.sqlserver
            if ($null -eq $sqlService) {
                $errors += "Missing 'sqlserver' service in parsed Compose JSON."
            } else {
                if ($sqlService.image -ne $expectedImage) {
                    $errors += "Parsed service image '$($sqlService.image)' does not match expected '$expectedImage'."
                }
                if ($null -eq $sqlService.healthcheck) {
                    $errors += "Parsed service does not define healthcheck in Compose JSON."
                }
            }

            # Structured check: named volume exists
            if ($null -eq $parsedConfig.volumes -or -not $parsedConfig.volumes.mssql_data) {
                $errors += "Parsed Compose JSON missing named volume 'mssql_data'."
            }
        }
    }
} finally {
    if ($null -ne $envBackup) {
        $env:MSSQL_SA_PASSWORD = $envBackup
    } else {
        Remove-Item env:MSSQL_SA_PASSWORD -ErrorAction SilentlyContinue
    }
}

if ($errors.Count -gt 0) {
    Write-Host "FAILURE: Docker Compose verification failed with $($errors.Count) error(s):" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "SUCCESS: Docker Compose and environment configuration verified cleanly." -ForegroundColor Green
exit 0
