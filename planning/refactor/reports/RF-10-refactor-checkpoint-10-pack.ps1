$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$stage = Join-Path $env:TEMP 'roadguard-rf10-checkpoint10-stage'
$zip = Join-Path $repo 'planning/refactor/reports/RF-10-refactor-checkpoint-10-correction-01-handoff.zip'
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force $stage | Out-Null
$stage = (Get-Item -LiteralPath $stage).FullName

$files = @(
  'planning/refactor/reports/RF-10-refactor-checkpoint-10-README.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-10-acceptance-matrix.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-10-correction-01-README.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-10-correction-01.md',
  'planning/refactor/reports/RF-10-03-C02.md',
  'planning/refactor/10-survey-request-task-assignment-characterization.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-09-README.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-09-acceptance-matrix.md',
  'tests/RoadGuardSystem.ApiTests/Surveys/Rf1003RequestTaskAssignmentCharacterizationTests.cs',
  'RoadGuardSystem.API/Controllers/SurveyV2Controller.cs',
  'RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs',
  'RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs',
  'RoadGuardSystem.Repositories/Implementations/Surveys/SurveyAssignmentPersistenceService.cs',
  'RoadGuardSystem.BusinessObjects/Idempotency/IdempotencyRecord.cs',
  'RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs',
  'RoadGuardSystem.BusinessObjects/Surveys/SurveyRequest.cs',
  'tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationSqlServerFixture.cs',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint10-build-console-final.txt',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint10-build-run-metadata.txt',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint10-test-console-final4.txt',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint10-test-final4.trx',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint10-test-run-metadata-final4.txt',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint10-test-final.trx',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint10-test-final2.trx',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint10-test-console-final.txt',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint10-test-console-final2.txt',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint10-test-run-metadata-final.txt',
  'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-checkpoint10-test-run-metadata-final2.txt',
  'planning/refactor/reports/RF-10-03-C02-evidence/checkpoint10-source-hashes-before-build.json',
  'planning/refactor/reports/RF-10-03-C02-evidence/checkpoint10-source-hashes-after-test.json'
)

foreach ($relative in $files) {
  $source = Join-Path $repo $relative
  if (-not (Test-Path -LiteralPath $source)) { throw "Missing package input: $relative" }
  $target = Join-Path $stage ($relative -replace '/', '\')
  New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
  Copy-Item -LiteralPath $source -Destination $target -Force
}

$snapshotRoot = Join-Path $env:TEMP 'roadguard-rf10-checkpoint10-correction01-presnapshot-20261001'
foreach ($relative in @(
  'manifest.json',
  'planning/refactor/reports/RF-10-03-C02.md',
  'planning/refactor/10-survey-request-task-assignment-characterization.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-10-README.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-10-acceptance-matrix.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-10-pack.ps1',
  'planning/refactor/reports/RF-10-refactor-checkpoint-10-verify.ps1')) {
  $source = Join-Path $snapshotRoot ($relative -replace '/', '\')
  if (-not (Test-Path -LiteralPath $source)) { throw "Missing snapshot input: $relative" }
  $target = Join-Path $stage ('snapshot-before-correction/' + ($relative -replace '/', '\'))
  New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
  Copy-Item -LiteralPath $source -Destination $target -Force
}

$diffPath = Join-Path $stage 'checkpoint10-task-specific.diff'
Remove-Item -LiteralPath $diffPath -ErrorAction SilentlyContinue
foreach ($relative in @(
  'planning/refactor/reports/RF-10-03-C02.md',
  'planning/refactor/10-survey-request-task-assignment-characterization.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-10-README.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-10-acceptance-matrix.md',
  'planning/refactor/reports/RF-10-refactor-checkpoint-10-pack.ps1',
  'planning/refactor/reports/RF-10-refactor-checkpoint-10-verify.ps1')) {
  $before = Join-Path $snapshotRoot ($relative -replace '/', '\')
  $after = Join-Path $repo ($relative -replace '/', '\')
  Add-Content -LiteralPath $diffPath -Value "===== $relative ====="
  & git diff --no-index --no-color -- $before $after 2>&1 | Add-Content -LiteralPath $diffPath
  if ($LASTEXITCODE -notin @(0,1)) { throw "Diff generation failed for $relative" }
}

$rootManifest = Join-Path $stage 'manifest.json'
$manifest = Get-ChildItem -LiteralPath $stage -File -Recurse | Where-Object { $_.FullName -ne $rootManifest } | ForEach-Object {
  $relative = $_.FullName.Substring($stage.Length + 1).Replace('\','/')
  [pscustomobject]@{ Path = $relative; Size = $_.Length; SHA256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash }
}
$paths = @($manifest | ForEach-Object Path)
if (($paths | Sort-Object -Unique).Count -ne $paths.Count) { throw 'Manifest has duplicate paths' }
foreach ($path in $paths) {
  if ([string]::IsNullOrWhiteSpace($path) -or $path.StartsWith('/') -or $path.Contains('\') -or $path.Split('/').Contains('..')) { throw "Invalid manifest path: $path" }
}
$manifest | Sort-Object Path | ConvertTo-Json -Depth 3 | Set-Content -Encoding utf8 (Join-Path $stage 'manifest.json')
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zip).Hash
Set-Content -Encoding ascii -LiteralPath ($zip + '.sha256') -Value "$hash  $(Split-Path $zip -Leaf)"
"entries=$($manifest.Count)"
"zip=$zip"
"sha256=$hash"
