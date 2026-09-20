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
$p100Worklog = Join-Path $RepoRoot "docs\worklogs\P1-00-completion.md"
$p101Worklog = Join-Path $RepoRoot "docs\worklogs\P1-01-completion.md"
$dataDictionary = Join-Path $RepoRoot "docs\diagram\RoadGuard_Data_Dictionary_v1.md"
$erd = Join-Path $RepoRoot "docs\diagram\RoadGuard_ERD_v1.md"
$domainModel = Join-Path $RepoRoot "docs\diagram\RoadGuard_Domain_Model_v1.md"
$useCases = Join-Path $RepoRoot "docs\diagram\Dac_ta_UseCase_v2.md"
$userStories = Join-Path $RepoRoot "docs\diagram\User_Stories_Acceptance_Criteria_v2.md"
$person1Plan = Join-Path $RepoRoot "planning\RoadGuard_Plan_Person_1.md"
$person2Plan = Join-Path $RepoRoot "planning\RoadGuard_Plan_Person_2.md"
$rootAgentRules = Join-Path $RepoRoot "AGENTS.md"
$taskLogTemplate = Join-Path $RepoRoot "docs\diagram\RoadGuard_Task_Log_Template.md"

$docs = @(
    $adr001, $adr002, $apiErrors, $worklog, $p100Worklog, $p101Worklog,
    $dataDictionary, $erd, $domainModel, $useCases, $userStories,
    $person1Plan, $person2Plan, $rootAgentRules, $taskLogTemplate
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
    $content = Get-Content $doc -Raw -Encoding UTF8
    if ($content -match '\[[^\]]+\]\(file:///[^)]+\)') {
        $errors += "Prohibited absolute 'file://' link found in: $doc. Use portable relative Markdown links instead."
    }
}

