using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.Repositories.Idempotency;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed partial class H6ProtectedNotificationRepository : INotificationRepository, IH6NotificationOperationsRepository
{
    private const string ReadOperation = "NotificationRead";
    private static readonly JsonSerializerOptions ReadJson = new() { PropertyNameCaseInsensitive = true };
    private readonly RoadGuardDbContext db;
    private readonly IdempotencyOperationService receipts;
    private readonly TimeProvider clock;
    private readonly IH6NotificationSourceAdapter[] sources;
    public H6ProtectedNotificationRepository(RoadGuardDbContext context, IdempotencyOperationService receipts,
        TimeProvider clock, IEnumerable<IH6NotificationSourceAdapter>? sources = null)
    { db = context; this.receipts = receipts; this.clock = clock; this.sources = (sources ?? []).ToArray(); }

    public Task<NotificationReadView?> GetAsync(Guid recipientUserId, Guid notificationId, CancellationToken cancellationToken = default)
        => ReadAsync<NotificationReadView?>(recipientUserId, async (role, token) =>
            await Visible(recipientUserId, role).AsNoTracking().Where(row => row.Id == notificationId)
                .Select(row => new NotificationReadView(row.Id, row.Body, row.SourceEntityId, row.ReadAt != null, row.OccurredAtUtc, row.RowVersion))
                .SingleOrDefaultAsync(token), () => null, cancellationToken);

    public Task<NotificationPageReadResult> ListAsync(Guid recipientUserId, DateTimeOffset? beforeOccurredAtUtc,
        Guid? beforeId, int limit, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1); ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, 100);
        return ReadAsync(recipientUserId, async (role, token) =>
        {
            var query = Visible(recipientUserId, role).AsNoTracking();
            if (beforeOccurredAtUtc.HasValue && beforeId.HasValue)
            {
                var at = beforeOccurredAtUtc.Value; var id = beforeId.Value;
                query = query.Where(row => row.OccurredAtUtc < at || row.OccurredAtUtc == at && row.Id.CompareTo(id) < 0);
            }
            var rows = await query.OrderByDescending(row => row.OccurredAtUtc).ThenByDescending(row => row.Id).Take(limit + 1)
                .Select(row => new NotificationReadView(row.Id, row.Body, row.SourceEntityId, row.ReadAt != null, row.OccurredAtUtc, row.RowVersion))
                .ToListAsync(token);
            var more = rows.Count > limit; if (more) rows.RemoveAt(rows.Count - 1);
            var last = more ? rows[^1] : null;
            return new NotificationPageReadResult(rows, last?.OccurredAtUtc, last?.Id, clock.GetUtcNow());
        }, () => new NotificationPageReadResult([], null, null, clock.GetUtcNow()), cancellationToken);
    }

    public Task<NotificationMarkReadPersistenceResult> MarkReadAsync(Guid recipientUserId, Guid notificationId,
        string idempotencyKey, string requestFingerprint, string expectedVersion, CancellationToken cancellationToken = default)
        => MarkReadAsync(recipientUserId, notificationId, idempotencyKey, requestFingerprint, expectedVersion, null, cancellationToken);

    public async Task<NotificationMarkReadPersistenceResult> MarkReadAsync(Guid recipientUserId, Guid notificationId,
        string idempotencyKey, string requestFingerprint, string expectedVersion, UserRoleCode? authenticatedRole,
        CancellationToken cancellationToken = default)
    {
        IdempotencyOperationResult result;
        try
        {
            result = await receipts.ExecuteSerializableAsync(recipientUserId, null, ReadOperation, idempotencyKey,
                requestFingerprint, async token =>
                {
                    var notification = await AuthorizedRowAsync(recipientUserId, notificationId, authenticatedRole, true, token);
                    if (notification is null) return (Guid.NewGuid(), Serialize(NotificationMarkReadPersistenceStatus.NotFound, null, notificationId));
                    if (Convert.ToBase64String(notification.RowVersion) != expectedVersion)
                        return (Guid.NewGuid(), Serialize(NotificationMarkReadPersistenceStatus.StaleConcurrency, null, notificationId));
                    notification.MarkRead(clock.GetUtcNow()); await db.SaveChangesAsync(token);
                    return (Guid.NewGuid(), Serialize(NotificationMarkReadPersistenceStatus.Success, View(notification), notificationId));
                }, cancellationToken, async token =>
                {
                    var notification = await AuthorizedRowAsync(recipientUserId, notificationId, authenticatedRole, false, token);
                    var receipt = await db.IdempotencyRecords.AsNoTracking().SingleAsync(row => row.ActorUserId == recipientUserId &&
                        row.ProjectId == null && row.Operation == ReadOperation && row.IdempotencyKey == idempotencyKey.Trim(), token);
                    var original = JsonSerializer.Deserialize<StoredOutcome>(receipt.OutcomeJson, ReadJson);
                    // Old successful receipts carry their original resource in the copied view. New
                    // outcomes also pin stale/missing targets; an unproven legacy target stays protected.
                    var originalId = original?.TargetNotificationId ?? original?.Notification?.Id;
                    if (originalId is not Guid target || target == Guid.Empty)
                        throw new AccessDenied(NotificationMarkReadPersistenceStatus.NotFound);
                    var originalRow = target == notificationId ? notification :
                        await AuthorizedRowAsync(recipientUserId, target, authenticatedRole, false, token);
                    if (originalRow is null && original?.Status != NotificationMarkReadPersistenceStatus.NotFound ||
                        notification is null && (target != notificationId || original?.Status != NotificationMarkReadPersistenceStatus.NotFound))
                        throw new AccessDenied(NotificationMarkReadPersistenceStatus.NotFound);
                });
        }
        catch (AccessDenied exception) { db.ChangeTracker.Clear(); return new(exception.Status); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(NotificationMarkReadPersistenceStatus.StaleConcurrency); }
        if (result.Status == IdempotencyOperationStatus.Conflict) return new(NotificationMarkReadPersistenceStatus.IdempotentConflict);
        var stored = JsonSerializer.Deserialize<StoredOutcome>(result.OutcomeJson, ReadJson)
            ?? throw new InvalidOperationException("Notification receipt outcome is invalid.");
        return new(result.Status == IdempotencyOperationStatus.Replayed && stored.Status == NotificationMarkReadPersistenceStatus.Success
            ? NotificationMarkReadPersistenceStatus.Replayed : stored.Status, stored.Notification);
    }

    private async Task<Notification?> AuthorizedRowAsync(Guid actor, Guid id, UserRoleCode? expectedRole, bool tracking, CancellationToken token)
    {
        var role = await PrincipalAsync(actor, expectedRole, token);
        if (role is null) throw new AccessDenied(NotificationMarkReadPersistenceStatus.Unauthorized);
        var query = db.Notifications.FromSqlInterpolated($"SELECT * FROM [Notifications] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}");
        var row = await (tracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(token);
        if (row is null) return null;
        if (row.RecipientUserId != actor || !await Visible(actor, role.Value).AnyAsync(value => value.Id == id, token))
            throw new AccessDenied(NotificationMarkReadPersistenceStatus.NotFound);
        return row;
    }

    private async Task<T> ReadAsync<T>(Guid actor, Func<UserRoleCode, CancellationToken, Task<T>> action, Func<T> denied, CancellationToken token,
        UserRoleCode? expectedRole = null)
    {
        async Task<T> Read()
        {
            var role = await PrincipalAsync(actor, expectedRole, token);
            if (role is null) return denied();
            await BackfillOwnedScopesAsync(actor, token);
            return await action(role.Value, token);
        }
        if (db.Database.CurrentTransaction is not null) return await Read();
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var value = await Read(); await transaction.CommitAsync(token); return value;
        });
    }
    private async Task<UserRoleCode?> PrincipalAsync(Guid actor, UserRoleCode? expectedRole, CancellationToken token)
    {
        await Anh02ReceiptAuthority.LockAsync(db, actor, null, token);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(row => row.Id == actor, token);
        if (user is null || user.Status != UserStatus.Active || user.MustChangePassword || user.RoleCode == UserRoleCode.Unknown ||
            expectedRole.HasValue && user.RoleCode != expectedRole.Value ||
            !await db.Roles.AsNoTracking().AnyAsync(row => row.Code == user.RoleCode && row.IsActive, token)) return null;
        await db.ProjectMembers.FromSqlInterpolated($"SELECT * FROM [ProjectMembers] WITH (UPDLOCK,HOLDLOCK) WHERE [UserId]={actor} ORDER BY [ProjectId],[Id]")
            .AsNoTracking().ToListAsync(token);
        return user.RoleCode;
    }
    private static NotificationReadView View(Notification row) => new(row.Id, row.Body, row.SourceEntityId, row.ReadAt != null, row.OccurredAtUtc, row.RowVersion);
    // Preserve original internal JSON names so old readers can decode outcomes.
    private static string Serialize(NotificationMarkReadPersistenceStatus status, NotificationReadView? notification, Guid targetNotificationId)
        => JsonSerializer.Serialize(new StoredOutcome(status, notification, targetNotificationId));
    private sealed record StoredOutcome(NotificationMarkReadPersistenceStatus Status, NotificationReadView? Notification, Guid? TargetNotificationId = null);
    private sealed class AccessDenied(NotificationMarkReadPersistenceStatus status) : Exception
    { public NotificationMarkReadPersistenceStatus Status { get; } = status; }
}
