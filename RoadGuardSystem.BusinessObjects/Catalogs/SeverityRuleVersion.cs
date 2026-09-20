using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Catalogs;

public sealed class SeverityRuleVersion
{
    private SeverityRuleVersion()
    {
    }

    public Guid Id { get; private set; }

    public string StandardCode { get; private set; } = string.Empty;

    public string RoadTypeCode { get; private set; } = string.Empty;

    public int VersionNo { get; private set; }

    public string RuleDefinition { get; private set; } = string.Empty;

    public DateOnly EffectiveFrom { get; private set; }

    public DateOnly? EffectiveTo { get; private set; }

    public static SeverityRuleVersion Create(
        Guid id,
        string standardCode,
        string roadTypeCode,
        int versionNo,
        string ruleDefinition,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Severity rule version id must not be empty.", nameof(id));
        }

        if (versionNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(versionNo), "Version number must be positive.");
        }

        if (effectiveTo < effectiveFrom)
        {
            throw new ArgumentException("EffectiveTo must not be before EffectiveFrom.", nameof(effectiveTo));
        }

        var normalizedDefinition = ValidateJsonObject(ruleDefinition);
        return new SeverityRuleVersion
        {
            Id = id,
            StandardCode = ValidateRequired(standardCode, nameof(standardCode), 80),
            RoadTypeCode = ValidateRequired(roadTypeCode, nameof(roadTypeCode), 80),
            VersionNo = versionNo,
            RuleDefinition = normalizedDefinition,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo
        };
    }

    private static string ValidateJsonObject(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("RuleDefinition must be a JSON object.", nameof(value));
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("RuleDefinition must be valid JSON.", nameof(value), exception);
        }

        return value.Trim();
    }

    private static string ValidateRequired(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maxLength}.", parameterName);
        }

        return normalized;
    }
}
