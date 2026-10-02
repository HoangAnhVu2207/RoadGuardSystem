namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyDatasetSubmissionRequest(
    Guid ActorUserId,
    Guid TaskId,
    IReadOnlyList<Guid> VideoFileIds,
    IReadOnlyList<Guid> TelemetryFileIds,
    DateTimeOffset RecordedAt,
    Guid DeviceId,
    string ScopeJson,
    string ExpectedTaskVersion,
    string IdempotencyKey,
    string RequestFingerprint,
    Guid? CorrelationId,
    string? PairsJson = null);
