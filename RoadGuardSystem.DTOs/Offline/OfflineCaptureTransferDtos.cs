using System.Text.Json.Serialization;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.DTOs.Files;

namespace RoadGuardSystem.DTOs.Offline;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineCaptureArtifactInput(OfflineCaptureManifest Manifest, string ManifestSignature,
    int ChunkIndex, H5EncryptedPackageDto Envelope);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineCaptureUploadInput(Guid OfflineAdmissionId, Guid CaptureOriginId,
    UploadCreateRequestDto Upload);

// Separately scoped transport output; it never replaces the original signed evidence declaration.
public sealed record OfflineResolvedCaptureView(Guid CaptureOriginId, Guid FileId, string UploadState,
    string Checksum, string Purpose, string MediaType, Guid ActualUploaderId);
