using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Files;

public sealed class Anh01FileMigrationTests
{
    [Fact]
    public async Task WidenPreservesExistingMetadataAndTrigger_UnsafeDownRollsBack()
    {
        var fixture = new SqlServerTestFixture(createSpatialProbeSchema: false);
        await fixture.InitializeAsync();
        try
        {
            await using var db = new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>()
                .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite()).Options);
            var migrator = db.GetService<IMigrator>();

            await migrator.MigrateAsync();
            var id = Guid.NewGuid();
            var checksum = new string('a', 64);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Files (Id,StorageUri,OriginalName,MimeType,SizeBytes,Checksum,UploadedAt) VALUES ({id},{id.ToString()},{"legacy.pdf"},{"application/pdf"},{int.MaxValue},{checksum},{DateTimeOffset.UtcNow})");
            await migrator.MigrateAsync();
            var legacy = await db.Files.AsNoTracking().SingleAsync(f => f.Id == id);
            legacy.SizeBytes.Should().Be(int.MaxValue);
            legacy.Checksum.Should().Be(checksum);
            var largeId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Files (Id,StorageUri,OriginalName,MimeType,SizeBytes,Checksum,UploadedAt) VALUES ({largeId},{largeId.ToString()},{"large.mp4"},{"video/mp4"},{8589934592L},{checksum},{DateTimeOffset.UtcNow})");
            (await db.Files.AsNoTracking().SingleAsync(f => f.Id == largeId)).SizeBytes.Should().Be(8589934592L);

            var update = () => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Files SET SizeBytes=1 WHERE Id={id}");
            (await update.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(51020);
        }
        finally { await fixture.DisposeAsync(); }
    }
}
