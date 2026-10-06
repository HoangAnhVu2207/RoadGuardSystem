using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Retention;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Retention;

public sealed class H0RetentionMigrationTests : IAsyncLifetime
{
    private readonly SqlServerTestFixture fixture = new(createSpatialProbeSchema: false);
    private const string Baseline = "20261003220000_Huy01AiAttemptClosure";
    private const string Latest = "20261006015156_H0RetentionIntegration";
    public Task InitializeAsync() => fixture.InitializeAsync();
    public Task DisposeAsync() => fixture.DisposeAsync();
    private RoadGuardDbContext Db() => new(new DbContextOptionsBuilder<RoadGuardDbContext>()
        .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite()).Options);

    [Fact]
    public async Task Fresh_and_populated_Huy_upgrade_preserve_history_and_model_parity()
    {
        await using var db = Db();
        var discovered = db.Database.GetMigrations().ToArray();
        Assert.Equal(1, discovered.Count(id => id == "20261003160000_AnhHuyDependencyDefectConcurrency"));
        Assert.Contains(Baseline, discovered);
        Assert.Contains("20261003082408_Huy01SessionTransport", discovered);
        Assert.Contains("20261003180000_Huy01DefectSourceLinks", discovered);
        Assert.Contains("20261003190000_Huy01TrainingLabels", discovered);
        Assert.Contains("20261003200000_Huy01ExportConsumer", discovered);
        Assert.Contains("20261003210000_Huy01AiProducer", discovered);
        Assert.DoesNotContain("20261002151928_Anh02AiReportingExportRetention", discovered);
        Assert.DoesNotContain("20261003090000_Anh02AnalysisAttemptClosure", discovered);
        Assert.Equal(discovered.Length, db.GetService<IMigrationsAssembly>().Migrations.Count);
        await using var finalModelCheck = Db();
        Assert.False(finalModelCheck.Database.HasPendingModelChanges());
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync();
        Assert.Contains(Latest, await db.Database.GetAppliedMigrationsAsync());
        await migrator.MigrateAsync(Baseline);
        var project = Guid.NewGuid();
        db.Projects.Add(Project.Create(project, project.ToString(), "H0 baseline survivor", null, null, null, null, DateTimeOffset.UtcNow));
        await db.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT 1 FROM Roles WHERE Code='SUPERVISOR') INSERT INTO Roles (Code, Name, NormalizedName, IsActive) VALUES ('SUPERVISOR','Supervisor','SUPERVISOR',1)");
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Users.Add(new ApplicationUser { Id = actor, UserName = actor.ToString(), NormalizedUserName = actor.ToString().ToUpperInvariant(), DisplayName = "H0 migration fixture", RoleCode = UserRoleCode.Supervisor, Status = UserStatus.Active, CreatedAt = now, PasswordHash = "fixture-no-login" });
        var session = new UserSession { Id = Guid.NewGuid(), UserId = actor, IssuedAt = now, ExpiresAt = now.AddHours(12), Transport = SessionTransport.Web, LastActivityAt = now, RevokedAt = now.AddMinutes(1) };
        db.Sessions.Add(session);
        var receipt = IdempotencyRecord.Create(actor, project, "H0.MigrationFixture", "preserved-key", new string('a', 64), Guid.NewGuid(), "{\"accepted\":true}", now);
        db.Add(receipt);
        var snapshot = ExportSnapshot.Create(Guid.NewGuid(), project, "{\"frozen\":true}", new string('b', 64), now);
        db.Add(snapshot);
        await db.SaveChangesAsync();
        var before = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        await migrator.MigrateAsync();
        Assert.Equal("H0 baseline survivor", (await db.Projects.AsNoTracking().SingleAsync(p => p.Id == project)).Name);
        db.ChangeTracker.Clear();
        var retainedSession = await db.Sessions.AsNoTracking().SingleAsync(row => row.Id == session.Id);
        Assert.Equal(session.Transport, retainedSession.Transport);
        Assert.Equal(session.ExpiresAt, retainedSession.ExpiresAt);
        Assert.Equal(session.RevokedAt, retainedSession.RevokedAt);
        Assert.Equal(receipt.OutcomeJson, (await db.Set<IdempotencyRecord>().AsNoTracking().SingleAsync(row => row.Id == receipt.Id)).OutcomeJson);
        Assert.Equal(snapshot.Hash, (await db.Set<ExportSnapshot>().AsNoTracking().SingleAsync(row => row.Id == snapshot.Id)).Hash);
        var preservedTables = await db.Database.SqlQueryRaw<string>("SELECT name AS [Value] FROM sys.tables WHERE name IN ('DefectSourceLinks','TrainingLabels','TrainingLabelRevisions','Anh02AiMockRuns','Anh02AiResultProvenance')").ToArrayAsync();
        Assert.Equal(5, preservedTables.Length);
        Assert.Equal(before.Append(Latest), await db.Database.GetAppliedMigrationsAsync());
        foreach (var trigger in new[] { "TR_RetentionBasisRevisions_Immutable", "TR_RetentionHoldHistories_Immutable", "TR_RetentionEvaluationItems_Immutable" })
            Assert.Contains("THROW 51220", await db.Database.SqlQuery<string>($"SELECT OBJECT_DEFINITION(OBJECT_ID({trigger})) AS [Value]").SingleAsync());
        var evaluation = Guid.NewGuid();
        db.Add(new RetentionEvaluation { Id = evaluation, ProjectId = project, RequestedBy = actor, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var denied = await Assert.ThrowsAnyAsync<Exception>(() => migrator.MigrateAsync(Baseline));
        Assert.Contains("Cannot downgrade populated retention integration", denied.Message);
        Assert.True(await db.Set<RetentionEvaluation>().AnyAsync(row => row.Id == evaluation));
        Assert.Contains(Latest, await db.Database.GetAppliedMigrationsAsync());
        await using var upgradedModelCheck = Db();
        Assert.False(upgradedModelCheck.Database.HasPendingModelChanges());
    }
}
