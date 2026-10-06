namespace RoadGuardSystem.BusinessObjects.Offline;

public sealed class OfflineHandoverGrantRevocation
{
    private OfflineHandoverGrantRevocation() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid GrantId { get; private set; }
    public Guid RevokedBy { get; private set; }
    public DateTimeOffset RevokedAt { get; private set; }
    public string Reason { get; private set; } = "";
    public static OfflineHandoverGrantRevocation Record(Guid id, Guid project, Guid grant, Guid actor,
        string reason, DateTimeOffset at)
    {
        OfflineRuntimeGuards.Identity(id, project, grant, actor); OfflineRuntimeGuards.Time(at);
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 2000) throw new ArgumentException("Revocation reason is required.");
        return new() { Id = id, ProjectId = project, GrantId = grant, RevokedBy = actor, RevokedAt = at.ToUniversalTime(), Reason = reason.Trim() };
    }
}
