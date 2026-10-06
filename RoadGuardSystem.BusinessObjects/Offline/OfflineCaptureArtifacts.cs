using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Offline;

// A finite photo transfer manifest. Its claims do not prove capture time or measurement accuracy.
public sealed record OfflineCaptureChunk(Guid ArtifactId, int Index, long Offset, int Length,
    string PlaintextChecksum, string EnvelopeFingerprint);
public sealed record OfflineCaptureManifest(int Version, Guid ParentPackageId, string ParentManifestHash,
    Guid ProjectId, Guid OriginalActorId, Guid SourceDeviceRegistrationId, Guid CaptureOriginId,
    Guid TaskId, string Purpose, string MediaType, long FileSize, string FileChecksum,
    OfflineCaptureChunk[] Chunks);

public static class OfflineCaptureArtifacts
{
    public const long MaximumPhotoBytes = 20L * 1024 * 1024;
    public const int MaximumChunks = 32;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static byte[] CanonicalManifest(OfflineCaptureManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        OfflineRuntimeGuards.Identity(manifest.ParentPackageId, manifest.ProjectId, manifest.OriginalActorId,
            manifest.SourceDeviceRegistrationId, manifest.CaptureOriginId, manifest.TaskId);
        OfflineCryptoFormat.HashBytes(manifest.ParentManifestHash);
        OfflineCryptoFormat.HashBytes(manifest.FileChecksum);
        ArgumentNullException.ThrowIfNull(manifest.Chunks);
        if (manifest.Version != 1 || manifest.Purpose is not ("BEFORE" or "AFTER" or "MEASUREMENT") ||
            manifest.MediaType is not ("image/jpeg" or "image/png") || manifest.FileSize is < 1 or > MaximumPhotoBytes ||
            manifest.Chunks.Length is < 1 or > MaximumChunks)
            throw new ArgumentException("Capture manifest is outside the versioned photo bounds.", nameof(manifest));
        var artifacts = new HashSet<Guid>(); long offset = 0;
        for (var index = 0; index < manifest.Chunks.Length; index++)
        {
            var chunk = manifest.Chunks[index]; ArgumentNullException.ThrowIfNull(chunk);
            OfflineRuntimeGuards.Identity(chunk.ArtifactId);
            OfflineCryptoFormat.HashBytes(chunk.PlaintextChecksum);
            OfflineCryptoFormat.HashBytes(chunk.EnvelopeFingerprint);
            if (!artifacts.Add(chunk.ArtifactId) || chunk.Index != index || chunk.Offset != offset ||
                chunk.Length is < 1 or > OfflineCryptoFormat.MaxPayloadBytes)
                throw new ArgumentException("Chunks require unique IDs and exact contiguous ordered offsets.", nameof(manifest));
            offset += chunk.Length;
            if (offset > manifest.FileSize) throw new ArgumentException("Chunks exceed the declared photo size.", nameof(manifest));
        }
        if (offset != manifest.FileSize) throw new ArgumentException("Chunks do not cover the declared photo size.", nameof(manifest));
        return JsonSerializer.SerializeToUtf8Bytes(new { purpose = "roadguard.h5.capture-manifest.v1", manifest }, Json);
    }
    public static void ValidateEnvelope(OfflineCaptureManifest manifest, int chunkIndex,
        OfflineEncryptedPackage package, string registeredSigningPublicKey)
    {
        CanonicalManifest(manifest);
        if (chunkIndex < 0 || chunkIndex >= manifest.Chunks.Length)
            throw new ArgumentException("Chunk index is outside the signed manifest.", nameof(chunkIndex));
        package = OfflinePackageAuthentication.VerifyEncryptedSignature(package, registeredSigningPublicKey);
        var chunk = manifest.Chunks[chunkIndex];
        if (package.Header.PackageId != chunk.ArtifactId || package.Header.ProjectId != manifest.ProjectId ||
            package.Header.OriginalActorId != manifest.OriginalActorId || package.Header.OriginIds.Count != 1 ||
            package.Header.OriginIds[0] != manifest.CaptureOriginId || package.Header.PayloadSha256 != chunk.PlaintextChecksum ||
            OfflineCryptoFormat.PackageContentFingerprint(package) != chunk.EnvelopeFingerprint ||
            OfflineCryptoFormat.Decode(package.Ciphertext, OfflineCryptoFormat.MaxPayloadBytes).Length != chunk.Length)
            throw new ArgumentException("Artifact envelope differs from its exact signed parent/capture/chunk binding.", nameof(package));
    }
}
