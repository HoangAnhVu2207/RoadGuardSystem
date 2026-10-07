using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using System.Text.Json;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed class H6WeeklyReviewSourceAdapter(RoadGuardDbContext db, TimeProvider clock) : IH6NotificationSourceAdapter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public bool Supports(string sourceKind) => sourceKind is "ReviewObligation" or "ReviewDigest";

    public IQueryable<H6SourceScope> ScopeQuery()
        => db.Set<DeadlineClock>().Where(row => row.Kind == DeadlineClockKind.ProjectManagerReview ||
            row.Kind == DeadlineClockKind.SupervisorInitialApproval || row.Kind == DeadlineClockKind.SupervisorFinalConfirmation)
            .Select(row => new H6SourceScope
            {
                SourceKind = "ReviewObligation",
                SourceId = row.Id,
                ProjectId = row.ProjectId,
                AssignedUserId = null
            }).Union(db.Set<WeeklyReviewDigest>().Select(d => new H6SourceScope
            { SourceKind = "ReviewDigest", SourceId = d.Id, ProjectId = d.ProjectId, AssignedUserId = d.RecipientId }));

    public async Task<H6SourceResolution> ResolveAsync(H6DispatchPlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Source proof requires the caller's transaction.");
        var source = plan.Source;
        if (source.SourceKind == "ReviewDigest") return await ResolveDigestAsync(plan, cancellationToken);
        if (plan.MessageType != "review.weekly_pending.v1" || source.Kind != "WEEKLY_PENDING" ||
            !Supports(source.SourceKind) || source.SourceRevisionId != source.EventId ||
            source.ResponsibleUserId is not null || source.ScheduledAtUtc is not DateTimeOffset scheduled)
            return new("REJECTED", "notification_source_relation_invalid");
        var transport = await db.OutboxMessages.FromSqlInterpolated($"SELECT * FROM [OutboxMessages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={source.EventId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (transport is null || !H6FieldNotificationSourceAdapter.MatchesTransport(transport, plan))
            return new("REJECTED", "notification_transport_relation_invalid");
        var period = await db.Set<H6NotificationCalendarRow>().FromSqlInterpolated($"SELECT * FROM [H6NotificationCalendar] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={source.EventId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var sourceClock = await db.Set<DeadlineClock>().FromSqlInterpolated($"SELECT * FROM [DeadlineClocks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={source.SourceId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (period is null || sourceClock is null || period.Status != "COMMITTED" ||
            period.OutboxMessageId != source.EventId || period.ClockId != sourceClock.Id ||
            period.ScheduledAtUtc != scheduled || period.ObservedAtUtc != source.OccurredAtUtc ||
            period.PlannedAtUtc >= period.ScheduledAtUtc ||
            sourceClock.Kind is not (DeadlineClockKind.ProjectManagerReview or DeadlineClockKind.SupervisorInitialApproval or
                DeadlineClockKind.SupervisorFinalConfirmation) || sourceClock.CompletedAt is not null ||
            sourceClock.ProjectId != source.ProjectId || sourceClock.OriginEventId != source.OriginEventId)
            return new("REJECTED", "notification_source_relation_invalid");
        if (sourceClock.Kind != DeadlineClockKind.ProjectManagerReview)
        {
            var lifecycle = sourceClock.Kind == DeadlineClockKind.SupervisorInitialApproval
                ? await db.Set<RepairItemLifecycleEvent>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == sourceClock.OriginEventId &&
                    row.ItemId == sourceClock.TargetId && row.ProjectId == source.ProjectId && row.Kind == "PROPOSED", cancellationToken)
                : await db.Set<RepairItemLifecycleEvent>().AsNoTracking().SingleOrDefaultAsync(row => row.ReviewId == sourceClock.OriginEventId &&
                    row.ItemId == sourceClock.TargetId && row.ProjectId == source.ProjectId && row.Kind == "PM_REVIEWED", cancellationToken);
            if (lifecycle is null) return new("REJECTED", "notification_source_relation_invalid");
            var original = await db.OutboxMessages.FromSqlInterpolated($"SELECT * FROM [OutboxMessages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={lifecycle.Id}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (original is null || original.MessageType != "review.supervisor_required.v1")
                return new("REJECTED", "notification_source_relation_invalid");
            H6StoredEvent? originalSource;
            try { originalSource = JsonSerializer.Deserialize<H6StoredEvent>(original.PayloadJson, Json); }
            catch (JsonException) { return new("REJECTED", "notification_source_relation_invalid"); }
            if (originalSource is null || originalSource.EventId != lifecycle.Id || originalSource.SourceId != sourceClock.TargetId ||
                originalSource.ProjectId != source.ProjectId || originalSource.OriginEventId != lifecycle.Id ||
                originalSource.OccurredAtUtc != lifecycle.At || originalSource.Kind != "SUPERVISOR_REQUIRED" ||
                originalSource.SourceKind != "RepairWork" || originalSource.SourceRevisionId != lifecycle.Id)
                return new("REJECTED", "notification_source_relation_invalid");
            var originalEnvelope = NotificationEventEnvelope.Create(lifecycle.Id, NotificationEventKind.SupervisorApprovalRequired,
                source.ProjectId, NotificationSourceKind.RepairWork, sourceClock.TargetId, lifecycle.Id, lifecycle.At,
                lifecycle.Id);
            var originalPlan = new H6DispatchPlan(originalSource, originalEnvelope, original.MessageType, "", "", false);
            var proof = await new H6RepairNotificationSourceAdapter(db).ResolveAsync(originalPlan, cancellationToken);
            return proof.Status == "VERIFIED" && sourceClock.OriginAt == lifecycle.At
                ? await new H6DeadlineNotificationSourceAdapter(db, clock).ResolveDutyAsync(sourceClock, cancellationToken)
                : new("REJECTED", "notification_source_relation_invalid");
        }
        var root = await db.FieldInspectionSubmissions.AsNoTracking().SingleOrDefaultAsync(row => row.Id == sourceClock.OriginEventId,
            cancellationToken);
        var task = await db.FieldInspectionTasks.AsNoTracking().SingleOrDefaultAsync(row => row.Id == sourceClock.TargetId,
            cancellationToken);
        if (root is null || task is null || root.RootId != root.Id || root.ParentId is not null || root.Revision != 1 ||
            root.TaskId != task.Id || root.ProjectId != source.ProjectId || task.ProjectId != source.ProjectId ||
            root.ServerReceivedAt != sourceClock.OriginAt)
            return new("REJECTED", "notification_source_relation_invalid");
        return await new H6DeadlineNotificationSourceAdapter(db, clock).ResolveDutyAsync(sourceClock, cancellationToken);
    }

    private async Task<H6SourceResolution> ResolveDigestAsync(H6DispatchPlan plan, CancellationToken token)
    {
        var source = plan.Source;
        var digest = await db.Set<WeeklyReviewDigest>().AsNoTracking().Include(d => d.Duties)
            .SingleOrDefaultAsync(d => d.Id == source.SourceId, token);
        var transport = await db.OutboxMessages.FromSqlInterpolated($"SELECT * FROM [OutboxMessages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={source.EventId}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (digest is null || transport is null || !H6FieldNotificationSourceAdapter.MatchesTransport(transport, plan) ||
            plan.MessageType != "review.weekly_pending.v1" || source.Kind != "WEEKLY_PENDING" ||
            source.EventId != digest.Id || source.OriginEventId != digest.Id || source.SourceRevisionId != digest.Id ||
            source.ProjectId != digest.ProjectId || source.ResponsibleUserId != digest.RecipientId ||
            source.ScheduledAtUtc != digest.ScheduledAtUtc || source.OccurredAtUtc != digest.RecoveredAtUtc || digest.Duties.Count == 0 ||
            !await db.Set<WeeklyReviewRecoveryPeriod>().AnyAsync(p => p.Id == digest.RecoveryPeriodId && p.ProjectId == digest.ProjectId &&
                p.ScheduledAtUtc == digest.ScheduledAtUtc && p.RecoveredAtUtc == digest.RecoveredAtUtc && p.IsLatestAtRecovery, token))
            return new("REJECTED", "notification_source_relation_invalid");
        var responsibilityCurrent = false;
        var currentCandidates = new HashSet<Guid>();
        foreach (var captured in digest.Duties)
        {
            var duty = await db.Set<DeadlineClock>().AsNoTracking().SingleOrDefaultAsync(c => c.Id == captured.ClockId, token);
            if (duty is null || duty.ProjectId != digest.ProjectId || duty.TargetId != captured.TargetId ||
                duty.Kind.ToString() != captured.Kind || duty.OriginEventId != captured.OriginEventId ||
                duty.OriginAt != captured.OriginAtUtc || duty.OriginAt > digest.RecoveredAtUtc ||
                duty.CompletedAt is DateTimeOffset completed && completed <= digest.RecoveredAtUtc)
                return new("REJECTED", "notification_source_relation_invalid");
            var proof = await new H6DeadlineNotificationSourceAdapter(db, clock).ResolveDutyAsync(duty, token);
            if (proof.Status != "VERIFIED") return new("REJECTED", "notification_source_relation_invalid");
            if (proof.ResponsibleUserId is Guid responsible) currentCandidates.Add(responsible);
            if (proof.ResponsibleIsSupervisor)
            {
                var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
                var supervisors = await db.ProjectMembers.Where(m => m.ProjectId == digest.ProjectId &&
                    m.RoleCode == UserRoleCode.Supervisor && m.Status == ProjectMemberStatus.Active &&
                    m.ValidFrom <= day && (m.ValidTo == null || m.ValidTo >= day)).Select(m => m.UserId).Distinct().ToArrayAsync(token);
                foreach (var actor in supervisors) currentCandidates.Add(actor);
            }
            if (proof.Status == "VERIFIED" && (proof.ResponsibleUserId == digest.RecipientId ||
                proof.ResponsibleIsSupervisor && digest.RecipientRole == UserRoleCode.Supervisor)) responsibilityCurrent = true;
        }
        var currentRecipient = digest.RecipientId is null && currentCandidates.Count == 1 ? currentCandidates.Single()
            : responsibilityCurrent ? digest.RecipientId : null;
        return new("VERIFIED", ResponsibleUserId: currentRecipient, ResponsibleRole: digest.RecipientRole,
            BodyOverride: $"{digest.Duties.Count} review duties pending at recovery {digest.RecoveredAtUtc:O}; latest Monday period {digest.ScheduledAtUtc:O}. Digest {digest.Id:D}.");
    }
}
