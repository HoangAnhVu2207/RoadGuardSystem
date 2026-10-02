using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record SurveyTaskReasonV2RequestDto(
    [Required, MinLength(1)] string Reason);
