param([switch]$Check)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$dataDir = Join-Path $root 'docs/backend/data'
$inventoryPath = Join-Path $dataDir 'current-schema.inventory.json'
$inventory = Get-Content -LiteralPath $inventoryPath -Raw | ConvertFrom-Json
if ($inventory.status -ne 'CURRENT_VERIFIED_ISOLATED_SQL') { throw 'Inventory lacks isolated SQL evidence.' }
function DefinitionHash($value) {
  $normalized = $value.Replace("`r`n", "`n").Replace("`r", "`n")
  return [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData(
    [System.Text.Encoding]::UTF8.GetBytes($normalized))).ToLowerInvariant()
}
foreach ($tool in $inventory.toolHashes) {
  $toolPath = Join-Path $root $tool.path
  if (-not (Test-Path -LiteralPath $toolPath)) { throw "Inventory tool input missing: $($tool.path)" }
  $actualHash = (Get-FileHash -LiteralPath $toolPath -Algorithm SHA256).Hash.ToLowerInvariant()
  if ($actualHash -ne $tool.sha256) { throw "Inventory tool fingerprint differs: $($tool.path); rerun isolated extractor" }
}
$triggerEvidence = @{}
foreach ($item in $inventory.triggerEvidence) {
  $key = "$($item.Schema).$($item.Table).$($item.Name)"
  if ($triggerEvidence.ContainsKey($key)) { throw "Duplicate trigger evidence: $key" }
  $triggerEvidence[$key] = $item
}
if ($triggerEvidence.Count -ne $inventory.sql.triggers.Count) { throw 'Trigger evidence/catalog count differs.' }
foreach ($trigger in $inventory.sql.triggers) {
  $key = "$($trigger.schema).$($trigger.table).$($trigger.name)"
  if (-not $triggerEvidence.ContainsKey($key)) { throw "Trigger migration evidence missing: $key" }
  if ([string]::IsNullOrWhiteSpace($trigger.definition)) { throw "Trigger definition unavailable: $key" }
  $source = $triggerEvidence[$key]
  if (-not (Test-Path -LiteralPath (Join-Path $root $source.sourcePath))) { throw "Trigger source missing: $key" }
  if ((DefinitionHash $trigger.definition) -ne $source.observedDefinitionSha256Utf8Lf) {
    throw "Trigger catalog definition/hash differs: $key"
  }
  if ($source.textComparison -notin @('EXACT_AFTER_LINE_ENDING_NORMALIZATION','OUTER_WHITESPACE_ONLY')) {
    throw "Trigger source comparison unresolved: $key"
  }
}

