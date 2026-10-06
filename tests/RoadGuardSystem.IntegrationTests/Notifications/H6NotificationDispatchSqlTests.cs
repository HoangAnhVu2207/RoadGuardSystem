using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class H6NotificationDispatchSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [Fact]
    public async Task DuplicateActualTransportCommitsTwoReceiptsAndOneRecipientEffect()
    {
        var seed = await new H6FieldSourceSqlTests(sql).Seed(); await using var db = sql.CreateDbContext();
        var original = await Lease(db, seed.Event); var originalPlan = Plan(original);
        var duplicateSource = originalPlan.Source with { EventId = Guid.NewGuid() };
        db.OutboxMessages.Add(OutboxMessage.Create(duplicateSource.EventId, original.MessageType,
            original.OccurredAtUtc, null, JsonSerializer.Serialize(duplicateSource, Json)));
        await db.SaveChangesAsync(); var duplicate = await Lease(db, duplicateSource.EventId);
        var repository = new H6NotificationDispatchRepository(db, TimeProvider.System);
        var first = await repository.DispatchAsync(original, originalPlan, default);
        var second = await repository.DispatchAsync(duplicate, Plan(duplicate), default);
        Assert.Equal("COMMITTED", first.Status); Assert.Equal("COMMITTED", second.Status);
        Assert.Equal(1, first.Delivered); Assert.Equal(0, second.Delivered); Assert.Equal(first.OccurrenceId, second.OccurrenceId);
        db.ChangeTracker.Clear();
        var notification = Assert.Single(await db.Notifications.Where(row => row.SourceEntityType == "FieldTask" && row.SourceEntityId == seed.Task).ToArrayAsync());
        Assert.Equal(seed.Crew, notification.RecipientUserId);
        Assert.Single(await db.Set<H6NotificationOccurrenceRow>().Where(row => row.SourceId == seed.Task).ToArrayAsync());
        Assert.Equal(2, await db.Set<H6NotificationEventReceipt>().CountAsync(row => row.OutboxMessageId == original.Id || row.OutboxMessageId == duplicate.Id));
        Assert.Equal(2, await db.OutboxMessages.CountAsync(row => (row.Id == original.Id || row.Id == duplicate.Id) && row.DeliveryStatus == OutboxDeliveryStatus.Completed));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StaleOrExpiredFenceCreatesNoDeliveryOrReceipt(bool expired)
    {
        var seed = await new H6FieldSourceSqlTests(sql).Seed(); await using var db = sql.CreateDbContext();
        var claim = await Lease(db, seed.Event);
        if (expired)
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [OutboxMessages] SET [LeaseExpiresAtUtc]={DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE [Id]={claim.Id}");
        else claim = claim with { Fence = Guid.NewGuid() };
        var result = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim, Plan(claim), default);
        Assert.Equal("STALE_LEASE", result.Status);
        Assert.False(await db.Notifications.AnyAsync(row => row.SourceEntityId == seed.Task));
        Assert.False(await db.Set<H6NotificationEventReceipt>().AnyAsync(row => row.OutboxMessageId == seed.Event));
        Assert.NotEqual(OutboxDeliveryStatus.Completed, await db.OutboxMessages.Where(row => row.Id == seed.Event).Select(row => row.DeliveryStatus).SingleAsync());
    }
    [Fact]
    public async Task AlteredAuditOnlyFlagCannotSuppressActualMandatoryNotification()
    {
        var seed = await new H6FieldSourceSqlTests(sql).Seed(); await using var db = sql.CreateDbContext(); var claim = await Lease(db, seed.Event);
        var result = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim, Plan(claim) with { AuditOnly = true }, default);
        Assert.Equal("REJECTED", result.Status); Assert.Equal("notification_envelope_invalid", result.ReasonCode);
        Assert.False(await db.Notifications.AnyAsync(row => row.SourceEntityId == seed.Task));
        Assert.Equal("REJECTED", await db.Set<H6NotificationEventReceipt>().Where(row => row.OutboxMessageId == seed.Event).Select(row => row.Status).SingleAsync());
    }
    [Fact]
    public async Task EndedSourceAssignmentCannotDeliverToItsFormerCrew()
    {
        var seed = await new H6FieldSourceSqlTests(sql).Seed(); await using var db = sql.CreateDbContext();
        var claim = await Lease(db, seed.Event);
        var assignment = await db.FieldInspectionAssignments.SingleAsync(row => row.FieldInspectionTaskId == seed.Task);
        assignment.End(DateTimeOffset.UtcNow, "actual handover after the original event"); await db.SaveChangesAsync();
        var result = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim, Plan(claim), default);
        Assert.Equal("COMMITTED", result.Status); Assert.Equal(0, result.Delivered); Assert.Equal(1, result.Unresolved);
        Assert.False(await db.Notifications.AnyAsync(row => row.SourceEntityId == seed.Task));
        var delivery = Assert.Single(await db.Set<H6NotificationDeliveryRow>().Where(row => row.OccurrenceId == result.OccurrenceId).ToArrayAsync());
        Assert.Equal(seed.Crew, delivery.RecipientUserId); Assert.Equal("UNRESOLVED", delivery.Status);
        Assert.Equal("notification_assignment_lost", delivery.ReasonCode);
    }
    [Fact]
    public async Task RevokedCrewMembershipRecordsUnresolvedWithoutRetargetingOrNotification()
    {
        var seed = await new H6FieldSourceSqlTests(sql).Seed(); await using var db = sql.CreateDbContext();
        var claim = await Lease(db, seed.Event);
        // External current-authority revocation after the genuine source event and lease.
        await db.ProjectMembers.Where(row => row.UserId == seed.Crew).ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        var result = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim, Plan(claim), default);
        Assert.Equal("COMMITTED", result.Status); Assert.Equal(0, result.Delivered); Assert.Equal(1, result.Unresolved);
        Assert.False(await db.Notifications.AnyAsync(row => row.SourceEntityId == seed.Task));
        var delivery = Assert.Single(await db.Set<H6NotificationDeliveryRow>().Where(row => row.OccurrenceId == result.OccurrenceId).ToArrayAsync());
        Assert.Equal(seed.Crew, delivery.RecipientUserId); Assert.Equal("UNRESOLVED", delivery.Status);
        Assert.Equal("notification_membership_lost", delivery.ReasonCode);
        Assert.Equal(OutboxDeliveryStatus.Completed, await db.OutboxMessages.Where(row => row.Id == seed.Event).Select(row => row.DeliveryStatus).SingleAsync());
    }
    private static H6DispatchPlan Plan(H6Claim claim)
        => H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson);
    private static async Task<H6Claim> Lease(RoadGuardDbContext db, Guid messageId)
    {
        // The finite claim implementation is separately SQL-verified. These dispatch cases use
        // the real persisted lease factory to isolate the completion fence and effect transaction.
        var message = await db.OutboxMessages.SingleAsync(row => row.Id == messageId); var now = DateTimeOffset.UtcNow; var fence = Guid.NewGuid();
        message.AcquireLease("h6:" + fence.ToString("N"), now, TimeSpan.FromMinutes(2), 32); await db.SaveChangesAsync();
        return new(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson, fence, message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
    }
}
