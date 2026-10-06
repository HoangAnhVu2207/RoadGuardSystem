using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed class H6SafetyNotificationSourceAdapter(RoadGuardDbContext db) : IH6NotificationSourceAdapter
{
    public bool Supports(string sourceKind) => sourceKind == "TemporarySafetyMeasure";

    public IQueryable<H6SourceScope> ScopeQuery()
        => db.Set<TemporarySafetyMeasure>().Select(row => new H6SourceScope
        { SourceKind = "TemporarySafetyMeasure", SourceId = row.Id, ProjectId = row.ProjectId,
            AssignedUserId = row.ResponsibleActorId });

    public async Task<H6SourceResolution> ResolveAsync(H6DispatchPlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Source proof requires the caller's transaction.");
        var claim = plan.Source;
        if (!Supports(claim.SourceKind) || claim.SourceRevisionId != claim.EventId ||
            claim.ScheduledAtUtc is not null) return new("REJECTED", "notification_source_relation_invalid");
        var transport = await db.OutboxMessages.FromSqlInterpolated($"SELECT * FROM [OutboxMessages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={claim.EventId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (transport is null || !H6FieldNotificationSourceAdapter.MatchesTransport(transport, plan))
            return new("REJECTED", "notification_transport_relation_invalid");
        var action = await db.Set<RepairSafetyActionSource>().FromSqlInterpolated($"SELECT * FROM [RepairSafetyActionSources] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={claim.EventId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var measure = await db.Set<TemporarySafetyMeasure>().AsNoTracking().SingleOrDefaultAsync(row =>
            row.Id == claim.SourceId && row.ProjectId == claim.ProjectId, cancellationToken);
        if (action is null || measure is null || action.MeasureId != measure.Id || action.ProjectId != claim.ProjectId ||
            action.OriginId != claim.OriginEventId || action.At != claim.OccurredAtUtc ||
            !await db.Set<RepairSafetyMonitoring>().AsNoTracking().AnyAsync(row => row.MeasureId == measure.Id &&
                row.FormalObligationId == measure.FormalRepairObligationId &&
                db.Set<RepairObligation>().Any(obligation => obligation.Id == row.SafetyObligationId &&
                    obligation.ProjectId == claim.ProjectId && obligation.DefectId == measure.DefectId &&
                    obligation.Kind == RepairObligationKind.TemporarySafety &&
                    obligation.EffectiveResolutionDecisionId == null), cancellationToken))
            return new("REJECTED", "notification_source_relation_invalid");
        var item = await db.Set<RepairItem>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == action.ItemId,
            cancellationToken);
        var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == action.BindingId,
            cancellationToken);
        if (item is null || binding is null || item.ProjectId != claim.ProjectId || item.DefectId != measure.DefectId ||
            item.ObligationId != measure.FormalRepairObligationId || item.Mode != RepairMode.Normal ||
            item.ApprovedBy is null || item.ApprovedPlanHash != item.ProposalPlanHash ||
            binding.ProjectId != claim.ProjectId || binding.ItemId != item.Id || binding.CrewId != measure.ResponsibleActorId ||
            binding.Mode != RepairMode.Normal)
            return new("REJECTED", "notification_source_relation_invalid");
        if (plan.MessageType == "safety.measure_assigned.v1")
        {
            if (action.Kind != "ASSIGNED" || claim.Kind != "SAFETY_ASSIGNED" || action.OriginId != measure.Id ||
                claim.ResponsibleUserId is not null)
                return new("REJECTED", "notification_source_relation_invalid");
            return new("VERIFIED", ResponsibleUserId: measure.ResponsibleActorId,
                ResponsibleRole: UserRoleCode.RepairCrew);
        }
        if (action.ActorId != measure.ResponsibleActorId || claim.ResponsibleUserId != measure.ResponsibleActorId)
            return new("REJECTED", "notification_source_relation_invalid");
        if (plan.MessageType == "safety.inspection_due.v1")
        {
            var clock = await db.Set<DeadlineClock>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.ProjectId == claim.ProjectId && row.TargetId == measure.Id &&
                row.Kind == DeadlineClockKind.FirstSafetyCheck && row.OriginEventId == action.OriginId,
                cancellationToken);
            return action.Kind == "INSTALLED" && claim.Kind == "INSPECTION_DUE" &&
                measure.InstallationEventId == action.OriginId && measure.InstalledAt == action.At &&
                clock?.OriginAt == action.At && clock.OriginalDueAt == measure.FirstCheckDueAt &&
                clock.OriginalDueAt <= action.At && clock.CompletedAt is null
                ? new("VERIFIED", ResponsibleUserId: measure.ResponsibleActorId,
                    ResponsibleRole: UserRoleCode.RepairCrew)
                : new("REJECTED", "notification_source_relation_invalid");
        }
        if (plan.MessageType == "safety.warning.v1")
        {
            var warning = await db.Set<RepairDangerWarning>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == action.OriginId && row.MonitoringId == measure.Id, cancellationToken);
            var check = warning is null ? null : await db.Set<RepairSafetyCheck>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.Id == warning.SourceId && row.MeasureId == measure.Id, cancellationToken);
            var deadline = warning is null ? null : await db.Set<DeadlineClock>().AsNoTracking().SingleOrDefaultAsync(row =>
                row.ProjectId == claim.ProjectId && row.TargetId == measure.Id &&
                row.Kind == DeadlineClockKind.DangerAcknowledgment && row.OriginEventId == warning.Id,
                cancellationToken);
            return action.Kind == "WARNING" && claim.Kind == "WARNING" && warning is not null &&
                warning.ResponsibleActorId == measure.ResponsibleActorId && warning.ServerReceivedAt == action.At &&
                warning.Reason == action.Reason && check?.Result == RepairSafetyCheckResult.Danger &&
                check.At == action.At && deadline?.OriginAt == action.At &&
                deadline.OriginalDueAt == warning.OriginalAcknowledgementDueAt && deadline.CompletedAt is null
                ? new("VERIFIED", ResponsibleUserId: measure.ResponsibleActorId,
                    ResponsibleRole: UserRoleCode.RepairCrew)
                : new("REJECTED", "notification_source_relation_invalid");
        }
        return new("REJECTED", "notification_source_action_mismatch");
    }
}
