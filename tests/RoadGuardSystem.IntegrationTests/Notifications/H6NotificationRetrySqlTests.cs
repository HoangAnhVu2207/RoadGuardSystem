using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class H6NotificationRetrySqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetryRechecksCurrentAssignmentAndCreatesAtMostOnePinnedRecipientEffect(bool endedAssignment)
    {
        var seed = await new H6FieldSourceSqlTests(sql).Seed(); await using var db = sql.CreateDbContext();
        var message = await db.OutboxMessages.SingleAsync(row => row.Id == seed.Event); var now = DateTimeOffset.UtcNow; var fence = Guid.NewGuid();
        message.AcquireLease("h6:" + fence.ToString("N"), now, TimeSpan.FromMinutes(2), 32); await db.SaveChangesAsync();
        var claim = new H6Claim(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson,
            fence, message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
        await db.ProjectMembers.Where(row => row.UserId == seed.Crew).ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        var first = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim,
            H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal(1, first.Unresolved);
        await db.ProjectMembers.Where(row => row.UserId == seed.Crew).ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Active));
        if (endedAssignment)
        {
            var assignment = await db.FieldInspectionAssignments.SingleAsync(row => row.FieldInspectionTaskId == seed.Task);
            assignment.End(now.AddSeconds(1), "handover before retry"); await db.SaveChangesAsync();
        }
        var repository = new H6NotificationDispatchRepository(db, new FixedClock(now.AddMinutes(6)));
        Assert.Equal(endedAssignment ? 0 : 1, await repository.RetryUnresolvedAsync(default));
        Assert.Equal(0, await repository.RetryUnresolvedAsync(default)); db.ChangeTracker.Clear();
        var delivery = await db.Set<H6NotificationDeliveryRow>().SingleAsync(row => row.OccurrenceId == first.OccurrenceId);
        Assert.Equal(seed.Crew, delivery.RecipientUserId);
        Assert.Equal(endedAssignment ? "UNRESOLVED" : "DELIVERED", delivery.Status);
        Assert.Equal(endedAssignment ? 0 : 1, await db.Notifications.CountAsync(row => row.SourceEntityId == seed.Task));
        Assert.Equal(2, await db.Set<H6NotificationDeliveryAttempt>().CountAsync(row => row.DeliveryId == delivery.Id));
        Assert.Equal(1, await db.Set<H6NotificationEventReceipt>().CountAsync(row => row.OutboxMessageId == seed.Event));
    }
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
}
