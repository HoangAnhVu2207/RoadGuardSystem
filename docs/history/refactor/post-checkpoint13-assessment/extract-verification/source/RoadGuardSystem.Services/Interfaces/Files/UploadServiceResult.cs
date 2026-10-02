using RoadGuardSystem.DTOs.Files;

namespace RoadGuardSystem.Services.Files;

public sealed record UploadServiceResult(
    UploadServiceStatus Status,
    UploadSessionResponseDto? Session = null,
    UploadPartUrlsResponseDto? PartUrls = null,
    FileMetadataResponseDto? File = null,
    Stream? Content = null);
