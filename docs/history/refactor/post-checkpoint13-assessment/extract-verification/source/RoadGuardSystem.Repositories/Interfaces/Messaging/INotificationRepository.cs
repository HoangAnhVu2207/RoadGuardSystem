namespace RoadGuardSystem.Repositories.Messaging;

public interface INotificationRepository
{
    Task<NotificationReadView?> GetAsync(
        Guid recipientUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<NotificationPageReadResult> ListAsync(
        Guid recipientUserId,
        DateTimeOffset? beforeOccurredAtUtc,
        Guid? beforeId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<NotificationMarkReadPersistenceResult> MarkReadAsync(
        Guid recipientUserId,
        Guid notificationId,
        string idempotencyKey,
        string requestFingerprint,
        string expectedVersion,
        CancellationToken cancellationToken = default);
}
