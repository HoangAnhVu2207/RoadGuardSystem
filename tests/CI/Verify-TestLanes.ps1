param(
    [Parameter(Mandatory)][string]$ResultsPath,
    [Parameter(Mandatory)][string]$ManifestPath,
    [Parameter(Mandatory)][string]$Prefix,
    [Parameter(Mandatory)][int]$LaneCount,
    [Parameter(Mandatory)][string]$MatrixResult,
    [switch]$AllowMinioSmokeSkip
)

$ErrorActionPreference = 'Stop'
if ($MatrixResult -ne 'success') { throw "$Prefix matrix result was $MatrixResult." }

$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$lanes = @($manifest.lanes)
if ($lanes.Count -ne $LaneCount -or @(Compare-Object (1..$LaneCount) $lanes.lane).Count -ne 0) {
    throw "Expected $LaneCount distinct $Prefix lanes numbered from 1."
}
$files = @(Get-ChildItem -LiteralPath $ResultsPath -Recurse -Filter "$Prefix-lane-*.trx" -File)
if ($files.Count -ne $LaneCount) { throw "Expected $LaneCount TRX files, found $($files.Count)." }

$expected = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$observed = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$passed = 0
$skipped = 0
$smoke = 'RoadGuardSystem.ApiTests.Files.UploadApiTests.UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio'
foreach ($lane in $lanes) {
    $laneName = "$Prefix-lane-$($lane.lane)"
    $matching = @($files | Where-Object { $_.Name -ceq "$laneName.trx" })
    if ($matching.Count -ne 1) { throw "Missing or duplicated TRX for $laneName." }
    $assigned = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($identity in $lane.expectedNames) {
        if ([string]::IsNullOrWhiteSpace($identity) -or -not $expected.Add($identity)) {
            throw "$laneName has a blank or duplicate expected identity."
        }
        $null = $assigned.Add($identity)
    }

    [xml]$trx = Get-Content -LiteralPath $matching[0].FullName -Raw
    $results = @($trx.TestRun.Results.UnitTestResult | Where-Object { $null -ne $_ })
    if ($results.Count -eq 0) { throw "Zero-test TRX: $laneName." }
    $lanePassed = 0
    foreach ($result in $results) {
        $identity = [string]$result.testName
        if (-not $observed.Add($identity)) { throw "Duplicate executed test identity: $identity" }
        if (-not $assigned.Remove($identity)) { throw "$laneName has missing or unexpected test identities: $identity" }
        if ($result.outcome -ceq 'Passed') {
            $lanePassed++
        } elseif ($result.outcome -ceq 'NotExecuted' -and $AllowMinioSmokeSkip -and
            $env:ROADGUARD_MINIO_SMOKE -ne '1' -and $identity -ceq $smoke) {
            $skipped++
        } else {
            throw "Required test did not pass: $identity ($($result.outcome))"
        }
    }
    if ($assigned.Count -ne 0) { throw "$laneName has missing or unexpected test identities: $($assigned -join ', ')" }
    $summary = $trx.TestRun.ResultSummary
    $counters = $summary.Counters
    if ($summary.outcome -cne 'Completed' -or $null -eq $counters -or
        $counters.total -cne [string]$results.Count -or $counters.executed -cne [string]$lanePassed -or
        $counters.passed -cne [string]$lanePassed -or $counters.failed -cne '0' -or $counters.error -cne '0' -or
        [int]$counters.aborted -ne 0 -or [int]$counters.timeout -ne 0 -or [int]$counters.passedButRunAborted -ne 0) {
        throw "Incomplete or unsuccessful TRX: $laneName."
    }
    $passed += $lanePassed
    Write-Host "$laneName verified: $lanePassed passed, $($results.Count - $lanePassed) optional skips."
}
if ($passed -eq 0) { throw 'No required tests passed.' }
Write-Host "$Prefix identity gate passed: $($observed.Count) unique identities across $LaneCount lanes, $passed passed, $skipped optional skips."
