namespace RoadGuardSystem.BusinessObjects.Messaging;

// This finite domain catalog is a producer seam, not a registered dispatcher or adopted public wire contract.
public enum NotificationEventKind : byte
{
    FieldAssigned = 1, FieldSubmitted = 2, FieldSupplementRequested = 3,
    RepairAssigned = 4, RepairSubmitted = 5, RepairReworkRequested = 6,
    SupervisorApprovalRequired = 7, DeadlineBreached = 8, SafetyWarning = 9,
    SafetyInspectionDue = 10, WeeklyReviewPending = 11,
    ProjectObligationTransferred = 12, ProjectHandlingRenewed = 13, FastTrackConfirmedInformation = 14,
    SafetyMeasureAssigned = 15
}

public enum NotificationSourceKind : byte
{
    FieldTask = 1, RepairWork = 2, DeadlineClock = 3, TemporarySafetyMeasure = 4,
    ReviewObligation = 5, ProjectLifecycle = 6
}

public enum NotificationRecipientStrategy : byte
{
    AssignedCrew = 1, ProjectManager = 2, Supervisor = 3, ResponsibleActorAndSupervisor = 4,
    ResponsibleReviewer = 5, ExplicitResponsibleActors = 6
}

public sealed class NotificationEventEnvelope
{
    private NotificationEventEnvelope() { }
    public int SchemaVersion { get; private set; } = 1;
    public Guid EventId { get; private set; }
    public NotificationEventKind Kind { get; private set; }
    public Guid ProjectId { get; private set; }
    public NotificationSourceKind SourceKind { get; private set; }
    public Guid SourceId { get; private set; }
    public Guid OriginEventId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public Guid? SourceRevisionId { get; private set; }
    public Guid? ResponsibleUserId { get; private set; }
    public string MessageType => Kind switch
    {
        NotificationEventKind.FieldAssigned => "field.task.assigned.v1",
        NotificationEventKind.FieldSubmitted => "field.task.submitted.v1",
        NotificationEventKind.FieldSupplementRequested => "field.task.supplement_requested.v1",
        NotificationEventKind.RepairAssigned => "repair.work.assigned.v1",
        NotificationEventKind.RepairSubmitted => "repair.work.submitted.v1",
        NotificationEventKind.RepairReworkRequested => "repair.work.rework_requested.v1",
        NotificationEventKind.FastTrackConfirmedInformation => "repair.work.fast_track_confirmed.v1",
        NotificationEventKind.SupervisorApprovalRequired => "review.supervisor_required.v1",
        NotificationEventKind.DeadlineBreached => "deadline.breached.v1",
        NotificationEventKind.SafetyWarning => "safety.warning.v1",
        NotificationEventKind.SafetyMeasureAssigned => "safety.measure_assigned.v1",
        NotificationEventKind.SafetyInspectionDue => "safety.inspection_due.v1",
        NotificationEventKind.WeeklyReviewPending => "review.weekly_pending.v1",
        NotificationEventKind.ProjectObligationTransferred => "project.obligation_transferred.v1",
        NotificationEventKind.ProjectHandlingRenewed => "project.handling_renewed.v1",
        _ => throw new InvalidOperationException("Unknown notification event kind.")
    };
    public NotificationRecipientStrategy RecipientStrategy => Kind switch
    {
        NotificationEventKind.FieldAssigned or NotificationEventKind.FieldSupplementRequested
            or NotificationEventKind.RepairAssigned or NotificationEventKind.RepairReworkRequested => NotificationRecipientStrategy.AssignedCrew,
        NotificationEventKind.FieldSubmitted or NotificationEventKind.RepairSubmitted => NotificationRecipientStrategy.ProjectManager,
        NotificationEventKind.SupervisorApprovalRequired or NotificationEventKind.FastTrackConfirmedInformation or
            NotificationEventKind.SafetyMeasureAssigned => NotificationRecipientStrategy.Supervisor,
        NotificationEventKind.DeadlineBreached or NotificationEventKind.SafetyWarning or NotificationEventKind.SafetyInspectionDue
            => NotificationRecipientStrategy.ResponsibleActorAndSupervisor,
        NotificationEventKind.WeeklyReviewPending => NotificationRecipientStrategy.ResponsibleReviewer,
        NotificationEventKind.ProjectObligationTransferred or NotificationEventKind.ProjectHandlingRenewed => NotificationRecipientStrategy.ExplicitResponsibleActors,
        _ => throw new InvalidOperationException("Unknown notification event kind.")
    };
    public static NotificationEventEnvelope Create(Guid eventId, NotificationEventKind kind, Guid projectId,
        NotificationSourceKind sourceKind, Guid sourceId, Guid originEventId, DateTimeOffset occurredAtUtc,
        Guid? sourceRevisionId = null, Guid? responsibleUserId = null)
    {
        NotificationDomainGuard.Id(eventId); NotificationDomainGuard.Id(projectId);
        NotificationDomainGuard.Id(sourceId); NotificationDomainGuard.Id(originEventId);
        NotificationDomainGuard.OptionalId(sourceRevisionId); NotificationDomainGuard.OptionalId(responsibleUserId);
        NotificationDomainGuard.Timestamp(occurredAtUtc);
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (!Enum.IsDefined(sourceKind)) throw new ArgumentOutOfRangeException(nameof(sourceKind));
        var allowed = kind switch
        {
            NotificationEventKind.FieldAssigned or NotificationEventKind.FieldSubmitted or NotificationEventKind.FieldSupplementRequested
                => sourceKind == NotificationSourceKind.FieldTask,
            NotificationEventKind.RepairAssigned or NotificationEventKind.RepairSubmitted or NotificationEventKind.RepairReworkRequested or NotificationEventKind.FastTrackConfirmedInformation
                => sourceKind == NotificationSourceKind.RepairWork,
            NotificationEventKind.SupervisorApprovalRequired => sourceKind is NotificationSourceKind.FieldTask or NotificationSourceKind.RepairWork or NotificationSourceKind.ReviewObligation,
            NotificationEventKind.DeadlineBreached => sourceKind == NotificationSourceKind.DeadlineClock,
            NotificationEventKind.SafetyWarning or NotificationEventKind.SafetyInspectionDue or
                NotificationEventKind.SafetyMeasureAssigned => sourceKind == NotificationSourceKind.TemporarySafetyMeasure,
            NotificationEventKind.WeeklyReviewPending => sourceKind == NotificationSourceKind.ReviewObligation,
            NotificationEventKind.ProjectObligationTransferred or NotificationEventKind.ProjectHandlingRenewed => sourceKind == NotificationSourceKind.ProjectLifecycle,
            _ => false
        };
        if (!allowed) throw new ArgumentException("The event has no resolver for this source kind.", nameof(sourceKind));
        if (kind is NotificationEventKind.FieldSubmitted or NotificationEventKind.FieldSupplementRequested
            or NotificationEventKind.RepairSubmitted or NotificationEventKind.RepairReworkRequested or NotificationEventKind.FastTrackConfirmedInformation
            && sourceRevisionId is null)
            throw new ArgumentException("Submission and review-request events require an immutable source revision.", nameof(sourceRevisionId));
        return new NotificationEventEnvelope
        {
            EventId = eventId,
            Kind = kind,
            ProjectId = projectId,
            SourceKind = sourceKind,
            SourceId = sourceId,
            OriginEventId = originEventId,
            OccurredAtUtc = occurredAtUtc.ToUniversalTime(),
            SourceRevisionId = sourceRevisionId,
            ResponsibleUserId = responsibleUserId
        };
    }
}
