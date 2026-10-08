using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

public sealed class Rf06aSchemaInventoryTests
{
    private static readonly JsonSerializerOptions InventoryJsonOptions = new() { WriteIndented = true };
    private static readonly Regex TriggerOperationPattern = new(
        @"(?im)^\s*(?<action>CREATE(?:\s+OR\s+ALTER)?|ALTER|DROP)\s+TRIGGER(?:\s+IF\s+EXISTS)?\s+(?:(?:\[dbo\]|dbo)\.)?(?:\[(?<name>TR_[^\]]+)\]|(?<name>TR_[A-Za-z0-9_{}]+))",
        RegexOptions.CultureInvariant);

    [Fact]
    public void SchemaShapeComparison_DetectsMissingTableColumnAndChangedForeignKey()
    {
        var expected = new SchemaShape(
            ["dbo.Projects", "dbo.RoadSections"],
            ["dbo.Projects.Id", "dbo.RoadSections.Id", "dbo.RoadSections.ProjectId"],
            ["dbo.RoadSections.ProjectId->dbo.Projects.Id"]);
        var observed = new SchemaShape(
            ["dbo.Projects"],
            ["dbo.Projects.Id", "dbo.RoadSections.Id"],
            ["dbo.RoadSections.ProjectId->dbo.Users.Id"]);

        Assert.Equal(new[] { "missing table dbo.RoadSections", "missing column dbo.RoadSections.ProjectId",
            "missing FK dbo.RoadSections.ProjectId->dbo.Projects.Id", "unexpected FK dbo.RoadSections.ProjectId->dbo.Users.Id" },
            CompareShape(expected, observed));
    }

    [Fact]
    public void TriggerEvidenceVerifier_ReportsMissingDefinitionAlteredDefinitionAndUnresolvedSource()
    {
        var source = new TriggerSource("TR_Test", "20260101000000_Create", "20260101000000_Create",
            "Migrations/20260101000000_Create.cs", 42, "CREATE TRIGGER [TR_Test]\nON [dbo].[Tests]\nAFTER UPDATE\nAS SELECT 1", []);
        var valid = new TriggerEvidence("dbo", "Tests", "TR_Test", source.Definition, source);
        Assert.Empty(VerifyTriggerEvidence([valid]));
        Assert.Equal("OUTER_WHITESPACE_ONLY", CompareDefinitionText($"\r\n  {source.Definition}\r\n", source.Definition));
        Assert.Equal(HashDefinition("A\r\nB"), HashDefinition("A\nB"));

        var missing = valid with { Definition = null };
        Assert.Equal<string>(["TR_Test: definition unavailable"], VerifyTriggerEvidence([missing]));

        var altered = valid with { Definition = source.Definition.Replace("SELECT 1", "SELECT 2", StringComparison.Ordinal) };
        Assert.Equal<string>(["TR_Test: catalog definition differs from latest migration SQL"], VerifyTriggerEvidence([altered]));

        var unmapped = valid with { Source = null };
        Assert.Equal<string>(["TR_Test: migration source unresolved"], VerifyTriggerEvidence([unmapped]));
        Assert.Equal<string>(["TR_Test: duplicate catalog trigger"], VerifyTriggerEvidence([valid, valid]));
    }

    [Fact]
    public void TriggerMigrationTrace_UsesLatestDefinitionAndPreservesDropRecreateHistory()
    {
        var active = new Dictionary<string, TriggerSource>(StringComparer.Ordinal);
        var history = new Dictionary<string, List<TriggerMigrationEvent>>(StringComparer.Ordinal);
        ApplyTriggerOperation(active, history, "TR_Test", "CREATE", "m1", "m1.cs", 10, "CREATE TRIGGER first");
        ApplyTriggerOperation(active, history, "TR_Test", "ALTER", "m2", "m2.cs", 20, "ALTER TRIGGER second");
        Assert.Equal("m1", active["TR_Test"].CreatedMigration);
        Assert.Equal("m2", active["TR_Test"].LastDefinitionMigration);
        Assert.Equal("ALTER TRIGGER second", active["TR_Test"].MigrationSql);
        Assert.Equal("CREATE TRIGGER second", active["TR_Test"].Definition);

        ApplyTriggerOperation(active, history, "TR_Test", "DROP", "m3", "m3.cs", 30, "DROP TRIGGER");
        Assert.False(active.ContainsKey("TR_Test"));
        ApplyTriggerOperation(active, history, "TR_Test", "CREATE", "m4", "m4.cs", 40, "CREATE TRIGGER replacement");
        Assert.Equal("m4", active["TR_Test"].CreatedMigration);
        Assert.Equal("m4", active["TR_Test"].LastDefinitionMigration);
        Assert.Equal(["CREATE", "ALTER", "DROP", "CREATE"], active["TR_Test"].History.Select(item => item.Action));
    }

