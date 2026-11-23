param([string]$DataRoot='D:/RoadGuard-AI-LocalTest/pm-crew')
$ErrorActionPreference='Stop'
$repo=(Resolve-Path -LiteralPath "$PSScriptRoot/../..").Path
if ([IO.Path]::GetFullPath($DataRoot) -ne 'D:\RoadGuard-AI-LocalTest\pm-crew') { throw 'This opt-in fixture uses only the documented private D: root.' }
foreach($port in @(5087,8997)){if(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue){throw "Port $port already occupied; inspect its owner before restarting."}}
if(Test-Path -LiteralPath "$DataRoot/host.pid"){
    $previousPid=[int](Get-Content -LiteralPath "$DataRoot/host.pid" -Raw)
    $previous=Get-CimInstance Win32_Process -Filter "ProcessId=$previousPid"
    if($previous -and $previous.CommandLine -match 'PmCrewHost.dll'){throw 'This fixture is already starting or running; inspect its protected logs.'}
}
New-Item -ItemType Directory -Force -Path $DataRoot | Out-Null
$identity=[Security.Principal.WindowsIdentity]::GetCurrent().Name
& icacls $DataRoot /inheritance:r /grant:r "${identity}:(OI)(CI)F" 'SYSTEM:(OI)(CI)F' | Out-Null
if($LASTEXITCODE -ne 0){throw 'Unable to restrict private fixture directory ACL.'}
$env:ROADGUARD_RUN_PM_CREW='1'
$env:TEMP=$DataRoot
$env:TMP=$DataRoot
$dll=Join-Path $PSScriptRoot 'PmCrewHost/bin/Debug/net8.0/PmCrewHost.dll'
if(-not(Test-Path -LiteralPath $dll)){throw 'Build tests/local-ai/PmCrewHost/PmCrewHost.csproj first.'}
$process=Start-Process -FilePath 'dotnet' -ArgumentList @('"'+$dll+'"','"'+$repo+'"') -WorkingDirectory $repo -WindowStyle Hidden -PassThru -RedirectStandardOutput "$DataRoot/host.out.log" -RedirectStandardError "$DataRoot/host.err.log"
$process.Id | Set-Content -LiteralPath "$DataRoot/host.pid"
Write-Output "PM_CREW_STARTING PID=$($process.Id); wait for PM_CREW_READY in protected host.out.log"
