using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Surveys;

public sealed record CreateSurveyPlanCommand(
    Guid ProjectId,
    Guid RoadSectionId,
    Guid RoadSectionVersionId,
    DateTimeOffset PlannedStartAt,
    DateTimeOffset PlannedEndAt,
    SurveyType SurveyType,
    string OutputRequirements,
    Guid OperationId,
    Guid? CorrelationId);
