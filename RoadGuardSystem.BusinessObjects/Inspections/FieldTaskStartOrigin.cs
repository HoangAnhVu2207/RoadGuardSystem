namespace RoadGuardSystem.BusinessObjects.Inspections;

public sealed class FieldTaskStartOrigin
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid OriginId { get; private set; }
    public Guid OperationOriginId { get; private set; }
    public string OperationKind { get; private set; } = "FIELD_START";
    public string ContentHash { get; private set; } = string.Empty;
    public Guid OriginalActorId { get; private set; }
    public Guid? DeviceId { get; private set; }
    public DateTimeOffset ClaimedAt { get; private set; }
    public long? MonotonicMilliseconds { get; private set; }
    public string? BootId { get; private set; }
    public DateTimeOffset ServerReceivedAt { get; private set; }
    public DateTimeOffset? VerifiedOriginalAt { get; private set; }
    public string TimeProvenance { get; private set; } = "CLAIMED_OFFLINE";
    public Guid RouteVersionId { get; private set; }
    public Guid? SegmentSetId { get; private set; }
    public Guid? LayoutRevisionId { get; private set; }
    public Guid? MapPublicationId { get; private set; }
    public Guid? CrsProfileRevisionId { get; private set; }
    public string? SlabId { get; private set; }
    public string LocationPolicyVersion { get; private set; } = "FIELD_LOCATION_V1";
    public string ClaimEvidenceJson { get; private set; } = "{}";
    private FieldTaskStartOrigin() { }
    public static FieldTaskStartOrigin Create(Guid id, Guid project, Guid task, Guid assignment, Guid origin,
        string hash, Guid originalActor, Guid? device, DateTimeOffset claimedAt, long? monotonic,
        string? boot, DateTimeOffset receivedAt, Guid route, Guid? set, Guid? layout, bool trustedOnlineAdmission, Guid? mapPublication = null, Guid? profile = null, string? slab = null, string claimEvidenceJson = "{}")
    {
        if (new[] { id, project, task, assignment, origin, originalActor, route }.Any(x => x == Guid.Empty) ||
            device == Guid.Empty || set == Guid.Empty || layout == Guid.Empty || mapPublication == Guid.Empty || profile == Guid.Empty || slab?.Length > 160 || string.IsNullOrEmpty(hash) || hash.Length != 64 ||
            hash.Any(x => !Uri.IsHexDigit(x)) || claimedAt == default || receivedAt == default || monotonic < 0 ||
            boot?.Length > 200) throw new ArgumentException("Explicit bounded first-start provenance is required.");
        using var proof = System.Text.Json.JsonDocument.Parse(claimEvidenceJson);
        if (proof.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) throw new ArgumentException("Claim evidence must be an object.");
        return new FieldTaskStartOrigin
        {
            Id = id,
            ProjectId = project,
            TaskId = task,
            AssignmentId = assignment,
            OriginId = origin,
            OperationOriginId = id,
            ContentHash = hash.ToLowerInvariant(),
            OriginalActorId = originalActor,
            DeviceId = device,
            ClaimedAt = claimedAt.ToUniversalTime(),
            MonotonicMilliseconds = monotonic,
            BootId = boot,
            ServerReceivedAt = receivedAt.ToUniversalTime(),
            VerifiedOriginalAt = trustedOnlineAdmission ? receivedAt.ToUniversalTime() : null,
            TimeProvenance = trustedOnlineAdmission ? "SERVER_ONLINE" : "CLAIMED_OFFLINE",
            RouteVersionId = route,
            SegmentSetId = set,
            LayoutRevisionId = layout,
            MapPublicationId = mapPublication,
            CrsProfileRevisionId = profile,
            SlabId = slab,
            ClaimEvidenceJson = claimEvidenceJson
        };
    }
}
