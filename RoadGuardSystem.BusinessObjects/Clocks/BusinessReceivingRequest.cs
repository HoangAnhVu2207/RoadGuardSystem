using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Clocks;

public sealed class BusinessReceivingRequest
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public DeadlineClockKind Kind { get; private set; }
    public string SourceKind { get; private set; } = "";
    public Guid SourceId { get; private set; }
    public string SourceVersion { get; private set; } = "";
    public Guid ScopeId { get; private set; }
    public Guid? ResponsibleActorId { get; private set; }
    public UserRoleCode ResponsibleRole { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public Guid? AcknowledgmentId { get; private set; }
    public Guid? AcknowledgedBy { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public DateTimeOffset? ClaimedDeviceAt { get; private set; }
    public Guid? ClockId { get; private set; }
    public DeadlineClock? Clock { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    private readonly List<BusinessDutyAppointment> appointments = [];
    public IReadOnlyCollection<BusinessDutyAppointment> Appointments => appointments;
    private BusinessReceivingRequest() { }

    public static BusinessReceivingRequest Create(Guid id, Guid project, DeadlineClockKind kind,
        string sourceKind, Guid source, string version, Guid scope, Guid? responsible,
        UserRoleCode role, DateTimeOffset at)
    {
        if (id == Guid.Empty || project == Guid.Empty || source == Guid.Empty || scope == Guid.Empty || responsible == Guid.Empty ||
            string.IsNullOrWhiteSpace(version) || version.Length > 256 || string.IsNullOrWhiteSpace(sourceKind) ||
            sourceKind.Length > 64 || at == default ||
            !(kind == DeadlineClockKind.CrewSupplement && role == UserRoleCode.RepairCrew ||
              kind == DeadlineClockKind.SupervisorEscalation && role == UserRoleCode.Supervisor))
            throw new ArgumentException("A receiving request needs an exact business source, scope and role.");
        return new()
        {
            Id = id,
            ProjectId = project,
            Kind = kind,
            SourceKind = sourceKind,
            SourceId = source,
            SourceVersion = version,
            ScopeId = scope,
            ResponsibleActorId = responsible,
            ResponsibleRole = role,
            RequestedAt = at.ToUniversalTime()
        };
    }

    public void Appoint(Guid next, Guid actor, string reason, DateTimeOffset at)
    {
        if (next == Guid.Empty || actor == Guid.Empty || string.IsNullOrWhiteSpace(reason) || reason.Length > 2000)
            throw new ArgumentException("A current appointment requires actor, assignee and reason.");
        if (CompletedAt is not null || at < RequestedAt) throw new InvalidOperationException("Request is no longer appointable.");
        appointments.Add(new(Guid.NewGuid(), Id, ResponsibleActorId, next, actor, reason.Trim(), at.ToUniversalTime()));
        ResponsibleActorId = next;
    }

    public DeadlineClock Acknowledge(Guid actor, Guid acknowledgment, DateTimeOffset serverAt, DateTimeOffset? claimedAt)
    {
        if (actor == Guid.Empty || actor != ResponsibleActorId || CompletedAt is not null)
            throw new InvalidOperationException("Only the current responsible actor can receive this request.");
        if (Clock is not null) return Clock;
        if (acknowledgment == Guid.Empty || serverAt < RequestedAt) throw new ArgumentException("A valid server ACK is required.");
        AcknowledgmentId = acknowledgment; AcknowledgedBy = actor; AcknowledgedAt = serverAt.ToUniversalTime();
        ClaimedDeviceAt = claimedAt?.ToUniversalTime();
        Clock = DeadlineClock.Create(Guid.NewGuid(), ProjectId, Kind, Id, acknowledgment, AcknowledgedAt.Value);
        ClockId = Clock.Id;
        return Clock;
    }

    public void Complete(DateTimeOffset at)
    {
        if (CompletedAt is not null) return;
        if (at < RequestedAt) throw new ArgumentException("Completion precedes request.");
        Clock?.Complete(at); CompletedAt = at.ToUniversalTime();
    }
}

public sealed class BusinessDutyAppointment
{
    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid? PreviousActorId { get; private set; }
    public Guid CurrentActorId { get; private set; }
    public Guid DecisionActorId { get; private set; }
    public string Reason { get; private set; } = "";
    public DateTimeOffset EffectiveAt { get; private set; }
    private BusinessDutyAppointment() { }
    internal BusinessDutyAppointment(Guid id, Guid request, Guid? previous, Guid next, Guid actor, string reason, DateTimeOffset at)
    { Id = id; RequestId = request; PreviousActorId = previous; CurrentActorId = next; DecisionActorId = actor; Reason = reason; EffectiveAt = at; }
}
