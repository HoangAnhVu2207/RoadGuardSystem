using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Offline;

public sealed class H5OfflineMigrationTests : IAsyncLifetime
{
    private readonly SqlServerTestFixture fixture = new(createSpatialProbeSchema: false);
    public Task InitializeAsync() => fixture.InitializeAsync();
    public Task DisposeAsync() => fixture.DisposeAsync();
    private RoadGuardDbContext Db() => new(new DbContextOptionsBuilder<RoadGuardDbContext>()
        .UseSqlServer(fixture.ConnectionString, options => options.UseNetTopologySuite()).Options);
    private const string Previous = "20261006084919_H4RepairProducers";
    private static readonly string[] Tables = ["OfflineDeviceRegistrations","OfflineDeviceRevocations",
        "OfflineTaskSnapshots","OfflineOperationBindings","OfflineEncryptedPackages","OfflineHandoverGrants",
        "OfflineHandoverGrantRevocations","OfflineSyncBatches","OfflineOperationAdmissions","OfflineOperationResults",
        "OfflineEvidenceCaptureReferences","OfflinePackageFileReferences","OfflineAdmittedFileReferences",
        "OfflineEncryptedCaptureArtifacts","OfflineOriginTimeVerifications"];

    [Fact]
    public async Task ActualSchemaHasAllDeclaredGuardsAndEmptyDownPreservesPriorSchema()
    {
        await using var db = Db(); await db.Database.MigrateAsync();
        var differences = db.GetService<IMigrationsModelDiffer>().GetDifferences(
            db.GetService<IModelRuntimeInitializer>().Initialize(db.GetService<IMigrationsAssembly>().ModelSnapshot!.Model, true).GetRelationalModel(),
            db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model.GetRelationalModel());
        Assert.Empty(differences);
        foreach (var table in Tables)
            foreach (var suffix in new[] { "_Immutable", "_Scope" })
            {
                var name = "TR_" + table + suffix;
                Assert.Equal(1, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.triggers WHERE name={name} AND is_disabled=0").SingleAsync());
            }
        await db.GetService<IMigrator>().MigrateAsync(Previous);
        Assert.Equal(1, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name='RepairItems'").SingleAsync());
        await db.Database.MigrateAsync();
    }

    [Fact]
    public async Task RetainedRegistrationCannotBeRewrittenDeletedOrErasedByDowngrade()
    {
        await using var db = Db(); await db.Database.MigrateAsync();
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Controlled offline schema source", null, null, null, null, now);
        if (!await db.Roles.AnyAsync(row => row.Code == UserRoleCode.RepairCrew)) db.Add(new ApplicationRole(UserRoleCode.RepairCrew, "Crew"));
        var actor = new ApplicationUser { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString(), PasswordHash = "fixture", DisplayName = "controlled actor", RoleCode = UserRoleCode.RepairCrew, CreatedAt = now };
        db.AddRange(project, actor); await db.SaveChangesAsync();
        using var encryption = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        using var signing = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var registration = OfflineDeviceRegistration.Register(Guid.NewGuid(), project.Id, actor.Id, Guid.NewGuid(), 1,
            UserRoleCode.RepairCrew, Convert.ToBase64String(encryption.ExportSubjectPublicKeyInfo()),
            Convert.ToBase64String(signing.ExportSubjectPublicKeyInfo()), now);
        db.Add(registration); await db.SaveChangesAsync();
        var update = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE OfflineDeviceRegistrations SET Revision=2 WHERE Id={registration.Id}"));
        Assert.Equal(51400, update.Number);
        var delete = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE OfflineDeviceRegistrations WHERE Id={registration.Id}"));
        Assert.Equal(51400, delete.Number);
        var downgrade = await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>().MigrateAsync(Previous));
        Assert.Equal(51490, downgrade.Number);
        Assert.Equal(1, await db.Set<OfflineDeviceRegistration>().Where(row => row.Id == registration.Id).Select(row => row.Revision).SingleAsync());
    }
}
