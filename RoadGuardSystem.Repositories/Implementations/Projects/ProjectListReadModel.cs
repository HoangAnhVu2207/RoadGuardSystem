using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Projects;

public sealed class ProjectListReadModel(RoadGuardDbContext db) : IProjectListRepository
{
    public async Task<IReadOnlyList<ProjectListPersistenceItem>> ListAsync(
        Guid actorUserId, UserRoleCode actorRole, DateOnly effectiveOn,
        string? search, ProjectStatus? status, DateTimeOffset? afterCreatedAt,
        Guid? afterId, int limit, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || limit is < 1 or > 101 ||
            actorRole is not (UserRoleCode.Supervisor or UserRoleCode.ProjectManager or UserRoleCode.DroneOperator or UserRoleCode.RepairCrew))
            throw new ArgumentException("Invalid project list scope.");

        var projects = db.Projects.AsNoTracking();
        if (actorRole != UserRoleCode.Supervisor)
        {
            projects = projects.Where(project => db.ProjectMembers.Any(member =>
                member.ProjectId == project.Id && member.UserId == actorUserId && member.RoleCode == actorRole &&
                member.Status == ProjectMemberStatus.Active && member.ValidFrom <= effectiveOn &&
                (member.ValidTo == null || member.ValidTo >= effectiveOn)));
        }
        if (actorRole == UserRoleCode.DroneOperator)
        {
            projects = projects.Where(project =>
                (from task in db.SurveyRequests
                 join assignment in db.SurveyAssignments on task.Id equals assignment.SurveyRequestId
                 where task.ProjectId == project.Id && assignment.OperatorUserId == actorUserId &&
                     assignment.EndedAt == null && assignment.RejectedAt == null &&
                     (task.Status == SurveyRequestStatus.NewAssigned || task.Status == SurveyRequestStatus.Reassigned ||
                      task.Status == SurveyRequestStatus.Accepted || task.Status == SurveyRequestStatus.InProgress ||
                      task.Status == SurveyRequestStatus.Submitted || task.Status == SurveyRequestStatus.SupplementRequired ||
                      task.Status == SurveyRequestStatus.Completed || task.Status == SurveyRequestStatus.Postponed)
                 select task.Id).Any());
        }
        if (actorRole == UserRoleCode.RepairCrew)
        {
            projects = projects.Where(project =>
                (from task in db.FieldInspectionTasks
                 join assignment in db.FieldInspectionAssignments on task.Id equals assignment.FieldInspectionTaskId
                 where task.ProjectId == project.Id && assignment.AssignedToUserId == actorUserId &&
                     assignment.Status == FieldInspectionAssignmentStatus.Active && assignment.EndedAt == null &&
                     (task.Status == FieldInspectionTaskStatus.NewAssigned || task.Status == FieldInspectionTaskStatus.Accepted ||
                      task.Status == FieldInspectionTaskStatus.InProgress || task.Status == FieldInspectionTaskStatus.Submitted ||
                      task.Status == FieldInspectionTaskStatus.SupplementRequired || task.Status == FieldInspectionTaskStatus.Completed) &&
                     (task.RepairItemId == null ||
                      (from item in db.RepairItems
                       join binding in db.Set<RepairFieldTaskBinding>() on item.CurrentBindingId equals (Guid?)binding.Id
                       where item.Id == task.RepairItemId && item.ProjectId == project.Id && item.CrewId == actorUserId &&
                           item.State != RepairItemState.Cancelled && item.SupersededByItemId == null &&
                           binding.ItemId == item.Id && binding.ProjectId == project.Id && binding.DefectId == task.DefectId &&
                           binding.Mode == item.Mode && binding.TaskId == task.Id &&
                           binding.AssignmentId == assignment.Id && binding.CrewId == actorUserId
                       select item.Id).Any())
                 select task.Id).Any());
        }
        if (search is not null)
            projects = projects.Where(project => project.ProjectCode.Contains(search) || project.Name.Contains(search));
        if (status is not null)
            projects = projects.Where(project => project.Status == status.Value);
        if (afterCreatedAt is not null && afterId is not null)
            projects = projects.Where(project => project.CreatedAt > afterCreatedAt.Value ||
                project.CreatedAt == afterCreatedAt.Value && project.Id.CompareTo(afterId.Value) > 0);

        var primaryManagers =
            from member in db.ProjectMembers.AsNoTracking()
            join user in db.Users.AsNoTracking() on member.UserId equals user.Id
            where member.IsPrimary && member.RoleCode == UserRoleCode.ProjectManager &&
                member.Status == ProjectMemberStatus.Active && member.ValidFrom <= effectiveOn &&
                (member.ValidTo == null || member.ValidTo >= effectiveOn) &&
                user.RoleCode == UserRoleCode.ProjectManager && user.Status == UserStatus.Active
            select new { member.ProjectId, MembershipId = member.Id, member.ValidFrom, UserId = user.Id, user.DisplayName };

        return await projects.OrderBy(project => project.CreatedAt).ThenBy(project => project.Id).Take(limit)
            .Select(project => new ProjectListPersistenceItem(
                project.Id, project.ProjectCode, project.Name, project.Status,
                primaryManagers.Where(pm => pm.ProjectId == project.Id).OrderByDescending(pm => pm.ValidFrom).ThenBy(pm => pm.MembershipId)
                    .Select(pm => (Guid?)pm.UserId).FirstOrDefault(),
                primaryManagers.Where(pm => pm.ProjectId == project.Id).OrderByDescending(pm => pm.ValidFrom).ThenBy(pm => pm.MembershipId)
                    .Select(pm => pm.DisplayName).FirstOrDefault(),
                project.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
