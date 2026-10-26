using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Surveys;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record BandScopeDto(
    Guid RouteVersionId,
    Guid SegmentSetId,
    [MinLength(1)] IReadOnlyList<Guid> SegmentIds,
    [Required, MinLength(1)] string TargetBand);
