using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record CreateSurveyPlanResponseDto(
    Guid PlanId,
    Guid ProjectId,
    Guid RoadSectionId,
    Guid? RoadSectionVersionId,
    DateTimeOffset PlannedStartAt,
    DateTimeOffset PlannedEndAt,
    SurveyType SurveyType,
    SurveyPlanStatus Status,
    string OutputRequirements);
