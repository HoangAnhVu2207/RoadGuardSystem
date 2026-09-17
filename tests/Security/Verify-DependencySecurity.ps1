[CmdletBinding()]
param (
    [string]$ProjectPath = "tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj",
    [string[]]$BlockedSeverities = @("High", "Critical"),
    [switch]$SelfTest
)

$ErrorActionPreference = "Stop"

function Test-VulnerabilityReportJson {
    param (
        [string]$JsonContent,
        [string[]]$TargetBlockedSeverities = @("High", "Critical")
    )

    if ([string]::IsNullOrWhiteSpace($JsonContent)) {
        return @{
            ExitCode = 1
            Success  = $false
            Message  = "Scanner JSON output is null, empty, or whitespace."
            Findings = @()
        }
    }

    # 1. Parse JSON fail-closed
    $parsedJson = $null
    try {
        $parsedJson = $JsonContent | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        return @{
            ExitCode = 1
            Success  = $false
            Message  = "Failed to parse vulnerability scanner JSON output: $($_.Exception.Message)"
            Findings = @()
        }
    }

    if ($null -eq $parsedJson) {
        return @{
            ExitCode = 1
            Success  = $false
            Message  = "Parsed scanner JSON is null."
            Findings = @()
        }
    }

    # 2. Schema check: version must exist, be numeric or string, and equal 1
    $hasVersion = $false
    if ($parsedJson.PSObject -and $parsedJson.PSObject.Properties['version']) {
        $hasVersion = $true
    }
    if (-not $hasVersion -or $null -eq $parsedJson.version -or [string]::IsNullOrWhiteSpace("$($parsedJson.version)") -or $parsedJson.version -ne 1) {
        return @{
            ExitCode = 1
            Success  = $false
            Message  = "Scanner JSON missing or invalid schema version. Expected 'version: 1', found: '$($parsedJson.version)'."
            Findings = @()
        }
    }

    # 3. Fail-closed if scanner reports problem records
    if ($parsedJson.PSObject.Properties['problems'] -and $parsedJson.problems -and $parsedJson.problems.Count -gt 0) {
        $probList = @()
        foreach ($prob in $parsedJson.problems) {
            $probList += "[$($prob.level)] $($prob.text)"
        }
        return @{
            ExitCode = 1
            Success  = $false
            Message  = "Scanner reported problem(s): $($probList -join '; ')"
            Findings = @()
        }
    }

    # 4. Schema check: projects must exist, be an array/collection, not null, and not empty
    $hasProjects = $false
    if ($parsedJson.PSObject -and $parsedJson.PSObject.Properties['projects']) {
        $hasProjects = $true
    }
    if (-not $hasProjects -or $null -eq $parsedJson.projects -or $parsedJson.projects.Count -eq 0) {
        return @{
            ExitCode = 1
            Success  = $false
            Message  = "Scanner JSON missing or empty 'projects' array."
            Findings = @()
        }
    }

    # 5. Schema check: each project must have non-empty 'path'
    foreach ($project in $parsedJson.projects) {
        $hasPath = $false
        if ($project.PSObject -and $project.PSObject.Properties['path']) {
            $hasPath = $true
        }
        if (-not $hasPath -or [string]::IsNullOrWhiteSpace($project.path)) {
            return @{
                ExitCode = 1
                Success  = $false
                Message  = "Scanner JSON contains a project entry missing required 'path' property."
                Findings = @()
            }
        }
    }

    # 6. Check for vulnerabilities in frameworks (if frameworks property exists)
    $findings = @()
    foreach ($project in $parsedJson.projects) {
        $projPath = $project.path
        if ($project.PSObject -and $project.PSObject.Properties['frameworks'] -and $project.frameworks) {
            foreach ($framework in $project.frameworks) {
                $fwName = $framework.framework

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

    if ($findings.Count -gt 0) {
        return @{
            ExitCode = 1
            Success  = $false
            Message  = "Detected $($findings.Count) vulnerable package(s) matching severities ($($TargetBlockedSeverities -join ', '))."
            Findings = $findings
        }
    }

    return @{
        ExitCode = 0
        Success  = $true
        Message  = "No vulnerable dependencies with severity ($($TargetBlockedSeverities -join ', ')) detected."
        Findings = @()
    }
}

function Format-VulnerabilityScanResult {
    param (
        [hashtable]$Result,
        [string]$TargetProjectPath,
        [string[]]$TargetBlockedSeverities
    )

    $lines = @()
    if (-not $Result.Success) {
        $lines += "FAILURE: $($Result.Message)"
        if ($Result.Findings -and $Result.Findings.Count -gt 0) {
            $lines += "Detected $($Result.Findings.Count) vulnerable package(s) matching severities ($($TargetBlockedSeverities -join ', ')):"
            foreach ($f in $Result.Findings) {
                $lines += "  - [$($f.Type)] $($f.Package) (Resolved: $($f.Resolved)) | Severity: $($f.Severity) | Advisory: $($f.AdvisoryUrl)"
            }
        }
    }
    else {
        $lines += "SUCCESS: No vulnerable dependencies with severity ($($TargetBlockedSeverities -join ', ')) detected in $TargetProjectPath."
    }
    return ($lines -join "`n")
}

if ($SelfTest) {
    Write-Host "=== EXECUTING DEPENDENCY SECURITY GATE REGRESSION SUITE ===" -ForegroundColor Cyan
    $testResults = @()
    $scriptPath = if ($PSCommandPath) { $PSCommandPath } else { $MyInvocation.MyCommand.Definition }

    function Invoke-CliCommandSafe {
        param (
            [string]$Command,
            [string]$Arguments
        )
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = $Command
        $psi.Arguments = $Arguments
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true

        $proc = [System.Diagnostics.Process]::Start($psi)
        $stdout = $proc.StandardOutput.ReadToEnd()
        $stderr = $proc.StandardError.ReadToEnd()
        $proc.WaitForExit()

        return [PSCustomObject]@{
            ExitCode = $proc.ExitCode
            Output   = ($stdout + "`n" + $stderr).Trim()
        }
    }

    # Case 1: Standard CLI rejects test-only -JsonInput parameter and does not output SUCCESS
    $case1Res = Invoke-CliCommandSafe -Command "powershell.exe" -Arguments "-ExecutionPolicy Bypass -File `"$scriptPath`" -JsonInput `"{}`""
    $case1Pass = ($case1Res.ExitCode -ne 0) -and ($case1Res.Output -notmatch "SUCCESS") -and ($case1Res.Output -match "JsonInput")
    $testResults += [PSCustomObject]@{
        Case      = "1. Standard CLI rejects test-only -JsonInput parameter"
        ExitCode  = $case1Res.ExitCode
        NoSuccess = ($case1Res.Output -notmatch "SUCCESS")
        Status    = if ($case1Pass) { "PASS" } else { "FAIL" }
    }

    # Case 2: Non-project existing path (AGENTS.md) causes scanner to fail non-zero without SUCCESS
    $case2Res = Invoke-CliCommandSafe -Command "powershell.exe" -Arguments "-ExecutionPolicy Bypass -File `"$scriptPath`" -ProjectPath `"AGENTS.md`""
    $case2Pass = ($case2Res.ExitCode -ne 0) -and ($case2Res.Output -notmatch "SUCCESS") -and ($case2Res.Output -match "FAILURE")
    $testResults += [PSCustomObject]@{
        Case      = "2. Non-project path (AGENTS.md) fails scanner non-zero"
        ExitCode  = $case2Res.ExitCode
        NoSuccess = ($case2Res.Output -notmatch "SUCCESS")
        Status    = if ($case2Pass) { "PASS" } else { "FAIL" }
    }

    # Case 3: Schema: empty JSON object {} fails non-zero without SUCCESS
    $case3Res = Test-VulnerabilityReportJson -JsonContent "{}" -TargetBlockedSeverities $BlockedSeverities
    $case3Text = Format-VulnerabilityScanResult -Result $case3Res -TargetProjectPath "mock.csproj" -TargetBlockedSeverities $BlockedSeverities
    $case3Pass = ($case3Res.ExitCode -ne 0) -and ($case3Res.Success -eq $false) -and ($case3Text -notmatch "SUCCESS")
    $testResults += [PSCustomObject]@{
        Case      = "3. Schema: empty JSON object {} fails non-zero"
        ExitCode  = $case3Res.ExitCode
        NoSuccess = ($case3Text -notmatch "SUCCESS")
        Status    = if ($case3Pass) { "PASS" } else { "FAIL" }
    }

    # Case 4: Schema: missing version fails non-zero without SUCCESS
    $case4Res = Test-VulnerabilityReportJson -JsonContent '{"projects":[{"path":"test.csproj"}]}' -TargetBlockedSeverities $BlockedSeverities
    $case4Text = Format-VulnerabilityScanResult -Result $case4Res -TargetProjectPath "mock.csproj" -TargetBlockedSeverities $BlockedSeverities
    $case4Pass = ($case4Res.ExitCode -ne 0) -and ($case4Res.Success -eq $false) -and ($case4Text -notmatch "SUCCESS")
    $testResults += [PSCustomObject]@{
        Case      = "4. Schema: missing version fails non-zero"
        ExitCode  = $case4Res.ExitCode
        NoSuccess = ($case4Text -notmatch "SUCCESS")
        Status    = if ($case4Pass) { "PASS" } else { "FAIL" }
    }

    # Case 5: Schema: empty projects array [] fails non-zero without SUCCESS
    $case5Res = Test-VulnerabilityReportJson -JsonContent '{"version":1,"projects":[]}' -TargetBlockedSeverities $BlockedSeverities
    $case5Text = Format-VulnerabilityScanResult -Result $case5Res -TargetProjectPath "mock.csproj" -TargetBlockedSeverities $BlockedSeverities
    $case5Pass = ($case5Res.ExitCode -ne 0) -and ($case5Res.Success -eq $false) -and ($case5Text -notmatch "SUCCESS")
    $testResults += [PSCustomObject]@{
        Case      = "5. Schema: empty projects [] fails non-zero"
        ExitCode  = $case5Res.ExitCode
        NoSuccess = ($case5Text -notmatch "SUCCESS")
        Status    = if ($case5Pass) { "PASS" } else { "FAIL" }
    }

    # Case 6: Schema: project entry missing path fails non-zero without SUCCESS
    $case6Res = Test-VulnerabilityReportJson -JsonContent '{"version":1,"projects":[{}]}' -TargetBlockedSeverities $BlockedSeverities
    $case6Text = Format-VulnerabilityScanResult -Result $case6Res -TargetProjectPath "mock.csproj" -TargetBlockedSeverities $BlockedSeverities
    $case6Pass = ($case6Res.ExitCode -ne 0) -and ($case6Res.Success -eq $false) -and ($case6Text -notmatch "SUCCESS")
    $testResults += [PSCustomObject]@{
        Case      = "6. Schema: project missing path fails non-zero"
        ExitCode  = $case6Res.ExitCode
        NoSuccess = ($case6Text -notmatch "SUCCESS")
        Status    = if ($case6Pass) { "PASS" } else { "FAIL" }
    }

    # Case 7: Parser: corrupt / unparseable JSON fails non-zero without SUCCESS
    $case7Res = Test-VulnerabilityReportJson -JsonContent "{ invalid json string ..." -TargetBlockedSeverities $BlockedSeverities
    $case7Text = Format-VulnerabilityScanResult -Result $case7Res -TargetProjectPath "mock.csproj" -TargetBlockedSeverities $BlockedSeverities
    $case7Pass = ($case7Res.ExitCode -ne 0) -and ($case7Res.Success -eq $false) -and ($case7Text -notmatch "SUCCESS")
    $testResults += [PSCustomObject]@{
        Case      = "7. Parser: corrupt JSON fails non-zero"
        ExitCode  = $case7Res.ExitCode
        NoSuccess = ($case7Text -notmatch "SUCCESS")
        Status    = if ($case7Pass) { "PASS" } else { "FAIL" }
    }

    # Case 8: Parser: scanner problems array fails non-zero without SUCCESS
    $case8Res = Test-VulnerabilityReportJson -JsonContent '{"version":1,"problems":[{"text":"Fatal scanner crash","level":"error"}]}' -TargetBlockedSeverities $BlockedSeverities
    $case8Text = Format-VulnerabilityScanResult -Result $case8Res -TargetProjectPath "mock.csproj" -TargetBlockedSeverities $BlockedSeverities
    $case8Pass = ($case8Res.ExitCode -ne 0) -and ($case8Res.Success -eq $false) -and ($case8Text -notmatch "SUCCESS")
    $testResults += [PSCustomObject]@{
        Case      = "8. Parser: scanner problems array fails non-zero"
        ExitCode  = $case8Res.ExitCode
        NoSuccess = ($case8Text -notmatch "SUCCESS")
        Status    = if ($case8Pass) { "PASS" } else { "FAIL" }
    }

    # Case 9: Findings: topLevelPackages containing High/Critical blocked without SUCCESS
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
    $case9Res = Test-VulnerabilityReportJson -JsonContent $fixtureTopLevel -TargetBlockedSeverities @("High", "Critical")
    $case9Text = Format-VulnerabilityScanResult -Result $case9Res -TargetProjectPath "test/Project.csproj" -TargetBlockedSeverities @("High", "Critical")
    $case9Pass = ($case9Res.ExitCode -eq 1) -and ($case9Res.Success -eq $false) -and ($case9Res.Findings.Count -eq 1) -and ($case9Text -notmatch "SUCCESS")
    $testResults += [PSCustomObject]@{
        Case      = "9. Findings: topLevelPackages High/Critical blocked"
        ExitCode  = $case9Res.ExitCode
        NoSuccess = ($case9Text -notmatch "SUCCESS")
        Status    = if ($case9Pass) { "PASS" } else { "FAIL" }
    }

    # Case 10: Findings: transitivePackages containing High/Critical blocked without SUCCESS
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
    $case10Res = Test-VulnerabilityReportJson -JsonContent $fixtureTransitive -TargetBlockedSeverities @("High", "Critical")
    $case10Text = Format-VulnerabilityScanResult -Result $case10Res -TargetProjectPath "test/Project.csproj" -TargetBlockedSeverities @("High", "Critical")
    $case10Pass = ($case10Res.ExitCode -eq 1) -and ($case10Res.Success -eq $false) -and ($case10Res.Findings.Count -eq 1) -and ($case10Text -notmatch "SUCCESS")
    $testResults += [PSCustomObject]@{
        Case      = "10. Findings: transitivePackages High/Critical blocked"
        ExitCode  = $case10Res.ExitCode
        NoSuccess = ($case10Text -notmatch "SUCCESS")
        Status    = if ($case10Pass) { "PASS" } else { "FAIL" }
    }

    # Case 11: Positive: Real clean JSON format (project/path present without frameworks) exits 0 with SUCCESS
    $fixtureRealCleanNoFrameworks = @'
{
  "version": 1,
  "parameters": "--vulnerable --include-transitive",
  "sources": [
    "https://api.nuget.org/v3/index.json"
  ],
  "projects": [
    {
      "path": "D:/Project BE/RoadGuardSystem/tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj"
    }
  ]
}
'@
    $case11Res = Test-VulnerabilityReportJson -JsonContent $fixtureRealCleanNoFrameworks -TargetBlockedSeverities @("High", "Critical")
    $case11Text = Format-VulnerabilityScanResult -Result $case11Res -TargetProjectPath "test/Project.csproj" -TargetBlockedSeverities @("High", "Critical")
    $case11Pass = ($case11Res.ExitCode -eq 0) -and ($case11Res.Success -eq $true) -and ($case11Text -match "SUCCESS")
    $testResults += [PSCustomObject]@{
        Case      = "11. Positive: Real clean JSON without frameworks passes"
        ExitCode  = $case11Res.ExitCode
        NoSuccess = ($case11Text -notmatch "SUCCESS")
        Status    = if ($case11Pass) { "PASS" } else { "FAIL" }
    }

    # Case 12: Positive: Clean JSON fixture with frameworks and safe/low vulnerabilities exits 0 with SUCCESS
    $fixtureCleanLowOnly = @'
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
    $case12Res = Test-VulnerabilityReportJson -JsonContent $fixtureCleanLowOnly -TargetBlockedSeverities @("High", "Critical")
    $case12Text = Format-VulnerabilityScanResult -Result $case12Res -TargetProjectPath "test/Project.csproj" -TargetBlockedSeverities @("High", "Critical")
    $case12Pass = ($case12Res.ExitCode -eq 0) -and ($case12Res.Success -eq $true) -and ($case12Text -match "SUCCESS")
    $testResults += [PSCustomObject]@{
        Case      = "12. Positive: Clean JSON with Low-only vulnerabilities passes"
        ExitCode  = $case12Res.ExitCode
        NoSuccess = ($case12Text -notmatch "SUCCESS")
        Status    = if ($case12Pass) { "PASS" } else { "FAIL" }
    }

    Write-Host "`n=== REGRESSION SUITE RESULTS ===" -ForegroundColor Cyan
    $testResults | Format-Table -AutoSize

    $failedCount = ($testResults | Where-Object { $_.Status -ne "PASS" }).Count
    if ($failedCount -gt 0) {
        Write-Host "FAILURE: $failedCount regression test(s) failed." -ForegroundColor Red
        exit 1
    }

    Write-Host "SUCCESS: All $($testResults.Count) dependency security gate regression tests passed!" -ForegroundColor Green
    exit 0
}

# Standard execution path
if (-not (Test-Path $ProjectPath)) {
    Write-Host "FAILURE: Target project path does not exist: '$ProjectPath'" -ForegroundColor Red
    exit 2
}

Write-Host "Running dependency vulnerability scan on: $ProjectPath" -ForegroundColor Cyan

# Standard execution MUST always run dotnet list ... --format json
$scanOutput = & dotnet list $ProjectPath package --vulnerable --include-transitive --format json 2>&1
$scannerExitCode = $LASTEXITCODE

if ($scannerExitCode -ne 0) {
    Write-Host "FAILURE: Scanner process failed with exit code $scannerExitCode on target: '$ProjectPath'" -ForegroundColor Red
    exit 1
}

$jsonText = $scanOutput -join "`n"
$parseResult = Test-VulnerabilityReportJson -JsonContent $jsonText -TargetBlockedSeverities $BlockedSeverities
$outputText = Format-VulnerabilityScanResult -Result $parseResult -TargetProjectPath $ProjectPath -TargetBlockedSeverities $BlockedSeverities

if (-not $parseResult.Success) {
    Write-Host $outputText -ForegroundColor Red
    exit $parseResult.ExitCode
}

# Only print SUCCESS after scanner exit 0, JSON parse succeed, schema valid, no problems, 0 findings
Write-Host $outputText -ForegroundColor Green
exit 0
