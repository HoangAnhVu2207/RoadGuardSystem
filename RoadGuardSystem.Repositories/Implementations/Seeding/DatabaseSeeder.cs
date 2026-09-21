using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace RoadGuardSystem.Repositories.Seeding;

/// <summary>
/// Deterministic database seeder orchestrator.
/// Enforces fail-fast database readiness check before executing registered seed steps in sequence.
/// Does not create or modify schema; requires schema to be managed by migrations/runtime.
/// Note: The seeder executes registered steps in order; idempotency must be guaranteed by each registered <see cref="ISeedStep"/>.
/// </summary>
public sealed class DatabaseSeeder : IDatabaseSeeder
{
    private readonly List<ISeedStep> _steps;

    public DatabaseSeeder(IEnumerable<ISeedStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        _steps = steps.OrderBy(s => s.Order).ToList();
    }

    public IReadOnlyList<ISeedStep> Steps => _steps.AsReadOnly();

    public async Task<SeedResult> SeedAsync(RoadGuardDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Fail-fast readiness check: ensure database is reachable without mutating schema
        bool canConnect;
        try
        {
            canConnect = await context.Database.CanConnectAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            throw new DatabaseNotReadyException(
                "Database connection failed during readiness probe. Seeder aborted fail-fast without altering schema.", ex);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!canConnect)
        {
            throw new DatabaseNotReadyException(
                "Database is unreachable or reported not ready during readiness probe. Seeder aborted fail-fast without altering schema.");
        }

        // 2. Execute registered seed steps in strict numerical order
        var executedStepNames = new List<string>(_steps.Count);
        var stopwatch = Stopwatch.StartNew();

        foreach (var step in _steps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await step.SeedAsync(context, cancellationToken);
            executedStepNames.Add(step.Name);
        }

        stopwatch.Stop();

        return new SeedResult(
            Success: true,
            StepsExecuted: executedStepNames.Count,
            ExecutedStepNames: executedStepNames.AsReadOnly(),
            Elapsed: stopwatch.Elapsed
        );
    }
}
