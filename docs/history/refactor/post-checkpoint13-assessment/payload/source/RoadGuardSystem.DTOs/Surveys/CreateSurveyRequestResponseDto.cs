using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.DTOs.Surveys;

public sealed record CreateSurveyRequestResponseDto(
    Guid RequestId,
    Guid ProjectId,
    Guid RoadSectionId,
    Guid? RoadSectionVersionId,
    Guid? SurveyPlanId,
    Guid RequestedByUserId,
    SurveyType SurveyType,
    SurveyRequestStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset DueAt,
    string OutputRequirements);