$moduleTables = [ordered]@{
  'identity' = @('Users','Roles','Sessions','RefreshTokens','PasswordResetLogs','AccountStatusChangeLogs','PasswordRecoveryRequests','ReporterRegistrationIntents','StaffInvitations','StaffInvitationProjects')
  'project-road' = @('Projects','ProjectMembers','RoadSections','RoadSectionVersions','RoadSegmentSets','RoadSegments','Warranties','HandoverDocuments')
  'survey' = @('SurveyPlans','SurveyPlanScopes','SurveyPlanPostponements','SurveyRequests','SurveyRequestScopes','Surveys','SurveyAssignments','Flights','SurveyFiles','SurveyDataVersions','QualityChecks','SupplementarySurveyRequests','DroneDevices')
  'files' = @('Files','FileScopes','UploadSessions','UploadParts')
  'processing' = @('ProcessingBlocks','ProcessingJobs','ProcessingAttempts','AIModelVersions','ValidationRuns')
  'defect-inspection' = @('DefectTypes','CauseCategories','SeverityRuleVersions','Defects','AIDetections','DefectVerificationLogs','FieldInspectionTasks','FieldInspectionAssignments','FieldInspectionSessions','GroundTruthMeasurements','DerivedMeasurements','MeasurementValidationSamples')
  'messaging' = @('AuditLogs','IdempotencyRecords','OutboxMessages','ConsumerEffectReceipts','Notifications')
}
$purpose = @{
  Users='User identity, credential and account state'; Roles='Role codes and labels'; Sessions='Issued user sessions and device metadata'; RefreshTokens='Refresh-token family and rotation records'; PasswordResetLogs='Password reset audit events'; AccountStatusChangeLogs='Account status transition events'; PasswordRecoveryRequests='Password recovery requests'; ReporterRegistrationIntents='Reporter onboarding intent'; StaffInvitations='Staff invitation lifecycle'; StaffInvitationProjects='Projects attached to an invitation'
  Projects='Project identity, status and spatial reference'; ProjectMembers='User membership within a project'; RoadSections='Named road sections in a project'; RoadSectionVersions='Versioned road geometry'; RoadSegmentSets='Segment-set publication state'; RoadSegments='Ordered segments within a set'; Warranties='Warranty terms linked to project scope'; HandoverDocuments='Project handover document references'
  SurveyPlans='Planned survey schedule and state'; SurveyPlanScopes='Road-version scope of a plan'; SurveyPlanPostponements='Append-only plan postponement history'; SurveyRequests='Survey request and approval state'; SurveyRequestScopes='Road-version scope of a request'; Surveys='Survey execution record'; SurveyAssignments='Survey operator assignment'; Flights='Flight execution records'; SurveyFiles='Files attached to survey/flight'; SurveyDataVersions='Versioned survey dataset'; QualityChecks='Dataset quality-check outcomes'; SupplementarySurveyRequests='Supplementary survey requests'; DroneDevices='Drone inventory and status'
  Files='Stored object identity, size and checksum'; FileScopes='Project/purpose/target association of file'; UploadSessions='Multipart upload session state'; UploadParts='Upload part numbers and object-store receipt'
  ProcessingBlocks='Immutable processing input scope'; ProcessingJobs='Processing job state'; ProcessingAttempts='Processing retry/attempt history'; AIModelVersions='AI model version metadata'; ValidationRuns='Validation execution linked to processing'
  DefectTypes='Defect taxonomy'; CauseCategories='Cause taxonomy'; SeverityRuleVersions='Severity rule version metadata'; Defects='Verified or tracked defect records'; AIDetections='AI detection candidates'; DefectVerificationLogs='Defect/detection review history'; FieldInspectionTasks='Field inspection work items'; FieldInspectionAssignments='Inspector assignment records'; FieldInspectionSessions='Inspector field sessions'; GroundTruthMeasurements='Field measurement evidence'; DerivedMeasurements='Derived measurement values'; MeasurementValidationSamples='Comparison samples for measurement validation'
  AuditLogs='Audit event history'; IdempotencyRecords='Durable request replay keys and outcomes'; OutboxMessages='Durable dispatch intents'; ConsumerEffectReceipts='Consumer effect deduplication receipts'; Notifications='User notification state'
}

$assigned = @{}
foreach ($module in $moduleTables.Keys) {
  foreach ($table in $moduleTables[$module]) {
    if ($assigned.ContainsKey($table)) { throw "Duplicate module mapping: $table" }
    $assigned[$table] = $module
  }
}
$sqlNames = @($inventory.sql.tables | ForEach-Object { $_.table })
$missing = @($sqlNames | Where-Object { -not $assigned.ContainsKey($_) })
$extra = @($assigned.Keys | Where-Object { $_ -notin $sqlNames })
if ($missing.Count -or $extra.Count) { throw "Module mapping mismatch: missing=$($missing -join ',') extra=$($extra -join ',')" }
if (@($sqlNames | Where-Object { -not $purpose.ContainsKey($_) }).Count) { throw 'Purpose map omits a table.' }

function Cell($value) {
  if ($null -eq $value -or "$value" -eq '') { return '-' }
  return ("$value" -replace '\|','\|' -replace "`r|`n",' ')
}
function TableItems($array, $table) { return @($array | Where-Object { $_.schema -eq 'dbo' -and $_.table -eq $table }) }

