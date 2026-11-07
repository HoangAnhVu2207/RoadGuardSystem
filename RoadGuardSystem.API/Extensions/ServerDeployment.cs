using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RoadGuardSystem.API.Workers;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Seeding;

namespace RoadGuardSystem.API.Extensions;

/// <summary>Opt-in LAN server composition. Normal startup never migrates or seeds.</summary>
public static class ServerDeployment
{
    public const string CorsPolicy = "ServerDeployment";

    public static IServiceCollection AddRoadGuardServerDeployment(this IServiceCollection services,
        IConfiguration configuration, string environmentName)
    {
        if (!configuration.GetValue("ServerDeployment:Enabled", false)) return services;
        var origins = configuration.GetSection("ServerDeployment:AllowedOrigins").Get<string[]>() ?? [];
        var keys = configuration["ServerDeployment:KeyPath"];
        if (environmentName != "Production" || string.IsNullOrWhiteSpace(keys) || !Path.IsPathFullyQualified(keys) ||
            configuration.GetValue("Sandbox:Enabled", false) || configuration.GetValue("Anh02:MockEnabled", false) ||
            configuration.GetValue("Handoff:Enabled", false) || origins.Length == 0 || origins.Any(origin =>
                !Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
                uri.Host.Contains('*', StringComparison.Ordinal) || uri.AbsolutePath != "/" || uri.UserInfo.Length != 0 ||
                uri.Query.Length != 0 || uri.Fragment.Length != 0 || origin.EndsWith('/')))
            throw new InvalidOperationException("Server deployment requires Production, persistent keys, explicit HTTPS origins and no mock/sandbox.");
        services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(origins).AllowAnyMethod().AllowAnyHeader().AllowCredentials()));
        services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keys)).SetApplicationName("RoadGuardSystem.Server");
        services.AddHostedService<UploadVerificationWorker>();
        services.AddSingleton<ServerDependencyHealthCheck>();
        services.AddHealthChecks().AddCheck<ServerDependencyHealthCheck>("server-dependencies");
        return services;
    }

    public static async Task<int?> TryExecuteMaintenanceAsync(WebApplication app)
    {
        var command = app.Configuration["ServerDeployment:Maintenance"];
        if (string.IsNullOrEmpty(command)) return null;
        if (!app.Environment.IsProduction() || !app.Configuration.GetValue("ServerDeployment:Enabled", false) || command != "initialize")
            throw new InvalidOperationException("Server maintenance target rejected.");
        try
        {
            await InitializeAsync(app);
            Console.WriteLine("Server maintenance succeeded: owned schema, canonical roles, runtime login and private storage ready.");
            return 0;
        }
        catch (Exception error) when (error is not OutOfMemoryException && error is not OperationCanceledException)
        {
            // Never forward SQL text, credentials or connection strings to logs.
            Console.Error.WriteLine($"Server maintenance failed ({error.GetType().Name}). No automatic rollback or data deletion.");
            return 70;
        }
    }

    private static async Task InitializeAsync(WebApplication app)
    {
        var config = app.Configuration;
        var target = new SqlConnectionStringBuilder(config["RoadGuardDatabase:ConnectionString"]);
        if (target.DataSource != "sql" || target.InitialCatalog != "RoadGuard_Server" || target.UserID != "sa" ||
            !Guid.TryParseExact(config["ServerDeployment:Owner"], "N", out var owner) || owner == Guid.Empty)
            throw new InvalidOperationException("Server maintenance target rejected.");
        RoadGuardDatabaseOptionsValidator.ValidateOrThrow(new RoadGuardDatabaseOptions { ConnectionString = target.ConnectionString });
        var masterTarget = new SqlConnectionStringBuilder(target.ConnectionString) { InitialCatalog = "master" };
        await using var master = new SqlConnection(masterTarget.ConnectionString);
        await master.OpenAsync();
        await using var command = master.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name=N'RoadGuard_Server'";
        var exists = Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture) != 0;
        if (!exists)
        {
            command.CommandText = "CREATE DATABASE [RoadGuard_Server]";
            await command.ExecuteNonQueryAsync();
            command.CommandText = "USE [RoadGuard_Server]; EXEC sys.sp_addextendedproperty @name=N'RoadGuardServerOwner',@value=@owner";
            command.Parameters.AddWithValue("@owner", owner.ToString("N"));
            await command.ExecuteNonQueryAsync();
            command.Parameters.Clear();
        }
        command.CommandText = "USE [RoadGuard_Server]; SELECT CONVERT(nvarchar(32),value) FROM sys.extended_properties WHERE class=0 AND name=N'RoadGuardServerOwner'";
        if (!string.Equals(await command.ExecuteScalarAsync() as string, owner.ToString("N"), StringComparison.Ordinal))
            throw new InvalidOperationException("Existing database ownership mismatch.");

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RoadGuardDbContext>();
        await db.Database.MigrateAsync();
        await new IdentityRoleSeedStep().SeedAsync(db);
        if (!await db.Users.AnyAsync())
        {
            var email = config["ServerDeployment:InitialSupervisor:Email"] ?? "";
            var password = config["ServerDeployment:InitialSupervisor:Password"] ?? "";
            var displayName = config["ServerDeployment:InitialSupervisor:DisplayName"] ?? "";
            if (!System.Net.Mail.MailAddress.TryCreate(email, out var parsed) || parsed.Address != email ||
                password.Length < 16 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
                !password.Any(char.IsDigit) || password.All(char.IsLetterOrDigit) || string.IsNullOrWhiteSpace(displayName))
                throw new InvalidOperationException("Initial Supervisor input rejected.");
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (await db.Users.AnyAsync()) throw new InvalidOperationException("Concurrent bootstrap refused.");
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                EmailConfirmed = true,
                DisplayName = displayName,
                RoleCode = UserRoleCode.Supervisor,
                Status = UserStatus.Active,
                MustChangePassword = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTimeOffset.UtcNow
            };
            user.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>().HashPassword(user, password);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        var runtimePassword = config["ServerDeployment:RuntimePassword"] ?? "";
        if (runtimePassword.Length < 32 || runtimePassword.Length > 100) throw new InvalidOperationException("Runtime credential rejected.");
        command.CommandText = """
            USE [master];
            IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name=N'roadguard_app')
            BEGIN
                DECLARE @statement nvarchar(max)=N'CREATE LOGIN [roadguard_app] WITH PASSWORD='+QUOTENAME(@password,'''')+N', CHECK_POLICY=ON';
                EXEC sys.sp_executesql @statement;
            END;
            USE [RoadGuard_Server];
            IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name=N'roadguard_app') CREATE USER [roadguard_app] FOR LOGIN [roadguard_app];
            IF IS_ROLEMEMBER(N'db_datareader',N'roadguard_app')<>1 ALTER ROLE [db_datareader] ADD MEMBER [roadguard_app];
            IF IS_ROLEMEMBER(N'db_datawriter',N'roadguard_app')<>1 ALTER ROLE [db_datawriter] ADD MEMBER [roadguard_app];
            GRANT EXECUTE TO [roadguard_app];
            """;
        command.Parameters.AddWithValue("@password", runtimePassword);
        await command.ExecuteNonQueryAsync();

        var storage = scope.ServiceProvider.GetRequiredService<IOptions<MinioStorageOptions>>().Value;
        using var client = ServerDependencyHealthCheck.CreateStorageClient(storage);
        try { await client.GetBucketLocationAsync(new GetBucketLocationRequest { BucketName = storage.BucketName }); }
        catch (AmazonS3Exception error) when (error.StatusCode == System.Net.HttpStatusCode.NotFound)
        { await client.PutBucketAsync(new PutBucketRequest { BucketName = storage.BucketName }); }
        try
        {
            var policy = await client.GetBucketPolicyAsync(new GetBucketPolicyRequest { BucketName = storage.BucketName });
            if (policy.HttpStatusCode == System.Net.HttpStatusCode.NotFound &&
                System.Xml.Linq.XDocument.Parse(policy.Policy).Root?.Element("Code")?.Value == "NoSuchBucketPolicy") return;
            if (policy.HttpStatusCode != System.Net.HttpStatusCode.OK || !string.IsNullOrWhiteSpace(policy.Policy))
                throw new InvalidOperationException("Existing bucket policy requires review; no policy was changed.");
        }
        catch (AmazonS3Exception error) when (error.ErrorCode == "NoSuchBucketPolicy") { }
    }
}

public sealed class ServerDependencyHealthCheck(IServiceScopeFactory scopes, IOptions<MinioStorageOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            if (!await scope.ServiceProvider.GetRequiredService<RoadGuardDbContext>().Database.CanConnectAsync(deadline.Token))
                return HealthCheckResult.Unhealthy("Database unavailable.");
            using var client = CreateStorageClient(options.Value);
            await client.GetBucketLocationAsync(new GetBucketLocationRequest { BucketName = options.Value.BucketName }, deadline.Token);
            return HealthCheckResult.Healthy();
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { return HealthCheckResult.Unhealthy("Server dependency unavailable."); }
    }

    internal static AmazonS3Client CreateStorageClient(MinioStorageOptions options)
        => new(new BasicAWSCredentials(options.AccessKey, options.SecretKey), new AmazonS3Config
        { ServiceURL = options.Endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
}
