# Automated documentation contract and integrity verification for Task P1-02
[CmdletBinding()]
param (
    [string]$RepoRoot = "",
    [switch]$SelfTestNegative
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
$dataDictionary = Join-Path $RepoRoot "docs\diagram\RoadGuard_Data_Dictionary_v1.md"
$erd = Join-Path $RepoRoot "docs\diagram\RoadGuard_ERD_v1.md"
$domainModel = Join-Path $RepoRoot "docs\diagram\RoadGuard_Domain_Model_v1.md"
$useCases = Join-Path $RepoRoot "docs\diagram\Dac_ta_UseCase_v2.md"
$userStories = Join-Path $RepoRoot "docs\diagram\User_Stories_Acceptance_Criteria_v2.md"
$person1Plan = Join-Path $RepoRoot "planning\RoadGuard_Plan_Person_1.md"
$person2Plan = Join-Path $RepoRoot "planning\RoadGuard_Plan_Person_2.md"

$docs = @(
    $adr001, $adr002, $apiErrors, $worklog,
    $dataDictionary, $erd, $domainModel, $useCases, $userStories,
    $person1Plan, $person2Plan
)

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

# 5. Validate ADR 002 content, exact use-case mappings, roles, and task ownership
$adr002Content = Get-Content $adr002 -Raw

# In SelfTestNegative mode, simulate an injected invalid mapping to verify verifier failure detection
if ($SelfTestNegative) {
    $adr002Content = $adr002Content -replace [regex]::Escape("CN01 (Login / Logout)"), "CN02 (Logout)"
    $adr002Content = $adr002Content -replace [regex]::Escape("device_metadata_json"), "created_at"
    $adr002Content = $adr002Content -replace [regex]::Escape("UserRoleChanged"), "RoleChangeIgnored"
}

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

# Exact use-case mappings (must be strictly present)
$exactMappings = @(
    "CN01 (Login / Logout)",
    "CN02 (Profile)",
    "CN03 (Assigned Project / Work Scope)",
    "CN10 (Password Reset)",
    "QT01 (Account Suspension)",
    "QT02 (Role and Project Access Management)"
)
foreach ($map in $exactMappings) {
    if ($adr002Content -notmatch [regex]::Escape($map)) {
        $errors += "ADR 002 missing exact required use-case mapping: '$map'"
    }
}

# Prohibited old incorrect mappings (must be strictly rejected)
$rejectedMappings = @(
    "CN02 (Logout)",
    "CN03 (Change Password)",
    "CN10 (Account Suspension)"
)
foreach ($rej in $rejectedMappings) {
    if ($adr002Content -match [regex]::Escape($rej)) {
        $errors += "ADR 002 contains rejected obsolete use-case mapping: '$rej'"
    }
}

# Stable machine-readable role codes
$requiredRoleCodes = @(
    "SUPERVISOR",
    "PM",
    "DRONE_OPERATOR",
    "REPAIR_CREW"
)
foreach ($code in $requiredRoleCodes) {
    if ($adr002Content -notmatch "\b$code\b") {
        $errors += "ADR 002 missing machine-readable role code: '$code'"
    }
}

# Task ownership validation
$adr002OwnershipRequirements = @(
    "P1-00", "P2-10", "P1-10", "P1-11", "P1-12", "P1-64", "P2-11",
    "Section 3.1",
    "internally provisioned credentials",
    "JWT Bearer + Rotating Opaque Refresh-Token Model",
    "JWT Signing Credentials",
    "sid",
    "token_hash",
    "fail closed"
)
foreach ($req in $adr002OwnershipRequirements) {
    if ($adr002Content -notmatch [regex]::Escape($req)) {
        $errors += "ADR 002 missing required ownership/architecture token: '$req'"
    }
}

# Reject attributing session entities to P1-00 / P1-11
if ($adr002Content -match "session entities.*P1-00\s*/\s*P1-11" -or
    $adr002Content -match "P1-00\s*/\s*P1-11.*session entities") {
    $errors += "ADR 002 incorrectly assigns session entities to P1-00 / P1-11 instead of P2-10"
}

# Session schema compatibility decision approved by the Product Owner on 2026-09-17.
$dataDictionaryContent = Get-Content $dataDictionary -Raw
$erdContent = Get-Content $erd -Raw
$domainModelContent = Get-Content $domainModel -Raw
$useCasesContent = Get-Content $useCases -Raw
$userStoriesContent = Get-Content $userStories -Raw
$person1PlanContent = Get-Content $person1Plan -Raw
$person2PlanContent = Get-Content $person2Plan -Raw

$sessionContractDocuments = @(
    @{ Name = "ADR 002"; Content = $adr002Content },
    @{ Name = "Data Dictionary"; Content = $dataDictionaryContent },
    @{ Name = "ERD"; Content = $erdContent },
    @{ Name = "Domain Model"; Content = $domainModelContent }
)

foreach ($document in $sessionContractDocuments) {
    foreach ($requiredToken in @("issued_at", "device_metadata_json")) {
        if ($document.Content -notmatch [regex]::Escape($requiredToken)) {
            $errors += "$($document.Name) missing approved Session contract token: '$requiredToken'"
        }
    }
}

foreach ($requiredToken in @("nvarchar(max)", "ISJSON", "application-level schema validation")) {
    if ($dataDictionaryContent -notmatch [regex]::Escape($requiredToken)) {
        $errors += "Data Dictionary missing Session device metadata requirement: '$requiredToken'"
    }
    if ($adr002Content -notmatch [regex]::Escape($requiredToken)) {
        $errors += "ADR 002 missing Session device metadata requirement: '$requiredToken'"
    }
}

if ($adr002Content -match '(?i)Session[^\r\n]*`created_at`') {
    $errors += "ADR 002 must use Session.issued_at and must not redefine the issuance timestamp as Session.created_at"
}

# Product Owner authorization decision approved on 2026-09-17.
foreach ($requiredToken in @(
    "User.role_code",
    "ProjectMember.role_code",
    "UserRoleChanged",
    "authoritative",
    "role claim mismatch"
)) {
    if ($adr002Content -notmatch [regex]::Escape($requiredToken)) {
        $errors += "ADR 002 missing approved authorization authority requirement: '$requiredToken'"
    }
}

foreach ($document in @(
    @{ Name = "Data Dictionary"; Content = $dataDictionaryContent },
    @{ Name = "ERD"; Content = $erdContent },
    @{ Name = "Domain Model"; Content = $domainModelContent }
)) {
    foreach ($requiredToken in @("User.role_code", "ProjectMember.role_code", "UserRoleChanged")) {
        if ($document.Content -notmatch [regex]::Escape($requiredToken)) {
            $errors += "$($document.Name) missing approved authorization invariant token: '$requiredToken'"
        }
    }
}

foreach ($document in @(
    @{ Name = "Use Case"; Content = $useCasesContent },
    @{ Name = "User Stories"; Content = $userStoriesContent }
)) {
    foreach ($requiredToken in @("có hiệu lực ngay", "thu hồi toàn bộ phiên")) {
        if ($document.Content -notmatch [regex]::Escape($requiredToken)) {
            $errors += "$($document.Name) missing approved immediate authorization-change rule: '$requiredToken'"
        }
    }
}

foreach ($plan in @(
    @{ Name = "Person 1 plan"; Content = $person1PlanContent },
    @{ Name = "Person 2 plan"; Content = $person2PlanContent }
)) {
    foreach ($requiredToken in @(
        "device_metadata_json",
        "P2-10",
        "UserRoleChanged",
        "ProjectMember.role_code"
    )) {
        if ($plan.Content -notmatch [regex]::Escape($requiredToken)) {
            $errors += "$($plan.Name) missing approved Session/authorization ownership token: '$requiredToken'"
        }
    }
}

# 6. Validate api-errors.md content
$apiErrorsContent = Get-Content $apiErrors -Raw
$apiErrorsHeadings = @(
    "API Error Handling and Taxonomy Specification", "Status",
    "Overview and RFC 7807/9110 Standard", "Standard Error Envelope Schema",
    "Error Code Naming Rules and Taxonomy", "HTTP Status Code Mapping",
    "Registry of Normative Error Codes", "Security and Information Disclosure Rules",
    "Versioning, Evolution, and Deprecation Policy", "Non-Normative Examples"
)
foreach ($heading in $apiErrorsHeadings) {
    if ($apiErrorsContent -notmatch "(?m)^#{1,4}\s+.*$([regex]::Escape($heading))") {
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

# 7. Validate worklog content
$worklogContent = Get-Content $worklog -Raw
$worklogRequirements = @(
    "P1-02", "Person 1", "Person 2",
    "Verify-P102Docs.ps1",
    "001-backend-boundary.md", "002-authentication.md", "api-errors.md",
    "8 projects",
    "43 integration tests",
    "104 solution tests",
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

Write-Host "SUCCESS: All P1-02 documentation contracts, exact use-case mappings, stable role codes, and dependency checks passed!" -ForegroundColor Green
exit 0
