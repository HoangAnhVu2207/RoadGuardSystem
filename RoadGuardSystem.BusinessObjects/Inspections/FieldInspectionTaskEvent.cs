namespace RoadGuardSystem.BusinessObjects.Inspections;
public sealed class FieldInspectionTaskEvent
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid? AssignmentId { get; private set; }
    public Guid ActorId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public string FactsJson { get; private set; } = "{}";
    public Guid? LocationImpactId { get; private set; }
    public Guid? LocationImpactDecisionId { get; private set; }
    private FieldInspectionTaskEvent() { }
    public static FieldInspectionTaskEvent Create(Guid id, Guid project, Guid task, Guid? assignment, Guid actor, string kind, string reason, DateTimeOffset at, string factsJson = "{}", Guid? impactId = null, Guid? impactDecisionId = null)
    {
        if (new[] {id,project,task,actor}.Any(x => x == Guid.Empty) || assignment == Guid.Empty ||
            kind is not ("CREATED" or "ASSIGNED" or "ACCEPTED" or "REJECTED" or "STARTED" or "SUBMITTED" or "SUPPLEMENT" or "REVIEWED" or "CANCELLED" or "REASSIGNED" or "IMPACT_CONTINUE" or "IMPACT_VERIFY") ||
            string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000) throw new ArgumentException("Explicit bounded task event required.");
        if(impactId==Guid.Empty || impactDecisionId==Guid.Empty || impactDecisionId.HasValue!=impactId.HasValue)throw new ArgumentException("Impact relation must be explicit.");
        using var facts=System.Text.Json.JsonDocument.Parse(factsJson);
        if(facts.RootElement.ValueKind!=System.Text.Json.JsonValueKind.Object)throw new ArgumentException("Task facts must be object.");
        return new FieldInspectionTaskEvent { Id=id,ProjectId=project,TaskId=task,AssignmentId=assignment,ActorId=actor,Kind=kind,Reason=reason.Trim(),OccurredAt=at.ToUniversalTime(),FactsJson=factsJson,LocationImpactId=impactId,LocationImpactDecisionId=impactDecisionId };
    }
}
