namespace RoadGuardSystem.DTOs.Files;

public sealed record UploadSessionResponseDto(
    Guid Id,
    Guid FileId,
    string Status,
    int PartSizeBytes,
    DateTimeOffset ExpiresAt,
    string Version);
