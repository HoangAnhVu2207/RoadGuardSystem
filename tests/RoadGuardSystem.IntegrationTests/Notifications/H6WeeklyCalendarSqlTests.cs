using RoadGuardSystem.Repositories.Repairs;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.IntegrationTests.Repairs;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class H6WeeklyCalendarSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly DateTimeOffset Monday = new(2026, 10, 12, 2, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PreplannedSameRunCallbackCommitsOneReviewOccurrenceAndCurrentManagerDelivery()
    {
        var seed = await new H6DeadlineProducerSqlTests(sql).Intake();
        await using var db = sql.CreateDbContext();
        var time = new MutableClock(Monday.AddMinutes(-1));
        var repository = new H6NotificationDispatchRepository(db, time);
        var run = Guid.NewGuid();
        Assert.Equal(0, await repository.ObserveCalendarAsync(run, null, default));
        var planned = await db.Set<H6NotificationCalendarRow>().SingleAsync(row => row.ClockId == seed.Clock);
        Assert.Equal(Monday, planned.ScheduledAtUtc);
        Assert.Equal("PLANNED", planned.Status);
        time.Now = Monday.AddMilliseconds(10);
        Assert.Equal(0, await repository.ObserveCalendarAsync(Guid.NewGuid(), null, default));
        Assert.Equal("PLANNED", (await db.Set<H6NotificationCalendarRow>().SingleAsync(row => row.Id == planned.Id)).Status);
        time.AdvanceBy = TimeSpan.FromMilliseconds(1);
        var callback = time.GetUtcNow();
        Assert.Equal(1, await repository.ObserveCalendarAsync(run,
            new(run, Monday, callback, true), default));
        Assert.Equal(0, await repository.ObserveCalendarAsync(run,
            new(run, Monday, callback, true), default));
        db.ChangeTracker.Clear();
        var committed = await db.Set<H6NotificationCalendarRow>().SingleAsync(row => row.Id == planned.Id);
        Assert.Equal("COMMITTED", committed.Status);
        Assert.Equal(planned.Id, committed.OutboxMessageId);
        var message = await db.OutboxMessages.SingleAsync(row => row.Id == committed.Id);
        var fence = Guid.NewGuid();
        message.AcquireLease("h6:" + fence.ToString("N"), time.Now, TimeSpan.FromMinutes(2), 32);
        await db.SaveChangesAsync();
        var claim = new H6Claim(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson,
            fence, message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
        var result = await repository.DispatchAsync(claim, H6NotificationCatalog.Parse(claim.Id, claim.MessageType,
            claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal("COMMITTED", result.Status);
        Assert.Equal(1, result.Delivered);
        Assert.Equal(seed.Manager, await db.Set<H6NotificationDeliveryRow>().Where(row => row.OccurrenceId == result.OccurrenceId)
            .Select(row => row.RecipientUserId).SingleAsync());
    }

    [Fact]
    public async Task RestartLeavesMissedPeriodPendingWithoutOutboxOrCatchup()
    {
        var seed = await new H6DeadlineProducerSqlTests(sql).Intake();
        await using var db = sql.CreateDbContext();
        var time = new MutableClock(Monday.AddMinutes(-1));
        var repository = new H6NotificationDispatchRepository(db, time);
        await repository.ObserveCalendarAsync(Guid.NewGuid(), null, default);
        time.Now = Monday.AddMinutes(2);
        Assert.Equal(0, await repository.ObserveCalendarAsync(Guid.NewGuid(), null, default));
        var missed = await db.Set<H6NotificationCalendarRow>().SingleAsync(row => row.ClockId == seed.Clock && row.ScheduledAtUtc == Monday);
        Assert.Equal("PENDING_POLICY", missed.Status);
        Assert.Null(missed.OutboxMessageId);
        Assert.False(await db.OutboxMessages.AnyAsync(row => row.Id == missed.Id));
    }

    [Fact]
    public async Task ActualOpenNormalProposalWeeklyDutyRechecksSupervisorSourceAndCurrentRole()
    {
        await using var db = sql.CreateDbContext(); var source = await H4GenuineRepairSource.Seed(db, sql);
        var obligation = RepairObligation.Create(Guid.NewGuid(), source.Project, source.Defect, RepairObligationKind.FormalRepair,
            true, RepairActualScope.Create(Guid.NewGuid(), source.Road, "actual-route:" + source.Route, "road", 1, 2, 0, 1));
        var package = RepairPackage.Create(Guid.NewGuid(), source.Project, source.Defect, [obligation]); db.Add(package);
        db.Entry(package).Property(row => row.DefectStatusAtAnchor).CurrentValue = DefectStatus.Open; await db.SaveChangesAsync();
        var result = await new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System).ProposeItemAsync(
            new(source.Pm, UserRoleCode.ProjectManager, source.Project, package.Id,
                new RepairItemProposeData(obligation.Id, "NORMAL", "actual proposal", "checklist-v1", "weekly review duty"),
                Guid.NewGuid().ToString(), Convert.ToBase64String(db.Entry(package).Property<byte[]>("RowVersion").CurrentValue!)), default);
        Assert.Equal(201, result.Status);
        var item = Assert.IsType<RepairItemFact>(result.Value);
        var reviewClock = await db.Set<DeadlineClock>().SingleAsync(row => row.TargetId == item.Id &&
            row.Kind == DeadlineClockKind.SupervisorInitialApproval);
        var time = new MutableClock(Monday.AddMinutes(-1)); var repository = new H6NotificationDispatchRepository(db, time);
        var run = Guid.NewGuid(); await repository.ObserveCalendarAsync(run, null, default);
        time.Now = Monday.AddMilliseconds(10);
        Assert.Equal(1, await repository.ObserveCalendarAsync(run, new(run, Monday, time.Now, true), default));
        var period = await db.Set<H6NotificationCalendarRow>().SingleAsync(row => row.ClockId == reviewClock.Id &&
            row.ScheduledAtUtc == Monday);
        var message = await db.OutboxMessages.SingleAsync(row => row.Id == period.Id);
        var fence = Guid.NewGuid(); message.AcquireLease("h6:" + fence.ToString("N"), time.Now, TimeSpan.FromMinutes(2), 32);
        await db.SaveChangesAsync();
        var claim = new H6Claim(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson,
            fence, message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
        var dispatched = await repository.DispatchAsync(claim, H6NotificationCatalog.Parse(claim.Id, claim.MessageType,
            claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal("COMMITTED", dispatched.Status); Assert.Equal(1, dispatched.Delivered);
        Assert.Equal(source.Supervisor, await db.Set<H6NotificationDeliveryRow>().Where(row => row.OccurrenceId == dispatched.OccurrenceId)
            .Select(row => row.RecipientUserId).SingleAsync());
    }

    private sealed class MutableClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public TimeSpan AdvanceBy { get; set; }
        public override DateTimeOffset GetUtcNow()
        {
            var value = Now;
            Now += AdvanceBy;
            return value;
        }
    }
}
