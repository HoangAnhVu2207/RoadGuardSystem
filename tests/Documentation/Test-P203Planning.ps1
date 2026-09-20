# Behavioral regression tests for P2-03. Fixtures are isolated from the checkout.
[CmdletBinding()]
param ([switch]$NegativeOnly)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$verifier = Join-Path $PSScriptRoot 'Verify-P102Docs.ps1'
$engine = (Get-Process -Id $PID).Path
$tempParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixture = Join-Path $tempParent ('RoadGuard-P203-' + [guid]::NewGuid().ToString('N'))
$utf8 = New-Object System.Text.UTF8Encoding($false)
$script:failures = 0

function Set-PlanFixture([string]$RelativePath, [scriptblock]$Mutate) {
    $path = Join-Path $fixture $RelativePath
    $original = [IO.File]::ReadAllText((Join-Path $repo $RelativePath))
    [IO.File]::WriteAllText($path, (& $Mutate $original), $utf8)
}

function Invoke-Case([string]$Name, [scriptblock]$Mutate, [int]$ExpectedExit, [string]$Diagnostic) {
    foreach ($p in @('planning/RoadGuard_Plan_Person_1.md', 'planning/RoadGuard_Plan_Person_2.md')) {
        Copy-Item -LiteralPath (Join-Path $repo $p) -Destination (Join-Path $fixture $p) -Force
    }
    & $Mutate
    $output = (& $engine -NoProfile -ExecutionPolicy Bypass -File $verifier -RepoRoot $fixture 2>&1 | Out-String)
    $actual = $LASTEXITCODE
    if ($actual -ne $ExpectedExit -or ($Diagnostic -and $output -notmatch [regex]::Escape($Diagnostic))) {
        $script:failures++
        Write-Host "FAIL $Name : expected exit $ExpectedExit / $Diagnostic; got $actual"
        Write-Host $output
    } else { Write-Host "PASS $Name" }
}

try {
    New-Item -ItemType Directory -Path $fixture | Out-Null
    foreach ($p in @('docs', 'planning', '.agents')) {
        Copy-Item -LiteralPath (Join-Path $repo $p) -Destination (Join-Path $fixture $p) -Recurse
    }
    Copy-Item -LiteralPath (Join-Path $repo 'AGENTS.md') -Destination $fixture
    foreach ($p in @('tests/Documentation/Verify-P102Docs.ps1', 'tests/RoadGuardSystem.UnitTests/Architecture/DependencyGraphTests.cs')) {
        $target = Join-Path $fixture $p
        New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $repo $p) -Destination $target
    }

    Invoke-Case 'Historical prose cannot replace a missing current status' {
        Set-PlanFixture 'planning/RoadGuard_Plan_Person_2.md' {
            param($s)
            [regex]::Replace($s, '(?m)^\| `P2-01` \|[^\r\n]+\r?\n', '')
        }
    } 1 'PLAN_STATUS'
    Invoke-Case 'Unrecognized current status is rejected' {
        Set-PlanFixture 'planning/RoadGuard_Plan_Person_2.md' {
            param($s)
            [regex]::Replace($s, '(?m)^(\| `P2-01` \| )`[^`]+`', '$1`Magic success`')
        }
    } 1 'PLAN_STATUS'
    Invoke-Case 'Duplicate current status rows are rejected' {
        Set-PlanFixture 'planning/RoadGuard_Plan_Person_2.md' {
            param($s)
            [regex]::Replace($s, '(?m)^(\| `P2-01` \|[^\r\n]+)', '$1' + "`n" + '$1')
        }
    } 1 'PLAN_STATUS'
    Invoke-Case 'Undefined task dependency is rejected' {
        Set-PlanFixture 'planning/RoadGuard_Plan_Person_2.md' {
            param($s)
            [regex]::Replace($s, '(?m)^(\| `P2-11` /[^|]+\|[^|]+)', '$1, P2-99')
        }
    } 1 'PLAN_DEPENDENCY'
    Invoke-Case 'Dependency cycle is rejected' {
        Set-PlanFixture 'planning/RoadGuard_Plan_Person_2.md' {
            param($s)
            [regex]::Replace($s, '(?m)^(\| `P2-00` /[^|]+\|[^|]+)', '$1, P2-11')
        }
    } 1 'PLAN_CYCLE'
    Invoke-Case 'Membership query must depend on its schema owner' {
        Set-PlanFixture 'planning/RoadGuard_Plan_Person_2.md' {
            param($s)
            [regex]::Replace($s, '(?m)^\| `P2-11` /[^\r\n]+', {
                param($m)
                $m.Value.Replace(', P2-20', '')
            })
        }
    } 1 'PLAN_DEPENDENCY'
    if (-not $NegativeOnly) {
        Invoke-Case 'Current repository contracts are accepted' {} 0 'SUCCESS'
        Invoke-Case 'Historical gate wording can be removed without blocking valid status' {
            Set-PlanFixture 'planning/RoadGuard_Plan_Person_2.md' {
                param($s)
                [regex]::Replace($s, '(?m)^Prior gate:[^\r\n]+', 'Prior gate: completed in baseline 20ff1d3 (3e13ca6 and b2662fe).')
            }
        } 0 'SUCCESS'
        Invoke-Case 'P2-01 can progress to Done without changing verifier source' {
            Set-PlanFixture 'planning/RoadGuard_Plan_Person_2.md' {
                param($s)
                [regex]::Replace($s, '(?m)^(\| `P2-01` \| )`[^`]+`', '$1`Done`')
            }
        } 0 'SUCCESS'
    }
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    if ($resolvedFixture.StartsWith($tempParent, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path $resolvedFixture -Leaf) -match '^RoadGuard-P203-[a-f0-9]{32}$' -and
        (Test-Path -LiteralPath $resolvedFixture)) {
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
if ($script:failures -gt 0) { throw "$script:failures planning regression test(s) failed." }
Write-Host 'SUCCESS: P2-03 planning regression tests passed.'
