using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Seeding;
using Testcontainers.MsSql;
using Xunit;

namespace RoadGuardSystem.ApiTests.Infrastructure;

public sealed class AuthenticationSqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;
    private string? _masterConnectionString;
    private string? _databaseName;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING");
        if (configured is null)
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04").Build();
            await _container.StartAsync();
            _masterConnectionString = _container.GetConnectionString();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(configured))
            {
                throw new InvalidOperationException(
                    "ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING is configured but empty; refusing fallback.");
            }

            _masterConnectionString = new SqlConnectionStringBuilder(configured)
            {
                InitialCatalog = "master"
            }.ConnectionString;
        }

        _databaseName = $"RoadGuard_ApiTest_{Guid.NewGuid():N}";
        var databaseBuilder = new SqlConnectionStringBuilder(_masterConnectionString)
        {
            InitialCatalog = _databaseName,
            TrustServerCertificate = true
        };
        ConnectionString = databaseBuilder.ConnectionString;

        await using (var connection = new SqlConnection(_masterConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{_databaseName}]";
            await command.ExecuteNonQueryAsync();
        }

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
        await new IdentityRoleSeedStep().SeedAsync(context, CancellationToken.None);
    }

    public RoadGuardDbContext CreateDbContext()
    {
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
            RoleCode = role,
            Status = status,
            MustChangePassword = mustChangePassword,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, password);

        await using var context = CreateDbContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (_masterConnectionString is not null && _databaseName is not null)
            {
                await using var connection = new SqlConnection(_masterConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]";
                await command.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            if (_container is not null)
            {
                await _container.DisposeAsync();
            }
        }
    }
}
