param(
    [Parameter(Mandatory = $true)][string]$ResultsPath,
    [string]$ManifestPath = (Join-Path $PSScriptRoot 'sql-lanes.json')
)

$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$lanes = @($manifest.lanes)
if ($lanes.Count -ne 4 -or @($lanes.lane | Sort-Object -Unique).Count -ne 4) {
    throw 'Expected four distinct SQL lanes.'
}
$expected = @($lanes | ForEach-Object { $_.expectedNames })
if (@($expected | Sort-Object -Unique).Count -ne $expected.Count) {
    throw 'SQL identity manifest assigns an expected test more than once.'
}
$files = @(Get-ChildItem -LiteralPath $ResultsPath -Recurse -Filter 'integration-lane-*.trx' -File)
if ($files.Count -ne 4) { throw "Expected four SQL TRX files, found $($files.Count)." }

$observed = [Collections.Generic.List[string]]::new()
foreach ($lane in $lanes) {
    $matching = @($files | Where-Object { $_.Name -eq "integration-lane-$($lane.lane).trx" })
    if ($matching.Count -ne 1) { throw "Missing or duplicated TRX for SQL lane $($lane.lane)." }
    [xml]$trx = Get-Content -LiteralPath $matching[0].FullName -Raw
    $counters = $trx.TestRun.ResultSummary.Counters
    $total = [int]$counters.total
    if ($trx.TestRun.ResultSummary.outcome -ne 'Completed' -or $total -le 0 -or
        [int]$counters.passed -ne $total -or [int]$counters.executed -ne $total) {
        throw "SQL lane $($lane.lane) has zero, failed, skipped or incomplete results."
    }
    $actual = @($trx.TestRun.Results.UnitTestResult | ForEach-Object { [string]$_.testName })
    if ($actual.Count -ne $total -or $actual.Count -ne @($lane.expectedNames).Count) {
        throw "SQL lane $($lane.lane) result count differs from its identity manifest."
    }
    $delta = @(Compare-Object @($lane.expectedNames) $actual)
    if ($delta.Count -ne 0) { throw "SQL lane $($lane.lane) has missing or unexpected test identities." }
    $observed.AddRange([string[]]$actual)
}
if (@($observed | Sort-Object -Unique).Count -ne $observed.Count -or
    @(Compare-Object $expected $observed).Count -ne 0) {
    throw 'SQL lane union has duplicate, missing or unexpected identities.'
}
Write-Host "SQL lane identity gate passed: $($observed.Count) unique executed tests across four lanes."
