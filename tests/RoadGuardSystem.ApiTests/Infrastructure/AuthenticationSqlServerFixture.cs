using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Seeding;
using Testcontainers.MsSql;
using Xunit;

namespace RoadGuardSystem.ApiTests.Infrastructure;

public sealed class AuthenticationSqlServerFixture : IAsyncLifetime
{
    private static readonly ConcurrentDictionary<string, byte> OwnedConnections = new(StringComparer.Ordinal);
    private MsSqlContainer? _container;
    private string? _masterConnectionString;
    private string? _databaseName;

    public string ConnectionString { get; private set; } = string.Empty;

    private readonly Guid _seedOwnerId = Guid.NewGuid();
    public (Guid OwnerId, string ContainerId, string DatabaseName) GetOwnedSeedDatabaseIdentity()
    {
        EnsureOwnedDatabase();
        if (!OwnedConnections.ContainsKey(ConnectionString)) throw new InvalidOperationException("Inactive seed fixture.");
        return (_seedOwnerId, _container!.Id, _databaseName!);
    }

    public Task InitializeAsync() => InitializeAtMigrationAsync(null);

    internal async Task InitializeAtMigrationAsync(string? migration)
    {
        try
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04").Build();
            await _container.StartAsync();
            _masterConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
            {
                InitialCatalog = "master"
            }.ConnectionString;

            _databaseName = $"RoadGuard_ApiTest_{Guid.NewGuid():N}";
            ConnectionString = new SqlConnectionStringBuilder(_masterConnectionString)
            {
                InitialCatalog = _databaseName,
                TrustServerCertificate = true
            }.ConnectionString;

            await using (var connection = new SqlConnection(_masterConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE [{_databaseName}]";
                await command.ExecuteNonQueryAsync();
            }

            await using var context = CreateDbContext();
            await context.GetService<IMigrator>().MigrateAsync(migration);
            await new IdentityRoleSeedStep().SeedAsync(context, CancellationToken.None);
            OwnedConnections.TryAdd(ConnectionString, 0);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public RoadGuardDbContext CreateDbContext()
    {
        EnsureOwnedDatabase();
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.UseNetTopologySuite())
            .Options;
        return new RoadGuardDbContext(options);
    }

    public async Task<ApplicationUser> CreateUserAsync(
        string username,
        string password,
        UserRoleCode role = UserRoleCode.DroneOperator,
        UserStatus status = UserStatus.Active,
        bool mustChangePassword = false)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            DisplayName = username,
            Email = EmailFor(username),
            NormalizedEmail = EmailFor(username).ToUpperInvariant(),
            RoleCode = role,
            Status = status,
            MustChangePassword = mustChangePassword,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = new PasswordHasher<ApplicationUser>(Options.Create(
            new PasswordHasherOptions { IterationCount = 10_000 })).HashPassword(user, password);

        await using var context = CreateDbContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public static string EmailFor(string username) =>
        username.Contains('@', StringComparison.Ordinal)
            ? username
            : $"{username}@example.test";

    internal static bool IsOwnedConnectionString(string connectionString) =>
        OwnedConnections.ContainsKey(connectionString);

    internal async Task<string> ReadDeadlockDiagnosticsAsync()
    {
        EnsureOwnedDatabase();
        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DECLARE @path nvarchar(4000) = (
                SELECT CAST(t.target_data AS xml).value('(EventFileTarget/File/@name)[1]', 'nvarchar(4000)')
                FROM sys.dm_xe_session_targets t JOIN sys.dm_xe_sessions s ON s.address=t.event_session_address
                WHERE s.name='system_health' AND t.target_name='event_file');
            SELECT TOP (5) event_data FROM sys.fn_xe_file_target_read_file(@path, NULL, NULL, NULL)
            WHERE object_name='xml_deadlock_report' ORDER BY file_offset DESC;
            """;
        var events = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) events.Add(reader.GetString(0));
        return events.Count == 0 ? "No retained system_health deadlock events." : string.Join(Environment.NewLine, events);
    }

    public async Task DisposeAsync()
    {
        OwnedConnections.TryRemove(ConnectionString, out _);
        try
        {
            if (_container is not null && _masterConnectionString is not null && _databaseName is not null)
            {
                await using var connection = new SqlConnection(_masterConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"IF DB_ID('{_databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]; END";
                await command.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            if (_container is not null)
            {
                await _container.DisposeAsync();
                _container = null;
            }
        }
    }

    private void EnsureOwnedDatabase()
    {
        if (_container is null || _masterConnectionString is null || _databaseName is null ||
            !string.Equals(new SqlConnectionStringBuilder(ConnectionString).InitialCatalog,
                _databaseName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("API test database is not owned by this fixture.");
        }
    }
}
