using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record PositionDto(
    [Required] PointDto Point,
    [Required] string Source,
    double? AccuracyMeters = null,
    DateTimeOffset? CapturedAt = null);
