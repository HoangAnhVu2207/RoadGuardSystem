# RG-CI-01: baseline migration, SQL validation, CI and dev/test scenarios

Status: ASSIGNED by the owner on 2026-10-09 in the RG-CI-01 task attachment. This document is the single implementation checklist and handoff report. The owner authorizes production migration consolidation for new dev/test databases and evidence-based test reductions; this supersedes the earlier organization-only restriction. The owner subsequently authorized a normal commit and push to `huy-review` for review; merge, develop/main promotion, force-push and branch-protection changes remain prohibited.

Writer/branch: Codex / `huy-review`. Starting branch: `huy-review`; starting HEAD: `fc70d4714921185d9b0d93e766835aed654102d2`; initial working tree: clean, no owner changes. Intended eventual integration branch: `develop`; no integration is performed by this task.

## Goal, scope and safety

Preserve current business/API semantics while correcting the notification race oracle, replacing historical migrations with one direct current-schema baseline, retaining valid risk coverage with lower SQL/CI cost, and extending the existing Seeder with coherent synthetic dev/test scenarios. No new business APIs, Postman collection, package upgrades or HUY business redesign.

Task-owned isolated databases only. Never reset shared/team databases or `RoadGuard_HUY_SWG_20261008_140358`, and never modify the real-Swagger evidence directories. Private configuration, keys and tokens stay outside tracked evidence. Existing Git history preserves the old source checkpoint; no archival commit or compiled old migrations are created.

One writer per file was maintained across the race, schema, test, CI and Seeder work. Initial independent audits were read-only and wrote task-local evidence outside the repository. No unrelated owner edits existed at task start.

## Checklist

- [x] Record starting source/working tree and read active repository guidance.
- [x] Fetch exact failing workflow/job logs and verify failure against current source.
- [x] Fix and execute the affected race method and class on genuine SQL.
- [x] Inventory old schema/custom SQL, fixtures/tests/timing and every actual table's seed coverage.
- [x] Capture the old-chain schema in a new isolated task database before deleting migrations.
- [x] Generate one direct baseline and preserve necessary custom SQL/foundational data.
- [x] Apply baseline on fresh SQL, compare schemas and verify no pending model changes.
- [x] Map every original executable case to retained SQL, pure unit, justified consolidation or obsolete-history removal.
- [x] Separate active integration and migration validation sources; update dependent paths only.
- [x] Optimize fixtures/CI using measured costs, isolated resources and authoritative aggregate gates.
- [x] Extend the existing Seeder and export scenario/account/ID and all-table coverage manifests.
- [x] Seed twice; verify invariants/storage and preservation of representative supported user changes.
- [x] Execute the new SQL suite and affected unit/source/workflow checks with identity-based lane reconciliation.
- [x] Run format/diff checks and two self-review passes; record exact local and hosted limitations.

## Current evidence

`CURRENT_VERIFIED`: workflow `37804511721`, SQL job `113405158157`, tested SHA `fc70d4714921185d9b0d93e766835aed654102d2`, failed only `Huy02NotificationAuthorityTests.ConcurrentSameKeyOrCompetingCommandHasSingleReadEffect(sameKey: False)` at line 153 with NullReferenceException; 722 passed / 1 failed / 723 total, test execution 17.3886 minutes. Exact masked GitHub log and run metadata were downloaded to the private task working directory `D:\DO_AN\rg-ci-01-work`.

At the starting checkpoint the test replayed key `race` regardless of the winning command. The affected test and production notification persistence code were unchanged from the earlier stable baseline; the fix targets the test oracle only. Current-schema validation and business risks remain mandatory even though historical upgrade/downgrade acceptance is waived.

The corrected method associates each key with its actual outcome, replays the single Success winner and checks Replayed/nonnull before comparing RowVersion. Fresh-build targeted method: 2 passed / 0 failed / 0 skipped. Affected class plus old catalog extractor: 19 passed / 0 failed / 0 skipped (32s reported test duration). Extended old-chain catalog capture: 1 passed (33s). These ran on genuine SQL Server 2019-CU18 in owned disposable fixture databases before migration replacement.

Old-chain source: 69 migrations, 62 designers, 10 partial guards, snapshot and factory (143 files; 17,027,621 bytes). Fresh captured catalog: 191 business tables and 189 installed triggers. EF trigger annotations alone omit 27 installed triggers and name two absent triggers; consolidation preserves installed behavior without inventing those two definitions. Required extra direct SQL includes five composite FKs and one filtered unique index. No migration-owned constant foundational data was found; source-driven historical backfills are unnecessary in empty new databases.

