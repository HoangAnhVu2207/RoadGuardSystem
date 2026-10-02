using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record CreateSurveyTaskV2RequestDto(
    [Required, MinLength(1)] IReadOnlyList<BandScopeDto> Scope,
    [Required] string SurveyType,
    Guid OperatorId,
    DateTimeOffset? DueAt,
    PositionDto? AccessPoint,
    Guid? PlanId = null);
