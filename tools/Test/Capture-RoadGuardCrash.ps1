[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Container,
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [DateTimeOffset]$SinceUtc = [DateTimeOffset]::UtcNow.AddDays(-1)
)
$ErrorActionPreference = 'Stop'
# Read-only diagnostics. No restart, kill, seed, database call or dump attachment.
if (-not [IO.Path]::IsPathRooted($OutputDirectory)) { throw 'Use an absolute evidence path outside the repository.' }
$destination = [IO.Path]::GetFullPath($OutputDirectory)
$repo = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim().Replace('/', [IO.Path]::DirectorySeparatorChar)
$prefix = $repo.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if ($destination.Equals($repo, [StringComparison]::OrdinalIgnoreCase) -or
    $destination.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Crash evidence must remain outside Git.' }
$inspection = & docker inspect $Container
if ($LASTEXITCODE -ne 0) { throw 'Container inspect failed; no mutation was attempted.' }
$item = @($inspection | ConvertFrom-Json)[0]
New-Item -ItemType Directory -Force -Path $destination | Out-Null
$until = [DateTimeOffset]::UtcNow
[ordered]@{
    capturedAtUtc = $until.ToString('O'); sinceUtc = $SinceUtc.ToUniversalTime().ToString('O')
    id = $item.Id; name = $item.Name; image = $item.Image; state = $item.State
    memoryLimitBytes = $item.HostConfig.Memory; restartCount = $item.RestartCount
    dotnetEntrypoint = if ($item.Config.Entrypoint[0] -eq 'dotnet') { @($item.Config.Entrypoint) } else { $null }
    environmentCaptured = $false
} | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $destination 'container-state.json') -Encoding utf8
& docker logs --timestamps --since $SinceUtc.ToUniversalTime().ToString('O') $item.Id 2>&1 |
    Out-File -LiteralPath (Join-Path $destination 'container.private.log') -Encoding utf8
$logsExit = $LASTEXITCODE
& docker events --since $SinceUtc.ToUnixTimeSeconds() --until $until.ToUnixTimeSeconds() --filter "container=$($item.Id)" --format '{{json .}}' 2>&1 |
    Out-File -LiteralPath (Join-Path $destination 'events.private.jsonl') -Encoding utf8
$eventsExit = $LASTEXITCODE
@"
Read-only capture. logs exit=$logsExit; events exit=$eventsExit.
Docker events may not retain evidence from an older daemon session. Empty events are not proof no crash occurred.
Exit 139 alone does not identify a SIGSEGV cause. Preserve the original command, UTC time, child-process output and exact image.
Logs and future dumps can contain sensitive data. Keep private and review/redact before sharing; never commit them.
If another failure needs reproduction, use a disposable stack/DB with the same image and input. Configure .NET crash dumps there, on a private diagnostic volume, before the attempt. Do not enable a dump or attach a debugger on the shared test runtime by this script.
"@ | Set-Content -LiteralPath (Join-Path $destination 'README.txt') -Encoding utf8
Write-Output "Captured read-only evidence outside Git: $destination"
