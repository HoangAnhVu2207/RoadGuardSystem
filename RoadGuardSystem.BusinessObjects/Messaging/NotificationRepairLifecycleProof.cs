using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;

namespace RoadGuardSystem.BusinessObjects.Messaging;

public sealed record NotificationRepairLifecycleClaim(Guid ProjectId, Guid ItemId, Guid OriginEventId,
    Guid RevisionId, string MessageType, string Kind, DateTimeOffset OccurredAtUtc);
public static class NotificationRepairLifecycleProof
{
    public static bool VerifyAssignment(NotificationRepairLifecycleClaim claim, RepairItem item, RepairItemLifecycleEvent source,
        RepairFieldTaskBinding binding, FieldInspectionTask task, FieldInspectionAssignment assignment, Guid? responsibleUserId)
    {
        ArgumentNullException.ThrowIfNull(claim); ArgumentNullException.ThrowIfNull(item); ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(binding); ArgumentNullException.ThrowIfNull(task); ArgumentNullException.ThrowIfNull(assignment);
        return source.Id != Guid.Empty && claim.ProjectId == item.ProjectId && claim.ItemId == item.Id &&
            claim.OriginEventId == source.Id && claim.RevisionId == source.Id && claim.MessageType == "repair.work.assigned.v1" &&
            claim.Kind == "ASSIGNED" && claim.OccurredAtUtc.Offset == TimeSpan.Zero && claim.OccurredAtUtc == source.At &&
            source.ProjectId == item.ProjectId && source.ItemId == item.Id && source.DefectId == item.DefectId && source.ObligationId == item.ObligationId &&
            source.Mode == item.Mode && source.Kind == "ASSIGNED" && source.Role == UserRoleCode.ProjectManager &&
            source.BindingId == binding.Id && source.AttemptId is null && source.SubmissionId is null && source.ReviewId is null && source.DecisionId is null &&
            binding.ItemId == item.Id && binding.ProjectId == item.ProjectId && binding.DefectId == item.DefectId && binding.ObligationId == item.ObligationId &&
            binding.Mode == item.Mode && binding.TaskId == task.Id && task.RepairItemId == item.Id && task.ProjectId == item.ProjectId && task.DefectId == item.DefectId &&
            binding.AssignmentId == assignment.Id && assignment.FieldInspectionTaskId == task.Id && assignment.AssignedToUserId == binding.CrewId &&
            (responsibleUserId is null || responsibleUserId == binding.CrewId) && source.ActorId == binding.AssignedBy && source.ActorId == assignment.AssignedByUserId &&
            binding.RouteVersionId == task.RoadSectionVersionId && binding.SegmentSetId == task.SegmentSetId &&
            binding.LayoutRevisionId == task.LayoutRevisionId && binding.MapPublicationId == task.MapPublicationId &&
            binding.CrsProfileRevisionId == task.CrsProfileRevisionId && binding.SlabId == task.SlabId &&
            binding.PlanHash == item.ProposalPlanHash && binding.ChecklistVersion == item.ChecklistVersion &&
            source.At == binding.AssignedAt && source.At == assignment.AssignedAt && !string.IsNullOrWhiteSpace(source.SourceVersion);
    }
    public static bool VerifyProposal(NotificationRepairLifecycleClaim claim, RepairItem item, RepairItemLifecycleEvent source)
    {
        ArgumentNullException.ThrowIfNull(claim); ArgumentNullException.ThrowIfNull(item); ArgumentNullException.ThrowIfNull(source);
        return claim.ProjectId == item.ProjectId && claim.ItemId == item.Id && source.Id != Guid.Empty &&
            claim.OriginEventId == source.Id && claim.RevisionId == source.Id &&
            claim.MessageType == "review.supervisor_required.v1" && claim.Kind == "SUPERVISOR_REQUIRED" &&
            claim.OccurredAtUtc.Offset == TimeSpan.Zero && claim.OccurredAtUtc == source.At &&
            source.ProjectId == item.ProjectId && source.ItemId == item.Id && source.DefectId == item.DefectId &&
            source.ObligationId == item.ObligationId && source.Mode == item.Mode && item.Mode == RepairMode.Normal &&
            source.Kind == "PROPOSED" && source.Role == UserRoleCode.ProjectManager && source.ActorId == item.ProposedBy &&
            source.At == item.ProposedAt && source.BindingId is null && source.AttemptId is null && source.SubmissionId is null &&
            source.ReviewId is null && source.DecisionId is null && !string.IsNullOrWhiteSpace(source.SourceVersion);
    }
}
