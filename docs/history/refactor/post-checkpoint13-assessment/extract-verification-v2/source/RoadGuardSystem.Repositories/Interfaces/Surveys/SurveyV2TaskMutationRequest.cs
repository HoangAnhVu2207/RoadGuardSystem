namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyV2TaskMutationRequest(
    Guid ActorUserId,
    Guid TaskId,
    string Operation,
    string? Reason,
    Guid? OperatorId,
    DateTimeOffset? DueAt,
    string? ScopeJson,
    string ExpectedVersion,
    string IdempotencyKey,
    string RequestFingerprint,
    Guid? CorrelationId);
