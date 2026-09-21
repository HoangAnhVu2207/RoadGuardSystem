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

    # Hosted service containers initialize before steps, so a bad external
    # secret skips the complete build. Use a diagnosable step-managed container.
    if ($Content -match 'secrets\.ROADGUARD_CI_SQL_PASSWORD' -or
        $Content -match '(?ms)^\s{4}services:\s*$.*?^\s{6}mssql:\s*$') {
        $findings += 'CI workflow must use an ephemeral runner credential and a step-managed SQL container; repository-secret service containers are prohibited.'
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

    if ($servicePasswordMatches.Count -gt 1) {
        $findings += "CI workflow service container 'mssql' contains duplicate MSSQL_SA_PASSWORD configuration entries."
    } elseif ($servicePasswordMatches.Count -eq 1) {
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
            $findings += "CI workflow service container 'mssql' MSSQL_SA_PASSWORD contains a plaintext literal (found: '$rawVal')."
        }
    }

    # Connection strings may interpolate only the in-process ephemeral variable.
    if ($Content -match 'Password=(?!(\$password\b|\$\{password\}))[^;''"`\s]+') {
        $findings += "CI workflow contains hardcoded password literal in connection string."
    }

    $hasProtectedCredential =
        $Content -match 'RandomNumberGenerator' -and
        $Content -match 'RUNNER_TEMP' -and
        $Content -match 'roadguard-sql-password' -and
        $Content -match '::add-mask::' -and
        $Content -match 'chmod\s+600'
    if (-not $hasProtectedCredential) {
        $findings += 'CI workflow must generate an ephemeral SQL credential, mask it, and protect its RUNNER_TEMP file with mode-600 permissions.'
    }

    $hasManagedContainer =
        $Content -match 'docker\s+run' -and
        $Content -match '--name\s+roadguard-ci-sqlserver' -and
        $Content -match 'mcr\.microsoft\.com/mssql/server:2019-CU18-ubuntu-20\.04'
    if (-not $hasManagedContainer) {
        $findings += 'CI workflow must launch the pinned SQL image as the step-managed roadguard-ci-sqlserver container.'
    }

    $hasBoundedReadiness =
        $Content -match 'docker\s+inspect' -and
        $Content -match 'docker\s+logs' -and
        $Content -match 'Start-Sleep' -and
        $Content -match '(?i)deadline|timeout'
    if (-not $hasBoundedReadiness) {
        $findings += 'CI workflow must use a bounded health wait and emit container diagnostics on readiness failure.'
    }

    $hasUnconditionalCleanup =
        $Content -match '(?ms)-\s+name:\s*Cleanup SQL Server container and credential.*?if:\s*always\(\).*?docker\s+rm\s+--force\s+roadguard-ci-sqlserver.*?Remove-Item'
    if (-not $hasUnconditionalCleanup) {
        $findings += 'CI workflow must provide unconditional cleanup for the SQL container and ephemeral credential file.'
    }

    # Check for database connection strings or secrets at workflow-level or job-level env
    # Workflow-level env has indent 0; job-level env has indent 4.
    # Service container env (e.g., services.mssql.env) has indent >= 8 and is authorized for MSSQL_SA_PASSWORD.
    $inRootOrJobEnv = $false
    $envBlockIndent = -1
    $rootOrJobEnvLines = @()
    $inSteps = $false
    $inServices = $false

    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        $trimmed = $line.Trim()
        if ($trimmed.StartsWith("#") -or [string]::IsNullOrWhiteSpace($trimmed)) {
            continue
        }

        if ($line -match '^\s*steps:\s*$') {
            $inRootOrJobEnv = $false
            $inSteps = $true
            $inServices = $false
            continue
        }

        if (-not $inSteps) {
            # Check if entering services block (indent 4)
            if ($line -match '^ {4}services:\s*$') {
                $inServices = $true
                $inRootOrJobEnv = $false
                continue
            }

            if ($inServices) {
                # If indent is <= 4 and non-empty, we have left services
                if ($line -match '^(\s*)\S') {
                    $currIndent = $Matches[1].Length
                    if ($currIndent -le 4) {
                        $inServices = $false
                    }
                }
            }

            if (-not $inServices) {
                if (-not $inRootOrJobEnv) {
                    # Workflow-level env (indent 0) or job-level env (indent 4)
                    if ($line -match '^( {0}| {4})env:\s*$') {
                        $inRootOrJobEnv = $true
                        $envBlockIndent = $Matches[1].Length
                    }
                } else {
                    if ($line -match '^(\s*)\S') {
                        $currIndent = $Matches[1].Length
                        if ($currIndent -le $envBlockIndent) {
                            $inRootOrJobEnv = $false
                            if ($line -match '^ {4}services:\s*$') {
                                $inServices = $true
                            }
                        } else {
                            $rootOrJobEnvLines += $line
                        }
                    }
                }
            }
        }
    }

    foreach ($jLine in $rootOrJobEnvLines) {
        if ($jLine -match 'secrets\.ROADGUARD_CI_SQL_PASSWORD' -or $jLine -match 'Password=' -or $jLine -match 'ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING' -or $jLine -match 'ROADGUARD_MIGRATION_CONNECTION_STRING' -or $jLine -match 'ROADGUARD_CONNECTION_STRING') {
            $findings += "CI workflow must not expose database connection strings or secrets at job-level env. Database connection secrets must be scoped strictly to steps that require them (integration-test and seeder steps)."
            break
        }
    }

    # Step-level secret scoping: only Integration Tests and Seeder steps are allowed database secrets
    $stepsText = ""
    if ($Content -match '(?s)steps:\s*\r?\n(.*)') {
        $stepsText = $Matches[1]
    }

    $stepBlocks = [regex]::Split($stepsText, '(?m)^\s*-\s+name:\s*')
    foreach ($block in $stepBlocks) {
        if ([string]::IsNullOrWhiteSpace($block)) { continue }
        $blockLines = $block -split "`r?`n"
        $stepName = $blockLines[0].Trim()

        $hasSecret = ($block -match 'secrets\.ROADGUARD_CI_SQL_PASSWORD') -or ($block -match 'ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING') -or ($block -match 'ROADGUARD_MIGRATION_CONNECTION_STRING') -or ($block -match 'ROADGUARD_CONNECTION_STRING')

        if ($hasSecret) {
            $isAuthorized = ($stepName -match 'Integration Tests') -or ($stepName -match 'Seeder')
            if (-not $isAuthorized) {
                $findings += "CI workflow step '$stepName' must not receive database connection secret. Only integration-test and seeder steps may receive database secrets."
            }
        }
    }

    # Healthcheck must NOT specify hardcoded password literal
    if (($Content -match '--health-cmd.*-P\s+[''"][^$\\"]') -or ($Content -match '--health-cmd.*-P\s+[^''"$\\s]')) {
        $findings += "CI workflow healthcheck specifies hardcoded password literal instead of container environment variable."
    }

    # Healthcheck must NOT use Compose-style $$MSSQL_SA_PASSWORD
    if ($Content -match '\$\$MSSQL_SA_PASSWORD') {
        $findings += 'CI workflow healthcheck must not use Compose-style "$$MSSQL_SA_PASSWORD". The container shell expands a single dollar sign.'
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

    $seederBlocks = @()
    foreach ($block in $stepBlocks) {
        if ([string]::IsNullOrWhiteSpace($block)) { continue }
        $blockLines = $block -split "`r?`n"
        if ($blockLines[0].Trim() -match 'Seeder') {
            $seederBlocks += $block
        }
    }

    if ($seederBlocks.Count -ne 1) {
        $findings += 'CI workflow must define exactly one Seeder validation step.'
    } else {
        $seederBlock = $seederBlocks[0]
        if ($seederBlock -match 'Database=master(?:;|\b)') {
            $findings += 'CI Seeder validation must target a dedicated migrated application database, never master.'
        }

        $hasPinnedEfTool =
            $seederBlock -match 'dotnet\s+tool\s+install\s+dotnet-ef' -and
            $seederBlock -match '--version\s+8\.0\.17'
        if (-not $hasPinnedEfTool) {
            $findings += 'CI Seeder validation must install exact dotnet-ef version 8.0.17 in an ephemeral tool path.'
        }

        if ($seederBlock -notmatch 'ROADGUARD_MIGRATION_CONNECTION_STRING') {
            $findings += 'CI Seeder validation must scope ROADGUARD_MIGRATION_CONNECTION_STRING to the migrated application database.'
        }

        $migrationIndex = $seederBlock.IndexOf('database update', [StringComparison]::OrdinalIgnoreCase)
        $seedCommand = 'dotnet run --project tools/RoadGuardSystem.Seeder --no-build'
        $firstSeedIndex = $seederBlock.IndexOf($seedCommand, [StringComparison]::OrdinalIgnoreCase)
        if ($migrationIndex -lt 0 -or $firstSeedIndex -lt 0 -or $migrationIndex -gt $firstSeedIndex) {
            $findings += 'CI Seeder validation must apply EF migrations before invoking the production Seeder CLI.'
        }

        $seedRuns = [regex]::Matches($seederBlock, 'dotnet\s+run\s+--project\s+tools/RoadGuardSystem\.Seeder\s+--no-build', [Text.RegularExpressions.RegexOptions]::IgnoreCase).Count
        if ($seedRuns -lt 2) {
            $findings += 'CI Seeder validation must invoke the production Seeder CLI twice against the same database to prove idempotency.'
        }
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
    steps:
      - name: Run Integration Tests with coverage
        run: dotnet test
        env:
          ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING: "Server=localhost,1433;Database=master;User Id=sa;Password=`${{ secrets.ROADGUARD_CI_SQL_PASSWORD }};TrustServerCertificate=True;"
      - name: Validate Seeder Entry Point
        run: dotnet run
        env:
          ROADGUARD_CONNECTION_STRING: "Server=localhost,1433;Database=master;User Id=sa;Password=`${{ secrets.ROADGUARD_CI_SQL_PASSWORD }};TrustServerCertificate=True;"
"@

    # Fixture 6: Connection string secret exposed at job-level env
    $fixture6JobLevelSecret = @"
name: Fixture 6 Job-Level Secret
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
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P \"`$MSSQL_SA_PASSWORD\" -Q 'SELECT 1'"
    env:
      ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING: "Server=localhost,1433;Database=master;User Id=sa;Password=`${{ secrets.ROADGUARD_CI_SQL_PASSWORD }};TrustServerCertificate=True;"
    steps:
      - name: Run Integration Tests with coverage
        run: dotnet test
      - name: Validate Seeder Entry Point
        run: dotnet run
"@

    # Fixture 7: Database secret exposed to unauthorized step (e.g. unit tests step)
    $fixture7UnauthorizedStepSecret = @"
name: Fixture 7 Unauthorized Step Secret
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
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P \"`$MSSQL_SA_PASSWORD\" -Q 'SELECT 1'"
    steps:
      - name: Run Unit Tests with coverage
        run: dotnet test
        env:
          ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING: "Server=localhost,1433;Database=master;User Id=sa;Password=`${{ secrets.ROADGUARD_CI_SQL_PASSWORD }};TrustServerCertificate=True;"
      - name: Run Integration Tests with coverage
        run: dotnet test
        env:
          ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING: "Server=localhost,1433;Database=master;User Id=sa;Password=`${{ secrets.ROADGUARD_CI_SQL_PASSWORD }};TrustServerCertificate=True;"
      - name: Validate Seeder Entry Point
        run: dotnet run
        env:
          ROADGUARD_CONNECTION_STRING: "Server=localhost,1433;Database=master;User Id=sa;Password=`${{ secrets.ROADGUARD_CI_SQL_PASSWORD }};TrustServerCertificate=True;"
"@

    # Fixture 8: Repository secret and pre-step service container reproduce the hosted credential mismatch boundary
    $fixture8RepositorySecretService = @"
name: Fixture 8 Repository Secret Service
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
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P \"`$MSSQL_SA_PASSWORD\" -Q 'SELECT 1'"
"@

    # Fixture 9: Ephemeral credential lacks masking and restrictive file permissions
    $fixture9UnprotectedCredential = @"
name: Fixture 9 Unprotected Credential
on: [ push ]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Generate ephemeral SQL credential
        shell: pwsh
        run: |
          `$password = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
          [IO.File]::WriteAllText((Join-Path `$env:RUNNER_TEMP 'roadguard-sql-password'), `$password)
"@

    # Fixture 10: Container starts without bounded health wait or failure diagnostics
    $fixture10UnboundedStartup = @"
name: Fixture 10 Unbounded Startup
on: [ push ]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Start SQL Server container
        run: docker run --detach --name roadguard-ci-sqlserver mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04
"@

    # Fixture 11: Workflow has no unconditional container and credential cleanup
    $fixture11MissingCleanup = @"
name: Fixture 11 Missing Cleanup
on: [ push ]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Generate ephemeral SQL credential
        run: Write-Output '::add-mask::fixture'
      - name: Start SQL Server container
        run: docker run --detach --name roadguard-ci-sqlserver mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04
      - name: Run Integration Tests with coverage
        run: dotnet test
"@

    # Fixture 12: Seeder points at reachable master without applying the application schema
    $fixture12UnmigratedMasterSeeder = @"
name: Fixture 12 Unmigrated Master Seeder
on: [ push ]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Validate Seeder Entry Point
        shell: pwsh
        run: |
          `$env:ROADGUARD_CONNECTION_STRING = "Server=localhost;Database=master;User Id=sa;Password=`$password;TrustServerCertificate=True;"
          dotnet run --project tools/RoadGuardSystem.Seeder --no-build
"@

    $res1 = Test-CiWorkflowContent $fixture1Hardcoded
    $res2 = Test-CiWorkflowContent $fixture2ComposeStyle
    $res3 = Test-CiWorkflowContent $fixture3SingleQuoted
    $res4 = Test-CiWorkflowContent $fixture4CliArgs
    $res5 = Test-CiWorkflowContent $fixture5UnquotedServicePassword
    $res6 = Test-CiWorkflowContent $fixture6JobLevelSecret
    $res7 = Test-CiWorkflowContent $fixture7UnauthorizedStepSecret
    $res8 = Test-CiWorkflowContent $fixture8RepositorySecretService
    $res9 = Test-CiWorkflowContent $fixture9UnprotectedCredential
    $res10 = Test-CiWorkflowContent $fixture10UnboundedStartup
    $res11 = Test-CiWorkflowContent $fixture11MissingCleanup
    $res12 = Test-CiWorkflowContent $fixture12UnmigratedMasterSeeder

    $detected1 = ($res1 | Where-Object { $_ -match "hardcoded password literal" }).Count -gt 0
    $detected2 = ($res2 | Where-Object { $_ -match "Compose-style" }).Count -gt 0
    $detected3 = ($res3 | Where-Object { $_ -match "single quote" -or $_ -match "single-quote" }).Count -gt 0
    $detected4 = ($res4 | Where-Object { $_ -match "CLI argument" }).Count -gt 0
    $detected5 = ($res5 | Where-Object { $_ -match "service container 'mssql' MSSQL_SA_PASSWORD" }).Count -gt 0
    $detected6 = ($res6 | Where-Object { $_ -match "job-level env" }).Count -gt 0
    $detected7 = ($res7 | Where-Object { $_ -match "must not receive database connection secret" }).Count -gt 0
    $detected8 = ($res8 | Where-Object { $_ -match "ephemeral runner credential" }).Count -gt 0
    $detected9 = ($res9 | Where-Object { $_ -match "mask.*mode-600" }).Count -gt 0
    $detected10 = ($res10 | Where-Object { $_ -match "bounded health wait.*diagnostic" }).Count -gt 0
    $detected11 = ($res11 | Where-Object { $_ -match "unconditional cleanup" }).Count -gt 0
    $detected12 = ($res12 | Where-Object { $_ -match "dedicated migrated application database" }).Count -gt 0

    $testCases = @(
        @{ Name = '1. Hardcoded password literal in healthcheck'; Detected = $detected1; Findings = $res1 },
        @{ Name = '2. Compose-style "$$MSSQL_SA_PASSWORD" in healthcheck'; Detected = $detected2; Findings = $res2 },
        @{ Name = '3. Single-quoted ''$MSSQL_SA_PASSWORD'' in healthcheck'; Detected = $detected3; Findings = $res3 },
        @{ Name = '4. CLI connection-string argument in seeder invocation'; Detected = $detected4; Findings = $res4 },
        @{ Name = '5. Unquoted service password literal while connection string uses secret'; Detected = $detected5; Findings = $res5 },
        @{ Name = '6. Connection string secret exposed at job-level env'; Detected = $detected6; Findings = $res6 },
        @{ Name = '7. Database secret exposed to unauthorized step (e.g. unit test step)'; Detected = $detected7; Findings = $res7 },
        @{ Name = '8. Repository-secret service container instead of ephemeral runner credential'; Detected = $detected8; Findings = $res8 },
        @{ Name = '9. Ephemeral credential is not masked and mode-600'; Detected = $detected9; Findings = $res9 },
        @{ Name = '10. SQL startup lacks bounded health wait and diagnostics'; Detected = $detected10; Findings = $res10 },
        @{ Name = '11. Workflow lacks unconditional container and credential cleanup'; Detected = $detected11; Findings = $res11 },
        @{ Name = '12. Seeder targets unmigrated master database'; Detected = $detected12; Findings = $res12 }
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

# 5. Step-managed SQL container verification: exact pinned image and healthcheck
$expectedSqlImage = "mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04"
if ($ciContent -notmatch [regex]::Escape($expectedSqlImage)) {
    $errors += "CI workflow SQL container must use pinned image: '$expectedSqlImage'"
}
if ($ciContent -notmatch "--health-cmd") {
    $errors += "CI workflow SQL Server container must specify a healthcheck command."
}

# Apply shared credential, secret, healthcheck, and CLI rules
$contentViolations = Test-CiWorkflowContent $ciContent
if ($contentViolations.Count -gt 0) {
    $errors += $contentViolations
}

# 6. Pipeline steps verification. Jobs run independently, so only the SQL
# lifecycle has a meaningful cross-step order; other required steps may live
# in their dedicated verify, unit, API, or integration job.
$pipelineTokens = @(
    "actions/checkout@v4",
    "actions/setup-dotnet@v4",
    "Verify-P102Docs.ps1",
    "Verify-DockerCompose.ps1",
    "Verify-CiWorkflow.ps1",
    "Verify-DependencySecurity.ps1",
    "Generate ephemeral SQL credential",
    "Start SQL Server container",
    "Wait for SQL Server readiness",
    "dotnet restore",
    "dotnet format --verify-no-changes",
    "dotnet build",
    "--no-incremental",
    "dotnet test",
    "XPlat Code Coverage",
    "actions/upload-artifact@v4",
    "Cleanup SQL Server container and credential"
)

foreach ($token in $pipelineTokens) {
    $currentIndex = $ciContent.IndexOf($token)
    if ($currentIndex -lt 0) {
        $errors += "CI workflow missing required pipeline token/step: '$token'"
    }
}

$integrationMatch = [regex]::Match(
    $ciContent,
    '(?ms)^ {2}integration:\s*\r?\n(?<body>.*?)(?=^ {2}\S|\z)')
if (-not $integrationMatch.Success) {
    $errors += "CI workflow must define an integration job for the managed SQL lifecycle."
} else {
    $integrationBody = $integrationMatch.Groups['body'].Value
    $integrationTokens = @(
        "Generate ephemeral SQL credential",
        "Start SQL Server container",
        "Wait for SQL Server readiness",
        "dotnet restore",
        "dotnet build",
        "Run integration tests",
        "Validate Seeder",
        "Cleanup SQL Server container and credential"
    )
    $lastIndex = -1
    foreach ($token in $integrationTokens) {
        $currentIndex = $integrationBody.IndexOf($token)
        if ($currentIndex -lt 0) {
            $errors += "CI workflow integration job missing required lifecycle token/step: '$token'"
        } elseif ($currentIndex -lt $lastIndex) {
            $errors += "CI workflow integration lifecycle step out of sequence: '$token' appeared before the previous required step."
        } else {
            $lastIndex = $currentIndex
        }
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
