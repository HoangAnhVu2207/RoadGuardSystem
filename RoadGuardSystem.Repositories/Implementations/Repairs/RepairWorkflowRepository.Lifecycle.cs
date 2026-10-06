using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed partial class RepairWorkflowRepository : IRepairLifecycleRepository
{
    private const string CancelItemOperation = "h4.repair.cancel.v1";
    private const string ContinueNormalOperation = "h4.repair.continue-normal.v1";

    public Task<RepairWorkflowResult> CancelItemAsync(RepairCancellationCommand command, CancellationToken token)
        => LifecycleMutation(command.ActorId, command.Role, command.ProjectId, command.PackageId, command.ItemId,
            command.Key, CancelItemOperation, command, async ct =>
            {
                var package = await LockedPackage(command.PackageId, ct);
                await FreshAnchor(command.ProjectId, package.DefectId, ct);
                var item = package.Items.Single(row => row.Id == command.ItemId);
                EnsureLifecycleHead(package, item, command.ExpectedVersion);
                var now = clock.GetUtcNow();
                var retired = await CancelAndRetire(item, command.ActorId, command.Input.Reason,
                    command.Input.Handover, command.ExpectedVersion, now, ct);
                Touch(package); await db.SaveChangesAsync(ct);
                var version = VersionOf(item);
                return (retired.CancelId, new RepairLifecycleFact(item.Id, null, item.ObligationId,
                    retired.CancelId, retired.HandoverId, null, item.State.ToString(), null, version, null));
            }, token);

    public Task<RepairWorkflowResult> ContinueNormallyAsync(RepairNormalContinuationCommand command, CancellationToken token)
        => LifecycleMutation(command.ActorId, command.Role, command.ProjectId, command.PackageId, command.ItemId,
            command.Key, ContinueNormalOperation, command, async ct =>
            {
                var package = await LockedPackage(command.PackageId, ct);
                await FreshAnchor(command.ProjectId, package.DefectId, ct);
                var source = package.Items.Single(row => row.Id == command.ItemId);
                EnsureLifecycleHead(package, source, command.ExpectedVersion);
                var obligation = package.Obligations.Single(row => row.Id == source.ObligationId);
                if (obligation.Kind != RepairObligationKind.FormalRepair || obligation.IsResolved)
                    Deny(409, "normal_continuation_source_conflict");
                var now = clock.GetUtcNow(); Guid? cancelId = null, handoverId = null, decisionId = null;
                if (source.State == RepairItemState.CorrectionRequired)
                {
                    if (command.Input.Handover is not null || source.EffectiveDecisionId is null ||
                        obligation.EffectiveResolutionHeadDecisionId != source.EffectiveDecisionId)
                        Deny(409, "normal_continuation_source_conflict");
                    await db.Entry(source).Collection(row => row.Decisions).LoadAsync(ct);
                    if (source.EffectiveDecision?.Accepted != false) Deny(409, "normal_continuation_source_conflict");
                    decisionId = source.EffectiveDecisionId;
                    await RetireCorrectedBinding(source, command.ActorId, command.Input.Reason, now, ct);
                }
                else if (source.State == RepairItemState.Cancelled)
                {
                    if (command.Input.Handover is not null || source.CurrentBindingId is not null)
                        Deny(409, "normal_continuation_source_conflict");
                    var cancellation = await db.Set<RepairItemLifecycleEvent>().AsNoTracking().SingleOrDefaultAsync(row =>
                        row.ItemId == source.Id && row.ProjectId == command.ProjectId && row.Kind == "CANCELLED", ct);
                    if (cancellation is null) Deny(409, "cancellation_source_required");
                    cancelId = cancellation.Id;
                    if (cancellation.BindingId is not null)
                    {
                        var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleAsync(row => row.Id == cancellation.BindingId, ct);
                        handoverId = await db.Set<FieldInspectionTaskEvent>().AsNoTracking().Where(row => row.TaskId == binding.TaskId &&
                            row.AssignmentId == binding.AssignmentId && row.Kind == "CANCELLED" && row.OccurredAt == cancellation.At)
                            .Select(row => (Guid?)row.Id).SingleOrDefaultAsync(ct);
                        if (handoverId is null) Deny(409, "handover_source_required");
                    }
                }
                else
                {
                    var retired = await CancelAndRetire(source, command.ActorId, command.Input.Reason,
                        command.Input.Handover, command.ExpectedVersion, now, ct);
                    cancelId = retired.CancelId; handoverId = retired.HandoverId;
                }
                // All original first-start, attempt, submission, decision and clock identities remain attached to the predecessor.
                RepairItem successor;
                try { successor = package.ContinueNormally(source.Id, Guid.NewGuid(), command.ActorId, now,
                    new(command.Input.RepairPlan, command.Input.ChecklistVersion)); }
                catch (InvalidOperationException) { Deny(409, "normal_continuation_source_conflict"); throw; }
                Touch(package); await db.SaveChangesAsync(ct);
                db.Entry(obligation).Property(row => row.CurrentRepairItemId).CurrentValue = successor.Id;
                var continuation = new RepairNormalSuccessor(Guid.NewGuid(), command.ProjectId, obligation.Id,
                    source.Id, successor.Id, decisionId, command.ActorId, command.Input.Reason, now,
                    source.CurrentAssessmentId, cancelId, handoverId);
                db.Add(continuation);
                var proposed = Lifecycle(successor, command.ActorId, command.Role, "PROPOSED", command.Input.Reason,
                    command.ExpectedVersion, now);
                db.Add(DeadlineClock.Create(Guid.NewGuid(), command.ProjectId, DeadlineClockKind.SupervisorInitialApproval,
                    successor.Id, proposed.Id, now));
                Notify(proposed, "review.supervisor_required.v1", "SUPERVISOR_REQUIRED");
                AuditProducer(command.ActorId, "repair_normal_continuation", "RepairItem", successor.Id, command.Input.Reason,
                    new { continuation.Id, sourceItemId = source.Id, obligationId = obligation.Id, decisionId, cancelId, handoverId,
                        successor.ProposalPlanHash, obligation.OriginalCrewFirstStartId });
                await db.SaveChangesAsync(ct);
                return (continuation.Id, new RepairLifecycleFact(source.Id, successor.Id, obligation.Id,
                    cancelId, handoverId, continuation.Id, source.State.ToString(), successor.State.ToString(),
                    VersionOf(source), VersionOf(successor)));
            }, token);

    private void EnsureLifecycleHead(RepairPackage package, RepairItem item, string expected)
    {
        if (VersionOf(item) != expected) Deny(409, "concurrency_conflict");
        if (item.SupersededByItemId is not null ||
            package.Obligations.Single(row => row.Id == item.ObligationId).CurrentRepairItemId != item.Id)
            Deny(409, "repair_item_not_current");
    }

    private async Task<(Guid CancelId, Guid? HandoverId)> CancelAndRetire(RepairItem item, Guid actor, string reason,
        RepairLifecycleHandoverData? handover, string version, DateTimeOffset now, CancellationToken token)
    {
        if (item.State is RepairItemState.Submitted or RepairItemState.Reviewed or RepairItemState.Confirmed or
            RepairItemState.Cancelled or RepairItemState.CorrectionRequired || item.CurrentExecutionFinishId is not null)
            Deny(409, "submitted_or_finished_history_requires_intake_review");
        var bindingId = item.CurrentBindingId; Guid? nativeEventId = null; RepairWorkHandover? performed = null;
        if (bindingId is not null)
        {
            var (binding, task, assignment) = await LifecycleBinding(item, true, token);
            if (await db.Set<FieldInspectionSubmission>().AnyAsync(row => row.TaskId == task.Id, token))
                Deny(409, "submitted_or_finished_history_requires_intake_review");
            var first = await db.Set<FieldTaskStartOrigin>().AsNoTracking().SingleOrDefaultAsync(row => row.TaskId == task.Id, token);
            if (first is not null)
            {
                // Receipt of unfinished work is limited to the already-authorized acting PM or the actual assigned Crew.
                // A new Crew appointment is made only by the existing normal assignment producer after approval.
                if (handover is null || handover.FirstStartId != first.Id || first.AssignmentId != assignment.Id ||
                    first.OriginalActorId != binding.CrewId ||
                    handover.RecipientUserId != actor && handover.RecipientUserId != binding.CrewId)
                    Deny(400, "handover_facts_required");
                if (handover.RecipientUserId == binding.CrewId)
                    await CurrentProducerAuthority(binding.CrewId, UserRoleCode.RepairCrew, UserRoleCode.RepairCrew, item.ProjectId, token);
                performed = new(Guid.NewGuid(), binding.CrewId, handover.RecipientUserId,
                    handover.PerformedPortion, handover.SafetyState, now);
            }
            else if (handover is not null || item.State == RepairItemState.InProgress) Deny(409, "handover_start_source_conflict");
            if (task.Status is FieldInspectionTaskStatus.Completed or FieldInspectionTaskStatus.Cancelled or
                FieldInspectionTaskStatus.Submitted or FieldInspectionTaskStatus.SupplementRequired)
                Deny(409, "submitted_or_finished_history_requires_intake_review");
            task.Transition(FieldInspectionTaskStatus.Cancelled);
            if (assignment.EndedAt is null) assignment.End(now, reason);
            var nativeEvent = FieldInspectionTaskEvent.Create(Guid.NewGuid(), item.ProjectId, task.Id, assignment.Id,
                actor, "CANCELLED", reason, now, JsonSerializer.Serialize(new
                {
                    repairItemId = item.Id, bindingId = binding.Id, firstStartId = first?.Id,
                    firstStartOriginId = first?.OriginId, firstStartContentHash = first?.ContentHash,
                    performedPortion = performed?.PerformedScope, safetyState = performed?.SafetyState,
                    recipientUserId = performed?.ToActorId, executionStartId = item.CurrentExecutionStartId,
                    task.RoadSectionVersionId, task.SegmentSetId, task.LayoutRevisionId, task.SlabId
                }, Json));
            db.Add(nativeEvent); nativeEventId = nativeEvent.Id;
        }
        else if (handover is not null || item.State is RepairItemState.Assigned or RepairItemState.InProgress)
            Deny(409, "repair_binding_source_required");
        item.Cancel(actor, UserRoleCode.ProjectManager, reason, now, performed);
        db.Entry(item).Property(row => row.CurrentBindingId).CurrentValue = null;
        var cancellation = Lifecycle(item, actor, UserRoleCode.ProjectManager, "CANCELLED", reason, version, now, bindingId);
        AuditProducer(actor, "repair_item_cancelled", "RepairItem", item.Id, reason,
            new { cancellation.Id, bindingId, nativeEventId, item.ObligationId, performed });
        // Unanswered approval ceases to be actionable; its origin and original due date remain intact.
        var approval = await db.Set<DeadlineClock>().SingleOrDefaultAsync(row => row.TargetId == item.Id &&
            row.Kind == DeadlineClockKind.SupervisorInitialApproval, token);
        if (approval is { CompletedAt: null }) approval.Complete(now);
        await db.SaveChangesAsync(token);
        return (cancellation.Id, nativeEventId);
    }

    private async Task RetireCorrectedBinding(RepairItem item, Guid actor, string reason, DateTimeOffset now, CancellationToken token)
    {
        if (item.CurrentBindingId is null) Deny(409, "repair_binding_source_required");
        var (_, task, assignment) = await LifecycleBinding(item, false, token);
        // Completed native intake remains completed; immutable submission/review rows are never changed.
        if (task.Status != FieldInspectionTaskStatus.Completed) Deny(409, "correction_intake_source_conflict");
        assignment.End(now, reason);
        db.Entry(item).Property(row => row.CurrentBindingId).CurrentValue = null;
        db.Add(FieldInspectionTaskEvent.Create(Guid.NewGuid(), item.ProjectId, task.Id, assignment.Id, actor,
            "REASSIGNED", reason, now, JsonSerializer.Serialize(new { repairItemId = item.Id,
                decisionId = item.EffectiveDecisionId, purpose = "NORMAL_CONTINUATION", preservesCompletedIntake = true }, Json)));
    }

    private async Task<(RepairFieldTaskBinding Binding, FieldInspectionTask Task, FieldInspectionAssignment Assignment)>
        LifecycleBinding(RepairItem item, bool allowRejected, CancellationToken token)
    {
        var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleAsync(row => row.Id == item.CurrentBindingId, token);
        var task = await db.FieldInspectionTasks.FromSqlInterpolated(
            $"SELECT * FROM [FieldInspectionTasks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={binding.TaskId}").SingleAsync(token);
        var assignment = await db.FieldInspectionAssignments.FromSqlInterpolated(
            $"SELECT * FROM [FieldInspectionAssignments] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={binding.AssignmentId}").SingleAsync(token);
        var active = assignment.Status == FieldInspectionAssignmentStatus.Active && assignment.EndedAt is null;
        var rejectedBeforeStart = allowRejected && item.State == RepairItemState.Assigned &&
            item.CurrentExecutionStartId is null && item.CurrentExecutionFinishId is null && item.CurrentAssessmentId is null &&
            task.Status == FieldInspectionTaskStatus.Rejected && assignment.Status == FieldInspectionAssignmentStatus.Rejected &&
            assignment.EndedAt is not null &&
            !await db.Set<FieldTaskStartOrigin>().AnyAsync(row => row.TaskId == task.Id, token) &&
            await db.Set<FieldInspectionTaskEvent>().AnyAsync(row => row.ProjectId == item.ProjectId && row.TaskId == task.Id &&
                row.AssignmentId == assignment.Id && row.ActorId == binding.CrewId && row.Kind == "REJECTED" &&
                row.OccurredAt == assignment.EndedAt, token);
        if (binding.ItemId != item.Id || binding.ProjectId != item.ProjectId || binding.ObligationId != item.ObligationId ||
            task.RepairItemId != item.Id || task.ProjectId != item.ProjectId || task.TaskMode == "MEASURE_ONLY" ||
            task.TaskMode != (item.Mode == RepairMode.Normal ? "NORMAL" : "CONDITIONAL_FT") ||
            assignment.FieldInspectionTaskId != task.Id || assignment.AssignedToUserId != binding.CrewId ||
            item.CrewId != binding.CrewId || !active && !rejectedBeforeStart)
            Deny(409, "repair_binding_source_conflict");
        return (binding, task, assignment);
    }

    private async Task<RepairWorkflowResult> LifecycleMutation(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, string key, string operation, object command,
        Func<CancellationToken, Task<(Guid Id, RepairLifecycleFact Fact)>> apply, CancellationToken token)
    {
        try
        {
            async Task Guard(CancellationToken ct)
            {
                await CurrentProducerAuthority(actor, role, UserRoleCode.ProjectManager, project, ct);
                await GuardItemResource(project, package, item, ct);
                var stored = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(row => row.ActorUserId == actor &&
                    row.ProjectId == project && row.Operation == operation && row.IdempotencyKey == key, ct);
                if (stored is null) return;
                Guid? sourceItem = operation == CancelItemOperation
                    ? await db.Set<RepairItemLifecycleEvent>().Where(row => row.Id == stored.OperationId &&
                        row.ProjectId == project && row.Kind == "CANCELLED").Select(row => (Guid?)row.ItemId).SingleOrDefaultAsync(ct)
                    : await db.Set<RepairNormalSuccessor>().Where(row => row.Id == stored.OperationId && row.ProjectId == project)
                        .Select(row => (Guid?)row.SourceItemId).SingleOrDefaultAsync(ct);
                if (sourceItem is null) Deny(403, "stored_receipt_access_forbidden");
                var sourcePackage = await db.Set<RepairItem>().Where(row => row.Id == sourceItem && row.ProjectId == project)
                    .Select(row => EF.Property<Guid?>(row, "PackageId")).SingleOrDefaultAsync(ct);
                if (sourcePackage is null) Deny(403, "stored_receipt_access_forbidden");
                await GuardItemResource(project, sourcePackage.Value, sourceItem.Value, ct);
            }
            var result = await receipts.ExecuteSerializableAsync(actor, project, operation, key,
                SourceHash(new { schemaVersion = 1, command }), async ct =>
                {
                    await Guard(ct);
                    if (await db.IdempotencyRecords.AsNoTracking().AnyAsync(row => row.ActorUserId == actor &&
                        row.ProjectId == project && row.Operation == operation && row.IdempotencyKey == key, ct)) throw new ExistingReceipt();
                    var effect = await apply(ct); await Guard(ct);
                    return (effect.Id, JsonSerializer.Serialize(effect.Fact, Json));
                }, token, receiptAccessGuard: Guard);
            if (result.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
            var fact = JsonSerializer.Deserialize<RepairLifecycleFact>(result.OutcomeJson, Json)
                ?? throw new InvalidOperationException("Durable lifecycle outcome missing.");
            return new(result.Status == IdempotencyOperationStatus.Replayed ? 200 : 201, Value: fact,
                Version: fact.SourceVersion, Replayed: result.Status == IdempotencyOperationStatus.Replayed);
        }
        catch (ExistingReceipt) { db.ChangeTracker.Clear(); return await LifecycleMutation(actor, role, project, package, item, key, operation, command, apply, token); }
        catch (Denied denial) { db.ChangeTracker.Clear(); return denial.Result; }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(409, "concurrency_conflict"); }
        catch (ArgumentException) { db.ChangeTracker.Clear(); return new(400, "validation_error"); }
    }
}
