using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Projects;

public sealed record CreateRoadSectionVersionRequestDto(
    [Range(32648, 32649)] int Srid,
    [Required, MinLength(2)] IReadOnlyList<RoadSectionCoordinateDto> Coordinates,
    DateTimeOffset EffectiveFrom,
    [Required] string ChangeReason,
    Guid ExpectedCurrentVersionId,
    Guid OperationId);
