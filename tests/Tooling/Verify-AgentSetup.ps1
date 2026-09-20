[CmdletBinding()]
param ([switch]$SelfTest)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$errors = [Collections.Generic.List[string]]::new()

function Require-File([string]$RelativePath) {
    $path = Join-Path $repo $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $errors.Add("Missing file: $RelativePath")
        return $null
    }
    return $path
}

function Require-Text([string]$Name, [string]$Content, [string[]]$Tokens) {
    foreach ($token in $Tokens) {
        if (-not $Content.Contains($token, [StringComparison]::Ordinal)) {
            $errors.Add("$Name missing token: $token")
        }
    }
}

$required = @(
    'AGENTS.md',
    '.agents/mcp_config.json',
    '.agents/rules/roadguard.md',
    '.agents/skills/roadguard-endpoint-delivery/SKILL.md',
    '.agents/skills/roadguard-endpoint-delivery/agents/openai.yaml',
    'docs/prompts/RoadGuard_Task_Workflow.md',
    'docs/diagram/RoadGuard_Task_Log_Template.md',
    'planning/RoadGuard_Plan_Person_1.md',
    'planning/RoadGuard_Plan_Person_2.md',
    'Directory.Build.props'
)

$paths = @{}
foreach ($relative in $required) { $paths[$relative] = Require-File $relative }

if (Test-Path -LiteralPath (Join-Path $repo '.antigravity')) {
    $errors.Add('Retired .antigravity directory still exists.')
}

$skillRoot = Join-Path $repo '.agents/skills'
$skillNames = @(Get-ChildItem -LiteralPath $skillRoot -Directory | Select-Object -ExpandProperty Name)
if ($skillNames.Count -ne 1 -or $skillNames[0] -cne 'roadguard-endpoint-delivery') {
    $errors.Add("Expected one rebuilt skill; found: $($skillNames -join ', ')")
}

if ($errors.Count -eq 0) {
    $agents = Get-Content -Raw -LiteralPath $paths['AGENTS.md']
    $skill = Get-Content -Raw -LiteralPath $paths['.agents/skills/roadguard-endpoint-delivery/SKILL.md']
    $ui = Get-Content -Raw -LiteralPath $paths['.agents/skills/roadguard-endpoint-delivery/agents/openai.yaml']
    $prompt = Get-Content -Raw -LiteralPath $paths['docs/prompts/RoadGuard_Task_Workflow.md']
    $plan1 = Get-Content -Raw -LiteralPath $paths['planning/RoadGuard_Plan_Person_1.md']
    $plan2 = Get-Content -Raw -LiteralPath $paths['planning/RoadGuard_Plan_Person_2.md']

    Require-Text 'AGENTS.md' $agents @('Before edits, show:', '5-8 line contract', 'Http/*.http', 'at or below 500 lines')
    Require-Text 'skill' $skill @('name: roadguard-endpoint-delivery', 'description: Use when', 'Scope gate', 'Endpoint contract', 'Verification')
    Require-Text 'openai.yaml' $ui @('display_name:', 'short_description:', '$roadguard-endpoint-delivery')
    Require-Text 'workflow prompt' $prompt @('In scope', 'Out of scope', 'Dong y <TASK-ID>', '3-5 lat cat')
    Require-Text 'Person 1 plan' $plan1 @('Historical `Done` rows', 'P1-70', 'risk catalogue')
    Require-Text 'Person 2 plan' $plan2 @('Historical `Done` rows', 'scope card', 'risk catalogue')

    if ((Get-Content -LiteralPath $paths['AGENTS.md']).Count -gt 50) {
        $errors.Add('AGENTS.md exceeds 50 lines.')
    }

    foreach ($relative in $required) {
        if ((Get-Content -LiteralPath $paths[$relative]).Count -gt 500) {
            $errors.Add("Managed file exceeds 500 lines: $relative")
        }
    }

    [xml]$props = Get-Content -Raw -LiteralPath $paths['Directory.Build.props']
    $group = $props.Project.PropertyGroup
    foreach ($property in @('Nullable', 'TreatWarningsAsErrors', 'AnalysisMode', 'EnforceCodeStyleInBuild')) {
        if ([string]$group.$property -cne 'enable' -and [string]$group.$property -cne 'true' -and [string]$group.$property -cne 'Recommended') {
            $errors.Add("Directory.Build.props has an invalid $property value.")
        }
    }

    $config = Get-Content -Raw -LiteralPath $paths['.agents/mcp_config.json'] | ConvertFrom-Json
    foreach ($server in @('microsoft-learn', 'context7')) {
        if ($null -eq $config.mcpServers.$server) { $errors.Add("Missing MCP server: $server") }
    }

    foreach ($relative in @('AGENTS.md', '.agents/rules/roadguard.md', '.agents/skills/roadguard-endpoint-delivery/SKILL.md', 'docs/prompts/RoadGuard_Task_Workflow.md')) {
        $path = $paths[$relative]
        $content = Get-Content -Raw -LiteralPath $path
        foreach ($match in [regex]::Matches($content, '\[[^\]]+\]\(([^)]+)\)')) {
            $target = $match.Groups[1].Value.Split('#')[0]
            if ($target -and $target -notmatch '^(https?://|mailto:)') {
                $resolved = Join-Path (Split-Path $path) $target
                if (-not (Test-Path -LiteralPath $resolved)) { $errors.Add("Broken link in ${relative}: $target") }
            }
        }
    }
}

if ($SelfTest) {
    if ($errors.Count -ne 0) { throw "Self-test requires a valid workspace first: $($errors -join '; ')" }
    $errors.Add('fixture failure')
    if ($errors.Count -ne 1) { throw 'Verifier self-test did not detect its fixture failure.' }
    Write-Host 'PASS: verifier detects an injected failure.'
    exit 0
}

if ($errors.Count -gt 0) { throw ($errors -join [Environment]::NewLine) }
Write-Host 'SUCCESS: compact agent rules, skill discovery, plans, prompt, compiler gates and MCP config are valid.'