    [Fact]
    public void CatalogComparisonRejectsChangedDefaultCheckAndForeignKeyDeleteAction()
    {
        using var expected = JsonDocument.Parse("""
            {"columns":[{"table":"Tasks","name":"Status","defaultSql":"((0))"}],
             "checks":[{"name":"CK_Status","definition":"([Status]>=(0))"}],
             "foreignKeys":[{"name":"FK_Project","onDelete":"NO_ACTION"}]}
            """);
        using var observed = JsonDocument.Parse("""
            {"columns":[{"table":"Tasks","name":"Status","defaultSql":"((1))"}],
             "checks":[{"name":"CK_Status","definition":"([Status]>(0))"}],
             "foreignKeys":[{"name":"FK_Project","onDelete":"CASCADE"}]}
            """);
        var differences = CompareCatalog(expected.RootElement, observed.RootElement);
        Assert.Equal(6, differences.Length);
        Assert.Contains(differences, difference => difference.StartsWith("missing columns:", StringComparison.Ordinal));
        Assert.Contains(differences, difference => difference.StartsWith("missing checks:", StringComparison.Ordinal));
        Assert.Contains(differences, difference => difference.StartsWith("missing foreignKeys:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MigratedSchema_MatchesModelAndSnapshot_AndWritesInventory()
    {
        var root = FindRepositoryRoot();
        var fixture = new SqlServerTestFixture(null, null, createSpatialProbeSchema: false);
        await fixture.InitializeAsync();
        try
        {
            var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
                .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite())
                .Options;
            await using var context = new RoadGuardDbContext(options);
            await context.Database.MigrateAsync();

            var migrations = context.Database.GetMigrations().ToArray();
            var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
            Assert.Equal(migrations, applied);

            var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot?.Model
                ?? throw new InvalidOperationException("Migration snapshot is missing.");
            var model = ExtractModel(context.GetService<IDesignTimeModel>().Model);
            var snapshotModel = ExtractModel(context.GetService<IModelRuntimeInitializer>().Initialize(snapshot, designTime: true));
            await using var connection = new SqlConnection(fixture.ConnectionString);
            await connection.OpenAsync();

            var sql = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["tables"] = await QueryJsonAsync(connection, """
                    SELECT s.name AS [schema], t.name AS [table]
                    FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
                    WHERE t.is_ms_shipped=0 AND t.name <> '__EFMigrationsHistory'
                    ORDER BY s.name,t.name FOR JSON PATH
                    """),
                ["columns"] = await QueryJsonAsync(connection, """
                    SELECT s.name AS [schema],t.name AS [table],c.name AS [name],
                      ty.name AS [type],c.max_length AS [maxLengthBytes],c.precision,c.scale,
                      c.is_nullable AS [nullable],c.is_identity AS [identity],c.is_computed AS [computed],
                      c.collation_name AS [collation], cc.is_persisted AS [persisted],
                      dc.definition AS [defaultSql],cc.definition AS [computedSql]
                    FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id
                    JOIN sys.schemas s ON s.schema_id=t.schema_id
                    JOIN sys.types ty ON ty.user_type_id=c.user_type_id
                    LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id
                    LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id
                    WHERE t.is_ms_shipped=0 AND t.name <> '__EFMigrationsHistory'
                    ORDER BY s.name,t.name,c.column_id FOR JSON PATH
                    """),
                ["keys"] = await QueryJsonAsync(connection, """
                    SELECT s.name AS [schema],t.name AS [table],kc.name,kc.type_desc AS [kind],
                      c.name AS [column],ic.key_ordinal AS [ordinal]
                    FROM sys.key_constraints kc JOIN sys.tables t ON t.object_id=kc.parent_object_id
                    JOIN sys.schemas s ON s.schema_id=t.schema_id
                    JOIN sys.index_columns ic ON ic.object_id=t.object_id AND ic.index_id=kc.unique_index_id
                    JOIN sys.columns c ON c.object_id=t.object_id AND c.column_id=ic.column_id
                    WHERE t.is_ms_shipped=0 AND t.name <> '__EFMigrationsHistory'
                    ORDER BY s.name,t.name,kc.name,ic.key_ordinal FOR JSON PATH
                    """),
                ["foreignKeys"] = await QueryJsonAsync(connection, """
                    SELECT ps.name AS [schema],pt.name AS [table],fk.name,
                      pc.name AS [column],fkc.constraint_column_id AS [ordinal],
                      rs.name AS [principalSchema],rt.name AS [principalTable],rc.name AS [principalColumn],
                      fk.delete_referential_action_desc AS [onDelete],fk.update_referential_action_desc AS [onUpdate],
                      fk.is_disabled AS [disabled],fk.is_not_trusted AS [notTrusted],fk.is_not_for_replication AS [notForReplication]
                    FROM sys.foreign_keys fk
                    JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id
                    JOIN sys.tables pt ON pt.object_id=fk.parent_object_id JOIN sys.schemas ps ON ps.schema_id=pt.schema_id
                    JOIN sys.columns pc ON pc.object_id=pt.object_id AND pc.column_id=fkc.parent_column_id
                    JOIN sys.tables rt ON rt.object_id=fk.referenced_object_id JOIN sys.schemas rs ON rs.schema_id=rt.schema_id
                    JOIN sys.columns rc ON rc.object_id=rt.object_id AND rc.column_id=fkc.referenced_column_id
                    ORDER BY ps.name,pt.name,fk.name,fkc.constraint_column_id FOR JSON PATH
                    """),
                ["indexes"] = await QueryJsonAsync(connection, """
                    SELECT s.name AS [schema],t.name AS [table],i.name,c.name AS [column],
                      ic.key_ordinal AS [ordinal],ic.is_included_column AS [included],ic.is_descending_key AS [descending],
                      i.is_unique AS [unique],i.has_filter AS [filtered],i.filter_definition AS [filter],i.type_desc AS [kind],
                      i.is_disabled AS [disabled],i.fill_factor AS [fillFactor],i.is_padded AS [padded],
                      i.allow_row_locks AS [allowRowLocks],i.allow_page_locks AS [allowPageLocks]
                    FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id JOIN sys.schemas s ON s.schema_id=t.schema_id
                    JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
                    JOIN sys.columns c ON c.object_id=i.object_id AND c.column_id=ic.column_id
                    WHERE t.is_ms_shipped=0 AND t.name <> '__EFMigrationsHistory' AND i.is_primary_key=0 AND i.is_unique_constraint=0
                    ORDER BY s.name,t.name,i.name,ic.is_included_column,ic.key_ordinal,ic.index_column_id FOR JSON PATH
                    """),
                ["checks"] = await QueryJsonAsync(connection, """
                    SELECT s.name AS [schema],t.name AS [table],cc.name,cc.definition,cc.is_disabled AS [disabled],
                      cc.is_not_trusted AS [notTrusted],cc.is_not_for_replication AS [notForReplication]
                    FROM sys.check_constraints cc JOIN sys.tables t ON t.object_id=cc.parent_object_id
                    JOIN sys.schemas s ON s.schema_id=t.schema_id
                    WHERE t.is_ms_shipped=0 AND t.name <> '__EFMigrationsHistory'
                    ORDER BY s.name,t.name,cc.name FOR JSON PATH
                    """),
                ["triggers"] = await QueryJsonAsync(connection, """
                    SELECT s.name AS [schema],t.name AS [table],tr.name,
                      tr.is_disabled AS [disabled],tr.is_instead_of_trigger AS [insteadOf],
                      tr.is_not_for_replication AS [notForReplication],sm.definition,
                      STUFF((SELECT ',' + te.type_desc FROM sys.trigger_events te
                        WHERE te.object_id=tr.object_id ORDER BY te.type_desc FOR XML PATH('')),1,1,'') AS [events]
                    FROM sys.triggers tr JOIN sys.tables t ON t.object_id=tr.parent_id
                    JOIN sys.schemas s ON s.schema_id=t.schema_id
                    LEFT JOIN sys.sql_modules sm ON sm.object_id=tr.object_id
                    WHERE tr.is_ms_shipped=0 ORDER BY s.name,t.name,tr.name FOR JSON PATH
                    """),
                ["views"] = await QueryJsonAsync(connection, """
                    SELECT s.name AS [schema],v.name FROM sys.views v JOIN sys.schemas s ON s.schema_id=v.schema_id
                    WHERE v.is_ms_shipped=0 ORDER BY s.name,v.name FOR JSON PATH
                    """)
            };

            sql["identityColumns"] = await QueryJsonAsync(connection, """
                SELECT s.name AS [schema],t.name AS [table],c.name AS [column],
                  CONVERT(nvarchar(100),c.seed_value) AS [seed],CONVERT(nvarchar(100),c.increment_value) AS [increment],
                  c.is_not_for_replication AS [notForReplication]
                FROM sys.identity_columns c JOIN sys.tables t ON t.object_id=c.object_id JOIN sys.schemas s ON s.schema_id=t.schema_id
                WHERE t.is_ms_shipped=0 AND t.name<>'__EFMigrationsHistory'
                ORDER BY s.name,t.name,c.name FOR JSON PATH
                """);
            sql["modules"] = await QueryJsonAsync(connection, """
                SELECT s.name AS [schema],o.name,o.type,sm.definition,sm.uses_ansi_nulls AS [ansiNulls],
                  sm.uses_quoted_identifier AS [quotedIdentifier],sm.is_schema_bound AS [schemaBound]
                FROM sys.objects o JOIN sys.schemas s ON s.schema_id=o.schema_id
                LEFT JOIN sys.sql_modules sm ON sm.object_id=o.object_id
                WHERE o.is_ms_shipped=0 AND o.type IN ('V','P','PC','FN','IF','TF','FS','FT')
                ORDER BY s.name,o.name FOR JSON PATH
                """);
            sql["sequences"] = await QueryJsonAsync(connection, """
                SELECT s.name AS [schema],q.name,TYPE_NAME(q.user_type_id) AS [type],
                  CONVERT(nvarchar(100),q.start_value) AS [start],CONVERT(nvarchar(100),q.increment) AS [increment],
                  CONVERT(nvarchar(100),q.minimum_value) AS [minimum],CONVERT(nvarchar(100),q.maximum_value) AS [maximum],
                  q.is_cycling AS [cycling],q.is_cached AS [cached],q.cache_size AS [cacheSize]
                FROM sys.sequences q JOIN sys.schemas s ON s.schema_id=q.schema_id ORDER BY s.name,q.name FOR JSON PATH
                """);
            sql["spatialIndexes"] = await QueryJsonAsync(connection, """
                SELECT s.name AS [schema],t.name AS [table],i.name,i.tessellation_scheme AS [scheme],
                  x.bounding_box_xmin AS [xmin],x.bounding_box_ymin AS [ymin],x.bounding_box_xmax AS [xmax],x.bounding_box_ymax AS [ymax],
                  x.level_1_grid AS [grid1],x.level_2_grid AS [grid2],x.level_3_grid AS [grid3],x.level_4_grid AS [grid4],x.cells_per_object AS [cells]
                FROM sys.spatial_indexes i JOIN sys.tables t ON t.object_id=i.object_id JOIN sys.schemas s ON s.schema_id=t.schema_id
                JOIN sys.spatial_index_tessellations x ON x.object_id=i.object_id AND x.index_id=i.index_id
                ORDER BY s.name,t.name,i.name FOR JSON PATH
                """);

            var sqlOnlyIndexes = TraceSqlOnlyIndexes(root, context.GetService<IMigrationsAssembly>(), migrations);
            var sqlOnlyForeignKeys = TraceSqlOnlyForeignKeys(root, context.GetService<IMigrationsAssembly>(), migrations);
            var migrationSources = TraceTriggerSources(root, context.GetService<IMigrationsAssembly>(), migrations);
            Assert.Equal(migrationSources.Keys.Order(StringComparer.Ordinal),
                sql["triggers"].EnumerateArray().Select(item => item.GetProperty("name").GetString()!)
                    .Order(StringComparer.Ordinal));
            var triggerEvidence = sql["triggers"].EnumerateArray().Select(trigger =>
            {
                var name = trigger.GetProperty("name").GetString()!;
                migrationSources.TryGetValue(name, out var source);
                return new TriggerEvidence(
                    trigger.GetProperty("schema").GetString()!, trigger.GetProperty("table").GetString()!, name,
                    trigger.TryGetProperty("definition", out var definition) ? definition.GetString() : null, source);
            }).ToArray();
            Assert.Empty(VerifyTriggerEvidence(triggerEvidence));

            var inputs = Directory.EnumerateFiles(Path.Combine(root, "RoadGuardSystem.Repositories"), "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(Path.Combine(root, "RoadGuardSystem.BusinessObjects"), "*.cs", SearchOption.AllDirectories))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                               !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => new { path = Path.GetRelativePath(root, path).Replace('\\', '/'), sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant() })
                .ToArray();
            var artifact = new
            {
                status = "CURRENT_VERIFIED_ISOLATED_SQL",
                provider = "Microsoft.EntityFrameworkCore.SqlServer 8.0.17",
                sqlImage = "mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04",
                lastMigration = migrations.LastOrDefault(),
                migrationCount = migrations.Length,
                inputHashes = inputs,
                toolHashes = new[]
                {
                    HashTool(root, "tests/RoadGuardSystem.IntegrationTests/Migrations/Validation/Rf06aSchemaInventoryTests.cs"),
                    HashTool(root, "tests/Tooling/Write-Rf06aSchemaDocs.ps1"),
                    HashTool(root, "tests/RoadGuardSystem.IntegrationTests/Infrastructure/SqlServerTestFixture.cs")
                },
                model,
                snapshot = snapshotModel,
                sql,
                triggerEvidence = triggerEvidence.Select(item => new
                {
                    item.Schema,
                    item.Table,
                    item.Name,
                    observedDefinitionSha256Utf8Lf = HashDefinition(item.Definition!),
                    createdMigration = item.Source!.CreatedMigration,
                    lastDefinitionMigration = item.Source.LastDefinitionMigration,
                    sourcePath = item.Source.Path,
                    sourceLine = item.Source.Line,
                    migrationSqlSha256Utf8Lf = HashDefinition(item.Source.MigrationSql ?? item.Source.Definition),
                    expectedCatalogDefinitionSha256Utf8Lf = HashDefinition(item.Source.Definition),
                    textComparison = CompareDefinitionText(item.Definition!, item.Source.Definition),
                    operationHistory = item.Source.History,
                    runtimeBehavior = "NOT_TESTED"
                }).ToArray(),
                sqlOnlyForeignKeys,
                sqlOnlyIndexes
            };
            var output = Environment.GetEnvironmentVariable("ROADGUARD_SCHEMA_INVENTORY_OUTPUT")
                ?? Path.Combine(root, "docs", "backend", "data", "current-schema.inventory.json");

            var modelTables = model.Select(item => item.TableKey).Order(StringComparer.Ordinal).ToArray();
            var snapshotTables = snapshotModel.Select(item => item.TableKey).Order(StringComparer.Ordinal).ToArray();
            var sqlTables = sql["tables"].EnumerateArray().Select(item => $"{item.GetProperty("schema").GetString()}.{item.GetProperty("table").GetString()}").Order(StringComparer.Ordinal).ToArray();
            Assert.Equal(modelTables, snapshotTables);
            Assert.Equal(modelTables, sqlTables);
            var modelColumns = model.SelectMany(item => item.Columns.Select(column => $"{item.TableKey}.{column.Name}")).Order(StringComparer.Ordinal).ToArray();
            var snapshotColumns = snapshotModel.SelectMany(item => item.Columns.Select(column => $"{item.TableKey}.{column.Name}")).Order(StringComparer.Ordinal).ToArray();
            var sqlColumns = sql["columns"].EnumerateArray().Select(item => $"{item.GetProperty("schema").GetString()}.{item.GetProperty("table").GetString()}.{item.GetProperty("name").GetString()}").Order(StringComparer.Ordinal).ToArray();
            Assert.Equal(modelColumns, snapshotColumns);
            Assert.Equal(modelColumns, sqlColumns);
            var snapshotByTable = snapshotModel.ToDictionary(item => item.TableKey, StringComparer.Ordinal);
            foreach (var table in model)
            {
                var snapshotTable = snapshotByTable[table.TableKey];
                Assert.Equal(table.Columns.Select(column => (column.Name, column.SqlType, column.Nullable)),
                    snapshotTable.Columns.Select(column => (column.Name, column.SqlType, column.Nullable)));
                Assert.Equal(table.ForeignKeys.Select(fk => (fk.Name, string.Join(',', fk.Columns), fk.PrincipalTable)),
                    snapshotTable.ForeignKeys.Select(fk => (fk.Name, string.Join(',', fk.Columns), fk.PrincipalTable)));
            }
            var modelColumnByName = model.SelectMany(table => table.Columns.Select(column =>
                    new KeyValuePair<string, ModelColumn>($"{table.TableKey}.{column.Name}", column)))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            foreach (var column in sql["columns"].EnumerateArray())
            {
                var key = $"{column.GetProperty("schema").GetString()}.{column.GetProperty("table").GetString()}.{column.GetProperty("name").GetString()}";
                var mapped = modelColumnByName[key];
                Assert.True(mapped.Nullable == column.GetProperty("nullable").GetBoolean(),
                    $"SQL nullability drift: {key}: model={mapped.Nullable}, catalog={column.GetProperty("nullable").GetBoolean()}");
                var sqlType = column.GetProperty("type").GetString()!;
                var modelType = mapped.SqlType ?? sqlType;
                Assert.True(modelType.StartsWith(sqlType, StringComparison.OrdinalIgnoreCase) ||
                            (modelType.Equals("rowversion", StringComparison.OrdinalIgnoreCase) && sqlType == "timestamp"),
                    $"SQL type drift: {key}: model={modelType}, catalog={sqlType}");
            }
            var mappedFks = model.SelectMany(item => item.ForeignKeys.Select(fk => $"{item.TableKey}.{fk.Name}")).Order(StringComparer.Ordinal).ToArray();
            var modelFks = mappedFks.Concat(sqlOnlyForeignKeys.Select(item => $"{item.TableKey}.{item.ForeignKey.Name}")).Order(StringComparer.Ordinal).ToArray();
            var snapshotFks = snapshotModel.SelectMany(item => item.ForeignKeys.Select(fk => $"{item.TableKey}.{fk.Name}")).Order(StringComparer.Ordinal).ToArray();
            var sqlFks = sql["foreignKeys"].EnumerateArray()
                .Select(item => $"{item.GetProperty("schema").GetString()}.{item.GetProperty("table").GetString()}.{item.GetProperty("name").GetString()}")
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            Assert.Equal(mappedFks, snapshotFks);
            Assert.True(modelFks.SequenceEqual(sqlFks, StringComparer.Ordinal),
                $"SQL FK drift: model-only=[{string.Join(';', modelFks.Except(sqlFks))}]; catalog-only=[{string.Join(';', sqlFks.Except(modelFks))}]");
            var modelShape = new SchemaShape(modelTables, modelColumns,
                model.SelectMany(table => table.ForeignKeys.Select(fk =>
                    $"{table.TableKey}.{string.Join(',', fk.Columns)}->{fk.PrincipalTable}.{string.Join(',', fk.PrincipalColumns)}"))
                    .Concat(sqlOnlyForeignKeys.Select(item => $"{item.TableKey}.{string.Join(',', item.ForeignKey.Columns)}->{item.ForeignKey.PrincipalTable}.{string.Join(',', item.ForeignKey.PrincipalColumns)}")).ToArray());
            var sqlShape = new SchemaShape(sqlTables, sqlColumns,
                sql["foreignKeys"].EnumerateArray().GroupBy(item =>
                    $"{item.GetProperty("schema").GetString()}.{item.GetProperty("table").GetString()}.{item.GetProperty("name").GetString()}")
                    .Select(group =>
                    {
                        var rows = group.OrderBy(item => item.GetProperty("ordinal").GetInt32()).ToArray();
                        return $"{rows[0].GetProperty("schema").GetString()}.{rows[0].GetProperty("table").GetString()}.{string.Join(',', rows.Select(item => item.GetProperty("column").GetString()))}->{rows[0].GetProperty("principalSchema").GetString()}.{rows[0].GetProperty("principalTable").GetString()}.{string.Join(',', rows.Select(item => item.GetProperty("principalColumn").GetString()))}";
                    }).ToArray());
            Assert.Empty(CompareShape(modelShape, sqlShape));
            var sqlFkGroups = sql["foreignKeys"].EnumerateArray().GroupBy(item =>
                $"{item.GetProperty("schema").GetString()}.{item.GetProperty("table").GetString()}.{item.GetProperty("name").GetString()}");
            var modelFkByName = model.SelectMany(table => table.ForeignKeys.Select(fk =>
                new KeyValuePair<string, ModelForeignKey>($"{table.TableKey}.{fk.Name}", fk)))
                .Concat(sqlOnlyForeignKeys.Select(item => new KeyValuePair<string, ModelForeignKey>($"{item.TableKey}.{item.ForeignKey.Name}", item.ForeignKey)))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            foreach (var group in sqlFkGroups)
            {
                var rows = group.OrderBy(item => item.GetProperty("ordinal").GetInt32()).ToArray();
                var mapped = modelFkByName[group.Key];
                Assert.Equal(mapped.Columns, rows.Select(item => item.GetProperty("column").GetString()));
                Assert.Equal(mapped.PrincipalColumns, rows.Select(item => item.GetProperty("principalColumn").GetString()));
                Assert.All(rows, row => Assert.False(row.GetProperty("disabled").GetBoolean()));
                var deleteAction = mapped.DeleteBehavior switch { "NoAction" or "Restrict" or "ClientSetNull" or "ClientCascade" or "ClientNoAction" => "NO_ACTION", "Cascade" => "CASCADE", "SetNull" => "SET_NULL", "SetDefault" => "SET_DEFAULT", _ => throw new InvalidOperationException($"Unsupported FK delete action: {mapped.DeleteBehavior}") };
                Assert.All(rows, row => Assert.Equal(deleteAction, row.GetProperty("onDelete").GetString()));
                Assert.Equal(mapped.PrincipalTable,
                    $"{rows[0].GetProperty("principalSchema").GetString()}.{rows[0].GetProperty("principalTable").GetString()}");
            }
            var modelIndexes = model.SelectMany(item => item.Indexes.Select(index => $"{item.TableKey}.{index}")).Order(StringComparer.Ordinal).ToArray();
            var snapshotIndexes = snapshotModel.SelectMany(item => item.Indexes.Select(index => $"{item.TableKey}.{index}")).Order(StringComparer.Ordinal).ToArray();
            var sqlIndexes = sql["indexes"].EnumerateArray()
                .Select(item => $"{item.GetProperty("schema").GetString()}.{item.GetProperty("table").GetString()}.{item.GetProperty("name").GetString()}")
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            Assert.Equal(modelIndexes, snapshotIndexes);
            var expectedIndexes = modelIndexes.Concat(sqlOnlyIndexes.Select(index => $"{index.TableKey}.{index.Name}")).Order(StringComparer.Ordinal).ToArray();
            Assert.Equal(expectedIndexes, sqlIndexes);
            foreach (var index in sqlOnlyIndexes)
            {
                var rows = sql["indexes"].EnumerateArray().Where(row => $"{row.GetProperty("schema").GetString()}.{row.GetProperty("table").GetString()}.{row.GetProperty("name").GetString()}" == $"{index.TableKey}.{index.Name}").OrderBy(row => row.GetProperty("ordinal").GetInt32()).ToArray();
                Assert.Equal(index.Columns, rows.Select(row => row.GetProperty("column").GetString()));
                Assert.Equal(Enumerable.Range(1, index.Columns.Length), rows.Select(row => row.GetProperty("ordinal").GetInt32()));
                Assert.All(rows, row =>
                {
                    Assert.True(row.GetProperty("unique").GetBoolean());
                    Assert.True(row.GetProperty("filtered").GetBoolean());
                    Assert.False(row.GetProperty("included").GetBoolean());
                    Assert.False(row.GetProperty("descending").GetBoolean());
                    Assert.Equal("NONCLUSTERED", row.GetProperty("kind").GetString());
                    Assert.Equal($"({index.Filter})", row.GetProperty("filter").GetString());
                });
            }
            var modelChecks = model.SelectMany(item => item.Checks.Select(name => $"{item.TableKey}.{name}")).Order(StringComparer.Ordinal).ToArray();
            var snapshotChecks = snapshotModel.SelectMany(item => item.Checks.Select(name => $"{item.TableKey}.{name}")).Order(StringComparer.Ordinal).ToArray();
            var sqlChecks = sql["checks"].EnumerateArray()
                .Select(item => $"{item.GetProperty("schema").GetString()}.{item.GetProperty("table").GetString()}.{item.GetProperty("name").GetString()}")
                .Order(StringComparer.Ordinal).ToArray();
            Assert.Equal(modelChecks, snapshotChecks);
            Assert.Equal(modelChecks, sqlChecks);
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            var json = JsonSerializer.Serialize(artifact, InventoryJsonOptions);
            await File.WriteAllTextAsync(output, json + "\n", Encoding.UTF8);
            var comparisonPath = Environment.GetEnvironmentVariable("ROADGUARD_SCHEMA_COMPARE_BASELINE");
            if (!string.IsNullOrWhiteSpace(comparisonPath))
            {
                using var expectedCatalog = JsonDocument.Parse(await File.ReadAllTextAsync(comparisonPath));
                using var observedCatalog = JsonDocument.Parse(json);
                var differences = CompareCatalog(expectedCatalog.RootElement.GetProperty("sql"), observedCatalog.RootElement.GetProperty("sql"));
                var comparison = new
                {
                    status = differences.Length == 0 ? "CURRENT_VERIFIED_EQUIVALENT" : "SCHEMA_DIFFERENCES",
                    expectedSource = comparisonPath,
                    expectedMigrationCount = expectedCatalog.RootElement.GetProperty("migrationCount").GetInt32(),
                    currentMigrationCount = migrations.Length,
                    expectedDifference = "Only __EFMigrationsHistory entries change; business catalog must be equivalent.",
                    differences
                };
                await File.WriteAllTextAsync(output + ".comparison.json",
                    JsonSerializer.Serialize(comparison, InventoryJsonOptions) + "\n", Encoding.UTF8);
                Assert.Empty(differences);
            }
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private static string[] CompareCatalog(JsonElement expected, JsonElement observed)
    {
        var differences = new List<string>();
        foreach (var group in expected.EnumerateObject())
        {
            if (!observed.TryGetProperty(group.Name, out var actual))
            {
                differences.Add($"missing catalog group {group.Name}");
                continue;
            }
            static string CanonicalRow(JsonElement row)
            {
                var values = new SortedDictionary<string, object?>(StringComparer.Ordinal);
                foreach (var property in row.EnumerateObject())
                {
                    if (property.Name == "definition" && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var definition = NormalizeLineEndings(property.Value.GetString()!).Trim();
                        values[property.Name] = Regex.Replace(definition, @"\ACREATE(?:\s+OR\s+ALTER)?\s+TRIGGER", "CREATE TRIGGER", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    }
                    else values[property.Name] = property.Value.Clone();
                }
                return JsonSerializer.Serialize(values);
            }
            var wanted = group.Value.EnumerateArray().Select(CanonicalRow).Order(StringComparer.Ordinal).ToArray();
            var found = actual.EnumerateArray().Select(CanonicalRow).Order(StringComparer.Ordinal).ToArray();
            differences.AddRange(wanted.Except(found, StringComparer.Ordinal).Select(row => $"missing {group.Name}: {row}"));
            differences.AddRange(found.Except(wanted, StringComparer.Ordinal).Select(row => $"unexpected {group.Name}: {row}"));
        }
        differences.AddRange(observed.EnumerateObject().Select(group => group.Name)
            .Except(expected.EnumerateObject().Select(group => group.Name), StringComparer.Ordinal)
            .Select(group => $"unexpected catalog group {group}"));
        return differences.ToArray();
    }

    private static ModelTable[] ExtractModel(IModel model)
    {
        var relationalModel = model.GetRelationalModel();
        return model.GetEntityTypes().Where(entity => entity.GetTableName() is not null)
            .GroupBy(entity => $"{entity.GetSchema() ?? "dbo"}.{entity.GetTableName()}")
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var entities = group.OrderBy(entity => entity.Name, StringComparer.Ordinal).ToArray();
                var store = StoreObjectIdentifier.Table(entities[0].GetTableName()!, entities[0].GetSchema());
                var columns = entities.SelectMany(entity => entity.GetProperties().Select(property => new ModelColumn(
                        property.GetColumnName(store) ?? property.Name,
                        entity.ClrType?.FullName ?? entity.Name,
                        property.Name,
                        FormatClrType(property.ClrType),
                        property.GetColumnType(),
                        property.IsColumnNullable(store),
                        property.GetMaxLength(),
                        property.GetPrecision(),
                        property.GetScale(),
                        property.GetDefaultValueSql(),
                        property.GetComputedColumnSql(),
                        property.ValueGenerated.ToString(),
                        property.IsConcurrencyToken,
                        property.IsShadowProperty(),
                        property.GetValueConverter()?.ProviderClrType.FullName,
                        GetEnumStoredValues(property))))
                    .GroupBy(column => column.Name).Select(grouping => grouping.First())
                    .OrderBy(column => column.Name, StringComparer.Ordinal).ToArray();
                var physicalTable = relationalModel.FindTable(entities[0].GetTableName()!, entities[0].GetSchema())
                    ?? throw new InvalidOperationException($"Relational table missing: {group.Key}");
                var foreignKeys = physicalTable.ForeignKeyConstraints.Select(fk =>
                    new ModelForeignKey(fk.Name, fk.Columns.Select(column => column.Name).ToArray(),
                        $"{fk.PrincipalTable.Schema ?? "dbo"}.{fk.PrincipalTable.Name}",
                        fk.PrincipalColumns.Select(column => column.Name).ToArray(),
                        fk.Columns.All(column => !column.IsNullable), fk.OnDeleteAction.ToString()))
                    .OrderBy(fk => fk.Name, StringComparer.Ordinal).ToArray();
                var indexes = physicalTable.Indexes.Select(index => index.Name).Order(StringComparer.Ordinal).ToArray();
                var checks = physicalTable.CheckConstraints.Select(check => check.Name
                    ?? throw new InvalidOperationException($"Relational check constraint name missing: {group.Key}"))
                    .Order(StringComparer.Ordinal).ToArray();
                return new ModelTable(group.Key, entities.Select(entity => entity.ClrType?.FullName ?? entity.Name).ToArray(),
                    entities.Select(entity => entity.GetQueryFilter()?.ToString()).Where(value => value is not null).ToArray(),
                    columns, foreignKeys, indexes, checks);
            }).ToArray();
    }

    private static async Task<JsonElement> QueryJsonAsync(SqlConnection connection, string query)
    {
        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var buffer = new StringBuilder();
        while (await reader.ReadAsync()) buffer.Append(reader.GetString(0));
        using var document = JsonDocument.Parse(buffer.Length == 0 ? "[]" : buffer.ToString());
        return document.RootElement.Clone();
    }

    private static SqlOnlyIndex[] TraceSqlOnlyIndexes(string root, IMigrationsAssembly assembly, IEnumerable<string> appliedMigrations)
    {
        var result = new List<SqlOnlyIndex>();
        var pattern = new Regex(@"\ACREATE UNIQUE INDEX \[(?<name>[A-Za-z0-9_]+)\] ON \[(?<table>[A-Za-z0-9_]+)\] \((?<columns>\[[A-Za-z0-9_]+\](?:,\[[A-Za-z0-9_]+\])*)\) WHERE (?<filter>\[[A-Za-z0-9_]+\] IS NULL);\s*\z", RegexOptions.CultureInvariant);
        foreach (var migrationId in appliedMigrations)
        {
            var migration = assembly.CreateMigration(assembly.Migrations[migrationId], "Microsoft.EntityFrameworkCore.SqlServer");
            foreach (var operation in migration.UpOperations.OfType<SqlOperation>())
            {
                if (!Regex.IsMatch(operation.Sql, @"\ACREATE (?:UNIQUE )?INDEX", RegexOptions.CultureInvariant)) continue;
                var match = pattern.Match(operation.Sql);
                if (!match.Success) throw new InvalidOperationException($"Unsupported SQL index operation: {migrationId}");
                var name = match.Groups["name"].Value;
                var candidates = new List<(string Path, int Line)>();
                foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "RoadGuardSystem.Repositories/Migrations"), "*.cs").Where(path => !path.EndsWith(".Designer.cs", StringComparison.Ordinal)))
                {
                    var text = File.ReadAllText(path);
                    if (!Regex.IsMatch(text, $@"\bclass\s+{Regex.Escape(migration.GetType().Name)}\b", RegexOptions.CultureInvariant)) continue;
                    var lines = File.ReadAllLines(path);
                    for (var line = 0; line < lines.Length; line++)
                        if (lines[line].Contains("CREATE UNIQUE INDEX", StringComparison.Ordinal) && lines[line].Contains(name, StringComparison.Ordinal))
                            candidates.Add((Path.GetRelativePath(root, path).Replace('\\', '/'), line + 1));
                }
                if (candidates.Count != 1) throw new InvalidOperationException($"SQL index source must resolve uniquely: {migrationId}:{name}");
                result.Add(new SqlOnlyIndex($"dbo.{match.Groups["table"].Value}", name, match.Groups["columns"].Value.Split(',').Select(column => column.Trim('[', ']')).ToArray(),
                    match.Groups["filter"].Value, migrationId, candidates[0].Path, candidates[0].Line, HashDefinition(operation.Sql)));
            }
        }
        if (result.Select(index => $"{index.TableKey}.{index.Name}").Distinct(StringComparer.Ordinal).Count() != result.Count) throw new InvalidOperationException("Duplicate SQL-only index source");
        return result.ToArray();
    }

