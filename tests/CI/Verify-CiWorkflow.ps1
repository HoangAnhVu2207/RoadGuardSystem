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

function Test-CiWorkflowContent {
    param (
        [string]$Content
    )
    $findings = @()

    # Secret reference and credential hardening
    if ($Content -notmatch 'secrets\.ROADGUARD_CI_SQL_PASSWORD') {
        $findings += 'CI workflow must reference repository secret "${{ secrets.ROADGUARD_CI_SQL_PASSWORD }}".'
    }

    # Extract mssql service block under services
    $inServices = $false
    $servicesIndent = -1
    $inMssql = $false
    $mssqlIndent = -1
    $mssqlLines = @()

    $lines = $Content -split "`r?`n"
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        $trimmed = $line.Trim()
        if ($trimmed.StartsWith("#") -or [string]::IsNullOrWhiteSpace($trimmed)) {
            continue
        }

        if (-not $inServices) {
            if ($line -match '^(\s*)services:\s*$') {
                $inServices = $true
                $servicesIndent = $Matches[1].Length
            }
            continue
        }

        # Inside services:
        if (-not $inMssql) {
            if ($line -match '^(\s*)mssql:\s*$') {
                $indent = $Matches[1].Length
                if ($indent -gt $servicesIndent) {
                    $inMssql = $true
                    $mssqlIndent = $indent
                }
            } elseif ($line -match '^(\s*)\S') {
                $indent = $Matches[1].Length
                if ($indent -le $servicesIndent) {
                    # Exited services block
                    $inServices = $false
                }
            }
            continue
        }

        # Inside mssql service:
        if ($line -match '^(\s*)\S') {
            $indent = $Matches[1].Length
            if ($indent -le $mssqlIndent) {
                # Exited mssql service block
                $inMssql = $false
                if ($indent -le $servicesIndent) {
                    $inServices = $false
                }
            } else {
                $mssqlLines += $line
            }
        }
    }

    # MSSQL_SA_PASSWORD in service mssql must strictly use secret reference, NOT literal
    $servicePasswordMatches = @()
    foreach ($mLine in $mssqlLines) {
        if ($mLine -match '^\s*MSSQL_SA_PASSWORD:\s*(.*)$') {
            $servicePasswordMatches += $Matches[1].Trim()
        }
    }

    if ($servicePasswordMatches.Count -eq 0) {
        $findings += "CI workflow service container 'mssql' is missing required MSSQL_SA_PASSWORD configuration."
    } elseif ($servicePasswordMatches.Count -gt 1) {
        $findings += "CI workflow service container 'mssql' contains duplicate MSSQL_SA_PASSWORD configuration entries."
    } else {
        $rawVal = $servicePasswordMatches[0]
        $val = $rawVal
        if ($val -match '^''([^'']*)''') {
            $val = $Matches[1].Trim()
        } elseif ($val -match '^"([^"]*)"') {
            $val = $Matches[1].Trim()
        } else {
            $val = ($val -split '\s+#')[0].Trim()
        }

        if ($val -notmatch '^\$\{\{\s*secrets\.ROADGUARD_CI_SQL_PASSWORD\s*\}\}$') {
            $findings += "CI workflow service container 'mssql' MSSQL_SA_PASSWORD must be strictly set to repository secret `${{ secrets.ROADGUARD_CI_SQL_PASSWORD }}` (found: '$rawVal'). Plaintext passwords and literals are prohibited."
        }
    }

    # Connection string in env must use secret reference, NOT literal password
    if ($Content -match 'Password=(?!(\$\{\{\s*secrets\.ROADGUARD_CI_SQL_PASSWORD\s*\}\}))[^;''"`\s]+') {
        $findings += "CI workflow contains hardcoded password literal in connection string."
    }

    # Healthcheck must NOT specify hardcoded password literal
    if (($Content -match '--health-cmd.*-P\s+[''"][^$\\"]') -or ($Content -match '--health-cmd.*-P\s+[^''"$\\s]')) {
        $findings += "CI workflow healthcheck specifies hardcoded password literal instead of container environment variable."
    }

    # Healthcheck must NOT use Compose-style $$MSSQL_SA_PASSWORD
    if ($Content -match '\$\$MSSQL_SA_PASSWORD') {
        $findings += 'CI workflow healthcheck must not use Compose-style "$$MSSQL_SA_PASSWORD". GitHub Actions service containers execute in container shell.'
    }

    # Healthcheck must NOT single-quote $MSSQL_SA_PASSWORD (disables parameter expansion)
    if ($Content -match '-P\s+''(?:\$\$)?\$MSSQL_SA_PASSWORD''') {
        $findings += "CI workflow healthcheck must not enclose `$MSSQL_SA_PASSWORD in single quotes because single quotes disable container shell parameter expansion."
    }

    # Healthcheck must use double-quoted single $MSSQL_SA_PASSWORD to allow shell expansion
    if (($Content -notmatch '--health-cmd.*-P\s+(\\"|")\$MSSQL_SA_PASSWORD(\\"|")') -or ($Content -match '\$\$MSSQL_SA_PASSWORD')) {
        $findings += 'CI workflow healthcheck must pass container environment variable "$MSSQL_SA_PASSWORD" double-quoted to allow container shell parameter expansion.'
    }

    # Seeder command must read from environment variable, NOT CLI argument (-c / --connection-string)
    if ($Content -match 'dotnet run.*--project.*tools/RoadGuardSystem\.Seeder.*--\s+(-c|--connection-string)') {
        $findings += "CI workflow seeder command must not pass connection string or password via CLI argument (-c / --connection-string). Must read from environment."
    }

    return $findings
}

