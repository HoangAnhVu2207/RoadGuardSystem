namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyPlanCompletionSourceRecord(Guid TaskId, Guid DatasetId, Guid AssessmentId);
public sealed record SurveyPlanCompletionRecord(Guid Id, DateTimeOffset At, Guid ActorId, string Reason, IReadOnlyList<SurveyPlanCompletionSourceRecord> Sources);
public sealed record SurveyPlanCompletionRequest(Guid ActorId, Guid PlanId, string ExpectedVersion, string Key, string Fingerprint, string Reason, IReadOnlyList<SurveyPlanCompletionSourceRecord> Sources);
