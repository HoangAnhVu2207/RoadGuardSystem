# RG-CI-01 schema source audit

CURRENT_VERIFIED source inspection at `fc70d4714921185d9b0d93e766835aed654102d2`. Starting working tree was clean. Read-only scope: DbContext/configurations/migrations/snapshot/schema inventory and references. No SQL connection, database write, source edit, staging or Git mutation performed. Only this audit and companion JSON are written outside the repository.

TARGET_CONFIRMED: owner task permits replacing historical migrations for newly created isolated dev/test databases; business schema/behavior must remain unchanged. Root agent owns implementation and unified report.

## Inventory and trust boundary

- migration: 69 files, 864,308 bytes.
- designer: 62 files, 15,416,441 bytes.
- guards: 10 files, 90,515 bytes.
- other: 2 files, 656,357 bytes.

Current migration chain: 69 numbered migration implementations, 62 designers, 10 partial guards, one snapshot and one design-time factory. Seven migrations contain their own migration attributes instead of separate designers. Existing JSON records 69 ending `20261007135009_OwnerDefectStatisticsActivation`.

Existing JSON is HISTORICAL for this task: 212/712 recorded source input byte hashes differ, including DbContext, snapshot, configurations and migrations. A normalized-LF snapshot hash also differs; source content trust cannot be recovered by assuming line endings. Do not use this JSON alone as current SQL equivalence proof. It provides valuable historical manual-object trace.

Historical observed catalog row counts (rows, not necessarily objects): checks=229, columns=1940, foreignKeys=722, indexes=899, keys=269, tables=191, triggers=189, views=0.

## Concrete current source/manual SQL discrepancies

CURRENT_VERIFIED: EF snapshot has 164 distinct HasTrigger names; existing SQL inventory has 189 trigger objects. HasTrigger metadata does not supply executable trigger bodies. Every trigger body must be copied from a fresh applied old-chain catalog (or equivalently evaluated UpOperations final state verified against that catalog), including loops, partial helpers, installed-definition rewrites and CREATE OR ALTER replacements.

The following 27 historical SQL trigger names are absent from current snapshot metadata (verify final catalog before adopting):

- `TR_CaseConclusionDefects_Immutable`
- `TR_CaseConclusionEvidence_Immutable`
- `TR_CaseConclusions_Immutable`
- `TR_CasePublicationDefects_Immutable`
- `TR_CasePublicationEvidence_Immutable`
- `TR_CasePublicationRecipients_Immutable`
- `TR_CasePublications_Immutable`
- `TR_CaseReportLinkHistoryReports_Immutable`
- `TR_CaseReportLinkHistory_Immutable`
- `TR_CaseReportLinks_AppendOnly`
- `TR_FieldInspectionTasks_H4RepairPin`
- `TR_FileScopes_Immutable`
- `TR_GroundTruthMeasurements_Integrity`
- `TR_RepairObligations_ProducerHeads`
- `TR_ReportOriginalEvidence_Identity`
- `TR_ReportOriginalEvidence_Immutable`
- `TR_ReportSupplementEvidence_Identity`
- `TR_ReportSupplementEvidence_Immutable`
- `TR_ReportSupplements_Immutable`
- `TR_Reports_OriginalImmutable`
- `TR_RoadGeometryMetadata_Immutable`
- `TR_SourceDecisions_Immutable`
- `TR_TrainingLabelReviews_AppendOnly`
- `TR_TrainingLabelRevisions_AppendOnly`
- `TR_TrainingLabels_Identity`
- `TR_WeeklyReviewDigestDuties_Source`
- `TR_WeeklyReviewDigests_Source`

The following 2 snapshot annotations have no corresponding recorded actual catalog object, and no explicit CREATE in current migration source:

- `TR_DerivedMeasurements_Immutable`
- `TR_MeasurementValidationSamples_Immutable`

`20260929125522_P2ValidationMeasurementProvenance.cs` creates DerivedMeasurements and MeasurementValidationSamples without trigger SQL. Adding trigger bodies here would change old-chain behavior and is not required to preserve schema. Flag the discrepancy; do not infer authorization from HasTrigger names.

Current manual composite FK source is `20261002120000_AnhHuySharedIntegration.cs:792-795,810`:

- `dbo.CasePublicationEvidence.FK_CasePublicationEvidence_OriginalEvidence` (OriginalEvidenceId, SourceReportId) -> `dbo.ReportOriginalEvidence` (Id, ReportId), NO ACTION.
- `dbo.CasePublicationEvidence.FK_CasePublicationEvidence_SupplementEvidence` (SupplementEvidenceId, SourceReportId) -> `dbo.ReportSupplementEvidence` (Id, ReportId), NO ACTION.
- `dbo.CaseConclusionEvidence.FK_CaseConclusionEvidence_OriginalEvidence` (OriginalEvidenceId, SourceReportId) -> `dbo.ReportOriginalEvidence` (Id, ReportId), NO ACTION.
- `dbo.CaseConclusionEvidence.FK_CaseConclusionEvidence_SupplementEvidence` (SupplementEvidenceId, SourceReportId) -> `dbo.ReportSupplementEvidence` (Id, ReportId), NO ACTION.
- `dbo.SourceDecisions.FK_SourceDecisions_CorrectionIdentity` (SupersedesDecisionId, SourceKind, SourceId, ProjectId) -> `dbo.SourceDecisions` (Id, SourceKind, SourceId, ProjectId), NO ACTION.

Current manual filtered unique index source `H4RepairCore.Guards.cs:49`: `UX_RepairDecisions_Initial` on `RepairDecisions(ItemId)` WHERE `[SupersedesDecisionId] IS NULL`; retain separately from generated EF indexes.

## Custom objects and foundational data

CURRENT_VERIFIED source search found trigger definitions, manual FKs, one raw unique index and data transformations. No CREATE VIEW/FUNCTION/PROCEDURE or EF HasData/InsertData was found in production migration/configuration sources. Historical inventory records zero views but does not query function/procedure catalog. A fresh catalog query must establish absence, rather than relying solely on text search.

There is no migration-owned constant foundational seed data to preserve. The only literal INSERT INTO statements are source-driven BusinessReceivingRequests backfills at `20261007052610_OwnerReceivingRequests.cs:132,141,148` from existing field/repair/breach state; fresh empty databases have none of these sources and need no corresponding backfill. Schema baseline should not fabricate receiving requests. Canonical roles are produced by existing `Implementations/Seeding/IdentityRoleSeedStep.cs:23-30` (five roles); retain Seeder responsibility. Other existing seed steps/Postman scenarios are separate from schema generation.

