namespace RoadGuardSystem.BusinessObjects.Messaging;

public sealed class ConsumerEffectReceipt
{
    private ConsumerEffectReceipt()
    {
    }

    public Guid Id { get; private set; }

    public Guid MessageId { get; private set; }

    public string ConsumerName { get; private set; } = string.Empty;

    public Guid EffectId { get; private set; }

    public DateTimeOffset ProcessedAtUtc { get; private set; }

    public static ConsumerEffectReceipt Create(
        Guid messageId,
        string consumerName,
        Guid effectId,
        DateTimeOffset processedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);
        if (consumerName.Length > 100)
        {
            throw new ArgumentException("Consumer name exceeds maximum length 100.", nameof(consumerName));
        }

        return new ConsumerEffectReceipt
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            ConsumerName = consumerName.Trim(),
            EffectId = effectId,
            ProcessedAtUtc = processedAt.ToUniversalTime()
        };
    }
}
