[CmdletBinding()]
param (
    [string]$ProjectPath = "tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj",
    [string[]]$BlockedSeverities = @("High", "Critical"),
    [string]$JsonInput = "",
    [switch]$SelfTest
)

$ErrorActionPreference = "Stop"

function Invoke-VulnerabilityVerification {
    param (
        [string]$TargetProjectPath,
        [string[]]$TargetBlockedSeverities,
        [string]$RawJsonContent
    )

    $jsonText = ""

    if (-not [string]::IsNullOrWhiteSpace($RawJsonContent)) {
        $jsonText = $RawJsonContent
    }
    else {
        if (-not (Test-Path $TargetProjectPath)) {
            Write-Host "FAILURE: Target project path does not exist: '$TargetProjectPath'" -ForegroundColor Red
            return @{ ExitCode = 2; Success = $false; Findings = @() }
        }

        Write-Host "Running dependency vulnerability scan on: $TargetProjectPath" -ForegroundColor Cyan

        # Execute dotnet list with --format json
        $scanOutput = & dotnet list $TargetProjectPath package --vulnerable --include-transitive --format json 2>&1
        $scannerExitCode = $LASTEXITCODE

        if ($scannerExitCode -ne 0) {
            Write-Host "FAILURE: Scanner process failed with exit code $scannerExitCode on target: '$TargetProjectPath'" -ForegroundColor Red
            return @{ ExitCode = 1; Success = $false; Findings = @() }
        }

        $jsonText = $scanOutput -join "`n"
    }

    # Parse JSON fail-closed
    $parsedJson = $null
    try {
        $parsedJson = $jsonText | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        Write-Host "FAILURE: Failed to parse vulnerability scanner JSON output: $($_.Exception.Message)" -ForegroundColor Red
        return @{ ExitCode = 1; Success = $false; Findings = @() }
    }

    if ($null -eq $parsedJson) {
        Write-Host "FAILURE: Scanner JSON output is null or empty." -ForegroundColor Red
        return @{ ExitCode = 1; Success = $false; Findings = @() }
    }

    # Fail-closed if scanner reports problem records
    if ($parsedJson.problems -and $parsedJson.problems.Count -gt 0) {
        Write-Host "FAILURE: Scanner reported problem(s):" -ForegroundColor Red
        foreach ($prob in $parsedJson.problems) {
            Write-Host "  - [$($prob.level)] $($prob.text)" -ForegroundColor Red
        }
        return @{ ExitCode = 1; Success = $false; Findings = @() }
    }

    $findings = @()

    if ($parsedJson.projects) {
        foreach ($project in $parsedJson.projects) {
            $projPath = $project.path
            if ($project.frameworks) {
                foreach ($framework in $project.frameworks) {
                    $fwName = $framework.framework

                    # Process both topLevelPackages and transitivePackages
                    $packageGroups = @(
                        @{ Type = "Top-level"; Packages = $framework.topLevelPackages },
                        @{ Type = "Transitive"; Packages = $framework.transitivePackages }
                    )

                    foreach ($group in $packageGroups) {
                        if ($group.Packages) {
                            foreach ($pkg in $group.Packages) {
                                if ($pkg.vulnerabilities) {
                                    foreach ($vuln in $pkg.vulnerabilities) {
                                        if ($TargetBlockedSeverities -contains $vuln.severity) {
                                            $advisory = if ($vuln.advisoryurl) { $vuln.advisoryurl } elseif ($vuln.advisoryUrl) { $vuln.advisoryUrl } else { "N/A" }
                                            $findings += [PSCustomObject]@{
                                                Project     = $projPath
                                                Framework   = $fwName
                                                Type        = $group.Type
                                                Package     = $pkg.id
                                                Resolved    = $pkg.resolvedVersion
                                                Severity    = $vuln.severity
                                                AdvisoryUrl = $advisory
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    if ($findings.Count -gt 0) {
        Write-Host "FAILURE: Detected $($findings.Count) vulnerable package(s) matching severities ($($TargetBlockedSeverities -join ', ')):" -ForegroundColor Red
        foreach ($f in $findings) {
            Write-Host "  - [$($f.Type)] $($f.Package) (Resolved: $($f.Resolved)) | Severity: $($f.Severity) | Advisory: $($f.AdvisoryUrl)" -ForegroundColor Red
        }
        return @{ ExitCode = 1; Success = $false; Findings = $findings }
    }

    Write-Host "SUCCESS: No vulnerable dependencies with severity ($($TargetBlockedSeverities -join ', ')) detected in $TargetProjectPath." -ForegroundColor Green
    return @{ ExitCode = 0; Success = $true; Findings = @() }
}

if ($SelfTest) {
    Write-Host "=== EXECUTING DEPENDENCY SECURITY GATE REGRESSION SUITE ===" -ForegroundColor Cyan
    $testResults = @()

    # Case 1: Existing non-project file (AGENTS.md) must fail with non-zero code and not output SUCCESS
    $case1Res = Invoke-VulnerabilityVerification -TargetProjectPath "AGENTS.md" -TargetBlockedSeverities $BlockedSeverities -RawJsonContent ""
    $case1Pass = ($case1Res.ExitCode -ne 0) -and ($case1Res.Success -eq $false)
    $testResults += [PSCustomObject]@{
        Case = "1. Non-project existing path (AGENTS.md) fails non-zero"
        ExitCode = $case1Res.ExitCode
        Success = $case1Res.Success
        Status = if ($case1Pass) { "PASS" } else { "FAIL" }
    }

    # Case 2: JSON fixture with topLevelPackages containing High/Critical must be blocked
    $fixtureTopLevel = @'
{
  "version": 1,
  "projects": [
    {
      "path": "test/Project.csproj",
      "frameworks": [
        {
          "framework": "net8.0",
          "topLevelPackages": [
            {
              "id": "Vulnerable.TopPackage",
              "resolvedVersion": "1.0.0",
              "vulnerabilities": [
                {
                  "severity": "High",
                  "advisoryurl": "https://github.com/advisories/GHSA-top-test"
                }
              ]
            }
          ]
        }
      ]
    }
  ]
}
'@
    $case2Res = Invoke-VulnerabilityVerification -TargetProjectPath "mock.csproj" -TargetBlockedSeverities @("High", "Critical") -RawJsonContent $fixtureTopLevel
    $case2Pass = ($case2Res.ExitCode -eq 1) -and ($case2Res.Success -eq $false) -and ($case2Res.Findings.Count -eq 1)
    $testResults += [PSCustomObject]@{
        Case = "2. JSON fixture with topLevelPackages High/Critical blocked"
        ExitCode = $case2Res.ExitCode
        Success = $case2Res.Success
        Status = if ($case2Pass) { "PASS" } else { "FAIL" }
    }

    # Case 3: JSON fixture with transitivePackages containing High/Critical must be blocked
    $fixtureTransitive = @'
{
  "version": 1,
  "projects": [
    {
      "path": "test/Project.csproj",
      "frameworks": [
        {
          "framework": "net8.0",
          "transitivePackages": [
            {
              "id": "SSH.NET",
              "resolvedVersion": "2023.0.0",
              "vulnerabilities": [
                {
                  "severity": "High",
                  "advisoryurl": "https://github.com/advisories/GHSA-q939-rpr3-3284"
                }
              ]
            }
          ]
        }
      ]
    }
  ]
}
'@
    $case3Res = Invoke-VulnerabilityVerification -TargetProjectPath "mock.csproj" -TargetBlockedSeverities @("High", "Critical") -RawJsonContent $fixtureTransitive
    $case3Pass = ($case3Res.ExitCode -eq 1) -and ($case3Res.Success -eq $false) -and ($case3Res.Findings.Count -eq 1)
    $testResults += [PSCustomObject]@{
        Case = "3. JSON fixture with transitivePackages High/Critical blocked"
        ExitCode = $case3Res.ExitCode
        Success = $case3Res.Success
        Status = if ($case3Pass) { "PASS" } else { "FAIL" }
    }

    # Case 4: Clean JSON fixture without blocked vulnerabilities must exit with code 0
    $fixtureClean = @'
{
  "version": 1,
  "projects": [
    {
      "path": "test/Project.csproj",
      "frameworks": [
        {
          "framework": "net8.0",
          "transitivePackages": [
            {
              "id": "Safe.Package",
              "resolvedVersion": "2.0.0",
              "vulnerabilities": [
                {
                  "severity": "Low",
                  "advisoryurl": "https://github.com/advisories/GHSA-low-ignore"
                }
              ]
            }
          ]
        }
      ]
    }
  ]
}
'@
    $case4Res = Invoke-VulnerabilityVerification -TargetProjectPath "mock.csproj" -TargetBlockedSeverities @("High", "Critical") -RawJsonContent $fixtureClean
    $case4Pass = ($case4Res.ExitCode -eq 0) -and ($case4Res.Success -eq $true)
    $testResults += [PSCustomObject]@{
        Case = "4. Clean JSON fixture without blocked vulnerabilities exits 0"
        ExitCode = $case4Res.ExitCode
        Success = $case4Res.Success
        Status = if ($case4Pass) { "PASS" } else { "FAIL" }
    }

    # Case 5: Corrupt / unparseable JSON and scanner problems must fail-closed with non-zero exit code
    $case5Res1 = Invoke-VulnerabilityVerification -TargetProjectPath "mock.csproj" -TargetBlockedSeverities $BlockedSeverities -RawJsonContent "{ invalid json string ..."
    $case5Res2 = Invoke-VulnerabilityVerification -TargetProjectPath "mock.csproj" -TargetBlockedSeverities $BlockedSeverities -RawJsonContent '{"version":1,"problems":[{"text":"Fatal scanner crash","level":"error"}]}'
    $case5Pass = ($case5Res1.ExitCode -ne 0) -and ($case5Res1.Success -eq $false) -and ($case5Res2.ExitCode -ne 0) -and ($case5Res2.Success -eq $false)
    $testResults += [PSCustomObject]@{
        Case = "5. Corrupted JSON / scanner problems fail closed"
        ExitCode = "$($case5Res1.ExitCode),$($case5Res2.ExitCode)"
        Success = $case5Res1.Success
        Status = if ($case5Pass) { "PASS" } else { "FAIL" }
    }

    Write-Host "`n=== REGRESSION SUITE RESULTS ===" -ForegroundColor Cyan
    $testResults | Format-Table -AutoSize

    $failedCount = ($testResults | Where-Object { $_.Status -ne "PASS" }).Count
    if ($failedCount -gt 0) {
        Write-Host "FAILURE: $failedCount regression test(s) failed." -ForegroundColor Red
        exit 1
    }

    Write-Host "SUCCESS: All 5 dependency security gate regression tests passed!" -ForegroundColor Green
    exit 0
}

# Standard execution path
$result = Invoke-VulnerabilityVerification -TargetProjectPath $ProjectPath -TargetBlockedSeverities $BlockedSeverities -RawJsonContent $JsonInput
exit $result.ExitCode
