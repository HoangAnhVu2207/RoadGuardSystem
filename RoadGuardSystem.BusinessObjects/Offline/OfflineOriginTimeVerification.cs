namespace RoadGuardSystem.BusinessObjects.Offline;

// Retained server-source proof. Signature integrity or client wall-clock claims cannot create this record.
public sealed class OfflineOriginTimeVerification
{
    private OfflineOriginTimeVerification() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid BindingId { get; private set; }
    public Guid CanonicalOriginId { get; private set; }
    public Guid TypedEffectId { get; private set; }
    public string ProofSourceKind { get; private set; } = "";
    public Guid ProofSourceId { get; private set; }
    public DateTimeOffset OriginalOccurredAtUtc { get; private set; }
    public Guid VerifiedBy { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }

    public static OfflineOriginTimeVerification Record(Guid id, Guid projectId, Guid bindingId,
        Guid canonicalOriginId, Guid typedEffectId, string proofSourceKind, Guid proofSourceId,
        DateTimeOffset originalOccurredAtUtc, Guid verifiedBy, DateTimeOffset recordedAtUtc)
    {
        OfflineRuntimeGuards.Identity(id, projectId, bindingId, canonicalOriginId, typedEffectId, proofSourceId, verifiedBy);
        OfflineRuntimeGuards.Time(originalOccurredAtUtc); OfflineRuntimeGuards.Time(recordedAtUtc);
        if (proofSourceKind is not ("FIELD_START_SERVER_ORIGIN" or "REPAIR_FINISH_SERVER_ORIGIN") ||
            originalOccurredAtUtc > recordedAtUtc)
            throw new ArgumentException("An independently retained original server event is required.");
        return new()
        {
            Id = id,
            ProjectId = projectId,
            BindingId = bindingId,
            CanonicalOriginId = canonicalOriginId,
            TypedEffectId = typedEffectId,
            ProofSourceKind = proofSourceKind,
            ProofSourceId = proofSourceId,
            OriginalOccurredAtUtc = originalOccurredAtUtc.ToUniversalTime(),
            VerifiedBy = verifiedBy,
            RecordedAtUtc = recordedAtUtc.ToUniversalTime()
        };
    }
}
