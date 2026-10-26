using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Collections;
using RoadGuardSystem.Repositories.Collections;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Exports;
using RoadGuardSystem.Services.Offline;
using RoadGuardSystem.Services.Processing;
using RoadGuardSystem.Services.Processing.Anh02;
using RoadGuardSystem.Services.Repairs;
using RoadGuardSystem.Services.Retention;
using RoadGuardSystem.Services.Surveys;
namespace RoadGuardSystem.Services.Collections;

public sealed class CollectionRequestException(int status, string code) : Exception(code)
{ public int Status { get; } = status; public string Code { get; } = code; }
public sealed record AuthorizedCollectionPage<T>(IReadOnlyList<T> Items, Guid? NextId);
public sealed class CollectionReadService(ICollectionReadRepository repository, IAnhHuyFactsRepository identity,
    IProjectScopeGuard scope, IRepairWorkflowService repairs, IExportService exports, ISurveyV2Service surveys,
    IProcessingV2Service processing, IAnh02AiService mock, IOfflineWorkflowService offline, IRetentionService retention)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task AuthorizeAsync(Guid actor, UserRoleCode role, Guid project, CollectionResource resource, Guid? parent, CancellationToken ct)
    {
        var allowed = resource switch
        {
            CollectionResource.SurveyTasks or CollectionResource.ProcessingJobs => role is UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.DroneOperator,
            CollectionResource.OfflineDevices or CollectionResource.OfflinePackages or CollectionResource.OfflineGrants => role is UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.RepairCrew,
            _ => role is UserRoleCode.ProjectManager or UserRoleCode.Supervisor
        };
        if (!allowed || !await identity.IsCurrentActorAsync(actor, role, ct) || await scope.AuthorizeAsync(actor, role, project, ct) is null)
            throw new CollectionRequestException(403, "access_forbidden");
        if (!await repository.ProjectExistsAsync(project, ct)) throw new CollectionRequestException(404, "not_found");
        if (resource == CollectionResource.RepairItems && (parent is null || !await repository.HasReadablePackageAsync(project, parent.Value, ct)))
            throw new CollectionRequestException(404, "not_found");
        // Reuse the resource's full admission even for an empty collection.
        if (resource == CollectionResource.Exports && !await exports.CanListAsync(actor, project, ct)) throw new CollectionRequestException(403, "access_forbidden");
        if (resource == CollectionResource.MockRuns)
        {
            try { await mock.GetAsync(actor, role, project, Guid.Empty, ct); }
            catch (AiRequestException ex) when (ex.Status == 404) { }
        }
        if (resource == CollectionResource.RetentionEvaluations)
        {
            await retention.AuthorizePrincipalAsync(actor, role, ct);
            await retention.AuthorizeAsync(actor, project, false, ct);
        }
    }
    public async Task<AuthorizedCollectionPage<T>> ReadAsync<T>(Guid actor, UserRoleCode role, Guid project, CollectionResource resource,
        Guid? parent, string? mode, Guid? after, int limit, CancellationToken ct)
    {
        await AuthorizeAsync(actor, role, project, resource, parent, ct);
        var visible = new List<(Guid Id, T Value)>();
        var scanned = after;
        while (visible.Count <= limit)
        {
            var ids = await repository.CandidatesAsync(resource, actor, project, parent, mode, scanned, 100, ct);
            foreach (var id in ids)
            {
                ct.ThrowIfCancellationRequested();
                var value = await ReadOneAsync(actor, role, project, resource, parent, id, ct);
                scanned = id;
                if (value is not null)
                {
                    // Serialize only a successfully authorized detail, then project into a closed typed summary.
                    var summary = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Json), Json)
                        ?? throw new InvalidOperationException("Collection summary could not be projected.");
                    visible.Add((id, summary));
                    if (visible.Count > limit) break;
                }
            }
            if (ids.Length < 100 || visible.Count > limit) break;
        }
        return new(visible.Take(limit).Select(x => x.Value).ToArray(), visible.Count > limit ? visible[limit - 1].Id : null);
    }
    private async Task<object?> ReadOneAsync(Guid actor, UserRoleCode role, Guid project, CollectionResource resource, Guid? parent, Guid id, CancellationToken ct)
    {
        switch (resource)
        {
            case CollectionResource.RepairPackages:
                if (!await identity.IsCurrentActorAsync(actor, role, ct) || await scope.AuthorizeAsync(actor, role, project, ct) is null) return null;
                var package = await repository.PackageAsync(project, id, ct);
                return package is null ? null : new RepairPackageSummary(package.Id, package.ProjectId, package.DefectId, package.AllMandatoryResolved, package.Version);
            case CollectionResource.RepairItems:
                var item = await repairs.ReadItemAsync(actor, role, project, parent!.Value, id, false, ct);
                return item.Status == 200 ? item.Value : item.Status is 403 or 404 ? null : throw new CollectionRequestException(item.Status, item.Code ?? "collection_unavailable");
            case CollectionResource.Exports:
                var export = await exports.GetAsync(actor, project, id, ct);
                return export.Code == "success" ? export.Value : export.Code is "forbidden" or "not_found" ? null : throw new CollectionRequestException(503, "collection_unavailable");
            case CollectionResource.SurveyPlans:
                var plan = await surveys.ReadResourceAsync(actor, role, id, "plan", ct);
                return plan.Status == SurveyV2ServiceStatus.Success ? plan.Value : plan.Status is SurveyV2ServiceStatus.Forbidden or SurveyV2ServiceStatus.NotFound ? null : throw new CollectionRequestException(503, "collection_unavailable");
            case CollectionResource.SurveyTasks:
                var task = await surveys.GetTaskAsync(actor, role, id, ct);
                return task.Status == SurveyV2ServiceStatus.Success ? task.Task : task.Status is SurveyV2ServiceStatus.Forbidden or SurveyV2ServiceStatus.NotFound ? null : throw new CollectionRequestException(503, "collection_unavailable");
            case CollectionResource.ProcessingJobs:
                var job = await processing.GetAsync(actor, role, id, ct);
                return job.Status == ProcessingV2ServiceStatus.Success ? job.Job : job.Status is ProcessingV2ServiceStatus.Forbidden or ProcessingV2ServiceStatus.NotFound ? null : throw new CollectionRequestException(503, "collection_unavailable");
            case CollectionResource.MockRuns:
                try { return await mock.GetAsync(actor, role, project, id, ct); }
                catch (AiRequestException ex) when (ex.Status is 403 or 404) { return null; }
            case CollectionResource.OfflineDevices or CollectionResource.OfflinePackages or CollectionResource.OfflineGrants:
                var action = resource == CollectionResource.OfflineDevices ? "device-get" : resource == CollectionResource.OfflinePackages ? "package-get" : "grant-get";
                var result = await offline.ExecuteAsync(actor, role, project, action, null, id, null, null, ct);
                return result.Status == 200 ? result.Value : result.Status is 403 or 404 ? null : throw new CollectionRequestException(result.Status, result.Code ?? "collection_unavailable");
            case CollectionResource.RetentionEvaluations:
                try { return await retention.GetEvaluationAsync(actor, project, id, null, 1, ct); }
                catch (RoadGuardSystem.Repositories.Retention.RetentionRequestException ex) when (ex.Status is 403 or 404) { return null; }
            default: throw new ArgumentOutOfRangeException(nameof(resource));
        }
    }
}