$dictionary = [System.Collections.Generic.List[string]]::new()
$dictionary.Add('# Current SQL data dictionary')
$dictionary.Add('')
$dictionary.Add("CURRENT_VERIFIED in isolated SQL after $($inventory.migrationCount) migrations ending $($inventory.lastMigration). Source: [inventory](current-schema.inventory.json), RoadGuardDbContext, EF configurations/entities and migration chain. This is not a production-schema claim.")
$dictionary.Add('')
$dictionary.Add('`dbo.__EFMigrationsHistory` is EF infrastructure and excluded. Column purpose defaults to UNKNOWN unless the linked entity/configuration and product source are inspected; a column name alone is not an accepted business definition. SQL rows below are catalog observations. Model columns carry CLR/property/converter metadata. Enum values show converter output or underlying numeric value; check constraints may reject some of them. `-` means absent or not applicable, not a guessed value.')
$dictionary.Add('')

foreach ($module in $moduleTables.Keys) {
  $dictionary.Add("## $module")
  $dictionary.Add('')
  foreach ($table in $moduleTables[$module]) {
    $key = "dbo.$table"
    $model = @($inventory.model | Where-Object TableKey -eq $key)[0]
    $columns = TableItems $inventory.sql.columns $table
    $keys = TableItems $inventory.sql.keys $table
    $fks = TableItems $inventory.sql.foreignKeys $table
    $indexes = TableItems $inventory.sql.indexes $table
    $checks = TableItems $inventory.sql.checks $table
    $triggers = TableItems $inventory.sql.triggers $table
    if ($columns.Count -ne $model.Columns.Count) { throw "Column coverage mismatch: $key" }
    $dictionary.Add("### $key")
    $dictionary.Add('')
    $dictionary.Add("- Module: $module; CLR mapping: $($model.Entities -join ', '). Purpose (source-interpreted from entity/configuration, not accepted business policy): $($purpose[$table]). Detailed field semantics remain UNKNOWN unless independently sourced.")
    $entityName = (@($model.Entities)[0] -split '\.')[-1]
    $configPath = "RoadGuardSystem.Repositories/Configurations/${entityName}Configuration.cs"
    $entityPath = @($inventory.inputHashes.path | Where-Object { $_ -like "RoadGuardSystem.BusinessObjects/*/$entityName.cs" })[0]
    if (-not $entityPath -or -not (Test-Path -LiteralPath (Join-Path $root $configPath))) { throw "Missing source path for $key" }
    $dictionary.Add("- Source: [entity](../../../$entityPath), [EF configuration](../../../$configPath), SQL catalog in inventory and migration chain. Query filter: $($(if ($model.QueryFilters.Count) { Cell ($model.QueryFilters -join '; ') } else { 'none in EF model' })).")
    $dictionary.Add('')
    $dictionary.Add('Columns (SQL type, length in bytes for `nvarchar`/`varchar`; precision/scale from catalog; CLR metadata from EF):')
    $dictionary.Add('')
$dictionary.Add('| Column | Property / CLR | SQL | Nullable | Length bytes | Precision/scale | Default / computed | Generation / concurrency | Converter / enum stored values |')
    $dictionary.Add('| --- | --- | --- | --- | --- | --- | --- | --- | --- |')
    foreach ($column in $columns) {
      $mapping = @($model.Columns | Where-Object Name -eq $column.name)[0]
      if ($null -eq $mapping) { throw "Missing model column: $key.$($column.name)" }
      $generation = @($mapping.ValueGenerated, $(if ($mapping.ConcurrencyToken) { 'concurrency' }), $(if ($column.identity) { 'identity' }), $(if ($mapping.Shadow) { 'shadow' })) | Where-Object { $_ }
      $definition = @($column.defaultSql, $column.computedSql) | Where-Object { $_ }
      $dictionary.Add('| ' + (@(
        (Cell $column.name), (Cell "$($mapping.Property) : $($mapping.ClrType)"), (Cell $column.type),
        (Cell $column.nullable), (Cell $column.maxLengthBytes), (Cell "$($column.precision)/$($column.scale)"),
        (Cell ($definition -join '; ')), (Cell ($generation -join '; ')), (Cell (@($mapping.ConverterProviderType,$mapping.EnumStoredValues) | Where-Object { $_ } | Join-String -Separator '; '))
      ) -join ' | ') + ' |')
    }
    $dictionary.Add('')
    $dictionary.Add('Keys (ordered columns):')
    $dictionary.Add('')
    $dictionary.Add('| Constraint | Kind | Columns in order |')
    $dictionary.Add('| --- | --- | --- |')
    foreach ($group in ($keys | Group-Object name)) {
      $ordered = @($group.Group | Sort-Object ordinal)
      $dictionary.Add("| $(Cell $group.Name) | $(Cell $ordered[0].kind) | $(Cell (($ordered.column) -join ', ')) |")
    }
    $dictionary.Add('')
    $dictionary.Add('Enforced foreign keys (ordered child -> principal columns; nullable child column means optional relationship):')
    $dictionary.Add('')
    $dictionary.Add('| FK | Child columns -> principal | Delete | Disabled |')
    $dictionary.Add('| --- | --- | --- | --- |')
    foreach ($group in ($fks | Group-Object name)) {
      $ordered = @($group.Group | Sort-Object ordinal)
      $pairs = @($ordered | ForEach-Object { "$($_.column) -> $($_.principalSchema).$($_.principalTable).$($_.principalColumn)" })
      $dictionary.Add("| $(Cell $group.Name) | $(Cell ($pairs -join '; ')) | $(Cell $ordered[0].onDelete) | $(Cell $ordered[0].disabled) |")
    }
    $dictionary.Add('')
    $dictionary.Add('Indexes (ordered key columns followed by included columns):')
    $dictionary.Add('')
    $dictionary.Add('| Index | Key / included | Unique | Filter | Kind |')
    $dictionary.Add('| --- | --- | --- | --- | --- |')
    foreach ($group in ($indexes | Group-Object name)) {
      $ordered = @($group.Group | Sort-Object included,ordinal)
      $parts = @($ordered | ForEach-Object { "$($_.column)$(if ($_.included) { ' [include]' })$(if ($_.descending) { ' DESC' })" })
      $dictionary.Add("| $(Cell $group.Name) | $(Cell ($parts -join ', ')) | $(Cell $ordered[0].unique) | $(Cell $ordered[0].filter) | $(Cell $ordered[0].kind) |")
    }
    $dictionary.Add('')
    $dictionary.Add('Check constraints and migration SQL triggers:')
    $dictionary.Add('')
    $dictionary.Add('| Object | Definition / state |')
    $dictionary.Add('| --- | --- |')
    foreach ($constraint in $checks) { $dictionary.Add("| $(Cell $constraint.name) | $(Cell $constraint.definition); disabled=$($constraint.disabled) |") }
    foreach ($trigger in $triggers) {
      $evidence = $triggerEvidence["$($trigger.schema).$($trigger.table).$($trigger.name)"]
      $anchor = ("dbo$($trigger.name)").ToLowerInvariant()
      $messages = @([regex]::Matches($trigger.definition, "THROW\s+\d+,\s*'((?:''|[^'])*)'", 'IgnoreCase') | ForEach-Object { $_.Groups[1].Value -replace "''", "'" })
      $summary = if ($messages.Count) { $messages -join '; ' } else { 'No direct THROW text; inspect raw definition' }
      $dictionary.Add("| [$(Cell $trigger.name)](current-triggers.md#$anchor) | Observed rejection text: $(Cell $summary) $($trigger.events); disabled=$($trigger.disabled); [migration](../../../$($evidence.sourcePath)#L$($evidence.sourceLine)); $($evidence.textComparison) |")
    }
    $dictionary.Add('')
  }
}

