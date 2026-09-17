namespace RoadGuardSystem.Repositories.Seeding;

/// <summary>
/// Root orchestrator contract for database seeding.
/// </summary>
public interface IDatabaseSeeder
{
    /// <summary>
    /// Collection of registered seed steps.
    /// </summary>
    IReadOnlyList<ISeedStep> Steps { get; }

    /// <summary>
    /// Verifies database readiness fail-fast and executes registered seed steps in order.
    /// </summary>
    Task<SeedResult> SeedAsync(RoadGuardDbContext context, CancellationToken cancellationToken = default);
}