Task resources: new SQL container `roadguard-rgci01-sql` on loopback port 14339; new genuine MinIO built from official source tag `RELEASE.2025-04-22T22-12-26Z`, source SHA `f19c534b9f457773dcd043d977433e1a71525c3b`, on loopback port 19000. MinIO readiness returned HTTP 200. Official image pulls failed (DockerHub access denied, Quay HTTP 401); source build passed. Existing Swagger SQL/storage and evidence packs remain preserved. Private resource credentials are outside the repository.

## Schema matrix

`CURRENT_VERIFIED`: the original 69-migration chain and new `20261008171739_BaselineCurrentSchema` each applied to separate fresh task-owned SQL Server 2019-CU18 databases. The extended catalog comparison in `D:\DO_AN\rg-ci-01-work\schema-equivalence-final.json` reports zero business-schema differences: 191 tables, 1,940 columns, 722 FKs, 899 indexes, 229 checks and 189 actual triggers; no views, procedures, functions, sequences or spatial indexes. It compares column type/nullability/identity/default/computed shape, FK columns/delete action, index filter/include, checks and trigger definitions. EF history deliberately differs: 69 old rows versus one new baseline row. Model snapshot and pinned EF 8.0.17 `has-pending-model-changes` passed. The new migration directly creates current tables, then applies only necessary custom SQL: 11 model-omitted defaults, five manual FKs, one filtered unique index and 189 installed trigger definitions. It does not concatenate historical `Up` methods. No migration-owned constant foundational data was found. The detailed old-chain catalog and source audit are at `D:\DO_AN\rg-ci-01-work\old-chain-schema.extended.inventory.json` and `schema-source-audit.md`; six baseline equivalence/negative tests passed. The 69 superseded migration sources, 62 designers and 10 historical partial guards were removed; only the new baseline, designer, snapshot and design-time factory remain.

## Test risk matrix

`CURRENT_VERIFIED`: the original executed 723 case identities are individually reconciled in `D:\DO_AN\rg-ci-01-work\test-risk-matrix-final.json` and `.md`: 652 retained for real SQL, 53 genuinely pure rows transferred to unit, 14 duplicate historical catalog checks consolidated into two current-schema checks, and four obsolete historical-upgrade rows removed. 53 transferred rows passed focused unit execution; the complete unit suite passed 966/966. The new SQL inventory has 658 executed identities across 102 classes; the first full local run passed 657 and exposed one `COL_LENGTH` Int16/Int32 fixture assumption, corrected with an explicit SQL cast; its focused rerun passed 1/1. The final four-lane run passed 123/130/215/190 = 658/658, zero skipped; the executed identity union matched the static manifest exactly once per case. A gate adversarial check rejected duplicate identity and missing-lane fixtures. Distinct SQL risks (constraints/triggers, spatial, rollback/ACK loss, idempotency, concurrency, DB-backed authorization, outbox, offline, storage and repeated seeding) remain in SQL. Migration-related test files now sit under `tests/RoadGuardSystem.IntegrationTests/Migrations/` without namespace/class/assertion changes; integration/business tests remain in their module folders. No CI or script path references to moved source files remained. The original 110 classes and 611 methods are documented in the matrix. The retained 658 cases exceed the soft 100–150 target because distinct SQL risks were not discarded for a count.

## Seed coverage matrix and scenarios

`CURRENT_VERIFIED` (RG-CI-01 Seeder follow-up): fresh isolated SQL checkpoints in `planning/development/evidence/rg-ci-01/seed-coverage-ci.json` and `seed-coverage.json` assessed all 192 tables, including EF history. The no-storage CI mode has 32 populated / 160 empty tables and three explicit GAPs: storage-backed report/dataset/FIELD/repair prerequisites, genuine signed offline payloads, and real AI inference. The task-owned SQL/MinIO mode has 69 populated / 123 empty tables and the latter two GAPs. Empty rows whose catalog says a producer extension must execute are `NOT_IMPLEMENTED` by this Seeder; a zero count does not establish an API guard failure. Both fresh modes ran the actual Development Seeder CLI twice, with the same project ID and zero table-count differences. Storage-backed FIELD reached `InProgress`, with one persisted start origin and three task events; no verified field measurement or real construction fact was fabricated. Nine storage-backed scenarios cover the synthetic project, road, survey task/dataset, three stored-byte reporter report graphs, two triaged/case graphs and FIELD task. The original 65/127 task manifest is historical and superseded for current coverage. The regression reruns before and after a supported ProjectUpdateService edit and preserves project identity/name. Private SQL, MinIO and password inputs remain outside Git; no shared/team/Swagger database was reset or modified.

