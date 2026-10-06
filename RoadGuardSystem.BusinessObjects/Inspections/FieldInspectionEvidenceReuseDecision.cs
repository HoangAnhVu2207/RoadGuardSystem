namespace RoadGuardSystem.BusinessObjects.Inspections;
public sealed class FieldInspectionEvidenceReuseDecision
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid FileId { get; private set; }
    public Guid SourceEvidenceId { get; private set; }
    public Guid ActorId { get; private set; }
    public string SourceKind { get; private set; } = string.Empty;
    public string FileChecksum { get; private set; } = string.Empty;
    public string ProvenanceJson { get; private set; } = "{}";
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    private FieldInspectionEvidenceReuseDecision() { }
    public static FieldInspectionEvidenceReuseDecision Create(Guid id, Guid project, Guid task, Guid file, Guid sourceEvidence,
        Guid actor, string source, string checksum, string provenance, string reason, DateTimeOffset at)
    {
        if (new[] {id,project,task,file,sourceEvidence,actor}.Any(x => x == Guid.Empty) || source is not ("REPORTER" or "DRONE") ||
            string.IsNullOrEmpty(checksum) || checksum.Length != 64 || checksum.Any(x => !Uri.IsHexDigit(x)) || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000)
            throw new ArgumentException("Explicit authorized BEFORE reuse provenance required.");
        using var document=System.Text.Json.JsonDocument.Parse(provenance);
        return new FieldInspectionEvidenceReuseDecision {Id=id,ProjectId=project,TaskId=task,FileId=file,SourceEvidenceId=sourceEvidence,ActorId=actor,
            SourceKind=source,FileChecksum=checksum.ToLowerInvariant(),ProvenanceJson=provenance,Reason=reason.Trim(),OccurredAt=at.ToUniversalTime()};
    }
}
