using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record PostponeSurveyPlanV2RequestDto(
    [Required, MinLength(1)] string Reason);
