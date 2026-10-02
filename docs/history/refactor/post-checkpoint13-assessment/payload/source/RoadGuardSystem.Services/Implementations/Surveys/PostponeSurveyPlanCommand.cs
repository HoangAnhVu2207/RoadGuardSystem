namespace RoadGuardSystem.Services.Surveys;

public sealed record PostponeSurveyPlanCommand(
    Guid ProjectId,
    Guid SurveyPlanId,
    DateTimeOffset? NewPlannedStartAt,
    string Reason,
    Guid OperationId,
    Guid? CorrelationId);
