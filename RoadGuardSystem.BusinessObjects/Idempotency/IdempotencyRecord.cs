using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Auditing;

namespace RoadGuardSystem.BusinessObjects.Idempotency;

public sealed class IdempotencyRecord
{
    private IdempotencyRecord()
    {
    }

    public Guid Id { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public Guid? ProjectId { get; private set; }

    public string Operation { get; private set; } = string.Empty;

    public string IdempotencyKey { get; private set; } = string.Empty;

    public string RequestFingerprint { get; private set; } = string.Empty;

    public Guid OperationId { get; private set; }

    public string OutcomeJson { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static IdempotencyRecord Create(
        Guid? actorUserId,
        Guid? projectId,
        string operation,
        string idempotencyKey,
        string requestFingerprint,
        Guid operationId,
        string outcomeJson,
        DateTimeOffset createdAt)
    {
        ValidateRequired(operation, nameof(operation), 100);
        ValidateRequired(idempotencyKey, nameof(idempotencyKey), 200);
        ValidateFingerprint(requestFingerprint, nameof(requestFingerprint));
        ValidateJson(outcomeJson, nameof(outcomeJson));

        return new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ProjectId = projectId,
            Operation = operation.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            RequestFingerprint = requestFingerprint,
            OperationId = operationId,
            OutcomeJson = outcomeJson,
            CreatedAtUtc = createdAt.ToUniversalTime()
        };
    }

    public static void ValidateScope(
        string operation,
        string idempotencyKey,
        string requestFingerprint)
    {
        ValidateRequired(operation, nameof(operation), 100);
        ValidateRequired(idempotencyKey, nameof(idempotencyKey), 200);
        ValidateFingerprint(requestFingerprint, nameof(requestFingerprint));
    }

    private static void ValidateRequired(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maxLength}.", parameterName);
        }
    }

    private static void ValidateFingerprint(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length != 64 || value.Any(character => character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
        {
            throw new ArgumentException(
                "Request fingerprint must be a lowercase SHA-256 hexadecimal value.",
                parameterName);
        }
    }

    private static void ValidateJson(string json, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json, parameterName);
        try
        {
            using var document = JsonDocument.Parse(json);
            StructuredJsonValidation.EnsureObjectOrArray(document.RootElement, parameterName);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Value must be valid JSON.", parameterName, exception);
        }
    }
}