Geometry is SQL Server geometry with project/profile-specific validation, not geography. `ProjectConfiguration.cs:18-19` constrains configured UTM SRID to 32648/32649. `RoadSectionVersionConfiguration.cs:39` maps geometry; `CrsProfileConfiguration.cs:12,27` contains profile/native route constraints and annotations. Preserve actual check text and trigger scope validation, SQL spatial metadata/indexes if any; do not impose one global SRID.

## Direct baseline generation method (PROPOSED)

1. Build old-chain repository at recorded SHA in isolated output; create a new task-owned SQL database and migrate all 69. Capture complete ordered catalog JSON plus applied migration list and source/tool hashes before removal. Existing `Rf06aSchemaInventoryTests.MigratedSchema_MatchesModelAndSnapshot_AndWritesInventory` is the closest implementation, but writes a tracked docs file and has catalog gaps; adapt/extract into a task-owned inventory runner/output path. Do not run it blindly against a shared DB.
2. Generate one migration from an empty migration set/snapshot with EF Core 8.0.17 against unchanged RoadGuardDbContext; do this only after preserving old source/checkpoint/inventory. Design-time factory requires ROADGUARD_MIGRATION_CONNECTION_STRING and NetTopologySuite. Numbered migrations use both RoadGuardSystem.Repositories.Migrations and RoadGuardSystem.cRepositories.Migrations namespaces; retain discovery attributes on the new generated baseline.
3. Append only final required fresh-schema SQL: final catalog trigger definitions, 5 manual composite FKs, 1 manual filtered index and any additional actual catalog-only objects found by fresh inventory. Generate final definitions directly, not historical Up concatenation. Catalog trigger bodies already include installed rewrites from H4RepairProducers and later CREATE OR ALTER replacements.
4. Apply baseline to another newly owned database on same SQL Server/version/collation. Compare canonical old/new catalog shape, required foundational data and module bodies; report EF migration history as the sole expected metadata difference. Compare generated FK/default/check names carefully and explain only non-semantic generated naming differences.
5. Run EF migrations has-pending-model-changes, focused schema/trigger/SQL business tests and fresh seeding verification. Keep new baseline/designer/snapshot and design-time factory; remove all 69 old migration implementations, 62 designers and 10 obsolete partial guard helpers only after equivalent baseline SQL exists.

## Catalog comparison completeness

Existing schema inventory captures tables, SQL column type/length/precision/scale/nullability/identity/computed/default SQL, PK/AK column order, FK columns/principal/delete flags, index key/includes/descending/filter/type, checks, detailed trigger definitions/events/flags and view names. It compares table/column identity and type/nullability plus FK structure, but general indexes/checks are largely compared by name. Comparing JSON between old/new must include full definitions, not reuse name-only assertions.

Add or explicitly assess: column collation, identity seed/increment (identity flag alone is insufficient), persisted-computed flag, default-constraint semantics, FK UPDATE action/trust/not-for-replication, check trust/not-for-replication, index disabled/unique-constraint/clustering/options where material, view/function/procedure module definitions and ANSI_NULLS/QUOTED_IDENTIFIER, sequences/types, spatial index tessellation/SRID metadata, database collation/compatibility. Normalize catalog definition line endings and deterministic system-generated constraint naming only; preserve substantive SQL text. Trigger CREATE OR ALTER persists a different header; existing inventory has narrow header normalization, useful for semantic comparison.

Existing inventory traces SQL-only index and FK operations using narrow regexes and unique source line matching (`TraceSqlOnlyIndexes`, `TraceSqlOnlyForeignKeys`, lines 416,448). Replacing old migrations with a new baseline helper will require adapting source tracing; bulk bundled custom SQL breaks the current FK regex. `TraceInstalledDefinitionRewrite` understands exactly two REPLACE calls per known dynamic rewrite. The production baseline should contain final rendered SQL; historical rewrite parsing need not survive if abandoned history tests are removed.

## Fixture and tooling caveat

`SqlServerTestFixture` defaults `_environmentAccessor = _ => null` (line 45), so normal fixtures ignore ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING. Even explicit master accessor/connection is rejected by `IsOwnedMasterConnection` unless it equals this process SharedContainer connection (lines 299-310). Thus current inventory test requires fixture Testcontainers; a configured shared/CI SQL instance alone does not make that test runnable. Root fixture writer must address ownership deliberately if reusing a task-owned server. No fixture edits made in this audit.

## Active migration references (source evidence)

The companion JSON contains every source match in tests/tools/.github/docs/backend, excluding bin/obj. Some docs may be intentionally historical and should remain historical. Active test code with explicit legacy IDs or GetMigrations assumptions needs deletion only for obsolete migration-history risk, or replacement setup preserving current business risk. CI uses generic database update/has-pending-model-changes, no hardcoded migration ID found; wording additive migration snapshot can be updated for baseline.