## Validation and handoff

Required gates: race method/class; old/new schema equivalence and model snapshot; active SQL suite; affected pure unit/source-contract/workflow checks; lane identity union/no-duplicates; Seeder twice and supported user-change preservation; principal scenario execution; format and diff checks. The original package's pre-push `PENDING_EXACT_SHA_RUN` note is historical: exact-SHA run `37824166393` failed the lane-4 Seeder two-run count check after all 190 lane tests passed. The follow-up fixes that observed failure locally; its hosted outcome is reported only after the follow-up push. Local timing is not hosted timing.

Self-review pass 1: scope/data reviewed; production changes are limited to the authorized baseline and Seeder, and the notification fix is test-only. Self-review pass 2: focused build/tests, two-run SQL/MinIO manifests, format, diff and private-secret checks reviewed; no task private SQL/password value appears in changed files. External review: pending. The original package checkpoint is historical; this follow-up is delivered as a separate `huy-review` commit. Merge: NOT PERFORMED.

CI measurements from the exact failed hosted run: queue to job start 3s; SQL job 20m02s; SQL credential/start/readiness 10s/14s/10s; restore 13s; full-solution build 96s; integration test step 17m25s. Seeder validation was skipped after the failed test step, so no Seeder timing is claimed for that run. Other jobs: unit 1m47s, API 6m03s, source verification 2m10s.

The first local new-suite SQL wall time was 601.85s, still above the 5–8 minute soft CI target before hosted overhead. The current CI design therefore assigns all 102 classes to four independent hosted runners (123/130/215/190 expected case identities) with separate SQL containers and fixture-owned databases. `tests/CI/sql-lanes.json` fixes the exact 658 expected display identities; `Invoke-SqlLane.ps1` runs a class-exclusive filter, and `Verify-SqlLanes.ps1` rejects zero, failed, skipped, missing, duplicate and unexpected identities. The required-check-compatible aggregate job remains named `SQL integration tests` and fails if any lane fails/cancels/skips. Seeder/baseline/model validation executes in one lane with a separate migrated database, twice; coverage and TRX artifacts remain available. Four-lane modeled maximum serial body-plus-fixture cost is ~302 seconds, not a hosted measurement. Scoped project builds, protected credentials, bounded readiness/cleanup, NuGet caching, scheduled/manual coverage and same-branch/PR cancellation remain. The original local four-lane run passed in a 3m22s maximum lane test step on the task-owned machine (the four steps ran concurrently); hosted run `37824166393` subsequently failed the Seeder count gate in lane 4. Follow-up hosted acceptance remains unverified until its exact SHA is observed.

## Final local validation and safe reproduction

- Baseline old/new catalog comparison: zero business-schema differences; six equivalence/negative tests passed; EF pending-model check passed with pinned 8.0.17.
- Four genuine SQL lanes: 123/123, 130/130, 215/215, 190/190. Aggregate identity reconciliation: 658 unique / no overlap, missing or unexpected identities. Duplicate-identity and missing-lane adversarial probes were rejected.
- Final solution build: PASS, 0 errors (323 analyzer warnings). Unit: 966/966 pass. API: 406/406 pass, zero skipped. Focused Seeder supported-edit preservation: 1/1 pass. Development Seeder on task SQL/MinIO: two initial successful invocations and two further post-login invocations, same project and all 192 table counts within each pair; five valid sessions preserved. API full rerun with genuine task-owned MinIO: 406/406 pass, zero skipped in 4m07s test duration. Seeded-principal live API smoke: five login HTTP 200; work-package 200/200/200/200/403 as above. `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore`: PASS; `git diff --check`: PASS.
- Workflow verifier: PASS. Its 12 negative security/configuration fixtures all produced the required rejecting exit code. TRX gate self-tests: PASS for success plus missing, zero, failed, skipped, duplicate and incomplete rejects. Documentation, agent setup, Docker Compose and dependency security source checks: PASS. RF05 Python readiness/guidance checks: 11/7/4 tests PASS, repository verifier PASS.
- Test-source organization: nine dedicated migration classes moved from Files, Inspections, Offline, Persistence, Projects and Retention into the same integration project’s `Migrations/` subtree; two schema validation classes also moved there. Namespaces, assertions, project references, test filters and discovery are preserved; final SQL count is 658 before and after physical moves. No production migration was moved for organization; production migration consolidation is separately owner-authorized by this task.

