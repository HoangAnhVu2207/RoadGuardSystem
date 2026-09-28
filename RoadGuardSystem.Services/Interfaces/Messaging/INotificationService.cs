using RoadGuardSystem.DTOs.Messaging;

namespace RoadGuardSystem.Services.Messaging;

public interface INotificationService
{
    Task<NotificationReadServiceResult> GetAsync(
        Guid actorUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<NotificationPageServiceResult> ListAsync(
        Guid actorUserId,
        string? cursor,
        int? limit,
        CancellationToken cancellationToken = default);

    Task<NotificationReadServiceResult> MarkReadAsync(
        Guid actorUserId,
        Guid notificationId,
        string idempotencyKey,
        string expectedVersion,
        CancellationToken cancellationToken = default);
}
