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

    public Task InitializeAsync() => fixture.InitializeAsync();
    public Task DisposeAsync() => fixture.DisposeAsync();
    private RoadGuardDbContext Db() => new(new DbContextOptionsBuilder<RoadGuardDbContext>()
        .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite()).Options);

    [Fact]
    public async Task Fresh_and_populated_Huy_upgrade_preserve_history_and_model_parity()
    {
        await using var db = Db();
        var discovered = db.Database.GetMigrations().ToArray();
        Assert.Equal(2, discovered.Length);
        Assert.EndsWith("_BaselineCurrentSchema", discovered[0]);
        Assert.EndsWith("_AllowVerifiedDefectPostRepairTasks", discovered[1]);
        Assert.Equal(discovered.Length, db.GetService<IMigrationsAssembly>().Migrations.Count);
        await using var finalModelCheck = Db();
        Assert.False(finalModelCheck.Database.HasPendingModelChanges());
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync();

        await migrator.MigrateAsync();
        var project = Guid.NewGuid();
        db.Projects.Add(Project.Create(project, project.ToString(), "H0 baseline survivor", null, null, null, null, DateTimeOffset.UtcNow));
        await db.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT 1 FROM Roles WHERE Code='SUPERVISOR') INSERT INTO Roles (Code, Name, NormalizedName, IsActive) VALUES ('SUPERVISOR','Supervisor','SUPERVISOR',1)");
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Users.Add(new ApplicationUser { Id = actor, UserName = actor.ToString(), NormalizedUserName = actor.ToString().ToUpperInvariant(), DisplayName = "H0 migration fixture", RoleCode = UserRoleCode.Supervisor, Status = UserStatus.Active, CreatedAt = now, PasswordHash = "fixture-no-login" });
        await db.SaveChangesAsync();
        var session = new UserSession { Id = Guid.NewGuid(), UserId = actor, IssuedAt = now, ExpiresAt = now.AddHours(12), Transport = SessionTransport.Web, LastActivityAt = now, RevokedAt = now.AddMinutes(1) };
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Sessions (Id,UserId,IssuedAt,ExpiresAt,Transport,LastActivityAt,RevokedAt) VALUES ({session.Id},{actor},{now},{session.ExpiresAt},{(byte)session.Transport},{now},{session.RevokedAt})");
        var receipt = IdempotencyRecord.Create(actor, project, "H0.MigrationFixture", "preserved-key", new string('a', 64), Guid.NewGuid(), "{\"accepted\":true}", now);
        db.Add(receipt);
        var snapshot = ExportSnapshot.Create(Guid.NewGuid(), project, "{\"frozen\":true}", new string('b', 64), now);
        db.Add(snapshot);
        await db.SaveChangesAsync();
        var before = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        await migrator.MigrateAsync();
        Assert.Equal("H0 baseline survivor", (await db.Projects.AsNoTracking().SingleAsync(p => p.Id == project)).Name);
        db.ChangeTracker.Clear();
        Assert.Equal(1, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM Sessions WHERE Id={session.Id} AND Transport={(byte)session.Transport} AND ExpiresAt={session.ExpiresAt} AND RevokedAt={session.RevokedAt}").SingleAsync());
        Assert.Equal(receipt.OutcomeJson, (await db.Set<IdempotencyRecord>().AsNoTracking().SingleAsync(row => row.Id == receipt.Id)).OutcomeJson);
        Assert.Equal(snapshot.Hash, (await db.Set<ExportSnapshot>().AsNoTracking().SingleAsync(row => row.Id == snapshot.Id)).Hash);
        var preservedTables = await db.Database.SqlQueryRaw<string>("SELECT name AS [Value] FROM sys.tables WHERE name IN ('DefectSourceLinks','TrainingLabels','TrainingLabelRevisions','Anh02AiMockRuns','Anh02AiResultProvenance')").ToArrayAsync();
        Assert.Equal(5, preservedTables.Length);
        Assert.Equal(before, await db.Database.GetAppliedMigrationsAsync());
        foreach (var trigger in new[] { "TR_RetentionBasisRevisions_Immutable", "TR_RetentionHoldHistories_Immutable", "TR_RetentionEvaluationItems_Immutable" })
            Assert.Contains("THROW 51220", await db.Database.SqlQuery<string>($"SELECT OBJECT_DEFINITION(OBJECT_ID({trigger})) AS [Value]").SingleAsync());
        var evaluation = Guid.NewGuid();
        db.Add(new RetentionEvaluation { Id = evaluation, ProjectId = project, RequestedBy = actor, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        Assert.True(await db.Set<RetentionEvaluation>().AnyAsync(row => row.Id == evaluation));

        await migrator.MigrateAsync();
        Assert.Equal(discovered, await db.Database.GetAppliedMigrationsAsync());
        var retainedSession = await db.Sessions.AsNoTracking().SingleAsync(row => row.Id == session.Id);
        Assert.Equal(session.Transport, retainedSession.Transport);
        Assert.Equal(session.ExpiresAt, retainedSession.ExpiresAt);
        Assert.Equal(session.RevokedAt, retainedSession.RevokedAt);
        await using var upgradedModelCheck = Db();
        Assert.False(upgradedModelCheck.Database.HasPendingModelChanges());
    }
}
