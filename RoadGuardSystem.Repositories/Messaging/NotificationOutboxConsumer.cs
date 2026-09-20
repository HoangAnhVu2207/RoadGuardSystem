using RoadGuardSystem.BusinessObjects.Messaging;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed record NotificationDeliveryResult(ConsumerEffectStatus Status, Guid NotificationId);

/// <summary>
/// Persists one in-app notification for an outbox message using the durable consumer receipt boundary.
/// Delivery scheduling and external notification channels are intentionally outside this persistence boundary.
/// </summary>
public sealed class NotificationOutboxConsumer
{
    public const string ConsumerName = "notification-inbox";

    private readonly RoadGuardDbContext _context;
    private readonly ConsumerEffectService _consumerEffectService;

    public NotificationOutboxConsumer(
        RoadGuardDbContext context,
        ConsumerEffectService consumerEffectService)
    {
        _context = context;
        _consumerEffectService = consumerEffectService;
    }

    public async Task<NotificationDeliveryResult> ConsumeAsync(
        Guid messageId,
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Outbox message id must not be empty.", nameof(messageId));
        }

        ArgumentNullException.ThrowIfNull(notification);

        var result = await _consumerEffectService.ProcessAsync(
            messageId,
            ConsumerName,
            notification.Id,
            operationCancellationToken =>
            {
                _context.Notifications.Add(notification);
                return Task.CompletedTask;
            },
            cancellationToken);

        return new NotificationDeliveryResult(result.Status, result.EffectId);
    }
}