$triggerDocs = [System.Collections.Generic.List[string]]::new()
$triggerDocs.Add('# Current SQL trigger definitions')
$triggerDocs.Add('')
$triggerDocs.Add('CURRENT_VERIFIED in an isolated migrated SQL Server only. Raw `sys.sql_modules.definition` is retained in [inventory](current-schema.inventory.json) under `sql.triggers`; this page presents it for reading. Definitions are source text, not runtime behavior tests or accepted business rules. SHA-256 uses UTF-8 after CRLF/CR to LF conversion, with no trimming or SQL rewriting. Comparison permits outer whitespace only as a separately labeled formatting difference; catalog and migration hashes remain distinct.')
$triggerDocs.Add('')
foreach ($trigger in $inventory.sql.triggers) {
  $evidence = $triggerEvidence["$($trigger.schema).$($trigger.table).$($trigger.name)"]
  $triggerDocs.Add("### $($trigger.schema).$($trigger.name)")
  $triggerDocs.Add('')
  $triggerDocs.Add("- Table: $($trigger.schema).$($trigger.table). Enabled: $(-not $trigger.disabled). Timing: $(if ($trigger.insteadOf) { 'INSTEAD OF' } else { 'AFTER' }). Events: $($trigger.events). Not-for-replication: $($trigger.notForReplication).")
  $triggerDocs.Add("- Observed definition SHA-256 (UTF-8/LF): $($evidence.observedDefinitionSha256Utf8Lf). Latest migration SQL SHA-256 (UTF-8/LF): $($evidence.migrationSqlSha256Utf8Lf). Text comparison: $($evidence.textComparison). Runtime behavior: $($evidence.runtimeBehavior).")
  $triggerDocs.Add("- Created in $($evidence.createdMigration). Latest definition in $($evidence.lastDefinitionMigration): [source](../../../$($evidence.sourcePath)#L$($evidence.sourceLine)). Ordered Up-operation history: $(($evidence.operationHistory | ForEach-Object { "$($_.MigrationId):$($_.Action)" }) -join ', ').")
  $messages = @([regex]::Matches($trigger.definition, "THROW\s+\d+,\s*'((?:''|[^'])*)'", 'IgnoreCase') | ForEach-Object { $_.Groups[1].Value -replace "''", "'" })
  $triggerDocs.Add("- Technical interpretation from observed THROW messages: $(if ($messages.Count) { $messages -join '; ' } else { 'UNKNOWN; read SQL below' }) This does not establish the full trigger policy.")
  $triggerDocs.Add('')
  $triggerDocs.Add('```sql')
  $triggerDocs.Add($trigger.definition)
  $triggerDocs.Add('```')
  $triggerDocs.Add('')
}

