namespace RoadGuardSystem.BusinessObjects.Inspections;
public sealed class FieldInspectionOperationOrigin
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid OriginId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public int SchemaVersion { get; private set; } = 1;
    public string ContentHash { get; private set; } = string.Empty;
    public Guid OriginalActorId { get; private set; }
    public Guid? DeviceId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid EffectId { get; private set; }
    public DateTimeOffset ServerReceivedAt { get; private set; }
    private FieldInspectionOperationOrigin() { }
    public static FieldInspectionOperationOrigin Create(Guid id, Guid project, Guid origin, string kind, string hash,
        Guid originalActor, Guid? device, Guid task, Guid effect, DateTimeOffset received)
    {
        if(new[] {id,project,origin,originalActor,task,effect}.Any(x=>x==Guid.Empty) || device==Guid.Empty ||
            kind is not ("FIELD_START" or "FIELD_SUBMISSION" or "FIELD_ACCEPT" or "REPAIR_ASSESSMENT" or
                "REPAIR_EXECUTION_START" or "REPAIR_EXECUTION_FINISH") || string.IsNullOrEmpty(hash) || hash.Length!=64 || hash.Any(x=>!Uri.IsHexDigit(x)) || received==default)
            throw new ArgumentException("Finite canonical FIELD origin required.");
        return new FieldInspectionOperationOrigin { Id=id,ProjectId=project,OriginId=origin,Kind=kind,ContentHash=hash.ToLowerInvariant(),
            OriginalActorId=originalActor,DeviceId=device,TaskId=task,EffectId=effect,ServerReceivedAt=received.ToUniversalTime() };
    }
}
