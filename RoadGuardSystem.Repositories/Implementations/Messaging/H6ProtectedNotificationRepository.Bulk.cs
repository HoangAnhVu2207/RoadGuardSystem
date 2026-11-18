using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Idempotency;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed partial class H6ProtectedNotificationRepository
{
    private sealed class BulkReceiptExists : Exception;
    private const string BulkOperation = "NotificationReadBatch.v1";
    private sealed record BulkOutcome(Guid[] Ids, bool HasMore, DateTimeOffset Through, string SnapshotVersion);

    public async Task<H6BulkReadFact> MarkManyReadAsync(Guid actor, UserRoleCode role, DateTimeOffset through, int limit,
        string key, string? snapshotVersion, CancellationToken token)
    {
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{through:O}|{limit}|{snapshotVersion}"))).ToLowerInvariant();
        try
        {
            var outcome = await receipts.ExecuteSerializableAsync(actor, null, BulkOperation, key, fingerprint, async ct =>
            {
                if (await PrincipalAsync(actor, role, ct) is null) throw new AccessDenied(NotificationMarkReadPersistenceStatus.Unauthorized);
                if (await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor && row.ProjectId == null &&
                    row.Operation == BulkOperation && row.IdempotencyKey == key, ct)) throw new BulkReceiptExists();
                var watermark = snapshotVersion is null
                    ? await db.Database.SqlQueryRaw<byte[]>("SELECT CONVERT(varbinary(8),@@DBTS) AS [Value]").SingleAsync(ct)
                    : Convert.FromBase64String(snapshotVersion);
                var pinnedIds = db.Notifications.FromSqlInterpolated($"SELECT * FROM [Notifications] WHERE [RowVersion] <= {watermark}").Select(row => row.Id);
                var rows = await Visible(actor, role).Where(row => row.ReadAt == null && row.OccurredAtUtc <= through && pinnedIds.Contains(row.Id))
                    .OrderBy(row => row.OccurredAtUtc).ThenBy(row => row.Id).Take(limit + 1).ToArrayAsync(ct);
                var selected = rows.Take(limit).ToArray();
                foreach (var row in selected) row.MarkRead(clock.GetUtcNow());
                await db.SaveChangesAsync(ct);
                return (Guid.NewGuid(), JsonSerializer.Serialize(new BulkOutcome(selected.Select(row => row.Id).ToArray(), rows.Length > limit, through, Convert.ToBase64String(watermark))));
            }, token, async ct =>
            {
                if (await PrincipalAsync(actor, role, ct) is null) throw new AccessDenied(NotificationMarkReadPersistenceStatus.Unauthorized);
                var receipt = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(row => row.ActorUserId == actor &&
                    row.ProjectId == null && row.Operation == BulkOperation && row.IdempotencyKey == key, ct);
                if (receipt is null) return;
                var stored = JsonSerializer.Deserialize<BulkOutcome>(receipt.OutcomeJson, ReadJson)!;
                if (await Visible(actor, role).CountAsync(row => stored.Ids.Contains(row.Id), ct) != stored.Ids.Length)
                    throw new AccessDenied(NotificationMarkReadPersistenceStatus.NotFound);
            });
            if (outcome.Status == IdempotencyOperationStatus.Conflict) return new(409, "duplicate_request", 0, false, through);
            var value = JsonSerializer.Deserialize<BulkOutcome>(outcome.OutcomeJson, ReadJson)!;
            return new(200, null, value.Ids.Length, value.HasMore, value.Through, value.SnapshotVersion);
        }
        catch (BulkReceiptExists) { db.ChangeTracker.Clear(); return await MarkManyReadAsync(actor, role, through, limit, key, snapshotVersion, token); }
        catch (AccessDenied) { db.ChangeTracker.Clear(); return new(403, "access_forbidden", 0, false, through); }
    }
}
