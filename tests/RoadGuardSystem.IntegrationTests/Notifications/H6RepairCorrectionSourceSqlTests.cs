using System.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class H6RepairCorrectionSourceSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task ActualCorrectionProducerHasProvableScopedHistoryAndNoNotificationFanout()
    {
        await using var db = sql.CreateDbContext(); var seed = await Seed(db); var source = await Correct(db, seed, seed.Original, "UNREPAIRED", "first");
        var message = await db.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == source);
        var plan = H6NotificationCatalog.Parse(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        Assert.Equal("VERIFIED", (await new H6RepairNotificationSourceAdapter(db).ResolveAsync(plan, default)).Status);
        Assert.True(plan.AuditOnly); Assert.Null(plan.Envelope); Assert.Empty(await db.Notifications.Where(row => row.SourceEntityId == seed.Item).ToArrayAsync());
        await tx.CommitAsync();
    }
    [Fact]
    public async Task EarlierActualCorrectionRemainsProvableAfterLaterHeadAdvances()
    {
        await using var db = sql.CreateDbContext(); var seed = await Seed(db); var first = await Correct(db, seed, seed.Original, "UNREPAIRED", "first");
        await Correct(db, seed, first, "CONFIRMED", "later");
        var message = await db.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == first);
        var plan = H6NotificationCatalog.Parse(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        Assert.Equal("VERIFIED", (await new H6RepairNotificationSourceAdapter(db).ResolveAsync(plan, default)).Status);
        Assert.NotEqual(first, await db.RepairItems.Where(row => row.Id == seed.Item).Select(row => row.EffectiveDecisionId).SingleAsync());
        await tx.CommitAsync();
    }
    [Fact]
    public async Task ExistingDecisionDoesNotProveForgedObligationOrCorrectedResult()
    {
        await using var db = sql.CreateDbContext(); var seed = await Seed(db); var source = await Correct(db, seed, seed.Original, "UNREPAIRED", "first");
        var message = await db.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == source);
        var plan = H6NotificationCatalog.Parse(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); var adapter = new H6RepairNotificationSourceAdapter(db);
        Assert.Equal("REJECTED", (await adapter.ResolveAsync(plan with { AuditFacts = plan.AuditFacts! with { ObligationId = Guid.NewGuid() } }, default)).Status);
        Assert.Equal("REJECTED", (await adapter.ResolveAsync(plan with { AuditFacts = plan.AuditFacts! with { Result = "CONFIRMED" } }, default)).Status);
        await tx.CommitAsync();
    }
    private static async Task<Guid> Correct(RoadGuardDbContext db, Facts seed, Guid supersedes, string result, string key)
    {
        db.ChangeTracker.Clear();
        var version = await db.RepairItems.Where(row => row.Id == seed.Item).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync();
        var command = new RepairCorrectionCommand(seed.Actor, UserRoleCode.ProjectManager, seed.Project, seed.Package, seed.Item,
            new RepairCorrectionData(supersedes, result, "current scoped correction", new("observed immutable basis", [])), key, Convert.ToBase64String(version));
        var outcome = await new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System).CorrectAsync(command, default);
        Assert.Equal(201, outcome.Status); db.ChangeTracker.Clear();
        return await db.RepairItems.Where(row => row.Id == seed.Item).Select(row => row.EffectiveDecisionId!.Value).SingleAsync();
    }
    private async Task<Facts> Seed(RoadGuardDbContext db)
    {
        await sql.SeedRolesAsync(db); var now = DateTimeOffset.UtcNow.AddMinutes(-5); var name = Guid.NewGuid().ToString("N");
        var actor = new ApplicationUser { Id = Guid.NewGuid(), UserName = name, NormalizedUserName = name.ToUpperInvariant(), DisplayName = "H6 actual correction fixture", PasswordHash = "fixture", RoleCode = UserRoleCode.ProjectManager, Status = UserStatus.Active, CreatedAt = now };
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "Actual correction source", null, null, null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "sample road"); var geometry = new GeometryFactory(new PrecisionModel(), 32648);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry.CreateLineString([new(0, 0), new(10, 0)]), now, "sample; no accuracy assertion");
        var type = DefectType.Create("C" + Guid.NewGuid().ToString("N"), "Source fixture");
        var defect = Defect.Create(Guid.NewGuid(), project.Id, route.Id, null, type.Code, null, DefectSeverity.Low, DefectStatus.Open, geometry.CreatePoint(new Coordinate(5, 0)), now);
        db.AddRange(actor, project, road, route, type, defect, ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, actor.Id, DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1))); await db.SaveChangesAsync();
        var obligation = RepairObligation.Create(Guid.NewGuid(), project.Id, defect.Id, RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), road.Id, "sample-v1", "road", 1, 2, 0, 1));
        var package = RepairPackage.Create(Guid.NewGuid(), project.Id, defect.Id, [obligation]);
        var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, RepairMode.FastTrack, actor.Id, UserRoleCode.ProjectManager, now, new("source fixture plan", "v1"));
        package.AddItem(item); db.Add(package); await db.SaveChangesAsync(); db.ChangeTracker.Clear(); var original = Guid.NewGuid();
        // Controlled historical acceptance isolates the real current-authority correction producer.
        // These rows do not assert production eligibility, execution grants or offline provenance.
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairDecisions (Id,ItemId,ObligationId,DefectId,Mode,ActorId,Role,Reason,At,Result) VALUES ({original},{item.Id},{obligation.Id},{defect.Id},2,{actor.Id},2,'historical source fixture',{now},3)");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairItems SET EffectiveDecisionId={original},State=7 WHERE Id={item.Id}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairObligations SET EffectiveResolutionHeadDecisionId={original},EffectiveResolutionDecisionId={original} WHERE Id={obligation.Id}");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairObligationResolutionEvents (DecisionId,Accepted,At,ObligationId) VALUES ({original},1,{now},{obligation.Id})");
        return new(actor.Id, project.Id, package.Id, item.Id, original);
    }
    private sealed record Facts(Guid Actor, Guid Project, Guid Package, Guid Item, Guid Original);
}
