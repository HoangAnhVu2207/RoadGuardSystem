namespace RoadGuardSystem.Repositories.Seeding;

/// <summary>
/// Result summary of a database seeding execution.
/// </summary>
public sealed record SeedResult(
    bool Success,
    int StepsExecuted,
    IReadOnlyList<string> ExecutedStepNames,
    TimeSpan Elapsed
);
