using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Processing;

public sealed record CreateValidationRunRequestDto(
    [param: Required, MinLength(1)] IReadOnlyList<ValidationPairDto> Pairs,
    Guid ModelVersionId,
    Guid DatasetSplitId,
    [param: Required, MinLength(1)] string MeasurementType,
    [param: Required, MinLength(1)] string Unit);