    private static SqlOnlyForeignKey[] TraceSqlOnlyForeignKeys(string root, IMigrationsAssembly assembly,
        IEnumerable<string> appliedMigrations)
    {
        var result = new List<SqlOnlyForeignKey>();
        var pattern = new Regex(@"\AALTER TABLE \[(?<table>[A-Za-z0-9_]+)\] ADD CONSTRAINT \[(?<name>FK_[A-Za-z0-9_]+)\] FOREIGN KEY \((?<columns>[^)]+)\) REFERENCES \[(?<principal>[A-Za-z0-9_]+)\] \((?<principalColumns>[^)]+)\);\s*\z", RegexOptions.CultureInvariant);
        foreach (var migrationId in appliedMigrations)
        {
            var migration = assembly.CreateMigration(assembly.Migrations[migrationId], "Microsoft.EntityFrameworkCore.SqlServer");
            foreach (var operation in migration.UpOperations.OfType<SqlOperation>())
            {
                if (!operation.Sql.Contains("FOREIGN KEY", StringComparison.Ordinal)) continue;
                var match = pattern.Match(operation.Sql);
                if (!match.Success) throw new InvalidOperationException($"Unsupported SQL foreign key operation: {migrationId}");
                var name = match.Groups["name"].Value;
                var path = $"RoadGuardSystem.Repositories/Migrations/{migrationId}.cs";
                var lines = File.ReadAllLines(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
                var locations = lines.Select((line, index) => (line, index)).Where(item => item.line.Contains(name, StringComparison.Ordinal) && item.line.Contains("FOREIGN KEY", StringComparison.Ordinal)).ToArray();
                if (locations.Length != 1) throw new InvalidOperationException($"SQL foreign key source must resolve uniquely: {migrationId}:{name}");
                static string[] Columns(string value) => value.Split(',').Select(column => column.Trim().Trim('[', ']')).ToArray();
                result.Add(new SqlOnlyForeignKey($"dbo.{match.Groups["table"].Value}",
                    new ModelForeignKey(name, Columns(match.Groups["columns"].Value), $"dbo.{match.Groups["principal"].Value}", Columns(match.Groups["principalColumns"].Value), false, "NoAction"),
                    migrationId, path, locations[0].index + 1, HashDefinition(operation.Sql)));
            }
        }
        if (result.Select(item => $"{item.TableKey}.{item.ForeignKey.Name}").Distinct(StringComparer.Ordinal).Count() != result.Count)
            throw new InvalidOperationException("Duplicate SQL-only foreign key source");
        return result.ToArray();
    }

    private static Dictionary<string, TriggerSource> TraceTriggerSources(string root, IMigrationsAssembly assembly,
        IEnumerable<string> appliedMigrations)
    {
        var active = new Dictionary<string, TriggerSource>(StringComparer.Ordinal);
        var history = new Dictionary<string, List<TriggerMigrationEvent>>(StringComparer.Ordinal);
        foreach (var migrationId in appliedMigrations)
        {
            var migration = assembly.CreateMigration(assembly.Migrations[migrationId], "Microsoft.EntityFrameworkCore.SqlServer");
            var path = $"RoadGuardSystem.Repositories/Migrations/{migrationId}.cs";
            var fullPath = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath)) throw new InvalidOperationException($"Migration source missing: {path}");
            foreach (var operation in migration.UpOperations.OfType<SqlOperation>())
            {
                TraceInstalledDefinitionRewrite(root, fullPath, migration.GetType().Name, migrationId, operation.Sql, active, history);
                foreach (Match match in TriggerOperationPattern.Matches(operation.Sql))
                {
                    var name = match.Groups["name"].Value;
                    var action = match.Groups["action"].Value.ToUpperInvariant();
                    var source = FindTriggerSource(root, fullPath, migration.GetType().Name, name, action);
                    ApplyTriggerOperation(active, history, name, action, migrationId, source.Path, source.Line, operation.Sql);
                }
            }
        }
        return active;
    }

    private static void ApplyTriggerOperation(Dictionary<string, TriggerSource> active,
        Dictionary<string, List<TriggerMigrationEvent>> history, string name, string action,
        string migrationId, string path, int line, string sql)
    {
        if (!history.TryGetValue(name, out var events)) history[name] = events = [];
        events.Add(new TriggerMigrationEvent(migrationId, action, path, line));
        if (action == "DROP")
        {
            active.Remove(name);
        }
        else if (action.StartsWith("CREATE", StringComparison.Ordinal))
        {
            var created = active.TryGetValue(name, out var previous) ? previous.CreatedMigration : migrationId;
            active[name] = new TriggerSource(name, created, migrationId, path, line, CatalogDefinition(sql), events.ToArray(), sql);
        }
        else if (active.TryGetValue(name, out var existing))
        {
            active[name] = existing with
            {
                LastDefinitionMigration = migrationId,
                Path = path,
                Line = line,
                Definition = CatalogDefinition(sql),
                MigrationSql = sql,
                History = events.ToArray()
            };
        }
        else
        {
            throw new InvalidOperationException($"ALTER TRIGGER without known CREATE: {name} in {migrationId}");
        }
    }

    // SQL Server persists CREATE OR ALTER as CREATE followed by three spaces,
    // and ALTER as CREATE. Derive only that DDL header from the emitted operation;
    // compare every remaining character and hash the original operation separately.
    private static string CatalogDefinition(string sql)
        => Regex.Replace(sql, @"\A(?<leading>\s*)(?:CREATE OR ALTER|ALTER)(?=\s+TRIGGER)",
            match => match.Groups["leading"].Value + (match.Value.TrimStart().StartsWith("CREATE", StringComparison.Ordinal) ? "CREATE  " : "CREATE"),
            RegexOptions.CultureInvariant);

    private static void TraceInstalledDefinitionRewrite(string root, string primaryPath, string migrationClass,
        string migrationId, string sql, Dictionary<string, TriggerSource> active,
        Dictionary<string, List<TriggerMigrationEvent>> history)
    {
        var declaration = Regex.Match(sql,
            @"DECLARE (?<variable>@[A-Za-z0-9_]+) nvarchar\(max\)=OBJECT_DEFINITION\(OBJECT_ID\(N'\[dbo\]\.\[(?<name>TR_[A-Za-z0-9_]+)\]'\)\);",
            RegexOptions.CultureInvariant);
        if (!declaration.Success) return;
        var variable = declaration.Groups["variable"].Value;
        var name = declaration.Groups["name"].Value;
        if (!active.TryGetValue(name, out var previous))
            throw new InvalidOperationException($"Installed trigger rewrite has no prior definition: {migrationId}:{name}");
        var escapedVariable = Regex.Escape(variable);
        var replacements = Regex.Matches(sql,
            $@"SET {escapedVariable}=REPLACE\({escapedVariable},N'(?<from>(?:''|[^'])*)',N'(?<to>(?:''|[^'])*)'\);",
            RegexOptions.CultureInvariant);
        if (replacements.Count != 2 || !sql.Contains($"EXEC sys.sp_executesql {variable};", StringComparison.Ordinal) ||
            !sql.Contains($"IF LEFT({variable},6)=N'CREATE' SET {variable}=STUFF({variable},1,6,N'ALTER');", StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported installed trigger rewrite: {migrationId}:{name}");
        var definition = previous.Definition;
        for (var index = 0; index < replacements.Count; index++)
        {
            var from = replacements[index].Groups["from"].Value.Replace("''", "'", StringComparison.Ordinal);
            var to = replacements[index].Groups["to"].Value.Replace("''", "'", StringComparison.Ordinal);
            if (index == 0 && !definition.Contains(from, StringComparison.Ordinal))
                throw new InvalidOperationException($"Installed trigger rewrite source precondition failed: {migrationId}:{name}");
            definition = definition.Replace(from, to, StringComparison.Ordinal);
        }
        if (definition.StartsWith("CREATE", StringComparison.Ordinal)) definition = "ALTER" + definition[6..];
        var source = FindInstalledRewriteSource(root, primaryPath, migrationClass, name);
        ApplyTriggerOperation(active, history, name, "ALTER", migrationId, source.Path, source.Line, definition);
        active[name] = active[name] with { MigrationSql = sql };
    }

    private static (string Path, int Line) FindInstalledRewriteSource(string root, string primaryPath,
        string migrationClass, string name)
    {
        var candidates = new List<(string Path, int Line)>();
        foreach (var path in Directory.EnumerateFiles(Path.GetDirectoryName(primaryPath)!, "*.cs")
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.Ordinal)))
        {
            var text = File.ReadAllText(path);
            if (!Regex.IsMatch(text, $@"\bclass\s+{Regex.Escape(migrationClass)}\b", RegexOptions.CultureInvariant)) continue;
            var lines = File.ReadAllLines(path);
            for (var index = 0; index < lines.Length; index++)
                if (lines[index].Contains("OBJECT_DEFINITION", StringComparison.Ordinal) && lines[index].Contains(name, StringComparison.Ordinal))
                    candidates.Add((Path.GetRelativePath(root, path).Replace('\\', '/'), index + 1));
        }
        return candidates.Count == 1 ? candidates[0] : throw new InvalidOperationException(
            $"Installed trigger rewrite source must resolve uniquely: {Path.GetFileName(primaryPath)}:{name} ({candidates.Count} candidates)");
    }

    private static (string Path, int Line) FindTriggerSource(string root, string primaryPath,
        string migrationClass, string name, string action)
    {
        var candidates = new List<(string Path, int Line)>();
        var declaration = new Regex($@"\bclass\s+{Regex.Escape(migrationClass)}\b", RegexOptions.CultureInvariant);
        foreach (var path in Directory.EnumerateFiles(Path.GetDirectoryName(primaryPath)!, "*.cs")
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.Ordinal)))
        {
            var text = File.ReadAllText(path);
            if (!declaration.IsMatch(text)) continue;
            var lines = File.ReadAllLines(path);
            var down = Array.FindIndex(lines, line => line.Contains("protected override void Down", StringComparison.Ordinal));
            var upLength = down < 0 ? lines.Length : down;
            for (var index = 0; index < upLength; index++)
            {
                var match = TriggerOperationPattern.Match(Regex.Replace(lines[index], "^.*?(?=(?:CREATE(?: OR ALTER)?|ALTER|DROP) TRIGGER)", "", RegexOptions.IgnoreCase));
                if (!match.Success || !match.Groups["action"].Value.Equals(action, StringComparison.OrdinalIgnoreCase)) continue;
                var template = match.Groups["name"].Value;
                // UpOperations supplies the exact generated SQL; source lookup also supports
                // the named interpolation templates and partial helpers which produce it.
                var pattern = "^" + string.Join("[A-Za-z0-9_]+",
                    Regex.Split(template, @"\{[A-Za-z_][A-Za-z0-9_]*\}").Select(Regex.Escape)) + "$";
                if (Regex.IsMatch(name, pattern, RegexOptions.CultureInvariant))
                    candidates.Add((Path.GetRelativePath(root, path).Replace('\\', '/'), index + 1));
            }
        }
        return candidates.Count == 1 ? candidates[0] : throw new InvalidOperationException(
            $"Trigger operation source must resolve uniquely: {Path.GetFileName(primaryPath)}:{name} ({candidates.Count} candidates)");
    }

    private static string[] VerifyTriggerEvidence(IEnumerable<TriggerEvidence> evidence)
    {
        var items = evidence.ToArray();
        var issues = new List<string>();
        foreach (var group in items.GroupBy(item => item.Name, StringComparer.Ordinal))
            if (group.Count() != 1) issues.Add($"{group.Key}: duplicate catalog trigger");
        foreach (var item in items.DistinctBy(item => item.Name, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(item.Definition)) issues.Add($"{item.Name}: definition unavailable");
            if (item.Source is null) issues.Add($"{item.Name}: migration source unresolved");
            if (!string.IsNullOrWhiteSpace(item.Definition) && item.Source is not null &&
                CompareDefinitionText(item.Definition, item.Source.Definition) == "DIFFERENT")
                issues.Add($"{item.Name}: catalog definition differs from latest migration SQL");
        }
        return issues.ToArray();
    }

    private static string HashDefinition(string definition)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(NormalizeLineEndings(definition)))).ToLowerInvariant();

    private static string CompareDefinitionText(string observed, string source)
    {
        var catalogText = NormalizeLineEndings(observed);
        var sourceText = NormalizeLineEndings(source);
        if (catalogText == sourceText) return "EXACT_AFTER_LINE_ENDING_NORMALIZATION";
        if (catalogText.Trim() == sourceText.Trim()) return "OUTER_WHITESPACE_ONLY";
        return "DIFFERENT";
    }

    private static string NormalizeLineEndings(string definition)
        => definition.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static object HashTool(string root, string path) => new
    {
        path,
        sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(
            Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))))).ToLowerInvariant()
    };

    private static string? GetEnumStoredValues(IProperty property)
    {
        var enumType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        if (!enumType.IsEnum) return null;

        var converter = property.GetValueConverter();
        return string.Join(", ", Enum.GetValues(enumType).Cast<object>().Select(value =>
        {
            try
            {
                return $"{value}={converter?.ConvertToProvider(value) ?? Convert.ToInt64(value)}";
            }
            catch (ArgumentException)
            {
                return $"{value}=REJECTED_BY_CONVERTER";
            }
        }));
    }

    private static string FormatClrType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null) return $"{FormatClrType(underlying)}?";
        if (type.IsArray) return $"{FormatClrType(type.GetElementType()!)}[]";
        return type.IsEnum ? type.FullName ?? type.Name : type.Name;
    }

    private static string[] CompareShape(SchemaShape expected, SchemaShape observed)
    {
        var differences = new List<string>();
        Compare("table", expected.Tables, observed.Tables, differences);
        Compare("column", expected.Columns, observed.Columns, differences);
        Compare("FK", expected.ForeignKeys, observed.ForeignKeys, differences);
        return differences.ToArray();
    }

    private static void Compare(string kind, IEnumerable<string> expected, IEnumerable<string> observed, List<string> differences)
    {
        var expectedSet = expected.ToHashSet(StringComparer.Ordinal);
        var observedSet = observed.ToHashSet(StringComparer.Ordinal);
        differences.AddRange(expectedSet.Except(observedSet).Order(StringComparer.Ordinal).Select(value => $"missing {kind} {value}"));
        differences.AddRange(observedSet.Except(expectedSet).Order(StringComparer.Ordinal).Select(value => $"unexpected {kind} {value}"));
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RoadGuardSystem.slnx"))) return directory.FullName;
        }
        throw new InvalidOperationException("Repository root not found.");
    }

    private sealed record ModelTable(string TableKey, string[] Entities, string?[] QueryFilters, ModelColumn[] Columns,
        ModelForeignKey[] ForeignKeys, string[] Indexes, string[] Checks);
    private sealed record ModelForeignKey(string Name, string[] Columns, string PrincipalTable, string[] PrincipalColumns,
        bool Required, string DeleteBehavior);
    private sealed record SqlOnlyIndex(string TableKey, string Name, string[] Columns, string Filter, string MigrationId, string Path, int Line, string MigrationSqlSha256Utf8Lf);
    private sealed record SqlOnlyForeignKey(string TableKey, ModelForeignKey ForeignKey, string MigrationId, string Path, int Line, string MigrationSqlSha256Utf8Lf);
    private sealed record SchemaShape(string[] Tables, string[] Columns, string[] ForeignKeys);
    private sealed record TriggerEvidence(string Schema, string Table, string Name, string? Definition, TriggerSource? Source);
    private sealed record TriggerSource(string Name, string CreatedMigration, string LastDefinitionMigration,
        string Path, int Line, string Definition, TriggerMigrationEvent[] History, string? MigrationSql = null);
    private sealed record TriggerMigrationEvent(string MigrationId, string Action, string Path, int Line);
    private sealed record ModelColumn(string Name, string Entity, string Property, string ClrType, string? SqlType,
        bool Nullable, int? MaxLength, int? Precision, int? Scale, string? DefaultSql, string? ComputedSql,
        string ValueGenerated, bool ConcurrencyToken, bool Shadow, string? ConverterProviderType, string? EnumStoredValues);
}
