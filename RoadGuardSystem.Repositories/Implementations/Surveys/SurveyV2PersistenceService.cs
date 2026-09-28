using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Implementations.Surveys;

public sealed class SurveyV2PersistenceService : ISurveyV2Repository
{
    private readonly RoadGuardDbContext _context;
    private readonly IdempotencyOperationService _idempotency;

    public SurveyV2PersistenceService(RoadGuardDbContext context, IdempotencyOperationService idempotency)
    {
        _context = context;
        _idempotency = idempotency;
    }

    public async Task<SurveyV2PlanPersistenceResult> CreatePlanAsync(SurveyV2PlanCreationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var outcome = await _idempotency.ExecuteAsync(request.ActorUserId, request.ProjectId, "SurveyPlanV2Created", request.IdempotencyKey, request.RequestFingerprint, async token =>
            {
                var scope = await ResolveScopeAsync(request.ProjectId, request.RouteVersionId, request.Scope, token);
                var plan = SurveyPlan.Create(Guid.NewGuid(), request.ProjectId, scope.RoadSectionId, request.PlannedAt, request.PlannedAt.AddHours(1), request.SurveyType, SurveyPlanStatus.Planned, request.ScopeJson, scope.Items[0].RouteVersionId);
                _context.SurveyPlans.Add(plan);
                _context.SurveyPlanScopes.AddRange(scope.Items.Select(item => SurveyPlanScope.Create(
                    Guid.NewGuid(),
                    plan.Id,
                    item.RouteVersionId,
                    item.SegmentSetId,
                    item.SegmentIdsJson,
                    item.TargetBand)));
                _context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), request.ActorUserId, DateTimeOffset.UtcNow, "survey_plan_created", "SurveyPlan", plan.Id, null, "{}", "V2 survey plan created", "p2-54", request.CorrelationId, ["projectId", "routeVersionId", "surveyType"]));
                await _context.SaveChangesAsync(token);
                var view = ToPlanView(plan);
                return (Guid.NewGuid(), JsonSerializer.Serialize(view));
            }, cancellationToken);
            return MapPlanOutcome(outcome);
        }
        catch (ScopeNotFoundException) { return new(SurveyV2PersistenceStatus.NotFound); }
        catch (ScopeConflictException) { return new(SurveyV2PersistenceStatus.Conflict); }
        catch (ArgumentException) { return new(SurveyV2PersistenceStatus.InvalidInput); }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception)) { return new(SurveyV2PersistenceStatus.Conflict); }
    }

    public async Task<SurveyV2PlanPersistenceResult> PostponePlanAsync(SurveyV2PlanPostponementRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var outcome = await _idempotency.ExecuteAsync(request.ActorUserId, null, "SurveyPlanV2Postponed", request.IdempotencyKey, request.RequestFingerprint, async token =>
            {
                var plan = await _context.SurveyPlans.SingleOrDefaultAsync(candidate => candidate.Id == request.PlanId, token) ?? throw new ScopeNotFoundException();
                if (!TryDecodeVersion(request.ExpectedVersion, out var expectedRowVersion) || !plan.RowVersion.SequenceEqual(expectedRowVersion))
                {
                    return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredPlanOutcome(SurveyV2PersistenceStatus.ConcurrencyConflict, null)));
                }

                _context.Entry(plan).Property(value => value.RowVersion).OriginalValue = expectedRowVersion;

                var now = DateTimeOffset.UtcNow;
                plan.Postpone(null);
                var postponement = SurveyPlanPostponement.Create(
                    Guid.NewGuid(),
                    plan.Id,
                    now,
                    request.Reason,
                    null);
                _context.SurveyPlanPostponements.Add(postponement);
                _context.AuditLogs.Add(AuditLog.Create(
                    Guid.NewGuid(),
                    request.ActorUserId,
                    now,
                    "survey_plan_postponed",
                    "SurveyPlan",
                    plan.Id,
                    null,
                    JsonSerializer.Serialize(new { planId = plan.Id, status = plan.Status, reasonHash = Hash(request.Reason) }),
                    "V2 survey plan postponed",
                    "p2-55",
                    request.CorrelationId,
                    ["planId", "status", "reasonHash"]));
                await _context.SaveChangesAsync(token);
                var view = ToPlanView(plan);
                return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredPlanOutcome(SurveyV2PersistenceStatus.Success, view)));
            }, cancellationToken);
            var stored = JsonSerializer.Deserialize<StoredPlanOutcome>(outcome.OutcomeJson) ?? throw new InvalidOperationException("Invalid survey plan outcome.");
            if (outcome.Status == IdempotencyOperationStatus.Conflict) return new(SurveyV2PersistenceStatus.IdempotentConflict);
            return new(outcome.Status == IdempotencyOperationStatus.Replayed && stored.Status == SurveyV2PersistenceStatus.Success ? SurveyV2PersistenceStatus.Replayed : stored.Status, stored.Plan);
        }
        catch (ScopeNotFoundException) { return new(SurveyV2PersistenceStatus.NotFound); }
        catch (DbUpdateConcurrencyException) { _context.ChangeTracker.Clear(); return new(SurveyV2PersistenceStatus.ConcurrencyConflict); }
        catch (InvalidOperationException) { return new(SurveyV2PersistenceStatus.Conflict); }
    }

    public async Task<SurveyV2TaskPersistenceResult> CreateTaskAsync(SurveyV2TaskCreationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var outcome = await _idempotency.ExecuteAsync(request.ActorUserId, request.ProjectId, "SurveyTaskV2Created", request.IdempotencyKey, request.RequestFingerprint, async token =>
            {
                var scope = await ResolveScopeAsync(request.ProjectId, request.RouteVersionId, request.Scope, token);
                var operatorIsEligible = await _context.Users.AsNoTracking().AnyAsync(
                    user => user.Id == request.OperatorId &&
                            user.RoleCode == UserRoleCode.DroneOperator &&
                            user.Status == UserStatus.Active,
                    token);
                if (!operatorIsEligible) throw new OperatorNotFoundException();
                var accessPoint = request.AccessPointJson is null ? (JsonElement?)null : JsonSerializer.Deserialize<JsonElement>(request.AccessPointJson);
                var payload = JsonSerializer.Serialize(new { scope = JsonSerializer.Deserialize<JsonElement>(request.ScopeJson), accessPoint });
                var now = DateTimeOffset.UtcNow;
                var task = SurveyRequest.Create(Guid.NewGuid(), request.ProjectId, scope.RoadSectionId, null, request.ActorUserId, request.SurveyType, SurveyRequestStatus.NewAssigned, now, request.DueAt, payload, scope.Items[0].RouteVersionId);
                var assignment = SurveyAssignment.Create(Guid.NewGuid(), task.Id, request.OperatorId, request.ActorUserId, now, null, null, null, null, null);
                _context.SurveyRequests.Add(task);
                _context.SurveyAssignments.Add(assignment);
                _context.SurveyRequestScopes.AddRange(scope.Items.Select(item => SurveyRequestScope.Create(
                    Guid.NewGuid(),
                    task.Id,
                    item.RouteVersionId,
                    item.SegmentSetId,
                    item.SegmentIdsJson,
                    item.TargetBand)));
                _context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), request.ActorUserId, now, "survey_task_created", "SurveyRequest", task.Id, null, "{}", "V2 survey task created", "p2-11", request.CorrelationId, ["projectId", "routeVersionId", "operatorId", "surveyType"]));
                await _context.SaveChangesAsync(token);
                var view = ToTaskView(task, assignment.OperatorUserId);
                return (Guid.NewGuid(), JsonSerializer.Serialize(view));
            }, cancellationToken);
            if (outcome.Status == IdempotencyOperationStatus.Conflict) return new(SurveyV2PersistenceStatus.IdempotentConflict);
            var view = JsonSerializer.Deserialize<SurveyV2TaskPersistenceView>(outcome.OutcomeJson) ?? throw new InvalidOperationException("Invalid survey task outcome.");
            return new(outcome.Status == IdempotencyOperationStatus.Replayed ? SurveyV2PersistenceStatus.Replayed : SurveyV2PersistenceStatus.Success, view);
        }
        catch (ScopeNotFoundException) { return new(SurveyV2PersistenceStatus.NotFound); }
        catch (ScopeConflictException) { return new(SurveyV2PersistenceStatus.Conflict); }
        catch (OperatorNotFoundException) { return new(SurveyV2PersistenceStatus.OperatorNotFound); }
        catch (ArgumentException) { return new(SurveyV2PersistenceStatus.InvalidInput); }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception)) { return new(SurveyV2PersistenceStatus.Conflict); }
    }

    public async Task<SurveyV2TaskPersistenceView?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var row = await (
            from task in _context.SurveyRequests.AsNoTracking()
            join assignment in _context.SurveyAssignments.AsNoTracking() on task.Id equals assignment.SurveyRequestId
            where task.Id == taskId && assignment.EndedAt == null
            select new { task, assignment.OperatorUserId })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        var scopeRows = await _context.SurveyRequestScopes
            .AsNoTracking()
            .Where(scope => scope.SurveyRequestId == taskId)
            .OrderBy(scope => scope.Id)
            .ToListAsync(cancellationToken);

        if (scopeRows.Count == 0) return ToTaskView(row.task, row.OperatorUserId);

        JsonElement? accessPoint = null;
        try
        {
            using var payload = JsonDocument.Parse(row.task.OutputRequirements);
            if (payload.RootElement.TryGetProperty("accessPoint", out var accessPointElement) && accessPointElement.ValueKind != JsonValueKind.Null)
            {
                accessPoint = accessPointElement.Clone();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        var scopes = scopeRows.Select(scope => new
        {
            routeVersionId = scope.RouteSectionVersionId,
            segmentSetId = scope.SegmentSetId,
            segmentIds = JsonSerializer.Deserialize<JsonElement>(scope.SegmentIdsJson),
            targetBand = scope.TargetBand
        });
        var normalizedPayload = JsonSerializer.Serialize(new { scope = scopes, accessPoint });
        return ToTaskView(row.task, row.OperatorUserId, normalizedPayload);
    }

    public Task<Guid?> GetPlanProjectIdAsync(Guid planId, CancellationToken cancellationToken = default)
        => _context.SurveyPlans.AsNoTracking().Where(plan => plan.Id == planId).Select(plan => (Guid?)plan.ProjectId).SingleOrDefaultAsync(cancellationToken);

    private async Task<ScopeResolution> ResolveScopeAsync(Guid projectId, Guid routeVersionId, IReadOnlyList<SurveyV2ScopeRequest>? requestedScope, CancellationToken cancellationToken)
    {
        if (requestedScope is not { Count: > 0 })
        {
            throw new ScopeConflictException();
        }

        var items = requestedScope;
        var routeVersionIds = items.Select(item => item.RouteVersionId).Distinct().ToArray();
        var resolved = await (from project in _context.Projects.AsNoTracking()
                              join section in _context.RoadSections.AsNoTracking() on project.Id equals section.ProjectId
                              join version in _context.RoadSectionVersions.AsNoTracking() on section.Id equals version.RoadSectionId
                              where project.Id == projectId && routeVersionIds.Contains(version.Id)
                              select new { RouteVersionId = version.Id, RoadSectionId = section.Id })
            .ToDictionaryAsync(value => value.RouteVersionId, value => value.RoadSectionId, cancellationToken);
        if (resolved.Count != routeVersionIds.Length)
        {
            var projectExists = await _context.Projects.AsNoTracking().AnyAsync(project => project.Id == projectId, cancellationToken);
            throw projectExists ? new ScopeConflictException() : new ScopeNotFoundException();
        }

        var resolvedItems = new List<ResolvedScopeItem>(items.Count);
        foreach (var item in items)
        {
            Guid[] segmentIds;
            try
            {
                segmentIds = JsonSerializer.Deserialize<Guid[]>(item.SegmentIdsJson) ?? [];
            }
            catch (JsonException)
            {
                throw new ScopeConflictException();
            }

            var publishedSet = await _context.RoadSegmentSets.AsNoTracking()
                .AnyAsync(set => set.Id == item.SegmentSetId &&
                                 set.RoadSectionVersionId == item.RouteVersionId &&
                                 set.Status == "PUBLISHED", cancellationToken);
            if (!publishedSet || segmentIds.Length == 0)
            {
                throw new ScopeConflictException();
            }

            var segmentCount = await _context.RoadSegments.AsNoTracking()
                .CountAsync(segment => segment.SegmentSetId == item.SegmentSetId &&
                                       segment.RoadSectionVersionId == item.RouteVersionId &&
                                       segmentIds.Contains(segment.Id), cancellationToken);
            if (segmentCount != segmentIds.Distinct().Count())
            {
                throw new ScopeConflictException();
            }

            resolvedItems.Add(new ResolvedScopeItem(
                item.RouteVersionId,
                item.SegmentSetId,
                item.SegmentIdsJson,
                item.TargetBand,
                resolved[item.RouteVersionId]));
        }
        return new ScopeResolution(resolvedItems[0].RoadSectionId, resolvedItems);
    }

    private static SurveyV2PlanPersistenceResult MapPlanOutcome(IdempotencyOperationResult outcome)
    {
        if (outcome.Status == IdempotencyOperationStatus.Conflict) return new(SurveyV2PersistenceStatus.IdempotentConflict);
        var view = JsonSerializer.Deserialize<SurveyV2PlanPersistenceView>(outcome.OutcomeJson) ?? throw new InvalidOperationException("Invalid survey plan outcome.");
        return new(outcome.Status == IdempotencyOperationStatus.Replayed ? SurveyV2PersistenceStatus.Replayed : SurveyV2PersistenceStatus.Success, view);
    }

    private static SurveyV2PlanPersistenceView ToPlanView(SurveyPlan plan)
        => new(plan.Id, plan.ProjectId, plan.OutputRequirements, plan.PlannedStartAt, plan.Status.ToString(), Version(plan));

    private static SurveyV2TaskPersistenceView ToTaskView(SurveyRequest task, Guid operatorId)
        => ToTaskView(task, operatorId, task.OutputRequirements);

    private static SurveyV2TaskPersistenceView ToTaskView(SurveyRequest task, Guid operatorId, string outputRequirements)
        => new(task.Id, task.ProjectId, outputRequirements, operatorId, task.Status.ToString(), Version(task), task.DueAt, null);

    private static string Version(SurveyPlan plan) => Convert.ToBase64String(plan.RowVersion);
    private static string Version(SurveyRequest task) => Convert.ToBase64String(task.RowVersion);
    private static string Hash(string value) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool TryDecodeVersion(string value, out byte[] rowVersion)
    {
        try
        {
            rowVersion = Convert.FromBase64String(value.Trim().Trim('"'));
            return rowVersion.Length > 0;
        }
        catch (FormatException)
        {
            rowVersion = [];
            return false;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is SqlException { Number: 2601 or 2627 }) return true;
            current = current.InnerException;
        }
        return false;
    }

    private sealed record StoredPlanOutcome(SurveyV2PersistenceStatus Status, SurveyV2PlanPersistenceView? Plan);
    private sealed record ResolvedScopeItem(Guid RouteVersionId, Guid SegmentSetId, string SegmentIdsJson, string TargetBand, Guid RoadSectionId);
    private sealed record ScopeResolution(Guid RoadSectionId, IReadOnlyList<ResolvedScopeItem> Items);
    private sealed class ScopeNotFoundException : Exception;
    private sealed class ScopeConflictException : Exception;
    private sealed class OperatorNotFoundException : Exception;
}
