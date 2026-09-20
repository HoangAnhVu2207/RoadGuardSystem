namespace RoadGuardSystem.BusinessObjects.Catalogs;

public sealed class DefectType
{
    private DefectType()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public static DefectType Create(
        string code,
        string name,
        string? description = null,
        bool isActive = true)
    {
        return new DefectType
        {
            Code = ValidateRequired(code, nameof(code), 80),
            Name = ValidateRequired(name, nameof(name), 150),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsActive = isActive
        };
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
