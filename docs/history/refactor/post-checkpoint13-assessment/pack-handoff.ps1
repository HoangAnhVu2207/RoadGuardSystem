param([ValidateSet('Snapshot','Package')][string]$Mode)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$bundle = Join-Path $PSScriptRoot 'payload'
$docs = @(
 'planning/refactor/10-refactor-slices.md',
 'planning/refactor/10-refactor-checklist.md',
 'planning/refactor/10-notification-outbox-characterization-baseline.md',
 'planning/refactor/10-inspection-measurement-characterization-baseline.md',
 'planning/refactor/10-survey-coexistence-baseline.md',
 'planning/refactor/10-survey-request-task-assignment-characterization.md',
 'planning/refactor/reports/RF-10-03-C02.md',
 'planning/refactor/reports/RF-10-06-C01.md',
 'planning/refactor/reports/RF-10-07-C01.md',
 'planning/refactor/reports/RF-10-07-R01.md',
 'planning/refactor/reports/RF-10-08-C01.md',
 'planning/refactor/reports/RF-10-09-C01.md'
)
function Copy-Payload([string]$from,[string]$relative) {
 $to = Join-Path $bundle $relative
 New-Item -ItemType Directory -Force (Split-Path $to) | Out-Null
 Copy-Item -LiteralPath $from -Destination $to
}
function Save-Json($value,[string]$path) {
 $value | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding utf8
}
function Hash-File([string]$path) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() }
Set-Location $repo
if ($Mode -eq 'Snapshot') {
 if (Test-Path (Join-Path $bundle 'baseline.json')) { throw 'Existing baseline must not be overwritten.' }
 New-Item -ItemType Directory -Force $bundle | Out-Null
 $status = @(git status --short --branch --untracked-files=all)
 $roots = @('RoadGuardSystem.API','RoadGuardSystem.Services','RoadGuardSystem.Repositories','RoadGuardSystem.BusinessObjects','RoadGuardSystem.DTOs','tests','contracts','.github','.agents','docs')
 $files = @(& rg --files --hidden $roots | Where-Object { $_ -notmatch '(^|[\\/])(bin|obj|\.git)([\\/]|$)' }) + @('AGENTS.md')
 $hashes = @($files | Sort-Object -Unique | ForEach-Object { [pscustomobject]@{path=$_;sha256=(Hash-File (Join-Path $repo $_))} })
 foreach($d in $docs) { Copy-Payload (Join-Path $repo $d) ('before/' + $d) }
 Save-Json ([ordered]@{date='2026-10-02 Asia/Bangkok';branch=(git branch --show-current);head=(git rev-parse HEAD);status=$status;readonlyHashes=$hashes;docs=$docs;docHashes=@($docs | ForEach-Object { @{path=$_;sha256=(Hash-File (Join-Path $repo $_))} });writeAllowlist=@($docs,'planning/refactor/reports/RF-10-08-assessment.md','planning/refactor/reports/RF-10-09-assessment.md','planning/refactor/evidence/post-checkpoint13-assessment/**');excluded='bin/obj; no runtime/database calls'}) (Join-Path $bundle 'baseline.json')
 Write-Output "Snapshot captured: $($hashes.Count) read-only files, $($docs.Count) documentation byte snapshots."
 exit
}
$baseline = Get-Content (Join-Path $bundle 'baseline.json') -Raw | ConvertFrom-Json
$drift = @($baseline.readonlyHashes | Where-Object { !(Test-Path -LiteralPath (Join-Path $repo $_.path)) -or (Hash-File (Join-Path $repo $_.path)) -ne $_.sha256 })
if ($drift.Count) { throw "Read-only drift: $($drift.path -join ', ')" }
if ((git rev-parse HEAD) -ne $baseline.head -or (git branch --show-current) -ne $baseline.branch) { throw 'Git baseline changed.' }
New-Item -ItemType Directory -Force (Join-Path $bundle 'diffs') | Out-Null
foreach($d in $docs) {
 Copy-Payload (Join-Path $repo $d) ('current/' + $d)
 $diff = & git diff --no-index --no-ext-diff -- (Join-Path $bundle ('before/' + $d)) (Join-Path $repo $d) 2>$null
 if ($LASTEXITCODE -gt 1) { throw "Diff failed: $d" }
 $diff | Set-Content -LiteralPath (Join-Path $bundle ('diffs/' + ($d -replace '[\\/]','__') + '.diff')) -Encoding utf8
}
$reports = @('RF-10-08-assessment.md','RF-10-09-assessment.md')
foreach($r in $reports) { Copy-Payload (Join-Path $repo ('planning/refactor/reports/' + $r)) ('current/planning/refactor/reports/' + $r) }
foreach($f in Get-ChildItem $PSScriptRoot -File | Where-Object { $_.Extension -in '.md','.ps1' }) {
 Copy-Payload $f.FullName ('assessment/' + $f.Name)
 if($f.Extension -eq '.md') { Copy-Payload $f.FullName ('current/planning/refactor/evidence/post-checkpoint13-assessment/' + $f.Name) }
}
$sourceListing=@(& rg --files RoadGuardSystem.API/Controllers RoadGuardSystem.API/Authorization RoadGuardSystem.API/Middlewares RoadGuardSystem.API/Constants RoadGuardSystem.Services/Implementations RoadGuardSystem.Services/Interfaces RoadGuardSystem.Repositories/Implementations RoadGuardSystem.Repositories/Interfaces RoadGuardSystem.Repositories/Idempotency RoadGuardSystem.Repositories/Transactions RoadGuardSystem.Repositories/Configurations RoadGuardSystem.Repositories/Extensions RoadGuardSystem.BusinessObjects/Idempotency RoadGuardSystem.BusinessObjects/Messaging RoadGuardSystem.DTOs)
if($LASTEXITCODE -ne 0) { throw 'Source inventory search failed.' }
$sources = @($sourceListing | Where-Object { $_ -match '(Files|Surveys|Projects|Warranties|Processing|Messaging|Defects|Storage|Authorization|Idempotency|Transactions|Notification|Outbox|ConsumerEffect|PersistenceExtensions|ProjectScope|ProjectAccess|ProjectRole|Error|Exception|Correlation|UploadsController|Survey.*Controller|ProjectsController|ProjectRoadSectionsController|ProjectWarrantiesController|ProcessingV2Controller)' -and $_ -notmatch '[\\/](bin|obj)[\\/]' })
$sources += @('RoadGuardSystem.Repositories/RoadGuardDbContext.cs','RoadGuardSystem.API/Program.cs','RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs','RoadGuardSystem.API/BackgroundServices/ValidationRunWorker.cs','RoadGuardSystem.API/Workers/UploadVerificationWorker.cs','tests/RoadGuardSystem.ApiTests/Idempotency/IdempotencyPerCommandCharacterizationTests.cs','tests/RoadGuardSystem.ApiTests/Notifications/Rf1009NotificationInboxCharacterizationTests.cs','tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationSqlServerFixture.cs','tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationWebApplicationFactory.cs')
$supportListing=@(& rg --files RoadGuardSystem.BusinessObjects RoadGuardSystem.Repositories/Spatial RoadGuardSystem.Repositories/Options)
if($LASTEXITCODE -ne 0) { throw 'Support source inventory search failed.' }
$sources += @($supportListing | Where-Object { $_ -match '(Common|Files|Surveys|Projects|Warranties|Processing|Auditing|Defects|Inspections|Spatial|Storage|Options)' -and $_ -notmatch '[\\/](bin|obj)[\\/]' })
foreach($s in $sources | Sort-Object -Unique) { Copy-Payload (Join-Path $repo $s) ('source/' + ($s -replace '\\','/')) }
$referenceDocs = @('AGENTS.md','.agents/manifest.json','.agents/modules/README.md','.agents/rules/evidence.md','.agents/rules/delivery.md','.agents/rules/safety.md','.agents/rules/review-and-coordination.md','planning/refactor/tasks/RF-10-08-offline-sync.md','planning/refactor/tasks/RF-10-09-messaging-reporting-retention.md','planning/refactor/templates/task-report.md','planning/refactor/02-decision-register.md','planning/refactor/03-two-developer-plan.md','planning/refactor/11-handoff-baseline.md','planning/refactor/reports/RF-10-refactor.md','docs/product/confirmed-decisions.md','docs/backend/persistence-and-operations.md','contracts/events/README.md')
foreach($d in $referenceDocs) { Copy-Payload (Join-Path $repo $d) ('reference/' + $d) }
$history = 'planning/refactor/evidence/checkpoint13-integration'
foreach($h in @('correction-01-findings-matrix.md','correction-02-findings-matrix.md','correction-03-handoff/FINDINGS-MATRIX.md','correction-04-handoff/documentation/FINDINGS-MATRIX.md','box3-phase-a-vietnamese-report.md','box3-final-report-vietnamese.md','phase-a-summary.md','finalization-handoff/documentation/FINDINGS-LEDGER.md','finalization-handoff/documentation/SOURCE-PROVENANCE.md','finalization-handoff/documentation/CONTENT-DIFF.md','finalization-handoff/FINALIZATION-SUMMARY.md','finalization-handoff/generate_manifest.py','finalization-handoff/payload-inventory.txt','finalization-handoff/MANIFEST.json')) { Copy-Payload (Join-Path $repo "$history/$h") ("historical/$history/$h") }
foreach($h in @('finalization-handoff/evidence/RF-10-08-C01-correction-04-final.trx','finalization-handoff/evidence/RF-10-09-C01-correction-03-final.trx','finalization-handoff/evidence/correction-04-build.log','finalization-handoff/evidence/correction-04-box1-run.log','finalization-handoff/evidence/correction-04-box2-reuse-note.txt','correction-04-tests-before.sha256','correction-04-tests-after.sha256','correction-03-tests-before.sha256','correction-03-tests-after.sha256')) { Copy-Payload (Join-Path $repo "$history/$h") ("reused/$history/$h") }
$searches = @(
 @{name='task-ids';args=@('-n','10-08-R01|10-09-R01','planning/refactor/tasks','planning/refactor/10-refactor-slices.md')},
 @{name='idempotency-callers';args=@('-n','IdempotencyOperationService|_idempotency.ExecuteAsync','RoadGuardSystem.API','RoadGuardSystem.Services','RoadGuardSystem.Repositories','-g','*.cs','-g','!**/Migrations/**','-g','!**/bin/**','-g','!**/obj/**')},
 @{name='outbox-reachability';args=@('-n','IOutboxWorkRepository|OutboxWorkRepository|NotificationOutboxConsumer|TryLeaseNextAsync|AddHostedService|BackgroundService|IHostedService|CompleteNextValidationRunAsync','RoadGuardSystem.API','RoadGuardSystem.Services','RoadGuardSystem.Repositories','-g','*.cs','-g','!**/Migrations/**','-g','!**/bin/**','-g','!**/obj/**')},
 @{name='producer-callers';args=@('-n','ISurveyAssignmentService|ISurveyAssignmentRepository|IDetectionReviewRepository|ISurveyDataValidationAdmissionRepository|OutboxMessages.Add','RoadGuardSystem.API','RoadGuardSystem.Services','RoadGuardSystem.Repositories','-g','*.cs','-g','!**/Migrations/**','-g','!**/bin/**','-g','!**/obj/**')}
)
New-Item -ItemType Directory -Force (Join-Path $bundle 'checks') | Out-Null
foreach($s in $searches) {
 $searchArgs=$s.args
 $output=@(& rg @searchArgs)
 $code=$LASTEXITCODE
 Save-Json @{command=('rg ' + ($searchArgs -join ' '));exitCode=$code;output=$output;scope='local production source / task docs; normal rg ignore rules; no deployed environment search'} (Join-Path $bundle ('checks/' + $s.name + '.json'))
 if($code -gt 1) { throw "Search failed: $($s.name)" }
}
$archives = @('RF-10-checkpoint-13-correction-04-handoff.tar.gz','RF-10-checkpoint-13-finalization-handoff.tar.gz')
Save-Json @($archives | ForEach-Object { $a=Get-Item (Join-Path $repo "$history/$_"); @{path="$history/$_";size=$a.Length;sha256=(Hash-File $a.FullName)} }) (Join-Path $bundle 'checks/external-runtime-archives.json')
$archiveChecks=@()
foreach($a in $archives) {
 $archivePath=Join-Path $repo "$history/$a"
 $scratch=Join-Path $PSScriptRoot ('runtime-inspection/' + ($a -replace '\.tar\.gz$',''))
 New-Item -ItemType Directory -Force $scratch | Out-Null
 $archiveNames=@(& tar -tf $archivePath)
 if($LASTEXITCODE -ne 0 -or @($archiveNames | Where-Object { $_ -match '(^/|^[A-Za-z]:|(^|/)\.\.(/|$))' }).Count) { throw "Invalid historical archive: $a" }
 & tar -xf $archivePath -C $scratch
 if($LASTEXITCODE -ne 0) { throw "Historical extraction failed: $a" }
 $archivedSources=@()
 foreach($f in Get-ChildItem $scratch -Recurse -File | Where-Object { $_.Extension -eq '.cs' }) {
  $matches=@($sources | Sort-Object -Unique | Where-Object { [IO.Path]::GetFileName($_) -eq $f.Name })
  if($matches.Count -ne 1) { throw "Ambiguous current source mapping for $($f.Name)" }
  $path=$matches[0]
  $archivedHash=Hash-File $f.FullName
  $currentHash=Hash-File (Join-Path $repo $path)
  $archivedSources+=@{archivedPath=$f.FullName.Substring($scratch.Length+1).Replace('\','/');currentPath=$path;archiveSha256=$archivedHash;currentSha256=$currentHash;matches=($archivedHash -eq $currentHash);impact= $(if($archivedHash -eq $currentHash){'byte identity only; runtime linkage still bounded'}else{'old TRX not attributed to changed current bytes; inspect source drift separately'})}
 }
 $oldManifestFile=@(Get-ChildItem $scratch -Recurse -File -Filter MANIFEST.json)
 $oldManifest=Get-Content $oldManifestFile[0].FullName -Raw | ConvertFrom-Json
 $oldEntries=@($oldManifest.payload.PSObject.Properties | ForEach-Object { $_.Value })
 $manifestDir=$oldManifestFile[0].DirectoryName
 $oldNames=@(Get-ChildItem $manifestDir -Recurse -File | ForEach-Object { $_.FullName.Substring($manifestDir.Length+1).Replace('\','/') })
 $oldListed=@($oldEntries | ForEach-Object { $_.path.Replace('\','/') })
 $oldUnlisted=@($oldNames | Where-Object { $_ -ne 'MANIFEST.json' -and $_ -notin $oldListed })
 $archiveChecks+=@{archive="$history/$a";fileCount=$oldNames.Count;manifestEntries=$oldEntries.Count;unlistedExceptManifest=$oldUnlisted;sources=$archivedSources}
}
Save-Json $archiveChecks (Join-Path $bundle 'checks/runtime-archive-source.json')
$historyRoots=@('planning/refactor/evidence/checkpoint13-integration','planning/refactor/evidence/rf1007-c01','planning/refactor/evidence/rf1007-c02','planning/refactor/reports/RF-10-03-C02-evidence','planning/refactor/reports')
$historicalFiles=@($historyRoots | ForEach-Object { Get-ChildItem -LiteralPath (Join-Path $repo $_) -File -Recurse | Where-Object { $_.Extension -in '.trx','.log' -or $_.Name -match '(handoff.*\.(zip|gz|rar)|build.*metadata|test.*metadata|console|sha256)$' } } | Sort-Object FullName -Unique)
Save-Json @($historicalFiles | ForEach-Object { @{path=$_.FullName.Substring($repo.Length+1).Replace('\','/');size=$_.Length;sha256=(Hash-File $_.FullName);label='HISTORICAL; retained at repository path, not re-executed'} }) (Join-Path $bundle 'checks/historical-evidence-index.json')
$trx = @()
foreach($t in Get-ChildItem (Join-Path $bundle 'reused') -Recurse -Filter *.trx) {
 [xml]$xml=Get-Content $t.FullName -Raw
 $counts=$xml.TestRun.ResultSummary.Counters
 $trx+=@{path=$t.FullName.Substring($bundle.Length+1).Replace('\','/');sha256=(Hash-File $t.FullName);total=$counts.total;executed=$counts.executed;passed=$counts.passed;failed=$counts.failed;notExecuted=$counts.notExecuted;start=$xml.TestRun.Times.start;finish=$xml.TestRun.Times.finish;classification='HISTORICAL reused runtime; NOT a new execution'}
}
Save-Json $trx (Join-Path $bundle 'checks/reused-trx.json')
if(@($trx | Where-Object { $_.failed -ne '0' -or $_.notExecuted -ne '0' }).Count -or ($trx | Measure-Object -Property passed -Sum).Sum -ne 15) { throw 'Unexpected historical TRX counters.' }
$parseTokens=$null; $parseErrors=$null
[System.Management.Automation.Language.Parser]::ParseFile($PSCommandPath,[ref]$parseTokens,[ref]$parseErrors) | Out-Null
if($parseErrors.Count) { throw 'Package script syntax errors.' }
$missingLinks=@()
$reviewDocs=@($docs) + @($reports | ForEach-Object { 'planning/refactor/reports/' + $_ }) + @(Get-ChildItem $PSScriptRoot -File -Filter *.md | ForEach-Object { $_.FullName.Substring($repo.Length+1) })
foreach($d in $reviewDocs) {
 $text=Get-Content -LiteralPath (Join-Path $repo $d) -Raw
 foreach($match in [regex]::Matches($text,'\[[^\]]+\]\(([^\)]+)\)')) {
  $target=$match.Groups[1].Value
  if($target -match '^(https?:|#|mailto:|codex:)') { continue }
  $target=($target -split '#')[0]
  if(!(Test-Path -LiteralPath (Join-Path (Split-Path (Join-Path $repo $d)) $target))) { $missingLinks+=@{path=$d;target=$target} }
 }
}
if($missingLinks.Count) { throw "Missing Markdown links: $($missingLinks | ConvertTo-Json -Compress)" }
$sections=@($reports | ForEach-Object { $r=$_; $count=([regex]::Matches((Get-Content (Join-Path $repo "planning/refactor/reports/$r") -Raw),'(?m)^## [1-7]\.')).Count; @{path=$r;sections=$count} })
if(@($sections | Where-Object sections -ne 7).Count) { throw 'Seven-section report requirement failed.' }
Save-Json @{packageScriptSyntaxErrors=$parseErrors.Count;markdownDocsChecked=$reviewDocs.Count;missingLinks=$missingLinks.Count;reportSections=$sections;sourceFiles=@($sources | Sort-Object -Unique).Count;runtimeExecutions=0;databaseOperations=0;readonlyBaselineFiles=$baseline.readonlyHashes.Count;review='two-pass self review; peer PENDING';command='PowerShell source/hash/TRX/link/section/archive checks, no runtime'} (Join-Path $bundle 'checks/static-checks.json')
Save-Json @{branch=(git branch --show-current);head=(git rev-parse HEAD);readonlyFilesChecked=$baseline.readonlyHashes.Count;readonlyDrift=$drift.Count;status=@(git status --short --branch --untracked-files=all);docs=@($docs | ForEach-Object { @{path=$_;before=($baseline.docHashes | Where-Object path -eq $_).sha256;after=(Hash-File (Join-Path $repo $_))} })} (Join-Path $bundle 'checks/current-state.json')
$inventory=@(Get-ChildItem $bundle -File -Recurse | Where-Object FullName -ne (Join-Path $bundle 'MANIFEST.json') | ForEach-Object { $_.FullName.Substring($bundle.Length+1).Replace('\','/') }) + @('payload-inventory.txt','MANIFEST.json')
$inventory | Sort-Object -Unique | Set-Content (Join-Path $bundle 'payload-inventory.txt') -Encoding utf8
$entries=@(Get-ChildItem $bundle -File -Recurse | Where-Object FullName -ne (Join-Path $bundle 'MANIFEST.json') | Sort-Object FullName | ForEach-Object { @{path=$_.FullName.Substring($bundle.Length+1).Replace('\','/');size=$_.Length;sha256=(Hash-File $_.FullName)} })
Save-Json @{algorithm='SHA-256';excluded=@('MANIFEST.json');entries=$entries} (Join-Path $bundle 'MANIFEST.json')
$zip=Join-Path $PSScriptRoot 'RF-10-post-checkpoint-13-assessment-handoff-v2.zip'
if(Test-Path $zip) { throw 'Archive exists; use a new version, do not overwrite.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($bundle,$zip)
$extract=Join-Path $PSScriptRoot 'extract-verification-v2'
if(Test-Path $extract) { throw 'Extract directory exists; do not overwrite.' }
[IO.Compression.ZipFile]::ExtractToDirectory($zip,$extract)
$archive=[IO.Compression.ZipFile]::OpenRead($zip)
$names=@($archive.Entries | Where-Object { $_.Name } | ForEach-Object FullName)
$duplicates=@($names | Group-Object | Where-Object Count -gt 1)
$archive.Dispose()
$m=Get-Content (Join-Path $extract 'MANIFEST.json') -Raw | ConvertFrom-Json
$missing=@();$mismatch=@()
foreach($e in $m.entries) {
 $file=Join-Path $extract $e.path
 if(!(Test-Path -LiteralPath $file)) { $missing+=$e.path;continue }
 if((Get-Item -LiteralPath $file).Length -ne $e.size -or (Hash-File $file) -ne $e.sha256) { $mismatch+=$e.path }
}
$expected=@($m.entries.path)+@('MANIFEST.json')
$unlisted=@($names | Where-Object { $_ -notin $expected })
$manifestDuplicates=@($m.entries.path | Group-Object | Where-Object Count -gt 1)
$result=@{missing=$missing.Count;mismatch=$mismatch.Count;unlisted=$unlisted.Count;duplicates=$duplicates.Count;manifestDuplicates=$manifestDuplicates.Count;payloadEntries=$m.entries.Count;archiveFiles=$names.Count;sha256=(Hash-File $zip);zip=$zip}
Save-Json $result (Join-Path $PSScriptRoot 'archive-verification.json')
if($missing.Count+$mismatch.Count+$unlisted.Count+$duplicates.Count+$manifestDuplicates.Count) { throw 'Extract verification failed.' }
"$($result.sha256) *$([IO.Path]::GetFileName($zip))" | Set-Content ($zip+'.sha256') -Encoding ascii
$result | ConvertTo-Json