$erd = [System.Collections.Generic.List[string]]::new()
$erd.Add('# Current relational ERD')
$erd.Add('')
$erd.Add('CURRENT_VERIFIED from [isolated SQL inventory](current-schema.inventory.json). Each Mermaid entity displays the exact `dbo.Table` name; `dbo_Table` is its internal Mermaid identifier. Table headings in the [dictionary](current-data-dictionary.md) give exact names. Solid relationships below exist in `sys.foreign_keys`. No edge is inferred from a property name.')
$erd.Add('')
$erd.Add('## Module overview')
$erd.Add('')
$erd.Add('```mermaid')
$erd.Add('flowchart LR')
foreach ($module in $moduleTables.Keys) { $erd.Add("  $($module -replace '-','_')[`"$module ($($moduleTables[$module].Count) tables)`"]") }
$cross = @($inventory.sql.foreignKeys | Where-Object { $assigned[$_.table] -ne $assigned[$_.principalTable] } | ForEach-Object { "$($assigned[$_.table])|$($assigned[$_.principalTable])" } | Sort-Object -Unique)
foreach ($edge in $cross) {
  $parts = $edge.Split('|')
  $erd.Add("  $($parts[0] -replace '-','_') --> $($parts[1] -replace '-','_')")
}
$erd.Add('```')
$erd.Add('')
$erd.Add('Arrows indicate child-module FK dependency on principal module, not transaction or service ownership. Cross-module FK details are in the per-table dictionary.')
$erd.Add('')
foreach ($module in $moduleTables.Keys) {
  $erd.Add("## $module")
  $erd.Add('')
  $erd.Add('```mermaid')
  $erd.Add('erDiagram')
  foreach ($table in $moduleTables[$module]) {
    $erd.Add("  dbo_$table[`"dbo.$table`"] {")
    $pk = @(TableItems $inventory.sql.keys $table | Where-Object kind -eq 'PRIMARY_KEY_CONSTRAINT' | ForEach-Object column)
    $fkCols = @(TableItems $inventory.sql.foreignKeys $table | ForEach-Object column)
    foreach ($column in (TableItems $inventory.sql.columns $table)) {
      if ($column.name -in $pk -or $column.name -in $fkCols) {
        $type = "$($column.type)" -replace '[^A-Za-z0-9_]','_'
        $keyType = $(if ($column.name -in $pk) { 'PK' } elseif ($column.name -in $fkCols) { 'FK' } else { '' })
        $erd.Add("    $type $($column.name) $keyType")
      }
    }
    $erd.Add('  }')
  }
  foreach ($group in (@($inventory.sql.foreignKeys | Where-Object { $_.table -in $moduleTables[$module] -and $_.principalTable -in $moduleTables[$module] }) | Group-Object name)) {
    $first = $group.Group[0]
    $required = @($group.Group | ForEach-Object { $childColumn = @(TableItems $inventory.sql.columns $_.table | Where-Object name -eq $_.column)[0]; -not $childColumn.nullable }) -notcontains $false
    $fkColumns = @($group.Group | Sort-Object ordinal | ForEach-Object column)
    $unique = $false
    foreach ($indexGroup in (@(TableItems $inventory.sql.indexes $first.table | Where-Object { $_.unique -eq $true -and $_.filtered -eq $false -and $_.included -eq $false }) | Group-Object name)) {
      $indexColumns = @($indexGroup.Group | Sort-Object ordinal | ForEach-Object column)
      if (($indexColumns -join '|') -eq ($fkColumns -join '|')) { $unique = $true }
    }
    $parentSide = $(if ($required) { '||' } else { 'o|' })
    $childSide = $(if ($unique) { 'o|' } else { 'o{' })
    $cardinality = "$parentSide--$childSide"
    $erd.Add("  dbo_$($first.principalTable) $cardinality dbo_$($first.table) : $($first.name)")
  }
  $erd.Add('```')
  $erd.Add('')
  $external = @($inventory.sql.foreignKeys | Where-Object { $_.table -in $moduleTables[$module] -and $_.principalTable -notin $moduleTables[$module] } | Group-Object name)
  if ($external.Count) {
    $erd.Add('Cross-module enforced FK (full composite columns and delete behavior: [dictionary](current-data-dictionary.md)):')
    $erd.Add('')
    foreach ($group in $external) {
      $first = $group.Group[0]
      $erd.Add(('- {0}.{1} -> {2}.{3} via {4}.' -f $first.schema,$first.table,$first.principalSchema,$first.principalTable,$group.Name))
    }
    $erd.Add('')
  }
}
$erd.Add('Logical application links without a SQL FK are intentionally omitted; inspect service/persistence code before drawing any such edge. On an edge principal-to-child, `||` means the child FK is required and `o|` means nullable; `o{` means zero-to-many child rows and `o|` zero-to-one when an exact unique FK index exists. Composite columns and delete behavior are in the dictionary.')

$outputs = @{
  (Join-Path $dataDir 'current-data-dictionary.md') = (($dictionary -join "`n") + "`n")
  (Join-Path $dataDir 'current-triggers.md') = (($triggerDocs -join "`n") + "`n")
  (Join-Path $dataDir 'current-erd.md') = (($erd -join "`n") + "`n")
}
foreach ($path in $outputs.Keys) {
  if ($Check) {
    if (-not (Test-Path -LiteralPath $path) -or (Get-Content -LiteralPath $path -Raw) -ne $outputs[$path]) { throw "Generated documentation differs: $path" }
  } else {
    [System.IO.File]::WriteAllText($path, $outputs[$path], [System.Text.UTF8Encoding]::new($false))
  }
}
"RF-06A docs: $($inventory.sql.tables.Count) tables, $($inventory.sql.columns.Count) columns, $((@($inventory.sql.foreignKeys | Group-Object schema,table,name)).Count) FK constraints; check=$Check"
