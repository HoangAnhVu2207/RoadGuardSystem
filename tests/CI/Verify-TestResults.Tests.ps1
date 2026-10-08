$ErrorActionPreference = 'Stop'
$directory = Join-Path ([IO.Path]::GetTempPath()) "RoadGuard_TrxGate_$([Guid]::NewGuid().ToString('N'))"
$gate = Join-Path $PSScriptRoot 'Verify-TestResults.ps1'
$null = New-Item -ItemType Directory -Path $directory
try {
    $cases = @(
        @{ Name = 'passed'; Results = '<UnitTestResult testName="A.Case(x: 1)" outcome="Passed" />'; Total = 1; Failed = 0; Pass = $true },
        @{ Name = 'zero'; Results = ''; Total = 0; Failed = 0; Pass = $false },
        @{ Name = 'failed'; Results = '<UnitTestResult testName="A.Case" outcome="Failed" />'; Total = 1; Failed = 1; Pass = $false },
        @{ Name = 'skipped-required'; Results = '<UnitTestResult testName="A.Case" outcome="NotExecuted" />'; Total = 1; Failed = 0; Pass = $false },
        @{ Name = 'duplicate'; Results = '<UnitTestResult testName="A.Case" outcome="Passed" /><UnitTestResult testName="A.Case" outcome="Passed" />'; Total = 2; Failed = 0; Pass = $false },
        @{ Name = 'incomplete'; Results = '<UnitTestResult testName="A.Case" outcome="Passed" />'; Total = 2; Failed = 0; Pass = $false }
    )
    foreach ($case in $cases) {
        $caseDirectory = Join-Path $directory $case.Name
        $null = New-Item -ItemType Directory -Path $caseDirectory
        $xml = "<TestRun><Results>$($case.Results)</Results><ResultSummary><Counters total='$($case.Total)' failed='$($case.Failed)' error='0'/></ResultSummary></TestRun>"
        Set-Content -LiteralPath (Join-Path $caseDirectory 'results.trx') -Value $xml
        & pwsh -NoProfile -File $gate -ResultsPath $caseDirectory *> (Join-Path $caseDirectory 'gate.log')
        if (($LASTEXITCODE -eq 0) -ne $case.Pass) { throw "Incorrect gate outcome: $($case.Name)" }
    }
    $emptyDirectory = Join-Path $directory 'missing'
    $null = New-Item -ItemType Directory -Path $emptyDirectory
    & pwsh -NoProfile -File $gate -ResultsPath $emptyDirectory *> (Join-Path $emptyDirectory 'gate.log')
    if ($LASTEXITCODE -eq 0) { throw 'Missing TRX was accepted.' }
    Write-Output 'PASS: TRX gate accepts successful results and rejects missing, zero, failed, skipped, duplicate and incomplete results.'
} finally {
    # The exact GUID-named directory was created by this invocation under the OS temp directory.
    $resolved = [IO.Path]::GetFullPath($directory)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (-not $resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolved) -notmatch '^RoadGuard_TrxGate_[a-f0-9]{32}$') {
        throw 'Unsafe test cleanup path.'
    }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
