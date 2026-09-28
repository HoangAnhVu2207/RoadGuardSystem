using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record BandScopeDto(
    Guid RouteVersionId,
    Guid SegmentSetId,
    [MinLength(1)] IReadOnlyList<Guid> SegmentIds,
    [Required, MinLength(1)] string TargetBand);
