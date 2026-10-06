namespace RoadGuardSystem.BusinessObjects.Inspections;

public sealed class FieldInspectionSubmission
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid RootId { get; private set; }
    public Guid? ParentId { get; private set; }
    public int Revision { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid StartOriginId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid OriginId { get; private set; }
    public Guid OperationOriginId { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public Guid OriginalActorId { get; private set; }
    public DateTimeOffset ServerReceivedAt { get; private set; }
    public string PayloadJson { get; private set; } = "{}";
    public string Readiness { get; private set; } = "INCOMPLETE";
    public string MissingReasonsJson { get; private set; } = "[]";
    private FieldInspectionSubmission() { }
    public static FieldInspectionSubmission Create(Guid id, Guid project, Guid task, Guid root, Guid? parent, int revision,
        Guid assignment, Guid start, Guid session, Guid origin, string hash, Guid actor, DateTimeOffset received,
        string payload, string readiness, string missingReasons)
    {
        if (new[] {id,project,task,root,assignment,start,session,origin,actor}.Any(x => x == Guid.Empty) ||
            parent == Guid.Empty || revision < 1 || (revision == 1) != (parent is null) ||
            revision == 1 && id != root || string.IsNullOrEmpty(hash) || hash.Length != 64 || hash.Any(x => !Uri.IsHexDigit(x)) ||
            readiness is not ("READY" or "INCOMPLETE") || received == default)
            throw new ArgumentException("Immutable intake lineage and readiness are required.");
        using var document = System.Text.Json.JsonDocument.Parse(payload);
        using var reasons = System.Text.Json.JsonDocument.Parse(missingReasons);
        if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object ||
            reasons.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) throw new ArgumentException("Invalid intake facts.");
        return new FieldInspectionSubmission { Id=id,ProjectId=project,TaskId=task,RootId=root,ParentId=parent,Revision=revision,
            AssignmentId=assignment,StartOriginId=start,SessionId=session,OriginId=origin,OperationOriginId=id,ContentHash=hash.ToLowerInvariant(),
            OriginalActorId=actor,ServerReceivedAt=received.ToUniversalTime(),PayloadJson=payload,Readiness=readiness,MissingReasonsJson=missingReasons };
    }
}
