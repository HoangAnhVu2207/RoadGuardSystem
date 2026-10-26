using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Seeding;

public sealed class AnhHuyDependencySchemaTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefectVersion_FreshAndBaseline_PreservesNullableLegacyAndDetectsStaleWrites(bool baseline)
    {
        var fixture = new SqlServerTestFixture(createSpatialProbeSchema: false);
        await fixture.InitializeAsync();
        try
        {
            var options = new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(fixture.ConnectionString,
                s => s.UseNetTopologySuite()).Options;
            await using var db = new RoadGuardDbContext(options);
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(baseline ? "20261008171739_BaselineCurrentSchema" : null);
            var id = Guid.NewGuid();
            await db.Database.ExecuteSqlRawAsync("INSERT INTO DefectTypes (Code,Name,IsActive) VALUES ('RG_SCHEMA','Synthetic schema probe',1)");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Defects (Id,DefectTypeCode,Status,Severity) VALUES ({id},'RG_SCHEMA',1,1)");
            if (baseline) await migrator.MigrateAsync();
            var row = await db.Set<Defect>().SingleAsync(d => d.Id == id);
            row.ProjectId.Should().BeNull(); row.RoadSectionVersionId.Should().BeNull();
            var version = db.Entry(row).Property<byte[]>("RowVersion").CurrentValue!;
            version.Should().HaveCount(8);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Defects SET Severity=2 WHERE Id={id}");
            row.Assess("RG_SCHEMA", null, DefectSeverity.High, Guid.NewGuid(), "Synthetic concurrency probe");
            var stale = () => db.SaveChangesAsync();
            await stale.Should().ThrowAsync<DbUpdateConcurrencyException>();
            db.ChangeTracker.Clear();
            (await db.Set<Defect>().Where(d => d.Id == id).Select(d => EF.Property<byte[]>(d, "RowVersion")).SingleAsync())
                .Should().NotEqual(version);
            await migrator.MigrateAsync("20261008171739_BaselineCurrentSchema");
            db.ChangeTracker.Clear();
            (await db.Set<Defect>().SingleAsync(d => d.Id == id)).ProjectId.Should().BeNull();
            await migrator.MigrateAsync();
            (await db.Set<Defect>().Where(d => d.Id == id).Select(d => EF.Property<byte[]>(d, "RowVersion")).SingleAsync())
                .Should().HaveCount(8);
        }
        finally { await fixture.DisposeAsync(); }
    }
}
