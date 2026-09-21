using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Messaging;

using RoadGuardSystem.aBusinessObjects.Commons;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    public string MessageType { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public Guid? CorrelationId { get; private set; }

    public string PayloadJson { get; private set; } = string.Empty;

    public OutboxDeliveryStatus DeliveryStatus { get; private set; }

    public int DeliveryAttemptCount { get; private set; }

    public DateTimeOffset NextAttemptAtUtc { get; private set; }

    public string? LeaseOwner { get; private set; }

    public DateTimeOffset? LeaseExpiresAtUtc { get; private set; }

    public string? LastErrorCode { get; private set; }

    public string? LastErrorMessage { get; private set; }

    public byte[] RowVersion { get; set; } = [];

    public static OutboxMessage Create(
        Guid id,
        string messageType,
        DateTimeOffset occurredAt,
        Guid? correlationId,
        string payloadJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        if (messageType.Length > 200)
        {
            throw new ArgumentException("Message type exceeds maximum length 200.", nameof(messageType));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            {
                throw new ArgumentException("Payload JSON root must be an object or array.", nameof(payloadJson));
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Payload must be valid JSON.", nameof(payloadJson), exception);
        }

        return new OutboxMessage
        {
            Id = id,
            MessageType = messageType.Trim(),
            OccurredAtUtc = occurredAt.ToUniversalTime(),
            CorrelationId = correlationId,
            PayloadJson = payloadJson,
            DeliveryStatus = OutboxDeliveryStatus.Pending,
            DeliveryAttemptCount = 0,
            NextAttemptAtUtc = occurredAt.ToUniversalTime()
        };
    }

    public void AcquireLease(string workerReference, DateTimeOffset now, TimeSpan leaseDuration, int maxAttempts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerReference);
        if (leaseDuration <= TimeSpan.Zero || maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        if (DeliveryStatus is OutboxDeliveryStatus.Completed or OutboxDeliveryStatus.DeadLetter ||
            DeliveryAttemptCount >= maxAttempts ||
            (LeaseExpiresAtUtc is not null && LeaseExpiresAtUtc > now.ToUniversalTime()))
        {
            throw new InvalidOperationException("Outbox message is not available for leasing.");
        }

        DeliveryStatus = OutboxDeliveryStatus.Leased;
        DeliveryAttemptCount++;
        LeaseOwner = workerReference.Trim();
        LeaseExpiresAtUtc = now.ToUniversalTime().Add(leaseDuration);
        NextAttemptAtUtc = now.ToUniversalTime();
    }

    public void CompleteLease(string workerReference)
    {
        EnsureLeaseOwner(workerReference);
        DeliveryStatus = OutboxDeliveryStatus.Completed;
        LeaseOwner = null;
        LeaseExpiresAtUtc = null;
        LastErrorCode = null;
        LastErrorMessage = null;
    }

    public void ScheduleRetry(
        string workerReference,
        DateTimeOffset nextAttemptAtUtc,
        string errorCode,
        string errorMessage,
        int maxAttempts)
    {
        EnsureLeaseOwner(workerReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        }

        LastErrorCode = NormalizeError(errorCode, nameof(errorCode), 80);
        LastErrorMessage = NormalizeError(errorMessage, nameof(errorMessage), 4000);
        LeaseOwner = null;
        LeaseExpiresAtUtc = null;
        if (DeliveryAttemptCount >= maxAttempts)
        {
            DeliveryStatus = OutboxDeliveryStatus.DeadLetter;
            return;
        }

        DeliveryStatus = OutboxDeliveryStatus.Pending;
        NextAttemptAtUtc = nextAttemptAtUtc.ToUniversalTime();
    }

    private void EnsureLeaseOwner(string workerReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerReference);
        if (DeliveryStatus != OutboxDeliveryStatus.Leased ||
            !string.Equals(LeaseOwner, workerReference.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Outbox lease owner is stale or invalid.");
        }
    }

    private static string NormalizeError(string value, string parameterName, int maxLength)
    {
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maxLength}.", parameterName);
        }

        return normalized;
    }
}
