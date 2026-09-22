using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record PostponeSurveyPlanRequestDto(
    DateTimeOffset? NewPlannedStartAt,
    [Required, MinLength(1)] string Reason,
    Guid OperationId);
