using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Mobile;
namespace RoadGuardSystem.Repositories.Inspections;

public sealed class InspectionTaskReadRepository(RoadGuardDbContext context, TimeProvider? clock = null) : IInspectionTaskReadRepository
{
    public async Task<InspectionTaskPagePersistenceResult> ListAssignedAsync(Guid assignedToUserId, DateTimeOffset? afterDueAt,
        Guid? afterId, int limit, MobileTaskListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        if (assignedToUserId == Guid.Empty || limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit));
        filter = (filter ?? new()).Normalize(false);
        var today = DateOnly.FromDateTime((clock ?? TimeProvider.System).GetUtcNow().UtcDateTime);
        var rejected = filter.Acceptance == "REJECTED";
        var query = from task in context.FieldInspectionTasks.AsNoTracking()
                    join assignment in context.FieldInspectionAssignments.AsNoTracking() on task.Id equals assignment.FieldInspectionTaskId
                    where assignment.AssignedToUserId == assignedToUserId &&
                        (rejected ? assignment.Status == FieldInspectionAssignmentStatus.Rejected : assignment.Status == FieldInspectionAssignmentStatus.Active && assignment.EndedAt == null)
                    where context.ProjectMembers.Any(m => m.ProjectId == task.ProjectId && m.UserId == assignedToUserId && m.RoleCode == UserRoleCode.RepairCrew &&
                        m.Status == ProjectMemberStatus.Active && m.ValidFrom <= today && (m.ValidTo == null || m.ValidTo >= today))
                    let acceptedAt = context.Set<FieldInspectionTaskEvent>().Where(e => e.TaskId == task.Id && e.AssignmentId == assignment.Id && e.Kind == "ACCEPTED")
                        .Select(e => (DateTimeOffset?)e.OccurredAt).Min()
                    select new
                    {
                        task,
                        assignment,
                        acceptedAt,
                        Mode = rejected ? null : task.TaskMode == "MEASURE_ONLY" && task.RepairItemId == null ? "MEASURE_ONLY" : context.Set<RepairFieldTaskBinding>().Where(b => b.TaskId == task.Id && b.AssignmentId == assignment.Id && b.CrewId == assignedToUserId &&
                        b.ProjectId == task.ProjectId && b.DefectId == task.DefectId && b.ItemId == task.RepairItemId &&
                        context.RepairItems.Any(i => i.Id == b.ItemId && i.CurrentBindingId == b.Id && i.SupersededByItemId == null && i.CrewId == assignedToUserId && i.Mode == b.Mode &&
                            context.RepairObligations.Any(o => o.Id == i.ObligationId && o.CurrentRepairItemId == i.Id)) &&
                        (task.TaskMode == "NORMAL" && b.Mode == RepairMode.Normal || task.TaskMode == "CONDITIONAL_FT" && b.Mode == RepairMode.FastTrack)).Count() == 1 ? task.TaskMode : null,
                        PolicyId = rejected || context.Set<RepairFieldTaskBinding>().Where(b => b.TaskId == task.Id && b.AssignmentId == assignment.Id && b.CrewId == assignedToUserId &&
                        b.ProjectId == task.ProjectId && b.DefectId == task.DefectId && b.ItemId == task.RepairItemId &&
                        context.RepairItems.Any(i => i.Id == b.ItemId && i.CurrentBindingId == b.Id && i.SupersededByItemId == null && i.CrewId == assignedToUserId && i.Mode == b.Mode &&
                            context.RepairObligations.Any(o => o.Id == i.ObligationId && o.CurrentRepairItemId == i.Id)) &&
                        (task.TaskMode == "NORMAL" && b.Mode == RepairMode.Normal || task.TaskMode == "CONDITIONAL_FT" && b.Mode == RepairMode.FastTrack)).Count() != 1 ? null : context.Set<RepairFieldTaskBinding>().Where(b => b.TaskId == task.Id && b.AssignmentId == assignment.Id && b.CrewId == assignedToUserId &&
                        b.ProjectId == task.ProjectId && b.DefectId == task.DefectId && b.ItemId == task.RepairItemId &&
                        context.RepairItems.Any(i => i.Id == b.ItemId && i.CurrentBindingId == b.Id && i.SupersededByItemId == null && i.CrewId == assignedToUserId && i.Mode == b.Mode &&
                            context.RepairObligations.Any(o => o.Id == i.ObligationId && o.CurrentRepairItemId == i.Id)) &&
                        (task.TaskMode == "NORMAL" && b.Mode == RepairMode.Normal || task.TaskMode == "CONDITIONAL_FT" && b.Mode == RepairMode.FastTrack)).Select(b => b.PolicyRevisionId).FirstOrDefault()
                    };
        if (filter.Status is not null)
        {
            var taskStatus = Enum.Parse<FieldInspectionTaskStatus>(filter.Status, true);
            query = query.Where(x => !rejected && x.task.Status == taskStatus);
        }
        if (filter.StatusGroup == "COMPLETED") query = query.Where(x => !rejected && x.task.Status == FieldInspectionTaskStatus.Completed);
        if (filter.StatusGroup == "INCOMPLETE") query = query.Where(x => !rejected && (x.task.Status == FieldInspectionTaskStatus.NewAssigned || x.task.Status == FieldInspectionTaskStatus.Accepted || x.task.Status == FieldInspectionTaskStatus.InProgress || x.task.Status == FieldInspectionTaskStatus.SupplementRequired || x.task.Status == FieldInspectionTaskStatus.Submitted));
        if (filter.StatusGroup == "OTHER") query = query.Where(x => rejected || x.task.Status != FieldInspectionTaskStatus.NewAssigned && x.task.Status != FieldInspectionTaskStatus.Accepted && x.task.Status != FieldInspectionTaskStatus.InProgress && x.task.Status != FieldInspectionTaskStatus.SupplementRequired && x.task.Status != FieldInspectionTaskStatus.Submitted && x.task.Status != FieldInspectionTaskStatus.Completed);
        if (filter.Mode is not null) query = query.Where(x => x.Mode == filter.Mode);
        if (filter.Acceptance == "PENDING") query = query.Where(x => x.acceptedAt == null && x.task.Status == FieldInspectionTaskStatus.NewAssigned);
        if (filter.Acceptance == "ACCEPTED") query = query.Where(x => x.acceptedAt != null);
        if (filter.Search is not null) query = query.Where(x => EF.Functions.Collate(x.task.TaskCode, "Latin1_General_100_CI_AS").Contains(filter.Search));
        if (afterDueAt.HasValue && afterId.HasValue) query = query.Where(x => x.task.DueAt > afterDueAt.Value || x.task.DueAt == afterDueAt.Value && x.assignment.Id.CompareTo(afterId.Value) > 0);
        var rows = await query.OrderBy(x => x.task.DueAt).ThenBy(x => x.assignment.Id).Take(limit + 1).ToListAsync(cancellationToken);
        var hasMore = rows.Count > limit; if (hasMore) rows.RemoveAt(rows.Count - 1);
        var views = rows.Select(x => new InspectionTaskPersistenceView(x.task.Id, x.task.ProjectId, x.task.DefectId, x.assignment.AssignedToUserId,
            x.task.Status, x.task.RequiredMeasurementType, x.task.TaskCode, x.task.DueAt, x.task.RowVersion, x.Mode, x.PolicyId, x.assignment.Id,
            x.assignment.Status.ToString().ToUpperInvariant(), rejected ? "REJECTED" : x.acceptedAt.HasValue ? "ACCEPTED" : x.task.Status == FieldInspectionTaskStatus.NewAssigned ? "PENDING" : "UNKNOWN",
            x.acceptedAt, rejected ? x.assignment.EndedAt : null, rejected)).ToArray();
        return new(views, rows.LastOrDefault()?.task.DueAt, rows.LastOrDefault()?.assignment.Id, hasMore, DateTimeOffset.UtcNow);
    }
}
