using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

public sealed class Rf09TransitionRehearsalTests
{
    [Fact]
    public async Task FilesCandidateWidening_RequiresNewReaderBeforeLargeWrite()
    {
        var fixture = new SqlServerTestFixture(null, null, createSpatialProbeSchema: false);
        await fixture.InitializeAsync();
        try
        {
            var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
                .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite()).Options;
            await using var context = new RoadGuardDbContext(options);
            const string widening = "20261002000100_Anh01M1FileSizeBigint";
            var migrations = context.Database.GetMigrations().ToArray();
            var predecessor = migrations[Array.IndexOf(migrations, widening) - 1];
            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(predecessor);
            await using var connection = new SqlConnection(fixture.ConnectionString);
            await connection.OpenAsync();
            var dependentShapeBefore = await ReadFilesDependentShapeAsync(connection);
            await ExecuteAsync(connection, """
                INSERT dbo.Files (Id, StorageUri, OriginalName, MimeType, SizeBytes, Checksum, UploadedAt)
                VALUES ('00000000-0000-0000-0000-000000000902', 'rf09-widen-old', 'old.bin',
                        'application/octet-stream', 2147483647,
                        'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb', SYSDATETIMEOFFSET());
                ALTER TABLE dbo.Files ADD SizeBytes64 AS CONVERT(bigint, SizeBytes) PERSISTED;
                """);
            Assert.Equal(2147483647L, Assert.IsType<long>(await ScalarAsync(connection,
                "SELECT SizeBytes64 FROM dbo.Files WHERE StorageUri = 'rf09-widen-old'")));

            await ExecuteAsync(connection, "ALTER TABLE dbo.Files DROP COLUMN SizeBytes64");
            await migrator.MigrateAsync(widening);
            await ExecuteAsync(connection, "ALTER TABLE dbo.Files ADD SizeBytes64 AS CONVERT(bigint, SizeBytes) PERSISTED");
            Assert.Equal("bigint", Assert.IsType<string>(await ScalarAsync(connection, """
                SELECT TYPE_NAME(user_type_id) FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.Files') AND name = 'SizeBytes'
                """)));
            Assert.Equal(2147483647L, Assert.IsType<long>(await ScalarAsync(connection,
                "SELECT SizeBytes FROM dbo.Files WHERE StorageUri = 'rf09-widen-old'")));
            Assert.Equal(1, Assert.IsType<int>(await ScalarAsync(connection, """
                SELECT COUNT(*) FROM sys.triggers
                WHERE parent_id = OBJECT_ID('dbo.Files') AND name = 'TR_Files_Immutable' AND is_disabled = 0
                """)));
            Assert.Equal(dependentShapeBefore, await ReadFilesDependentShapeAsync(connection));
            Assert.Equal(1, Assert.IsType<int>(await ScalarAsync(connection, """
                SELECT COUNT(*) FROM sys.check_constraints
                WHERE parent_object_id = OBJECT_ID('dbo.Files')
                  AND name = 'CK_Files_SizeBytes_NonNegative' AND is_disabled = 0
                  AND definition LIKE '%SizeBytes%' AND definition LIKE '%>=%'
                """)));

            // A historical Int32 reader fails even for an in-range row after the authoritative bigint migration.
            await using (var command = new SqlCommand("SELECT SizeBytes FROM dbo.Files WHERE StorageUri='rf09-widen-old'", connection))
            await using (var reader = await command.ExecuteReaderAsync())
            {
                Assert.True(await reader.ReadAsync());
                Assert.Throws<InvalidCastException>(() => reader.GetInt32(0));
                Assert.Equal(2147483647L, reader.GetInt64(0));
            }
            await ExecuteAsync(connection, """
                INSERT dbo.Files (Id, StorageUri, OriginalName, MimeType, SizeBytes, Checksum, UploadedAt)
                VALUES ('00000000-0000-0000-0000-000000000903', 'rf09-widen-large', 'large.bin',
                        'video/mp4', 8589934592,
                        'cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc', SYSDATETIMEOFFSET());
                """);
            Assert.Equal(8589934592L, Assert.IsType<long>(await ScalarAsync(connection,
                "SELECT SizeBytes64 FROM dbo.Files WHERE StorageUri = 'rf09-widen-large'")));
            var immutable = await Assert.ThrowsAsync<SqlException>(() => ExecuteAsync(connection,
                "UPDATE dbo.Files SET SizeBytes = 1 WHERE StorageUri = 'rf09-widen-large'"));
            Assert.Equal(51020, immutable.Number);
            await context.Database.MigrateAsync();
            Assert.Equal(2147483647L, (await context.Files.AsNoTracking().SingleAsync(file => file.StorageUri == "rf09-widen-old")).SizeBytes);
            Assert.Equal(8589934592L, (await context.Files.AsNoTracking().SingleAsync(file => file.StorageUri == "rf09-widen-large")).SizeBytes);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task OwnedMigratedSql_BackfillResumeBoundaryAndBackupRestore()
    {
        var fixture = new SqlServerTestFixture(null, null, createSpatialProbeSchema: false);
        await fixture.InitializeAsync();
        var restoreFixture = new SqlServerTestFixture(null, null, createSpatialProbeSchema: false);
        var backupPath = $"/var/opt/mssql/data/rf09_{Guid.NewGuid():N}.bak";
        try
        {
            await restoreFixture.InitializeAsync();
            var restoredName = restoreFixture.DatabaseName;
            var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
                .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite()).Options;
            int migrationCount;
            await using (var context = new RoadGuardDbContext(options))
            {
                await context.Database.MigrateAsync();
                migrationCount = context.Database.GetMigrations().Count();
                Assert.Equal(context.Database.GetMigrations().ToArray(),
                    (await context.Database.GetAppliedMigrationsAsync()).ToArray());
            }

            await using var connection = new SqlConnection(fixture.ConnectionString);
            await connection.OpenAsync();
            Assert.Equal("bigint", Assert.IsType<string>(await ScalarAsync(connection, """
                SELECT TYPE_NAME(user_type_id) FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.Files') AND name = 'SizeBytes'
                """)));
            Assert.Equal("bigint", Assert.IsType<string>(await ScalarAsync(connection, """
                SELECT TYPE_NAME(user_type_id) FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.UploadSessions') AND name = 'ExpectedSizeBytes'
                """)));
            Assert.Equal(1, Assert.IsType<int>(await ScalarAsync(connection, """
                SELECT COUNT(*) FROM sys.triggers
                WHERE parent_id = OBJECT_ID('dbo.Files') AND name = 'TR_Files_Immutable' AND is_disabled = 0
                """)));

            await ExecuteAsync(connection, """
                INSERT dbo.Files (Id, StorageUri, OriginalName, MimeType, SizeBytes, Checksum, UploadedAt)
                VALUES ('00000000-0000-0000-0000-000000000901', 'rf09-synthetic', 'rf09.bin',
                        'application/octet-stream', 1,
                        'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa', SYSDATETIMEOFFSET())
                """);
            var immutable = await Assert.ThrowsAsync<SqlException>(() => ExecuteAsync(connection, """
                UPDATE dbo.Files SET SizeBytes = 2 WHERE StorageUri = 'rf09-synthetic'
                """));
            Assert.Equal(51020, immutable.Number);

            // Test-only candidate table: no production mapping or migration is changed.
            await ExecuteAsync(connection, """
                CREATE TABLE dbo.Rf09SizeProbe (
                    Id int NOT NULL PRIMARY KEY, LegacyBytes int NULL);
                INSERT dbo.Rf09SizeProbe (Id, LegacyBytes) VALUES (1, 1), (2, 2147483647);
                """);
            var baselineRows = await ReadLegacyRowsAsync(connection);
            var baselineDigest = Digest(baselineRows);
            await ExecuteAsync(connection, $"BACKUP DATABASE [{fixture.DatabaseName}] TO DISK = '{backupPath}' WITH INIT, CHECKSUM");
            await ExecuteAsync(connection, $"RESTORE VERIFYONLY FROM DISK = '{backupPath}' WITH CHECKSUM");
            await ExecuteAsync(connection, "ALTER TABLE dbo.Rf09SizeProbe ADD SizeBytes64 bigint NULL");
            await ExecuteAsync(connection, """
                ALTER TABLE dbo.Rf09SizeProbe ADD CONSTRAINT CK_Rf09SizeProbe_Positive
                    CHECK (SizeBytes64 IS NULL OR SizeBytes64 > 0)
                """);
            Assert.Equal(2, Assert.IsType<int>(await ScalarAsync(connection,
                "SELECT COUNT(*) FROM dbo.Rf09SizeProbe WHERE SizeBytes64 IS NULL")));

            // One bounded batch, then a simulated interruption. Resume uses NULL as its checkpoint.
            await BackfillBatchAsync(connection, 1);
            Assert.Equal(1, Assert.IsType<int>(await ScalarAsync(connection,
                "SELECT COUNT(*) FROM dbo.Rf09SizeProbe WHERE SizeBytes64 IS NULL")));
            await BackfillBatchAsync(connection, 1);
            var beforeReplay = await ReadRowsAsync(connection);
            await BackfillBatchAsync(connection, 1);
            Assert.Equal(beforeReplay, await ReadRowsAsync(connection));
            Assert.Equal("1:1:1|2:2147483647:2147483647", beforeReplay);

            await ExecuteAsync(connection, """
                INSERT dbo.Rf09SizeProbe (Id, SizeBytes64) VALUES
                    (3, 2147483648), (4, 8589934592), (5, 8589934593);
                """);
            Assert.Equal(8589934592L, Assert.IsType<long>(await ScalarAsync(connection,
                "SELECT SizeBytes64 FROM dbo.Rf09SizeProbe WHERE Id = 4")));
            Assert.Equal(8589934593L, Assert.IsType<long>(await ScalarAsync(connection,
                "SELECT SizeBytes64 FROM dbo.Rf09SizeProbe WHERE Id = 5")));
            Assert.Equal(DBNull.Value, await ScalarAsync(connection,
                "SELECT TRY_CONVERT(int, SizeBytes64) FROM dbo.Rf09SizeProbe WHERE Id = 3"));
            var eightGiB = 8589934592L;
            Assert.Throws<OverflowException>(() => checked((int)eightGiB));
            await using (var oldReaderCommand = new SqlCommand(
                "SELECT SizeBytes64 FROM dbo.Rf09SizeProbe WHERE Id = 1", connection))
            await using (var oldReader = await oldReaderCommand.ExecuteReaderAsync())
            {
                Assert.True(await oldReader.ReadAsync());
                Assert.Throws<InvalidCastException>(() => oldReader.GetInt32(0));
                Assert.Equal(1L, oldReader.GetInt64(0));
            }
            var invalid = await Assert.ThrowsAsync<SqlException>(() => ExecuteAsync(connection,
                "INSERT dbo.Rf09SizeProbe (Id, SizeBytes64) VALUES (6, -1)"));
            Assert.Equal(547, invalid.Number);
            var zero = await Assert.ThrowsAsync<SqlException>(() => ExecuteAsync(connection,
                "INSERT dbo.Rf09SizeProbe (Id, SizeBytes64) VALUES (7, 0)"));
            Assert.Equal(547, zero.Number);
            var sourceRows = await ReadRowsAsync(connection);
            var sourceDigest = Digest(sourceRows);
            Assert.Equal("1:1:1|2:2147483647:2147483647|3:NULL:2147483648|4:NULL:8589934592|5:NULL:8589934593",
                sourceRows);
            Assert.Equal(sourceDigest, Digest(await ReadRowsAsync(connection)));

            var files = new List<(string Logical, string Type)>();
            await using (var cmd = new SqlCommand($"RESTORE FILELISTONLY FROM DISK = '{backupPath}'", connection))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    files.Add((reader.GetString(reader.GetOrdinal("LogicalName")), reader.GetString(reader.GetOrdinal("Type"))));
            }
            Assert.Equal(2, files.Count);
            var moves = files.Select(file =>
                $"MOVE '{file.Logical.Replace("'", "''", StringComparison.Ordinal)}' TO '/var/opt/mssql/data/{restoredName}{(file.Type == "L" ? ".ldf" : ".mdf")}'");
            await using (var master = new SqlConnection(fixture.MasterConnectionString))
            {
                await master.OpenAsync();
                await ExecuteAsync(master,
                    $"RESTORE DATABASE [{restoredName}] FROM DISK = '{backupPath}' WITH REPLACE, {string.Join(", ", moves)}, RECOVERY");
            }
            await using var restoredConnection = new SqlConnection(restoreFixture.ConnectionString);
            await restoredConnection.OpenAsync();
            var restoredRows = await ReadLegacyRowsAsync(restoredConnection);
            Assert.Equal(baselineRows, restoredRows);
            Assert.Equal(baselineDigest, Digest(restoredRows));
            Assert.Equal(0, Assert.IsType<int>(await ScalarAsync(restoredConnection, """
                SELECT COUNT(*) FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.Rf09SizeProbe') AND name = 'SizeBytes64'
                """)));
            Assert.Equal(migrationCount, Assert.IsType<int>(await ScalarAsync(restoredConnection,
                "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory")));
            Assert.Equal(1L, Assert.IsType<long>(await ScalarAsync(restoredConnection,
                "SELECT SizeBytes FROM dbo.Files WHERE StorageUri = 'rf09-synthetic'")));
        }
        finally
        {
            await restoreFixture.DisposeAsync();
            await fixture.DisposeAsync();
        }
    }

