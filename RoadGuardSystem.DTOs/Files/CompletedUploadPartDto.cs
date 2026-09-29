using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Files;

public sealed record CompletedUploadPartDto(
    [Range(1, int.MaxValue)] int PartNumber,
    [Required, MaxLength(512)] string ETag);
