using System.Text.Json;

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
            PayloadJson = payloadJson
        };
    }
}
