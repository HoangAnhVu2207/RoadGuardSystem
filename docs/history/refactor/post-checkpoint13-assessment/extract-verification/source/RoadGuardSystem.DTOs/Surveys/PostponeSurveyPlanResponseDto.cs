using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record PostponeSurveyPlanResponseDto(
    Guid PlanId,
    SurveyPlanStatus Status,
    DateTimeOffset? NewPlannedStartAt,
    Guid PostponementId);
