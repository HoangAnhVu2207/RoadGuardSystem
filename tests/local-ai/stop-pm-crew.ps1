$ErrorActionPreference='Stop'
$root='D:/RoadGuard-AI-LocalTest/pm-crew'
$hostProcessId=[int](Get-Content -LiteralPath "$root/host.pid" -Raw)
$process=Get-CimInstance Win32_Process -Filter "ProcessId=$hostProcessId"
if(-not $process -or $process.CommandLine -notmatch 'PmCrewHost.dll'){throw 'Host PID identity no longer matches this fixture.'}
New-Item -ItemType File -Force -Path "$root/stop.request" | Out-Null
Write-Output 'Graceful stop requested. Host stops its own private worker and disposes its SQL/MinIO containers; runtime evidence remains on D:.'

\n