using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Files;
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
            where task.Id == taskId
            orderby assignment.EndedAt == null descending, assignment.AssignedAt descending
            select new { task, assignment.OperatorUserId })
            .FirstOrDefaultAsync(cancellationToken);
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

    public async Task<SurveyV2TaskPagePersistenceResult> ListMyTasksAsync(Guid operatorUserId, string? cursor, int limit, CancellationToken cancellationToken = default)
    {
        if (operatorUserId == Guid.Empty || limit is < 1 or > 100) throw new ArgumentException("Survey task list parameters are invalid.");
        var decoded = DecodeCursor(cursor);
        var cursorRequestedAt = decoded?.RequestedAt;
        var cursorId = decoded?.Id ?? Guid.Empty;
        var query =
            from task in _context.SurveyRequests.AsNoTracking()
            join assignment in _context.SurveyAssignments.AsNoTracking() on task.Id equals assignment.SurveyRequestId
            where assignment.OperatorUserId == operatorUserId && assignment.EndedAt == null
            where !cursorRequestedAt.HasValue || task.RequestedAt > cursorRequestedAt.Value || task.RequestedAt == cursorRequestedAt.Value && task.Id.CompareTo(cursorId) > 0
            orderby task.RequestedAt, task.Id
            select new { task, assignment.OperatorUserId };
        var rows = await query.Take(limit + 1).ToListAsync(cancellationToken);
        var hasMore = rows.Count > limit;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        var taskIds = rows.Select(row => row.task.Id).ToArray();
        var scopeRows = await _context.SurveyRequestScopes.AsNoTracking()
            .Where(scope => taskIds.Contains(scope.SurveyRequestId))
            .OrderBy(scope => scope.Id)
            .ToListAsync(cancellationToken);
        var scopesByTask = scopeRows.GroupBy(scope => scope.SurveyRequestId).ToDictionary(group => group.Key, group => group.ToArray());
        var items = rows.Select(row =>
        {
            if (!scopesByTask.TryGetValue(row.task.Id, out var scopes) || scopes.Length == 0) return ToTaskView(row.task, row.OperatorUserId);
            var payloadScopes = scopes.Select(scope => new
            {
                routeVersionId = scope.RouteSectionVersionId,
                segmentSetId = scope.SegmentSetId,
                segmentIds = JsonSerializer.Deserialize<JsonElement>(scope.SegmentIdsJson),
                targetBand = scope.TargetBand
            });
            return ToTaskView(row.task, row.OperatorUserId, JsonSerializer.Serialize(new { scope = payloadScopes }));
        }).ToArray();
        var nextCursor = hasMore && rows.Count > 0 ? EncodeCursor(rows[^1].task.RequestedAt, rows[^1].task.Id) : null;
        return new(items, nextCursor, DateTimeOffset.UtcNow);
    }

    public async Task<SurveyV2TaskPersistenceResult> MutateTaskAsync(SurveyV2TaskMutationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ActorUserId == Guid.Empty || request.TaskId == Guid.Empty) return new(SurveyV2PersistenceStatus.InvalidInput);
        var projectId = await _context.SurveyRequests.AsNoTracking()
            .Where(task => task.Id == request.TaskId)
            .Select(task => (Guid?)task.ProjectId)
            .SingleOrDefaultAsync(cancellationToken);
        if (projectId is null) return new(SurveyV2PersistenceStatus.NotFound);

        try
        {
            var outcome = await _idempotency.ExecuteAsync(request.ActorUserId, projectId.Value, $"SurveyTaskV2{request.Operation}", request.IdempotencyKey, request.RequestFingerprint, async token =>
            {
                var task = await _context.SurveyRequests.SingleOrDefaultAsync(candidate => candidate.Id == request.TaskId, token)
                    ?? throw new ScopeNotFoundException();
                var assignment = await _context.SurveyAssignments
                    .Where(candidate => candidate.SurveyRequestId == task.Id)
                    .OrderByDescending(candidate => candidate.EndedAt == null)
                    .ThenByDescending(candidate => candidate.AssignedAt)
                    .FirstOrDefaultAsync(token)
                    ?? throw new ScopeNotFoundException();
                if (!TryDecodeVersion(request.ExpectedVersion, out var expectedRowVersion) || !task.RowVersion.SequenceEqual(expectedRowVersion))
                {
                    return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredTaskOutcome(SurveyV2PersistenceStatus.ConcurrencyConflict, null)));
                }

                _context.Entry(task).Property(value => value.RowVersion).OriginalValue = expectedRowVersion;
                var now = DateTimeOffset.UtcNow;
                switch (request.Operation)
                {
                    case "accept":
                        if (assignment.EndedAt is not null || assignment.OperatorUserId != request.ActorUserId) return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredTaskOutcome(SurveyV2PersistenceStatus.Conflict, null)));
                        assignment.Accept(now);
                        task.Accept();
                        break;
                    case "decline":
                        if (assignment.EndedAt is not null || assignment.OperatorUserId != request.ActorUserId || string.IsNullOrWhiteSpace(request.Reason)) return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredTaskOutcome(SurveyV2PersistenceStatus.Conflict, null)));
                        assignment.Reject(now, request.Reason);
                        task.MarkRejected();
                        break;
                    case "cancel":
                        var hasServerConfirmedDataset = await (
                            from survey in _context.Surveys.AsNoTracking()
                            join dataVersion in _context.SurveyDataVersions.AsNoTracking() on survey.Id equals dataVersion.SurveyId
                            where survey.SurveyRequestId == task.Id && dataVersion.Status == SurveyDataVersionStatus.ServerConfirmed
                            select dataVersion.Id)
                            .AnyAsync(token);
                        if (hasServerConfirmedDataset) return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredTaskOutcome(SurveyV2PersistenceStatus.Conflict, null)));
                        task.Cancel(request.Reason ?? string.Empty, now);
                        break;
                    case "reassign":
                        if (request.OperatorId is null || string.IsNullOrWhiteSpace(request.Reason)) return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredTaskOutcome(SurveyV2PersistenceStatus.InvalidInput, null)));
                        var eligible = await _context.Users.AsNoTracking().AnyAsync(user => user.Id == request.OperatorId.Value && user.RoleCode == UserRoleCode.DroneOperator && user.Status == UserStatus.Active, token);
                        if (!eligible) return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredTaskOutcome(SurveyV2PersistenceStatus.OperatorNotFound, null)));
                        if (assignment.EndedAt is null) assignment.EndForReassignment(now, request.Reason);
                        var replacement = SurveyAssignment.Create(Guid.NewGuid(), task.Id, request.OperatorId.Value, request.ActorUserId, now, null, null, null, null, null);
                        _context.SurveyAssignments.Add(replacement);
                        task.MarkReassigned();
                        if (request.DueAt is { } reassignedDueAt) task.ChangeDueAt(reassignedDueAt);
                        assignment = replacement;
                        break;
                    case "supplement":
                        if (request.OperatorId is null || string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.ScopeJson)) return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredTaskOutcome(SurveyV2PersistenceStatus.InvalidInput, null)));
                        var supplementOperatorEligible = await _context.Users.AsNoTracking().AnyAsync(user => user.Id == request.OperatorId.Value && user.RoleCode == UserRoleCode.DroneOperator && user.Status == UserStatus.Active, token);
                        if (!supplementOperatorEligible) return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredTaskOutcome(SurveyV2PersistenceStatus.OperatorNotFound, null)));
                        var surveyId = await _context.Surveys.AsNoTracking().Where(survey => survey.SurveyRequestId == task.Id).Select(survey => (Guid?)survey.Id).SingleOrDefaultAsync(token);
                        if (surveyId is null) return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredTaskOutcome(SurveyV2PersistenceStatus.NotFound, null)));
                        var round = (await _context.SupplementarySurveyRequests.AsNoTracking().Where(item => item.SurveyId == surveyId.Value).MaxAsync(item => (int?)item.RoundNo, token) ?? 0) + 1;
                        _context.SupplementarySurveyRequests.Add(SupplementarySurveyRequest.Create(Guid.NewGuid(), surveyId.Value, task.Id, request.ActorUserId, request.Reason, JsonSerializer.Serialize(new { scope = JsonSerializer.Deserialize<JsonElement>(request.ScopeJson), operatorId = request.OperatorId }), round, SupplementarySurveyRequestStatus.Requested, null, null, "Original source remains immutable."));
                        task.MarkSupplementRequired();
                        break;
                    default:
                        return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredTaskOutcome(SurveyV2PersistenceStatus.InvalidInput, null)));
                }

                _context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), request.ActorUserId, now, $"survey_task_{request.Operation}", "SurveyRequest", task.Id, null, JsonSerializer.Serialize(new { operation = request.Operation, taskId = task.Id }), "V2 survey task workflow transition", $"p2-{request.Operation}", request.CorrelationId, ["operation", "taskId"]));
                await _context.SaveChangesAsync(token);
                var view = ToTaskView(task, assignment.OperatorUserId);
                return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredTaskOutcome(SurveyV2PersistenceStatus.Success, view)));
            }, cancellationToken);
            if (outcome.Status == IdempotencyOperationStatus.Conflict) return new(SurveyV2PersistenceStatus.IdempotentConflict);
            var stored = JsonSerializer.Deserialize<StoredTaskOutcome>(outcome.OutcomeJson) ?? throw new InvalidOperationException("Invalid survey task outcome.");
            return new(outcome.Status == IdempotencyOperationStatus.Replayed && stored.Status == SurveyV2PersistenceStatus.Success ? SurveyV2PersistenceStatus.Replayed : stored.Status, stored.Task);
        }
        catch (ScopeNotFoundException) { return new(SurveyV2PersistenceStatus.NotFound); }
        catch (DbUpdateConcurrencyException) { _context.ChangeTracker.Clear(); return new(SurveyV2PersistenceStatus.ConcurrencyConflict); }
        catch (ArgumentException) { return new(SurveyV2PersistenceStatus.InvalidInput); }
        catch (InvalidOperationException) { return new(SurveyV2PersistenceStatus.Conflict); }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception)) { return new(SurveyV2PersistenceStatus.Conflict); }
    }

    public Task<Guid?> GetPlanProjectIdAsync(Guid planId, CancellationToken cancellationToken = default)
        => _context.SurveyPlans.AsNoTracking().Where(plan => plan.Id == planId).Select(plan => (Guid?)plan.ProjectId).SingleOrDefaultAsync(cancellationToken);

    public async Task<SurveyDatasetPersistenceResult> SubmitDatasetAsync(SurveyDatasetSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ActorUserId == Guid.Empty || request.TaskId == Guid.Empty || request.DeviceId == Guid.Empty ||
            request.VideoFileIds is not { Count: > 0 } || request.VideoFileIds.Any(id => id == Guid.Empty) ||
            request.TelemetryFileIds is null || request.TelemetryFileIds.Any(id => id == Guid.Empty) ||
            request.RecordedAt == default || string.IsNullOrWhiteSpace(request.ScopeJson) ||
            string.IsNullOrWhiteSpace(request.ExpectedTaskVersion) || string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return new(SurveyDatasetPersistenceStatus.InvalidInput);
        }

        var projectId = await _context.SurveyRequests.AsNoTracking()
            .Where(task => task.Id == request.TaskId)
            .Select(task => (Guid?)task.ProjectId)
            .SingleOrDefaultAsync(cancellationToken);
        if (projectId is null)
        {
            return new(SurveyDatasetPersistenceStatus.NotFound);
        }

        try
        {
            var outcome = await _idempotency.ExecuteAsync(
                request.ActorUserId,
                projectId.Value,
                "SurveyDatasetSubmitted",
                request.IdempotencyKey,
                request.RequestFingerprint,
                async token =>
                {
                    var task = await _context.SurveyRequests.SingleOrDefaultAsync(value => value.Id == request.TaskId, token)
                        ?? throw new ScopeNotFoundException();
                    var assignment = await _context.SurveyAssignments
                        .Where(value => value.SurveyRequestId == task.Id && value.EndedAt == null)
                        .OrderByDescending(value => value.AssignedAt)
                        .FirstOrDefaultAsync(token)
                        ?? throw new ScopeNotFoundException();
                    if (assignment.OperatorUserId != request.ActorUserId ||
                        task.Status is SurveyRequestStatus.Cancelled or SurveyRequestStatus.Completed or SurveyRequestStatus.Rejected)
                    {
                        return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.Conflict, null)));
                    }

                    if (!TryDecodeVersion(request.ExpectedTaskVersion, out var expectedVersion) || !task.RowVersion.SequenceEqual(expectedVersion))
                    {
                        return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.ConcurrencyConflict, null)));
                    }
                    _context.Entry(task).Property(value => value.RowVersion).OriginalValue = expectedVersion;

                    var allFileIds = request.VideoFileIds.Concat(request.TelemetryFileIds).Distinct().ToArray();
                    if (allFileIds.Length != request.VideoFileIds.Count + request.TelemetryFileIds.Count)
                    {
                        return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.InvalidInput, null)));
                    }

                    var fileRows = await (
                        from file in _context.Files.AsNoTracking()
                        join fileScope in _context.FileScopes.AsNoTracking() on file.Id equals fileScope.FileId
                        join upload in _context.UploadSessions.AsNoTracking() on file.Id equals upload.FileId
                        where allFileIds.Contains(file.Id)
                        select new { file.Id, file.Checksum, fileScope.ProjectId, fileScope.TargetId, upload.Status })
                        .ToListAsync(token);
                    if (fileRows.Count != allFileIds.Length || fileRows.Any(value =>
                            value.ProjectId != task.ProjectId || value.TargetId != task.Id || value.Status != UploadSessionStatus.Verified))
                    {
                        return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.Conflict, null)));
                    }

                    var survey = await _context.Surveys.SingleOrDefaultAsync(value => value.SurveyRequestId == task.Id, token);
                    if (survey is null)
                    {
                        if (task.RoadSectionVersionId is null)
                        {
                            return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.Conflict, null)));
                        }

                        survey = Survey.Create(Guid.NewGuid(), task.Id, task.ProjectId, task.RoadSectionVersionId.Value, task.SurveyType, SurveyStatus.Submitted, false, null, null);
                        _context.Surveys.Add(survey);
                    }

                    var versionNo = (await _context.SurveyDataVersions.AsNoTracking()
                        .Where(value => value.SurveyId == survey.Id)
                        .Select(value => (int?)value.VersionNo)
                        .MaxAsync(token) ?? 0) + 1;
                    var sourceManifest = JsonSerializer.Serialize(fileRows.OrderBy(value => value.Id).Select(value => new { fileId = value.Id, checksumSha256 = value.Checksum }));
                    var dataVersion = SurveyDataVersion.CreateSubmitted(Guid.NewGuid(), survey.Id, versionNo, request.RecordedAt, DateTimeOffset.UtcNow, request.DeviceId, sourceManifest, request.ScopeJson);
                    _context.SurveyDataVersions.Add(dataVersion);
                    _context.SurveyFiles.AddRange(fileRows.Select(value => SurveyFile.Create(
                        Guid.NewGuid(), survey.Id, null, value.Id,
                        request.VideoFileIds.Contains(value.Id) ? SurveyFileType.Video : SurveyFileType.Srt,
                        request.RecordedAt, request.RecordedAt, SurveyFileSyncStatus.ServerConfirmed, value.Checksum)));
                    task.MarkSubmitted();
                    var now = DateTimeOffset.UtcNow;
                    _context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), request.ActorUserId, now, "survey_dataset_submitted", "SurveyDataVersion", dataVersion.Id, null,
                        JsonSerializer.Serialize(new { surveyTaskId = task.Id, dataVersionId = dataVersion.Id, allFileIds }), "Survey dataset submitted", "p2-019", request.CorrelationId, ["surveyTaskId", "dataVersionId"]));
                    await _context.SaveChangesAsync(token);
                    var view = new SurveyDatasetPersistenceView(dataVersion.Id, task.Id, task.ProjectId, dataVersion.Id, "PASSED", request.TelemetryFileIds.Count == 0 ? "MISSING" : "PRESENT", Version(dataVersion), request.ScopeJson);
                    return (dataVersion.Id, JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.Success, view)));
                },
                cancellationToken);
            if (outcome.Status == IdempotencyOperationStatus.Conflict)
            {
                return new(SurveyDatasetPersistenceStatus.IdempotentConflict);
            }
            var stored = JsonSerializer.Deserialize<StoredDatasetOutcome>(outcome.OutcomeJson)
                ?? throw new InvalidOperationException("Dataset idempotency outcome is invalid.");
            return new(outcome.Status == IdempotencyOperationStatus.Replayed && stored.Status == SurveyDatasetPersistenceStatus.Success
                ? SurveyDatasetPersistenceStatus.Replayed
                : stored.Status, stored.Dataset);
        }
        catch (ScopeNotFoundException) { return new(SurveyDatasetPersistenceStatus.NotFound); }
        catch (DbUpdateConcurrencyException) { _context.ChangeTracker.Clear(); return new(SurveyDatasetPersistenceStatus.ConcurrencyConflict); }
        catch (ArgumentException) { return new(SurveyDatasetPersistenceStatus.InvalidInput); }
        catch (InvalidOperationException) { return new(SurveyDatasetPersistenceStatus.Conflict); }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception)) { return new(SurveyDatasetPersistenceStatus.Conflict); }
    }

    public async Task<SurveyDatasetAccessView?> GetDatasetAccessAsync(Guid datasetId, CancellationToken cancellationToken = default)
    {
        var row = await (
            from dataset in _context.SurveyDataVersions.AsNoTracking()
            join survey in _context.Surveys.AsNoTracking() on dataset.SurveyId equals survey.Id
            join task in _context.SurveyRequests.AsNoTracking() on survey.SurveyRequestId equals task.Id
            join assignment in _context.SurveyAssignments.AsNoTracking() on task.Id equals assignment.SurveyRequestId
            where dataset.Id == datasetId && assignment.EndedAt == null
            orderby assignment.AssignedAt descending
            select new { dataset, task, assignment.OperatorUserId })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null
            ? null
            : new SurveyDatasetAccessView(row.dataset.Id, row.task.Id, row.task.ProjectId, row.OperatorUserId, row.dataset.ScopeManifest ?? "[]", Version(row.dataset));
    }

    private async Task<ScopeResolution> ResolveScopeAsync(Guid projectId, Guid routeVersionId, IReadOnlyList<SurveyV2ScopeRequest>? requestedScope, CancellationToken cancellationToken)
    {
        if (projectId == Guid.Empty || routeVersionId == Guid.Empty || requestedScope is not { Count: > 0 })
        {
            throw new ScopeConflictException();
        }

        var items = requestedScope;
        if (!items.Any(item => item.RouteVersionId == routeVersionId))
        {
            throw new ScopeConflictException();
        }

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
    private static string Version(SurveyDataVersion version) => Convert.ToBase64String(version.RowVersion);
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

    private static string EncodeCursor(DateTimeOffset requestedAt, Guid id)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new TaskCursor(requestedAt, id))));

    private static TaskCursor? DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return null;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var decoded = JsonSerializer.Deserialize<TaskCursor>(json);
            if (decoded is null || decoded.Id == Guid.Empty || decoded.RequestedAt == default)
            {
                throw new ArgumentException("Cursor is invalid.");
            }

            return decoded;
        }
        catch (FormatException exception) { throw new ArgumentException("Cursor is invalid.", exception); }
        catch (JsonException exception) { throw new ArgumentException("Cursor is invalid.", exception); }
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
    private sealed record StoredTaskOutcome(SurveyV2PersistenceStatus Status, SurveyV2TaskPersistenceView? Task);
    private sealed record StoredDatasetOutcome(SurveyDatasetPersistenceStatus Status, SurveyDatasetPersistenceView? Dataset);
    private sealed record TaskCursor(DateTimeOffset RequestedAt, Guid Id);
    private sealed record ResolvedScopeItem(Guid RouteVersionId, Guid SegmentSetId, string SegmentIdsJson, string TargetBand, Guid RoadSectionId);
    private sealed record ScopeResolution(Guid RoadSectionId, IReadOnlyList<ResolvedScopeItem> Items);
    private sealed class ScopeNotFoundException : Exception;
    private sealed class ScopeConflictException : Exception;
    private sealed class OperatorNotFoundException : Exception;
}
