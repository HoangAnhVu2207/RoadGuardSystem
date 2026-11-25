$ErrorActionPreference = 'Stop'
$gate = Join-Path $PSScriptRoot 'Verify-TestLanes.ps1'
$smoke = 'RoadGuardSystem.ApiTests.Files.UploadApiTests.UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio'
$cases = @(
    @{ Name = 'valid'; Outcome = 'Passed'; Summary = 'Completed' },
    @{ Name = 'optional-smoke'; Outcome = 'NotExecuted'; Summary = 'Completed'; Optional = $true },
    @{ Name = 'missing-lane'; Outcome = 'Passed'; Summary = 'Completed'; MissingLane = $true; Error = 'Expected 2 TRX files' },
    @{ Name = 'missing-case'; Outcome = 'Passed'; Summary = 'Completed'; Expected = @('Fixture.Second', 'Fixture.Missing'); Error = 'missing or unexpected' },
    @{ Name = 'duplicate'; Outcome = 'Passed'; Summary = 'Completed'; Actual = @('Fixture.Second', 'Fixture.Second'); Error = 'Duplicate executed' },
    @{ Name = 'wrong-lane'; Outcome = 'Passed'; Summary = 'Completed'; Actual = @('Fixture.Other'); Error = 'missing or unexpected' },
    @{ Name = 'zero'; Outcome = 'Passed'; Summary = 'Completed'; Actual = @(); Error = 'Zero-test' },
    @{ Name = 'skip'; Outcome = 'NotExecuted'; Summary = 'Completed'; Error = 'Required test did not pass' },
    @{ Name = 'fail'; Outcome = 'Failed'; Summary = 'Failed'; Error = 'Required test did not pass' },
    @{ Name = 'cancelled-matrix'; Outcome = 'Passed'; Summary = 'Completed'; Matrix = 'cancelled'; Error = 'matrix result was cancelled' },
    @{ Name = 'cancelled-run'; Outcome = 'Passed'; Summary = 'Aborted'; Error = 'Incomplete or unsuccessful' },
    @{ Name = 'missing-counter'; Outcome = 'Passed'; Summary = 'Completed'; MissingCounter = $true; Error = 'Incomplete or unsuccessful' }
)

function Write-FixtureTrx($path, [string[]]$identities, $outcome, $summary, $missingCounter) {
    $results = foreach ($identity in $identities) {
        "<UnitTestResult testName='$identity' outcome='$outcome' />"
    }
    $passed = if ($outcome -eq 'Passed') { $identities.Count } else { 0 }
    $executed = if ($outcome -eq 'NotExecuted') { 0 } else { $identities.Count }
    $executedAttribute = if (-not $missingCounter) { "executed='$executed'" } else { '' }
    $failed = if ($outcome -eq 'Failed') { $identities.Count } else { 0 }
    Set-Content -LiteralPath $path -Value @"
<TestRun><Results>$results</Results><ResultSummary outcome='$summary'><Counters total='$($identities.Count)' passed='$passed' $executedAttribute failed='$failed' error='0' /></ResultSummary></TestRun>
"@
}

$directory = Join-Path ([IO.Path]::GetTempPath()) "RoadGuard_LaneGate_$([Guid]::NewGuid().ToString('N'))"
$null = New-Item -ItemType Directory -Path $directory
try {
    foreach ($case in $cases) {
        $caseDirectory = Join-Path $directory $case.Name
        $null = New-Item -ItemType Directory -Path $caseDirectory
        $expected = $case.Expected ?? @('Fixture.Second')
        $actual = $case.Actual ?? @('Fixture.Second')
        if ($case.Optional) { $expected = $actual = @($smoke) }
        $manifestPath = Join-Path $caseDirectory 'manifest.json'
        $manifest = @{ lanes = @(
            @{ lane = 1; expectedNames = @('Fixture.First') },
            @{ lane = 2; expectedNames = $expected }
        ) }
        ConvertTo-Json -InputObject $manifest -Depth 5 | Set-Content -LiteralPath $manifestPath
        Write-FixtureTrx (Join-Path $caseDirectory 'fixture-lane-1.trx') @('Fixture.First') 'Passed' 'Completed'
        if (-not $case.MissingLane) {
            Write-FixtureTrx (Join-Path $caseDirectory 'fixture-lane-2.trx') $actual $case.Outcome $case.Summary $case.MissingCounter
        }
        $matrix = $case.Matrix ?? 'success'
        $arguments = @('-ResultsPath', $caseDirectory, '-ManifestPath', $manifestPath, '-Prefix', 'fixture',
            '-LaneCount', '2', '-MatrixResult', $matrix)
        if ($case.Optional) { $arguments += '-AllowMinioSmokeSkip' }
        $output = & pwsh -NoProfile -File $gate @arguments 2>&1 | Out-String
        if ($case.Error) {
            if ($LASTEXITCODE -eq 0 -or $output -notmatch [regex]::Escape($case.Error)) {
                throw "Gate did not reject $($case.Name) as expected. $output"
            }
        } elseif ($LASTEXITCODE -ne 0) {
            throw "Gate rejected $($case.Name). $output"
        }
    }
    Write-Output "PASS: $($cases.Count) lane gate fixtures."
} finally {
    $resolved = [IO.Path]::GetFullPath($directory)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (-not $resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolved) -notmatch '^RoadGuard_LaneGate_[a-f0-9]{32}$') {
        throw 'Unsafe test cleanup path.'
    }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
