[CmdletBinding()]
param([ValidateSet('Build','Verify')][string]$Mode='Build')
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$package=Join-Path $PSScriptRoot 'RF-10-refactor-checkpoint-06-handoff.zip'
$hashFile="$package.sha256"
$stage=Join-Path $env:TEMP 'roadguard-rf10-checkpoint-06-stage'
if($Mode -eq 'Build'){
  Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
  New-Item -ItemType Directory -Force $stage | Out-Null
  $files=@(
    'planning/refactor/reports/RF-10-refactor-checkpoint-06-README.md',
    'planning/refactor/reports/RF-10-refactor-checkpoint-06-dirty-baseline.txt',
    'planning/refactor/reports/RF-10-03-C02.md',
    'planning/refactor/10-survey-request-task-assignment-characterization.md',
    'planning/refactor/reports/RF-10-02-C01.md',
    'planning/refactor/10-refactor-checklist.md',
    'planning/refactor/10-refactor-slices.md',
    'planning/refactor/tasks/RF-10-02-project-road-scope.md',
    'planning/refactor/tasks/RF-10-03-survey-dataset.md',
    'planning/refactor/reports/RF-10-refactor.md',
    'planning/refactor/10-identity-characterization-baseline.md',
    'tests/RoadGuardSystem.ApiTests/Projects/Rf1002ProjectGisCharacterizationTests.cs',
    'tests/RoadGuardSystem.ApiTests/Surveys/Rf1003SameRowSurveyTests.cs',
    'tests/RoadGuardSystem.ApiTests/Surveys/Rf1003RequestTaskAssignmentCharacterizationTests.cs',
    'RoadGuardSystem.API/Controllers/SurveyPlanningController.cs',
    'RoadGuardSystem.API/Controllers/SurveyV2Controller.cs',
    'RoadGuardSystem.Repositories/Implementations/Surveys/SurveyPlanningPersistenceService.cs',
    'RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs',
    'planning/refactor/reports/RF-10-03-C02-evidence/rf1002-rf1003-c02.trx',
    'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-final.trx',
    'planning/refactor/reports/RF-10-03-C02-evidence/rf1002-membership-rerun.trx'
  )
  foreach($f in $files){$src=Join-Path $root $f; if(!(Test-Path $src)){throw "Missing package file: $f"}; $dst=Join-Path $stage $f; New-Item -ItemType Directory -Force (Split-Path $dst) | Out-Null; Copy-Item $src $dst -Force}
  $manifest=@{baseHead=(git -C $root rev-parse HEAD);branch=(git -C $root branch --show-current);patchApplicability='NOT_VERIFIED';entries=@()}
  foreach($f in $files){$src=Join-Path $stage $f; $manifest.entries += [ordered]@{path=$f;size=(Get-Item $src).Length;sha256=(Get-FileHash $src -Algorithm SHA256).Hash}}
  $manifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $stage 'manifest.json') -Encoding utf8
  Remove-Item $package -Force -ErrorAction SilentlyContinue; Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $package -CompressionLevel Optimal
  (Get-FileHash $package -Algorithm SHA256).Hash | Set-Content $hashFile -Encoding ascii
}
if(!(Test-Path $package)){throw "Package missing"}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::OpenRead($package); try{$names=@($zip.Entries | ForEach-Object FullName); if(!$names.Contains('manifest.json')){throw 'manifest.json missing'}; "PASS entries=$($zip.Entries.Count) zipSha256=$((Get-FileHash $package -Algorithm SHA256).Hash)"} finally {$zip.Dispose()}
