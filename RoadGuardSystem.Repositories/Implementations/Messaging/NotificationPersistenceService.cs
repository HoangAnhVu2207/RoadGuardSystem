using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

    public async Task<NotificationMarkReadPersistenceResult> MarkReadAsync(
        Guid recipientUserId,
        Guid notificationId,
        string idempotencyKey,
        string requestFingerprint,
        string expectedVersion,
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
                    var notification = await _context.Notifications
                        .SingleOrDefaultAsync(
                            candidate => candidate.Id == notificationId && candidate.RecipientUserId == recipientUserId,
                            token);
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
                cancellationToken);
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
