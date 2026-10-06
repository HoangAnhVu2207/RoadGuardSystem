using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Messaging;

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
            var due = await db.Set<H6NotificationCalendarRow>().FromSqlInterpolated($"""
                SELECT TOP (100) * FROM [H6NotificationCalendar] WITH (UPDLOCK,HOLDLOCK)
                WHERE [Status]='PLANNED' AND [ScheduledAtUtc]<={now}
                ORDER BY [ScheduledAtUtc],[Id]
                """).ToArrayAsync(cancellationToken);
            foreach (var period in due)
            {
                var sourceClock = await db.Set<DeadlineClock>().FromSqlInterpolated($"SELECT * FROM [DeadlineClocks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={period.ClockId}")
                    .SingleAsync(cancellationToken);
                if (sourceClock.Kind is not (DeadlineClockKind.ProjectManagerReview or DeadlineClockKind.SupervisorInitialApproval or
                    DeadlineClockKind.SupervisorFinalConfirmation) || sourceClock.CompletedAt is not null)
                {
                    period.ObservedAtUtc = now;
                    period.Status = "NO_REVIEW";
                    continue;
                }
                // Another live run may reach this row first. Preserve the planned run's
                // short callback window; after it expires, the missed-period policy is pending.
                if (period.SchedulerRunId != schedulerRunId && now < period.ScheduledAtUtc.AddMinutes(1))
                    continue;
                // The worker captures the scheduled callback before opening this transaction.
                // A second clock read is only an upper bound, never the callback identity.
                var callbackAt = witness?.ObservedAtUtc ?? now;
                var callbackPrompt = witness is not null && callbackAt <= now &&
                    now - callbackAt < TimeSpan.FromSeconds(30);
                var decision = period.SchedulerRunId == schedulerRunId
                    ? NotificationCalendarAdmissionProof.Evaluate(schedulerRunId, period.PlannedAtUtc,
                        period.ScheduledAtUtc, callbackAt, callbackPrompt ? witness : null)
                    : NotificationCalendarAdmission.PendingCatchupPolicy;
                if (decision != NotificationCalendarAdmission.Ready)
                {
                    period.ObservedAtUtc = now;
                    period.Status = "PENDING_POLICY";
                    continue;
                }
                var source = new H6StoredEvent(1, period.Id, "WEEKLY_PENDING", sourceClock.ProjectId,
                    "ReviewObligation", sourceClock.Id, sourceClock.OriginEventId, callbackAt,
                    SourceRevisionId: period.Id, ScheduledAtUtc: period.ScheduledAtUtc);
                db.OutboxMessages.Add(OutboxMessage.Create(period.Id, "review.weekly_pending.v1", callbackAt,
                    sourceClock.Id, JsonSerializer.Serialize(source, ClockJson)));
                period.ObservedAtUtc = callbackAt;
                period.OutboxMessageId = period.Id;
                period.Status = "COMMITTED";
                admitted++;
            }
            var next = NotificationCalendarPolicy.NextWeeklyReview(now);
            var clocks = await db.Set<DeadlineClock>().FromSqlInterpolated($"""
                SELECT TOP (100) c.* FROM [DeadlineClocks] c WITH (UPDLOCK,HOLDLOCK)
                WHERE c.[Kind] IN ({(byte)DeadlineClockKind.ProjectManagerReview},{(byte)DeadlineClockKind.SupervisorInitialApproval},
                    {(byte)DeadlineClockKind.SupervisorFinalConfirmation}) AND c.[CompletedAt] IS NULL
                  AND NOT EXISTS (SELECT 1 FROM [H6NotificationCalendar] p
                    WHERE p.[ClockId]=c.[Id] AND p.[ScheduledAtUtc]={next})
                ORDER BY c.[Id]
                """).ToArrayAsync(cancellationToken);
            foreach (var sourceClock in clocks)
                db.Set<H6NotificationCalendarRow>().Add(new() { Id = Guid.NewGuid(), ClockId = sourceClock.Id,
                    PlannedAtUtc = now, ScheduledAtUtc = next, Status = "PLANNED", SchedulerRunId = schedulerRunId });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
        return admitted;
    }
}
