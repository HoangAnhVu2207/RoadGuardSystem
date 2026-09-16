using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace RoadGuardSystem.Repositories.Options;

/// <summary>
/// Validates RoadGuardDatabaseOptions to ensure fast failure on missing or malformed configuration.
/// Enforces security rules such as prohibiting TrustServerCertificate=true in production.
/// </summary>
public sealed class RoadGuardDatabaseOptionsValidator : IValidateOptions<RoadGuardDatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, RoadGuardDatabaseOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("Database options instance cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("ConnectionString is required and cannot be empty or whitespace.");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(options.ConnectionString);
        }
        catch (ArgumentException ex)
        {
            return ValidateOptionsResult.Fail($"ConnectionString is malformed: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            return ValidateOptionsResult.Fail("ConnectionString must specify a valid Data Source / Server.");
        }

        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            return ValidateOptionsResult.Fail("ConnectionString must specify an Initial Catalog / Database name.");
        }

        if (options.IsProduction && builder.TrustServerCertificate)
        {
            return ValidateOptionsResult.Fail(
                "TrustServerCertificate=true is strictly forbidden in production database configuration. " +
                "Production SQL Server connections must use a trusted certificate.");
        }

        if (options.CommandTimeoutSeconds <= 0)
        {
            return ValidateOptionsResult.Fail("CommandTimeoutSeconds must be greater than zero.");
        }

        if (options.MaxRetryCount < 0)
        {
            return ValidateOptionsResult.Fail("MaxRetryCount cannot be negative.");
        }

        return ValidateOptionsResult.Success;
    }

    /// <summary>
    /// Validates database options and throws an ArgumentException immediately if invalid.
    /// Used for fail-fast startup behavior.
    /// </summary>
    public static void ValidateOrThrow(RoadGuardDatabaseOptions options)
    {
        var validator = new RoadGuardDatabaseOptionsValidator();
        var result = validator.Validate(null, options);
        if (result.Failed)
        {
            throw new ArgumentException(
                $"Database configuration validation failed: {result.FailureMessage}",
                nameof(options));
        }
    }
}
