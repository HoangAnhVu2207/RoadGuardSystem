using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Offline;

// Retains encrypted bytes and signed claims only. Registration is not a plaintext/capture-time proof.
public sealed class OfflineEncryptedCaptureArtifact
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private OfflineEncryptedCaptureArtifact() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid ParentPackageId { get; private set; }
    public Guid CaptureOriginId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid OriginalActorId { get; private set; }
    public Guid SourceDeviceRegistrationId { get; private set; }
    public int ChunkIndex { get; private set; }
    public long ChunkOffset { get; private set; }
    public int ChunkLength { get; private set; }
    public string PlaintextChecksum { get; private set; } = "";
    public string EnvelopeFingerprint { get; private set; } = "";
    public string ManifestHash { get; private set; } = "";
    public string SignedManifestJson { get; private set; } = "{}";
    public string ManifestSignature { get; private set; } = "";
    public string CipherEnvelopeJson { get; private set; } = "{}";
    public Guid RegisteredBy { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }

    public static OfflineEncryptedCaptureArtifact Register(OfflineEncryptedPackageRecord parent,
        OfflineDeviceRegistration source, OfflineCaptureManifest manifest, string manifestSignature, int chunkIndex,
        OfflineEncryptedPackage encryptedChunk, Guid currentRegistrar, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(parent); ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(manifest); ArgumentNullException.ThrowIfNull(manifest.Chunks);
        OfflineRuntimeGuards.Identity(currentRegistrar); OfflineRuntimeGuards.Time(at);
        // Snapshot mutable transport collections before validation and persistence.
        manifest = manifest with { Chunks = manifest.Chunks.ToArray() };
        var canonical = OfflineCaptureArtifacts.CanonicalManifest(manifest);
        if (manifest.ParentPackageId != parent.Id || manifest.ParentManifestHash != parent.ManifestHash ||
            manifest.ProjectId != parent.ProjectId || manifest.OriginalActorId != parent.OriginalActorId ||
            manifest.SourceDeviceRegistrationId != parent.SourceDeviceRegistrationId || source.Id != parent.SourceDeviceRegistrationId ||
            source.ProjectId != parent.ProjectId || source.ActorId != parent.OriginalActorId)
            throw new ArgumentException("Artifact must retain the exact parent and registered source identity.", nameof(manifest));
        encryptedChunk = OfflinePackageAuthentication.VerifyEncryptedSignature(encryptedChunk, source.SigningPublicKey);
        if (!string.Equals(encryptedChunk.Header.SourceDeviceId, source.DeviceId.ToString("D"), StringComparison.Ordinal))
            throw new ArgumentException("Artifact device differs from its retained source registration.", nameof(encryptedChunk));
        OfflinePackageAuthentication.VerifyClaim(canonical, manifestSignature, source.SigningPublicKey);
        OfflineCaptureArtifacts.ValidateEnvelope(manifest, chunkIndex, encryptedChunk, source.SigningPublicKey);
        var chunk = manifest.Chunks[chunkIndex];
        return new()
        {
            Id = chunk.ArtifactId, ProjectId = manifest.ProjectId, ParentPackageId = parent.Id,
            CaptureOriginId = manifest.CaptureOriginId, TaskId = manifest.TaskId, OriginalActorId = source.ActorId,
            SourceDeviceRegistrationId = source.Id, ChunkIndex = chunk.Index, ChunkOffset = chunk.Offset,
            ChunkLength = chunk.Length, PlaintextChecksum = chunk.PlaintextChecksum,
            EnvelopeFingerprint = chunk.EnvelopeFingerprint,
            ManifestHash = Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant(),
            SignedManifestJson = Encoding.UTF8.GetString(canonical), ManifestSignature = manifestSignature,
            CipherEnvelopeJson = JsonSerializer.Serialize(encryptedChunk, Json), RegisteredBy = currentRegistrar,
            RegisteredAt = at.ToUniversalTime()
        };
    }
}
