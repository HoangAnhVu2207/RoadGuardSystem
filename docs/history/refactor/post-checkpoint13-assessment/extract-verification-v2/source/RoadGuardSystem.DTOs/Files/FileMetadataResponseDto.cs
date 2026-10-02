namespace RoadGuardSystem.DTOs.Files;

public sealed record FileMetadataResponseDto(
    Guid Id,
    string Status,
    string ChecksumSha256,
    string MediaType,
    long SizeBytes,
    string Version);
