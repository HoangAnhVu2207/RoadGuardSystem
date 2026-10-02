$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$zip = Join-Path $repo 'planning/refactor/reports/RF-10-refactor-checkpoint-10-correction-01-handoff.zip'
$verify = Join-Path $env:TEMP 'roadguard-rf10-checkpoint10-verify'
if (Test-Path $verify) { Remove-Item -Recurse -Force $verify }
Expand-Archive -LiteralPath $zip -DestinationPath $verify -Force
$verify = (Get-Item -LiteralPath $verify).FullName
$manifest = Get-Content -Raw (Join-Path $verify 'manifest.json') | ConvertFrom-Json
$manifestPaths = @($manifest | ForEach-Object Path)
if (($manifestPaths | Sort-Object -Unique).Count -ne $manifestPaths.Count) { throw 'Manifest has duplicate paths' }
foreach ($path in $manifestPaths) {
  if ([string]::IsNullOrWhiteSpace($path) -or $path.StartsWith('/') -or $path.Contains('\') -or $path.Split('/').Contains('..')) { throw "Invalid manifest path: $path" }
}
$missing = @()
foreach ($entry in $manifest) {
  $path = Join-Path $verify ($entry.Path -replace '/', '\')
  if (-not (Test-Path -LiteralPath $path)) { $missing += "$($entry.Path):missing"; continue }
  $actual = Get-FileHash -Algorithm SHA256 -LiteralPath $path
  if ($actual.Hash -ne $entry.SHA256 -or (Get-Item -LiteralPath $path).Length -ne [int64]$entry.Size) { $missing += "$($entry.Path):mismatch" }
}
if ($missing.Count -gt 0) { $missing | ForEach-Object { Write-Error $_ }; exit 1 }
$actualPaths = @(Get-ChildItem -LiteralPath $verify -File -Recurse | Where-Object { $_.FullName -ne (Join-Path $verify 'manifest.json') } | ForEach-Object { $_.FullName.Substring($verify.Length + 1).Replace('\','/') })
$unlisted = @($actualPaths | Where-Object { $_ -notin $manifestPaths })
if ($unlisted.Count -gt 0) { $unlisted | ForEach-Object { Write-Error "Unlisted payload: $_" }; exit 1 }
foreach ($required in @('planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint10-build-console-final.txt','planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint10-test-run-metadata-final4.txt')) {
  if (-not (Test-Path (Join-Path $verify ($required -replace '/', '\')))) { throw "Required evidence missing: $required" }
}
"manifest_entries=$($manifest.Count)"
"missing_or_mismatch=0"
"unlisted_payload=0"
"duplicate_paths=0"
