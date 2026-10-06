namespace RoadGuardSystem.BusinessObjects.Messaging;

public enum NotificationRecipientDeliveryStatus : byte { Pending = 1, Unresolved = 2, Delivered = 3 }
public enum NotificationRecipientUnavailableReason : byte { MissingResponsibleActor = 1, UserInactive = 2, RoleInactive = 3, MembershipLost = 4 }
public sealed record NotificationDeliveryHistory(DateTimeOffset OccurredAtUtc, NotificationRecipientDeliveryStatus Status,
    Guid? RecipientUserId, NotificationRecipientUnavailableReason? Reason);

public sealed class NotificationRecipientDelivery
{
    private NotificationRecipientDelivery() { }
    public Guid Id { get; private set; }
    public Guid OccurrenceId { get; private set; }
    public Guid? RecipientUserId { get; private set; }
    public NotificationRecipientStrategy? UnresolvedStrategy { get; private set; }
    public NotificationRecipientDeliveryStatus Status { get; private set; }
    public Guid? NotificationId { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }
    private readonly List<NotificationDeliveryHistory> _history = [];
    public IReadOnlyCollection<NotificationDeliveryHistory> History => _history.AsReadOnly();
    public static NotificationRecipientDelivery Create(Guid id, Guid occurrenceId, Guid recipientUserId)
    {
        NotificationDomainGuard.Id(id); NotificationDomainGuard.Id(occurrenceId); NotificationDomainGuard.Id(recipientUserId);
        return new NotificationRecipientDelivery
        {
            Id = id,
            OccurrenceId = occurrenceId,
            RecipientUserId = recipientUserId,
            Status = NotificationRecipientDeliveryStatus.Pending
        };
    }
    public static NotificationRecipientDelivery CreateUnresolved(Guid id, Guid occurrenceId, NotificationRecipientStrategy strategy, DateTimeOffset now)
    {
        NotificationDomainGuard.Id(id); NotificationDomainGuard.Id(occurrenceId); NotificationDomainGuard.Timestamp(now);
        if (!Enum.IsDefined(strategy)) throw new ArgumentOutOfRangeException(nameof(strategy));
        var delivery = new NotificationRecipientDelivery
        {
            Id = id,
            OccurrenceId = occurrenceId,
            UnresolvedStrategy = strategy,
            Status = NotificationRecipientDeliveryStatus.Unresolved
        };
        delivery.AddHistory(now, NotificationRecipientUnavailableReason.MissingResponsibleActor); return delivery;
    }
    // Resolution records an identity; the transactional caller must independently validate current authority.
    public void ResolveRecipient(Guid recipientUserId, DateTimeOffset now)
    {
        NotificationDomainGuard.Id(recipientUserId); NotificationDomainGuard.Timestamp(now);
        ValidateHistoryTime(now);
        if (RecipientUserId.HasValue || Status != NotificationRecipientDeliveryStatus.Unresolved)
            throw new InvalidOperationException("An existing recipient cannot be retargeted.");
        RecipientUserId = recipientUserId; Status = NotificationRecipientDeliveryStatus.Pending; AddHistory(now, null);
    }
    public void RecordDelivered(Guid notificationId, DateTimeOffset now)
    {
        NotificationDomainGuard.Id(notificationId); NotificationDomainGuard.Timestamp(now);
        if (Status == NotificationRecipientDeliveryStatus.Delivered)
        {
            if (NotificationId == notificationId) return;
            throw new InvalidOperationException("A delivered notification effect cannot be replaced.");
        }
        if (Status != NotificationRecipientDeliveryStatus.Pending || !RecipientUserId.HasValue)
            throw new InvalidOperationException("An unresolved recipient cannot have a successful delivery effect.");
        ValidateHistoryTime(now);
        NotificationId = notificationId; DeliveredAtUtc = now.ToUniversalTime();
        Status = NotificationRecipientDeliveryStatus.Delivered; AddHistory(now, null);
    }
    public void RecordUnavailable(NotificationRecipientUnavailableReason reason, DateTimeOffset now)
    {
        NotificationDomainGuard.Timestamp(now);
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason));
        if (Status == NotificationRecipientDeliveryStatus.Delivered)
            throw new InvalidOperationException("Delivery history cannot be removed by authority loss.");
        ValidateHistoryTime(now);
        Status = NotificationRecipientDeliveryStatus.Unresolved; AddHistory(now, reason);
    }
    private void ValidateHistoryTime(DateTimeOffset now)
    {
        if (_history.Count > 0 && now.ToUniversalTime() < _history[^1].OccurredAtUtc)
            throw new ArgumentException("Delivery history cannot be backdated.", nameof(now));
    }
    private void AddHistory(DateTimeOffset now, NotificationRecipientUnavailableReason? reason)
        => _history.Add(new NotificationDeliveryHistory(now.ToUniversalTime(), Status, RecipientUserId, reason));
}
