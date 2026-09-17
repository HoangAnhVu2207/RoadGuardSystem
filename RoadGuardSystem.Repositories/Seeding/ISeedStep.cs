namespace RoadGuardSystem.Repositories.Seeding;

/// <summary>
/// Represents an ordered, idempotent database seeding step.
/// </summary>
public interface ISeedStep
{
    /// <summary>
    /// Execution order of the seed step (lower numbers execute first).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Descriptive name of the seed step for logging and audit.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the seed logic idempotently.
    /// </summary>
    Task SeedAsync(RoadGuardDbContext context, CancellationToken cancellationToken = default);
}