- `tests/RoadGuardSystem.ApiTests/Defects/Huy01DefectConcurrencySchemaTests.cs:27` await baseline.InitializeAtMigrationAsync("20261003082408_Huy01SessionTransport");
- `tests/RoadGuardSystem.ApiTests/Projects/Anh01LegacyGeometryCorrectionTests.cs:22` await sql.InitializeAtMigrationAsync("20261002000100_Anh01M1FileSizeBigint");
- `tests/RoadGuardSystem.ApiTests/Projects/Anh01LegacyGeometryCorrectionTests.cs:38` (await db.Database.GetAppliedMigrationsAsync()).Should().Contain("20261002031518_Anh01GeometrySurveyReview");
- `tests/RoadGuardSystem.ApiTests/Reports/Huy01ReporterReportsApiTests.cs:964` var migration = assembly.CreateMigration(assembly.Migrations["20261003190000_Huy01TrainingLabels"], db.Database.ProviderName!);
- `tests/RoadGuardSystem.IntegrationTests/Defects/P232DetectionDefectSchemaTests.cs:236` const string testedMigration = "20260922034831_P232DetectionDefectTaskSchema";
- `tests/RoadGuardSystem.IntegrationTests/Defects/P232DetectionDefectSchemaTests.cs:240` await migrator.MigrateAsync("20260921182227_P231ProcessingAndOutboxDelivery");
- `tests/RoadGuardSystem.IntegrationTests/Files/Anh01FileMigrationTests.cs:24` const string baseline = "20260929125522_P2ValidationMeasurementProvenance";
- `tests/RoadGuardSystem.IntegrationTests/Files/Anh01FileMigrationTests.cs:25` const string widened = "20261002000100_Anh01M1FileSizeBigint";
- `tests/RoadGuardSystem.IntegrationTests/Files/FileRepositorySqlTests.cs:232` const string previous = "20260918185738_EnforceSecurityLogSafeCodes";
- `tests/RoadGuardSystem.IntegrationTests/Files/FileRepositorySqlTests.cs:237` await migrator.MigrateAsync("20260919085118_AddImmutableFileStorageBoundary");
- `tests/RoadGuardSystem.IntegrationTests/Files/FileRepositorySqlTests.cs:243` await migrator.MigrateAsync("20260919085118_AddImmutableFileStorageBoundary");
- `tests/RoadGuardSystem.IntegrationTests/Files/MultipartRecoveryMigrationTests.cs:28` const string baseline = "20261003160000_AnhHuyDependencyDefectConcurrency";
- `tests/RoadGuardSystem.IntegrationTests/Files/MultipartRecoveryMigrationTests.cs:29` const string testedMigration = "20261003170000_Anh01MultipartRecovery";
- `tests/RoadGuardSystem.IntegrationTests/Huy01/Huy01SharedSchemaTests.cs:38` await migrator.MigrateAsync("20261002120000_AnhHuySharedIntegration");
- `tests/RoadGuardSystem.IntegrationTests/Huy01/Huy01SharedSchemaTests.cs:39` await migrator.MigrateAsync("20261002100000_Anh01RequestScopeRootCorrection");
- `tests/RoadGuardSystem.IntegrationTests/Huy01/Huy01SharedSchemaTests.cs:40` (await db.Database.GetAppliedMigrationsAsync()).Should().NotContain("20261002120000_AnhHuySharedIntegration");
- `tests/RoadGuardSystem.IntegrationTests/Huy01/Huy01SharedSchemaTests.cs:41` (await db.Database.GetAppliedMigrationsAsync()).Should().Contain("20261002100000_Anh01RequestScopeRootCorrection");
- `tests/RoadGuardSystem.IntegrationTests/Huy01/Huy01SharedSchemaTests.cs:42` await migrator.MigrateAsync("20261002120000_AnhHuySharedIntegration");
- `tests/RoadGuardSystem.IntegrationTests/Huy01/Huy01SharedSchemaTests.cs:43` (await db.Database.GetAppliedMigrationsAsync()).Should().Contain("20261002120000_AnhHuySharedIntegration");
- `tests/RoadGuardSystem.IntegrationTests/Huy01/Huy01SharedSchemaTests.cs:63` await db.GetService<IMigrator>().MigrateAsync("20261002100000_Anh01RequestScopeRootCorrection");
- `tests/RoadGuardSystem.IntegrationTests/Huy01/Huy01SharedSchemaTests.cs:180` Func<Task> destructiveDown = () => db.GetService<IMigrator>().MigrateAsync("20261002100000_Anh01RequestScopeRootCorrection");
- `tests/RoadGuardSystem.IntegrationTests/Huy01/Huy01SharedSchemaTests.cs:182` (await db.Database.GetAppliedMigrationsAsync()).Should().Contain("20261002120000_AnhHuySharedIntegration");
- `tests/RoadGuardSystem.IntegrationTests/Identity/H1PersistentIdentitySqlTests.cs:70` await migrator.MigrateAsync("20261006015156_H0RetentionIntegration");
- `tests/RoadGuardSystem.IntegrationTests/Identity/H1PersistentIdentitySqlTests.cs:84` await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => migrator.MigrateAsync("20261006015156_H0RetentionIntegration"));
- `tests/RoadGuardSystem.IntegrationTests/Identity/IdentityPersistenceNegativeTests.cs:1361` await migrator.MigrateAsync("20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives");
- `tests/RoadGuardSystem.IntegrationTests/Identity/IdentityPersistenceNegativeTests.cs:1376` await migrator.MigrateAsync("20260918152126_AddIdentitySessionSecurityLogs");
- `tests/RoadGuardSystem.IntegrationTests/Identity/IdentityPersistenceNegativeTests.cs:1397` var actMigrate = () => migrator.MigrateAsync("20260918185738_EnforceSecurityLogSafeCodes");
- `tests/RoadGuardSystem.IntegrationTests/Identity/IdentityPersistenceNegativeTests.cs:1422` await migrator.MigrateAsync("20260918185738_EnforceSecurityLogSafeCodes");
- `tests/RoadGuardSystem.IntegrationTests/Identity/IdentityPersistenceNegativeTests.cs:1448` await migrator.MigrateAsync("20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives");
- `tests/RoadGuardSystem.IntegrationTests/Identity/IdentityPersistencePositiveTests.cs:596` await context.GetService<IMigrator>().MigrateAsync("20260918185738_EnforceSecurityLogSafeCodes");
- `tests/RoadGuardSystem.IntegrationTests/Identity/IdentityPersistencePositiveTests.cs:613` await migrator.MigrateAsync("20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives");
- `tests/RoadGuardSystem.IntegrationTests/Identity/IdentityPersistencePositiveTests.cs:640` await context.GetService<IMigrator>().MigrateAsync("20260918185738_EnforceSecurityLogSafeCodes");
- `tests/RoadGuardSystem.IntegrationTests/Infrastructure/Rf06aSchemaInventoryTests.cs:43` var source = new TriggerSource("TR_Test", "20260101000000_Create", "20260101000000_Create",
- `tests/RoadGuardSystem.IntegrationTests/Infrastructure/Rf06aSchemaInventoryTests.cs:44` "Migrations/20260101000000_Create.cs", 42, "CREATE TRIGGER [TR_Test]\nON [dbo].[Tests]\nAFTER UPDATE\nAS SELECT 1", []);
- `tests/RoadGuardSystem.IntegrationTests/Infrastructure/Rf06aSchemaInventoryTests.cs:95` var migrations = context.Database.GetMigrations().ToArray();
- `tests/RoadGuardSystem.IntegrationTests/Infrastructure/Rf09TransitionRehearsalTests.cs:24` const string widening = "20261002000100_Anh01M1FileSizeBigint";
- `tests/RoadGuardSystem.IntegrationTests/Infrastructure/Rf09TransitionRehearsalTests.cs:25` var migrations = context.Database.GetMigrations().ToArray();
- `tests/RoadGuardSystem.IntegrationTests/Infrastructure/Rf09TransitionRehearsalTests.cs:109` migrationCount = context.Database.GetMigrations().Count();
- `tests/RoadGuardSystem.IntegrationTests/Infrastructure/Rf09TransitionRehearsalTests.cs:110` Assert.Equal(context.Database.GetMigrations().ToArray(),
- `tests/RoadGuardSystem.IntegrationTests/Inspections/H3FieldInspectionWorkflowSqlTests.cs:63` await migration.MigrateAsync("20261006163740_H4SafetySourceAdmission");
- `tests/RoadGuardSystem.IntegrationTests/Inspections/H3FieldInspectionWorkflowSqlTests.cs:77` await Assert.ThrowsAsync<SqlException>(() => migration.MigrateAsync("20261006163740_H4SafetySourceAdmission"));
- `tests/RoadGuardSystem.IntegrationTests/Inspections/H3FieldMigrationTests.cs:23` var target = Assert.Single(db.Database.GetMigrations().Where(name =>
- `tests/RoadGuardSystem.IntegrationTests/Inspections/H3FieldMigrationTests.cs:45` await db.GetService<IMigrator>().MigrateAsync("20261006030147_H2NativeGeometryAndPavement");
- `tests/RoadGuardSystem.IntegrationTests/Inspections/H3FieldMigrationTests.cs:55` var target = Assert.Single(db.Database.GetMigrations().Where(name =>
- `tests/RoadGuardSystem.IntegrationTests/Inspections/H3FieldMigrationTests.cs:57` const string baseline = "20261006030147_H2NativeGeometryAndPavement";
- `tests/RoadGuardSystem.IntegrationTests/Notifications/H6NotificationPersistenceGuardSqlTests.cs:44` var error = await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>().MigrateAsync("20261006094905_H5OfflinePersistence"));
- `tests/RoadGuardSystem.IntegrationTests/Notifications/OwnerWeeklyRecoverySqlTests.cs:84` await migrator.MigrateAsync("20261007053131_OwnerClockDutyAppointments");
- `tests/RoadGuardSystem.IntegrationTests/Notifications/OwnerWeeklyRecoverySqlTests.cs:109` await Assert.ThrowsAnyAsync<Exception>(() => migrator.MigrateAsync("20261007053131_OwnerClockDutyAppointments"));
- `tests/RoadGuardSystem.IntegrationTests/Offline/H5OfflineMigrationTests.cs:23` private const string Previous = "20261006084919_H4RepairProducers";
- `tests/RoadGuardSystem.IntegrationTests/Persistence/P202MigrationLifecycleTests.cs:44` const string testedMigration = "20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives";
- `tests/RoadGuardSystem.IntegrationTests/Persistence/V2P1063MigrationUpgradeTests.cs:33` const string baseline = "20260928160534_AddRoadSegmentScopeReferences";
- `tests/RoadGuardSystem.IntegrationTests/Persistence/V2P1063MigrationUpgradeTests.cs:34` const string rowVersionMigration = "20260929113527_V2P1063FieldInspectionTaskRowVersion";
- `tests/RoadGuardSystem.IntegrationTests/Processing/P231ProcessingPersistenceTests.cs:159` const string testedMigration = "20260921182227_P231ProcessingAndOutboxDelivery";
- `tests/RoadGuardSystem.IntegrationTests/Processing/P231ProcessingPersistenceTests.cs:178` await migrator.MigrateAsync("20260921134719_AddP230FlightSurveyIdentityImmutability");
- `tests/RoadGuardSystem.IntegrationTests/Projects/H2NativeMigrationTests.cs:23` var discovered = db.Database.GetMigrations().ToArray();
- `tests/RoadGuardSystem.IntegrationTests/Projects/H2NativeMigrationTests.cs:29` const string baseline = "20261006021748_H1PersistentIdentityAndClocks";
- `tests/RoadGuardSystem.IntegrationTests/Projects/LD06LifecycleMigrationTests.cs:17` private const string Predecessor = "20261007060739_OwnerWeeklyReviewRecovery";
- `tests/RoadGuardSystem.IntegrationTests/Projects/P220ProjectMembershipSchemaTests.cs:483` const string testedMigration = "20260920081348_AddProjectMembershipSchema";
- `tests/RoadGuardSystem.IntegrationTests/Projects/P220ProjectMembershipSchemaTests.cs:486` await migrator.MigrateAsync("20260919085118_AddImmutableFileStorageBoundary");
- `tests/RoadGuardSystem.IntegrationTests/Projects/P221RoadWarrantySchemaTests.cs:153` await context.GetService<IMigrator>().MigrateAsync("20260920154542_AddRoadSectionVersionAndWarrantySchema");
- `tests/RoadGuardSystem.IntegrationTests/Projects/P221RoadWarrantySchemaTests.cs:157` await migrator.MigrateAsync("20260920140643_AddNotificationPersistenceBoundary");
- `tests/RoadGuardSystem.IntegrationTests/Projects/P221RoadWarrantySchemaTests.cs:160` await context.GetService<IMigrator>().MigrateAsync("20260920154542_AddRoadSectionVersionAndWarrantySchema");
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairCoreMigrationTests.cs:161` var downgrade = await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>().MigrateAsync("20261006035334_H3FieldLifecycleAndIntake")); Assert.Equal(51290, downgrade.Number);
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairCoreMigrationTests.cs:214` const string baseline = "20261006035334_H3FieldLifecycleAndIntake";
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairCoreMigrationTests.cs:215` var target = Assert.Single(db.Database.GetMigrations().Where(name => name.EndsWith("_H4RepairCore", StringComparison.Ordinal)));
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairProducerMigrationTests.cs:27` private const string Core = "20261006054054_H4RepairCore";
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4SafetyRuntimeSqlTests.cs:73` const string safetyMigration = "20261006163740_H4SafetySourceAdmission";
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4SafetyRuntimeSqlTests.cs:76` Assert.Equal(db.Database.GetMigrations(), applied);
- `tests/RoadGuardSystem.IntegrationTests/Repairs/LD07RoadCoverageSqlTests.cs:104` var downgrade = await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>().MigrateAsync("20261007115552_OwnerLifecycleActivation"));
- `tests/RoadGuardSystem.IntegrationTests/Repairs/LD07RoadCoverageSqlTests.cs:117` await db.GetService<IMigrator>().MigrateAsync("20261007115552_OwnerLifecycleActivation");
- `tests/RoadGuardSystem.IntegrationTests/Repairs/LD08DefectStatisticsSqlTests.cs:78` blocked = await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>().MigrateAsync("20261007131646_OwnerRoadCoverageActivation")); Assert.Equal(51890, blocked.Number);
- `tests/RoadGuardSystem.IntegrationTests/Repairs/LD08DefectStatisticsSqlTests.cs:111` await db.GetService<IMigrator>().MigrateAsync("20261007131646_OwnerRoadCoverageActivation"); await db.Database.MigrateAsync();
- `tests/RoadGuardSystem.IntegrationTests/Retention/H0RetentionMigrationTests.cs:19` private const string Baseline = "20261003220000_Huy01AiAttemptClosure";
- `tests/RoadGuardSystem.IntegrationTests/Retention/H0RetentionMigrationTests.cs:20` private const string Latest = "20261006015156_H0RetentionIntegration";
- `tests/RoadGuardSystem.IntegrationTests/Retention/H0RetentionMigrationTests.cs:30` var discovered = db.Database.GetMigrations().ToArray();
- `tests/RoadGuardSystem.IntegrationTests/Retention/H0RetentionMigrationTests.cs:31` Assert.Equal(1, discovered.Count(id => id == "20261003160000_AnhHuyDependencyDefectConcurrency"));
- `tests/RoadGuardSystem.IntegrationTests/Retention/H0RetentionMigrationTests.cs:33` Assert.Contains("20261003082408_Huy01SessionTransport", discovered);
- `tests/RoadGuardSystem.IntegrationTests/Retention/H0RetentionMigrationTests.cs:34` Assert.Contains("20261003180000_Huy01DefectSourceLinks", discovered);
- `tests/RoadGuardSystem.IntegrationTests/Retention/H0RetentionMigrationTests.cs:35` Assert.Contains("20261003190000_Huy01TrainingLabels", discovered);
- `tests/RoadGuardSystem.IntegrationTests/Retention/H0RetentionMigrationTests.cs:36` Assert.Contains("20261003200000_Huy01ExportConsumer", discovered);
- `tests/RoadGuardSystem.IntegrationTests/Retention/H0RetentionMigrationTests.cs:37` Assert.Contains("20261003210000_Huy01AiProducer", discovered);
- `tests/RoadGuardSystem.IntegrationTests/Retention/H0RetentionMigrationTests.cs:38` Assert.DoesNotContain("20261002151928_Anh02AiReportingExportRetention", discovered);
- `tests/RoadGuardSystem.IntegrationTests/Retention/H0RetentionMigrationTests.cs:39` Assert.DoesNotContain("20261003090000_Anh02AnalysisAttemptClosure", discovered);
- `tests/RoadGuardSystem.IntegrationTests/Surveys/Anh01ScopeAdoptionCorrectionTests.cs:27` await migrator.MigrateAsync("20261002000100_Anh01M1FileSizeBigint");
- `tests/RoadGuardSystem.IntegrationTests/Surveys/Anh01ScopeAdoptionCorrectionTests.cs:106` await migrator.MigrateAsync("20261002031518_Anh01GeometrySurveyReview");
- `tests/RoadGuardSystem.IntegrationTests/Surveys/Anh01ScopeAdoptionCorrectionTests.cs:112` await migrator.MigrateAsync("20261002090000_Anh01ScopeAdoptionCorrection");
- `tests/RoadGuardSystem.IntegrationTests/Surveys/Anh01ScopeAdoptionCorrectionTests.cs:134` var down = () => migrator.MigrateAsync("20261002031518_Anh01GeometrySurveyReview");
- `tests/RoadGuardSystem.IntegrationTests/Surveys/Anh01ScopeAdoptionCorrectionTests.cs:136` (await db.Database.GetAppliedMigrationsAsync()).Should().Contain("20261002090000_Anh01ScopeAdoptionCorrection");
- `tests/RoadGuardSystem.IntegrationTests/Surveys/Anh01ScopeAdoptionCorrectionTests.cs:137` (await db.Database.GetAppliedMigrationsAsync()).Should().Contain("20261002100000_Anh01RequestScopeRootCorrection");
- `tests/RoadGuardSystem.IntegrationTests/Surveys/P122SurveyPlanningPersistenceTests.cs:144` const string testedMigration = "20260921170153_P122CanonicalSurveyRequestStatus";
- `tests/RoadGuardSystem.IntegrationTests/Surveys/P122SurveyPlanningPersistenceTests.cs:146` await migrator.MigrateAsync("20260921134719_AddP230FlightSurveyIdentityImmutability");
- `tests/RoadGuardSystem.IntegrationTests/Surveys/P222SurveyPlanningSchemaTests.cs:175` const string testedMigration = "20260920172407_AddSurveyPlanningSchema";
- `tests/RoadGuardSystem.IntegrationTests/Surveys/P222SurveyPlanningSchemaTests.cs:178` await migrator.MigrateAsync("20260920154542_AddRoadSectionVersionAndWarrantySchema");
- `tests/RoadGuardSystem.IntegrationTests/Surveys/P223SurveyAssignmentSchemaTests.cs:384` const string testedMigration = "20260920182623_AddSurveyAssignmentSchema";
- `tests/RoadGuardSystem.IntegrationTests/Surveys/P223SurveyAssignmentSchemaTests.cs:389` await migrator.MigrateAsync("20260920172407_AddSurveyPlanningSchema");
- `tests/RoadGuardSystem.IntegrationTests/Surveys/P230DataVersionQualityCheckSchemaTests.cs:194` const string testedMigration = "20260921131520_AddP230DataVersionQualityCheckSchema";
- `tests/RoadGuardSystem.IntegrationTests/Surveys/P230DataVersionQualityCheckSchemaTests.cs:199` await migrator.MigrateAsync("20260921125553_AddP230FlightSurveyFileSchema");
- `tests/RoadGuardSystem.IntegrationTests/Surveys/P230FlightSurveyFileSchemaTests.cs:146` const string testedMigration = "20260921125553_AddP230FlightSurveyFileSchema";
- `tests/RoadGuardSystem.IntegrationTests/Surveys/P230FlightSurveyFileSchemaTests.cs:151` await migrator.MigrateAsync("20260920182623_AddSurveyAssignmentSchema");
- `tests/RoadGuardSystem.IntegrationTests/Surveys/P230SupplementarySurveyRequestSchemaTests.cs:81` const string testedMigration = "20260921131946_AddP230SupplementarySurveyRequestSchema";
- `tests/RoadGuardSystem.IntegrationTests/Surveys/P230SupplementarySurveyRequestSchemaTests.cs:86` await migrator.MigrateAsync("20260921131520_AddP230DataVersionQualityCheckSchema");
- `tests/RoadGuardSystem.UnitTests/Authentication/V2AuthenticationPersistenceContractTests.cs:24` context.Database.GetMigrations()
- `tests/RoadGuardSystem.UnitTests/Authentication/V2AuthenticationPersistenceContractTests.cs:25` .Should().Contain("20260927153000_AddPasswordRecoveryRequests");
- `docs/backend/data/current-data-dictionary.md:3` CURRENT_VERIFIED in isolated SQL after 37 migrations ending 20260929125522_P2ValidationMeasurementProvenance. Source: [inventory](current-schema.inventory.json), RoadGuardDbContext, EF configurations/entities and migration chain. This is not a production-schema claim.
- `docs/backend/data/current-data-dictionary.md:235` | [TR_PasswordResetLogs_AppendOnly](current-triggers.md#dbotr_passwordresetlogs_appendonly) | Observed rejection text: PasswordResetLogs are append-only; updates and deletions are forbidden. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260918152126_AddIdentitySessionSecurityLogs.cs#L261); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:287` | [TR_AccountStatusChangeLogs_AppendOnly](current-triggers.md#dbotr_accountstatuschangelogs_appendonly) | Observed rejection text: AccountStatusChangeLogs are append-only; updates and deletions are forbidden. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260918152126_AddIdentitySessionSecurityLogs.cs#L273); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:634` | [TR_RoadSectionVersions_Immutable](current-triggers.md#dbotr_roadsectionversions_immutable) | Observed rejection text: RoadSectionVersion records cannot be deleted.; RoadSectionVersion history is immutable; only IsCurrent may change. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260920154542_AddRoadSectionVersionAndWarrantySchema.cs#L164); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:867` | [TR_SurveyPlans_ScopeIntegrity](current-triggers.md#dbotr_surveyplans_scopeintegrity) | Observed rejection text: SurveyPlan road section must belong to its project. INSERT,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260920172407_AddSurveyPlanningSchema.cs#L160); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:948` | [TR_SurveyPlanPostponements_AppendOnly](current-triggers.md#dbotr_surveyplanpostponements_appendonly) | Observed rejection text: SurveyPlanPostponements are append-only. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260920172407_AddSurveyPlanningSchema.cs#L220); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:1008` | [TR_SurveyRequests_ScopeIntegrity](current-triggers.md#dbotr_surveyrequests_scopeintegrity) | Observed rejection text: SurveyRequest road section must belong to its project.; SurveyRequest source plan must match project, road section, and survey type. INSERT,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260920172407_AddSurveyPlanningSchema.cs#L183); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:1102` | [TR_Surveys_ScopeIntegrity](current-triggers.md#dbotr_surveys_scopeintegrity) | Observed rejection text: Survey road section version must belong to its project.; Survey request must match project, road section version, and survey type. INSERT,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260920182623_AddSurveyAssignmentSchema.cs#L147); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:1201` | [TR_Flights_ImmutableSurvey](current-triggers.md#dbotr_flights_immutablesurvey) | Observed rejection text: Flight survey identity is immutable. UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260921134719_AddP230FlightSurveyIdentityImmutability.cs#L15); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:1252` | [TR_SurveyFiles_ScopeIntegrity](current-triggers.md#dbotr_surveyfiles_scopeintegrity) | Observed rejection text: Survey file flight must belong to the same survey.; Survey file checksum must match the immutable stored file checksum. INSERT,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260921125553_AddP230FlightSurveyFileSchema.cs#L124); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:1303` | [TR_SurveyDataVersions_Immutable](current-triggers.md#dbotr_surveydataversions_immutable) | Observed rejection text: Survey dataset identity is immutable.; Confirmed or superseded survey dataset cannot regress status.; Confirmed or superseded survey dataset manifest is immutable. UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260921132735_AddP230ConfirmedDatasetImmutability.cs#L15); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:1502` | [TR_Files_Immutable](current-triggers.md#dbotr_files_immutable) | Observed rejection text: Files are immutable; create a new file identity and use the retention workflow for deletion. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260919085118_AddImmutableFileStorageBoundary.cs#L54); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:1680` | [TR_ProcessingBlocks_Immutable](current-triggers.md#dbotr_processingblocks_immutable) | Observed rejection text: ProcessingBlocks are immutable. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260921182227_P231ProcessingAndOutboxDelivery.cs#L241); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:1776` | [TR_ProcessingAttempts_AppendOnly](current-triggers.md#dbotr_processingattempts_appendonly) | Observed rejection text: ProcessingAttempts are append-only. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260921182227_P231ProcessingAndOutboxDelivery.cs#L253); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:2095` | [TR_AIDetections_Immutable](current-triggers.md#dbotr_aidetections_immutable) | Observed rejection text: AIDetections are immutable. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260922034831_P232DetectionDefectTaskSchema.cs#L360); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:2151` | [TR_DefectVerificationLogs_AppendOnly](current-triggers.md#dbotr_defectverificationlogs_appendonly) | Observed rejection text: DefectVerificationLogs are append-only. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260922034831_P232DetectionDefectTaskSchema.cs#L372); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:2326` | [TR_FieldInspectionSessions_Immutable](current-triggers.md#dbotr_fieldinspectionsessions_immutable) | Observed rejection text: Completed, imported, or locked field inspection sessions are immutable. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L295); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:2327` | [TR_FieldInspectionSessions_Integrity](current-triggers.md#dbotr_fieldinspectionsessions_integrity) | Observed rejection text: Field inspection session scope or active assignment is invalid. INSERT,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L260); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:2391` | [TR_GroundTruthMeasurements_Immutable](current-triggers.md#dbotr_groundtruthmeasurements_immutable) | Observed rejection text: Submitted ground truth measurements are immutable. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L351); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:2392` | [TR_GroundTruthMeasurements_Integrity](current-triggers.md#dbotr_groundtruthmeasurements_integrity) | Observed rejection text: Ground truth measurement purpose or scope is invalid. INSERT,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L315); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-data-dictionary.md:2544` | [TR_AuditLogs_AppendOnly](current-triggers.md#dbotr_auditlogs_appendonly) | Observed rejection text: AuditLogs are append-only; corrections require a new audit event. DELETE,UPDATE; disabled=False; [migration](../../../RoadGuardSystem.Repositories/Migrations/20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives.cs#L138); OUTER_WHITESPACE_ONLY |
- `docs/backend/data/current-triggers.md:9` - Created in 20260918152126_AddIdentitySessionSecurityLogs. Latest definition in 20260918152126_AddIdentitySessionSecurityLogs: [source](../../../RoadGuardSystem.Repositories/Migrations/20260918152126_AddIdentitySessionSecurityLogs.cs#L273). Ordered Up-operation history: 20260918152126_AddIdentitySessionSecurityLogs:CREATE.
- `docs/backend/data/current-triggers.md:28` - Created in 20260922034831_P232DetectionDefectTaskSchema. Latest definition in 20260922034831_P232DetectionDefectTaskSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260922034831_P232DetectionDefectTaskSchema.cs#L360). Ordered Up-operation history: 20260922034831_P232DetectionDefectTaskSchema:CREATE.
- `docs/backend/data/current-triggers.md:47` - Created in 20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives. Latest definition in 20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives: [source](../../../RoadGuardSystem.Repositories/Migrations/20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives.cs#L138). Ordered Up-operation history: 20260918065914_AddAuditOutboxIdempotencyConcurrencyPrimitives:CREATE.
- `docs/backend/data/current-triggers.md:66` - Created in 20260922034831_P232DetectionDefectTaskSchema. Latest definition in 20260922034831_P232DetectionDefectTaskSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260922034831_P232DetectionDefectTaskSchema.cs#L372). Ordered Up-operation history: 20260922034831_P232DetectionDefectTaskSchema:CREATE.
- `docs/backend/data/current-triggers.md:85` - Created in 20260922045838_P240FieldInspectionMeasurementSchema. Latest definition in 20260922045838_P240FieldInspectionMeasurementSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L295). Ordered Up-operation history: 20260922045838_P240FieldInspectionMeasurementSchema:CREATE.
- `docs/backend/data/current-triggers.md:112` - Created in 20260922045838_P240FieldInspectionMeasurementSchema. Latest definition in 20260922045838_P240FieldInspectionMeasurementSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L260). Ordered Up-operation history: 20260922045838_P240FieldInspectionMeasurementSchema:CREATE.
- `docs/backend/data/current-triggers.md:154` - Created in 20260919085118_AddImmutableFileStorageBoundary. Latest definition in 20260919085118_AddImmutableFileStorageBoundary: [source](../../../RoadGuardSystem.Repositories/Migrations/20260919085118_AddImmutableFileStorageBoundary.cs#L54). Ordered Up-operation history: 20260919085118_AddImmutableFileStorageBoundary:CREATE.
- `docs/backend/data/current-triggers.md:173` - Created in 20260921134719_AddP230FlightSurveyIdentityImmutability. Latest definition in 20260921134719_AddP230FlightSurveyIdentityImmutability: [source](../../../RoadGuardSystem.Repositories/Migrations/20260921134719_AddP230FlightSurveyIdentityImmutability.cs#L15). Ordered Up-operation history: 20260921134719_AddP230FlightSurveyIdentityImmutability:CREATE.
- `docs/backend/data/current-triggers.md:196` - Created in 20260922045838_P240FieldInspectionMeasurementSchema. Latest definition in 20260922045838_P240FieldInspectionMeasurementSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L351). Ordered Up-operation history: 20260922045838_P240FieldInspectionMeasurementSchema:CREATE.
- `docs/backend/data/current-triggers.md:225` - Created in 20260922045838_P240FieldInspectionMeasurementSchema. Latest definition in 20260922045838_P240FieldInspectionMeasurementSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260922045838_P240FieldInspectionMeasurementSchema.cs#L315). Ordered Up-operation history: 20260922045838_P240FieldInspectionMeasurementSchema:CREATE.
- `docs/backend/data/current-triggers.md:268` - Created in 20260918152126_AddIdentitySessionSecurityLogs. Latest definition in 20260918152126_AddIdentitySessionSecurityLogs: [source](../../../RoadGuardSystem.Repositories/Migrations/20260918152126_AddIdentitySessionSecurityLogs.cs#L261). Ordered Up-operation history: 20260918152126_AddIdentitySessionSecurityLogs:CREATE.
- `docs/backend/data/current-triggers.md:287` - Created in 20260921182227_P231ProcessingAndOutboxDelivery. Latest definition in 20260921182227_P231ProcessingAndOutboxDelivery: [source](../../../RoadGuardSystem.Repositories/Migrations/20260921182227_P231ProcessingAndOutboxDelivery.cs#L253). Ordered Up-operation history: 20260921182227_P231ProcessingAndOutboxDelivery:CREATE.
- `docs/backend/data/current-triggers.md:306` - Created in 20260921182227_P231ProcessingAndOutboxDelivery. Latest definition in 20260921182227_P231ProcessingAndOutboxDelivery: [source](../../../RoadGuardSystem.Repositories/Migrations/20260921182227_P231ProcessingAndOutboxDelivery.cs#L241). Ordered Up-operation history: 20260921182227_P231ProcessingAndOutboxDelivery:CREATE.
- `docs/backend/data/current-triggers.md:325` - Created in 20260920154542_AddRoadSectionVersionAndWarrantySchema. Latest definition in 20260920154542_AddRoadSectionVersionAndWarrantySchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260920154542_AddRoadSectionVersionAndWarrantySchema.cs#L164). Ordered Up-operation history: 20260920154542_AddRoadSectionVersionAndWarrantySchema:CREATE.
- `docs/backend/data/current-triggers.md:354` - Created in 20260921132735_AddP230ConfirmedDatasetImmutability. Latest definition in 20260921132735_AddP230ConfirmedDatasetImmutability: [source](../../../RoadGuardSystem.Repositories/Migrations/20260921132735_AddP230ConfirmedDatasetImmutability.cs#L15). Ordered Up-operation history: 20260921132735_AddP230ConfirmedDatasetImmutability:CREATE.
- `docs/backend/data/current-triggers.md:402` - Created in 20260921125553_AddP230FlightSurveyFileSchema. Latest definition in 20260921125553_AddP230FlightSurveyFileSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260921125553_AddP230FlightSurveyFileSchema.cs#L124). Ordered Up-operation history: 20260921125553_AddP230FlightSurveyFileSchema:CREATE.
- `docs/backend/data/current-triggers.md:445` - Created in 20260920172407_AddSurveyPlanningSchema. Latest definition in 20260920172407_AddSurveyPlanningSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260920172407_AddSurveyPlanningSchema.cs#L220). Ordered Up-operation history: 20260920172407_AddSurveyPlanningSchema:CREATE.
- `docs/backend/data/current-triggers.md:464` - Created in 20260920172407_AddSurveyPlanningSchema. Latest definition in 20260920172407_AddSurveyPlanningSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260920172407_AddSurveyPlanningSchema.cs#L160). Ordered Up-operation history: 20260920172407_AddSurveyPlanningSchema:CREATE.
- `docs/backend/data/current-triggers.md:494` - Created in 20260920172407_AddSurveyPlanningSchema. Latest definition in 20260920172407_AddSurveyPlanningSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260920172407_AddSurveyPlanningSchema.cs#L183). Ordered Up-operation history: 20260920172407_AddSurveyPlanningSchema:CREATE.
- `docs/backend/data/current-triggers.md:538` - Created in 20260920182623_AddSurveyAssignmentSchema. Latest definition in 20260920182623_AddSurveyAssignmentSchema: [source](../../../RoadGuardSystem.Repositories/Migrations/20260920182623_AddSurveyAssignmentSchema.cs#L147). Ordered Up-operation history: 20260920182623_AddSurveyAssignmentSchema:CREATE.
- `docs/backend/data/README.md:3` **CURRENT_VERIFIED for this checkout only:** local branch `anh`, HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`, with dirty source. The [inventory](current-schema.inventory.json) includes SHA-256 hashes for every surveyed BusinessObjects/Repositories `.cs` input, so HEAD alone is not its fingerprint. EF Core SQL Server 8.0.17 and Testcontainers SQL Server 2019-CU18 applied all 37 migrations to a fresh, owned database, ending at `20260929125522_P2ValidationMeasurementProvenance`. The SQL catalog was read after migration and the database/container disposed. This does **not** establish deployed or shared-environment schema.

## Removal and retention boundary

Guards helper files are partial classes tied only to old migrations; all ten are removable after final SQL bodies are installed by the baseline. They are not runtime business services. Retain RoadGuardDbContextDesignTimeFactory.cs, current configurations, DbContext SaveChanges invariant validation, RowVersionConvention and all runtime services. Historical docs/evidence packs remain untouched. Helpers in tests used only for historical upgrade/downgrade may be retired by root test-risk audit; do not remove fixtures/builders protecting current schema/business solely because their names mention P2/Huy/RF.

## Verification status

CURRENT_VERIFIED: source counts, hashes, declarations, model trigger metadata, references and existing inventory structure inspected. NOT_RUN: actual SQL old-chain capture, final custom-object catalog, baseline generation/application/equivalence, pending model-change command or behavior tests (read-only delegated scope). Historical catalog trigger definitions and per-trigger source evidence are included verbatim in the companion JSON with HISTORICAL trust boundary. Root should freshly capture them before production migration removal.


## Implementation checkpoint (supersedes initial read-only audit status)

CURRENT_VERIFIED: root expanded the assignment to exclusive writer of production migrations and Rf06a inventory tooling. Fresh extended old-chain capture passed (1/1, 33s) before old migrations were removed. The old source remains recoverable at fc70d4714921185d9b0d93e766835aed654102d2; no compiled historical migration archive is retained.

Retained production files: 20261008171739_BaselineCurrentSchema.cs, its designer, unchanged RoadGuardDbContextModelSnapshot.cs and unchanged RoadGuardDbContextDesignTimeFactory.cs. Deleted 69 historical migration implementations, 62 designers and 10 partial guard helpers. Removed temporary DiagnosticPendingChanges source and directory. The direct baseline uses generated table/index/constraint operations plus final catalog SQL for 189 triggers, five manual FKs, one filtered unique index and eleven model-omitted SQL defaults. No business/model/DbContext changes made.

The EF8 code generator drops direct Metadata.DeleteBehavior=Restrict on 14 owned relationships from serialized snapshot/designer. A diagnostic generation proved it then requested 14 Restrict FK changes despite unchanged runtime model. Snapshot restored exact original model serialization, designer updated to the same model body; no generated diagnostic retained. Baseline FK delete actions already match old catalog. This generator limitation is preserved as evidence, not resolved by changing business configuration.

Actual schema compare found 11 historical catalog defaults omitted from the current EF model: Defects.Severity, Defects.Status, H6NotificationCalendar.PlannedAtUtc, Notifications.OccurredAtUtc, OutboxMessages.DeliveryAttemptCount, OutboxMessages.DeliveryStatus, OutboxMessages.NextAttemptAtUtc, ProcessingJobs.ProjectId, SurveyPlans.OutputRequirements, SurveyRequests.DueAt and SurveyRequests.OutputRequirements. These defaults were restored directly in baseline SQL while the model remains unchanged. A Python default-encoding issue initially corrupted the Unicode square-meter check literal; corrected to original m² before passing schema comparison.

Baseline application + exact extended old/new business catalog comparison PASS: 5/5 (21s), baseline-equivalence.trx. Machine report schema-equivalence.json has differences=[] and excludes only EF migration-history metadata (69 old entries vs 1 new entry). Actual catalogs match 191 tables, 1,940 columns, 229 check rows, 722 FK column rows, 899 secondary index column rows, 269 PK/AK column rows, 189 trigger objects and zero identity columns/modules/sequences/spatial indexes/views. Trigger DDL headers and line endings are narrowly normalized; substantive SQL definitions preserved. Physical column ordering is not a business-schema difference and was not included in original inventory.

Pinned task-local EF Core 8.0.17 has-pending-model-changes returned exit0 / no changes after restoring owned relationship serialization. The existing FieldInspectionPurpose sentinel warning remains pre-existing; no new sentinel behavior added.

Inventory tooling now allows ROADGUARD_SCHEMA_INVENTORY_OUTPUT to write outside tracked docs and ROADGUARD_SCHEMA_COMPARE_BASELINE to enforce old/new equivalence, including full defaults/checks/index flags/trigger bodies. On comparison it writes a machine-readable .comparison.json report. A negative detector regression case covers default/check/FK action changes; added after the initial equivalence PASS and awaiting final root build window. Root owns fixture/CI and full-suite gates. No automatic commit/push/stage performed.


## Final schema verification and organization

CURRENT_VERIFIED: BaselineSchemaTests.cs and Rf06aSchemaInventoryTests.cs moved to tests/RoadGuardSystem.IntegrationTests/Migrations/Validation/. Namespaces and test identities remain RoadGuardSystem.IntegrationTests.Infrastructure; source-hash self path updated. No other active literal path consumer found. Inventory refreshed after move.

Final normal integration project analyzer build PASS (0 errors; 392 existing permitted warnings), schema-final-build.log. No RunAnalyzers=false or temporary excluded compile items in final build. Final focused real SQL checks PASS 6/6, 0 skipped, 19 seconds, baseline-equivalence-final.trx. Machine schema-equivalence-final.json reports CURRENT_VERIFIED_EQUIVALENT, differences=[], old69/new1. Final pinned EF8.0.17 pending model check exit0, schema-final-pending-model.log. Focused scope diff-check passed. Root owns full suite/lane acceptance and format gates.

Self-review pass1: verified final catalog/business schema preservation, manual objects, triggers/default/check text, no unintended foundational data and unchanged model/DbContext behavior. Resolved missing defaults and EF owned-FK serialization; tests rerun. Self-review pass2: inspected scope/shared writer, 4 retained migration-source files, no temporary diagnostic/historical compiled copies, preserved factory/snapshot, moved source consumers/test identities and evidence hashes. No remaining in-scope finding. External review not performed by this agent.
