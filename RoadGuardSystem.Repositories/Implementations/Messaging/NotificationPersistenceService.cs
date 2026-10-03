using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.Repositories.Idempotency;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed class NotificationPersistenceService : INotificationRepository
{
    private const string ReadOperation = "NotificationRead";
    private readonly RoadGuardDbContext _context;
    private readonly IdempotencyOperationService _idempotency;

    public NotificationPersistenceService(
        RoadGuardDbContext context,
        IdempotencyOperationService idempotency)
    {
        _context = context;
        _idempotency = idempotency;
    }

    public Task<NotificationReadView?> GetAsync(
        Guid recipientUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        return _context.Notifications
            .AsNoTracking()
            .Where(notification => notification.RecipientUserId == recipientUserId && notification.Id == notificationId)
            .Select(notification => new NotificationReadView(
                notification.Id,
                notification.Body,
                notification.SourceEntityId,
                notification.ReadAt != null,
                notification.OccurredAtUtc,
                notification.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<NotificationPageReadResult> ListAsync(
        Guid recipientUserId,
        DateTimeOffset? beforeOccurredAtUtc,
        Guid? beforeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(notification => notification.RecipientUserId == recipientUserId);

        if (beforeOccurredAtUtc is not null && beforeId is not null)
        {
            var cursorTime = beforeOccurredAtUtc.Value;
            var cursorId = beforeId.Value;
            query = query.Where(notification =>
                notification.OccurredAtUtc < cursorTime ||
                (notification.OccurredAtUtc == cursorTime && notification.Id.CompareTo(cursorId) < 0));
        }

        var rows = await query
            .OrderByDescending(notification => notification.OccurredAtUtc)
            .ThenByDescending(notification => notification.Id)
            .Take(limit + 1)
            .Select(notification => new NotificationReadView(
                notification.Id,
                notification.Body,
                notification.SourceEntityId,
                notification.ReadAt != null,
                notification.OccurredAtUtc,
                notification.RowVersion))
            .ToListAsync(cancellationToken);

        var hasNextPage = rows.Count > limit;
        if (hasNextPage)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        var last = hasNextPage ? rows[^1] : null;
        return new NotificationPageReadResult(
            rows,
            last?.OccurredAtUtc,
            last?.Id,
            DateTimeOffset.UtcNow);
    }

    public Task<NotificationMarkReadPersistenceResult> MarkReadAsync(
        Guid recipientUserId, Guid notificationId, string idempotencyKey,
        string requestFingerprint, string expectedVersion, CancellationToken cancellationToken = default)
        => MarkReadAsync(recipientUserId, notificationId, idempotencyKey, requestFingerprint,
            expectedVersion, null, cancellationToken);

    public async Task<NotificationMarkReadPersistenceResult> MarkReadAsync(
        Guid recipientUserId,
        Guid notificationId,
        string idempotencyKey,
        string requestFingerprint,
        string expectedVersion,
        UserRoleCode? authenticatedRole,
        CancellationToken cancellationToken = default)
    {
        IdempotencyOperationResult outcome;
        try
        {
            outcome = await _idempotency.ExecuteAsync(
                recipientUserId,
                null,
                ReadOperation,
                idempotencyKey,
                requestFingerprint,
                async token =>
                {
                    var notification = await ReadAuthorizedNotificationAsync(
                        recipientUserId, notificationId, authenticatedRole, tracking: true, token);
                    if (notification is null)
                    {
                        return (Guid.NewGuid(), Serialize(NotificationMarkReadPersistenceStatus.NotFound, null));
                    }

                    var currentVersion = Convert.ToBase64String(notification.RowVersion);
                    if (!string.Equals(currentVersion, expectedVersion, StringComparison.Ordinal))
                    {
                        return (Guid.NewGuid(), Serialize(NotificationMarkReadPersistenceStatus.StaleConcurrency, null));
                    }

                    notification.MarkRead(DateTimeOffset.UtcNow);
                    await _context.SaveChangesAsync(token);
                    return (
                        Guid.NewGuid(),
                        Serialize(NotificationMarkReadPersistenceStatus.Success, ToView(notification)));
                },
                cancellationToken,
                async token =>
                {
                    var notification = await ReadAuthorizedNotificationAsync(
                        recipientUserId, notificationId, authenticatedRole, tracking: false, token);
                    if (notification is not null) return;

                    // A previously stored not-found outcome remains replayable for an active
                    // principal. A missing row must not expose an old successful receipt.
                    var receipt = await _context.IdempotencyRecords.AsNoTracking().SingleAsync(
                        row => row.ActorUserId == recipientUserId && row.ProjectId == null &&
                            row.Operation == ReadOperation && row.IdempotencyKey == idempotencyKey.Trim(), token);
                    if (JsonSerializer.Deserialize<StoredOutcome>(receipt.OutcomeJson)?.Status !=
                        NotificationMarkReadPersistenceStatus.NotFound)
                        throw new NotificationAccessDeniedException(NotificationMarkReadPersistenceStatus.NotFound);
                });
        }
        catch (NotificationAccessDeniedException exception)
        {
            _context.ChangeTracker.Clear();
            return new(exception.Status);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            return new(NotificationMarkReadPersistenceStatus.StaleConcurrency);
        }

        if (outcome.Status == IdempotencyOperationStatus.Conflict)
        {
            return new(NotificationMarkReadPersistenceStatus.IdempotentConflict);
        }

        var stored = JsonSerializer.Deserialize<StoredOutcome>(outcome.OutcomeJson)
            ?? throw new InvalidOperationException("Notification idempotency outcome is invalid.");
        var status = stored.Status;
        if (outcome.Status == IdempotencyOperationStatus.Replayed &&
            status == NotificationMarkReadPersistenceStatus.Success)
        {
            status = NotificationMarkReadPersistenceStatus.Replayed;
        }

        return new(status, stored.Notification);
    }

    // Both callers run inside the shared engine's transaction. Read authoritative rows
    // afresh; hold update/range locks until outcome/receipt commit, in this stable order.
    // Session revoke/expiry is checked by HTTP authentication at the request boundary.
    private async Task<Notification?> ReadAuthorizedNotificationAsync(
        Guid actor, Guid notificationId, UserRoleCode? authenticatedRole, bool tracking, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var user = await _context.Users.FromSqlInterpolated(
            $"SELECT * FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={actor}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (user is null || user.Status != UserStatus.Active || user.MustChangePassword ||
            user.RoleCode == UserRoleCode.Unknown ||
            (authenticatedRole.HasValue && user.RoleCode != authenticatedRole.Value))
            throw new NotificationAccessDeniedException(NotificationMarkReadPersistenceStatus.Unauthorized);
        var roleCode = user.RoleCode.ToDbCode();
        var role = await _context.Roles.FromSqlInterpolated(
            $"SELECT * FROM [Roles] WITH (UPDLOCK,HOLDLOCK) WHERE [Code]={roleCode}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (role is null || !role.IsActive)
            throw new NotificationAccessDeniedException(NotificationMarkReadPersistenceStatus.Unauthorized);
        var query = _context.Notifications.FromSqlInterpolated(
            $"SELECT * FROM [Notifications] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={notificationId}");
        var notification = await (tracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(token);
        if (notification is not null && notification.RecipientUserId != actor)
        {
            if (tracking) return null; // Preserve the ordinary stored not-found command outcome.
            throw new NotificationAccessDeniedException(NotificationMarkReadPersistenceStatus.NotFound);
        }
        return notification;
    }

    private sealed class NotificationAccessDeniedException(NotificationMarkReadPersistenceStatus status) : Exception
    {
        public NotificationMarkReadPersistenceStatus Status { get; } = status;
    }

    private static NotificationReadView ToView(RoadGuardSystem.BusinessObjects.Messaging.Notification notification)
        => new(
            notification.Id,
            notification.Body,
            notification.SourceEntityId,
            notification.ReadAt is not null,
            notification.OccurredAtUtc,
            notification.RowVersion);

    private static string Serialize(
        NotificationMarkReadPersistenceStatus status,
        NotificationReadView? notification)
        => JsonSerializer.Serialize(new StoredOutcome(status, notification));

    private sealed record StoredOutcome(
        NotificationMarkReadPersistenceStatus Status,
        NotificationReadView? Notification);
}
