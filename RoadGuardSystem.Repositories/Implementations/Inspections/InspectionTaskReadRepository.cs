using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Repositories.Inspections;

public sealed class InspectionTaskReadRepository : IInspectionTaskReadRepository
{
    private readonly RoadGuardDbContext _context;

    public InspectionTaskReadRepository(RoadGuardDbContext context)
    {
        _context = context;
    }

    public async Task<InspectionTaskPagePersistenceResult> ListAssignedAsync(
        Guid assignedToUserId,
        DateTimeOffset? afterDueAt,
        Guid? afterId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (assignedToUserId == Guid.Empty || limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        var query =
            from task in _context.FieldInspectionTasks.AsNoTracking()
            join assignment in _context.FieldInspectionAssignments.AsNoTracking()
                on task.Id equals assignment.FieldInspectionTaskId
            where assignment.AssignedToUserId == assignedToUserId &&
                  assignment.Status == FieldInspectionAssignmentStatus.Active
            select new
            {
                task.Id,
                task.ProjectId,
                task.DefectId,
                assignment.AssignedToUserId,
                task.Status,
                task.RequiredMeasurementType,
                task.TaskCode,
                task.DueAt,
                task.RowVersion
            };

        if (afterDueAt is not null && afterId is not null)
        {
            query = query.Where(item => item.DueAt > afterDueAt ||
                (item.DueAt == afterDueAt && item.Id.CompareTo(afterId.Value) > 0));
        }

        var rows = await query
            .OrderBy(item => item.DueAt)
            .ThenBy(item => item.Id)
            .Take(limit + 1)
            .Select(item => new InspectionTaskPersistenceView(
                item.Id,
                item.ProjectId,
                item.DefectId,
                item.AssignedToUserId,
                item.Status,
                item.RequiredMeasurementType,
                item.TaskCode,
                item.DueAt,
                item.RowVersion))
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        if (hasMore)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        var last = rows.LastOrDefault();
        return new(
            rows,
            last?.DueAt,
            last?.Id,
            hasMore,
            DateTimeOffset.UtcNow);
    }
}
