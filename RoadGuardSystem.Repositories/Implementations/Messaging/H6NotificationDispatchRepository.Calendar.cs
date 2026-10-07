using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed partial class H6NotificationDispatchRepository
{
    public async Task<int> ObserveCalendarAsync(Guid schedulerRunId, NotificationScheduledCallbackWitness? witness,
        CancellationToken cancellationToken)
    {
        if (schedulerRunId == Guid.Empty) throw new ArgumentException("A scheduler run identity is required.", nameof(schedulerRunId));
        if (db.Database.CurrentTransaction is not null) throw new InvalidOperationException("Calendar admission owns its source transaction.");
        var admitted = 0;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear(); admitted = 0; var now = clock.GetUtcNow().ToUniversalTime();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            // Serialize recoverers before reading a project's complete pending-at-recovery aggregate.
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result=sys.sp_getapplock @Resource=N'RoadGuard.WeeklyReviewRecovery', @LockMode='Exclusive',
                    @LockOwner='Transaction', @LockTimeout=30000;
                IF @result<0 THROW 51107, 'Weekly recovery lock unavailable.', 1;
                """, cancellationToken);
            var latest = NotificationCalendarPolicy.NextWeeklyReview(now).AddDays(-7);
            var next = latest.AddDays(7);
            var clocks = await db.Set<DeadlineClock>().FromSqlInterpolated($"""
                SELECT c.* FROM [DeadlineClocks] c WITH (UPDLOCK,HOLDLOCK)
                WHERE c.[Kind] IN ({(byte)DeadlineClockKind.ProjectManagerReview},{(byte)DeadlineClockKind.SupervisorInitialApproval},
                    {(byte)DeadlineClockKind.SupervisorFinalConfirmation}) AND
                    (c.[CompletedAt] IS NULL OR EXISTS(SELECT 1 FROM [H6NotificationCalendar] p
                      WHERE p.[ClockId]=c.[Id] AND p.[ScheduledAtUtc]<=c.[CompletedAt] AND p.[Status] IN ('PLANNED','PENDING_POLICY')
                        AND NOT EXISTS(SELECT 1 FROM [WeeklyReviewRecoveryPeriods] r WHERE r.[ProjectId]=c.[ProjectId]
                          AND r.[ScheduledAtUtc]=p.[ScheduledAtUtc])))
                ORDER BY c.[ProjectId],c.[Id]
                """).ToArrayAsync(cancellationToken);
            foreach (var project in clocks.GroupBy(c => c.ProjectId))
            {
                var clockIds = project.Select(c => c.Id).ToArray();
                var calendars = await db.Set<H6NotificationCalendarRow>().Where(p => clockIds.Contains(p.ClockId))
                    .ToArrayAsync(cancellationToken);
                var first = NotificationCalendarPolicy.NextWeeklyReview(project.Min(c => c.OriginAt));
                if (calendars.Length > 0 && calendars.Min(p => p.ScheduledAtUtc) < first) first = calendars.Min(p => p.ScheduledAtUtc);
                var historyLast = project.Any(c => c.CompletedAt is null) ? latest
                    : NotificationCalendarPolicy.NextWeeklyReview(project.Max(c => c.CompletedAt!.Value)).AddDays(-7);
                if (historyLast > latest) historyLast = latest;
                if (first <= latest && !await db.Set<WeeklyReviewRecoveryPeriod>().AnyAsync(p =>
                    p.ProjectId == project.Key && p.ScheduledAtUtc == latest, cancellationToken))
                {
                    WeeklyReviewRecoveryPeriod? latestHistory = null;
                    for (var period = first; period <= historyLast; period = period.AddDays(7))
                    {
                        if (await db.Set<WeeklyReviewRecoveryPeriod>().AnyAsync(p => p.ProjectId == project.Key &&
                            p.ScheduledAtUtc == period, cancellationToken)) continue;
                        var history = new WeeklyReviewRecoveryPeriod
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = project.Key,
                            ScheduledAtUtc = period,
                            RecoveredAtUtc = now,
                            IsLatestAtRecovery = period == latest
                        };
                        db.Add(history); if (period == latest) latestHistory = history;
                    }
                    var groups = new Dictionary<(Guid? Actor, UserRoleCode Role), List<DeadlineClock>>();
                    foreach (var duty in project.Where(c => c.CompletedAt is null && c.OriginAt <= now))
                    {
                        if (duty.Kind == DeadlineClockKind.ProjectManagerReview && await db.FieldInspectionTasks.AnyAsync(t =>
                            t.Id == duty.TargetId && (t.Status == FieldInspectionTaskStatus.Cancelled || t.Status == FieldInspectionTaskStatus.Completed), cancellationToken)) continue;
                        var proof = await new H6DeadlineNotificationSourceAdapter(db, clock).ResolveDutyAsync(duty, cancellationToken);
                        if (proof.Status != "VERIFIED") continue;
                        var role = duty.Kind == DeadlineClockKind.ProjectManagerReview ? UserRoleCode.ProjectManager : UserRoleCode.Supervisor;
                        Guid?[] recipients;
                        if (proof.ResponsibleIsSupervisor)
                        {
                            var day = DateOnly.FromDateTime(now.UtcDateTime);
                            var supervisors = await db.ProjectMembers.Where(m => m.ProjectId == project.Key && m.RoleCode == role &&
                                m.Status == ProjectMemberStatus.Active && m.ValidFrom <= day && (m.ValidTo == null || m.ValidTo >= day))
                                .Select(m => m.UserId).Distinct().ToArrayAsync(cancellationToken);
                            recipients = supervisors.Length == 0 ? [null] : supervisors.Select(a => (Guid?)a).ToArray();
                        }
                        else recipients = [proof.ResponsibleUserId];
                        foreach (var recipient in recipients)
                        {
                            var eligible = recipient is Guid actor && await BusinessDutyRepository.CurrentAuthority(db, clock,
                                actor, role, project.Key, cancellationToken) ? recipient : null;
                            var key = (eligible, role);
                            if (!groups.TryGetValue(key, out var duties)) groups[key] = duties = [];
                            duties.Add(duty);
                        }
                    }
                    foreach (var group in groups)
                    {
                        // Retain committed legacy occurrence identities and their existing retry path.
                        if (await db.Set<H6NotificationOccurrenceRow>().AnyAsync(o => o.ProjectId == project.Key &&
                            o.EventType == "review.weekly_pending.v1" && o.ScheduledAtUtc == latest &&
                            db.Set<H6NotificationDeliveryRow>().Any(d => d.OccurrenceId == o.Id && d.RecipientUserId == group.Key.Actor), cancellationToken)) continue;
                        var digest = new WeeklyReviewDigest
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = project.Key,
                            RecoveryPeriodId = latestHistory!.Id,
                            ScheduledAtUtc = latest,
                            RecoveredAtUtc = now,
                            RecipientId = group.Key.Actor,
                            RecipientRole = group.Key.Role,
                            RecipientKey = group.Key.Actor is Guid actor ? "actor:" + actor.ToString("N") : "pending:" + group.Key.Role
                        };
                        foreach (var duty in group.Value.DistinctBy(c => c.Id))
                            digest.Duties.Add(new()
                            {
                                DigestId = digest.Id,
                                ClockId = duty.Id,
                                OriginEventId = duty.OriginEventId,
                                TargetId = duty.TargetId,
                                Kind = duty.Kind.ToString(),
                                OriginAtUtc = duty.OriginAt,
                                DueAtRecoveryUtc = duty.CurrentDueAt
                            });
                        db.Add(digest);
                        var source = new H6StoredEvent(1, digest.Id, "WEEKLY_PENDING", digest.ProjectId, "ReviewDigest",
                            digest.Id, digest.Id, now, digest.Id, digest.RecipientId, latest);
                        db.OutboxMessages.Add(OutboxMessage.Create(digest.Id, "review.weekly_pending.v1", now,
                            digest.Id, JsonSerializer.Serialize(source, ClockJson)));
                        admitted++;
                    }
                }
                foreach (var duty in project.Where(c => c.CompletedAt is null))
                    if (!calendars.Any(p => p.ClockId == duty.Id && p.ScheduledAtUtc == next))
                        db.Add(new H6NotificationCalendarRow
                        {
                            Id = Guid.NewGuid(),
                            ClockId = duty.Id,
                            PlannedAtUtc = now,
                            ScheduledAtUtc = next,
                            Status = "PLANNED",
                            SchedulerRunId = schedulerRunId
                        });
            }
            await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        });
        return admitted;
    }
}