# 1. SelfTestNegative mode: prove gate detects all distinct security and configuration flaws
if ($SelfTestNegative) {
    Write-Host "[SelfTestNegative] Running negative fixtures against CI verification rules..." -ForegroundColor Yellow

    # Fixture 1: Hardcoded password literal in healthcheck
    $fixture1Hardcoded = @"
name: Fixture 1 Hardcoded Password
on:
  push:
    branches: [ develop ]
jobs:
  build:
    runs-on: ubuntu-latest
    services:
      mssql:
        image: mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04
        env:
          MSSQL_SA_PASSWORD: `${{ secrets.ROADGUARD_CI_SQL_PASSWORD }}
        options: >-
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'HardcodedPlainPassword123!' -Q 'SELECT 1'"
"@

    # Fixture 2: Compose-style $$ in healthcheck
    $fixture2ComposeStyle = @"
name: Fixture 2 Compose Style
on:
  push:
    branches: [ develop ]
jobs:
  build:
    runs-on: ubuntu-latest
    services:
      mssql:
        image: mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04
        env:
          MSSQL_SA_PASSWORD: `${{ secrets.ROADGUARD_CI_SQL_PASSWORD }}
        options: >-
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P '`$`$MSSQL_SA_PASSWORD' -Q 'SELECT 1'"
"@

    # Fixture 3: Single-quoted environment variable in healthcheck
    $fixture3SingleQuoted = @"
name: Fixture 3 Single Quoted
on:
  push:
    branches: [ develop ]
jobs:
  build:
    runs-on: ubuntu-latest
    services:
      mssql:
        image: mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04
        env:
          MSSQL_SA_PASSWORD: `${{ secrets.ROADGUARD_CI_SQL_PASSWORD }}
        options: >-
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P '`$MSSQL_SA_PASSWORD' -Q 'SELECT 1'"
"@

    # Fixture 4: CLI connection-string argument in seeder invocation
    $fixture4CliArgs = @"
name: Fixture 4 Seeder CLI Args
on:
  push:
    branches: [ develop ]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Validate Seeder
        run: dotnet run --project tools/RoadGuardSystem.Seeder --no-build -- -c "Server=localhost;Password=secret"
"@

    # Fixture 5: Service password unquoted literal while connection string uses secret
    $fixture5UnquotedServicePassword = @"
name: Fixture 5 Unquoted Service Password
on:
  push:
    branches: [ develop ]
jobs:
  build:
    runs-on: ubuntu-latest
    services:
      mssql:
        image: mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04
        env:
          MSSQL_SA_PASSWORD: HardcodedPlainPassword123!
        options: >-
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P \"`$MSSQL_SA_PASSWORD\" -Q 'SELECT 1'"
    env:
      ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING: "Server=localhost,1433;Database=master;User Id=sa;Password=`${{ secrets.ROADGUARD_CI_SQL_PASSWORD }};TrustServerCertificate=True;"
"@

    $res1 = Test-CiWorkflowContent $fixture1Hardcoded
    $res2 = Test-CiWorkflowContent $fixture2ComposeStyle
    $res3 = Test-CiWorkflowContent $fixture3SingleQuoted
    $res4 = Test-CiWorkflowContent $fixture4CliArgs
    $res5 = Test-CiWorkflowContent $fixture5UnquotedServicePassword

    $detected1 = ($res1 | Where-Object { $_ -match "hardcoded password literal" }).Count -gt 0
    $detected2 = ($res2 | Where-Object { $_ -match "Compose-style" }).Count -gt 0
    $detected3 = ($res3 | Where-Object { $_ -match "single quote" -or $_ -match "single-quote" }).Count -gt 0
    $detected4 = ($res4 | Where-Object { $_ -match "CLI argument" }).Count -gt 0
    $detected5 = ($res5 | Where-Object { $_ -match "service container 'mssql' MSSQL_SA_PASSWORD" }).Count -gt 0

    $testCases = @(
        @{ Name = '1. Hardcoded password literal in healthcheck'; Detected = $detected1; Findings = $res1 },
        @{ Name = '2. Compose-style "$$MSSQL_SA_PASSWORD" in healthcheck'; Detected = $detected2; Findings = $res2 },
        @{ Name = '3. Single-quoted ''$MSSQL_SA_PASSWORD'' in healthcheck'; Detected = $detected3; Findings = $res3 },
        @{ Name = '4. CLI connection-string argument in seeder invocation'; Detected = $detected4; Findings = $res4 },
        @{ Name = '5. Unquoted service password literal while connection string uses secret'; Detected = $detected5; Findings = $res5 }
    )

    $failedTests = $testCases | Where-Object { -not $_.Detected }
    if ($failedTests.Count -gt 0) {
        Write-Host "FATAL: SelfTestNegative failed! $($failedTests.Count) negative fixture(s) were NOT detected as violations:" -ForegroundColor Red
        foreach ($ft in $failedTests) {
            Write-Host "  - FAILED: $($ft.Name)" -ForegroundColor Red
        }
        exit 2
    }

    Write-Host "[SelfTestNegative] All $($testCases.Count) distinct negative fixtures were verified and blocked as expected:" -ForegroundColor Green
    foreach ($tc in $testCases) {
        Write-Host "  [PASS] $($tc.Name)" -ForegroundColor Green
        foreach ($f in $tc.Findings) {
            Write-Host "         -> Caught: $f" -ForegroundColor Gray
        }
    }
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

# 5. Service container verification: exact pinned SQL Server image and healthcheck
$expectedSqlImage = "mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04"
if ($ciContent -notmatch [regex]::Escape($expectedSqlImage)) {
    $errors += "CI workflow service container must use pinned SQL Server image: '$expectedSqlImage'"
}
if ($ciContent -notmatch "(?s)options:.*--health-cmd") {
    $errors += "CI workflow SQL Server service container must specify a healthcheck command."
}

# Apply shared credential, secret, healthcheck, and CLI rules
$contentViolations = Test-CiWorkflowContent $ciContent
if ($contentViolations.Count -gt 0) {
    $errors += $contentViolations
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
