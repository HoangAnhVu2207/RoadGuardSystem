using RoadGuardSystem.Repositories.Idempotency;

namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyDataValidationAdmissionResult(
    Guid SurveyDataVersionId,
    Guid OutboxMessageId,
    IdempotencyOperationStatus Status);
