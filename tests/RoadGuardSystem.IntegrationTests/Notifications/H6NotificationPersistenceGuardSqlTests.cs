using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class H6NotificationPersistenceGuardSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task PopulatedNotificationHistoryCannotBeErasedByDowngrade()
    {
        // A destructive migration probe needs its own database, never the shared
        // producer fixture used by the other cases in this class.
        var isolated = new SqlServerTestFixture(createSpatialProbeSchema: false); await isolated.InitializeAsync();
        try
        {
            await using var db = new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>()
                .UseSqlServer(isolated.ConnectionString, options => options.UseNetTopologySuite()).Options);
            await db.Database.MigrateAsync(); var now = DateTimeOffset.UtcNow;
            var message = OutboxMessage.Create(Guid.NewGuid(), "field.future.v99", now, null, "{}");
            db.Add(message); await db.SaveChangesAsync();
            // Controlled retained audit tests preservation, not notification admission authority.
            var audit = new H6NotificationAuditRow
            {
                Id = Guid.NewGuid(),
                OutboxMessageId = message.Id,
                Classification = "UNKNOWN_PROTECTED",
                SourceKind = "OutboxMessage",
                SourceId = message.Id,
                ReasonCode = "notification_event_unregistered",
                ResolverVersion = "fixture",
                DedupKey = new string('a', 64),
                RecordedAtUtc = now
            };
            db.Add(audit); await db.SaveChangesAsync();
            var error = await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>().MigrateAsync("20261006094905_H5OfflinePersistence"));
            Assert.Equal(51199, error.Number);
            Assert.True(await db.Set<H6NotificationAuditRow>().AnyAsync(row => row.Id == audit.Id));
            Assert.True(await db.OutboxMessages.AnyAsync(row => row.Id == message.Id));
        }
        finally { await isolated.DisposeAsync(); }
    }
    [Theory]
    [InlineData("OCCURRENCE")]
    [InlineData("RECIPIENT")]
    [InlineData("SCOPE")]
    [InlineData("RECEIPT")]
    public async Task DirectSqlCannotRewriteCommittedSourceRecipientScopeOrFence(string target)
    {
        var seed = await new H6FieldSourceSqlTests(sql).Seed(); await using var db = sql.CreateDbContext();
        var message = await db.OutboxMessages.SingleAsync(row => row.Id == seed.Event);
        var fence = Guid.NewGuid(); message.AcquireLease("h6:" + fence.ToString("N"), DateTimeOffset.UtcNow, TimeSpan.FromMinutes(2), 32);
        await db.SaveChangesAsync();
        var claim = new H6Claim(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson,
            fence, message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
        var result = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim,
            H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal(1, result.Delivered);
        var notification = await db.Notifications.SingleAsync(row => row.SourceEntityId == seed.Task);
        var changed = Guid.NewGuid();
        await Assert.ThrowsAsync<SqlException>(async () =>
        {
            if (target == "OCCURRENCE")
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [H6NotificationOccurrences] SET [OriginEventId]={changed} WHERE [Id]={result.OccurrenceId}");
            else if (target == "RECIPIENT")
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [H6NotificationDeliveries] SET [RecipientUserId]={seed.Reporter} WHERE [OccurrenceId]={result.OccurrenceId}");
            else if (target == "SCOPE")
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [H6NotificationScopes] SET [SourceId]={changed} WHERE [NotificationId]={notification.Id}");
            else
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [H6NotificationEventReceipts] SET [CompletionFence]={changed} WHERE [OutboxMessageId]={seed.Event}");
        });
    }
}
