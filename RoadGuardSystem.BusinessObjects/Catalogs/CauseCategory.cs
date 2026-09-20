namespace RoadGuardSystem.BusinessObjects.Catalogs;

public sealed class CauseCategory
{
    private CauseCategory()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static CauseCategory Create(
        string code,
        string name,
        bool isActive = true)
    {
        return new CauseCategory
        {
            Code = ValidateRequired(code, nameof(code), 80),
            Name = ValidateRequired(name, nameof(name), 150),
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
