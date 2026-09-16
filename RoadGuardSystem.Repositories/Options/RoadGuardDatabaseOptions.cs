namespace RoadGuardSystem.Repositories.Options;

/// <summary>
/// Configuration options for RoadGuard SQL Server database persistence.
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
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; }

    /// <summary>
    /// Indicates if the application is running in Production mode.
    /// In production, TrustServerCertificate=true is strictly forbidden.
    /// </summary>
    public bool IsProduction { get; set; } = true;
}
