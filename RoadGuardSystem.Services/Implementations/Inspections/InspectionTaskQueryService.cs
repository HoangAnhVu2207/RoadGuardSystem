using RoadGuardSystem.Repositories.Mobile;
using System.Globalization;
using System.Text;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Inspections;

public sealed class InspectionTaskQueryService : IInspectionTaskQueryService
{
    private readonly IInspectionTaskReadRepository _repository;
    private readonly IProjectScopeGuard _scopeGuard;

    public InspectionTaskQueryService(
        IInspectionTaskReadRepository repository,
        IProjectScopeGuard scopeGuard)
    {
        _repository = repository;
        _scopeGuard = scopeGuard;
    }

    public async Task<InspectionTaskQueryResult> ListAssignedAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        string? cursor,
        int limit,
        RoadGuardSystem.Repositories.Mobile.MobileTaskListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || actorRole == UserRoleCode.Unknown)
        {
            return new(InspectionTaskQueryStatus.Unauthorized, null);
        }

        if (actorRole != UserRoleCode.RepairCrew)
        {
            return new(InspectionTaskQueryStatus.Forbidden, null);
        }

        if (limit is < 1 or > 100)
        {
            return new(InspectionTaskQueryStatus.InvalidInput, null);
        }

        string binding; DateTimeOffset? afterDueAt; Guid? afterId;
        try
        {
            filter = (filter ?? new()).Normalize(false);
            binding = filter.Binding(actorUserId, "crew-inspection", limit);
            (afterDueAt, afterId) = MobileTaskListFilter.Decode(cursor, binding);
        }
        catch (ArgumentException) { return new(InspectionTaskQueryStatus.InvalidCursor, null); }

        var result = await _repository.ListAssignedAsync(
            actorUserId,
            afterDueAt,
            afterId,
            limit,
            filter, cancellationToken);

        var visible = new List<InspectionTaskResponseDto>(result.Items.Count);
        var scopeCache = new Dictionary<Guid, bool>();
        foreach (var item in result.Items)
        {
            if (!scopeCache.TryGetValue(item.ProjectId, out var inScope))
            {
                inScope = await _scopeGuard.AuthorizeAsync(
                    actorUserId,
                    actorRole,
                    item.ProjectId,
                    cancellationToken) is not null;
                scopeCache[item.ProjectId] = inScope;
            }

            if (!inScope)
            {
                continue;
            }

            visible.Add(new(
                item.Id,
                item.ProjectId,
                item.IsHistory ? [] : [item.DefectId],
                item.Mode,
                item.AssignedToUserId,
                item.PolicyRevisionId,
                item.IsHistory ? null : item.Status.ToString().ToUpperInvariant(),
                item.IsHistory ? "" : Convert.ToBase64String(item.RowVersion), item.TaskCode, item.IsHistory ? null : item.DueAt, item.AssignmentId, item.AssignmentStatus, item.AcceptanceOutcome, item.AcceptedAt, item.RejectedAt, item.IsHistory));
        }

        var nextCursor = result.HasMore && result.LastDueAt is not null && result.LastId is not null
            ? MobileTaskListFilter.Encode(binding, result.LastDueAt.Value, result.LastId.Value)
            : null;

        return new(
            InspectionTaskQueryStatus.Success,
            new(visible, nextCursor, result.AsOf));
    }

}
