using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class H6UnknownEventAuditSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task UnknownOwnedActionHasOneSanitizedAuditAndNoLeaseReceiptOrStateEffect()
    {
        await using var db = sql.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        // Unknown diagnostic data is deliberately untrusted; no source fact is manufactured.
        var unknown = OutboxMessage.Create(Guid.NewGuid(), "field.task.future_action.v99", now, null,
            "{\"privateReporterIdentity\":\"must-not-copy-to-audit\"}");
        var processing = OutboxMessage.Create(Guid.NewGuid(), "processing_job.dispatch", now, null, "{}");
        db.AddRange(unknown, processing); await db.SaveChangesAsync();
        var repository = new H6NotificationUnknownAuditRepository(db, TimeProvider.System);
        Assert.Equal(1, await repository.AuditUnregisteredAsync(default));
        Assert.Equal(0, await repository.AuditUnregisteredAsync(default));
        db.ChangeTracker.Clear();
        var audit = Assert.Single(await db.Set<H6NotificationAuditRow>().Where(row => row.OutboxMessageId == unknown.Id).ToArrayAsync());
        Assert.Equal("UNKNOWN_PROTECTED", audit.Classification); Assert.Equal("notification_event_unregistered", audit.ReasonCode);
        Assert.Null(audit.ProjectId); Assert.Null(audit.NotificationId);
        Assert.DoesNotContain("must-not-copy-to-audit", audit.ReasonCode, StringComparison.Ordinal);
        Assert.False(await db.Set<H6NotificationAuditRow>().AnyAsync(row => row.OutboxMessageId == processing.Id));
        Assert.False(await db.Set<H6NotificationEventReceipt>().AnyAsync(row => row.OutboxMessageId == unknown.Id || row.OutboxMessageId == processing.Id));
        foreach (var id in new[] { unknown.Id, processing.Id })
        {
            var untouched = await db.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == id);
            Assert.Equal(OutboxDeliveryStatus.Pending, untouched.DeliveryStatus); Assert.Equal(0, untouched.DeliveryAttemptCount); Assert.Null(untouched.LeaseOwner);
        }
    }
}
