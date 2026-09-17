# Automated documentation contract and integrity verification for Task P1-02
[CmdletBinding()]
param (
    [string]$RepoRoot = ""
)

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
    $RepoRoot = Resolve-Path (Join-Path $scriptDir "..\..")
}

Write-Host "=== P1-02 Documentation Verification ===" -ForegroundColor Cyan
Write-Host "Repository root: $RepoRoot" -ForegroundColor Gray

$errors = @()

$adr001 = Join-Path $RepoRoot "docs\adr\001-backend-boundary.md"
$adr002 = Join-Path $RepoRoot "docs\adr\002-authentication.md"
$apiErrors = Join-Path $RepoRoot "docs\api-errors.md"
$worklog = Join-Path $RepoRoot "docs\worklogs\P1-02-completion.md"

$docs = @($adr001, $adr002, $apiErrors, $worklog)

# 1. Verify existence of required files
foreach ($doc in $docs) {
    if (-not (Test-Path $doc)) {
        $errors += "Missing required document: $doc"
    }
}

if ($errors.Count -gt 0) {
    Write-Host "FAILURE: Missing files:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

# 2. Check for prohibited absolute file:// links in all documents
foreach ($doc in $docs) {
    $content = Get-Content $doc -Raw
    if ($content -match '\[[^\]]+\]\(file:///[^)]+\)') {
        $errors += "Prohibited absolute 'file://' link found in: $doc. Use portable relative Markdown links instead."
    }
}

# 3. Validate relative links resolve correctly
$linkRegex = '\[([^\]]+)\]\(([^)]+)\)'
foreach ($doc in $docs) {
    $content = Get-Content $doc -Raw
    $matches = [regex]::Matches($content, $linkRegex)
    foreach ($m in $matches) {
        $target = $m.Groups[2].Value
        if (-not ($target.StartsWith("http://") -or $target.StartsWith("https://") -or $target.StartsWith("#") -or $target.StartsWith("mailto:"))) {
            $dir = Split-Path $doc
            # Strip fragment anchor if present
            $filePathTarget = $target.Split('#')[0]
            if (-not [string]::IsNullOrWhiteSpace($filePathTarget)) {
                $resolved = Join-Path $dir $filePathTarget
                if (-not (Test-Path $resolved)) {
                    $errors += "Broken relative link in $($doc): '$target' -> unresolved target '$resolved'"
                }
            }
        }
    }
}

# 4. Validate ADR 001 content & dependency graph
$adr001Content = Get-Content $adr001 -Raw
$adr001Headings = @(
    "ADR 001: Backend Architecture Boundaries", "Status", "Context",
    "Decision", "Layer Ownership and Clean Architecture Invariants",
    "Runtime and Toolchain Baseline", "Consequences",
    "Rejected Alternatives", "Unresolved Decisions", "Compliance and Verification"
)
foreach ($heading in $adr001Headings) {
    if ($adr001Content -notmatch "(?m)^#{1,4}\s+.*$([regex]::Escape($heading))") {
        $errors += "ADR 001 missing required heading: '$heading'"
    }
}

$adr001Requirements = @(
    "API -> Services -> Repositories",
    "Repositories -> BusinessObjects và DTOs",
    "DTOs -> BusinessObjects",
    "RoadGuardDbContext",
    "DependencyGraphTests.cs",
    "P1-31",
    "net8.0",
    "10.0.401",
    "P2-01",
    "P2-10"
)
foreach ($req in $adr001Requirements) {
    if ($adr001Content -notmatch [regex]::Escape($req)) {
        $errors += "ADR 001 missing required requirement token: '$req'"
    }
}

# 5. Validate ADR 002 content, use-case mapping, roles, and security policy
$adr002Content = Get-Content $adr002 -Raw
$adr002Headings = @(
    "ADR 002: Authentication Architecture", "Status", "Context", "Decision",
    "Core Authentication Architecture", "Token Lifecycle and Session Management",
    "Server-Side Project Membership Authorization", "Security, Secret Handling, and Logging Policy",
    "Deferred and Out-of-Scope Capabilities", "Rejected Alternatives",
    "Consequences and Follow-ups", "Unresolved Decisions"
)
foreach ($heading in $adr002Headings) {
    if ($adr002Content -notmatch "(?m)^#{1,4}\s+.*$([regex]::Escape($heading))") {
        $errors += "ADR 002 missing required heading: '$heading'"
    }
}

$adr002Requirements = @(
    "CN01", "CN02", "CN03", "CN10", "QT01",
    "Supervisor", "PM", "Drone Operator", "Repair Crew",
    "JWT Bearer + Rotating Opaque Refresh-Token Model",
    "JWT Signing Credentials",
    "sid",
    "token_hash",
    "fail closed",
    "P1-10", "P2-10", "P1-12"
)
foreach ($req in $adr002Requirements) {
    if ($adr002Content -notmatch [regex]::Escape($req)) {
        $errors += "ADR 002 missing required requirement token: '$req'"
    }
}

# 6. Validate api-errors.md content
$apiErrorsContent = Get-Content $apiErrors -Raw
$apiErrorsHeadings = @(
    "# API Error Handling and Taxonomy Specification", "## Status",
    "## Overview and RFC 7807/9110 Standard", "## Standard Error Envelope Schema",
    "## Error Code Naming Rules and Taxonomy", "## HTTP Status Code Mapping",
    "## Registry of Normative Error Codes", "## Security and Information Disclosure Rules",
    "## Versioning, Evolution, and Deprecation Policy", "## Non-Normative Examples"
)
foreach ($heading in $apiErrorsHeadings) {
    if ($apiErrorsContent -notmatch [regex]::Escape($heading)) {
        $errors += "api-errors.md missing required heading: '$heading'"
    }
}

$apiErrorsRequirements = @(
    "RFC 7807", "RFC 9110", "application/problem+json",
    "Current Baseline (P1-01)",
    "API 4xx/5xx error responses",
    "validation_error", "unsupported_api_version", "not_found", "internal_error",
    "method_not_allowed", "unsupported_media_type",
    "400", "401", "403", "404", "405", "409", "415", "422", "500",
    "<domain>_<reason>", "correlationId", "snake_case"
)
foreach ($req in $apiErrorsRequirements) {
    if ($apiErrorsContent -notmatch [regex]::Escape($req)) {
        $errors += "api-errors.md missing required requirement token: '$req'"
    }
}

# 7. Validate worklog content & status
$worklogContent = Get-Content $worklog -Raw
$worklogRequirements = @(
    "P1-02", "Person 1", "Person 2", "Changes requested",
    "Verify-P102Docs.ps1",
    "001-backend-boundary.md", "002-authentication.md", "api-errors.md",
    "8 projects",
    "21 integration tests",
    "Docker"
)
foreach ($req in $worklogRequirements) {
    if ($worklogContent -notmatch [regex]::Escape($req)) {
        $errors += "Worklog missing required token: '$req'"
    }
}

if ($errors.Count -gt 0) {
    Write-Host "FAILURE: Verification found $($errors.Count) errors:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "SUCCESS: All P1-02 documentation contracts, relative links, use-cases, and dependency checks passed!" -ForegroundColor Green
exit 0
