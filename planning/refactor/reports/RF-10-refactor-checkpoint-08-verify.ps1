$ErrorActionPreference = 'Stop'
$zipPath = Join-Path $PSScriptRoot 'RF-10-refactor-checkpoint-08-handoff.zip'
$temp = Join-Path $env:TEMP 'roadguard-rf10-checkpoint08-verify'
Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue
Expand-Archive $zipPath $temp
$manifest = Get-Content (Join-Path $temp 'manifest.json') -Raw | ConvertFrom-Json
$bad = @()
foreach ($entry in $manifest.entries) {
  $path = Join-Path $temp ($entry.path.Replace('/', '\'))
  if (!(Test-Path $path) -or (Get-Item $path).Length -ne $entry.size -or (Get-FileHash $path -Algorithm SHA256).Hash -ne $entry.sha256) { $bad += $entry.path }
}
"manifest_entries=$($manifest.entries.Count) missing_or_mismatch=$($bad.Count)"
if ($bad.Count -gt 0) { throw ($bad -join ',') }
