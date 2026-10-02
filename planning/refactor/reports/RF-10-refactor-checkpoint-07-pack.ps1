[CmdletBinding()]
param([ValidateSet('Build','Verify')][string]$Mode='Build')
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$package=Join-Path $PSScriptRoot 'RF-10-refactor-checkpoint-07-handoff.zip'; $hashFile="$package.sha256"
$stage=Join-Path $env:TEMP 'roadguard-rf10-checkpoint-07-stage'; $pre=Join-Path $env:TEMP 'roadguard-rf10-checkpoint07-precorrection-20261001'
$files=@(
'planning/refactor/reports/RF-10-refactor-checkpoint-07-README.md','planning/refactor/reports/RF-10-refactor-checkpoint-07-case-matrix.md',
'planning/refactor/reports/RF-10-03-C02.md','planning/refactor/10-survey-request-task-assignment-characterization.md','planning/refactor/reports/RF-10-02-C01.md',
'planning/refactor/10-refactor-checklist.md','planning/refactor/10-refactor-slices.md','planning/refactor/tasks/RF-10-03-survey-dataset.md','planning/refactor/reports/RF-10-refactor.md',
'tests/RoadGuardSystem.ApiTests/Surveys/Rf1003RequestTaskAssignmentCharacterizationTests.cs','tests/RoadGuardSystem.ApiTests/Projects/Rf1002ProjectGisCharacterizationTests.cs',
'RoadGuardSystem.API/Controllers/SurveyPlanningController.cs','RoadGuardSystem.API/Controllers/SurveyV2Controller.cs','RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs',
'RoadGuardSystem.Repositories/Implementations/Surveys/SurveyPlanningPersistenceService.cs','RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs','RoadGuardSystem.Repositories/Implementations/Surveys/SurveyAssignmentPersistenceService.cs',
'RoadGuardSystem.BusinessObjects/Surveys/SurveyAssignment.cs','tests/RoadGuardSystem.ApiTests/Surveys/P2SurveyV2ApiTests.cs','tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationSqlServerFixture.cs',
'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-correction-console.txt','planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-correction.trx',
'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-correction2-console.txt','planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-correction2.trx',
'planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-correction3-console.txt','planning/refactor/reports/RF-10-03-C02-evidence/rf1003-c02-correction3.trx',
'planning/refactor/reports/RF-10-03-C02-evidence/rf1002-rf1003-correction-final-console.txt','planning/refactor/reports/RF-10-03-C02-evidence/rf1002-rf1003-correction-final.trx',
'planning/refactor/reports/RF-10-03-C02-evidence/rf1002-membership-correction-final-console.txt','planning/refactor/reports/RF-10-03-C02-evidence/rf1002-membership-correction-final.trx',
'planning/refactor/reports/RF-10-03-C02-evidence/api-build-correction-final-console.txt','planning/refactor/reports/RF-10-03-C02-evidence/integration-build-correction-final-console.txt',
'planning/refactor/reports/RF-10-03-C02-evidence/rf1002-rf1003-correction-final2-console.txt','planning/refactor/reports/RF-10-03-C02-evidence/rf1002-rf1003-correction-final2.trx',
'planning/refactor/reports/RF-10-03-C02-evidence/rf1002-membership-correction-final2-console.txt','planning/refactor/reports/RF-10-03-C02-evidence/rf1002-membership-correction-final2.trx'
)
if($Mode -eq 'Build'){
 Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue; New-Item -ItemType Directory -Force $stage | Out-Null
 $stage=(Get-Item $stage).FullName
 foreach($f in $files){$src=Join-Path $root $f;if(!(Test-Path $src)){throw "Missing package file: $f"};$dst=Join-Path $stage $f;New-Item -ItemType Directory -Force (Split-Path $dst)|Out-Null;Copy-Item $src $dst -Force}
 $baselineDir=Join-Path $stage 'baseline-before-correction'; foreach($f in $files[2..10]){$src=Join-Path $pre $f;if(Test-Path $src){$dst=Join-Path $baselineDir $f;New-Item -ItemType Directory -Force (Split-Path $dst)|Out-Null;Copy-Item $src $dst -Force}}
 $diff=Join-Path $stage 'task-specific-diff.txt'; "Base HEAD: $((git -C $root rev-parse HEAD))`r`nPatch applicability: NOT_VERIFIED`r`n" | Set-Content $diff
 foreach($f in $files[2..10]){$old=Join-Path $pre $f;$new=Join-Path $root $f;if((Test-Path $old) -and (Test-Path $new)){"`r`n===== $f =====" | Add-Content $diff; (git diff --no-index -- $old $new 2>&1 | Out-String) | Add-Content $diff}}
 $manifest=[ordered]@{baseHead=(git -C $root rev-parse HEAD);branch=(git -C $root branch --show-current);patchApplicability='NOT_VERIFIED';entries=@()}
 Get-ChildItem $stage -File -Recurse | ForEach-Object { $rel=$_.FullName.Substring($stage.Length).TrimStart('\').Replace('\','/'); $manifest.entries += [ordered]@{path=$rel;size=$_.Length;sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash} }
 $manifest | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $stage 'manifest.json') -Encoding utf8
 Remove-Item $package -Force -ErrorAction SilentlyContinue; Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $package -CompressionLevel Optimal; (Get-FileHash $package -Algorithm SHA256).Hash | Set-Content $hashFile -Encoding ascii
}
if(!(Test-Path $package)){throw 'Package missing'}; Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::OpenRead($package);try{$entries=@($zip.Entries);$manifest=$entries|Where-Object FullName -eq 'manifest.json';if(!$manifest){throw 'manifest missing'};"PASS entries=$($entries.Count) zipSha256=$((Get-FileHash $package -Algorithm SHA256).Hash)"}finally{$zip.Dispose()}
