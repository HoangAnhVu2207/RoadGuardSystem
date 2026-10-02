using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Files;

public sealed record UploadPartUrlsRequestDto(
    [Required, MinLength(1)] IReadOnlyList<int> PartNumbers);
