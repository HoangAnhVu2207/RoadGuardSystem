namespace RoadGuardSystem.Repositories.Options;

/// <summary>
/// Configuration options for RoadGuard SQL Server database persistence.
/// Bound strictly from configuration section "RoadGuardDatabase".
/// Note: Production security mode is controlled by host environment/composition root, not bound from configuration.
/// </summary>
public sealed class RoadGuardDatabaseOptions
{
    public const string SectionName = "RoadGuardDatabase";

    /// <summary>
    /// The ADO.NET / EF Core connection string for SQL Server.
    /// Must be non-empty and well-formed.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Default command timeout in seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of transient failure retries.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Enables EF Core detailed errors.
    /// </summary>
    public bool EnableDetailedErrors { get; set; }

    /// <summary>
    /// Enables sensitive data logging in dev/test only.
    /// Strictly prohibited in production.
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; }
}
