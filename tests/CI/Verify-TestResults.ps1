[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)][string]$ResultsPath,
    [switch]$AllowMinioSmokeSkip
)

$ErrorActionPreference = 'Stop'
$files = @(Get-ChildItem -LiteralPath $ResultsPath -Recurse -Filter '*.trx')
if ($files.Count -eq 0) { throw 'No TRX results: the required test lane did not execute.' }
$identities = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$passed = 0
$skipped = 0
foreach ($file in $files) {
    [xml]$document = Get-Content -LiteralPath $file.FullName -Raw
    $results = @($document.SelectNodes('//*[local-name()="UnitTestResult"]'))
    if ($results.Count -eq 0) { throw "Zero-test TRX: $($file.Name)" }
    foreach ($result in $results) {
        $identity = [string]$result.testName
        if (-not $identities.Add($identity)) { throw "Duplicate executed test identity: $identity" }
        if ($result.outcome -eq 'Passed') {
            $passed++
        } elseif ($result.outcome -eq 'NotExecuted' -and $AllowMinioSmokeSkip -and
            $identity -eq 'RoadGuardSystem.ApiTests.Files.UploadApiTests.UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio' -and
            $env:ROADGUARD_MINIO_SMOKE -ne '1') {
            $skipped++
        } else {
            throw "Required test did not pass: $identity ($($result.outcome))"
        }
    }
    $counters = $document.SelectSingleNode('//*[local-name()="Counters"]')
    if ($null -eq $counters -or [int]$counters.total -ne $results.Count -or
        [int]$counters.failed -ne 0 -or [int]$counters.error -ne 0) {
        throw "Incomplete or unsuccessful TRX: $($file.Name)"
    }
}
if ($passed -eq 0) { throw 'No required tests passed.' }
$sorted = @($identities | Sort-Object -CaseSensitive)
$outputDirectory = if (Test-Path -LiteralPath $ResultsPath -PathType Container) { $ResultsPath } else { Split-Path -Parent $ResultsPath }
$sorted | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputDirectory 'executed-identities.json') -Encoding utf8
Write-Output "PASS: $passed passed, $skipped explicitly optional MinIO smoke skips; $($sorted.Count) unique executed identities."
