using RoadGuardSystem.Repositories.Repairs;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.IntegrationTests.Repairs;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class H6RepairDeadlineSourceSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task ActualNormalProposalDeadlineDispatchesToCurrentProjectSupervisorWithoutInventedIndividual()
    {
        await using var db = sql.CreateDbContext(); var source = await H4GenuineRepairSource.Seed(db, sql);
        var obligation = RepairObligation.Create(Guid.NewGuid(), source.Project, source.Defect, RepairObligationKind.FormalRepair,
            true, RepairActualScope.Create(Guid.NewGuid(), source.Road, "actual-route:" + source.Route, "road", 1, 2, 0, 1));
        var package = RepairPackage.Create(Guid.NewGuid(), source.Project, source.Defect, [obligation]); db.Add(package);
        db.Entry(package).Property(row => row.DefectStatusAtAnchor).CurrentValue = DefectStatus.Open; await db.SaveChangesAsync();
        var proposal = await new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System).ProposeItemAsync(
            new(source.Pm, UserRoleCode.ProjectManager, source.Project, package.Id,
                new RepairItemProposeData(obligation.Id, "NORMAL", "actual proposal", "checklist-v1", "actual Supervisor duty"),
                Guid.NewGuid().ToString(), Convert.ToBase64String(db.Entry(package).Property<byte[]>("RowVersion").CurrentValue!)), default);
        Assert.Equal(201, proposal.Status); var item = Assert.IsType<RepairItemFact>(proposal.Value);
        var sourceClock = await db.Set<DeadlineClock>().SingleAsync(row => row.TargetId == item.Id && row.Kind == DeadlineClockKind.SupervisorInitialApproval);
        var time = new FixedClock(sourceClock.OriginalDueAt.AddMinutes(1)); var repository = new H6NotificationDispatchRepository(db, time);
        await repository.ObserveClocksAsync(default); db.ChangeTracker.Clear();
        var breach = await db.Set<DeadlineBreach>().SingleAsync(row => row.ClockId == sourceClock.Id);
        var message = await db.OutboxMessages.SingleAsync(row => row.Id == breach.Id); var fence = Guid.NewGuid();
        message.AcquireLease("h6:" + fence.ToString("N"), time.GetUtcNow(), TimeSpan.FromMinutes(2), 32); await db.SaveChangesAsync();
        var claim = new H6Claim(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson, fence,
            message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
        var result = await repository.DispatchAsync(claim, H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal("COMMITTED", result.Status); Assert.Equal(1, result.Delivered); Assert.Equal(0, result.Unresolved);
        Assert.Equal(source.Supervisor, (await db.Notifications.SingleAsync(row => row.SourceEntityId == sourceClock.Id)).RecipientUserId);
    }
    [Fact]
    public async Task HistoricalSupervisorReviewBreachCannotDeliverAfterNormalContinuationMovesTheObligationHead()
    {
        await using var db = sql.CreateDbContext(); var source = await H4GenuineRepairSource.Seed(db, sql);
        var obligation = RepairObligation.Create(Guid.NewGuid(), source.Project, source.Defect,
            RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), source.Road, "actual-route:" + source.Route, "road", 1, 2, 0, 1));
        var package = RepairPackage.Create(Guid.NewGuid(), source.Project, source.Defect, [obligation]);
        db.Add(package);
        db.Entry(package).Property(row => row.DefectStatusAtAnchor).CurrentValue = DefectStatus.Open;
        await db.SaveChangesAsync();
        var proposed = await new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System)
            .ProposeItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, package.Id,
                new RepairItemProposeData(obligation.Id, "NORMAL", "actual proposal", "checklist-v1", "Supervisor duty"),
                Guid.NewGuid().ToString(), Convert.ToBase64String(db.Entry(package)
                    .Property<byte[]>("RowVersion").CurrentValue!)), default);
        Assert.Equal(201, proposed.Status);
        var item = Assert.IsType<RepairItemFact>(proposed.Value);
        var sourceClock = await db.Set<DeadlineClock>().SingleAsync(row => row.TargetId == item.Id &&
            row.Kind == DeadlineClockKind.SupervisorInitialApproval);
        var time = new FixedClock(sourceClock.OriginalDueAt.AddMinutes(1));
        var continuation = await new RepairWorkflowRepository(db, new IdempotencyOperationService(db), time)
            .ContinueNormallyAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, package.Id,
                item.Id, new("continued normal plan", "checklist-v1", "continue current repair", null),
                Guid.NewGuid().ToString(), item.Version), default);
        Assert.Equal(201, continuation.Status);
        var successor = Assert.IsType<RepairLifecycleFact>(continuation.Value);
        Assert.NotEqual(item.Id, successor.SuccessorItemId);
        var repository = new H6NotificationDispatchRepository(db, new FixedClock(time.GetUtcNow().AddMinutes(1)));
        Assert.True(await repository.ObserveClocksAsync(default) > 0);
        db.ChangeTracker.Clear();
        var breach = await db.Set<DeadlineBreach>().SingleAsync(row => row.ClockId == sourceClock.Id);
        var message = await db.OutboxMessages.SingleAsync(row => row.Id == breach.Id);
        var fence = Guid.NewGuid();
        message.AcquireLease("h6:" + fence.ToString("N"), time.GetUtcNow().AddMinutes(1),
            TimeSpan.FromMinutes(2), 32);
        await db.SaveChangesAsync();
        var claim = new H6Claim(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson,
            fence, message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
        var dispatch = await repository.DispatchAsync(claim,
            H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal("REJECTED", dispatch.Status);
        Assert.Equal("notification_source_relation_invalid", dispatch.ReasonCode);
        Assert.False(await db.Notifications.AnyAsync(row => row.SourceEntityId == sourceClock.Id));
    }
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
}
