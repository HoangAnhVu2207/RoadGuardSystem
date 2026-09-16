using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

/// <summary>
/// Delegate for retrieving environment variables, enabling deterministic testing without process-wide mutation.
/// </summary>
public delegate string? EnvironmentVariableAccessor(string variableName);

/// <summary>
/// Manages isolated SQL Server test databases for integration tests.
/// Supports connection resolution via:
/// 1. ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING (strict: fail-fast without fallback if empty, whitespace, malformed, or unreachable)
/// 2. Testcontainers MsSql container when the environment variable is unset (no hardcoded instance names).
/// Provides reliable lifecycle management and unswallowed teardown failures.
/// </summary>
public sealed class SqlServerTestFixture : IAsyncLifetime
{
    private readonly EnvironmentVariableAccessor _environmentAccessor;
    private MsSqlContainer? _container;
    private bool _ownsContainer;
    private string? _masterConnectionString;
    private string? _databaseName;
    private string? _databaseConnectionString;
    private bool _databaseDropped;

    public string DatabaseName => _databaseName ?? throw new InvalidOperationException("Fixture has not been initialized.");
    public string ConnectionString => _databaseConnectionString ?? throw new InvalidOperationException("Fixture has not been initialized.");
    public string MasterConnectionString => _masterConnectionString ?? throw new InvalidOperationException("Fixture has not been initialized.");

    /// <summary>
    /// For testing teardown error observation without corrupting connections.
    /// </summary>
    public bool SimulateDropFailure { get; set; }

    public SqlServerTestFixture() : this(null, null)
    {
    }

    internal SqlServerTestFixture(EnvironmentVariableAccessor? environmentAccessor = null, string? masterConnectionString = null)
    {
        _environmentAccessor = environmentAccessor ?? Environment.GetEnvironmentVariable;
        _masterConnectionString = masterConnectionString;
    }

    public static SqlServerTestFixture CreateWithEnvironmentAccessor(EnvironmentVariableAccessor accessor)
        => new(accessor);

    public static SqlServerTestFixture CreateWithMasterConnectionString(string masterConnectionString)
        => new(null, masterConnectionString);

    public async Task InitializeAsync()
    {
        _masterConnectionString ??= await ResolveMasterConnectionStringAsync();
        _databaseName = $"RoadGuard_Test_{Guid.NewGuid():N}";

        // Build connection string for the isolated test database
        var dbBuilder = new SqlConnectionStringBuilder(_masterConnectionString)
        {
            InitialCatalog = _databaseName,
            TrustServerCertificate = true
        };
        _databaseConnectionString = dbBuilder.ConnectionString;

        // Create the isolated test database
        await CreateDatabaseAsync(_databaseName);

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

    public async Task CreateDatabaseAsync(string databaseName)
    {
        await using var masterConn = new SqlConnection(MasterConnectionString);
        await masterConn.OpenAsync();
        await using var cmd = masterConn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE [{databaseName}];";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DropDatabaseAsync()
    {
        if (SimulateDropFailure)
        {
            throw new InvalidOperationException("Simulated database drop failure for teardown testing.");
        }

        if (_databaseName is not null)
        {
            await DropDatabaseByNameAsync(_databaseName);
            _databaseDropped = true;
        }
    }

    public async Task DropDatabaseByNameAsync(string databaseName)
    {
        await using var masterConn = new SqlConnection(MasterConnectionString);
        await masterConn.OpenAsync();
        await using var cmd = masterConn.CreateCommand();
        cmd.CommandText = $@"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{databaseName}')
BEGIN
    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{databaseName}];
END";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> DatabaseExistsAsync(string databaseName)
    {
        await using var masterConn = new SqlConnection(MasterConnectionString);
        await masterConn.OpenAsync();
        await using var cmd = masterConn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(1) FROM sys.databases WHERE name = '{databaseName}';";
        var count = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }

    public async Task DisposeAsync()
    {
        Exception? dbCleanupException = null;

        try
        {
            if (!_databaseDropped && _masterConnectionString is not null && _databaseName is not null)
            {
                try
                {
                    await DropDatabaseAsync();
                }
                catch (Exception ex)
                {
                    dbCleanupException = ex;
                }
            }
        }
        finally
        {
            // Guarantee container cleanup runs in finally even if unexpected exceptions occurred
            if (_ownsContainer && _container is not null)
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
        var envConn = _environmentAccessor("ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING");
        if (envConn is not null)
        {
            if (string.IsNullOrWhiteSpace(envConn))
            {
                throw new SqlTestEnvironmentUnavailableException(
                    "ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING was provided but is empty or whitespace. " +
                    "Refusing fallback to maintain deterministic test configuration.");
            }

            SqlConnectionStringBuilder builder;
            try
            {
                builder = new SqlConnectionStringBuilder(envConn) { InitialCatalog = "master" };
            }
            catch (Exception ex)
            {
                throw new SqlTestEnvironmentUnavailableException(
                    $"ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING was specified but is malformed: {ex.Message}. " +
                    "Refusing fallback to maintain deterministic test configuration.", ex);
            }

            if (!await CanConnectAsync(builder.ConnectionString))
            {
                throw new SqlTestEnvironmentUnavailableException(
                    "ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING was specified but could not connect to SQL Server. " +
                    "Refusing fallback to local SQL Server or Docker to prevent unintended test execution context.");
            }

            return builder.ConnectionString;
        }

        // 2. If environment variable is unset: use Testcontainers directly (no hardcoded instance strings)
        try
        {
            var container = new MsSqlBuilder().Build();
            await container.StartAsync();
            _container = container;
            _ownsContainer = true;
            return container.GetConnectionString();
        }
        catch (Exception ex)
        {
            throw new SqlTestEnvironmentUnavailableException(
                "SQL Server integration test environment is unavailable. ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING is unset, " +
                "and Testcontainers could not start a SQL Server container (ensure Docker daemon is running).", ex);
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
