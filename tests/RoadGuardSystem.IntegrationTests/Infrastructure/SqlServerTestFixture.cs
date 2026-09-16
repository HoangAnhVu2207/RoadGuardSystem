using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

/// <summary>
/// Manages isolated SQL Server test databases for integration tests.
/// Supports runtime connection via:
/// 1. ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING environment variable (strictly enforced without fallback if set)
/// 2. Detected local SQL Server instance (e.g. MSSQL$HANHNAV)
/// 3. Testcontainers MsSql container (when Docker daemon is active)
/// If no SQL Server instance can be reached, throws SqlTestEnvironmentUnavailableException (no false-green).
/// Guarantees database cleanup in DisposeAsync without swallowing teardown failures.
/// </summary>
public sealed class SqlServerTestFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;
    private string? _masterConnectionString;
    private string? _databaseName;
    private string? _databaseConnectionString;

    public string DatabaseName => _databaseName ?? throw new InvalidOperationException("Fixture has not been initialized.");
    public string ConnectionString => _databaseConnectionString ?? throw new InvalidOperationException("Fixture has not been initialized.");
    public string MasterConnectionString => _masterConnectionString ?? throw new InvalidOperationException("Fixture has not been initialized.");

    public void CorruptMasterConnectionStringForTesting(string corruptedConn)
    {
        _masterConnectionString = corruptedConn;
    }

    public async Task InitializeAsync()
    {
        _masterConnectionString = await ResolveMasterConnectionStringAsync();
        _databaseName = $"RoadGuard_Test_{Guid.NewGuid():N}";

        // Build connection string for the isolated test database
        var dbBuilder = new SqlConnectionStringBuilder(_masterConnectionString)
        {
            InitialCatalog = _databaseName,
            // Test fixture connects directly to local or container test instance
            TrustServerCertificate = true
        };
        _databaseConnectionString = dbBuilder.ConnectionString;

        // Create the isolated test database
        await using (var masterConn = new SqlConnection(_masterConnectionString))
        {
            await masterConn.OpenAsync();
            await using var cmd = masterConn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE [{_databaseName}];";
            await cmd.ExecuteNonQueryAsync();
        }

        // Initialize schema with SpatialProbeDbContext
        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    public SpatialProbeDbContext CreateDbContext()
    {
        if (_databaseConnectionString is null)
            throw new InvalidOperationException("Fixture has not been initialized.");

        var options = new DbContextOptionsBuilder<SpatialProbeDbContext>()
            .UseSqlServer(_databaseConnectionString, x => x.UseNetTopologySuite())
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .Options;

        return new SpatialProbeDbContext(options);
    }

    public async Task DisposeAsync()
    {
        Exception? dbCleanupException = null;

        if (_masterConnectionString is not null && _databaseName is not null)
        {
            try
            {
                await using var masterConn = new SqlConnection(_masterConnectionString);
                await masterConn.OpenAsync();
                await using var cmd = masterConn.CreateCommand();
                cmd.CommandText = $@"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{_databaseName}')
BEGIN
    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{_databaseName}];
END";
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                dbCleanupException = ex;
            }
        }

        // Guarantee container cleanup runs even if database cleanup threw an exception
        if (_container is not null)
        {
            try
            {
                await _container.DisposeAsync();
            }
            catch (Exception containerEx)
            {
                if (dbCleanupException is not null)
                {
                    throw new AggregateException(
                        "Both database cleanup and container disposal failed during test fixture teardown.",
                        dbCleanupException,
                        containerEx);
                }
                throw;
            }
        }

        // Re-throw database drop errors so cleanup failures are observed by test runner (no false-green)
        if (dbCleanupException is not null)
        {
            throw new InvalidOperationException(
                $"Failed to drop isolated test database '{_databaseName}': {dbCleanupException.Message}",
                dbCleanupException);
        }
    }

    private async Task<string> ResolveMasterConnectionStringAsync()
    {
        // 1. Check environment variable: if configured, must succeed without fallback
        var envConn = Environment.GetEnvironmentVariable("ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(envConn))
        {
            SqlConnectionStringBuilder builder;
            try
            {
                builder = new SqlConnectionStringBuilder(envConn) { InitialCatalog = "master" };
            }
            catch (Exception ex)
            {
                throw new SqlTestEnvironmentUnavailableException(
                    $"ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING was specified but is malformed: {ex.Message}. " +
                    "Refusing fallback to local SQL or Docker to maintain deterministic test configuration.", ex);
            }

            if (!await CanConnectAsync(builder.ConnectionString))
            {
                throw new SqlTestEnvironmentUnavailableException(
                    "ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING was specified but could not connect to SQL Server. " +
                    "Refusing fallback to local SQL Server or Docker to prevent unintended test execution context.");
            }

            return builder.ConnectionString;
        }

        // 2. Check local running SQL Server instances (only when env var is not set)
        var localCandidates = new[]
        {
            @"Server=.\HANHNAV;Database=master;Integrated Security=True;TrustServerCertificate=True",
            @"Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True",
            @"Server=(localdb)\mssqllocaldb;Database=master;Integrated Security=True;TrustServerCertificate=True"
        };

        foreach (var candidate in localCandidates)
        {
            if (await CanConnectAsync(candidate))
            {
                return candidate;
            }
        }

        // 3. Check Testcontainers if Docker is available
        try
        {
            var container = new MsSqlBuilder().Build();
            await container.StartAsync();
            _container = container;
            return container.GetConnectionString();
        }
        catch (Exception ex)
        {
            throw new SqlTestEnvironmentUnavailableException(
                "SQL Server integration test environment is unavailable. Neither Docker/Testcontainers nor an accessible " +
                "local SQL Server instance could be reached. Set ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING or ensure " +
                "SQL Server / Docker is running before executing integration tests.", ex);
        }
    }

    public static async Task<bool> CanConnectAsync(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = 2
            };
            await using var conn = new SqlConnection(builder.ConnectionString);
            await conn.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
