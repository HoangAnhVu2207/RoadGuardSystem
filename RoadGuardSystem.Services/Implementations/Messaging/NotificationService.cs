using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Messaging;

public sealed class NotificationService : INotificationService
{
    private const int DefaultLimit = 25;
    private const int MaxLimit = 100;
    private readonly INotificationRepository _repository;

    public NotificationService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<NotificationReadServiceResult> GetAsync(
        Guid actorUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || notificationId == Guid.Empty)
        {
            return new(NotificationServiceStatus.InvalidInput);
        }

        var notification = await _repository.GetAsync(actorUserId, notificationId, cancellationToken);
        return notification is null
            ? new(NotificationServiceStatus.NotFound)
            : new(NotificationServiceStatus.Success, ToDto(notification));
    }

    public async Task<NotificationPageServiceResult> ListAsync(
        Guid actorUserId,
        string? cursor,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || limit is < 1 or > MaxLimit)
        {
            return new(NotificationServiceStatus.InvalidInput);
        }

        var effectiveLimit = limit ?? DefaultLimit;
        if (!TryDecodeCursor(cursor, out var cursorValue))
        {
            return new(NotificationServiceStatus.InvalidInput);
        }

        var result = await _repository.ListAsync(
            actorUserId,
            cursorValue?.OccurredAtUtc,
            cursorValue?.Id,
            effectiveLimit,
            cancellationToken);

        string? nextCursor = null;
        if (result.NextOccurredAtUtc is not null && result.NextId is not null)
        {
            nextCursor = EncodeCursor(new NotificationCursor(result.NextOccurredAtUtc.Value, result.NextId.Value));
        }

        return new(
            NotificationServiceStatus.Success,
            new NotificationPageDto(
                result.Items.Select(ToDto).ToArray(),
                nextCursor,
                result.AsOf));
    }

    public Task<NotificationReadServiceResult> MarkReadAsync(
        Guid actorUserId, Guid notificationId, string idempotencyKey,
        string expectedVersion, CancellationToken cancellationToken = default)
        => MarkReadAsync(actorUserId, notificationId, idempotencyKey, expectedVersion, null, cancellationToken);

    public async Task<NotificationReadServiceResult> MarkReadAsync(
        Guid actorUserId,
        Guid notificationId,
        string idempotencyKey,
        string expectedVersion,
        UserRoleCode? authenticatedRole,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || notificationId == Guid.Empty || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return new(NotificationServiceStatus.InvalidInput);
        }

        var normalizedVersion = expectedVersion?.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(normalizedVersion))
        {
            return new(NotificationServiceStatus.InvalidInput);
        }

        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{notificationId:N}|{normalizedVersion}"))).ToLowerInvariant();
        var result = await _repository.MarkReadAsync(
            actorUserId,
            notificationId,
            idempotencyKey.Trim(),
            fingerprint,
            normalizedVersion,
            authenticatedRole,
            cancellationToken);

        return result.Status switch
        {
            NotificationMarkReadPersistenceStatus.Success when result.Notification is not null =>
                new(NotificationServiceStatus.Success, ToDto(result.Notification)),
            NotificationMarkReadPersistenceStatus.Replayed when result.Notification is not null =>
                new(NotificationServiceStatus.Replayed, ToDto(result.Notification)),
            NotificationMarkReadPersistenceStatus.NotFound => new(NotificationServiceStatus.NotFound),
            NotificationMarkReadPersistenceStatus.StaleConcurrency => new(NotificationServiceStatus.StaleConcurrency),
            NotificationMarkReadPersistenceStatus.IdempotentConflict => new(NotificationServiceStatus.IdempotentConflict),
            NotificationMarkReadPersistenceStatus.Unauthorized => new(NotificationServiceStatus.Unauthorized),
            _ => new(NotificationServiceStatus.InvalidInput)
        };
    }

    private static NotificationDto ToDto(NotificationReadView value)
        => new(
            value.Id,
            value.Message,
            value.ResourceId,
            value.Read,
            value.OccurredAtUtc,
            Convert.ToBase64String(value.RowVersion));

    private static string EncodeCursor(NotificationCursor cursor)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cursor)));

    private static bool TryDecodeCursor(string? encoded, out NotificationCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return true;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            cursor = JsonSerializer.Deserialize<NotificationCursor>(json);
            return cursor is not null && cursor.Id != Guid.Empty && cursor.OccurredAtUtc != default;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record NotificationCursor(DateTimeOffset OccurredAtUtc, Guid Id);
}
