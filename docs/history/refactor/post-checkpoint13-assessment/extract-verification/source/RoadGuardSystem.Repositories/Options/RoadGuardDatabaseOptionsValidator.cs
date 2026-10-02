using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace RoadGuardSystem.Repositories.Options;

/// <summary>
/// Validates RoadGuardDatabaseOptions to ensure fast failure on missing or malformed configuration.
/// Enforces security rules such as prohibiting TrustServerCertificate=true, Encrypt=false, and
/// EnableSensitiveDataLogging=true in production.
/// </summary>
public sealed class RoadGuardDatabaseOptionsValidator : IValidateOptions<RoadGuardDatabaseOptions>
{
    private readonly bool _isProduction;

    public RoadGuardDatabaseOptionsValidator(bool isProduction = true)
    {
        _isProduction = isProduction;
    }

    public ValidateOptionsResult Validate(string? name, RoadGuardDatabaseOptions options)
    {
        return Validate(options, _isProduction);
    }

    public static ValidateOptionsResult Validate(RoadGuardDatabaseOptions options, bool isProduction)
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

        if (isProduction)
        {
            if (builder.TrustServerCertificate)
            {
                return ValidateOptionsResult.Fail(
                    "TrustServerCertificate=true is strictly forbidden in production database configuration. " +
                    "Production SQL Server connections must use a trusted certificate.");
            }

            if (!builder.Encrypt)
            {
                return ValidateOptionsResult.Fail(
                    "Encrypt=false is strictly forbidden in production database configuration. " +
                    "Production SQL Server connections must enforce encrypted transport.");
            }

            if (options.EnableSensitiveDataLogging)
            {
                return ValidateOptionsResult.Fail(
                    "EnableSensitiveDataLogging=true is strictly forbidden in production configuration to protect PII.");
            }
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
    public static void ValidateOrThrow(RoadGuardDatabaseOptions options, bool isProduction = true)
    {
        var result = Validate(options, isProduction);
        if (result.Failed)
        {
            throw new ArgumentException(
                $"Database configuration validation failed: {result.FailureMessage}",
                nameof(options));
        }
    }
}
