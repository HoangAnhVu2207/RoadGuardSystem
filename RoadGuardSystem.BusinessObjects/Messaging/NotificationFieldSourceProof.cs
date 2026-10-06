using RoadGuardSystem.BusinessObjects.Inspections;

namespace RoadGuardSystem.BusinessObjects.Messaging;

// These are persisted-source relationship checks, not recipient permissions or offline claims.
// The caller must separately prove EventId against the persisted transport envelope; OriginEventId
// pins the immutable business event across duplicate transport admissions.
public sealed record NotificationFieldSourceClaim(Guid EventId, Guid OriginEventId, Guid ProjectId,
    Guid TaskId, string Action, DateTimeOffset OccurredAtUtc, Guid? RevisionId, Guid? ResponsibleUserId);
public static class NotificationFieldSourceProof
{
    public static bool Verify(NotificationFieldSourceClaim claim, FieldInspectionTask task,
        FieldInspectionTaskEvent sourceEvent, FieldInspectionAssignment? assignment,
        FieldInspectionSubmission? submission = null, FieldInspectionReview? review = null)
    {
        ArgumentNullException.ThrowIfNull(claim); ArgumentNullException.ThrowIfNull(task); ArgumentNullException.ThrowIfNull(sourceEvent);
        if (task.LifecycleVersion != 2 || claim.EventId == Guid.Empty ||
            claim.OriginEventId != sourceEvent.Id || claim.ProjectId != task.ProjectId || sourceEvent.ProjectId != task.ProjectId ||
            claim.TaskId != task.Id || sourceEvent.TaskId != task.Id || claim.Action != sourceEvent.Kind ||
            claim.OccurredAtUtc != sourceEvent.OccurredAt || claim.OccurredAtUtc.Offset != TimeSpan.Zero ||
            claim.RevisionId == Guid.Empty || claim.ResponsibleUserId == Guid.Empty)
            return false;
        if (sourceEvent.AssignmentId is Guid assignmentId &&
            (assignment is null || assignment.Id != assignmentId || assignment.FieldInspectionTaskId != task.Id)) return false;
        if (sourceEvent.AssignmentId is null && assignment is not null) return false;
        if (claim.Action is "ASSIGNED" or "REASSIGNED" or "SUPPLEMENT")
        {
            if (assignment is null || claim.ResponsibleUserId != assignment.AssignedToUserId) return false;
        }
        else if (claim.ResponsibleUserId is not null) return false;
        if (claim.Action is "ASSIGNED" or "REASSIGNED")
            return claim.RevisionId is null && assignment is not null && assignment.AssignedAt == sourceEvent.OccurredAt;
        if (claim.Action is "SUBMITTED" or "SUPPLEMENT" or "REVIEWED")
        {
            if (submission is null || claim.RevisionId != submission.Id || submission.TaskId != task.Id || submission.ProjectId != task.ProjectId)
                return false;
            if (claim.Action == "SUBMITTED")
                return submission.AssignmentId == sourceEvent.AssignmentId && submission.ServerReceivedAt == sourceEvent.OccurredAt;
            // Supplement responsibility follows the exact assignment at review. It need not be
            // the original submitter after a real handover; the intake lineage stays unchanged.
            return review is not null && review.ProjectId == task.ProjectId && review.TaskId == task.Id &&
                review.SubmissionId == submission.Id && review.ActorId == sourceEvent.ActorId && review.OccurredAt == sourceEvent.OccurredAt &&
                (claim.Action == "SUPPLEMENT" ? review.Decision == "SUPPLEMENT" : review.Decision is "CONFIRM" or "NO_DEFECT");
        }
        return claim.RevisionId is null && claim.Action is "ACCEPTED" or "REJECTED" or "STARTED" or "CANCELLED" or "IMPACT_CONTINUE" or "IMPACT_VERIFY";
    }
}
