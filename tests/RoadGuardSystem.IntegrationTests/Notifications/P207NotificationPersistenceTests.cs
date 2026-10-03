using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

[Trait("TaskId", "P2-07")]
public sealed class P207NotificationPersistenceTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P207NotificationPersistenceTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-07: valid notification round-trips through SQL Server")]
    public async Task Notification_ValidRecord_RoundTrips()
    {
        await using var context = _fixture.CreateDbContext();
        var recipient = await CreateRecipientAsync(context);
        var notification = CreateNotification(recipient.Id);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var persisted = await context.Notifications
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == notification.Id);

        persisted.RecipientUserId.Should().Be(recipient.Id);
        persisted.SourceEntityType.Should().Be("SurveyAssignment");
        persisted.EventType.Should().Be("assignment.reassigned");
        persisted.Title.Should().Be("Assignment updated");
        persisted.Body.Should().Be("Your assigned work has changed.");
        persisted.ReadAt.Should().BeNull();
    }

    [Fact(DisplayName = "P2-07: duplicate recipient source events are rejected")]
    public async Task Notification_DuplicateRecipientSourceEvent_IsRejectedBySqlServer()
    {
        Guid recipientId;
        var sourceId = Guid.NewGuid();
        await using (var seedContext = _fixture.CreateDbContext())
        {
            recipientId = (await CreateRecipientAsync(seedContext)).Id;
            seedContext.Notifications.Add(CreateNotification(recipientId, sourceId));
            await seedContext.SaveChangesAsync();
        }

        await using var context = _fixture.CreateDbContext();
        context.Notifications.Add(CreateNotification(recipientId, sourceId));
        var persist = () => context.SaveChangesAsync();

        await persist.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-07: notification with missing recipient is rejected")]
    public async Task Notification_MissingRecipient_IsRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        context.Notifications.Add(CreateNotification(Guid.NewGuid()));
        var persist = () => context.SaveChangesAsync();

        await persist.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-07: notification content rejects credential or token markers")]
    public void Notification_SensitiveContent_IsRejected()
    {
        var create = () => Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SurveyAssignment",
            Guid.NewGuid(),
            "assignment.reassigned",
            "Assignment updated",
            "token=credential-value",
            DateTimeOffset.UtcNow);

        create.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "P2-07: outbox notification delivery retries without a second effect")]
    public async Task NotificationOutboxConsumer_Replay_ReturnsDurableNotificationWithoutDuplicateEffect()
    {
        await using var context = _fixture.CreateDbContext();
        var recipient = await CreateRecipientAsync(context);
        var message = OutboxMessage.Create(
            Guid.NewGuid(),
            "p2_07.notification_delivery",
            DateTimeOffset.UtcNow,
            null,
            "{\"schemaVersion\":1}");
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();

        var notification = CreateNotification(recipient.Id);
        var consumer = new NotificationOutboxConsumer(context, new ConsumerEffectService(context));

        var first = await consumer.ConsumeAsync(message.Id, notification);
        var replay = await consumer.ConsumeAsync(message.Id, notification);

        first.Status.Should().Be(ConsumerEffectStatus.Recorded);
        first.NotificationId.Should().Be(notification.Id);
        replay.Status.Should().Be(ConsumerEffectStatus.Replayed);
        replay.NotificationId.Should().Be(notification.Id);
        (await context.Notifications.CountAsync(candidate => candidate.Id == notification.Id)).Should().Be(1);
        (await context.ConsumerEffectReceipts.CountAsync(receipt =>
            receipt.MessageId == message.Id && receipt.ConsumerName == NotificationOutboxConsumer.ConsumerName))
            .Should().Be(1);
    }

    [Fact(DisplayName = "ANH-04: notification read is recipient scoped, versioned and replay safe")]
    public async Task MarkRead_RecipientVersionAndReplay_PersistOneReadEffect()
    {
        Guid recipientId;
        Guid notificationId;
        byte[] originalVersion;
        await using (var seed = _fixture.CreateDbContext())
        {
            recipientId = (await CreateRecipientAsync(seed)).Id;
            var notification = CreateNotification(recipientId);
            notificationId = notification.Id;
            seed.Notifications.Add(notification);
            await seed.SaveChangesAsync();
            originalVersion = notification.RowVersion;
        }

        await using var context = _fixture.CreateDbContext();
        var repository = new NotificationPersistenceService(context, new IdempotencyOperationService(context));
        var version = Convert.ToBase64String(originalVersion);
        var outsiderId = (await CreateRecipientAsync(context)).Id;
        var outsider = await repository.MarkReadAsync(outsiderId, notificationId, Guid.NewGuid().ToString(), Fingerprint("outsider"), version);
        outsider.Status.Should().Be(NotificationMarkReadPersistenceStatus.NotFound);
        var stale = await repository.MarkReadAsync(recipientId, notificationId, Guid.NewGuid().ToString(), Fingerprint("stale"), Convert.ToBase64String(Guid.NewGuid().ToByteArray()));
        stale.Status.Should().Be(NotificationMarkReadPersistenceStatus.StaleConcurrency);

        var key = Guid.NewGuid().ToString();
        var first = await repository.MarkReadAsync(recipientId, notificationId, key, Fingerprint("read-once"), version);
        var replay = await repository.MarkReadAsync(recipientId, notificationId, key, Fingerprint("read-once"), version);
        var conflict = await repository.MarkReadAsync(recipientId, notificationId, key, Fingerprint("different-payload"), version);

        first.Status.Should().Be(NotificationMarkReadPersistenceStatus.Success);
        replay.Status.Should().Be(NotificationMarkReadPersistenceStatus.Replayed);
        conflict.Status.Should().Be(NotificationMarkReadPersistenceStatus.IdempotentConflict);
        replay.Notification!.Id.Should().Be(notificationId);
        await using var verify = _fixture.CreateDbContext();
        var persisted = await verify.Notifications.AsNoTracking().SingleAsync(value => value.Id == notificationId);
        persisted.ReadAt.Should().NotBeNull();
        persisted.RowVersion.Should().NotEqual(originalVersion);
        (await verify.IdempotencyRecords.CountAsync(value => value.ActorUserId == recipientId && value.Operation == "NotificationRead" && value.IdempotencyKey == key)).Should().Be(1);
    }

    private async Task<ApplicationUser> CreateRecipientAsync(RoadGuardDbContext context)
    {
        await _fixture.SeedRolesAsync(context);
        var recipient = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"p207_recipient_{Guid.NewGuid():N}",
            DisplayName = "P2-07 notification recipient",
            PasswordHash = "fixture-password-hash",
            RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(recipient);
        await context.SaveChangesAsync();
        return recipient;
    }

    private static Notification CreateNotification(Guid recipientUserId, Guid? sourceEntityId = null)
        => Notification.Create(
            Guid.NewGuid(),
            recipientUserId,
            "SurveyAssignment",
            sourceEntityId ?? Guid.NewGuid(),
            "assignment.reassigned",
            "Assignment updated",
            "Your assigned work has changed.",
            DateTimeOffset.UtcNow);

    private static string Fingerprint(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
