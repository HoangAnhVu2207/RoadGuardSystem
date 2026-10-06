using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Repairs;

public sealed record RepairPolicyDraftChange(Guid Id, Guid ActorId, DateTimeOffset At, string Reason,
    string DefectTypeCode, string ChecklistVersion, IReadOnlyList<RepairMeasurementRule> Measurements,
    IReadOnlyList<string> StopConditions);

public sealed class RepairPolicyDraft
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public IReadOnlyList<RepairPolicyDraftChange> Changes => _changes.AsReadOnly();
    private readonly List<RepairPolicyDraftChange> _changes = [];
    public RepairPolicyRevision? PublishedRevision { get; private set; }
    public Guid? CurrentChangeId { get; private set; }
    public Guid? PublishedRevisionId { get; private set; }
    private RepairPolicyDraft() { }
    public static RepairPolicyDraft Create(Guid id, Guid project, Guid eventId, Guid actor, UserRoleCode role,
        DateTimeOffset at, string reason, string defectType, string checklist, IReadOnlyList<RepairMeasurementRule> measurements,
        IReadOnlyList<string> stopConditions)
    {
        RepairGuards.Id(id); RepairGuards.Id(project);
        var draft = new RepairPolicyDraft { Id = id, ProjectId = project };
        draft.Append(eventId, actor, role, at, reason, defectType, checklist, measurements, stopConditions);
        return draft;
    }
    public void Update(Guid eventId, Guid expectedHead, Guid actor, UserRoleCode role, DateTimeOffset at, string reason,
        string defectType, string checklist, IReadOnlyList<RepairMeasurementRule> measurements, IReadOnlyList<string> stopConditions)
    {
        if (PublishedRevisionId is not null || CurrentChangeId != expectedHead || _changes.Any(change => change.Id == eventId))
            throw new InvalidOperationException("Draft changes require the unpublished current head and a distinct event.");
        if (at < Head.At) throw new ArgumentException("Draft history cannot be backdated.");
        Append(eventId, actor, role, at, reason, defectType, checklist, measurements, stopConditions);
    }
    public RepairPolicyRevision Publish(Guid revisionId, int revision, Guid actor, UserRoleCode role, DateTimeOffset at)
    {
        if (PublishedRevisionId is not null) throw new InvalidOperationException("Publication cannot be replaced.");
        var head = Head;
        if (at < head.At) throw new ArgumentException("Publication cannot predate its source draft.");
        PublishedRevision = RepairPolicyRevision.Publish(revisionId, ProjectId, revision, actor, role, at,
            head.DefectTypeCode, head.ChecklistVersion, head.Measurements, head.StopConditions);
        PublishedRevisionId = PublishedRevision.Id;
        return PublishedRevision;
    }
    private void Append(Guid eventId, Guid actor, UserRoleCode role, DateTimeOffset at, string reason,
        string defectType, string checklist, IReadOnlyList<RepairMeasurementRule> measurements, IReadOnlyList<string> stops)
    {
        RepairGuards.Id(eventId); var why = RepairGuards.Text(reason);
        // Reuse the publication invariant validator; this value is never exposed as a published grant.
        var facts = RepairPolicyRevision.Publish(eventId, ProjectId, 1, actor, role, at, defectType, checklist, measurements, stops);
        _changes.Add(new(eventId, actor, facts.PublishedAt, why, facts.DefectTypeCode, facts.ChecklistVersion, facts.Measurements, facts.StopConditions));
        CurrentChangeId = eventId;
    }
    private RepairPolicyDraftChange Head => _changes.Single(change => change.Id == CurrentChangeId);
}
