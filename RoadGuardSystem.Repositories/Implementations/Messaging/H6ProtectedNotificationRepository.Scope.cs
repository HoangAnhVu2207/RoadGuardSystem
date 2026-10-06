using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed partial class H6ProtectedNotificationRepository
{
    private const string ExactNames = "Latin1_General_100_BIN2";
    // Source joins and current scope are applied in SQL before cursor/order/limit. A sidecar is
    // never an independent permission grant; legacy null/unknown sources do not become non-project.
    private IQueryable<Notification> Visible(Guid actor, UserRoleCode role)
    {
        var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var projects = db.ProjectMembers.Where(member => member.UserId == actor && member.RoleCode == role &&
            member.Status == ProjectMemberStatus.Active && member.ValidFrom <= day && (!member.ValidTo.HasValue || member.ValidTo >= day))
            .Select(member => member.ProjectId);
        var generic = role is UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.DroneOperator;
        var taskIds = db.FieldInspectionTasks.Where(task => projects.Contains(task.ProjectId) &&
            (generic || role == UserRoleCode.RepairCrew && db.FieldInspectionAssignments.Any(assignment =>
                assignment.FieldInspectionTaskId == task.Id && assignment.AssignedToUserId == actor &&
                assignment.Status == FieldInspectionAssignmentStatus.Active && assignment.EndedAt == null))).Select(task => task.Id);
        var owned = db.Notifications.Where(row => row.RecipientUserId == actor);
        var ids = owned.Where(row =>
            generic && EF.Functions.Collate(row.SourceEntityType, ExactNames) == "Project" && projects.Contains(row.SourceEntityId) ||
            (EF.Functions.Collate(row.SourceEntityType, ExactNames) == "FieldTask" || EF.Functions.Collate(row.SourceEntityType, ExactNames) == "FieldInspectionTask") && taskIds.Contains(row.SourceEntityId) ||
            generic && EF.Functions.Collate(row.SourceEntityType, ExactNames) == "Defect" && db.Defects.Any(source => source.Id == row.SourceEntityId && source.ProjectId.HasValue && projects.Contains(source.ProjectId.Value)) ||
            generic && EF.Functions.Collate(row.SourceEntityType, ExactNames) == "SurveyRequest" && db.SurveyRequests.Any(source => source.Id == row.SourceEntityId && projects.Contains(source.ProjectId)) ||
            generic && EF.Functions.Collate(row.SourceEntityType, ExactNames) == "Survey" && db.Surveys.Any(source => source.Id == row.SourceEntityId && projects.Contains(source.ProjectId)) ||
            generic && EF.Functions.Collate(row.SourceEntityType, ExactNames) == "RoadSectionVersion" && db.RoadSectionVersions.Any(source => source.Id == row.SourceEntityId && db.RoadSections.Any(road => road.Id == source.RoadSectionId && projects.Contains(road.ProjectId))) ||
            generic && (EF.Functions.Collate(row.SourceEntityType, ExactNames) == "Case" || EF.Functions.Collate(row.SourceEntityType, ExactNames) == "IncidentCase") && db.IncidentCases.Any(source => source.Id == row.SourceEntityId && source.ProjectId.HasValue && projects.Contains(source.ProjectId.Value)) ||
            role == UserRoleCode.Supervisor && EF.Functions.Collate(row.SourceEntityType, ExactNames) == "PasswordRecoveryRequest" &&
                EF.Functions.Collate(row.EventType, ExactNames) == "password_recovery_requested" && db.PasswordRecoveryRequests.Any(source => source.Id == row.SourceEntityId && source.TargetUserId.HasValue))
            .Select(row => row.Id);
        foreach (var adapter in sources)
        {
            var actual = adapter.ScopeQuery();
            ids = ids.Union(owned.Where(row => actual.Any(source => source.SourceId == row.SourceEntityId &&
                source.SourceKind == EF.Functions.Collate(row.SourceEntityType, ExactNames) && projects.Contains(source.ProjectId) &&
                (generic || role == UserRoleCode.RepairCrew && source.AssignedUserId == actor))).Select(row => row.Id));
        }
        return owned.Where(row => ids.Contains(row.Id));
    }
}
