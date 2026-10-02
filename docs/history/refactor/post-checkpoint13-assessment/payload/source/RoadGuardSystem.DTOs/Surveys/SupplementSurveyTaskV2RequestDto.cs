using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record SupplementSurveyTaskV2RequestDto(
    [Required, MinLength(1)] IReadOnlyList<BandScopeDto> Scope,
    [Required, MinLength(1)] string Reason,
    Guid OperatorId);
