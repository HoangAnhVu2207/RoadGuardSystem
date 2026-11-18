namespace RoadGuardSystem.Repositories.Collections;

public sealed record RepairPackageReadRecord(Guid Id, Guid ProjectId, Guid DefectId, bool AllMandatoryResolved, string Version);
public enum CollectionResource { RepairPackages, RepairItems, Exports, SurveyPlans, SurveyTasks, ProcessingJobs, MockRuns, OfflineDevices, OfflinePackages, OfflineGrants, RetentionEvaluations }
public interface ICollectionReadRepository
{
    Task<SurveyCollectionSummary[]> SurveySummariesAsync(CollectionResource resource, Guid project, Guid[] ids, CancellationToken token)
        => Task.FromResult(Array.Empty<SurveyCollectionSummary>());
    Task<bool> ProjectExistsAsync(Guid project, CancellationToken ct);
    Task<Guid[]> CandidatesAsync(CollectionResource resource, Guid actor, Guid project, Guid? parent, string? mode, Guid? after, int take, CancellationToken ct);
    Task<RepairPackageReadRecord?> PackageAsync(Guid project, Guid id, CancellationToken ct);
    Task<bool> HasReadablePackageAsync(Guid project, Guid id, CancellationToken ct);
}

public sealed record SurveyScopeSummary(Guid RouteVersionId, Guid SegmentSetId, string TargetBand, int SegmentCount);
public sealed record SurveyCollectionSummary(Guid Id, Guid ProjectId, string Status, string Version,
    DateTimeOffset? PlannedAt, DateTimeOffset? PlannedEndAt, Guid OperatorId, string? OperatorDisplayName,
    string OperatorNameSource, Guid? PlanId, DateTimeOffset? DueAt, int DatasetCount, Guid? SupplementTaskId,
    SurveyScopeSummary[] ScopeSummary, string ScopeAvailability);
