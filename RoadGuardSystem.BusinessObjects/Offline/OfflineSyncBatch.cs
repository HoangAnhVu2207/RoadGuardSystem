namespace RoadGuardSystem.BusinessObjects.Offline;

public sealed class OfflineSyncBatch
{
    private OfflineSyncBatch() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid SourceBatchId { get; private set; }
    public Guid SourceDeviceRegistrationId { get; private set; }
    public Guid CurrentImporterId { get; private set; }
    public Guid? PackageId { get; private set; }
    public Guid? GrantId { get; private set; }
    public Guid? RecipientDeviceRegistrationId { get; private set; }
    public string? RecipientSignature { get; private set; }
    public string SignedDescriptorJson { get; private set; } = "{}";
    public string SourceSignature { get; private set; } = "";
    public string ContentHash { get; private set; } = "";
    // Null is explicit legacy/foundation absence; it cannot authorize a payload-authenticated import.
    public string? AttachedPayloadJson { get; private set; }
    public string? AttachedPayloadHash { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public static OfflineSyncBatch ReceiveAuthenticated(Guid id, Guid project, Guid sourceBatch, Guid sourceRegistration,
        Guid importer, Guid? package, Guid? grant, string descriptorJson, string signature, string canonicalAttachedPayloadJson,
        DateTimeOffset at, Guid? recipientRegistration = null, string? recipientSignature = null)
    {
        OfflineRuntimeGuards.Json(canonicalAttachedPayloadJson, OfflineCryptoFormat.MaxPayloadBytes);
        var batch = Receive(id, project, sourceBatch, sourceRegistration, importer, package, grant, descriptorJson,
            signature, at, recipientRegistration, recipientSignature);
        batch.AttachedPayloadJson = canonicalAttachedPayloadJson;
        batch.AttachedPayloadHash = OfflineRuntimeGuards.Digest(canonicalAttachedPayloadJson);
        return batch;
    }
    public static OfflineSyncBatch Receive(Guid id, Guid project, Guid sourceBatch, Guid sourceRegistration,
        Guid importer, Guid? package, Guid? grant, string descriptorJson, string signature, DateTimeOffset at,
        Guid? recipientRegistration = null, string? recipientSignature = null)
    {
        OfflineRuntimeGuards.Identity(id, project, sourceBatch, sourceRegistration, importer);
        OfflineRuntimeGuards.Time(at); OfflineRuntimeGuards.Json(descriptorJson, 1048576);
        if (package.HasValue != grant.HasValue || package == Guid.Empty || grant == Guid.Empty)
            throw new ArgumentException("A package and grant must be bound together.");
        if (recipientRegistration == Guid.Empty || package.HasValue != recipientRegistration.HasValue ||
            package.HasValue != (recipientSignature is not null))
            throw new ArgumentException("Handover batches require their exact recipient registration and signature.");
        try { if (Convert.FromBase64String(signature).Length != 64) throw new ArgumentException("A P256 source signature is required."); }
        catch (FormatException exception) { throw new ArgumentException("Source signature is malformed.", exception); }
        if (recipientSignature is not null)
        {
            try
            {
                var bytes = Convert.FromBase64String(recipientSignature);
                if (bytes.Length != 64 || Convert.ToBase64String(bytes) != recipientSignature)
                    throw new ArgumentException("A canonical P256 recipient signature is required.");
            }
            catch (FormatException exception) { throw new ArgumentException("Recipient signature is malformed.", exception); }
        }
        return new()
        {
            Id = id,
            ProjectId = project,
            SourceBatchId = sourceBatch,
            SourceDeviceRegistrationId = sourceRegistration,
            CurrentImporterId = importer,
            PackageId = package,
            GrantId = grant,
            RecipientDeviceRegistrationId = recipientRegistration,
            RecipientSignature = recipientSignature,
            SignedDescriptorJson = descriptorJson,
            SourceSignature = signature,
            ContentHash = OfflineRuntimeGuards.Digest(descriptorJson),
            ReceivedAt = at.ToUniversalTime()
        };
    }
}
