namespace RoadGuardSystem.BusinessObjects.Messaging;

// Persisted projections/receipts are separate from the pure foundation. SQL scope and immutable
// history constraints are part of the coordinated additive H6 migration, not factory authority.
public sealed class H6NotificationOccurrenceRow
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid SourceEventId { get; set; }
    public string OccurrenceKey { get; set; } = "";
    public string ContentFingerprint { get; set; } = "";
    public string EventType { get; set; } = "";
    public string SourceKind { get; set; } = "";
    public Guid SourceId { get; set; }
    public Guid OriginEventId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset? ScheduledAtUtc { get; set; }
}
public sealed class H6NotificationDeliveryRow
{
    public Guid Id { get; set; }
    public Guid OccurrenceId { get; set; }
    public Guid? RecipientUserId { get; set; }
    public string RecipientKey { get; set; } = "";
    public string Status { get; set; } = "PENDING";
    public Guid? NotificationId { get; set; }
    public string? ReasonCode { get; set; }
    public DateTimeOffset? DeliveredAtUtc { get; set; }
    public DateTimeOffset NextAttemptAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class H6NotificationDeliveryAttempt
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public DateTimeOffset ObservedAtUtc { get; set; }
    public string Status { get; set; } = "";
    public string? ReasonCode { get; set; }
}
public sealed class H6NotificationScopeRow
{
    public Guid NotificationId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? OccurrenceId { get; set; }
    public string Classification { get; set; } = "UNKNOWN_PROTECTED";
    public string SourceKind { get; set; } = "";
    public Guid SourceId { get; set; }
    public string EventType { get; set; } = "";
    public string ResolverVersion { get; set; } = "";
    public Guid CurrentAuditId { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class H6NotificationAuditRow
{
    public Guid Id { get; set; }
    public Guid? NotificationId { get; set; }
    public Guid? OutboxMessageId { get; set; }
    public Guid? PreviousAuditId { get; set; }
    public Guid? ProjectId { get; set; }
    public string Classification { get; set; } = "";
    public string SourceKind { get; set; } = "";
    public Guid SourceId { get; set; }
    public string ReasonCode { get; set; } = "";
    public string ResolverVersion { get; set; } = "";
    public string DedupKey { get; set; } = "";
    public DateTimeOffset RecordedAtUtc { get; set; }
}
public sealed class H6NotificationEventReceipt
{
    public Guid OutboxMessageId { get; set; }
    public Guid CompletionFence { get; set; }
    public Guid? OccurrenceId { get; set; }
    public string MessageType { get; set; } = "";
    public string PayloadHash { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTimeOffset RecordedAtUtc { get; set; }
}
public sealed class H6NotificationCalendarRow
{
    public Guid Id { get; set; }
    public Guid ClockId { get; set; }
    public DateTimeOffset PlannedAtUtc { get; set; }
    public DateTimeOffset ScheduledAtUtc { get; set; }
    public string Status { get; set; } = "PLANNED";
    public Guid SchedulerRunId { get; set; }
    public Guid? OutboxMessageId { get; set; }
    public DateTimeOffset? ObservedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
