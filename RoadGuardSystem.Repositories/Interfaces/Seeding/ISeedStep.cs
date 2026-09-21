namespace RoadGuardSystem.Repositories.Seeding;

/// <summary>
/// Represents an ordered database seeding step.
/// CONTRACT INVARIANT: Idempotency is a mandatory contract requirement of each individual <see cref="ISeedStep"/> implementation.
/// The orchestrator (<see cref="IDatabaseSeeder"/>) delegates to registered steps in sequential order and does not automatically
/// enforce idempotency; each step implementation must ensure that repeated executions produce identical state without duplicate records.
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
