namespace RoadGuardSystem.Repositories.Surveys;
public sealed record DatasetDetailRecord(Guid Id, Guid TaskId, Guid ProjectId, Guid? SubmittedBy, DateTimeOffset? SubmittedAt,
    DateTimeOffset? RecordedAt, Guid? DeviceId, string ScopeJson, string SourceJson, string PairsJson, string Version, string IntegrityStatus = "UNKNOWN");