    private static async Task BackfillBatchAsync(SqlConnection connection, int count)
        => await ExecuteAsync(connection, $"""
            ;WITH next_batch AS (
                SELECT TOP ({count}) Id FROM dbo.Rf09SizeProbe
                WHERE SizeBytes64 IS NULL AND LegacyBytes IS NOT NULL ORDER BY Id)
            UPDATE p SET SizeBytes64 = CONVERT(bigint, p.LegacyBytes)
            FROM dbo.Rf09SizeProbe p JOIN next_batch b ON b.Id = p.Id;
            """);

    private static async Task<string> ReadRowsAsync(SqlConnection connection)
    {
        await using var cmd = new SqlCommand("SELECT Id, LegacyBytes, SizeBytes64 FROM dbo.Rf09SizeProbe ORDER BY Id", connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
            rows.Add($"{reader.GetInt32(0)}:{(reader.IsDBNull(1) ? "NULL" : reader.GetInt32(1))}:{(reader.IsDBNull(2) ? "NULL" : reader.GetInt64(2))}");
        return string.Join('|', rows);
    }

    private static async Task<string> ReadLegacyRowsAsync(SqlConnection connection)
    {
        await using var cmd = new SqlCommand("SELECT Id, LegacyBytes FROM dbo.Rf09SizeProbe ORDER BY Id", connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
            rows.Add($"{reader.GetInt32(0)}:{reader.GetInt32(1)}");
        return string.Join('|', rows);
    }

    private static async Task<string[]> ReadFilesDependentShapeAsync(SqlConnection connection)
    {
        var queries = new[]
        {
            """
            SELECT k.name, k.type, ic.key_ordinal, c.name AS column_name
            FROM sys.key_constraints k JOIN sys.index_columns ic
              ON ic.object_id=k.parent_object_id AND ic.index_id=k.unique_index_id
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE k.parent_object_id=OBJECT_ID('dbo.Files') ORDER BY k.name,ic.key_ordinal FOR JSON PATH
            """,
            """
            SELECT f.name,f.delete_referential_action_desc,fc.constraint_column_id,
                   c.name AS child_column,pc.name AS parent_column
            FROM sys.foreign_keys f JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=f.object_id
            JOIN sys.columns c ON c.object_id=fc.parent_object_id AND c.column_id=fc.parent_column_id
            JOIN sys.columns pc ON pc.object_id=fc.referenced_object_id AND pc.column_id=fc.referenced_column_id
            WHERE f.parent_object_id=OBJECT_ID('dbo.Files') ORDER BY f.name,fc.constraint_column_id FOR JSON PATH
            """,
            """
            SELECT i.name,i.is_unique,i.has_filter,i.filter_definition,ic.key_ordinal,
                   ic.is_included_column,ic.is_descending_key,c.name AS column_name
            FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE i.object_id=OBJECT_ID('dbo.Files') ORDER BY i.name,ic.key_ordinal,c.name FOR JSON PATH
            """,
            """
            SELECT name,definition,is_disabled FROM sys.check_constraints
            WHERE parent_object_id=OBJECT_ID('dbo.Files') AND name<>'CK_Files_SizeBytes_NonNegative'
            ORDER BY name FOR JSON PATH
            """,
            """
            SELECT c.name,TYPE_NAME(c.user_type_id) AS sql_type,c.is_nullable,c.is_identity,c.is_computed,
                   dc.definition AS default_sql,cc.definition AS computed_sql
            FROM sys.columns c LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id
            LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id
            WHERE c.object_id=OBJECT_ID('dbo.Files') AND c.name NOT IN ('SizeBytes','SizeBytes64')
            ORDER BY c.column_id FOR JSON PATH
            """,
            """
            SELECT t.name,t.is_disabled,t.is_instead_of_trigger,m.definition
            FROM sys.triggers t JOIN sys.sql_modules m ON m.object_id=t.object_id
            WHERE t.parent_id=OBJECT_ID('dbo.Files') ORDER BY t.name FOR JSON PATH
            """
        };
        var results = new List<string>();
        foreach (var query in queries)
            results.Add(Assert.IsType<string>(await ScalarAsync(connection, query)));
        return results.ToArray();
    }

    private static string Digest(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static async Task<object?> ScalarAsync(SqlConnection connection, string sql)
    {
        await using var cmd = new SqlCommand(sql, connection);
        return await cmd.ExecuteScalarAsync();
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var cmd = new SqlCommand(sql, connection) { CommandTimeout = 120 };
        await cmd.ExecuteNonQueryAsync();
    }
}
