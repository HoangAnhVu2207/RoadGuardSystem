using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Files;

public sealed record UploadCompleteRequestDto(
    [Required, MinLength(1)] IReadOnlyList<CompletedUploadPartDto> Parts,
    [Required, RegularExpression("^[a-f0-9]{64}$")] string ChecksumSha256);
