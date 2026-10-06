namespace RoadGuardSystem.BusinessObjects.Messaging;

public enum NotificationScopeDecision : byte { UnknownProtected = 1, Project = 2, ProvenNonProject = 3 }

public sealed class NotificationScopeAudit
{
    // Records a registered resolver's evidence. The factory does not prove the source relation or grant read permission.
    private NotificationScopeAudit() { }
    public Guid Id { get; private set; }
    public Guid NotificationId { get; private set; }
    public Guid? PreviousAuditId { get; private set; }
    public string ResolverVersion { get; private set; } = "";
    public NotificationScopeDecision Decision { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string EvidenceSourceType { get; private set; } = "";
    public Guid EvidenceSourceId { get; private set; }
    public string ReasonCode { get; private set; } = "";
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public static NotificationScopeAudit Record(Guid id, Guid notificationId, Guid? previousAuditId, string resolverVersion,
        NotificationScopeDecision decision, Guid? projectId, string evidenceSourceType, Guid evidenceSourceId,
        string reasonCode, DateTimeOffset recordedAtUtc)
    {
        NotificationDomainGuard.Id(id); NotificationDomainGuard.Id(notificationId);
        NotificationDomainGuard.OptionalId(previousAuditId); NotificationDomainGuard.OptionalId(projectId);
        NotificationDomainGuard.Id(evidenceSourceId); NotificationDomainGuard.Timestamp(recordedAtUtc);
        if (!Enum.IsDefined(decision)) throw new ArgumentOutOfRangeException(nameof(decision));
        if (previousAuditId == id) throw new ArgumentException("An audit cannot precede itself.", nameof(previousAuditId));
        ArgumentException.ThrowIfNullOrWhiteSpace(resolverVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceSourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        if (resolverVersion.Length > 100 || evidenceSourceType.Length > 80 || reasonCode.Length > 80
            || reasonCode.Any(x => !(x is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')))
            throw new ArgumentException("Audit resolver, source and reason identifiers must be bounded.");
        if ((decision == NotificationScopeDecision.Project) != projectId.HasValue)
            throw new ArgumentException("Only a resolved project decision carries a project identity.", nameof(projectId));
        return new NotificationScopeAudit
        {
            Id = id,
            NotificationId = notificationId,
            PreviousAuditId = previousAuditId,
            ResolverVersion = resolverVersion.Trim(),
            Decision = decision,
            ProjectId = projectId,
            EvidenceSourceType = evidenceSourceType.Trim(),
            EvidenceSourceId = evidenceSourceId,
            ReasonCode = reasonCode,
            RecordedAtUtc = recordedAtUtc.ToUniversalTime()
        };
    }
}