To reproduce against a **new, explicitly task-owned** SQL database, set `ROADGUARD_MIGRATION_CONNECTION_STRING` and `ROADGUARD_CONNECTION_STRING` locally to that database, then run the commands below. The angle-bracket values are local private inputs, never literal values to commit:

```powershell
dotnet tool install dotnet-ef --tool-path <private-tool-dir> --version 8.0.17
& <private-tool-dir>/dotnet-ef database update --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj --context RoadGuardDbContext
$env:ROADGUARD_SEED_PROFILE = 'Development'
$env:ROADGUARD_SEED_MANIFEST_PATH = '<private-manifest-path>'
$env:ROADGUARD_SEED_PASSWORD = '<locally supplied compliant development password>'
$env:ROADGUARD_SEED_STORAGE_CONFIG_PATH = '<private task-owned MinIO config JSON>'
dotnet run --project tools/RoadGuardSystem.Seeder
dotnet run --project tools/RoadGuardSystem.Seeder
dotnet build tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore
$env:ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING = '<task-owned SQL Server master connection>'
1..4 | ForEach-Object { pwsh -File tests/CI/Invoke-SqlLane.ps1 -Lane $_ -ResultsPath "<private-result-root>/lane-$_" }
pwsh -File tests/CI/Verify-SqlLanes.ps1 -ResultsPath '<private-result-root>'
```

The migration command applies only to the exact database in the supplied local connection string. Use a fresh task-owned name when reset is needed; no command here drops a shared database. Never place private connection strings, passwords or storage keys in the manifest or Git. The actual task-run commands used only `D:\DO_AN\rg-ci-01-work\private` inputs, task-owned SQL port 14339 and task-owned MinIO port 19000.

Merge and branch-protection edits: NOT PERFORMED. Owner-authorized `huy-review` commit/push result is reported with the delivery. `main` and `develop`: untouched. Original package base: `fc70d4714921185d9b0d93e766835aed654102d2`; follow-up base: `9afa7da6b6f9b5cf31811ba22e75ccd80dba97b7`. Hosted CI for the follow-up is assessed once after push and reported in the delivery; no hosted timing claim yet. Business/API behavior: no intentional change. DTO/wire semantics: no change. Production migrations: consolidated in the original authorized package, unchanged by this follow-up.


## Local Development startup cutover (owner-authorized follow-up)

`CURRENT_VERIFIED`: ordinary `dotnet run --no-build` from `RoadGuardSystem.API` uses the `http` launch profile (`Development`, port 5112). `Program.cs` appends `appsettings.Development.local.json` after default providers, so its `RoadGuardDatabase:ConnectionString` takes precedence over earlier user/environment values. The file has no UserSecretsId, and relevant process environment overrides were unset. Before correction, the ignored private local config pointed to `DESKTOP-MO4SJ7Q\MSSQLSERVERGIAHU` / `RoadGuardSystem`; that existing database has `CauseCategories` and old migration-history entries ending at `20261007135009_OwnerDefectStatisticsActivation`, but no direct-baseline row. Applying `20261008171739_BaselineCurrentSchema` there caused SQL 2714. No table/history/source was changed on that old database.

The ignored private `RoadGuardSystem.API/appsettings.Development.local.json` now points to task-owned SQL Server `localhost,14339` / `RoadGuard_RGCI01_Seed_c71f2655cc1b407da33d448e36eb285c` and task-owned MinIO `localhost:19000`. A private backup of the prior local config is outside Git. Automatic migration and Development user seeding remain enabled; this is a configuration cutover, not a source/migration workaround. SQL history on the task database contains exactly `20261008171739_BaselineCurrentSchema`.

Owner startup path verified: ordinary `dotnet run --no-build` completed without SQL 2714; `http://localhost:5112/health` returned 200; `http://localhost:5112/swagger/index.html` and `/swagger/v1/swagger.json` returned 200, with 237 operations; the seeded PM login returned 200, followed by authorized task-project work-package read 200. Passwords/tokens/responses were never placed in Git or the report. The smoke process was stopped afterward so the owner can use port 5112. To repeat, keep the ignored local config pointed at a freshly baseline-initialized task-owned dev/test database, then run `dotnet run` from `RoadGuardSystem.API`. Existing old-chain databases must remain preserved; the direct baseline is for fresh isolated dev/test databases only. The FieldInspectionPurpose sentinel warning did not cause SQL 2714 and was left out of scope.

