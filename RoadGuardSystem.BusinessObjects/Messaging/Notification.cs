namespace RoadGuardSystem.BusinessObjects.Messaging;

public sealed class Notification
{
    private static readonly string[] SensitiveMarkers =
    [
        "password=",
        "token=",
        "secret=",
        "authorization:",
        "cookie=",
        "connectionstring="
    ];

    private Notification()
    {
    }

    public Guid Id { get; private set; }

    public Guid RecipientUserId { get; private set; }

    public string SourceEntityType { get; private set; } = string.Empty;

    public Guid SourceEntityId { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public DateTimeOffset? ReadAt { get; private set; }

    public static Notification Create(
        Guid id,
        Guid recipientUserId,
        string sourceEntityType,
        Guid sourceEntityId,
        string eventType,
        string title,
        string body)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Notification id must not be empty.", nameof(id));
        }

        if (recipientUserId == Guid.Empty)
        {
            throw new ArgumentException("Recipient user id must not be empty.", nameof(recipientUserId));
        }

        if (sourceEntityId == Guid.Empty)
        {
            throw new ArgumentException("Source entity id must not be empty.", nameof(sourceEntityId));
        }

        return new Notification
        {
            Id = id,
            RecipientUserId = recipientUserId,
            SourceEntityType = ValidateRequired(
                sourceEntityType,
                nameof(sourceEntityType),
                80),
            SourceEntityId = sourceEntityId,
            EventType = ValidateRequired(eventType, nameof(eventType), 80),
            Title = ValidateRequired(title, nameof(title), 200),
            Body = ValidateBody(body, nameof(body))
        };
    }

    public void MarkRead(DateTimeOffset readAt)
    {
        if (ReadAt is null)
        {
            ReadAt = readAt.ToUniversalTime();
        }
    }

    private static string ValidateRequired(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maxLength}.", parameterName);
        }

        RejectSensitiveContent(normalized, parameterName);
        return normalized;
    }

    private static string ValidateBody(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        RejectSensitiveContent(normalized, parameterName);
        return normalized;
    }

    private static void RejectSensitiveContent(string value, string parameterName)
    {
        if (SensitiveMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Notification content must not contain credentials or tokens.", parameterName);
        }
    }
}
