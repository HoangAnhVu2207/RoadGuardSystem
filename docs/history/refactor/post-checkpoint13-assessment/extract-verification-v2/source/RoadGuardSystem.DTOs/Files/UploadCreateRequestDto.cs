using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Files;

public sealed record UploadCreateRequestDto(
    [Required, MaxLength(40)] string Purpose,
    Guid? ProjectId,
    Guid? TargetId,
    [Required, MaxLength(255)] string FileName,
    [Required, MaxLength(120)] string MediaType,
    [Range(1, int.MaxValue)] long SizeBytes,
    [Required, RegularExpression("^[a-f0-9]{64}$")] string ChecksumSha256);
