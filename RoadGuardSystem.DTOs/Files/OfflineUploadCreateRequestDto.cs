using System.Text.Json.Serialization;
namespace RoadGuardSystem.DTOs.Files;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineUploadCreateRequestDto(Guid AdmissionId, Guid CaptureOriginId, UploadCreateRequestDto Upload);
