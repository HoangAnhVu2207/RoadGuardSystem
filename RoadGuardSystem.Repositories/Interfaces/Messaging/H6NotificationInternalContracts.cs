using System.Text.Json.Serialization;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;

namespace RoadGuardSystem.Repositories.Messaging;

// Repository contracts describe trusted work and stored facts, not HTTP response shapes.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record H6StoredEvent(int SchemaVersion, Guid EventId, string Kind, Guid ProjectId,
    string SourceKind, Guid SourceId, Guid OriginEventId, DateTimeOffset OccurredAtUtc,
    Guid? SourceRevisionId = null, Guid? ResponsibleUserId = null, DateTimeOffset? ScheduledAtUtc = null);
public sealed record H6StoredCorrectionFacts(Guid ObligationId, Guid SupersedesDecisionId, string Result);
public sealed record H6DispatchPlan(H6StoredEvent Source, NotificationEventEnvelope? Envelope,
    string MessageType, string Title, string Body, bool AuditOnly, H6StoredCorrectionFacts? AuditFacts = null);
public sealed record H6SourceScope(string SourceKind, Guid SourceId, Guid ProjectId, Guid? AssignedUserId = null)
{
    public H6SourceScope() : this("", Guid.Empty, Guid.Empty) { }
}
public sealed record H6SourceResolution(string Status, string? ReasonCode = null,
    Guid? TaskId = null, Guid? AssignmentId = null, Guid? ResponsibleUserId = null, bool ResponsibleIsSupervisor = false,
    Guid? BindingId = null, UserRoleCode? ResponsibleRole = null);
public sealed record H6Claim(Guid Id, string MessageType, DateTimeOffset OccurredAtUtc,
    string PayloadJson, Guid Fence, DateTimeOffset LeaseExpiresAtUtc, int Attempt);
public sealed record H6DispatchOutcome(string Status, Guid? MessageId = null, Guid? OccurrenceId = null,
    int Delivered = 0, int Unresolved = 0, string? ReasonCode = null);
public sealed record H6ScopeFact(Guid NotificationId, Guid? ProjectId, Guid? OccurrenceId,
    string Classification, string SourceKind, Guid SourceId, string EventType, string ResolverVersion);
public sealed record H6ClockCursorFact(Guid ActorId, Guid ProjectId, string Role, DateTimeOffset AfterDueAtUtc, Guid AfterClockId);
public sealed record H6ClockExtensionFact(Guid Id, Guid ActorId, DateTimeOffset At, DateTimeOffset PreviousDueAt,
    DateTimeOffset NewDueAt, string Reason, bool PreviousDeadlineBreached);
public sealed record H6ClockBreachFact(Guid Id, DateTimeOffset DueAt, DateTimeOffset ObservedAt);
public sealed record H6ClockFact(Guid Id, Guid ProjectId, string Kind, Guid TargetId, Guid OriginEventId,
    DateTimeOffset OriginAt, DateTimeOffset OriginalDueAt, DateTimeOffset CurrentDueAt, bool Overdue,
    DateTimeOffset? CompletedAt, DateTimeOffset? AcknowledgedAt, string Version,
    H6ClockExtensionFact[] Extensions, H6ClockBreachFact[] Breaches, string[] PendingCapabilities);
public sealed record H6ClockPageFact(string Status, H6ClockFact[] Items, H6ClockCursorFact? Continuation = null);
