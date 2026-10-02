using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record CreateSurveyPlanV2RequestDto(
    [Required, MinLength(1)] IReadOnlyList<BandScopeDto> Scope,
    DateTimeOffset PlannedAt,
    [Required] string SurveyType);
