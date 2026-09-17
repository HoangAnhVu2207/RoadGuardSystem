namespace RoadGuardSystem.Repositories.Seeding;

/// <summary>
/// Exception thrown when the database readiness probe fails before seeding begins.
/// Guarantees fail-fast behavior without attempting schema modification or partial seeding.
/// </summary>
public class DatabaseNotReadyException : InvalidOperationException
{
    public DatabaseNotReadyException(string message) : base(message)
    {
    }

    public DatabaseNotReadyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
