namespace RoadGuardSystem.Repositories.Surveys;
public sealed record SurveyV2PlanPersistenceView(Guid Id, Guid ProjectId, string ScopeJson, DateTimeOffset PlannedAt, string Status, string Version);
