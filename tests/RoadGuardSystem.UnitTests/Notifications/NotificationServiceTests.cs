using FluentAssertions;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.UnitTests.Notifications;

[Trait("TaskId", "P2-001")]
public sealed class NotificationServiceTests
{
    [Fact]
    public async Task ListAsync_WithoutLimit_UsesV2DefaultLimit()
    {
        var repository = new RecordingNotificationRepository();
        var service = new NotificationService(repository);

        var result = await service.ListAsync(Guid.NewGuid(), null, null);

        result.Status.Should().Be(NotificationServiceStatus.Success);
        repository.Limit.Should().Be(25);
    }

    private sealed class RecordingNotificationRepository : INotificationRepository
    {
        public int? Limit { get; private set; }

        public Task<NotificationReadView?> GetAsync(Guid recipientUserId, Guid notificationId, CancellationToken cancellationToken = default)
            => Task.FromResult<NotificationReadView?>(null);

        public Task<NotificationPageReadResult> ListAsync(Guid recipientUserId, DateTimeOffset? beforeOccurredAtUtc, Guid? beforeId, int limit, CancellationToken cancellationToken = default)
        {
            Limit = limit;
            return Task.FromResult(new NotificationPageReadResult([], null, null, DateTimeOffset.UtcNow));
        }

        public Task<NotificationMarkReadPersistenceResult> MarkReadAsync(Guid recipientUserId, Guid notificationId, string idempotencyKey, string requestFingerprint, string expectedVersion, CancellationToken cancellationToken = default)
            => Task.FromResult(new NotificationMarkReadPersistenceResult(NotificationMarkReadPersistenceStatus.NotFound));
    }
}
