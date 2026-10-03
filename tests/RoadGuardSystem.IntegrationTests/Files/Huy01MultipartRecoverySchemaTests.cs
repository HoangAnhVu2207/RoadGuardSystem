using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Files;

[Trait("Package", "HUY-01")]
public sealed class Huy01MultipartRecoverySchemaTests(IdentitySqlServerFixture sql)
    : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task MultipartRecoverySchema_PersistsFenceStateAndSweepTable()
    {
        await using var db = sql.CreateDbContext();

        var columnCount = await db.Database.SqlQueryRaw<int>("""
            SELECT CAST(COUNT(*) AS int) AS [Value]
            FROM sys.columns
            WHERE [object_id] = OBJECT_ID(N'dbo.UploadSessions')
              AND [name] IN (N'MultipartFence', N'MultipartPhase', N'MultipartDeadline', N'MultipartNextCheckAt')
            """).SingleAsync();
        Assert.Equal(4, columnCount);

        var tableCount = await db.Database.SqlQueryRaw<int>("""
            SELECT CAST(COUNT(*) AS int) AS [Value]
            FROM sys.tables
            WHERE [name] = N'UploadMultipartSweeps'
            """).SingleAsync();
        Assert.Equal(1, tableCount);
    }
}
