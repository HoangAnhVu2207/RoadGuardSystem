using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Auditing;

public sealed class AuditLog
{
    private AuditLog()
    {
    }

    public Guid Id { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public string? BeforeSnapshot { get; private set; }

    public string? AfterSnapshot { get; private set; }

    public string? Reason { get; private set; }

    public string Source { get; private set; } = string.Empty;

    public Guid? CorrelationId { get; private set; }

    public static AuditLog Create(
        Guid id,
        Guid? actorUserId,
        DateTimeOffset occurredAt,
        string eventType,
        string entityType,
        Guid entityId,
        string? beforeSnapshot,
        string? afterSnapshot,
        string? reason,
        string source,
        Guid? correlationId)
    {
        ValidateRequired(eventType, nameof(eventType), 100);
        ValidateRequired(entityType, nameof(entityType), 100);
        ValidateRequired(source, nameof(source), 80);
        var sanitizedBeforeSnapshot = SanitizeOptionalJson(beforeSnapshot, nameof(beforeSnapshot));
        var sanitizedAfterSnapshot = SanitizeOptionalJson(afterSnapshot, nameof(afterSnapshot));

        return new AuditLog
        {
            Id = id,
            ActorUserId = actorUserId,
            OccurredAtUtc = occurredAt.ToUniversalTime(),
            EventType = eventType.Trim(),
            EntityType = entityType.Trim(),
            EntityId = entityId,
            BeforeSnapshot = sanitizedBeforeSnapshot,
            AfterSnapshot = sanitizedAfterSnapshot,
            Reason = reason,
            Source = source.Trim(),
            CorrelationId = correlationId
        };
    }

    private static void ValidateRequired(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maxLength}.", parameterName);
        }
    }

    private static string? SanitizeOptionalJson(string? json, string parameterName)
    {
        if (json is null)
        {
            return null;
        }

        try
        {
            return SensitiveJsonSanitizer.Redact(json);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Value must be valid JSON.", parameterName, exception);
        }
    }
}

internal static class StructuredJsonValidation
{
    internal static void EnsureObjectOrArray(JsonElement root, string parameterName)
    {
        if (root.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
        {
            throw new ArgumentException("JSON root must be an object or array.", parameterName);
        }
    }
}
