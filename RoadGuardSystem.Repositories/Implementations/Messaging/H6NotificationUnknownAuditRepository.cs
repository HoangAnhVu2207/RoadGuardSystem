using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed class H6NotificationUnknownAuditRepository(RoadGuardDbContext db, TimeProvider clock) : IH6NotificationUnknownAuditRepository
{
    public async Task<int> AuditUnregisteredAsync(CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is not null) throw new InvalidOperationException("Diagnostic scan owns its short audit transaction.");
        var types = JsonSerializer.Serialize(NotificationRegisteredTypes.All); var recorded = 0;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear(); recorded = 0;
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            // This query never acquires or updates a delivery lease. Exact exclusion precedes TOP;
            // NOT EXISTS audit avoids repeatedly selecting old rows and starving later diagnostics.
            var candidates = await db.OutboxMessages.FromSqlInterpolated($"""
                SELECT TOP (200) o.* FROM [OutboxMessages] o WITH (UPDLOCK,HOLDLOCK)
                WHERE o.[DeliveryStatus] NOT IN ({(byte)OutboxDeliveryStatus.Completed},{(byte)OutboxDeliveryStatus.DeadLetter})
                  AND (o.[MessageType] COLLATE Latin1_General_100_BIN2 LIKE 'field.task.%'
                    OR o.[MessageType] COLLATE Latin1_General_100_BIN2 LIKE 'repair.work.%'
                    OR o.[MessageType] COLLATE Latin1_General_100_BIN2 LIKE 'repair.decision.%'
                    OR o.[MessageType] COLLATE Latin1_General_100_BIN2 LIKE 'review.supervisor[_]required.%'
                    OR o.[MessageType] COLLATE Latin1_General_100_BIN2 LIKE 'review.weekly[_]pending.%'
                    OR o.[MessageType] COLLATE Latin1_General_100_BIN2 LIKE 'deadline.%'
                    OR o.[MessageType] COLLATE Latin1_General_100_BIN2 LIKE 'safety.%'
                    OR o.[MessageType] COLLATE Latin1_General_100_BIN2 LIKE 'project.obligation[_]transferred.%'
                    OR o.[MessageType] COLLATE Latin1_General_100_BIN2 LIKE 'project.handling[_]renewed.%')
                  AND NOT EXISTS (SELECT 1 FROM OPENJSON({types}) allowed
                    WHERE allowed.[value] COLLATE Latin1_General_100_BIN2=o.[MessageType] COLLATE Latin1_General_100_BIN2
                      AND DATALENGTH(o.[MessageType])=DATALENGTH(CONVERT(varchar(200),allowed.[value])))
                  AND NOT EXISTS (SELECT 1 FROM [H6NotificationAudits] a WHERE a.[OutboxMessageId]=o.[Id]
                    AND a.[ResolverVersion]={NotificationRegisteredTypes.RegistryVersion} AND a.[ReasonCode]='notification_event_unregistered')
                ORDER BY o.[OccurredAtUtc],o.[Id]
                """).AsNoTracking().ToArrayAsync(cancellationToken);
            foreach (var message in candidates)
            {
                if (!NotificationRegisteredTypes.IsOwnedUnregistered(message.MessageType)) continue;
                db.Set<H6NotificationAuditRow>().Add(new()
                {
                    Id = Guid.NewGuid(),
                    OutboxMessageId = message.Id,
                    Classification = "UNKNOWN_PROTECTED",
                    SourceKind = "UNREGISTERED",
                    SourceId = Guid.Empty,
                    ReasonCode = "notification_event_unregistered",
                    ResolverVersion = NotificationRegisteredTypes.RegistryVersion,
                    DedupKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(message.Id.ToString("N") + "|" + NotificationRegisteredTypes.RegistryVersion))).ToLowerInvariant(),
                    RecordedAtUtc = clock.GetUtcNow().ToUniversalTime()
                });
                recorded++;
            }
            await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        });
        return recorded;
    }
}
