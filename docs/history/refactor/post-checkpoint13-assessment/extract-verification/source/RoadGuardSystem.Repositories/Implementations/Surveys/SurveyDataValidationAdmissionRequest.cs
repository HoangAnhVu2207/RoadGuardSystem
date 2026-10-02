using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyDataValidationAdmissionRequest(
    SurveyDataVersion DataVersion,
    IReadOnlyCollection<Guid> SurveyFileIds,
    Guid ActorUserId,
    string IdempotencyKey,
    string RequestFingerprint,
    Guid CorrelationId);
