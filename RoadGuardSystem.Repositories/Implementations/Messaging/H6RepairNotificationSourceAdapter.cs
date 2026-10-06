using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Clocks;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed class H6RepairNotificationSourceAdapter(RoadGuardDbContext db) : IH6NotificationSourceAdapter
{
    public bool Supports(string sourceKind) => sourceKind == "RepairWork";
    public IQueryable<H6SourceScope> ScopeQuery()
        => db.RepairItems.Select(item => new H6SourceScope
        {
            SourceKind = "RepairWork",
            SourceId = item.Id,
            ProjectId = item.ProjectId,
            AssignedUserId = (from binding in db.Set<RepairFieldTaskBinding>()
                              join assignment in db.FieldInspectionAssignments on binding.AssignmentId equals assignment.Id
                              join task in db.FieldInspectionTasks on binding.TaskId equals task.Id
                              where item.CurrentBindingId == binding.Id && item.SupersededByItemId == null &&
                                  binding.ItemId == item.Id && binding.ProjectId == item.ProjectId &&
                                  task.RepairItemId == item.Id && task.ProjectId == item.ProjectId &&
                                  assignment.FieldInspectionTaskId == task.Id && assignment.AssignedToUserId == binding.CrewId &&
                                  assignment.Status == FieldInspectionAssignmentStatus.Active && assignment.EndedAt == null &&
                                  db.RepairObligations.Any(obligation => obligation.Id == item.ObligationId && obligation.CurrentRepairItemId == item.Id)
                              select (Guid?)assignment.AssignedToUserId).FirstOrDefault()
        });
    public async Task<H6SourceResolution> ResolveAsync(H6DispatchPlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Source proof requires the caller's transaction.");
        if (!Supports(plan.Source.SourceKind)) return new("REJECTED", "notification_source_kind_invalid");
        if (plan.MessageType == "repair.work.submitted.v1")
        {
            var transport = await db.OutboxMessages.FromSqlInterpolated($"SELECT * FROM [OutboxMessages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={plan.Source.EventId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (transport is null || !H6FieldNotificationSourceAdapter.MatchesTransport(transport, plan))
                return new("REJECTED", "notification_transport_relation_invalid");
            var item = await db.RepairItems.FromSqlInterpolated($"SELECT * FROM [RepairItems] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={plan.Source.SourceId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            var source = await db.Set<RepairItemLifecycleEvent>().FromSqlInterpolated($"SELECT * FROM [RepairItemLifecycleEvents] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={plan.Source.OriginEventId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (item is null || source is null || source.AttemptId is not Guid attemptId ||
                source.SubmissionId is not Guid submissionId || source.BindingId is not Guid bindingId)
                return new("REJECTED", "notification_source_relation_invalid");
            var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == bindingId, cancellationToken);
            var attempt = await db.Set<RepairAttempt>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == attemptId, cancellationToken);
            var link = await db.Set<RepairAttemptSubmissionLink>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.AttemptId == attemptId && row.SubmissionId == submissionId && row.PreviousLinkId == null,
                cancellationToken);
            var submission = await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == submissionId, cancellationToken);
            var reviewClock = link is null ? null : await db.Set<DeadlineClock>().AsNoTracking()
                .SingleOrDefaultAsync(row => row.Id == link.ReviewClockId, cancellationToken);
            return binding is not null && attempt is not null && link is not null && submission is not null &&
                reviewClock is not null && plan.Source.SourceRevisionId == source.Id &&
                plan.Source.EventId == source.Id && plan.Source.Kind == "SUBMITTED" &&
                plan.Source.ProjectId == item.ProjectId && plan.Source.OccurredAtUtc == source.At &&
                source.Kind == "SUBMITTED" && source.ProjectId == item.ProjectId &&
                source.ItemId == item.Id && source.DefectId == item.DefectId &&
                source.ObligationId == item.ObligationId && source.Mode == item.Mode &&
                source.Role == UserRoleCode.RepairCrew && source.ActorId == attempt.CrewId &&
                binding.Id == link.BindingId && binding.ItemId == item.Id && binding.ProjectId == item.ProjectId &&
                binding.TaskId == attempt.TaskId && binding.AssignmentId == attempt.AssignmentId &&
                attempt.ItemId == item.Id && attempt.ProjectId == item.ProjectId &&
                attempt.ObligationId == item.ObligationId && attempt.DefectId == item.DefectId &&
                attempt.OriginId == submission.OriginId && attempt.PayloadHash == submission.ContentHash &&
                attempt.ServerReceivedAt == submission.ServerReceivedAt &&
                link.ProjectId == item.ProjectId && link.ItemId == item.Id &&
                link.FormalRootSubmissionId == submission.Id && link.SubmissionContentHash == submission.ContentHash &&
                link.FormalRootServerReceivedAt == submission.ServerReceivedAt &&
                submission.ProjectId == item.ProjectId && submission.TaskId == binding.TaskId &&
                submission.AssignmentId == binding.AssignmentId && submission.OriginalActorId == attempt.CrewId &&
                submission.RootId == submission.Id && submission.ParentId is null && submission.Revision == 1 &&
                reviewClock.Kind == DeadlineClockKind.ProjectManagerReview && reviewClock.ProjectId == item.ProjectId &&
                reviewClock.TargetId == binding.TaskId && reviewClock.OriginEventId == submission.Id &&
                reviewClock.OriginAt == submission.ServerReceivedAt &&
                reviewClock.OriginalDueAt == link.OriginalReviewDueAt
                ? new("VERIFIED", TaskId: binding.TaskId, AssignmentId: binding.AssignmentId,
                    BindingId: binding.Id)
                : new("REJECTED", "notification_source_relation_invalid");
        }
        if (plan.MessageType == "repair.work.assigned.v1")
        {
            var transport = await db.OutboxMessages.FromSqlInterpolated($"SELECT * FROM [OutboxMessages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={plan.Source.EventId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (transport is null || !H6FieldNotificationSourceAdapter.MatchesTransport(transport, plan))
                return new("REJECTED", "notification_transport_relation_invalid");
            var assignedItem = await db.RepairItems.FromSqlInterpolated($"SELECT * FROM [RepairItems] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={plan.Source.SourceId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            var assignedEvent = await db.Set<RepairItemLifecycleEvent>().FromSqlInterpolated($"SELECT * FROM [RepairItemLifecycleEvents] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={plan.Source.OriginEventId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (assignedItem is null || assignedEvent?.BindingId is not Guid bindingId || plan.Source.SourceRevisionId is not Guid revision)
                return new("REJECTED", "notification_source_relation_invalid");
            var binding = await db.Set<RepairFieldTaskBinding>().FromSqlInterpolated($"SELECT * FROM [RepairFieldTaskBindings] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={bindingId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (binding is null) return new("REJECTED", "notification_source_relation_invalid");
            var task = await db.FieldInspectionTasks.FromSqlInterpolated($"SELECT * FROM [FieldInspectionTasks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={binding.TaskId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            var assignment = await db.FieldInspectionAssignments.FromSqlInterpolated($"SELECT * FROM [FieldInspectionAssignments] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={binding.AssignmentId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            return task is not null && assignment is not null && NotificationRepairLifecycleProof.VerifyAssignment(
                new(plan.Source.ProjectId, plan.Source.SourceId, plan.Source.OriginEventId, revision, plan.MessageType,
                    plan.Source.Kind, plan.Source.OccurredAtUtc), assignedItem, assignedEvent, binding, task, assignment, plan.Source.ResponsibleUserId)
                ? new("VERIFIED", TaskId: task.Id, AssignmentId: assignment.Id, ResponsibleUserId: binding.CrewId, BindingId: binding.Id)
                : new("REJECTED", "notification_source_relation_invalid");
        }
        if (plan.MessageType == "repair.work.rework_requested.v1")
        {
            var transport = await db.OutboxMessages.FromSqlInterpolated($"SELECT * FROM [OutboxMessages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={plan.Source.EventId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (transport is null || !H6FieldNotificationSourceAdapter.MatchesTransport(transport, plan))
                return new("REJECTED", "notification_transport_relation_invalid");
            var item = await db.RepairItems.FromSqlInterpolated($"SELECT * FROM [RepairItems] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={plan.Source.SourceId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            var source = await db.Set<RepairItemLifecycleEvent>().FromSqlInterpolated($"SELECT * FROM [RepairItemLifecycleEvents] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={plan.Source.OriginEventId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (item is null || source?.ReviewId is not Guid reviewId || source.BindingId is not Guid bindingId)
                return new("REJECTED", "notification_source_relation_invalid");
            var review = await db.Set<RepairAttemptReview>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == reviewId && row.ProjectId == item.ProjectId && row.ItemId == item.Id,
                cancellationToken);
            var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == bindingId && row.ProjectId == item.ProjectId && row.ItemId == item.Id,
                cancellationToken);
            var link = review is null ? null : await db.Set<RepairAttemptSubmissionLink>().AsNoTracking()
                .SingleOrDefaultAsync(row => row.Id == review.IntakeLinkId && row.ItemId == item.Id,
                    cancellationToken);
            var task = binding is null ? null : await db.FieldInspectionTasks.AsNoTracking()
                .SingleOrDefaultAsync(row => row.Id == binding.TaskId && row.ProjectId == item.ProjectId,
                    cancellationToken);
            var assignment = binding is null ? null : await db.FieldInspectionAssignments.AsNoTracking()
                .SingleOrDefaultAsync(row => row.Id == binding.AssignmentId, cancellationToken);
            var fieldReview = review is not null && binding is not null && await db.Set<FieldInspectionReview>()
                .AsNoTracking().AnyAsync(row => row.ProjectId == item.ProjectId && row.TaskId == binding.TaskId &&
                    row.SubmissionId == review.SubmissionId && row.ActorId == review.ActorId &&
                    row.Decision == "SUPPLEMENT" && row.OccurredAt == review.At, cancellationToken);
            var currentObligation = await db.RepairObligations.AsNoTracking().AnyAsync(row =>
                row.Id == item.ObligationId && row.ProjectId == item.ProjectId &&
                row.CurrentRepairItemId == item.Id && row.EffectiveResolutionDecisionId == null, cancellationToken);
            return review is not null && binding is not null && link is not null && task is not null &&
                assignment is not null && fieldReview && currentObligation &&
                plan.Source.EventId == source.Id && plan.Source.SourceRevisionId == source.Id &&
                plan.Source.Kind == "REWORK" && plan.Source.ProjectId == item.ProjectId &&
                plan.Source.OccurredAtUtc == source.At && source.Kind == "REWORK" &&
                source.ProjectId == item.ProjectId && source.ItemId == item.Id &&
                source.DefectId == item.DefectId && source.ObligationId == item.ObligationId &&
                source.Mode == item.Mode && source.Role == UserRoleCode.ProjectManager &&
                source.ActorId == review.ActorId && source.At == review.At &&
                source.AttemptId == review.AttemptId && source.SubmissionId == review.SubmissionId &&
                review.BindingId == binding.Id && review.Decision == "SUPPLEMENT" &&
                review.AttemptId == link.AttemptId && review.SubmissionId == link.SubmissionId &&
                link.BindingId == binding.Id && item.CurrentBindingId == binding.Id &&
                item.CurrentIntakeLinkId == link.Id && item.CurrentReviewId == review.Id &&
                item.EffectiveIntakeSubmissionId == review.SubmissionId &&
                item.State == RepairItemState.Submitted && item.SupersededByItemId is null &&
                task.RepairItemId == item.Id && task.Status == FieldInspectionTaskStatus.SupplementRequired &&
                assignment.FieldInspectionTaskId == task.Id && assignment.AssignedToUserId == binding.CrewId &&
                assignment.Status == FieldInspectionAssignmentStatus.Active && assignment.EndedAt is null
                ? new("VERIFIED", TaskId: task.Id, AssignmentId: assignment.Id,
                    ResponsibleUserId: binding.CrewId, BindingId: binding.Id)
                : new("REJECTED", "notification_source_relation_invalid");
        }
        if (plan.MessageType == "review.supervisor_required.v1")
        {
            var transport = await db.OutboxMessages.FromSqlInterpolated($"SELECT * FROM [OutboxMessages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={plan.Source.EventId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (transport is null || !H6FieldNotificationSourceAdapter.MatchesTransport(transport, plan))
                return new("REJECTED", "notification_transport_relation_invalid");
            var proposedItem = await db.RepairItems.FromSqlInterpolated($"SELECT * FROM [RepairItems] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={plan.Source.SourceId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            var proposal = await db.Set<RepairItemLifecycleEvent>().FromSqlInterpolated($"SELECT * FROM [RepairItemLifecycleEvents] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={plan.Source.OriginEventId}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (proposedItem is null || proposal is null || plan.Source.SourceRevisionId is not Guid revision)
                return new("REJECTED", "notification_source_relation_invalid");
            if (proposal.Kind == "PM_REVIEWED")
            {
                var review = proposal.ReviewId is Guid reviewId ? await db.Set<RepairAttemptReview>().AsNoTracking()
                    .SingleOrDefaultAsync(row => row.Id == reviewId, cancellationToken) : null;
                var binding = proposal.BindingId is Guid bindingId ? await db.Set<RepairFieldTaskBinding>()
                    .AsNoTracking().SingleOrDefaultAsync(row => row.Id == bindingId, cancellationToken) : null;
                var link = review is null ? null : await db.Set<RepairAttemptSubmissionLink>().AsNoTracking()
                    .SingleOrDefaultAsync(row => row.Id == review.IntakeLinkId, cancellationToken);
                var finalClock = review is null ? null : await db.Set<DeadlineClock>().AsNoTracking()
                    .SingleOrDefaultAsync(row => row.TargetId == proposedItem.Id &&
                        row.Kind == DeadlineClockKind.SupervisorFinalConfirmation && row.OriginEventId == review.Id,
                        cancellationToken);
                var fieldReview = review is not null && binding is not null && await db.Set<FieldInspectionReview>()
                    .AsNoTracking().AnyAsync(row => row.ProjectId == proposedItem.ProjectId &&
                        row.TaskId == binding.TaskId && row.SubmissionId == review.SubmissionId &&
                        row.ActorId == review.ActorId && row.Decision == "CONFIRM" && row.OccurredAt == review.At,
                        cancellationToken);
                return review is not null && binding is not null && link is not null && finalClock is not null &&
                    fieldReview && plan.Source.EventId == proposal.Id && plan.Source.OriginEventId == proposal.Id &&
                    revision == proposal.Id && plan.Source.Kind == "SUPERVISOR_REQUIRED" &&
                    plan.Source.ProjectId == proposedItem.ProjectId && plan.Source.OccurredAtUtc == proposal.At &&
                    proposedItem.Mode == RepairMode.Normal && proposedItem.State == RepairItemState.Reviewed &&
                    proposedItem.CurrentReviewId == review.Id && proposedItem.CurrentIntakeLinkId == link.Id &&
                    proposedItem.EffectiveIntakeSubmissionId == review.SubmissionId &&
                    proposedItem.CurrentBindingId == binding.Id &&
                    proposal.ProjectId == proposedItem.ProjectId && proposal.ItemId == proposedItem.Id &&
                    proposal.ObligationId == proposedItem.ObligationId && proposal.DefectId == proposedItem.DefectId &&
                    proposal.Mode == proposedItem.Mode && proposal.ActorId == review.ActorId &&
                    proposal.Role == UserRoleCode.ProjectManager && proposal.At == review.At &&
                    proposal.AttemptId == review.AttemptId && proposal.SubmissionId == review.SubmissionId &&
                    review.ProjectId == proposedItem.ProjectId && review.ItemId == proposedItem.Id &&
                    review.BindingId == binding.Id && review.Decision == "ACCEPT" && review.EvidenceSufficient &&
                    review.ExecutionAuthority == RepairFactState.Confirmed &&
                    binding.ProjectId == proposedItem.ProjectId && binding.ItemId == proposedItem.Id &&
                    link.ItemId == proposedItem.Id && link.BindingId == binding.Id &&
                    link.AttemptId == review.AttemptId && link.SubmissionId == review.SubmissionId &&
                    finalClock.ProjectId == proposedItem.ProjectId && finalClock.OriginAt == review.At &&
                    finalClock.CompletedAt is null
                    ? new("VERIFIED", ResponsibleIsSupervisor: true)
                    : new("REJECTED", "notification_source_relation_invalid");
            }
            if (proposal.Kind != "PROPOSED") return new("PENDING_PRODUCER", "notification_repair_producer_not_adopted");
            return NotificationRepairLifecycleProof.VerifyProposal(new(plan.Source.ProjectId, plan.Source.SourceId,
                plan.Source.OriginEventId, revision, plan.MessageType, plan.Source.Kind, plan.Source.OccurredAtUtc), proposedItem, proposal)
                ? new("VERIFIED", ResponsibleIsSupervisor: true) : new("REJECTED", "notification_source_relation_invalid");
        }
        // Other repair producers bind their immutable lifecycle/assignment revisions at H4 freeze.
        // An item row alone cannot prove one of those events or appoint a Crew recipient.
        if (plan.MessageType != "repair.decision.corrected.v1" || !plan.AuditOnly || plan.AuditFacts is null)
            return new("PENDING_PRODUCER", "notification_repair_producer_not_adopted");
        var correctionSource = plan.Source; var facts = plan.AuditFacts;
        var correctionItem = await db.RepairItems.FromSqlInterpolated($"SELECT * FROM [RepairItems] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={correctionSource.SourceId}")
            .Include(row => row.Decisions).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (correctionItem is null) return new("REJECTED", "notification_source_not_found");
        var obligation = await db.RepairObligations.FromSqlInterpolated($"SELECT * FROM [RepairObligations] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={correctionItem.ObligationId}")
            .Include(row => row.ResolutionHistory).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var decision = correctionItem.Decisions.SingleOrDefault(row => row.Id == correctionSource.SourceRevisionId);
        if (obligation is null || decision is null || correctionSource.EventId != correctionSource.OriginEventId ||
            correctionSource.EventId != correctionSource.SourceRevisionId)
            return new("REJECTED", "notification_source_relation_invalid");
        var result = facts.Result switch
        {
            "UNREPAIRED" => RepairPresentationState.Unrepaired,
            "REPORTED_AWAITING_REVIEW" => RepairPresentationState.ReportedAwaitingReview,
            "CONFIRMED" => RepairPresentationState.Confirmed,
            _ => (RepairPresentationState)0
        };
        var claim = new NotificationRepairCorrectionClaim(correctionSource.EventId, correctionSource.ProjectId,
            correctionSource.SourceId, facts.ObligationId, facts.SupersedesDecisionId, result,
            correctionSource.OccurredAtUtc);
        return NotificationRepairCorrectionProof.Verify(claim, correctionItem, obligation, decision)
            ? new("VERIFIED") : new("REJECTED", "notification_source_relation_invalid");
    }
}
