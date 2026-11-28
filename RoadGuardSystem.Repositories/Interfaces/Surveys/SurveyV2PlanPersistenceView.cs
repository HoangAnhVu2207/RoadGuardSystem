namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyV2PlanPersistenceView(Guid Id, Guid ProjectId, string ScopeJson, DateTimeOffset PlannedAt, string Status, string Version, string SurveyType = "UNKNOWN", SurveyPlanCompletionRecord? Completion = null, Guid? WarrantyId = null, IReadOnlyList<SurveyV2OccurrenceView>? Occurrences = null);

public sealed record SurveyV2OccurrenceView(Guid Id, DateTimeOffset Start, DateTimeOffset End, string Status, Guid? SurveyPlanId);
