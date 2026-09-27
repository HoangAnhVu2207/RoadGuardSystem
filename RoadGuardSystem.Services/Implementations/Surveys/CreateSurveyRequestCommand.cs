using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Surveys;

public sealed record CreateSurveyRequestCommand(
    Guid ProjectId,
    Guid RoadSectionId,
    Guid RoadSectionVersionId,
    Guid? SurveyPlanId,
    SurveyType SurveyType,
    DateTimeOffset? DueAt,
    string OutputRequirements,
    Guid OperationId,
    Guid? CorrelationId);
