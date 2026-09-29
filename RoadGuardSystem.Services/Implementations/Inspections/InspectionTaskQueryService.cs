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
        CancellationToken cancellationToken = default)
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

        if (!TryDecodeCursor(cursor, out var afterDueAt, out var afterId))
        {
            return new(InspectionTaskQueryStatus.InvalidCursor, null);
        }

        var result = await _repository.ListAssignedAsync(
            actorUserId,
            afterDueAt,
            afterId,
            limit,
            cancellationToken);

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
                [item.DefectId],
                "MEASURE_ONLY",
                item.AssignedToUserId,
                null,
                item.Status.ToString().ToUpperInvariant(),
                Convert.ToBase64String(item.RowVersion)));
        }

        var nextCursor = result.HasMore && result.LastDueAt is not null && result.LastId is not null
            ? EncodeCursor(result.LastDueAt.Value, result.LastId.Value)
            : null;

        return new(
            InspectionTaskQueryStatus.Success,
            new(visible, nextCursor, result.AsOf));
    }

    private static string EncodeCursor(DateTimeOffset dueAt, Guid id)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{dueAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}|{id:D}"));

    private static bool TryDecodeCursor(
        string? cursor,
        out DateTimeOffset? dueAt,
        out Guid? id)
    {
        dueAt = null;
        id = null;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        try
        {
            var value = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = value.Split('|', 2);
            if (parts.Length != 2 ||
                !long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks) ||
                !Guid.TryParse(parts[1], out var parsedId))
            {
                return false;
            }

            dueAt = new DateTimeOffset(ticks, TimeSpan.Zero);
            id = parsedId;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
