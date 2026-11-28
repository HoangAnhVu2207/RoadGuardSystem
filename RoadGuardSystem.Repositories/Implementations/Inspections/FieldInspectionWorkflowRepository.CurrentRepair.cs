using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;
using RoadGuardSystem.Repositories.Implementations.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Inspections;

public sealed partial class FieldInspectionWorkflowRepository
{
    // Called only inside the guarded FIELD read transaction. Intake readiness/hash is never changed.
    private async Task<FieldSubmissionCurrentRepairFact?> SubmissionCurrentRepairAsync(FieldInspectionSubmission submission, CancellationToken token)
    {
        var itemId = await db.FieldInspectionTasks.AsNoTracking().Where(x => x.Id == submission.TaskId && x.ProjectId == submission.ProjectId)
            .Select(x => x.RepairItemId).SingleAsync(token);
        if (itemId is null) return null;
        var item = await db.Set<RepairItem>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == itemId && x.ProjectId == submission.ProjectId, token);
        if (item is null) Deny(409, "field_submission_snapshot_inconsistent");
        var version = await db.Set<RepairItem>().Where(x => x.Id == item.Id).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync(token);
        var currentItem = item.SupersededByItemId is null && await db.Set<RepairObligation>().AsNoTracking().AnyAsync(x => x.Id == item.ObligationId && x.ProjectId == submission.ProjectId && x.DefectId == item.DefectId && x.CurrentRepairItemId == item.Id, token);
        var binding = !currentItem ? null : await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == item.CurrentBindingId &&
            x.ItemId == item.Id && x.ProjectId == submission.ProjectId && x.ObligationId == item.ObligationId &&
            x.TaskId == submission.TaskId && x.AssignmentId == submission.AssignmentId, token);
        var link = binding is null ? null : await db.Set<RepairAttemptSubmissionLink>().AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == item.CurrentIntakeLinkId && x.ItemId == item.Id && x.ProjectId == submission.ProjectId &&
            x.BindingId == binding.Id && x.AttemptId == item.CurrentAttemptId && x.SubmissionId == submission.Id &&
            x.SubmissionId == item.EffectiveIntakeSubmissionId && x.SubmissionContentHash == submission.ContentHash, token);
        var review = link is null ? null : await db.Set<RepairAttemptReview>().AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == item.CurrentReviewId && x.ProjectId == submission.ProjectId && x.ItemId == item.Id &&
            x.BindingId == link.BindingId && x.AttemptId == link.AttemptId && x.IntakeLinkId == link.Id &&
            x.SubmissionId == submission.Id && x.SubmissionContentHash == submission.ContentHash, token);
        var decision = link is null ? null : await db.Set<RepairDecision>().AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == item.EffectiveDecisionId && x.ItemId == item.Id && x.ObligationId == item.ObligationId && x.DefectId == item.DefectId, token);
        if (decision?.Accepted == true && (review?.Decision != "ACCEPT" || item.State != RepairItemState.Confirmed)) decision = null;
        var missing = new List<string>();
        if (!currentItem) missing.Add("REPAIR_ITEM_NOT_CURRENT");
        if (binding is null) missing.Add("CURRENT_BINDING_UNAVAILABLE");
        if (link is null) missing.Add("NOT_CURRENT_EFFECTIVE_SUBMISSION");
        else if (review is null) missing.Add("CURRENT_REVIEW_MISSING");
        else if (review.Decision != "ACCEPT") missing.Add("CURRENT_REVIEW_NOT_ACCEPTED");
        if (decision is null || !decision.Accepted) missing.Add("FINAL_CONFIRMATION_NOT_EFFECTIVE");
        return new(item.Id, Convert.ToBase64String(version), binding?.Id, link?.AttemptId,
            item.EffectiveIntakeSubmissionId, link is not null, review?.Id, review?.Decision,
            review?.Decision == "SUPPLEMENT" ? review.Reason : null, item.State.ToString(),
            decision?.Id, decision is null ? null : RepairWorkflowRepository.ResultName(decision.Result), missing.ToArray());
    }
}
