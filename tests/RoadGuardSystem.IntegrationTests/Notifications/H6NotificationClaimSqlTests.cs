using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class H6NotificationClaimSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    // Protocol-only claim fixtures: these payloads are deliberately not source-verified events.
    [Fact]
    public async Task DiagnosticPendingProducerRemainsRecoverableAfterGenericAttemptCapWithoutResettingHistory()
    {
        await using var db = sql.CreateDbContext(); var at = DateTimeOffset.UtcNow.AddMinutes(-1);
        var message = OutboxMessage.Create(Guid.NewGuid(), "project.handling_renewed.v1", at, null, "{}");
        for (var attempt = 0; attempt < 32; attempt++)
        {
            var owner = "h6:" + Guid.NewGuid().ToString("N");
            message.AcquireLease(owner, at, TimeSpan.FromMinutes(2), int.MaxValue);
            message.ScheduleRetry(owner, at, "notification_source_pending_producer", "Source adapter adoption is pending.", int.MaxValue);
        }
        db.OutboxMessages.Add(message); await db.SaveChangesAsync();
        var claim = await new H6NotificationDispatchRepository(db, TimeProvider.System).ClaimAsync([message.MessageType], default);
        Assert.NotNull(claim); Assert.Equal(message.Id, claim.Id); Assert.Equal(33, claim.Attempt);
        Assert.Equal(33, await db.OutboxMessages.Where(row => row.Id == message.Id).Select(row => row.DeliveryAttemptCount).SingleAsync());
    }
    [Fact]
    public async Task ExactFiniteClaimDoesNotLeaseProcessingOrUnknownEvents()
    {
        await using var db = sql.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        var processing = OutboxMessage.Create(Guid.NewGuid(), "processing_job.dispatch", now.AddMinutes(-5), null, "{}");
        var unknown = OutboxMessage.Create(Guid.NewGuid(), "notification.unknown.future.v99", now.AddMinutes(-4), null, "{}");
        var expected = OutboxMessage.Create(Guid.NewGuid(), "field.task.assigned.v1", now.AddMinutes(-1), null, "{}");
        db.AddRange(processing, unknown, expected); await db.SaveChangesAsync();
        var claim = await new H6NotificationDispatchRepository(db, TimeProvider.System).ClaimAsync(["field.task.assigned.v1"], default);
        Assert.NotNull(claim); Assert.Equal(expected.Id, claim.Id); Assert.NotEqual(Guid.Empty, claim.Fence);
        db.ChangeTracker.Clear();
        foreach (var id in new[] { processing.Id, unknown.Id })
        {
            var untouched = await db.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == id);
            Assert.Equal(OutboxDeliveryStatus.Pending, untouched.DeliveryStatus); Assert.Equal(0, untouched.DeliveryAttemptCount); Assert.Null(untouched.LeaseOwner);
        }
    }
    [Fact]
    public async Task SqlPaddingCannotTurnAnUnregisteredMessageTypeIntoARegisteredType()
    {
        await using var db = sql.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        var unknown = OutboxMessage.Create(Guid.NewGuid(), "field.task.assigned.v1", now.AddMinutes(-5), null, "{}");
        var genuine = OutboxMessage.Create(Guid.NewGuid(), "field.task.assigned.v1", now.AddMinutes(-1), null, "{}");
        db.AddRange(unknown, genuine); await db.SaveChangesAsync();
        // Controlled external row fixture retains bytes that the producer factory normally trims.
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [OutboxMessages] SET [MessageType]={"field.task.assigned.v1 "} WHERE [Id]={unknown.Id}");
        var claim = await new H6NotificationDispatchRepository(db, TimeProvider.System).ClaimAsync(["field.task.assigned.v1"], default);
        Assert.NotNull(claim); Assert.Equal(genuine.Id, claim.Id);
        var untouched = await db.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == unknown.Id);
        Assert.Equal(OutboxDeliveryStatus.Pending, untouched.DeliveryStatus); Assert.Equal(0, untouched.DeliveryAttemptCount);
    }
    [Fact]
    public async Task ConcurrentWorkersCannotOwnTheSameUnexpiredMessage()
    {
        await using var seed = sql.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        var first = OutboxMessage.Create(Guid.NewGuid(), "field.task.submitted.v1", now.AddMinutes(-2), null, "{}");
        var second = OutboxMessage.Create(Guid.NewGuid(), "field.task.submitted.v1", now.AddMinutes(-1), null, "{}");
        seed.AddRange(first, second); await seed.SaveChangesAsync();
        await using var left = sql.CreateDbContext(); await using var right = sql.CreateDbContext();
        var claims = await Task.WhenAll(new H6NotificationDispatchRepository(left, TimeProvider.System).ClaimAsync(["field.task.submitted.v1"], default),
            new H6NotificationDispatchRepository(right, TimeProvider.System).ClaimAsync(["field.task.submitted.v1"], default));
        Assert.All(claims, claim => Assert.NotNull(claim)); Assert.NotEqual(claims[0]!.Id, claims[1]!.Id); Assert.NotEqual(claims[0]!.Fence, claims[1]!.Fence);
        Assert.Equal(2, await seed.OutboxMessages.CountAsync(row => (row.Id == first.Id || row.Id == second.Id) && row.DeliveryStatus == OutboxDeliveryStatus.Leased));
    }
    [Fact]
    public async Task ExpiredLeaseReclaimsSameSourceMessageWithNewFenceAndAttempt()
    {
        await using var db = sql.CreateDbContext(); var time = new ProbeClock(DateTimeOffset.UtcNow);
        var source = OutboxMessage.Create(Guid.NewGuid(), "repair.work.assigned.v1", time.GetUtcNow().AddMinutes(-1), null, "{}"); db.Add(source); await db.SaveChangesAsync();
        var repo = new H6NotificationDispatchRepository(db, time); var first = await repo.ClaimAsync(["repair.work.assigned.v1"], default); Assert.NotNull(first);
        time.Now = first.LeaseExpiresAtUtc.AddSeconds(1);
        var later = await repo.ClaimAsync(["repair.work.assigned.v1"], default); Assert.NotNull(later);
        Assert.Equal(first.Id, later.Id); Assert.NotEqual(first.Fence, later.Fence); Assert.Equal(first.Attempt + 1, later.Attempt);
    }
    private sealed class ProbeClock(DateTimeOffset initial) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = initial;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