# 3. Validate relative links resolve correctly
$linkRegex = '\[([^\]]+)\]\(([^)]+)\)'
foreach ($doc in $docs) {
    $content = Get-Content $doc -Raw -Encoding UTF8
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
$adr001Content = Get-Content $adr001 -Raw -Encoding UTF8
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
    [regex]::Unescape("Repositories -> BusinessObjects v\u00e0 DTOs"),
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
$adr002Content = Get-Content $adr002 -Raw -Encoding UTF8

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
$dataDictionaryContent = Get-Content $dataDictionary -Raw -Encoding UTF8
$erdContent = Get-Content $erd -Raw -Encoding UTF8
$domainModelContent = Get-Content $domainModel -Raw -Encoding UTF8
$useCasesContent = Get-Content $useCases -Raw -Encoding UTF8
$userStoriesContent = Get-Content $userStories -Raw -Encoding UTF8
$person1PlanContent = Get-Content $person1Plan -Raw -Encoding UTF8
$person2PlanContent = Get-Content $person2Plan -Raw -Encoding UTF8
$rootAgentRulesContent = Get-Content $rootAgentRules -Raw -Encoding UTF8
$taskLogTemplateContent = Get-Content $taskLogTemplate -Raw -Encoding UTF8

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
    foreach ($requiredToken in @(
        [regex]::Unescape("c\u00f3 hi\u1ec7u l\u1ef1c ngay"),
        [regex]::Unescape("thu h\u1ed3i to\u00e0n b\u1ed9 phi\u00ean")
    )) {
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

# P1-70 prospective workflow; historical P1 Wave 0 evidence stays immutable.
foreach ($plan in @(
    @{ Name = "Person 1 plan"; Content = $person1PlanContent },
    @{ Name = "Person 2 plan"; Content = $person2PlanContent }
)) {
    foreach ($requiredToken in @(
        'Historical `Done` rows',
        'scope card',
        'explicit owner approval',
        'risk catalogue'
    )) {
        if ($plan.Content -notmatch [regex]::Escape($requiredToken)) {
            $errors += "$($plan.Name) missing lightweight workflow token: '$requiredToken'"
        }
    }
}

# Preserve the approved Wave 0 synchronization evidence while validating the
# current status table independently from historical prose.
foreach ($requiredToken in @(
    'P2-00` | `Done`',
    'P2-01` |',
    'P2-02` |',
    '3e13ca6',
    'b2662fe'
)) {
    if ($person2PlanContent -notmatch [regex]::Escape($requiredToken)) {
        $errors += "Person 2 plan missing approved Wave 0 status/synchronization token: '$requiredToken'"
    }
}

# P2-03: Read the current status section, not historical prose elsewhere.
# Legal progression must not require editing the verifier's source.
$allowedStatuses = @('Not started', 'In Progress', 'Blocked', 'Ready for review',
    'Ready for Codex review', 'Ready for cross-review', 'Changes requested', 'Done')
$currentStatuses = @{}
$taskDependencies = @{}
foreach ($planContent in @($person1PlanContent, $person2PlanContent)) {
    $section = [regex]::Match($planContent, '(?ms)^## Current status[^\r\n]*\r?\n(?<body>.*?)(?=^## |\z)')
    foreach ($row in [regex]::Matches($section.Groups['body'].Value,
        '(?m)^\|\s*`(?<id>P[12]-\d{2})`\s*\|\s*`(?<status>[^`]+)`\s*\|')) {
        $id = $row.Groups['id'].Value
        $status = $row.Groups['status'].Value
        if ($currentStatuses.ContainsKey($id)) { $errors += "PLAN_STATUS: duplicate current row for $id" }
        if ($status -notin $allowedStatuses) { $errors += "PLAN_STATUS: invalid status for $id : $status" }
        $currentStatuses[$id] = $status
    }
    foreach ($row in [regex]::Matches($planContent,
        '(?m)^\|\s*`(?<id>P[12]-\d{2})`\s*/[^|]+\|(?<trace>[^|]+)\|')) {
        $id = $row.Groups['id'].Value
        if ($taskDependencies.ContainsKey($id)) { $errors += "PLAN_DEPENDENCY: duplicate task definition $id" }
        $taskDependencies[$id] = @([regex]::Matches($row.Groups['trace'].Value, '\bP[12]-\d{2}\b') |
            ForEach-Object { $_.Value } | Select-Object -Unique)
    }
}
foreach ($id in @('P1-00', 'P1-01', 'P1-02', 'P1-03', 'P2-00', 'P2-01', 'P2-02')) {
    if (-not $currentStatuses.ContainsKey($id)) { $errors += "PLAN_STATUS: missing current row for $id" }
}
foreach ($id in $currentStatuses.Keys) {
    if (-not $taskDependencies.ContainsKey($id)) { $errors += "PLAN_STATUS: undefined current task $id" }
}
foreach ($id in $taskDependencies.Keys) {
    foreach ($dependency in $taskDependencies[$id]) {
        if (-not $taskDependencies.ContainsKey($dependency)) {
            $errors += "PLAN_DEPENDENCY: $id references undefined $dependency"
        }
    }
}
# These are cross-owner handoffs, not assumptions based on wave/row ordering.
foreach ($edge in @(
    @('P2-10', 'P2-02'), @('P2-11', 'P2-20'), @('P2-20', 'P2-10'),
    @('P1-12', 'P1-10'), @('P1-12', 'P2-11')
)) {
    if (-not $taskDependencies.ContainsKey($edge[0]) -or $edge[1] -notin $taskDependencies[$edge[0]]) {
        $errors += "PLAN_DEPENDENCY: $($edge[0]) must declare $($edge[1])"
    }
}
# Kahn's algorithm: a nonempty remainder is a cycle or unresolved dependency.
$remaining = @{}
foreach ($id in $taskDependencies.Keys) { $remaining[$id] = $taskDependencies[$id] }
$resolvedTasks = @{}
while ($remaining.Count -gt 0) {
    $ready = @($remaining.Keys | Where-Object {
        @($remaining[$_] | Where-Object { -not $resolvedTasks.ContainsKey($_) }).Count -eq 0
    })
    if ($ready.Count -eq 0) {
        $errors += "PLAN_CYCLE: unresolved dependency graph: $(($remaining.Keys | Sort-Object) -join ', ')"
        break
    }
    foreach ($id in $ready) { $resolvedTasks[$id] = $true; $remaining.Remove($id) }
}

foreach ($requiredToken in @(
    'Before edits, show:',
    '5-8 line contract',
    'Http/*.http',
    'at or below 500 lines',
    'Person 1 (`anh`)',
    'Person 2 (`huy`)'
)) {
    if ($rootAgentRulesContent -notmatch [regex]::Escape($requiredToken)) {
        $errors += "root AGENTS.md missing current workflow token: '$requiredToken'"
    }
}

foreach ($requiredToken in @(
    "Scope approval",
    "In scope:",
    "Out of scope:",
    "Verification tier and commands:",
    "Endpoint contract"
)) {
    if ($taskLogTemplateContent -notmatch [regex]::Escape($requiredToken)) {
        $errors += "Task-log template missing scope field: '$requiredToken'"
    }
}

# 6. Validate api-errors.md content
$apiErrorsContent = Get-Content $apiErrors -Raw -Encoding UTF8
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
$worklogContent = Get-Content $worklog -Raw -Encoding UTF8
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

# 8. Validate final Person 2 cross-review evidence for the complete P1 Wave 0 correction set.
$crossReviewRequirements = @(
    @{ Task = "P1-00"; Commit = "eb246f2"; Path = $p100Worklog },
    @{ Task = "P1-01"; Commit = "3072852"; Path = $p101Worklog },
    @{ Task = "P1-02"; Commit = "5223105"; Path = $worklog }
)
$reviewChecklist = "authorization, state transitions, immutability/versioning, idempotency, concurrency, audit, and missing tests"

foreach ($review in $crossReviewRequirements) {
    $reviewContent = Get-Content $review.Path -Raw -Encoding UTF8
    foreach ($requiredToken in @(
        "Final Person 2 cross-review (2026-09-17)",
        "**Reviewer:** Person 2",
        "**Reviewed commit:** ``$($review.Commit)``",
        $reviewChecklist,
        "**Review result:** Accepted with no open findings",
        "**Final status:** ``Done``"
    )) {
        if ($reviewContent -notmatch [regex]::Escape($requiredToken)) {
            $errors += "$($review.Task) worklog missing final cross-review evidence token: '$requiredToken'"
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host "FAILURE: Verification found $($errors.Count) errors:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "SUCCESS: All P1-02 documentation contracts, exact use-case mappings, stable role codes, and dependency checks passed!" -ForegroundColor Green
exit 0
