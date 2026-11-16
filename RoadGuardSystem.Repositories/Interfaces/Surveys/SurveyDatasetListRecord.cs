namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyDatasetListRecord(
    Guid Id, int VersionNo, DateTimeOffset? SubmittedAt, string IntegrityStatus, string Version, Guid? AssessmentId);
