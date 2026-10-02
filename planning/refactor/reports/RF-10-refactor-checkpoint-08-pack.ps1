$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$stage = Join-Path $env:TEMP 'roadguard-rf10-checkpoint08-stage'
$package = Join-Path $PSScriptRoot 'RF-10-refactor-checkpoint-08-handoff.zip'
Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $stage | Out-Null
$stage = (Get-Item $stage).FullName
$files = @(
  'planning/refactor/reports/RF-10-refactor-checkpoint-08-README.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-08-case-matrix.md',
  'planning/refactor/reports/RF-10-03-C02.md',
  'planning/refactor/10-survey-request-task-assignment-characterization.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-07-README.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-07-case-matrix.md',
  'tests/RoadGuardSystem.ApiTests/Surveys/Rf1003RequestTaskAssignmentCharacterizationTests.cs',
  'RoadGuardSystem.API/Controllers/SurveyV2Controller.cs',
  'RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs',
  'RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs',
  'RoadGuardSystem.BusinessObjects/Surveys/SurveyAssignment.cs',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint08-final.trx',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint08-final2.trx',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint08-final3.trx'
)
foreach ($file in $files) {
  $source = Join-Path $root $file
  if (!(Test-Path $source)) { throw "Missing package file: $file" }
  $destination = Join-Path $stage $file
  New-Item -ItemType Directory -Force (Split-Path $destination) | Out-Null
  Copy-Item $source $destination -Force
}
$diff = Join-Path $stage 'task-specific-diff.txt'
"Base HEAD: $((git -C $root rev-parse HEAD))`r`nBranch: $((git -C $root branch --show-current))`r`nPatch applicability: NOT_VERIFIED`r`nNote: checkpoint-07 baseline snapshot is retained; a pre-first-edit byte snapshot for this correction was not captured." | Set-Content $diff
$manifest = [ordered]@{ baseHead = (git -C $root rev-parse HEAD); branch = (git -C $root branch --show-current); patchApplicability = 'NOT_VERIFIED'; entries = @() }
Get-ChildItem $stage -File -Recurse | ForEach-Object {
  $relative = $_.FullName.Substring($stage.Length).TrimStart('\').Replace('\','/')
  $manifest.entries += [ordered]@{ path = $relative; size = $_.Length; sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash }
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $stage 'manifest.json') -Encoding utf8
Remove-Item $package -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $package -CompressionLevel Optimal
$hash = (Get-FileHash $package -Algorithm SHA256).Hash
$hash | Set-Content "$package.sha256" -Encoding ascii
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($package)
try { "entries=$(@($zip.Entries).Count) manifest=$([bool]($zip.Entries | Where-Object FullName -eq 'manifest.json')) zipSha256=$hash" } finally { $zip.Dispose() }
