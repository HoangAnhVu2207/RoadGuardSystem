using System.Text.Json.Serialization;
namespace RoadGuardSystem.DTOs.Files;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReporterEvidenceCreateRequestDto(string FileName, string MediaType, long SizeBytes, string ChecksumSha256);
