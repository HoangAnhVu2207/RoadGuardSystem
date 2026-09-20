namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class RoadSection
{
    private RoadSection()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string? Name { get; private set; }

    public static RoadSection Create(Guid id, Guid projectId, string code, string? name = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Road section id must not be empty.", nameof(id));
        }

        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id must not be empty.", nameof(projectId));
        }

        return new RoadSection
        {
            Id = id,
            ProjectId = projectId,
            Code = ValidateRequired(code, nameof(code), 80),
            Name = NormalizeOptional(name, nameof(name), 255)
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

    private static string? NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maxLength}.", parameterName);
        }

        return normalized;
    }
}
