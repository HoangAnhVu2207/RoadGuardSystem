using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Messaging;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed class H6DeadlineNotificationSourceAdapter(RoadGuardDbContext db, TimeProvider? clock = null) : IH6NotificationSourceAdapter
{
    private readonly TimeProvider time = clock ?? TimeProvider.System;
    public bool Supports(string sourceKind) => sourceKind == "DeadlineClock";
    public IQueryable<H6SourceScope> ScopeQuery()
        => db.Set<DeadlineClock>().Select(clock => new H6SourceScope
        {
            SourceKind = "DeadlineClock",
            SourceId = clock.Id,
            ProjectId = clock.ProjectId,
            AssignedUserId = clock.Kind == DeadlineClockKind.FirstSafetyCheck ||
                clock.Kind == DeadlineClockKind.DangerAcknowledgment
                ? db.Set<TemporarySafetyMeasure>().Where(row => row.Id == clock.TargetId &&
                    row.ProjectId == clock.ProjectId).Select(row => (Guid?)row.ResponsibleActorId).FirstOrDefault()
                : clock.Kind == DeadlineClockKind.DeviceHandover
                    ? db.Set<OfflineHandoverGrant>().Where(row => row.Id == clock.TargetId &&
                        row.ProjectId == clock.ProjectId).Select(row => (Guid?)row.RecipientActorId).FirstOrDefault()
                    : clock.Kind == DeadlineClockKind.FastTrackExecution || clock.Kind == DeadlineClockKind.FinishedDataSync
                        ? db.Set<RepairFieldTaskBinding>().Where(binding =>
                            db.Set<RepairItem>().Any(item => item.Id == clock.TargetId &&
                                item.ProjectId == clock.ProjectId && item.CurrentBindingId == binding.Id &&
                                item.SupersededByItemId == null))
                            .Select(binding => (Guid?)binding.CrewId).FirstOrDefault()
                        : null
        });
    public async Task<H6SourceResolution> ResolveAsync(H6DispatchPlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan); cancellationToken.ThrowIfCancellationRequested();
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Source proof requires the caller's transaction.");
        var source = plan.Source;
        if (!Supports(source.SourceKind) || plan.MessageType != "deadline.breached.v1" || source.Kind != "BREACHED" ||
            source.SourceRevisionId is not Guid revision || source.ResponsibleUserId is not null || source.ScheduledAtUtc is not null)
            return new("REJECTED", "notification_source_relation_invalid");
        var transport = await db.OutboxMessages.FromSqlInterpolated($"SELECT * FROM [OutboxMessages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={source.EventId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (transport is null || !H6FieldNotificationSourceAdapter.MatchesTransport(transport, plan))
            return new("REJECTED", "notification_transport_relation_invalid");
        var sourceClock = await db.Set<DeadlineClock>().FromSqlInterpolated($"SELECT * FROM [DeadlineClocks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={source.SourceId}")
            .Include(row => row.Breaches).Include(row => row.Extensions).AsSplitQuery().SingleOrDefaultAsync(cancellationToken);
        if (sourceClock is null || !NotificationDeadlineSourceProof.Verify(new(source.ProjectId, source.SourceId,
            source.OriginEventId, revision, source.OccurredAtUtc), sourceClock))
            return new("REJECTED", "notification_source_relation_invalid");
        // A registered clock alone supplies no duty. Each kind proves its actual producer chain.
        if (sourceClock.Kind == DeadlineClockKind.SupervisorInitialApproval)
        {
            var item = await db.RepairItems.AsNoTracking().SingleOrDefaultAsync(row => row.Id == sourceClock.TargetId, cancellationToken);
            var proposal = await db.Set<RepairItemLifecycleEvent>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == sourceClock.OriginEventId, cancellationToken);
            if (item is null || proposal is null || !await CurrentRepairHead(item, cancellationToken) ||
                !NotificationRepairLifecycleProof.VerifyProposal(
                new(sourceClock.ProjectId, sourceClock.TargetId, sourceClock.OriginEventId, sourceClock.OriginEventId,
                    "review.supervisor_required.v1", "SUPERVISOR_REQUIRED", sourceClock.OriginAt), item, proposal))
                return new("REJECTED", "notification_source_relation_invalid");
            // This is the confirmed current project Supervisor duty, not an invented single
            // approver. The recipient strategy fans out to the actual current scoped group.
            return new("VERIFIED", ResponsibleIsSupervisor: true);
        }
        if (sourceClock.Kind == DeadlineClockKind.SupervisorFinalConfirmation)
        {
            var item = await db.RepairItems.AsNoTracking().SingleOrDefaultAsync(row => row.Id == sourceClock.TargetId,
                cancellationToken);
            var review = await db.Set<RepairAttemptReview>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == sourceClock.OriginEventId, cancellationToken);
            var lifecycle = await db.Set<RepairItemLifecycleEvent>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.ReviewId == sourceClock.OriginEventId && row.ItemId == sourceClock.TargetId &&
                row.Kind == "PM_REVIEWED", cancellationToken);
            if (item is null || review is null || lifecycle is null || !await CurrentRepairHead(item, cancellationToken) ||
                item.Mode != RepairMode.Normal ||
                item.ProjectId != sourceClock.ProjectId || item.CurrentReviewId != review.Id ||
                review.ProjectId != sourceClock.ProjectId || review.ItemId != item.Id ||
                review.Role != UserRoleCode.ProjectManager || review.Decision != "ACCEPT" ||
                !review.EvidenceSufficient || review.ExecutionAuthority != RepairFactState.Confirmed ||
                lifecycle.ProjectId != sourceClock.ProjectId || lifecycle.Mode != RepairMode.Normal ||
                lifecycle.Role != UserRoleCode.ProjectManager || lifecycle.BindingId != review.BindingId ||
                lifecycle.At != review.At || sourceClock.OriginAt != review.At)
                return new("REJECTED", "notification_source_relation_invalid");
            return new("VERIFIED", ResponsibleIsSupervisor: true);
        }
        if (sourceClock.Kind is DeadlineClockKind.FirstSafetyCheck or DeadlineClockKind.DangerAcknowledgment)
            return await ResolveSafetyClock(sourceClock, cancellationToken);
        if (sourceClock.Kind == DeadlineClockKind.FastTrackExecution)
            return await ResolveFastTrackClock(sourceClock, cancellationToken);
        if (sourceClock.Kind == DeadlineClockKind.FinishedDataSync)
            return await ResolveFinishedSyncClock(sourceClock, cancellationToken);
        if (sourceClock.Kind == DeadlineClockKind.DeviceHandover)
            return await ResolveDeviceHandoverClock(sourceClock, cancellationToken);
        if (sourceClock.Kind != DeadlineClockKind.ProjectManagerReview)
            return new("PENDING_PRODUCER", "notification_source_pending_producer");
        var root = await db.FieldInspectionSubmissions.AsNoTracking().SingleOrDefaultAsync(row => row.Id == sourceClock.OriginEventId,
            cancellationToken);
        var task = await db.FieldInspectionTasks.AsNoTracking().SingleOrDefaultAsync(row => row.Id == sourceClock.TargetId, cancellationToken);
        if (root is null || task is null || root.RootId != root.Id || root.ParentId is not null || root.Revision != 1 ||
            root.TaskId != task.Id || root.ProjectId != sourceClock.ProjectId || task.ProjectId != sourceClock.ProjectId ||
            root.ServerReceivedAt != sourceClock.OriginAt)
            return new("REJECTED", "notification_source_relation_invalid");
        var day = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var managers = await db.ProjectMembers.AsNoTracking().Where(row => row.ProjectId == sourceClock.ProjectId &&
            row.RoleCode == UserRoleCode.ProjectManager && row.IsPrimary && row.Status == ProjectMemberStatus.Active &&
            row.ValidFrom <= day && (!row.ValidTo.HasValue || row.ValidTo >= day)).Select(row => row.UserId).Distinct().Take(2).ToArrayAsync(cancellationToken);
        return new("VERIFIED", TaskId: task.Id, ResponsibleUserId: managers.Length == 1 ? managers[0] : null,
            ResponsibleRole: UserRoleCode.ProjectManager);
    }

    private async Task<H6SourceResolution> ResolveSafetyClock(DeadlineClock sourceClock, CancellationToken token)
    {
        var measure = await db.Set<TemporarySafetyMeasure>().AsNoTracking().SingleOrDefaultAsync(row =>
            row.Id == sourceClock.TargetId && row.ProjectId == sourceClock.ProjectId, token);
        var monitoring = measure is null ? null : await db.Set<RepairSafetyMonitoring>().AsNoTracking()
            .SingleOrDefaultAsync(row => row.MeasureId == measure.Id &&
                row.FormalObligationId == measure.FormalRepairObligationId, token);
        if (measure is null || monitoring is null ||
            !await db.Set<RepairObligation>().AsNoTracking().AnyAsync(row => row.Id == monitoring.SafetyObligationId &&
                row.ProjectId == sourceClock.ProjectId && row.DefectId == measure.DefectId &&
                row.Kind == RepairObligationKind.TemporarySafety && row.EffectiveResolutionDecisionId == null, token))
            return new("REJECTED", "notification_source_relation_invalid");
        var actionKind = sourceClock.Kind == DeadlineClockKind.FirstSafetyCheck ? "INSTALLED" : "WARNING";
        var action = await db.Set<RepairSafetyActionSource>().AsNoTracking().SingleOrDefaultAsync(row =>
            row.MeasureId == measure.Id && row.ProjectId == sourceClock.ProjectId && row.Kind == actionKind &&
            row.OriginId == sourceClock.OriginEventId && row.At == sourceClock.OriginAt, token);
        if (action is null) return new("REJECTED", "notification_source_relation_invalid");
        if (sourceClock.Kind == DeadlineClockKind.FirstSafetyCheck)
        {
            if (measure.InstallationEventId != sourceClock.OriginEventId ||
                measure.InstalledAt != sourceClock.OriginAt || measure.FirstCheckDueAt != sourceClock.OriginalDueAt ||
                action.ActorId != measure.InstalledBy || action.ActorId != measure.ResponsibleActorId)
                return new("REJECTED", "notification_source_relation_invalid");
        }
        else
        {
            var warning = await db.Set<RepairDangerWarning>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == sourceClock.OriginEventId && row.MonitoringId == measure.Id, token);
            var check = warning is null ? null : await db.Set<RepairSafetyCheck>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == warning.SourceId && row.MeasureId == measure.Id, token);
            if (warning is null || check is null || check.Result != RepairSafetyCheckResult.Danger ||
                warning.ServerReceivedAt != sourceClock.OriginAt ||
                warning.OriginalAcknowledgementDueAt != sourceClock.OriginalDueAt ||
                warning.ResponsibleActorId != measure.ResponsibleActorId ||
                action.ActorId != check.ActorId || action.Reason != warning.Reason || check.At != action.At)
                return new("REJECTED", "notification_source_relation_invalid");
        }
        return new("VERIFIED", ResponsibleUserId: measure.ResponsibleActorId,
            ResponsibleRole: UserRoleCode.RepairCrew);
    }

    private async Task<H6SourceResolution> ResolveFastTrackClock(DeadlineClock sourceClock, CancellationToken token)
    {
        var item = await db.Set<RepairItem>().AsNoTracking().SingleOrDefaultAsync(row =>
            row.Id == sourceClock.TargetId && row.ProjectId == sourceClock.ProjectId, token);
        if (item?.CurrentBindingId is not Guid bindingId || item.Mode != RepairMode.FastTrack ||
            item.SupersededByItemId is not null) return new("REJECTED", "notification_source_relation_invalid");
        var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == bindingId,
            token);
        var first = await db.Set<FieldTaskStartOrigin>().AsNoTracking().SingleOrDefaultAsync(row =>
            row.Id == sourceClock.OriginEventId, token);
        var authorization = binding?.AuthorizationId is Guid authorizationId
            ? await db.Set<RepairExecutionAuthorization>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == authorizationId, token) : null;
        if (binding is null || first is null || authorization is null ||
            binding.ProjectId != sourceClock.ProjectId || binding.ItemId != item.Id ||
            binding.Mode != RepairMode.FastTrack || binding.TaskId != first.TaskId ||
            binding.AssignmentId != first.AssignmentId || binding.CrewId != first.OriginalActorId ||
            first.VerifiedOriginalAt != sourceClock.OriginAt || first.TimeProvenance is not ("SERVER_ONLINE" or "VERIFIED_ORIGINAL_SOURCE") ||
            authorization.ProjectId != sourceClock.ProjectId || authorization.TaskId != binding.TaskId ||
            authorization.AssignmentId != binding.AssignmentId || authorization.CrewId != binding.CrewId ||
            authorization.FirstStartOriginId != first.Id || authorization.VerifiedStartedAt != sourceClock.OriginAt ||
            authorization.ExpiresAt != sourceClock.OriginalDueAt ||
            !await db.Set<RepairObligation>().AsNoTracking().AnyAsync(row => row.Id == item.ObligationId &&
                row.CurrentRepairItemId == item.Id && row.OriginalCrewFirstStartId == first.Id, token) ||
            !await db.FieldInspectionAssignments.AsNoTracking().AnyAsync(row => row.Id == binding.AssignmentId &&
                row.FieldInspectionTaskId == binding.TaskId && row.AssignedToUserId == binding.CrewId &&
                row.Status == FieldInspectionAssignmentStatus.Active && row.EndedAt == null, token))
            return new("REJECTED", "notification_source_relation_invalid");
        return new("VERIFIED", TaskId: binding.TaskId, AssignmentId: binding.AssignmentId,
            ResponsibleUserId: binding.CrewId, ResponsibleRole: UserRoleCode.RepairCrew, BindingId: binding.Id);
    }

    private async Task<H6SourceResolution> ResolveFinishedSyncClock(DeadlineClock sourceClock, CancellationToken token)
    {
        var item = await db.Set<RepairItem>().AsNoTracking().SingleOrDefaultAsync(row =>
            row.Id == sourceClock.TargetId && row.ProjectId == sourceClock.ProjectId, token);
        if (item?.CurrentBindingId is not Guid bindingId || item.SupersededByItemId is not null ||
            item.CurrentExecutionFinishId != sourceClock.OriginEventId)
            return new("REJECTED", "notification_source_relation_invalid");
        var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == bindingId,
            token);
        var finish = await db.Set<RepairExecutionFinish>().AsNoTracking().SingleOrDefaultAsync(row =>
            row.Id == sourceClock.OriginEventId, token);
        if (binding is null || finish is null || binding.ProjectId != sourceClock.ProjectId ||
            binding.ItemId != item.Id || binding.CrewId != finish.OriginalActorId ||
            finish.ProjectId != sourceClock.ProjectId || finish.ItemId != item.Id ||
            finish.BindingId != binding.Id || finish.VerifiedOriginalAt != sourceClock.OriginAt ||
            finish.TimeProvenance is not (RepairTimeProvenance.VerifiedOnline or RepairTimeProvenance.VerifiedOffline) ||
            !await db.FieldInspectionAssignments.AsNoTracking().AnyAsync(row => row.Id == binding.AssignmentId &&
                row.FieldInspectionTaskId == binding.TaskId && row.AssignedToUserId == binding.CrewId &&
                row.Status == FieldInspectionAssignmentStatus.Active && row.EndedAt == null, token))
            return new("REJECTED", "notification_source_relation_invalid");
        return new("VERIFIED", TaskId: binding.TaskId, AssignmentId: binding.AssignmentId,
            ResponsibleUserId: binding.CrewId, ResponsibleRole: UserRoleCode.RepairCrew, BindingId: binding.Id);
    }

    private async Task<H6SourceResolution> ResolveDeviceHandoverClock(DeadlineClock sourceClock, CancellationToken token)
    {
        var grant = await db.Set<OfflineHandoverGrant>().AsNoTracking().SingleOrDefaultAsync(row =>
            row.Id == sourceClock.TargetId && row.ProjectId == sourceClock.ProjectId, token);
        if (grant is null || grant.Id != sourceClock.OriginEventId || grant.IssuedAt != sourceClock.OriginAt ||
            grant.ExpiresAt != sourceClock.OriginalDueAt || grant.RecipientRole is not (UserRoleCode.ProjectManager or
                UserRoleCode.RepairCrew) ||
            await db.Set<OfflineHandoverGrantRevocation>().AsNoTracking().AnyAsync(row =>
                row.GrantId == grant.Id && row.ProjectId == sourceClock.ProjectId, token))
            return new("REJECTED", "notification_source_relation_invalid");
        return new("VERIFIED", ResponsibleUserId: grant.RecipientActorId, ResponsibleRole: grant.RecipientRole);
    }

    private async Task<bool> CurrentRepairHead(RepairItem item, CancellationToken token)
        => item.SupersededByItemId is null && await db.Set<RepairObligation>().AsNoTracking()
            .AnyAsync(row => row.Id == item.ObligationId && row.ProjectId == item.ProjectId &&
                row.CurrentRepairItemId == item.Id, token);
}
