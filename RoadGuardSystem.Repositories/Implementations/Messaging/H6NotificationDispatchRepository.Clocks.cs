using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed partial class H6NotificationDispatchRepository
{
    private static readonly JsonSerializerOptions ClockJson = new(JsonSerializerDefaults.Web);
    public async Task<int> ObserveClocksAsync(CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is not null) throw new InvalidOperationException("Clock observation owns its source admission transaction.");
        var admitted = 0;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear(); admitted = 0; var now = clock.GetUtcNow().ToUniversalTime();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            // Filter before the bounded batch: already admitted old clocks must not starve a
            // later source. Historical breaches remain admissible after extension/completion.
            var clocks = await db.Set<DeadlineClock>().FromSqlInterpolated($"""
                SELECT TOP (100) c.* FROM [DeadlineClocks] c WITH (UPDLOCK,HOLDLOCK)
                WHERE (c.[CompletedAt] IS NULL AND c.[CurrentDueAt]<={now}
                  AND NOT EXISTS (SELECT 1 FROM [DeadlineBreaches] b WHERE b.[ClockId]=c.[Id] AND b.[DueAt]=c.[CurrentDueAt]))
                  OR EXISTS (SELECT 1 FROM [DeadlineBreaches] b WHERE b.[ClockId]=c.[Id]
                    AND NOT EXISTS (SELECT 1 FROM [OutboxMessages] o WHERE o.[Id]=b.[Id]))
                ORDER BY c.[CurrentDueAt],c.[Id]
                """).Include(row => row.Breaches).Include(row => row.Extensions).AsSplitQuery().ToArrayAsync(cancellationToken);
            foreach (var sourceClock in clocks)
            {
                sourceClock.ObserveBreach(Guid.NewGuid(), now);
                foreach (var breach in sourceClock.Breaches.OrderBy(row => row.DueAt).ThenBy(row => row.Id))
                {
                    if (await db.OutboxMessages.AnyAsync(row => row.Id == breach.Id, cancellationToken)) continue;
                    var source = new H6StoredEvent(1, breach.Id, "BREACHED", sourceClock.ProjectId,
                        "DeadlineClock", sourceClock.Id, breach.Id, breach.ObservedAt, breach.Id);
                    db.OutboxMessages.Add(OutboxMessage.Create(breach.Id, "deadline.breached.v1", breach.ObservedAt,
                        sourceClock.Id, JsonSerializer.Serialize(source, ClockJson)));
                    if (sourceClock.Kind == DeadlineClockKind.ProjectManagerReview && sourceClock.CompletedAt is null &&
                        await db.FieldInspectionSubmissions.AnyAsync(s => s.Id == sourceClock.OriginEventId &&
                            s.RootId == s.Id && s.ProjectId == sourceClock.ProjectId && s.TaskId == sourceClock.TargetId &&
                            s.ServerReceivedAt == sourceClock.OriginAt, cancellationToken) &&
                        !await db.Set<BusinessReceivingRequest>().AnyAsync(r => r.SourceKind == "ReviewBreach" && r.SourceId == breach.Id, cancellationToken))
                    {
                        var effectiveProject = await new H6DeadlineNotificationSourceAdapter(db, clock).ResponsibilityProjectAsync(sourceClock, cancellationToken);
                        var day = DateOnly.FromDateTime(now.UtcDateTime);
                        var supervisors = await db.ProjectMembers.Where(m => m.ProjectId == effectiveProject &&
                            m.RoleCode == UserRoleCode.Supervisor && m.Status == ProjectMemberStatus.Active &&
                            m.ValidFrom <= day && (m.ValidTo == null || m.ValidTo >= day))
                            .Select(m => m.UserId).Distinct().Take(2).ToArrayAsync(cancellationToken);
                        db.Add(BusinessReceivingRequest.Create(Guid.NewGuid(), sourceClock.ProjectId,
                            DeadlineClockKind.SupervisorEscalation, "ReviewBreach", breach.Id, breach.Id.ToString("N"),
                            sourceClock.Id, supervisors.Length == 1 ? supervisors[0] : null, UserRoleCode.Supervisor, breach.ObservedAt));
                    }
                    admitted++;
                }
            }
            var settled = await db.Set<BusinessReceivingRequest>().Where(r => r.SourceKind == "ReviewBreach" &&
                r.CompletedAt == null && db.Set<DeadlineClock>().Any(c => c.Id == r.ScopeId && c.CompletedAt != null))
                .Include(r => r.Clock).ThenInclude(c => c!.Breaches).ToArrayAsync(cancellationToken);
            foreach (var request in settled) request.Complete(now);
            await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        });
        return admitted;
    }
}
