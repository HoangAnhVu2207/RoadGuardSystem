namespace RoadGuardSystem.BusinessObjects.Repairs;

public sealed record SafetyResponsibilityTransfer(Guid Id, Guid PreviousActorId, Guid NextActorId, Guid ChangedBy,
    string Handover, string Reason, DateTimeOffset At)
{
    public Guid MeasureId { get; init; }
}
public sealed class TemporarySafetyMeasure
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid DefectId { get; private set; }
    public Guid FormalRepairObligationId { get; private set; }
    public Guid ResponsibleActorId { get; private set; }
    public Guid? CurrentResponsibilityTransferId { get; private set; }
    public string CheckSchedule { get; private set; } = "";
    public string ReplacementCondition { get; private set; } = "";
    public string RemovalCondition { get; private set; } = "";
    public Guid? InstallationEventId { get; private set; }
    public Guid? InstalledBy { get; private set; }
    public DateTimeOffset? InstalledAt { get; private set; }
    public DateTimeOffset? FirstCheckDueAt { get; private set; }
    public bool RequiresMonitoring => InstalledAt is not null;
    public IReadOnlyList<SafetyResponsibilityTransfer> Transfers => _transfers.AsReadOnly();
    private readonly List<SafetyResponsibilityTransfer> _transfers = [];
    private TemporarySafetyMeasure() { }
    public static TemporarySafetyMeasure Create(Guid id, Guid project, Guid defect, Guid formalObligation, Guid responsible,
        string schedule, string replacement, string removal)
    {
        RepairGuards.Id(id); RepairGuards.Id(project); RepairGuards.Id(defect); RepairGuards.Id(formalObligation); RepairGuards.Id(responsible);
        return new TemporarySafetyMeasure { Id = id, ProjectId = project, DefectId = defect, FormalRepairObligationId = formalObligation,
            ResponsibleActorId = responsible, CheckSchedule = RepairGuards.Text(schedule), ReplacementCondition = RepairGuards.Text(replacement),
            RemovalCondition = RepairGuards.Text(removal) };
    }
    public void Install(Guid eventId, Guid actor, DateTimeOffset at, DateTimeOffset firstCheckDue)
    {
        RepairGuards.Id(eventId); RepairGuards.Id(actor); var time = RepairGuards.Time(at); var due = RepairGuards.Time(firstCheckDue);
        if (due < time || due > time.AddHours(24)) throw new ArgumentException("First safety check must be scheduled within 24 elapsed hours.");
        if (InstallationEventId is not null)
        {
            if (InstallationEventId == eventId && InstalledBy == actor && InstalledAt == time && FirstCheckDueAt == due) return;
            throw new InvalidOperationException("Original installation cannot be rewritten.");
        }
        InstallationEventId = eventId; InstalledBy = actor; InstalledAt = time; FirstCheckDueAt = due;
    }
    // Caller establishes current project/action authority. No automatic substitute-role assignment.
    public void Transfer(Guid eventId, Guid next, Guid actor, string handover, string reason, DateTimeOffset at)
    {
        RepairGuards.Id(eventId); RepairGuards.Id(next); RepairGuards.Id(actor);
        var text = RepairGuards.Text(handover); var why = RepairGuards.Text(reason); var time = RepairGuards.Time(at);
        if (next == ResponsibleActorId || _transfers.Any(transfer => transfer.Id == eventId))
            throw new InvalidOperationException("Transfer needs a new responsible actor and distinct event.");
        if (time < InstalledAt || _transfers.Count > 0 && time < _transfers[^1].At)
            throw new ArgumentException("Responsibility transfer cannot predate existing history.");
        _transfers.Add(new(eventId, ResponsibleActorId, next, actor, text, why, time) { MeasureId = Id });
        ResponsibleActorId = next; CurrentResponsibilityTransferId = eventId;
    }
}
