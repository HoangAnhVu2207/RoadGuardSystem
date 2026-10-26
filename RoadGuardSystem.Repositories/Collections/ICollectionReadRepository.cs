namespace RoadGuardSystem.Repositories.Collections;

public sealed record RepairPackageReadRecord(Guid Id, Guid ProjectId, Guid DefectId, bool AllMandatoryResolved, string Version);
public enum CollectionResource { RepairPackages, RepairItems, Exports, SurveyPlans, SurveyTasks, ProcessingJobs, MockRuns, OfflineDevices, OfflinePackages, OfflineGrants, RetentionEvaluations }
public interface ICollectionReadRepository
{
    Task<bool> ProjectExistsAsync(Guid project, CancellationToken ct);
    Task<Guid[]> CandidatesAsync(CollectionResource resource, Guid actor, Guid project, Guid? parent, string? mode, Guid? after, int take, CancellationToken ct);
    Task<RepairPackageReadRecord?> PackageAsync(Guid project, Guid id, CancellationToken ct);
    Task<bool> HasReadablePackageAsync(Guid project, Guid id, CancellationToken ct);
}
