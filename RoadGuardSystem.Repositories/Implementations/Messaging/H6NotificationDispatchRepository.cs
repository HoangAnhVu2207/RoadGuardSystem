using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed partial class H6NotificationDispatchRepository : IH6NotificationDispatchRepository
{
    private readonly RoadGuardDbContext db;
    private readonly TimeProvider clock;
    private readonly IH6NotificationSourceAdapter[] sources;
    public H6NotificationDispatchRepository(RoadGuardDbContext db, TimeProvider clock, IEnumerable<IH6NotificationSourceAdapter>? sourceAdapters = null)
    {
        this.db = db; this.clock = clock;
        sources = sourceAdapters?.ToArray() ?? [new H6FieldNotificationSourceAdapter(db), new H6RepairNotificationSourceAdapter(db),
            new H6DeadlineNotificationSourceAdapter(db, clock), new H6WeeklyReviewSourceAdapter(db, clock),
            new H6SafetyNotificationSourceAdapter(db)];
    }
    public async Task<H6Claim?> ClaimAsync(IReadOnlyList<string> registeredTypes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registeredTypes);
        if (registeredTypes.Count == 0 || registeredTypes.Count > NotificationRegisteredTypes.All.Count || registeredTypes.Any(type => !NotificationRegisteredTypes.Owns(type)))
            throw new ArgumentException("Only the finite notification registry can be leased.", nameof(registeredTypes));
        if (db.Database.CurrentTransaction is not null) throw new InvalidOperationException("Claim owns its short independent lease transaction.");
        var typesJson = JsonSerializer.Serialize(registeredTypes.Distinct(StringComparer.Ordinal).ToArray());
        H6Claim? result = null;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear(); var now = clock.GetUtcNow().ToUniversalTime();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            var message = await db.OutboxMessages.FromSqlInterpolated($"""
                SELECT TOP (1) * FROM [OutboxMessages] WITH (UPDLOCK,READPAST,READCOMMITTEDLOCK,ROWLOCK)
                WHERE [DeliveryStatus] NOT IN ({(byte)OutboxDeliveryStatus.Completed},{(byte)OutboxDeliveryStatus.DeadLetter})
                  AND ([DeliveryAttemptCount]<{32} OR [LastErrorCode]='notification_source_pending_producer') AND [NextAttemptAtUtc]<={now}
                  AND ([LeaseExpiresAtUtc] IS NULL OR [LeaseExpiresAtUtc]<={now})
                  AND EXISTS (SELECT 1 FROM OPENJSON({typesJson}) allowed
                    WHERE allowed.[value] COLLATE Latin1_General_100_BIN2=[MessageType] COLLATE Latin1_General_100_BIN2
                      AND DATALENGTH([MessageType])=DATALENGTH(CONVERT(varchar(200),allowed.[value])))
                ORDER BY [NextAttemptAtUtc],[OccurredAtUtc],[Id]
                """).FirstOrDefaultAsync(cancellationToken);
            if (message is not null)
            {
                var fence = Guid.NewGuid(); message.AcquireLease("h6:" + fence.ToString("N"), now, TimeSpan.FromMinutes(2),
                    message.LastErrorCode == "notification_source_pending_producer" ? int.MaxValue : 32);
                await db.SaveChangesAsync(cancellationToken);
                result = new(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson, fence,
                    message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
            }
            await transaction.CommitAsync(cancellationToken);
        });
        return result;
    }
}
