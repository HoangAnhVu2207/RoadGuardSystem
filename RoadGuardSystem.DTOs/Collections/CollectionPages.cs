namespace RoadGuardSystem.DTOs.Collections;

public sealed record CollectionPage<T>(IReadOnlyList<T> Items, string? NextCursor);
public sealed record RepairPackageSummary(Guid Id, Guid ProjectId, Guid DefectId, bool AllMandatoryResolved, string Version);
public sealed record RepairItemSummary(Guid Id, Guid ProjectId, Guid DefectId, Guid ObligationId, string Mode, string State, string Result, string Version);
public sealed record ExportSummary(Guid Id, Guid ProjectId, string Kind, string Format, string Status, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, DateTimeOffset? ExpiresAt, string? ErrorCode, string Completeness, string[] MissingSections, string Version);
public sealed record SurveyPlanSummary(Guid Id, Guid ProjectId, DateTimeOffset PlannedAt, string Status, string Version);
public sealed record SurveyTaskSummary(Guid Id, Guid ProjectId, Guid OperatorId, string Status, string Version, Guid? SupplementTaskId);
public sealed record ProcessingJobSummary(Guid Id, string Status, Guid? ResultId, int AttemptNumber, string Version, string JobType);
public sealed record MockRunSummary(Guid Id, Guid ProjectId, string Stage, string Mode, string Status, Guid ProcessingJobId, Guid? ResultId, string FixtureVersion, string? ErrorCode, string Version);
public sealed record OfflineDeviceSummary(Guid Id, Guid ProjectId, Guid ActorId, Guid DeviceId, int Revision, string Role, DateTimeOffset RegisteredAt, DateTimeOffset? RevokedAt);
public sealed record OfflinePackageSummary(Guid Id, Guid ProjectId, Guid OriginalActorId, Guid SourceDeviceRegistrationId, Guid SourceBatchId);
public sealed record OfflineGrantSummary(Guid Id, Guid ProjectId, Guid PackageId, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt);
public sealed record RetentionEvaluationSummary(Guid Id, Guid ProjectId, string Status, DateTimeOffset CreatedAt, DateTimeOffset? EvaluatedAt, string PolicyVersion, string Version);
