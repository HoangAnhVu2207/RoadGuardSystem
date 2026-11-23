param(
    [string]$Python = 'D:/RoadGuard-AI-LocalTest/venv/Scripts/python.exe',
    [string]$DataRoot = 'D:/RoadGuard-AI-LocalTest/studio',
    [string]$FFmpeg = "$env:LOCALAPPDATA/Temp/roadguard-media-tools/ffmpeg-9.0.2-essentials_build/bin/ffmpeg.exe",
    [string]$FFprobe = "$env:LOCALAPPDATA/Temp/roadguard-media-tools/ffmpeg-9.0.2-essentials_build/bin/ffprobe.exe",
    [string]$BackendUrl = '',
    [string]$FixtureFile = ''
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath "$PSScriptRoot/../..").Path
$ai = Join-Path $repo 'AI/roadguard-ai-engine'
$checkpoint = Join-Path $ai 'weights/trained/RoadGuard_YOLO11_UAVPDD2023_FT95_v1.pt'
foreach ($required in @($Python, $FFmpeg, $FFprobe, $checkpoint)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Missing required file: $required" }
}
$resolvedData = [IO.Path]::GetFullPath($DataRoot)
if (-not $resolvedData.StartsWith('D:\', [StringComparison]::OrdinalIgnoreCase)) { throw 'DataRoot must be on D:' }
if (Get-NetTCPConnection -State Listen -LocalPort 8765 -ErrorAction SilentlyContinue) { throw 'Port 8765 already occupied; inspect the existing studio before starting another.' }
New-Item -ItemType Directory -Force -Path $resolvedData | Out-Null
New-Item -ItemType Directory -Force -Path "$resolvedData/tmp", "$resolvedData/ultralytics" | Out-Null
$env:ROADGUARD_STUDIO_DATA_ROOT = $resolvedData
$env:ROADGUARD_WORKER_CHECKPOINT = $checkpoint
$env:ROADGUARD_WORKER_FFMPEG = $FFmpeg
$env:ROADGUARD_WORKER_FFPROBE = $FFprobe
$env:ROADGUARD_STUDIO_BE_URL = $BackendUrl
$env:ROADGUARD_STUDIO_FIXTURE_FILE = $FixtureFile
$env:TEMP = "$resolvedData/tmp"
$env:TMP = "$resolvedData/tmp"
$env:YOLO_CONFIG_DIR = "$resolvedData/ultralytics"
$process = Start-Process -FilePath $Python -ArgumentList @('-m','uvicorn','src.api.test_studio:create_app_from_env','--factory','--host','127.0.0.1','--port','8765','--no-access-log') -WorkingDirectory $ai -WindowStyle Hidden -PassThru -RedirectStandardOutput "$resolvedData/studio.out.log" -RedirectStandardError "$resolvedData/studio.err.log"
$process.Id | Set-Content -LiteralPath "$resolvedData/studio.pid"
Write-Output "STUDIO_STARTING PID=$($process.Id) URL=http://127.0.0.1:8765"
Write-Output "Logs: $resolvedData/studio.err.log"
Write-Output 'Stop gracefully with the documented studio stop command; do not stop during inference.'
