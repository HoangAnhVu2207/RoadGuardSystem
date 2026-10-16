using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Inspections;

namespace RoadGuardSystem.Repositories.Implementations.Inspections;

public sealed partial class FieldInspectionWorkflowRepository
{
    // Called only by the guarded task GET, inside its serializable read transaction.
    // Absence preserves historical FIELD read rights without exposing a stale repair capability.
    private async Task<CrewRepairContextFact?> CrewRepairContextAsync(FieldWorkflowCommand command,
        FieldInspectionTask task, CancellationToken token)
    {
        if (command.Admission.CallerRole != UserRoleCode.RepairCrew || task.RepairItemId is null || task.TaskMode != "NORMAL")
            return null;
        var actor = command.Admission.CallerId;
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var source = await (from item in db.Set<RepairItem>().AsNoTracking()
                            join binding in db.Set<RepairFieldTaskBinding>().AsNoTracking() on item.CurrentBindingId equals (Guid?)binding.Id
                            join obligation in db.Set<RepairObligation>().AsNoTracking() on item.ObligationId equals obligation.Id
                            join package in db.Set<RepairPackage>().AsNoTracking() on EF.Property<Guid?>(item, "PackageId") equals (Guid?)package.Id
                            join assignment in db.FieldInspectionAssignments.AsNoTracking() on binding.AssignmentId equals assignment.Id
                            where item.Id == task.RepairItemId && item.ProjectId == task.ProjectId && item.DefectId == task.DefectId &&
                                item.Mode == RepairMode.Normal && item.SupersededByItemId == null && item.CrewId == actor &&
                                binding.ItemId == item.Id && binding.ProjectId == task.ProjectId && binding.DefectId == task.DefectId &&
                                binding.ObligationId == obligation.Id && binding.TaskId == task.Id && binding.CrewId == actor && binding.Mode == item.Mode &&
                                obligation.CurrentRepairItemId == item.Id && obligation.ProjectId == task.ProjectId && obligation.DefectId == task.DefectId &&
                                EF.Property<Guid?>(obligation, "PackageId") == package.Id &&
                                package.ProjectId == task.ProjectId && package.DefectId == task.DefectId &&
                                assignment.FieldInspectionTaskId == task.Id && assignment.AssignedToUserId == actor &&
                                assignment.Status == FieldInspectionAssignmentStatus.Active && assignment.EndedAt == null &&
                                db.ProjectMembers.Any(member => member.ProjectId == task.ProjectId && member.UserId == actor &&
                                    member.RoleCode == UserRoleCode.RepairCrew && member.Status == ProjectMemberStatus.Active &&
                                    member.ValidFrom <= today && (member.ValidTo == null || member.ValidTo >= today))
                            select new
                            {
                                PackageId = package.Id,
                                ItemId = item.Id,
                                BindingId = binding.Id,
                                item.State,
                                Version = EF.Property<byte[]>(item, "RowVersion"),
                                item.RepairPlan,
                                item.ChecklistVersion
                            }).SingleOrDefaultAsync(token);
        return source is null ? null : new(source.PackageId, source.ItemId, source.BindingId, "NORMAL",
            source.State.ToString(), Convert.ToBase64String(source.Version), source.RepairPlan!, source.ChecklistVersion!);
    }
}
