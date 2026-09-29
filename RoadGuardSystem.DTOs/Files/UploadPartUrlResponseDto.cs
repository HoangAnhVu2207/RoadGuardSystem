namespace RoadGuardSystem.DTOs.Files;

public sealed record UploadPartUrlResponseDto(int PartNumber, string Url, DateTimeOffset ExpiresAt);
