using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class Project
{
    public Guid Id { get; set; }

    public string ProjectCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? EngineeringUtmSrid { get; set; }

    public ProjectStatus Status { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public static Project Create(
        Guid id,
        string projectCode,
        string name,
        string? description,
        int? engineeringUtmSrid,
        DateOnly? startDate,
        DateOnly? endDate,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Project id must not be empty.", nameof(id));
        }

        if (endDate < startDate)
        {
            throw new ArgumentException("Project end date must not be before its start date.", nameof(endDate));
        }

        return new Project
        {
            Id = id,
            ProjectCode = NormalizeRequired(projectCode, nameof(projectCode), 50),
            Name = NormalizeRequired(name, nameof(name), 255),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            EngineeringUtmSrid = engineeringUtmSrid,
            Status = ProjectStatus.Active,
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = createdAt.ToUniversalTime()
        };
    }

    public void UpdateDetails(
        string name,
        string? description,
        int? engineeringUtmSrid,
        DateOnly? startDate,
        DateOnly? endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("Project end date must not be before its start date.", nameof(endDate));
        }

        Name = NormalizeRequired(name, nameof(name), 255);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        EngineeringUtmSrid = engineeringUtmSrid;
        StartDate = startDate;
        EndDate = endDate;
    }

    private static string NormalizeRequired(string value, string parameterName, int maxLength)
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
