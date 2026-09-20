namespace RoadGuardSystem.BusinessObjects.Messaging;

public static class NotificationContentValidator
{
    private static readonly string[] SensitiveMarkers =
    {
        "password=",
        "token=",
        "secret=",
        "authorization:",
        "cookie=",
        "connectionstring="
    };

    public static string ValidateRequired(string value, string parameterName, int maxLength)
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

    public static string ValidateBody(string value, string parameterName)
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
