$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$stage = Join-Path $env:TEMP 'roadguard-rf10-checkpoint09-stage'
Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
$stage = (New-Item -ItemType Directory -Force $stage).FullName
$package = Join-Path $PSScriptRoot 'RF-10-refactor-checkpoint-09-handoff.zip'
$files = @(
  'planning/refactor/reports/RF-10-refactor-checkpoint-09-README.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-09-acceptance-matrix.md',
  'planning/refactor/reports/RF-10-03-C02.md',
  'planning/refactor/10-survey-request-task-assignment-characterization.md',
  'tests/RoadGuardSystem.ApiTests/Surveys/Rf1003RequestTaskAssignmentCharacterizationTests.cs',
  'RoadGuardSystem.API/Controllers/SurveyV2Controller.cs',
  'RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs',
  'RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs',
  'RoadGuardSystem.BusinessObjects/Messaging/OutboxMessage.cs',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint09-final.trx',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint09-final-console-run.trx',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint09-final-console.txt'
)
foreach ($file in $files) {
  $source = Join-Path $root $file
  if (!(Test-Path $source)) { throw "Missing package file: $file" }
  $destination = Join-Path $stage $file
  New-Item -ItemType Directory -Force (Split-Path $destination) | Out-Null
  Copy-Item $source $destination -Force
}
$pre = Join-Path $env:TEMP 'roadguard-rf10-checkpoint09-presnapshot-20261001'
foreach ($file in @('tests/RoadGuardSystem.ApiTests/Surveys/Rf1003RequestTaskAssignmentCharacterizationTests.cs','planning/refactor/reports/RF-10-03-C02.md','planning/refactor/10-survey-request-task-assignment-characterization.md')) {
  $source = Join-Path $pre $file
  if (Test-Path $source) {
    $destination = Join-Path $stage "snapshot-before-correction/$file"
    New-Item -ItemType Directory -Force (Split-Path $destination) | Out-Null
    Copy-Item $source $destination -Force
  }
}
$diff = Join-Path $stage 'task-specific-diff.txt'
"Base HEAD: $((git -C $root rev-parse HEAD))`r`nBranch: $((git -C $root branch --show-current))`r`nPatch applicability: NOT_VERIFIED`r`nCheckpoint 08 provenance limitation is retained; its pre-edit snapshot was not reconstructed." | Set-Content $diff
foreach ($file in @('tests/RoadGuardSystem.ApiTests/Surveys/Rf1003RequestTaskAssignmentCharacterizationTests.cs','planning/refactor/reports/RF-10-03-C02.md','planning/refactor/10-survey-request-task-assignment-characterization.md')) {
  $old = Join-Path $pre $file; $new = Join-Path $root $file
  if ((Test-Path $old) -and (Test-Path $new)) { "`r`n===== $file =====" | Add-Content $diff; (git diff --no-index -- $old $new 2>&1 | Out-String) | Add-Content $diff }
}
$manifest = [ordered]@{ baseHead = (git -C $root rev-parse HEAD); branch = (git -C $root branch --show-current); patchApplicability = 'NOT_VERIFIED'; entries = @() }
Get-ChildItem $stage -File -Recurse | ForEach-Object { $relative = $_.FullName.Substring($stage.Length).TrimStart('\').Replace('\','/'); $manifest.entries += [ordered]@{ path = $relative; size = $_.Length; sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash } }
$manifest | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $stage 'manifest.json') -Encoding utf8
Remove-Item $package -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $package -CompressionLevel Optimal
(Get-FileHash $package -Algorithm SHA256).Hash | Set-Content "$package.sha256" -Encoding ascii
"entries=$(@((Get-ChildItem $stage -File -Recurse)).Count) zipSha256=$((Get-FileHash $package -Algorithm SHA256).Hash)"
