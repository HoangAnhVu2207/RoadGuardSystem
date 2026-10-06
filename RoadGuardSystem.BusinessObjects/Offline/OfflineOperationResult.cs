namespace RoadGuardSystem.BusinessObjects.Offline;

// Append-only per-item attempts preserve partial/conflicting imports without replacing an earlier result.
public sealed class OfflineOperationResult
{
    private OfflineOperationResult() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid BatchId { get; private set; }
    public Guid AdmissionId { get; private set; }
    public Guid OriginId { get; private set; }
    public Guid? EffectId { get; private set; }
    public string State { get; private set; } = "";
    public string? Code { get; private set; }
    public bool DurableAck { get; private set; }
    public string TimeProvenance { get; private set; } = "";
    public string SyncLateness { get; private set; } = "UNKNOWN";
    public DateTimeOffset? ClaimedFinishedAt { get; private set; }
    public DateTimeOffset? VerifiedFinishedAt { get; private set; }
    public string OutcomeJson { get; private set; } = "{}";
    public string? ResourceVersion { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public static OfflineOperationResult Record(Guid id, Guid project, Guid batch, Guid admission, Guid origin,
        Guid? effect, string state, string? code, string timeProvenance, string syncLateness,
        DateTimeOffset? claimedFinish, DateTimeOffset? verifiedFinish, string outcomeJson, string? version, DateTimeOffset at)
    {
        OfflineRuntimeGuards.Identity(id, project, batch, admission, origin); OfflineRuntimeGuards.Time(at);
        OfflineRuntimeGuards.Json(outcomeJson, 1048576);
        if (state is not ("COMMITTED" or "REPLAYED" or "CONFLICT" or "PENDING_DEPENDENCY" or "REJECTED" or "STALE_SNAPSHOT") ||
            timeProvenance is not ("UNCERTAIN" or "VERIFIED_ORIGINAL") || syncLateness is not ("UNKNOWN" or "ON_TIME" or "LATE") ||
            verifiedFinish is null && syncLateness != "UNKNOWN" || effect == Guid.Empty || code?.Length > 150)
            throw new ArgumentException("A finite truthful item result is required.");
        var acknowledged = state is "COMMITTED" or "REPLAYED";
        if (acknowledged && effect is null) throw new ArgumentException("Acknowledged effects must be durable.");
        return new() { Id=id, ProjectId=project, BatchId=batch, AdmissionId=admission, OriginId=origin,
            EffectId=effect, State=state, Code=code, DurableAck=acknowledged, TimeProvenance=timeProvenance,
            SyncLateness=syncLateness, ClaimedFinishedAt=claimedFinish, VerifiedFinishedAt=verifiedFinish,
            OutcomeJson=outcomeJson, ResourceVersion=version, RecordedAt=at.ToUniversalTime() };
    }
}