## Review evidence

Sanitized evidence committed with this package is under `planning/development/evidence/rg-ci-01/`: schema-equivalence result and source audit; compressed complete old catalog and 723-case risk matrix; all-table seed/scenario manifest; validation/identity summaries; and task API status evidence. See its `README.md` for file roles and synthetic limitations. The current-schema inventory remains at `docs/backend/data/current-schema.inventory.json`. No private config, credentials, runtime database files, MinIO data, TRX or full logs are included. `Verify-SqlLaneDiscovery.ps1` compares the compiled xUnit classes with `sql-lanes.json` before each lane: 102/102 matched, and removal of one class from a temporary manifest was rejected. A new test in an assigned class is caught by the executed-identity aggregate gate; both duplicate identity and missing lane probes were rejected. Staged review caught whitespace-only lines inside raw SQL trigger literals. Removing them altered exact catalog definitions, so the original SQL text was restored and a path-specific `.gitattributes` blank-line rule was added. Fresh baseline/schema revalidation passed 2/2 with zero catalog differences; the schema inventory source hash was refreshed.

## Exact changed-path inventory (review candidate)

Generated from the staged Git diff, with rename detection disabled to show superseded test/migration sources and replacements explicitly. `D` means an authorized obsolete source; `A` includes the direct baseline, tests and sanitized evidence. All paths are task-related; no initial owner-dirty paths existed.

### Baseline/schema (145 paths)

