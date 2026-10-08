using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.IntegrationTests.Infrastructure;
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
            await context.Database.MigrateAsync();
            await using var connection = new SqlConnection(fixture.ConnectionString);
            await connection.OpenAsync();
            Assert.Equal("bigint", Assert.IsType<string>(await ScalarAsync(connection, """
                SELECT TYPE_NAME(user_type_id) FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.Files') AND name = 'SizeBytes'
                """)));
            Assert.Equal(1, Assert.IsType<int>(await ScalarAsync(connection, """
                SELECT COUNT(*) FROM sys.triggers
                WHERE parent_id = OBJECT_ID('dbo.Files') AND name = 'TR_Files_Immutable' AND is_disabled = 0
                """)));
            Assert.Equal(1, Assert.IsType<int>(await ScalarAsync(connection, """
                SELECT COUNT(*) FROM sys.check_constraints
                WHERE parent_object_id = OBJECT_ID('dbo.Files')
                  AND name = 'CK_Files_SizeBytes_NonNegative' AND is_disabled = 0
                  AND definition LIKE '%SizeBytes%' AND definition LIKE '%>=%'
                """)));
            await ExecuteAsync(connection, """
                INSERT dbo.Files (Id, StorageUri, OriginalName, MimeType, SizeBytes, Checksum, UploadedAt)
                VALUES ('00000000-0000-0000-0000-000000000902', 'rf09-widen-old', 'old.bin',
                        'application/octet-stream', 2147483647,
                        'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb', SYSDATETIMEOFFSET()),
                       ('00000000-0000-0000-0000-000000000903', 'rf09-widen-large', 'large.bin',
                        'video/mp4', 8589934592,
                        'cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc', SYSDATETIMEOFFSET());
                """);
            // The current SQL bigint contract requires an Int64 reader even for in-range rows.
            await using (var command = new SqlCommand("SELECT SizeBytes FROM dbo.Files WHERE StorageUri='rf09-widen-old'", connection))
            await using (var reader = await command.ExecuteReaderAsync())
            {
                Assert.True(await reader.ReadAsync());
                Assert.Throws<InvalidCastException>(() => reader.GetInt32(0));
                Assert.Equal(2147483647L, reader.GetInt64(0));
            }
            var immutable = await Assert.ThrowsAsync<SqlException>(() => ExecuteAsync(connection,
                "UPDATE dbo.Files SET SizeBytes = 1 WHERE StorageUri = 'rf09-widen-large'"));
            Assert.Equal(51020, immutable.Number);
            Assert.Equal(2147483647L, (await context.Files.AsNoTracking().SingleAsync(file => file.StorageUri == "rf09-widen-old")).SizeBytes);
            Assert.Equal(8589934592L, (await context.Files.AsNoTracking().SingleAsync(file => file.StorageUri == "rf09-widen-large")).SizeBytes);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

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
