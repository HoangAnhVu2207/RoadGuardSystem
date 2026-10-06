namespace RoadGuardSystem.BusinessObjects.Clocks;

public enum DeadlineClockKind : byte
{
    FastTrackExecution = 1, DeviceHandover = 2, FinishedDataSync = 3, ProjectManagerReview = 4,
    SupervisorInitialApproval = 5, SupervisorFinalConfirmation = 6, CrewSupplement = 7,
    SupervisorEscalation = 8, DangerAcknowledgment = 9, FirstSafetyCheck = 10
}

public sealed class DeadlineClock
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public DeadlineClockKind Kind { get; private set; }
    public Guid TargetId { get; private set; }
    public Guid OriginEventId { get; private set; }
    public DateTimeOffset OriginAt { get; private set; }
    public DateTimeOffset OriginalDueAt { get; private set; }
    public DateTimeOffset CurrentDueAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public Guid? AcknowledgedByUserId { get; private set; }
    public Guid? AcknowledgmentEventId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<DeadlineExtension> Extensions => _extensions;
    public IReadOnlyCollection<DeadlineBreach> Breaches => _breaches;
    private readonly List<DeadlineExtension> _extensions = [];
    private readonly List<DeadlineBreach> _breaches = [];
    private DeadlineClock() { }

    public static DeadlineClock Create(Guid id, Guid projectId, DeadlineClockKind kind, Guid targetId, Guid originEventId, DateTimeOffset originAt)
    {
        RequireId(id); RequireId(projectId); RequireId(targetId); RequireId(originEventId);
        if (originAt == default) throw new ArgumentException("A verified origin timestamp is required.", nameof(originAt));
        var hours = kind switch
        {
            DeadlineClockKind.DangerAcknowledgment => 1,
            DeadlineClockKind.SupervisorInitialApproval or DeadlineClockKind.SupervisorFinalConfirmation or DeadlineClockKind.CrewSupplement => 48,
            DeadlineClockKind.FastTrackExecution or DeadlineClockKind.DeviceHandover or DeadlineClockKind.FinishedDataSync
                or DeadlineClockKind.ProjectManagerReview or DeadlineClockKind.SupervisorEscalation or DeadlineClockKind.FirstSafetyCheck => 24,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var origin = originAt.ToUniversalTime();
        return new DeadlineClock { Id = id, ProjectId = projectId, Kind = kind, TargetId = targetId,
            OriginEventId = originEventId, OriginAt = origin, OriginalDueAt = origin.AddHours(hours), CurrentDueAt = origin.AddHours(hours) };
    }
    public bool IsOverdueAt(DateTimeOffset now) => CompletedAt is null && now.ToUniversalTime() >= CurrentDueAt;
    public bool ObserveBreach(Guid id, DateTimeOffset now)
    {
        RequireId(id);
        var at = now.ToUniversalTime();
        if (!IsOverdueAt(at) || _breaches.Any(x => x.DueAt == CurrentDueAt)) return false;
        _breaches.Add(new DeadlineBreach { Id = id, ClockId = Id, DueAt = CurrentDueAt, ObservedAt = at });
        return true;
    }
    public DeadlineExtension Extend(Guid id, Guid actor, DateTimeOffset due, string reason, DateTimeOffset now)
    {
        RequireId(id); RequireId(actor);
        if (CompletedAt is not null) throw new InvalidOperationException("A completed clock cannot be extended.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000) throw new ArgumentException("An extension reason is required.", nameof(reason));
        var at = now.ToUniversalTime(); var newDue = due.ToUniversalTime();
        if (at < OriginAt || newDue <= CurrentDueAt || newDue <= at) throw new ArgumentException("An extension must move the current deadline forward.", nameof(due));
        var previous = CurrentDueAt;
        var breached = at >= previous;
        if (breached) ObserveBreach(Guid.NewGuid(), at);
        var extension = new DeadlineExtension { Id = id, ClockId = Id, ActorUserId = actor, OccurredAt = at,
            PreviousDueAt = previous, NewDueAt = newDue, Reason = reason.Trim(), PreviousDeadlineBreached = breached };
        _extensions.Add(extension); CurrentDueAt = newDue;
        return extension;
    }
    public void Complete(DateTimeOffset at)
    {
        var completed = at.ToUniversalTime();
        if (CompletedAt == completed) return;
        if (CompletedAt is not null) throw new InvalidOperationException("Completion cannot be rewritten.");
        if (completed < OriginAt) throw new ArgumentException("Completion precedes the origin.", nameof(at));
        if (Kind == DeadlineClockKind.DangerAcknowledgment && AcknowledgedAt is null)
            throw new InvalidOperationException("A danger acknowledgment requires its own actor and event.");
        ObserveBreach(Guid.NewGuid(), completed);
        CompletedAt = completed;
    }
    public void Acknowledge(Guid eventId, Guid actor, DateTimeOffset at)
    {
        RequireId(eventId); RequireId(actor);
        if (Kind != DeadlineClockKind.DangerAcknowledgment) throw new InvalidOperationException("This clock is not an acknowledgment deadline.");
        if (AcknowledgmentEventId == eventId && AcknowledgedByUserId == actor && AcknowledgedAt == at.ToUniversalTime()) return;
        if (CompletedAt is not null || AcknowledgedAt is not null) throw new InvalidOperationException("An acknowledgment cannot be rewritten.");
        if (at.ToUniversalTime() < OriginAt) throw new ArgumentException("Acknowledgment precedes the warning.", nameof(at));
        AcknowledgmentEventId = eventId; AcknowledgedByUserId = actor; AcknowledgedAt = at.ToUniversalTime();
        Complete(at);
    }
    public static DateTimeOffset NextWeeklyReviewDigest(DateTimeOffset after)
    {
        // Assigned calendar is Asia/Ho_Chi_Minh. This computes the next occurrence;
        // it does not decide whether a missed occurrence should be dispatched.
        var local = after.ToOffset(TimeSpan.FromHours(7));
        var date = DateOnly.FromDateTime(local.DateTime);
        var days = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;
        var next = new DateTimeOffset(date.AddDays(days).ToDateTime(new TimeOnly(9, 0)), TimeSpan.FromHours(7));
        return (next <= local ? next.AddDays(7) : next).ToUniversalTime();
    }
    private static void RequireId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("An event, scope and actor identity must be explicit.", nameof(value));
    }
}

public sealed class DeadlineExtension
{
    public Guid Id { get; internal set; }
    public Guid ClockId { get; internal set; }
    public Guid ActorUserId { get; internal set; }
    public DateTimeOffset OccurredAt { get; internal set; }
    public DateTimeOffset PreviousDueAt { get; internal set; }
    public DateTimeOffset NewDueAt { get; internal set; }
    public string Reason { get; internal set; } = "";
    public bool PreviousDeadlineBreached { get; internal set; }
}

public sealed class DeadlineBreach
{
    public Guid Id { get; internal set; }
    public Guid ClockId { get; internal set; }
    public DateTimeOffset DueAt { get; internal set; }
    public DateTimeOffset ObservedAt { get; internal set; }
}