- `D` `RoadGuardSystem.Repositories/Migrations/20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260918152126_AddIdentitySessionSecurityLogs.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260918152126_AddIdentitySessionSecurityLogs.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260918185427_EnforceAuditActorUserForeignKey.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260918185427_EnforceAuditActorUserForeignKey.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260918185738_EnforceSecurityLogSafeCodes.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260918185738_EnforceSecurityLogSafeCodes.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260919085118_AddImmutableFileStorageBoundary.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260919085118_AddImmutableFileStorageBoundary.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920081348_AddProjectMembershipSchema.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920081348_AddProjectMembershipSchema.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920101324_AddDefectCatalogAndSeverityRules.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920101324_AddDefectCatalogAndSeverityRules.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920104522_AddDefectCatalogConsumerBoundary.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920104522_AddDefectCatalogConsumerBoundary.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920133014_AddDroneDeviceRegistry.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920133014_AddDroneDeviceRegistry.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920140643_AddNotificationPersistenceBoundary.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920140643_AddNotificationPersistenceBoundary.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920154542_AddRoadSectionVersionAndWarrantySchema.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920154542_AddRoadSectionVersionAndWarrantySchema.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920172407_AddSurveyPlanningSchema.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920172407_AddSurveyPlanningSchema.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920182623_AddSurveyAssignmentSchema.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260920182623_AddSurveyAssignmentSchema.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921125553_AddP230FlightSurveyFileSchema.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921125553_AddP230FlightSurveyFileSchema.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921131520_AddP230DataVersionQualityCheckSchema.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921131520_AddP230DataVersionQualityCheckSchema.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921131946_AddP230SupplementarySurveyRequestSchema.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921131946_AddP230SupplementarySurveyRequestSchema.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921132735_AddP230ConfirmedDatasetImmutability.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921132735_AddP230ConfirmedDatasetImmutability.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921134719_AddP230FlightSurveyIdentityImmutability.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921134719_AddP230FlightSurveyIdentityImmutability.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921160000_P122SurveyPlanningContracts.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921160000_P122SurveyPlanningContracts.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921170153_P122CanonicalSurveyRequestStatus.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921170153_P122CanonicalSurveyRequestStatus.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921182227_P231ProcessingAndOutboxDelivery.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260921182227_P231ProcessingAndOutboxDelivery.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260922034831_P232DetectionDefectTaskSchema.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260922034831_P232DetectionDefectTaskSchema.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260922110000_P122SurveyPlanningRoadSectionVersionAnchor.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260927153000_AddPasswordRecoveryRequests.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260927191334_AddNotificationReadVersion.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260927191334_AddNotificationReadVersion.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260928094828_AddIdentityOnboardingEmailTemplates.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260928094828_AddIdentityOnboardingEmailTemplates.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260928141150_AddSurveyV2ScopesAndConcurrency.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260928141150_AddSurveyV2ScopesAndConcurrency.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260928160534_AddRoadSegmentScopeReferences.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260928160534_AddRoadSegmentScopeReferences.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929063929_P2ReconcileIdentityOnboardingSnapshot.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929063929_P2ReconcileIdentityOnboardingSnapshot.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929064148_P2AddUploadSessionsAndFileScopes.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929064148_P2AddUploadSessionsAndFileScopes.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929093550_P2DatasetProcessingFoundation.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929093550_P2DatasetProcessingFoundation.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929101050_P2ProcessingValidationFoundation.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929101050_P2ProcessingValidationFoundation.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929102803_P2ProcessingManifest.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929102803_P2ProcessingManifest.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929113527_V2P1063FieldInspectionTaskRowVersion.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929113527_V2P1063FieldInspectionTaskRowVersion.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929120044_P2AllowProcessingJobModelVariants.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929120044_P2AllowProcessingJobModelVariants.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929125522_P2ValidationMeasurementProvenance.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20260929125522_P2ValidationMeasurementProvenance.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261002000100_Anh01M1FileSizeBigint.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261002031518_Anh01GeometrySurveyReview.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261002031518_Anh01GeometrySurveyReview.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261002090000_Anh01ScopeAdoptionCorrection.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261002100000_Anh01RequestScopeRootCorrection.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261002120000_AnhHuySharedIntegration.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261002120000_AnhHuySharedIntegration.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261003082408_Huy01SessionTransport.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261003082408_Huy01SessionTransport.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261003160000_AnhHuyDependencyDefectConcurrency.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261003170000_Anh01MultipartRecovery.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261003170000_Anh01MultipartRecovery.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261003180000_Huy01DefectSourceLinks.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261003180000_Huy01DefectSourceLinks.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261003190000_Huy01TrainingLabels.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261003190000_Huy01TrainingLabels.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261003200000_Huy01ExportConsumer.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261003200000_Huy01ExportConsumer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261003210000_Huy01AiProducer.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261003210000_Huy01AiProducer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261003220000_Huy01AiAttemptClosure.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006015156_H0RetentionIntegration.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006015156_H0RetentionIntegration.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006021748_H1PersistentIdentityAndClocks.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006021748_H1PersistentIdentityAndClocks.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006030147_H2NativeGeometryAndPavement.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006030147_H2NativeGeometryAndPavement.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006035334_H3FieldLifecycleAndIntake.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006035334_H3FieldLifecycleAndIntake.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006054054_H4RepairCore.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006054054_H4RepairCore.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006084919_H4RepairProducers.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006084919_H4RepairProducers.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006094905_H5OfflinePersistence.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006094905_H5OfflinePersistence.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006100156_H6NotificationPersistence.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006100156_H6NotificationPersistence.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006101243_H4RepairContinuationSources.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006101243_H4RepairContinuationSources.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006101324_H5AuthenticatedAttachedPayload.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006101324_H5AuthenticatedAttachedPayload.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006103243_H6ProjectLifecycleHistory.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006103243_H6ProjectLifecycleHistory.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006155824_H6CalendarPlanningProof.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006155824_H6CalendarPlanningProof.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006163740_H4SafetySourceAdmission.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261006163740_H4SafetySourceAdmission.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261007052610_OwnerReceivingRequests.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261007052610_OwnerReceivingRequests.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261007053131_OwnerClockDutyAppointments.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261007053131_OwnerClockDutyAppointments.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261007060739_OwnerWeeklyReviewRecovery.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261007060739_OwnerWeeklyReviewRecovery.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261007115552_OwnerLifecycleActivation.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261007115552_OwnerLifecycleActivation.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261007131646_OwnerRoadCoverageActivation.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261007131646_OwnerRoadCoverageActivation.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261007135009_OwnerDefectStatisticsActivation.Designer.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/20261007135009_OwnerDefectStatisticsActivation.cs`
- `A` `RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.Designer.cs`
- `A` `RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/H3FieldLifecycleAndIntake.Guards.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/H4RepairContinuationSources.Guards.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/H4RepairCore.Guards.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/H4RepairProducers.Guards.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/H5OfflinePersistence.Guards.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/H6NotificationPersistence.Guards.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/H6ProjectLifecycleHistory.Guards.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/OwnerDefectStatisticsActivation.Guards.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/OwnerLifecycleActivation.Guards.cs`
- `D` `RoadGuardSystem.Repositories/Migrations/OwnerRoadCoverageActivation.Guards.cs`
- `M` `docs/backend/data/README.md`
- `M` `docs/backend/data/current-schema.inventory.json`

