using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Inspections;

namespace RoadGuardSystem.Repositories.Implementations.Inspections;

public sealed partial class FieldInspectionWorkflowRepository
{
    // Called only by the guarded task GET, inside its serializable read transaction.
    // Absence preserves historical FIELD read rights without exposing a stale repair capability.
    private async Task<CrewRepairContextFact?> CrewRepairContextAsync(FieldWorkflowCommand command,
        FieldInspectionTask task, CancellationToken token)
    {
        var mode = task.TaskMode switch
        {
            "NORMAL" => RepairMode.Normal,
            "CONDITIONAL_FT" => RepairMode.FastTrack,
            _ => (RepairMode?)null
        };
        if (command.Admission.CallerRole != UserRoleCode.RepairCrew || task.RepairItemId is null || mode is null)
            return null;
        var actor = command.Admission.CallerId;
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var source = await (from item in db.Set<RepairItem>().AsNoTracking()
                            join binding in db.Set<RepairFieldTaskBinding>().AsNoTracking() on item.CurrentBindingId equals (Guid?)binding.Id
                            join obligation in db.Set<RepairObligation>().AsNoTracking() on item.ObligationId equals obligation.Id
                            join package in db.Set<RepairPackage>().AsNoTracking() on EF.Property<Guid?>(item, "PackageId") equals (Guid?)package.Id
                            join assignment in db.FieldInspectionAssignments.AsNoTracking() on binding.AssignmentId equals assignment.Id
                            where item.Id == task.RepairItemId && item.ProjectId == task.ProjectId && item.DefectId == task.DefectId &&
                                item.Mode == mode && item.SupersededByItemId == null && item.CrewId == actor &&
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
                                AssignmentId = assignment.Id,
                                ObligationId = obligation.Id,
                                binding.AuthorizationId,
                                binding.PolicyRevisionId,
                                binding.PolicyContentHash,
                                binding.LocationVersion,
                                binding.PlanHash,
                                BindingChecklistVersion = binding.ChecklistVersion,
                                item.ProposalPlanHash,
                                item.CurrentAttemptId,
                                item.CurrentIntakeLinkId,
                                item.EffectiveIntakeSubmissionId,
                                item.CurrentReviewId,
                                item.EffectiveDecisionId,
                                item.State,
                                Version = EF.Property<byte[]>(item, "RowVersion"),
                                item.RepairPlan,
                                item.ChecklistVersion
                            }).SingleOrDefaultAsync(token);
        if (source is null) return null;
        if (mode == RepairMode.FastTrack)
        {
            var hasAuthorization = await db.Set<RepairExecutionAuthorization>().AsNoTracking().AnyAsync(row =>
                row.Id == source.AuthorizationId && row.ProjectId == task.ProjectId && row.DefectId == task.DefectId &&
                row.TaskId == task.Id && row.AssignmentId == source.AssignmentId && row.CrewId == actor &&
                row.Permission == RepairTaskMode.ConditionalFastTrack && row.LocationVersion == source.LocationVersion &&
                row.PolicyRevisionId == source.PolicyRevisionId, token);
            if (!hasAuthorization || source.PlanHash != source.ProposalPlanHash ||
                source.BindingChecklistVersion != source.ChecklistVersion) return null;
            var policy = await db.Set<RepairPolicyRevision>().AsNoTracking()
                .Include(row => row.Measurements).Include(row => row.Revocations)
                .SingleOrDefaultAsync(row => row.Id == source.PolicyRevisionId && row.ProjectId == task.ProjectId, token);
            if (policy is null || policy.IsRevoked || policy.Measurements.Count == 0 ||
                policy.ChecklistVersion != source.BindingChecklistVersion ||
                RepairFtEligibility.PolicyHash(policy) != source.PolicyContentHash) return null;
        }

        CrewRepairAttemptFact? attempt = null;
        if (source.CurrentAttemptId is Guid attemptId && source.CurrentIntakeLinkId is Guid linkId &&
            source.EffectiveIntakeSubmissionId is Guid submissionId)
        {
            var intake = await (from link in db.Set<RepairAttemptSubmissionLink>().AsNoTracking()
                                join physical in db.Set<RepairAttempt>().AsNoTracking() on link.AttemptId equals physical.Id
                                join submission in db.Set<FieldInspectionSubmission>().AsNoTracking() on link.SubmissionId equals submission.Id
                                where link.Id == linkId && link.AttemptId == attemptId && link.SubmissionId == submissionId &&
                                    link.ProjectId == task.ProjectId && link.ItemId == source.ItemId && link.BindingId == source.BindingId &&
                                    physical.ProjectId == task.ProjectId && physical.ItemId == source.ItemId &&
                                    physical.ObligationId == source.ObligationId && physical.DefectId == task.DefectId &&
                                    physical.TaskId == task.Id && physical.AssignmentId == source.AssignmentId && physical.CrewId == actor &&
                                    submission.ProjectId == task.ProjectId && submission.TaskId == task.Id &&
                                    submission.AssignmentId == source.AssignmentId && submission.OriginalActorId == actor &&
                                    submission.RootId == link.FormalRootSubmissionId && submission.ContentHash == link.SubmissionContentHash
                                select new { link.SubmissionContentHash }).SingleOrDefaultAsync(token);
            if (intake is not null)
            {
                CrewRepairReviewFact? review = null;
                if (source.CurrentReviewId is Guid reviewId)
                    review = await db.Set<RepairAttemptReview>().AsNoTracking().Where(row =>
                        row.Id == reviewId && row.ProjectId == task.ProjectId && row.ItemId == source.ItemId &&
                        row.BindingId == source.BindingId && row.AttemptId == attemptId && row.IntakeLinkId == linkId &&
                        row.SubmissionId == submissionId && row.SubmissionContentHash == intake.SubmissionContentHash &&
                        row.Role == UserRoleCode.ProjectManager && (row.Decision == "SUPPLEMENT" || row.Decision == "ACCEPT"))
                        .Select(row => new CrewRepairReviewFact(row.Id, row.Decision)).SingleOrDefaultAsync(token);
                attempt = new(attemptId, submissionId, review);
            }
        }
        var decisions = db.Set<RepairDecision>().AsNoTracking().Where(row => row.ItemId == source.ItemId &&
            row.ObligationId == source.ObligationId && row.DefectId == task.DefectId && row.Mode == mode);
        var head = await decisions.Where(row => row.Id == source.EffectiveDecisionId)
            .Select(row => new { row.Id, row.Result, row.At }).SingleOrDefaultAsync(token);
        var requests = await (from request in db.Set<RepairReviewRequest>().AsNoTracking()
                              join decision in decisions on request.DecisionId equals decision.Id
                              where request.ProjectId == task.ProjectId && request.ItemId == source.ItemId &&
                                  request.ActorId == actor && request.Role == UserRoleCode.RepairCrew
                              orderby request.At descending, request.Id descending
                              select new CrewReviewRequestFact(request.Id, request.DecisionId, request.Reason, request.At))
            .ToArrayAsync(token);
        return new(source.PackageId, source.ItemId, source.BindingId, mode == RepairMode.FastTrack ? "FAST_TRACK" : "NORMAL",
            source.State.ToString(), Convert.ToBase64String(source.Version), source.RepairPlan!, source.ChecklistVersion!, attempt,
            head is null ? null : new(head.Id, RepairWorkflowRepository.ResultName(head.Result), head.At), requests);
    }
}
