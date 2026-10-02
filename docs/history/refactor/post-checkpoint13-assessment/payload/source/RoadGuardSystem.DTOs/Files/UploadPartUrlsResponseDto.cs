namespace RoadGuardSystem.DTOs.Files;

public sealed record UploadPartUrlsResponseDto(IReadOnlyList<UploadPartUrlResponseDto> Parts);
