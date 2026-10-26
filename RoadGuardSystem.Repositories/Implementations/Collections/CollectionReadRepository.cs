using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Retention;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Collections;
using RoadGuardSystem.Repositories.Repairs;
namespace RoadGuardSystem.Repositories.Implementations.Collections;

public sealed class CollectionReadRepository(RoadGuardDbContext db) : ICollectionReadRepository
{
    public Task<bool> ProjectExistsAsync(Guid project, CancellationToken ct) => db.Projects.AsNoTracking().AnyAsync(x => x.Id == project, ct);
    public async Task<Guid[]> CandidatesAsync(CollectionResource resource, Guid actor, Guid project, Guid? parent, string? mode, Guid? after, int take, CancellationToken ct)
    {
        IQueryable<Guid> ids = resource switch
        {
            CollectionResource.RepairPackages => db.RepairPackages.AsNoTracking().Where(x => x.ProjectId == project || x.Obligations.Any(o => db.Set<ObligationResponsibility>().Any(r => r.ObligationId == o.Id && r.CurrentProjectId == project))).Select(x => x.Id),
            CollectionResource.RepairItems => db.RepairItems.AsNoTracking().Where(x => EF.Property<Guid?>(x, "PackageId") == parent && (mode == null || x.Mode == (mode == "NORMAL" ? RepairMode.Normal : RepairMode.FastTrack))).Select(x => x.Id),
            CollectionResource.Exports => db.Set<ExportJob>().AsNoTracking().Where(x => x.ProjectId == project && x.RequestedBy == actor).Select(x => x.Id),
            CollectionResource.SurveyPlans => db.SurveyPlans.AsNoTracking().Where(x => x.ProjectId == project && x.ScopeFormatVersion == "BAND_V1").Select(x => x.Id),
            CollectionResource.SurveyTasks => db.SurveyRequests.AsNoTracking().Where(x => x.ProjectId == project && x.ScopeFormatVersion == "BAND_V1").Select(x => x.Id),
            CollectionResource.ProcessingJobs => db.ProcessingJobs.AsNoTracking().Where(x => x.ProjectId == project).Select(x => x.Id),
            CollectionResource.MockRuns => db.Set<AiMockRun>().AsNoTracking().Where(x => x.ProjectId == project).Select(x => x.Id),
            CollectionResource.OfflineDevices => db.Set<OfflineDeviceRegistration>().AsNoTracking().Where(x => x.ProjectId == project).Select(x => x.Id),
            CollectionResource.OfflinePackages => db.Set<OfflineEncryptedPackageRecord>().AsNoTracking().Where(x => x.ProjectId == project).Select(x => x.Id),
            CollectionResource.OfflineGrants => db.Set<OfflineHandoverGrant>().AsNoTracking().Where(x => x.ProjectId == project).Select(x => x.Id),
            CollectionResource.RetentionEvaluations => db.Set<RetentionEvaluation>().AsNoTracking().Where(x => x.ProjectId == project).Select(x => x.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(resource))
        };
        // Guid.CompareTo is translated by EF SQL Server; both ordering and boundary use uniqueidentifier ordering.
        if (after.HasValue) ids = ids.Where(x => x.CompareTo(after.Value) > 0);
        return await ids.OrderBy(x => x).Take(take).ToArrayAsync(ct);
    }
    public Task<RepairPackageReadRecord?> PackageAsync(Guid project, Guid id, CancellationToken ct) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync<RepairPackageReadRecord?>(async () =>
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var package = await db.RepairPackages.AsNoTracking().Include(x => x.Obligations).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (package is null) return null;
        // A compound summary must not disclose completion across a transferred obligation boundary.
        foreach (var obligation in package.Obligations)
            if (await ObligationResponsibilityScope.ResolveAsync(db, obligation.Id, obligation.ProjectId, ct) != project) return null;
        var version = await db.RepairPackages.Where(x => x.Id == id).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync(ct);
        await transaction.CommitAsync(ct);
        return new RepairPackageReadRecord(id, package.ProjectId, package.DefectId, package.IsComplete, Convert.ToBase64String(version));
    });
    public Task<bool> HasReadablePackageAsync(Guid project, Guid id, CancellationToken ct) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var package = await db.RepairPackages.AsNoTracking().Include(x => x.Obligations).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (package is null) return false;
        foreach (var obligation in package.Obligations)
            if (await ObligationResponsibilityScope.ResolveAsync(db, obligation.Id, obligation.ProjectId, ct) == project) return true;
        return false;
    });
}