### Seeder (7 paths)

- `M` `RoadGuardSystem.Repositories/Implementations/Seeding/PostmanScenarioSeedStep.cs`
- `M` `RoadGuardSystem.Repositories/Implementations/Seeding/PostmanUserSeedStep.cs`
- `A` `tools/RoadGuardSystem.Seeder/DevelopmentScenarios.cs`
- `M` `tools/RoadGuardSystem.Seeder/Program.cs`
- `M` `tools/RoadGuardSystem.Seeder/RoadGuardSystem.Seeder.csproj`
- `A` `tools/RoadGuardSystem.Seeder/SeedCoverageCatalog.json`
- `A` `tools/RoadGuardSystem.Seeder/synthetic-survey.mp4`

### CI (8 paths)

- `M` `.github/workflows/ci.yml`
- `A` `tests/CI/Invoke-SqlLane.ps1`
- `M` `tests/CI/Verify-CiWorkflow.ps1`
- `A` `tests/CI/Verify-SqlLaneDiscovery.ps1`
- `A` `tests/CI/Verify-SqlLanes.ps1`
- `A` `tests/CI/Verify-TestResults.Tests.ps1`
- `A` `tests/CI/Verify-TestResults.ps1`
- `A` `tests/CI/sql-lanes.json`

### Tests/fixtures (85 paths)

- `M` `tests/RoadGuardSystem.ApiTests/Defects/Huy01DefectConcurrencySchemaTests.cs`
- `M` `tests/RoadGuardSystem.ApiTests/Projects/Anh01LegacyGeometryCorrectionTests.cs`
- `M` `tests/RoadGuardSystem.ApiTests/Reports/Huy01ReporterReportsApiTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Configuration/DatabaseOptionsValidationTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Defects/P232DetectionDefectSchemaTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Files/Anh01FileMigrationTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Files/FileRepositoryBoundaryTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Files/FileRepositorySqlTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Files/FileSchemaContractTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Files/MultipartRecoveryMigrationTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Huy01/Huy01SharedSchemaTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Identity/H1PersistentIdentitySqlTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Identity/IdentityPersistenceNegativeTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Identity/IdentityPersistencePositiveTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Identity/P110AuthenticationPersistenceTests.cs`
- `A` `tests/RoadGuardSystem.IntegrationTests/Infrastructure/ConfiguredSqlServerOwnershipTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Infrastructure/IdentitySqlServerFixture.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Infrastructure/Rf06aSchemaInventoryTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Infrastructure/Rf09TransitionRehearsalTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Infrastructure/SqlServerTestFixture.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Inspections/H3FieldInspectionWorkflowSqlTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Inspections/H3FieldMigrationTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Inspections/P240FieldInspectionMeasurementSchemaTests.cs`
- `A` `tests/RoadGuardSystem.IntegrationTests/Migrations/CurrentSchemaRiskTests.cs`
- `A` `tests/RoadGuardSystem.IntegrationTests/Migrations/Files/Anh01FileMigrationTests.cs`
- `A` `tests/RoadGuardSystem.IntegrationTests/Migrations/Files/MultipartRecoveryMigrationTests.cs`
- `A` `tests/RoadGuardSystem.IntegrationTests/Migrations/Inspections/H3FieldMigrationTests.cs`
- `A` `tests/RoadGuardSystem.IntegrationTests/Migrations/Offline/H5OfflineMigrationTests.cs`
- `A` `tests/RoadGuardSystem.IntegrationTests/Migrations/Persistence/P202MigrationLifecycleTests.cs`
- `A` `tests/RoadGuardSystem.IntegrationTests/Migrations/Persistence/V2P1063MigrationUpgradeTests.cs`
- `A` `tests/RoadGuardSystem.IntegrationTests/Migrations/Projects/H2NativeMigrationTests.cs`
- `A` `tests/RoadGuardSystem.IntegrationTests/Migrations/Projects/LD06LifecycleMigrationTests.cs`
- `A` `tests/RoadGuardSystem.IntegrationTests/Migrations/Retention/H0RetentionMigrationTests.cs`
- `A` `tests/RoadGuardSystem.IntegrationTests/Migrations/Rf09TransitionRehearsalTests.cs`
- `A` `tests/RoadGuardSystem.IntegrationTests/Migrations/Validation/BaselineSchemaTests.cs`
- `A` `tests/RoadGuardSystem.IntegrationTests/Migrations/Validation/Rf06aSchemaInventoryTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Notifications/H6NotificationPersistenceGuardSqlTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Notifications/Huy02NotificationAuthorityTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Notifications/OwnerWeeklyRecoverySqlTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Offline/H5OfflineMigrationTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Persistence/P202MigrationLifecycleTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Persistence/P202ValidationAndRedactionTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Persistence/V2P1063MigrationUpgradeTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Processing/P230ProcessingJobContractTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Processing/P231ProcessingPersistenceTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Processing/P234ValidationRunContractTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Projects/H2NativeMigrationTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Projects/LD06LifecycleMigrationTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Projects/P220ProjectMembershipSchemaTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Projects/P221RoadWarrantySchemaTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairCoreMigrationTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairProducerMigrationTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Repairs/H4SafetyRuntimeSqlTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Repairs/LD07RoadCoverageSqlTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Repairs/LD08DefectStatisticsSqlTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Retention/H0RetentionMigrationTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Seeding/SeederTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Spatial/SpatialInvariantTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Surveys/Anh01ScopeAdoptionCorrectionTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Surveys/P122SurveyPlanningPersistenceTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Surveys/P219DatasetContractTests.cs`
- `D` `tests/RoadGuardSystem.IntegrationTests/Surveys/P222SurveyPlanModelTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Surveys/P222SurveyPlanningSchemaTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Surveys/P223SurveyAssignmentSchemaTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Surveys/P230DataVersionQualityCheckSchemaTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Surveys/P230FlightSurveyFileSchemaTests.cs`
- `M` `tests/RoadGuardSystem.IntegrationTests/Surveys/P230SupplementarySurveyRequestSchemaTests.cs`
- `M` `tests/RoadGuardSystem.UnitTests/Authentication/V2AuthenticationPersistenceContractTests.cs`
- `M` `tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/DatabaseOptionsValidationTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/FileRepositoryBoundaryTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/FileSchemaContractTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/Huy01SharedSchemaTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/IdentityPersistenceNegativeTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/P110AuthenticationPersistenceTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/P122SurveyPlanningPersistenceTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/P202ValidationAndRedactionTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/P219DatasetContractTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/P220ProjectMembershipModelTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/P222SurveyPlanModelTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/P230ProcessingJobContractTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/P231ProcessingPersistenceTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/P234ValidationRunContractTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/P240FieldInspectionMeasurementSchemaTests.cs`
- `A` `tests/RoadGuardSystem.UnitTests/TransferredIntegration/SpatialInvariantTests.cs`

