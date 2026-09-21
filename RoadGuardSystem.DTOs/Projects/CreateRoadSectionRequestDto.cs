using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Projects;

public sealed record CreateRoadSectionRequestDto(
    [Required, StringLength(80)] string Code,
    [StringLength(255)] string? Name,
    [Range(32648, 32649)] int Srid,
    [Required, MinLength(2)] IReadOnlyList<RoadSectionCoordinateDto> Coordinates,
    DateTimeOffset EffectiveFrom,
    [Required] string ChangeReason,
    Guid OperationId);
