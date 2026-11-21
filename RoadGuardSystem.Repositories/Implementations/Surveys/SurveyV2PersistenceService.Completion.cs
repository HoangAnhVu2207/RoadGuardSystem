using System.Text.Json;
using RoadGuardSystem.Repositories.Idempotency;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Surveys;
namespace RoadGuardSystem.Repositories.Implementations.Surveys;

public sealed partial class SurveyV2PersistenceService
{
    private Task<IdempotencyOperationResult> ExecuteTaskMutationAsync(SurveyV2TaskMutationRequest request, Guid project,
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> handler, CancellationToken token)
        => request.Operation == "complete"
            ? _idempotency.ExecuteSerializableAsync(request.ActorUserId, project, "SurveyTaskV2complete", request.IdempotencyKey, request.RequestFingerprint,
                handler, token, ct => GuardCompletionActorAsync(request.ActorUserId, project, ct))
            : _idempotency.ExecuteAsync(request.ActorUserId, project, $"SurveyTaskV2{request.Operation}", request.IdempotencyKey, request.RequestFingerprint, handler, token);
    private async Task GuardCompletionActorAsync(Guid actor, Guid project, CancellationToken token)
    {
        await RoadGuardSystem.Repositories.Idempotency.Anh02ReceiptAuthority.LockAsync(_context, actor, project, token);
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        if (!await _context.Users.AnyAsync(x => x.Id == actor && x.RoleCode == UserRoleCode.ProjectManager && x.Status == UserStatus.Active && !x.MustChangePassword &&
            _context.Roles.Any(r => r.Code == x.RoleCode && r.IsActive), token) ||
            !await _context.ProjectMembers.AnyAsync(x => x.UserId == actor && x.ProjectId == project && x.RoleCode == UserRoleCode.ProjectManager && x.Status == ProjectMemberStatus.Active && x.ValidFrom <= today && (x.ValidTo == null || x.ValidTo >= today), token))
            throw new UnauthorizedAccessException("Current project PM authority required.");
    }

    private async Task CompleteTaskInTransactionAsync(SurveyV2TaskMutationRequest request, SurveyRequest task, CancellationToken token)
    {
        await GuardCompletionActorAsync(request.ActorUserId, task.ProjectId, token);
        var source = await (from dataset in _context.SurveyDataVersions
                            join survey in _context.Surveys on dataset.SurveyId equals survey.Id
                            where dataset.Id == request.DatasetId && survey.SurveyRequestId == task.Id && survey.ProjectId == task.ProjectId && dataset.Status == SurveyDataVersionStatus.ServerConfirmed
                            select dataset).SingleOrDefaultAsync(token);
        if (source is null) throw new InvalidOperationException("The selected submitted dataset must belong to this exact task.");
        var assessment = await SurveyAssessmentRepository.LatestFirst(_context.Set<DatasetAssessment>().Where(x => x.DatasetId == source.Id)).FirstOrDefaultAsync(token);
        if (assessment is null || assessment.Id != request.AssessmentId || assessment.MethodVersion != "pm-evidence-review.v1") throw new InvalidOperationException("Select the current assessment for this dataset.");
        if (task.Status != SurveyRequestStatus.Submitted) throw new InvalidOperationException("Only a submitted task can complete.");
        var scopes = await _context.SurveyRequestScopes.Where(x => x.SurveyRequestId == task.Id).ToArrayAsync(token);
        var required = scopes.SelectMany(x => (JsonSerializer.Deserialize<Guid[]>(x.SegmentIdsJson) ?? []).Select(segment => (x.RouteSectionVersionId, x.SegmentSetId, SegmentId: segment, x.TargetBand))).ToHashSet();
        if (scopes.Length == 0)
        {
            using var payload = JsonDocument.Parse(task.OutputRequirements);
            if (!payload.RootElement.TryGetProperty("scope", out var scope)) throw new InvalidOperationException("Task scope is unavailable.");
            var items = scope.Deserialize<DatasetScopeItem[]>(DatasetScopeJsonOptions) ?? [];
            ValidateCompletionScope(items);
            required = items.SelectMany(x => x.SegmentIds.Select(segment => (x.RouteVersionId, x.SegmentSetId, SegmentId: segment, x.TargetBand))).ToHashSet();
        }
        var datasetScope = JsonSerializer.Deserialize<DatasetScopeItem[]>(source.ScopeManifest ?? "[]", DatasetScopeJsonOptions) ?? [];
        ValidateCompletionScope(datasetScope);
        var submitted = datasetScope.SelectMany(x => x.SegmentIds.Select(segment => (x.RouteVersionId, x.SegmentSetId, SegmentId: segment, x.TargetBand))).ToHashSet();
        var reviewed = await _context.Set<DatasetAssessmentItem>().Where(x => x.AssessmentId == assessment.Id).ToArrayAsync(token);
        if (required.Count == 0 || required.Any(x => x.Item1 == Guid.Empty || x.SegmentSetId == Guid.Empty || x.SegmentId == Guid.Empty || x.TargetBand is not ("SURFACE" or "LEFT_EDGE" or "RIGHT_EDGE")) || !required.IsSubsetOf(submitted) || required.Any(key => !reviewed.Any(x =>
            (x.RouteVersionId, x.SegmentSetId, x.SegmentId, x.TargetBand) == key && x.PositionStatus == "PASS" && x.QualityStatus == "PASS" && x.CoverageStatus == "PASS")))
            throw new InvalidOperationException("Every task scope requires a position, quality and coverage PASS.");
        task.Complete();
    }
    private static void ValidateCompletionScope(DatasetScopeItem[] items)
    {
        if (items.Length == 0 || items.Any(x => x is null || x.RouteVersionId == Guid.Empty || x.SegmentSetId == Guid.Empty ||
            x.SegmentIds is not { Length: > 0 } || x.SegmentIds.Any(id => id == Guid.Empty) || x.TargetBand is not ("SURFACE" or "LEFT_EDGE" or "RIGHT_EDGE")))
            throw new InvalidOperationException("A complete pinned task scope is required.");
    }

}