### Documentation/evidence (10 paths)

- `M` `.gitattributes`
- `A` `planning/development/RG-CI-01.md`
- `A` `planning/development/evidence/rg-ci-01/README.md`
- `A` `planning/development/evidence/rg-ci-01/api-seed-smoke.json`
- `A` `planning/development/evidence/rg-ci-01/old-chain-schema.inventory.json.gz`
- `A` `planning/development/evidence/rg-ci-01/schema-equivalence.json`
- `A` `planning/development/evidence/rg-ci-01/schema-source-audit.md`
- `A` `planning/development/evidence/rg-ci-01/seed-coverage.json`
- `A` `planning/development/evidence/rg-ci-01/test-risk-matrix.json.gz`
- `A` `planning/development/evidence/rg-ci-01/validation-summary.json`

## Review limits

The seeded graph’s project/geometry, report/evidence/case, survey and FIELD transitions were exercised through current service/repository producers and persisted in real SQL/MinIO; project update preservation was exercised through `ProjectUpdateService`. All five fake `example.test` principals logged in through a temporary Development API bound only to the task-owned database: SUPERVISOR, PM, DRONE_OPERATOR, REPAIR_CREW and REPORTER each returned HTTP 200. An authenticated project work-package read returned 200 for the four current staff members and 403 for the Reporter without ProjectMember membership, matching the source authorization boundary. The API process was stopped. After those five logins, two further Seeder runs kept the same project and all 192 table counts; five real persistent login sessions remained present. `D:\DO_AN\rg-ci-01-work\api-seed-smoke-final.json` records statuses only; no password, JWT, refresh token or response body. This does not turn synthetic observations into verified construction facts. The current fresh checkpoints above supersede the old count evidence and distinguish the three CI-mode GAPs from the two storage-backed GAPs.
