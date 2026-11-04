# Normalize Compose without starting containers or reading local .env secrets.
[CmdletBinding()]
param([string]$RepoRoot = '')
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Definition) '../..')).Path
}
$taskEmptyEnv = [IO.Path]::GetTempFileName()
$taskOldPassword = $env:MSSQL_SA_PASSWORD
try {
    $env:MSSQL_SA_PASSWORD = 'ValidationOnly_NotAnAccount_9!'
    function Read-Compose([string]$File) {
        $result = & docker compose --env-file $taskEmptyEnv -f (Join-Path $RepoRoot $File) config --format json 2>$null
        if ($LASTEXITCODE -ne 0) { throw "Compose normalization failed: $File (configuration withheld)" }
        return ($result | ConvertFrom-Json)
    }
    function Assert-Budget($Config) {
        [long]$total = 0
        foreach ($entry in $Config.services.PSObject.Properties) {
            $service = $entry.Value
            if (-not $service.mem_limit -or [long]$service.mem_limit -le 0) { throw "Missing memory cap: $($entry.Name)" }
            if (-not $service.cpus -or [double]$service.cpus -le 0 -or [double]$service.cpus -gt 2) { throw "Invalid CPU cap: $($entry.Name)" }
            $total += [long]$service.mem_limit
        }
        if ($total -gt 4GB) { throw 'Container budget exceeds 4 GiB VM ceiling' }
    }
    $root = Read-Compose 'docker-compose.yml'
    $handoff = Read-Compose 'deploy/fe-handoff/compose.yaml'
    foreach ($script in @('sql-start.sh','tls-init.sh')) {
        $bytes = [IO.File]::ReadAllBytes((Join-Path $RepoRoot "deploy/fe-handoff/$script"))
        if ($bytes -contains 13) { throw "Linux entrypoint contains CR bytes: $script; normalize LF before Docker build" }
    }
    $attributes = Get-Content -LiteralPath (Join-Path $RepoRoot '.gitattributes')
    if ($attributes -notcontains 'deploy/fe-handoff/*.sh text eol=lf') { throw 'Handoff shell scripts require LF checkout policy' }
    Assert-Budget $root
    Assert-Budget $handoff
    foreach ($sql in @($root.services.sqlserver, $handoff.services.sql)) {
        if ([long]$sql.mem_limit -lt 2GB) { throw 'SQL Linux startup requires a container cap of at least 2 GiB' }
        if ([int]$sql.environment.MSSQL_MEMORY_LIMIT_MB -ne 1024) { throw 'SQL engine budget must remain 1024 MB' }
    }
    if ($handoff.services.sql.ports) { throw 'Handoff SQL must not publish a host port' }
    foreach ($name in @('api','minio','mailpit')) {
        foreach ($port in $handoff.services.$name.ports) {
            if ($port.host_ip -ne '127.0.0.1') { throw "Non-loopback handoff port: $name" }
        }
    }
    if (-not $handoff.networks.internal.internal) { throw 'Private backend network lost isolation' }
    foreach ($name in @('state','sql-data','objects','mail','keys','scenario-progress')) {
        if ($handoff.volumes.PSObject.Properties.Name -notcontains $name) { throw "Persistent volume missing: $name" }
    }
    $ignore = Get-Content -LiteralPath (Join-Path $RepoRoot '.dockerignore')
    foreach ($script in @('scenarios.py','demo_ai.py','demo_collections.py','demo_repairs.py')) {
        if ($ignore -notcontains "!deploy/fe-handoff/$script") { throw "Explicit build source entry missing: $script" }
    }
    # Exercise the same budget gate with bad normalized configuration, without a daemon.
    $blocked = 0
    foreach ($fixture in @(
        '{"services":{"api":{"cpus":1}}}',
        '{"services":{"api":{"mem_limit":1024,"cpus":0}}}',
        '{"services":{"api":{"mem_limit":1024,"cpus":3}}}',
        '{"services":{"api":{"mem_limit":4294967297,"cpus":1}}}'
    )) {
        try { Assert-Budget ($fixture | ConvertFrom-Json) } catch { $blocked++ }
    }
    if ($blocked -ne 4) { throw 'Budget negative fixtures did not all block' }
    Write-Output 'PASS: root + six handoff service budgets, SQL minimum/engine limit, loopback/private network, six persistent volumes, four explicit build sources; 4 negative fixtures blocked.'
} finally {
    $env:MSSQL_SA_PASSWORD = $taskOldPassword
    Remove-Item -LiteralPath $taskEmptyEnv -Force
}
