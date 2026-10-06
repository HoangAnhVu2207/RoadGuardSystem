using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.DTOs.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class H6NotificationOperationsSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task ExplicitClockPageContinuesOnlyWithinTheSameCurrentActorProjectRoleAndAnchor()
    {
        var seed = await new H6DeadlineProducerSqlTests(sql).Intake(); await using var db = sql.CreateDbContext();
        var source = await db.Set<DeadlineClock>().SingleAsync(row => row.Id == seed.Clock);
        // Technical clock-history fixture only; no supplement receipt protocol/command authority.
        var other = DeadlineClock.Create(Guid.NewGuid(), source.ProjectId, DeadlineClockKind.CrewSupplement,
            source.TargetId, source.OriginEventId, source.OriginAt);
        db.Add(other); await db.SaveChangesAsync();
        var repository = new H6ProtectedNotificationRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var page = await repository.ClocksPageAsync(seed.Manager, UserRoleCode.ProjectManager, source.ProjectId, null, 1, default);
        Assert.Equal("READY", page.Status); Assert.Single(page.Items); Assert.NotNull(page.Continuation);
        var next = await repository.ClocksPageAsync(seed.Manager, UserRoleCode.ProjectManager, source.ProjectId, page.Continuation, 1, default);
        Assert.Equal("READY", next.Status); Assert.Single(next.Items); Assert.NotEqual(page.Items[0].Id, next.Items[0].Id); Assert.Null(next.Continuation);
        var forged = page.Continuation! with { ProjectId = Guid.NewGuid() };
        Assert.Equal("CURSOR_INVALID", (await repository.ClocksPageAsync(seed.Manager, UserRoleCode.ProjectManager, source.ProjectId, forged, 1, default)).Status);
        await db.ProjectMembers.Where(row => row.UserId == seed.Manager && row.ProjectId == source.ProjectId)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        var denied = await repository.ClocksPageAsync(seed.Manager, UserRoleCode.ProjectManager, source.ProjectId, page.Continuation, 1, default);
        Assert.Equal("DENIED", denied.Status); Assert.Empty(denied.Items);
    }
    [Fact]
    public async Task ChangedDeadlineAnchorRequiresExplicitReloadInsteadOfSilentCursorRetarget()
    {
        var seed = await new H6DeadlineProducerSqlTests(sql).Intake(); await using var db = sql.CreateDbContext();
        var source = await db.Set<DeadlineClock>().Include(row => row.Extensions).SingleAsync(row => row.Id == seed.Clock);
        var cursor = new H6ClockCursorFact(seed.Manager, source.ProjectId, UserRoleCode.ProjectManager.ToString(), source.CurrentDueAt, source.Id);
        // Isolated domain history fixture; production extension authority remains pending.
        source.Extend(Guid.NewGuid(), seed.Manager, source.CurrentDueAt.AddHours(1), "technical cursor fixture", source.OriginAt.AddMinutes(1)); await db.SaveChangesAsync();
        var repository = new H6ProtectedNotificationRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var page = await repository.ClocksPageAsync(seed.Manager, UserRoleCode.ProjectManager, source.ProjectId, cursor, 1, default);
        Assert.Equal("CURSOR_STALE", page.Status); Assert.Empty(page.Items);
    }
    [Fact]
    public async Task CurrentManagerReadsActualClockHistoryAndRevocationHidesIt()
    {
        var seed = await new H6DeadlineProducerSqlTests(sql).Intake(); await using var db = sql.CreateDbContext();
        var clock = await db.Set<DeadlineClock>().SingleAsync(row => row.Id == seed.Clock);
        var repository = new H6ProtectedNotificationRepository(db, new IdempotencyOperationService(db), TimeProvider.System,
            [new H6DeadlineNotificationSourceAdapter(db)]);
        var view = Assert.Single((await repository.ClocksPageAsync(seed.Manager, UserRoleCode.ProjectManager, clock.ProjectId, null, 20, default)).Items);
        Assert.Equal(clock.Id, view.Id); Assert.Equal(clock.OriginalDueAt, view.OriginalDueAt); Assert.Equal(clock.OriginEventId, view.OriginEventId);
        Assert.Contains("RECEIVED_PROTOCOL_PENDING", view.PendingCapabilities);
        await db.ProjectMembers.Where(row => row.UserId == seed.Manager && row.ProjectId == clock.ProjectId)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        Assert.Empty((await repository.ClocksPageAsync(seed.Manager, UserRoleCode.ProjectManager, clock.ProjectId, null, 20, default)).Items);
    }
    [Fact]
    public async Task ClaimedSupervisorRoleCannotReadManagerClockProjection()
    {
        var seed = await new H6DeadlineProducerSqlTests(sql).Intake(); await using var db = sql.CreateDbContext();
        var project = await db.Set<DeadlineClock>().Where(row => row.Id == seed.Clock).Select(row => row.ProjectId).SingleAsync();
        var repository = new H6ProtectedNotificationRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        Assert.Empty((await repository.ClocksPageAsync(seed.Manager, UserRoleCode.Supervisor, project, null, 20, default)).Items);
    }
    [Fact]
    public async Task ScopeProjectionUsesActualProtectedNotificationAndCurrentRole()
    {
        var seed = await new H6DeadlineProducerSqlTests(sql).Intake(); await using var db = sql.CreateDbContext();
        var project = await db.Set<DeadlineClock>().Where(row => row.Id == seed.Clock).Select(row => row.ProjectId).SingleAsync();
        var notification = Notification.Create(Guid.NewGuid(), seed.Manager, "Project", project, "legacy.project.event",
            "Recorded project history", "Protected history", DateTimeOffset.UtcNow);
        db.Notifications.Add(notification); await db.SaveChangesAsync();
        var repository = new H6ProtectedNotificationRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var scope = await repository.ScopeAsync(seed.Manager, UserRoleCode.ProjectManager, notification.Id, default);
        Assert.NotNull(scope); Assert.Equal(project, scope.ProjectId); Assert.Equal("PROJECT", scope.Classification);
        Assert.Null(await repository.ScopeAsync(seed.Manager, UserRoleCode.Supervisor, notification.Id, default));
    }
}
