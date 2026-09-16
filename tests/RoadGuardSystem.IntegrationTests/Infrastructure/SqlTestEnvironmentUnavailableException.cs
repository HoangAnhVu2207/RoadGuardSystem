namespace RoadGuardSystem.IntegrationTests.Infrastructure;

/// <summary>
/// Thrown when no SQL Server environment (Docker/Testcontainers or explicit SQL Server instance)
/// is reachable, preventing silent false-green execution.
/// </summary>
public sealed class SqlTestEnvironmentUnavailableException : Exception
{
    public SqlTestEnvironmentUnavailableException(string message) : base(message)
    {
    }

    public SqlTestEnvironmentUnavailableException(string message, Exception inner) : base(message, inner)
    {
    }
}
