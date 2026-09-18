[CmdletBinding()]
param (
    [switch]$Live,
    [switch]$SelfTest
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Test-McpConfiguration([string]$Json) {
    $config = $Json | ConvertFrom-Json
    if ($null -eq $config -or $null -eq $config.mcpServers) { throw 'Missing mcpServers object.' }
    $expected = @{
        'microsoft-learn' = 'https://learn.microsoft.com/api/mcp'
        'context7' = 'https://mcp.context7.com/mcp'
    }
    foreach ($name in $expected.Keys) {
        $server = $config.mcpServers.$name
        if ($null -eq $server -or $server.serverUrl -cne $expected[$name]) {
            throw "Missing or incorrect serverUrl for $name."
        }
        if ($server.disabled -eq $true) { throw "$name is disabled." }
        $unexpected = @($server.PSObject.Properties.Name | Where-Object { $_ -notin @('serverUrl', 'disabled') })
        if ($unexpected.Count) { throw "Workspace $name must contain only the public endpoint; keep credentials in private user configuration." }
    }
    return $config
}

function Invoke-Rpc([string]$Url, [hashtable]$Headers, [hashtable]$Message, [switch]$Notification) {
    $body = $Message | ConvertTo-Json -Depth 12 -Compress
    $response = Invoke-WebRequest -UseBasicParsing -Uri $Url -Method Post -Headers $Headers -ContentType 'application/json' -Body $body -TimeoutSec 30
    if ($response.Headers['Mcp-Session-Id']) { $Headers['Mcp-Session-Id'] = [string]$response.Headers['Mcp-Session-Id'] }
    if ($Notification) { return }
    $raw = [string]$response.Content
    if ($raw.TrimStart().StartsWith('{')) { $payload = $raw | ConvertFrom-Json }
    else {
        $payload = $null
        foreach ($line in ($raw -split "`n")) {
            if ($line.StartsWith('data:')) {
                $candidate = $line.Substring(5).Trim() | ConvertFrom-Json
                if ($candidate.id -eq $Message.id) { $payload = $candidate }
            }
        }
    }
    if ($null -eq $payload -or $payload.id -ne $Message.id -or $payload.error) { throw 'MCP returned a missing, mismatched or failed JSON-RPC response.' }
    if ($payload.result.isError -eq $true) { throw 'MCP tool reported an error; inspect service availability or anonymous rate limits.' }
    return $payload.result
}

if ($SelfTest) {
    $valid = '{"mcpServers":{"microsoft-learn":{"serverUrl":"https://learn.microsoft.com/api/mcp"},"context7":{"serverUrl":"https://mcp.context7.com/mcp"}}}'
    $cases = @(
        '{not json', '{}',
        $valid.Replace('serverUrl', 'url'),
        $valid.Replace('https://mcp.context7.com/mcp', 'https://example.invalid/mcp'),
        $valid.Replace('"context7":{', '"context7":{"disabled":true,'),
        $valid.Replace('"context7":{', '"context7":{"headers":{"Authorization":"fixture-only"},')
    )
    foreach ($case in $cases) {
        $rejected = $false
        try { $null = Test-McpConfiguration $case } catch { $rejected = $true }
        if (-not $rejected) { throw 'Invalid configuration was accepted.' }
    }
    $null = Test-McpConfiguration $valid
    Write-Host 'PASS: six invalid configurations rejected; valid public configuration accepted.'
    exit 0
}

$configPath = Join-Path $repo '.agents/mcp_config.json'
if (-not (Test-Path -LiteralPath $configPath)) { throw 'Missing native workspace .agents/mcp_config.json.' }
$config = Test-McpConfiguration ([IO.File]::ReadAllText($configPath))
$rootRules = [IO.File]::ReadAllText((Join-Path $repo 'AGENTS.md')).Replace("`r`n", "`n")
$mirrorRules = [IO.File]::ReadAllText((Join-Path $repo '.antigravity/AGENTS.md')).Replace("`r`n", "`n")
if ($rootRules -cne $mirrorRules) { throw 'Root AGENTS.md and Antigravity mirror differ.' }
$paths = @(
    '.agents/rules/roadguard.md', '.agents/skills/roadguard-agile-delivery/SKILL.md',
    '.antigravity/skills/roadguard-agile-delivery/SKILL.md',
    '.antigravity/skills/roadguard-agile-delivery/references/antigravity-handoff.md',
    '.antigravity/skills/roadguard-agile-delivery/references/csharp-dotnet-stack.md',
    '.antigravity/skills/roadguard-agile-delivery/references/negative-first-workflow.md',
    '.antigravity/skills/roadguard-agile-delivery/references/mcp-tools.md'
)
foreach ($relative in $paths) {
    $path = Join-Path $repo $relative
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing discovery/reference file: $relative" }
    $content = [IO.File]::ReadAllText($path)
    foreach ($link in [regex]::Matches($content, '\[[^\]]+\]\(([^)]+)\)')) {
        $target = $link.Groups[1].Value.Split('#')[0]
        if ($target -and $target -notmatch '^(https?://|mailto:)') {
            if (-not (Test-Path -LiteralPath (Join-Path (Split-Path $path) $target))) { throw "Broken relative link in $relative" }
        }
    }
}
Write-Host 'PASS: workspace MCP configuration, mirrored rules and discovery/reference paths.'
if (-not $Live) { exit 0 }

foreach ($name in @('microsoft-learn', 'context7')) {
    $url = $config.mcpServers.$name.serverUrl
    $headers = @{ Accept = 'application/json, text/event-stream' }
    $init = Invoke-Rpc $url $headers @{
        jsonrpc = '2.0'; id = 1; method = 'initialize'; params = @{
            protocolVersion = '2024-11-05'; capabilities = @{}
            clientInfo = @{ name = 'roadguard-setup-verifier'; version = '1.0.0' }
        }
    }
    $headers['MCP-Protocol-Version'] = $init.protocolVersion
    Invoke-Rpc $url $headers @{ jsonrpc = '2.0'; method = 'notifications/initialized' } -Notification
    $list = Invoke-Rpc $url $headers @{ jsonrpc = '2.0'; id = 2; method = 'tools/list' }
    $names = @($list.tools | ForEach-Object { $_.name })
    if ($name -eq 'microsoft-learn') {
        if ('microsoft_docs_search' -notin $names) { throw 'Microsoft Learn documentation search tool is missing.' }
        $result = Invoke-Rpc $url $headers @{
            jsonrpc = '2.0'; id = 3; method = 'tools/call'; params = @{
                name = 'microsoft_docs_search'; arguments = @{ query = 'EF Core 8 SQL Server optimistic concurrency rowversion' }
            }
        }
    } else {
        if ('resolve-library-id' -notin $names -or 'query-docs' -notin $names) { throw 'Context7 documentation tools are missing.' }
        $result = Invoke-Rpc $url $headers @{
            jsonrpc = '2.0'; id = 3; method = 'tools/call'; params = @{
                name = 'resolve-library-id'; arguments = @{ libraryName = 'Testcontainers for .NET'; query = 'SQL Server container integration tests in C#' }
            }
        }
        $text = ($result.content | Where-Object { $_.type -eq 'text' } | ForEach-Object { $_.text }) -join "`n"
        $idMatch = [regex]::Match($text, 'Context7-compatible library ID:\s*(/[^\s]+)')
        if (-not $idMatch.Success) { throw 'Context7 did not resolve a usable library ID; no successful retrieval is claimed.' }
        $result = Invoke-Rpc $url $headers @{
            jsonrpc = '2.0'; id = 4; method = 'tools/call'; params = @{
                name = 'query-docs'; arguments = @{ libraryId = $idMatch.Groups[1].Value; query = 'Start a Microsoft SQL Server container for a C# integration test' }
            }
        }
    }
    $text = ($result.content | Where-Object { $_.type -eq 'text' } | ForEach-Object { $_.text }) -join "`n"
    if ([string]::IsNullOrWhiteSpace($text)) { throw "$name returned no documentation." }
    Write-Host "PASS: $name initialize, tools/list and public documentation query ($($text.Length) characters)."
}
