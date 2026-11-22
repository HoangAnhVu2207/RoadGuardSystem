using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Surveys;
namespace RoadGuardSystem.Repositories.Implementations.Surveys;

public sealed partial class SurveyV2PersistenceService
{
    private static bool IsActivePlanUniqueViolation(DbUpdateException exception)
        => exception.InnerException is SqlException { Number: 2601 or 2627 } sql && sql.Message.Contains("UX_SurveyPlans_ActiveScope", StringComparison.Ordinal);
    private async Task<SurveyV2PlanPersistenceResult> ReadActivePlanConflictAsync(SurveyV2PlanCreationRequest request, CancellationToken token)
        => await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, token);
        try { await GuardCompletionActorAsync(request.ActorUserId, request.ProjectId, token); }
        catch (UnauthorizedAccessException) { return new SurveyV2PlanPersistenceResult(SurveyV2PersistenceStatus.Forbidden); }
        var road = await _context.RoadSectionVersions.Where(x => x.Id == request.RouteVersionId).Select(x => (Guid?)x.RoadSectionId).SingleOrDefaultAsync(token);
        var plan = await _context.SurveyPlans.AsNoTracking().Where(x => x.ProjectId == request.ProjectId && x.RoadSectionId == road && x.SurveyType == request.SurveyType &&
            (x.Status == SurveyPlanStatus.Planned || x.Status == SurveyPlanStatus.Postponed || x.Status == SurveyPlanStatus.InProgress)).Select(x => new { x.Id, x.Status }).SingleOrDefaultAsync(token);
        return plan is null ? new SurveyV2PlanPersistenceResult(SurveyV2PersistenceStatus.Conflict) : new SurveyV2PlanPersistenceResult(SurveyV2PersistenceStatus.ActivePlanConflict, ExistingPlanId: plan.Id, ExistingPlanStatus: plan.Status == SurveyPlanStatus.InProgress ? "IN_PROGRESS" : plan.Status.ToString().ToUpperInvariant());
    });
    private sealed class PlanCompletionReject(string code) : Exception(code) { public string Code { get; } = code; }
    public async Task<SurveyV2PlanPersistenceResult> CompletePlanAsync(SurveyPlanCompletionRequest request, CancellationToken token = default)
    {
        var project = await GetPlanProjectIdAsync(request.PlanId, token);
        if (project is null) return new(SurveyV2PersistenceStatus.NotFound);
        try
        {
            var result = await _idempotency.ExecuteSerializableAsync(request.ActorId, project, "SurveyPlanV2Completed", request.Key, request.Fingerprint, async ct =>
            {
                await GuardCompletionActorAsync(request.ActorId, project.Value, ct);
                var plan = await _context.SurveyPlans.SingleAsync(x => x.Id == request.PlanId, ct);
                if (!TryDecodeVersion(request.ExpectedVersion, out var version) || !plan.RowVersion.SequenceEqual(version)) throw new PlanCompletionReject("plan_version_stale");
                if (plan.ScopeFormatVersion != "BAND_V1") throw new PlanCompletionReject("plan_lineage_unavailable");
                if (plan.Status != SurveyPlanStatus.InProgress) throw new PlanCompletionReject("plan_not_in_progress");
                var required = await PlanScopeAsync(plan, ct);
                var tasks = await _context.SurveyRequests.Where(x => x.ProjectId == project && (x.SurveyPlanId == plan.Id || x.ParentTaskId != null)).ToArrayAsync(ct);
                var roots = tasks.Where(x => x.SurveyPlanId == plan.Id && x.ParentTaskId == null).ToArray();
                if (roots.Length == 0 || roots.Any(x => x.SurveyType != plan.SurveyType) || tasks.Any(x => x.SurveyPlanId == plan.Id && x.ParentTaskId != null)) throw new PlanCompletionReject("plan_lineage_unavailable");
                var related = roots.Select(x => x.Id).ToHashSet();
                for (var i = 0; i <= tasks.Length; i++)
                {
                    var children = tasks.Where(x => x.ParentTaskId.HasValue && related.Contains(x.ParentTaskId.Value) && !related.Contains(x.Id)).ToArray();
                    if (children.Length == 0) break;
                    foreach (var child in children)
                    {
                        if (child.SurveyType != SurveyType.Supplementary || child.SupplementRequestId is not Guid supplement || !await (from r in _context.SupplementarySurveyRequests
                                                                                                                                        join s in _context.Surveys on r.SurveyId equals s.Id
                                                                                                                                        where r.Id == supplement && r.SurveyRequestId == child.ParentTaskId && s.SurveyRequestId == child.ParentTaskId && s.ProjectId == project
                                                                                                                                        select r.Id).AnyAsync(ct)) throw new PlanCompletionReject("plan_lineage_unavailable");
                        if (!(await TaskPlanScopeAsync(child, ct)).IsSubsetOf(await TaskPlanScopeAsync(tasks.Single(x => x.Id == child.ParentTaskId), ct)))
                            throw new PlanCompletionReject("plan_lineage_unavailable");
                        related.Add(child.Id);
                    }
                }
                if (await _context.SurveyRequests.AnyAsync(x => x.ParentTaskId.HasValue && related.Contains(x.ParentTaskId.Value) && x.ProjectId != project, ct))
                    throw new PlanCompletionReject("plan_lineage_unavailable");
                foreach (var task in tasks.Where(x => related.Contains(x.Id)))
                {
                    if (task.RoadSectionId != plan.RoadSectionId || task.ScopeFormatVersion != "BAND_V1" || !(await TaskPlanScopeAsync(task, ct)).IsSubsetOf(required))
                        throw new PlanCompletionReject("plan_lineage_unavailable");
                }
                var selected = request.Sources.Select(x => x.TaskId).ToHashSet();
                var covered = new HashSet<(Guid Route, Guid Set, Guid Segment, string Band)>();
                foreach (var source in request.Sources)
                {
                    var task = tasks.SingleOrDefault(x => x.Id == source.TaskId && related.Contains(x.Id));
                    if (task is null || task.ScopeFormatVersion != "BAND_V1" || task.Status is not (SurveyRequestStatus.Submitted or SurveyRequestStatus.Completed or SurveyRequestStatus.SupplementRequired)) throw new PlanCompletionReject("plan_source_task_invalid");
                    var dataset = await (from d in _context.SurveyDataVersions
                                         join s in _context.Surveys on d.SurveyId equals s.Id
                                         where d.Id == source.DatasetId && s.SurveyRequestId == task.Id && s.ProjectId == project && d.Status == SurveyDataVersionStatus.ServerConfirmed
                                         select d).SingleOrDefaultAsync(ct);
                    if (dataset is null) throw new PlanCompletionReject("plan_dataset_lineage_invalid");
                    var assessment = await SurveyAssessmentRepository.LatestFirst(_context.Set<DatasetAssessment>().Where(x => x.DatasetId == dataset.Id)).FirstOrDefaultAsync(ct);
                    if (assessment is null || assessment.Id != source.AssessmentId || assessment.MethodVersion != "pm-evidence-review.v1") throw new PlanCompletionReject("plan_assessment_not_current");
                    var taskScope = await TaskPlanScopeAsync(task, ct);
                    var manifest = JsonSerializer.Deserialize<DatasetScopeItem[]>(dataset.ScopeManifest ?? "[]", DatasetScopeJsonOptions) ?? [];
                    ValidateCompletionScope(manifest);
                    var submitted = ScopeKeys(manifest);
                    if (!taskScope.IsSubsetOf(required) || !submitted.IsSubsetOf(taskScope)) throw new PlanCompletionReject("plan_source_scope_mismatch");
                    var reviews = await _context.Set<DatasetAssessmentItem>().Where(x => x.AssessmentId == assessment.Id).ToArrayAsync(ct);
                    foreach (var review in reviews)
                    {
                        var key = (review.RouteVersionId, review.SegmentSetId, review.SegmentId, review.TargetBand);
                        if (submitted.Contains(key) && review.PositionStatus == "PASS" && review.QualityStatus == "PASS" && review.CoverageStatus == "PASS") covered.Add(key);
                    }
                }
                if (!required.IsSubsetOf(covered)) throw new PlanCompletionReject("plan_scope_not_satisfied");
                foreach (var task in tasks.Where(x => related.Contains(x.Id)))
                {
                    if (task.Status is SurveyRequestStatus.Completed or SurveyRequestStatus.Cancelled) continue;
                    if (!selected.Contains(task.Id) || task.Status is not (SurveyRequestStatus.Submitted or SurveyRequestStatus.SupplementRequired)) throw new PlanCompletionReject("plan_unhandled_task_branch");
                }
                plan.Complete();
                var now = DateTimeOffset.UtcNow;
                var basis = new SurveyPlanCompletionRecord(Guid.NewGuid(), now, request.ActorId, request.Reason.Trim(), request.Sources);
                _context.AuditLogs.Add(AuditLog.Create(basis.Id, request.ActorId, now, "survey_plan_completed", "SurveyPlan", plan.Id, null,
                    JsonSerializer.Serialize(basis, DatasetScopeJsonOptions), request.Reason.Trim(), "survey.plan.complete", null, ["id", "at", "actorId", "reason", "sources", "taskId", "datasetId", "assessmentId"]));
                await _context.SaveChangesAsync(ct);
                return (basis.Id, JsonSerializer.Serialize(ToPlanView(plan) with { Completion = basis }));
            }, token, ct => GuardCompletionActorAsync(request.ActorId, project.Value, ct));
            return MapPlanOutcome(result);
        }
        catch (PlanCompletionReject e) { _context.ChangeTracker.Clear(); return new(e.Code == "plan_version_stale" ? SurveyV2PersistenceStatus.ConcurrencyConflict : SurveyV2PersistenceStatus.PlanCompletionBlocked, ReasonCode: e.Code); }
        catch (UnauthorizedAccessException) { return new(SurveyV2PersistenceStatus.Forbidden); }
        catch (KeyNotFoundException) { return new(SurveyV2PersistenceStatus.PlanCompletionBlocked, ReasonCode: "plan_scope_unavailable"); }
        catch (JsonException) { return new(SurveyV2PersistenceStatus.PlanCompletionBlocked, ReasonCode: "plan_scope_unavailable"); }
        catch (InvalidOperationException) { return new(SurveyV2PersistenceStatus.PlanCompletionBlocked, ReasonCode: "plan_scope_unavailable"); }
        catch (DbUpdateConcurrencyException) { return new(SurveyV2PersistenceStatus.ConcurrencyConflict); }
    }
    private static HashSet<(Guid Route, Guid Set, Guid Segment, string Band)> ScopeKeys(DatasetScopeItem[] items)
        => items.SelectMany(x => x.SegmentIds.Select(id => (x.RouteVersionId, x.SegmentSetId, id, x.TargetBand))).ToHashSet();
    private async Task<HashSet<(Guid Route, Guid Set, Guid Segment, string Band)>> PlanScopeAsync(SurveyPlan plan, CancellationToken ct)
    {
        var rows = await _context.SurveyPlanScopes.Where(x => x.SurveyPlanId == plan.Id).ToArrayAsync(ct);
        var items = rows.Length == 0 ? JsonSerializer.Deserialize<DatasetScopeItem[]>(plan.OutputRequirements, DatasetScopeJsonOptions) ?? [] : rows.Select(x => new DatasetScopeItem(x.RouteSectionVersionId, x.SegmentSetId, JsonSerializer.Deserialize<Guid[]>(x.SegmentIdsJson) ?? [], x.TargetBand)).ToArray();
        ValidateCompletionScope(items); return ScopeKeys(items);
    }
    private async Task<HashSet<(Guid Route, Guid Set, Guid Segment, string Band)>> TaskPlanScopeAsync(SurveyRequest task, CancellationToken ct)
    {
        var rows = await _context.SurveyRequestScopes.Where(x => x.SurveyRequestId == task.Id).ToArrayAsync(ct);
        DatasetScopeItem[] items;
        if (rows.Length == 0) { using var payload = JsonDocument.Parse(task.OutputRequirements); items = payload.RootElement.GetProperty("scope").Deserialize<DatasetScopeItem[]>(DatasetScopeJsonOptions) ?? []; }
        else items = rows.Select(x => new DatasetScopeItem(x.RouteSectionVersionId, x.SegmentSetId, JsonSerializer.Deserialize<Guid[]>(x.SegmentIdsJson) ?? [], x.TargetBand)).ToArray();
        ValidateCompletionScope(items); return ScopeKeys(items);
    }
}
