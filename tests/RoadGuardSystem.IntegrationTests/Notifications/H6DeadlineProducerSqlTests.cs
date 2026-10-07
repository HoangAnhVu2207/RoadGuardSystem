using System.Data;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class H6DeadlineProducerSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task ActualIncompleteFieldIntakeHasOneStableBreachAndCurrentPrimaryManagerResponsibility()
    {
        var seed = await Intake(); await using var db = sql.CreateDbContext();
        var clock = await db.Set<DeadlineClock>().SingleAsync(row => row.Id == seed.Clock);
        var repository = new H6NotificationDispatchRepository(db, new FixedClock(clock.OriginalDueAt.AddMinutes(1)));
        Assert.Equal(1, await repository.ObserveClocksAsync(default)); Assert.Equal(0, await repository.ObserveClocksAsync(default));
        db.ChangeTracker.Clear(); clock = await db.Set<DeadlineClock>().Include(row => row.Breaches).SingleAsync(row => row.Id == seed.Clock);
        var breach = Assert.Single(clock.Breaches);
        var message = await db.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == breach.Id);
        var plan = H6NotificationCatalog.Parse(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson);
        Assert.Equal(clock.Id, plan.Source.SourceId); Assert.Equal(breach.Id, plan.Source.OriginEventId); Assert.Equal(breach.Id, plan.Source.SourceRevisionId);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var proof = await new H6DeadlineNotificationSourceAdapter(db).ResolveAsync(plan, default);
        Assert.Equal("VERIFIED", proof.Status); Assert.Equal(seed.Manager, proof.ResponsibleUserId);
        Assert.Equal(UserRoleCode.ProjectManager, proof.ResponsibleRole);
        await transaction.CommitAsync();
    }
    [Fact]
    public async Task UnadmittedEarlierBreachIsNotLostWhenClockWasExtendedAndCompleted()
    {
        var seed = await Intake(); await using var db = sql.CreateDbContext();
        var clock = await db.Set<DeadlineClock>().Include(row => row.Breaches).Include(row => row.Extensions).SingleAsync(row => row.Id == seed.Clock);
        var breachId = Guid.NewGuid(); var observed = clock.OriginAt.AddHours(25); clock.ObserveBreach(breachId, observed);
        // This isolated history fixture does not adopt any production extension-command authority.
        clock.Extend(Guid.NewGuid(), seed.Manager, clock.OriginAt.AddHours(60), "fixture history", clock.OriginAt.AddHours(26));
        clock.Complete(clock.OriginAt.AddHours(30)); await db.SaveChangesAsync();
        Assert.Equal(1, await new H6NotificationDispatchRepository(db, new FixedClock(clock.OriginAt.AddHours(70))).ObserveClocksAsync(default));
        var message = await db.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == breachId);
        Assert.Equal(observed, message.OccurredAtUtc); Assert.Equal("deadline.breached.v1", message.MessageType);
    }
    [Fact]
    public async Task CurrentSupervisorRoleCannotReceiveAnOldPrimaryManagerDeadlineDuty()
    {
        var seed = await Intake(); await using var db = sql.CreateDbContext();
        var sourceClock = await db.Set<DeadlineClock>().SingleAsync(row => row.Id == seed.Clock);
        var observed = sourceClock.OriginalDueAt.AddMinutes(1);
        var repository = new H6NotificationDispatchRepository(db, new FixedClock(observed));
        Assert.Equal(1, await repository.ObserveClocksAsync(default));
        if (!await db.Roles.AnyAsync(row => row.Code == UserRoleCode.Supervisor))
            db.Roles.Add(new ApplicationRole(UserRoleCode.Supervisor, "Supervisor"));
        // Controlled current-role fixture; role-change command authority is tested by Identity.
        await db.Users.Where(row => row.Id == seed.Manager)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.RoleCode, UserRoleCode.Supervisor));
        db.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = sourceClock.ProjectId,
            UserId = seed.Manager,
            RoleCode = UserRoleCode.Supervisor,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var breach = await db.Set<DeadlineBreach>().AsNoTracking().SingleAsync(row => row.ClockId == seed.Clock);
        var message = await db.OutboxMessages.SingleAsync(row => row.Id == breach.Id);
        var fence = Guid.NewGuid(); message.AcquireLease("h6:" + fence.ToString("N"), observed, TimeSpan.FromMinutes(2), 32);
        await db.SaveChangesAsync();
        var claim = new H6Claim(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson,
            fence, message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
        var result = await repository.DispatchAsync(claim,
            H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal("COMMITTED", result.Status); Assert.Equal(0, result.Delivered); Assert.Equal(1, result.Unresolved);
        Assert.False(await db.Notifications.AnyAsync(row => row.RecipientUserId == seed.Manager && row.SourceEntityId == seed.Clock));
    }
    internal async Task<(Guid Clock, Guid Manager)> Intake()
    {
        var seed = await new H6FieldSourceSqlTests(sql).Seed(); await using var db = sql.CreateDbContext();
        var task = await db.FieldInspectionTasks.AsNoTracking().SingleAsync(row => row.Id == seed.Task);
        var guard = new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System);
        var repository = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        async Task Run(string action, object input)
        {
            db.ChangeTracker.Clear(); var version = await db.FieldInspectionTasks.Where(row => row.Id == seed.Task)
                .Select(row => Convert.ToBase64String(row.RowVersion)).SingleAsync();
            var result = await repository.ExecuteAsync(new(task.ProjectId, task.Id, action, input, Guid.NewGuid().ToString("N"), version,
                new(seed.Crew, UserRoleCode.RepairCrew, seed.Crew, "DIRECT", true)),
                async token => await guard.AuthorizeAsync(seed.Crew, UserRoleCode.RepairCrew, task.ProjectId, token) is not null, default);
            Assert.InRange(result.Status, 200, 201);
        }
        await Run("accept", new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskActionInputFact("actual current assignment"));
        await Run("start", new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldStartInputFact(Guid.NewGuid(), DateTimeOffset.UtcNow));
        var start = await db.FieldTaskStartOrigins.AsNoTracking().SingleAsync(row => row.TaskId == seed.Task);
        await Run("submit", new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldSubmissionInputFact(Guid.NewGuid(), start.Id, null, [], [], null, "MEASUREMENT", null, null));
        var clock = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(row => row.TargetId == seed.Task && row.Kind == DeadlineClockKind.ProjectManagerReview);
        var manager = await db.ProjectMembers.Where(row => row.ProjectId == task.ProjectId && row.IsPrimary && row.RoleCode == UserRoleCode.ProjectManager)
            .Select(row => row.UserId).SingleAsync();
        return (clock.Id, manager);
    }
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => now; }
}
