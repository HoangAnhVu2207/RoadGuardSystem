# Current backend documentation, ownership and local-link guard.
[CmdletBinding()]
param ([string]$RepoRoot = '', [switch]$SelfTestNegative)

$ErrorActionPreference = 'Stop'
if (-not $RepoRoot) { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path }
$hasCurrentDesignLayout = Test-Path -LiteralPath (Join-Path $RepoRoot 'docs/design/README.md') -PathType Leaf
$designRoot = if ($hasCurrentDesignLayout) { 'docs/design' } else { 'docs/diagram/V2' }
$required = @(
    'AGENTS.md', 'docs/README.md', "$designRoot/README.md",
    "$designRoot/03_Data/01_Data_Dictionary.md",
    "$designRoot/03_Data/02_ERD_V2.md",
    'docs/adr/001-backend-boundary.md', 'docs/adr/002-authentication.md',
    'docs/adr/003-backend-delivery-and-ai-boundary.md',
    'docs/adr/004-n-layer-backend-structure.md',
    'docs/adr/005-product-workflow-synchronization.md',
    'docs/adr/006-v2-endpoint-ownership-and-persistence-coordination.md',
    'planning/RoadGuard_Plan_Person_1.md',
    'planning/RoadGuard_Plan_Person_2.md', 'planning/V2/README.md',
    '.agents/skills/roadguard-endpoint-delivery/SKILL.md',
    '.agents/skills/roadguard-review-autofix/SKILL.md'
)
if ($hasCurrentDesignLayout) {
    $required += @(
        'docs/design/03_Data/ERD_GAPS.md',
        'planning/CROSS_OWNER_HANDOFFS.md'
    )
    $domains = @('Auth_Identity', 'Project_Route', 'Survey_Dataset',
        'Report_Case_Defect', 'Inspection_Repair', 'Processing_AI', 'Durability_Ops')
    $required += $domains | ForEach-Object { "docs/design/03_Data/ERD_$_.md" }
}
$errors = [Collections.Generic.List[string]]::new()

foreach ($relative in $required) {
    $path = Join-Path $RepoRoot $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $errors.Add("Missing required document: $relative")
        continue
    }
    $content = Get-Content -Raw -LiteralPath $path -Encoding UTF8
    if ($SelfTestNegative -and $relative -eq 'docs/adr/006-v2-endpoint-ownership-and-persistence-coordination.md') {
        $content = $content.Replace('Person 1', 'Missing owner')
    }
    foreach ($match in [regex]::Matches($content, '\[[^\]]+\]\(([^)]+)\)')) {
        $target = $match.Groups[1].Value.Split('#')[0]
        if (-not $target -or $target -match '^(https?://|mailto:)') { continue }
        $resolved = Join-Path (Split-Path $path -Parent) $target
        if (-not (Test-Path -LiteralPath $resolved)) {
            $errors.Add("Broken link: ${relative} -> $target")
        }
    }
    if ($content -match '\]\(file:///') { $errors.Add("Non-portable file link: $relative") }
}

$p1 = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot 'planning/RoadGuard_Plan_Person_1.md') -Encoding UTF8
$p2 = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot 'planning/RoadGuard_Plan_Person_2.md') -Encoding UTF8
$adr = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot 'docs/adr/006-v2-endpoint-ownership-and-persistence-coordination.md') -Encoding UTF8
if ($SelfTestNegative) { $adr = $adr.Replace('Person 1', 'Missing owner') }
foreach ($id in 1..6) {
    if ($p1 -notmatch "DB-0$id") { $errors.Add("PLAN_COVERAGE: missing DB-0$id") }
    if ($p2 -notmatch "APP-0$id") { $errors.Add("PLAN_COVERAGE: missing APP-0$id") }
}
foreach ($token in @('Person 1', 'BusinessObjects', 'Repositories', 'Person 2', 'Services', 'DTOs', 'VERIFIED')) {
    if ($adr.IndexOf($token, [StringComparison]::Ordinal) -lt 0) {
        $errors.Add("OWNERSHIP: ADR 006 missing $token")
    }
}
$expectedSkillDirectories = if ($hasCurrentDesignLayout) { 2 } else { 5 }
if ((Get-ChildItem -LiteralPath (Join-Path $RepoRoot '.agents/skills') -Directory).Count -ne $expectedSkillDirectories) {
    $errors.Add("SKILLS: expected exactly $expectedSkillDirectories skill directories for the selected documentation layout")
}

if ($SelfTestNegative) {
    if (-not $errors.Contains('OWNERSHIP: ADR 006 missing Person 1')) { throw 'Negative fixture was not detected.' }
    Write-Host 'PASS: ownership negative fixture detected.'
    exit 0
}
if ($errors.Count) { throw ($errors -join [Environment]::NewLine) }
Write-Host "SUCCESS: $($required.Count) current backend documents, ownership and links verified."
