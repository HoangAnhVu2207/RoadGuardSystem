$ErrorActionPreference='Stop'
$zip=Join-Path $PSScriptRoot 'RF-10-refactor-checkpoint-07-handoff.zip'
$out=Join-Path $env:TEMP 'roadguard-rf10-checkpoint07-verify'
Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
Expand-Archive -LiteralPath $zip -DestinationPath $out
$manifest=Get-Content (Join-Path $out 'manifest.json') -Raw | ConvertFrom-Json
$bad=0
foreach($entry in $manifest.entries){
  $path=Join-Path $out ($entry.path -replace '/','\')
  if(!(Test-Path $path)){ Write-Output "missing $($entry.path)"; $bad++; continue }
  if((Get-Item $path).Length -ne $entry.size -or (Get-FileHash $path -Algorithm SHA256).Hash -ne $entry.sha256){ Write-Output "mismatch $($entry.path)"; $bad++ }
}
if($bad){ exit 1 }
Write-Output "manifest-pass entries=$($manifest.entries.Count)"
