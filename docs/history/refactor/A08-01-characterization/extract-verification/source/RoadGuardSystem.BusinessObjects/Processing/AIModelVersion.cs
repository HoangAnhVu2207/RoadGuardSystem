using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Processing;

public sealed class AIModelVersion
{
    private AIModelVersion()
    {
    }

    public Guid Id { get; private set; }
    public string ModelName { get; private set; } = string.Empty;
    public string VersionLabel { get; private set; } = string.Empty;
    public string ArtifactUri { get; private set; } = string.Empty;
    public string? Metrics { get; private set; }
    public string? OperatingThresholds { get; private set; }
    public AIModelVersionStatus Status { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    public Guid? ReleasedByUserId { get; private set; }

    public static AIModelVersion Create(
        Guid id,
        string modelName,
        string versionLabel,
        string artifactUri,
        string? metrics,
        string? operatingThresholds,
        AIModelVersionStatus status,
        DateTimeOffset? releasedAt,
        Guid? releasedByUserId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("AI model version id must not be empty.", nameof(id));
        }

        if (!Enum.IsDefined(status) || status == AIModelVersionStatus.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(status), "AI model version status must be specified.");
        }

        var normalizedReleasedAt = releasedAt?.ToUniversalTime();
        if (normalizedReleasedAt.HasValue != releasedByUserId.HasValue)
        {
            throw new ArgumentException("Release timestamp and releasing user must be supplied together.");
        }

        return new AIModelVersion
        {
            Id = id,
            ModelName = Required(modelName, nameof(modelName), 120),
            VersionLabel = Required(versionLabel, nameof(versionLabel), 80),
            ArtifactUri = Required(artifactUri, nameof(artifactUri), 2048),
            Metrics = ValidateOptionalJson(metrics, nameof(metrics)),
            OperatingThresholds = ValidateOptionalJson(operatingThresholds, nameof(operatingThresholds)),
            Status = status,
            ReleasedAt = normalizedReleasedAt,
            ReleasedByUserId = releasedByUserId
        };
    }

    private static string Required(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maxLength}.", parameterName);
        }

        return normalized;
    }

    private static string? ValidateOptionalJson(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            {
                throw new ArgumentException("Value must be a JSON object or array.", parameterName);
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Value must be valid JSON.", parameterName, exception);
        }

        return value.Trim();
    }
}
