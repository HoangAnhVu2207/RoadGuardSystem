namespace RoadGuardSystem.BusinessObjects.Inspections;

public sealed class FieldInspectionLocationProof
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid SubmissionId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string FactsJson { get; private set; } = "{}";
    public string VerificationState { get; private set; } = "CLAIMED";
    private FieldInspectionLocationProof() { }
    public static FieldInspectionLocationProof Create(Guid id, Guid project, Guid task, Guid submission, string kind, string facts)
    {
        if (new[] { id, project, task, submission }.Any(x => x == Guid.Empty) || kind is not ("GPS_CAPTURE" or "POSITION_CHECKLIST" or "UNKNOWN"))
            throw new ArgumentException("Typed location proof required.");
        using var document = System.Text.Json.JsonDocument.Parse(facts);
        if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) throw new ArgumentException("Location facts must be object.");
        return new FieldInspectionLocationProof { Id = id, ProjectId = project, TaskId = task, SubmissionId = submission, Kind = kind, FactsJson = facts };
    }
}
