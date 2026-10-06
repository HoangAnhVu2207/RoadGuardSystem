using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Offline;

public sealed class OfflineEncryptedPackageRecord
{
    private OfflineEncryptedPackageRecord() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid OriginalActorId { get; private set; }
    public Guid SourceDeviceRegistrationId { get; private set; }
    public Guid SourceBatchId { get; private set; }
    public string CipherPackageJson { get; private set; } = "{}";
    public string SignedManifestJson { get; private set; } = "{}";
    public string SourceSignature { get; private set; } = "";
    public string ManifestHash { get; private set; } = "";
    public string PayloadHash { get; private set; } = "";
    public string PackageFingerprint { get; private set; } = "";
    public Guid RegisteredBy { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }
    public string RegistrationMode { get; private set; } = "";
    public static OfflineEncryptedPackageRecord RegisterPrepared(Guid id, Guid project, Guid originalActor,
        Guid sourceRegistration, Guid sourceBatch, Guid registeredBy, UserRoleCode registrantRole,
        string cipherJson, string manifestJson, string signature, string payloadHash,
        string packageFingerprint, DateTimeOffset registeredAt)
    {
        OfflineRuntimeGuards.Identity(registeredBy);
        if (registrantRole != UserRoleCode.Supervisor)
            throw new ArgumentException("Only the current project Supervisor may prepare a source package.");
        var row = Capture(id, project, originalActor, sourceRegistration, sourceBatch, cipherJson,
            manifestJson, signature, payloadHash, packageFingerprint, registeredAt);
        row.RegisteredBy = registeredBy;
        row.RegistrationMode = "SUPERVISOR_PREPARED";
        return row;
    }
    public static OfflineEncryptedPackageRecord Capture(Guid id, Guid project, Guid originalActor,
        Guid sourceRegistration, Guid sourceBatch, string cipherJson, string manifestJson, string signature,
        string payloadHash, string packageFingerprint, DateTimeOffset at)
    {
        OfflineRuntimeGuards.Identity(id,project,originalActor,sourceRegistration,sourceBatch);
        OfflineRuntimeGuards.Time(at);OfflineRuntimeGuards.Hash(payloadHash);OfflineRuntimeGuards.Hash(packageFingerprint);
        OfflineRuntimeGuards.Json(cipherJson,24*1024*1024);OfflineRuntimeGuards.Json(manifestJson,1048576);
        try {if(Convert.FromBase64String(signature).Length!=64)throw new ArgumentException("A bounded source signature is required.");}
        catch(FormatException exception){throw new ArgumentException("Source signature is malformed.",exception);}
        return new(){Id=id,ProjectId=project,OriginalActorId=originalActor,SourceDeviceRegistrationId=sourceRegistration,
            SourceBatchId=sourceBatch,CipherPackageJson=cipherJson,SignedManifestJson=manifestJson,SourceSignature=signature,
            ManifestHash=OfflineRuntimeGuards.Digest(manifestJson),PayloadHash=payloadHash.ToLowerInvariant(),
            PackageFingerprint=packageFingerprint.ToLowerInvariant(),RegisteredAt=at.ToUniversalTime(),
            RegisteredBy=originalActor,RegistrationMode="SELF_EXPORT"};
    }
}
