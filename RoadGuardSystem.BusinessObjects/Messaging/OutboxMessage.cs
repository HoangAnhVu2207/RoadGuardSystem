using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Auditing;

namespace RoadGuardSystem.BusinessObjects.Messaging;

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
        string sanitizedPayloadJson;
        try
        {
            sanitizedPayloadJson = SensitiveJsonSanitizer.Redact(payloadJson);
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
            PayloadJson = sanitizedPayloadJson
        };
    }
}
