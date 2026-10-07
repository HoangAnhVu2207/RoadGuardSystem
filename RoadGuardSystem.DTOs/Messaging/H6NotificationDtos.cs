using System.Text.Json.Serialization;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.DTOs.Messaging;

public sealed record WeeklyDigestDutyView(Guid ClockId, Guid TargetId, string Kind, Guid OriginEventId,
    DateTimeOffset OriginAtUtc, DateTimeOffset DueAtRecoveryUtc);
public sealed record WeeklyDigestView(Guid Id, Guid ProjectId, Guid? RecipientId, DateTimeOffset ScheduledAtUtc,
    DateTimeOffset RecoveredAtUtc, WeeklyDigestDutyView[] PendingAtRecovery, DateTimeOffset[] RecoveryPeriods);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record H6NotificationEventDto(int SchemaVersion, Guid EventId, string Kind, Guid ProjectId,
    string SourceKind, Guid SourceId, Guid OriginEventId, DateTimeOffset OccurredAtUtc,
    Guid? SourceRevisionId = null, Guid? ResponsibleUserId = null, DateTimeOffset? ScheduledAtUtc = null);
public sealed record H6NotificationScopeDto(Guid NotificationId, Guid? ProjectId, Guid? OccurrenceId,
    string Classification, string SourceKind, Guid SourceId, string EventType, string ResolverVersion);
public sealed record H6DispatchResult(string Status, Guid? MessageId = null, Guid? OccurrenceId = null,
    int Delivered = 0, int Unresolved = 0, string? ReasonCode = null);
public sealed record H6NotificationPlan(H6NotificationEventDto Source, NotificationEventEnvelope? Envelope,
    string MessageType, string Title, string Body, bool AuditOnly, H6RepairCorrectionAuditFacts? AuditFacts = null);
public sealed record H6NotificationSourceScope(string SourceKind, Guid SourceId, Guid ProjectId,
    Guid? AssignedUserId = null)
{
    // Member-init projections let EF bind predicates to columns before SQL paging.
    public H6NotificationSourceScope() : this("", Guid.Empty, Guid.Empty) { }
}
public sealed record H6NotificationSourceResolution(string Status, string? ReasonCode = null,
    Guid? TaskId = null, Guid? AssignmentId = null, Guid? ResponsibleUserId = null, bool ResponsibleIsSupervisor = false,
    Guid? BindingId = null, UserRoleCode? ResponsibleRole = null);
public sealed record H6OutboxClaim(Guid Id, string MessageType, DateTimeOffset OccurredAtUtc,
    string PayloadJson, Guid Fence, DateTimeOffset LeaseExpiresAtUtc, int Attempt);
public sealed record H6ClockView(Guid Id, Guid ProjectId, string Kind, Guid TargetId, Guid OriginEventId,
    DateTimeOffset OriginAt, DateTimeOffset OriginalDueAt, DateTimeOffset CurrentDueAt, bool Overdue,
    DateTimeOffset? CompletedAt, DateTimeOffset? AcknowledgedAt, string Version,
    H6ClockExtensionView[] Extensions, H6ClockBreachView[] Breaches, string[] PendingCapabilities)
{
    public Guid? AppointedActorId { get; init; }
    public string? AppointedRole { get; init; }
    public object[] Appointments { get; init; } = [];
}
public sealed record H6ClockExtensionView(Guid Id, Guid ActorId, DateTimeOffset At, DateTimeOffset PreviousDueAt,
    DateTimeOffset NewDueAt, string Reason, bool PreviousDeadlineBreached);
public sealed record H6ClockBreachView(Guid Id, DateTimeOffset DueAt, DateTimeOffset ObservedAt);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record H6ClockCursor(Guid ActorId, Guid ProjectId, string Role, DateTimeOffset AfterDueAtUtc, Guid AfterClockId);
public sealed record H6ClockPage(string Status, H6ClockView[] Items, H6ClockCursor? Continuation = null);
