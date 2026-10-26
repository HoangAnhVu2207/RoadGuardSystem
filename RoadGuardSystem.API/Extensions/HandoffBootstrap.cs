using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Seeding.Sandbox;

namespace RoadGuardSystem.API.Extensions;

/// <summary>Opt-in local Docker handoff only. Never adopts an unowned database.</summary>
public static class HandoffBootstrap
{
    // Registry fixture only: this is not a released real model or an AI service identity.
    public static RoadGuardSystem.BusinessObjects.Processing.AIModelVersion CreateDemoModel()
        => RoadGuardSystem.BusinessObjects.Processing.AIModelVersion.Create(
            Guid.Parse("72f84201-d033-4000-8000-000000000001"), "MOCK_AI_DEVELOPMENT_DATA",
            "synthetic-road-v1", "fixture://local/synthetic-road-v1", null, null,
            RoadGuardSystem.aBusinessObjects.Commons.AIModelVersionStatus.Released,
            DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            Guid.Parse("72f84201-a001-4000-8000-000000000001"));

    public static string[] ResolveOrigins(string? configured, string[] persisted)
        => ValidateOrigins(configured is null ? persisted : configured.Split(','));

    public static string[] ValidateOrigins(string[] origins)
    {
        if (origins.Length == 0 || origins.Any(origin => !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            !uri.IsLoopback || (uri.Scheme != "http" && uri.Scheme != "https") ||
            uri.AbsolutePath != "/" || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 || origin.EndsWith('/')))
            throw new InvalidOperationException("Handoff origins must be explicit loopback HTTP(S) origins without a trailing slash.");
        return origins;
    }

    public static void MapReadiness(WebApplication app)
    {
        if (!app.Configuration.GetValue("Handoff:Enabled", false)) return;
        app.MapGet("/ready", async (RoadGuardDbContext db, IConfiguration config,
            Microsoft.Extensions.Options.IOptions<RoadGuardSystem.Repositories.Options.MinioStorageOptions> options) =>
        {
            try
            {
                if (!await db.Database.CanConnectAsync()) return Results.StatusCode(503);
                var storage = options.Value;
                using var client = new Amazon.S3.AmazonS3Client(
                    new Amazon.Runtime.BasicAWSCredentials(storage.AccessKey, storage.SecretKey),
                    new Amazon.S3.AmazonS3Config { ServiceURL = storage.Endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await client.GetBucketLocationAsync(new Amazon.S3.Model.GetBucketLocationRequest { BucketName = storage.BucketName }, timeout.Token);
                using var mail = new System.Net.Sockets.TcpClient();
                await mail.ConnectAsync(config["Sandbox:MailHost"] ?? "mailpit", 1025, timeout.Token);
                return Results.Ok(new { status = "ready", database = true, privateStorage = true, testMail = true });
            }
            catch (Exception error) when (error is not OutOfMemoryException) { return Results.StatusCode(503); }
        });
    }

    public static void ValidateTarget(string environment, string server, string database, string owner)
    {
        if (environment != "Development" || server != "sql" ||
            !Guid.TryParseExact(owner, "N", out var id) || id == Guid.Empty ||
            database != $"RoadGuard_FE_{id:N}")
            throw new InvalidOperationException("Handoff target rejected.");
    }

    // Transport/startup failures only. Authentication, authorization and ownership failures
    // must remain fail-closed; bootstrap commands themselves are never replayed here.
    public static bool IsStartupConnectionTransient(int number)
        => number is -2 or 0 or 20 or 64 or 233 or 10053 or 10054 or 10060 or 17142 or 18401;

    private static async Task OpenMasterAsync(SqlConnection master, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(120));
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await master.OpenAsync(deadline.Token);
                return;
            }
            catch (SqlException error) when (attempt < 30 &&
                error.Errors.Cast<SqlError>().All(x => IsStartupConnectionTransient(x.Number)))
            {
                await Task.Delay(TimeSpan.FromSeconds(2), deadline.Token);
            }
        }
    }

    public static async Task InitializeAsync(WebApplication app)
    {
        if (!app.Configuration.GetValue("Handoff:Enabled", false)) return;
        if (!app.Configuration.GetValue("Sandbox:Enabled", false))
            throw new InvalidOperationException("Handoff requires sandbox isolation.");
        var config = app.Configuration;
        var owner = config["Handoff:Owner"] ?? "";
        var connection = new SqlConnectionStringBuilder(config["RoadGuardDatabase:ConnectionString"]);
        ValidateTarget(app.Environment.EnvironmentName, connection.DataSource, connection.InitialCatalog, owner);
        var database = connection.InitialCatalog;
        var identity = Regex.Match(await File.ReadAllTextAsync("/proc/self/mountinfo"), @"/containers/([a-f0-9]{64})/hostname ");
        if (!identity.Success) throw new InvalidOperationException("Docker container identity unavailable.");
        connection.InitialCatalog = "master";
        connection.ConnectTimeout = 5;
        connection.PoolBlockingPeriod = PoolBlockingPeriod.NeverBlock;
        await using var master = new SqlConnection(connection.ConnectionString);
        await OpenMasterAsync(master, app.Lifetime.ApplicationStopping);
        await using var command = master.CreateCommand();
        command.CommandTimeout = 180;
        // Session lock covers creation, migration and seed; seeder retains its own transactional lock.
        command.CommandText = "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'RoadGuardHandoffBootstrap',@LockMode=N'Exclusive',@LockOwner=N'Session',@LockTimeout=30000; SELECT @r;";
        if (Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture) < 0) throw new InvalidOperationException("Handoff lock unavailable.");
        command.CommandText = "SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'));";
        var server = (string)(await command.ExecuteScalarAsync())!;
        if (server != "roadguard-handoff") throw new InvalidOperationException("Handoff SQL server rejected.");
        command.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name=@database";
        command.Parameters.AddWithValue("@database", database);
        var exists = Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture) != 0;
        if (!exists)
        {
            command.CommandText = $"CREATE DATABASE [{database}];";
            await command.ExecuteNonQueryAsync();
            command.CommandText = $"USE [{database}]; EXEC sys.sp_addextendedproperty @name=N'RoadGuardSandboxOwner',@value=@owner;";
            command.Parameters.AddWithValue("@owner", owner);
            await command.ExecuteNonQueryAsync();
        }
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RoadGuardDbContext>();
        var target = new SandboxSeedTarget("Session", database, server, Guid.ParseExact(owner, "N"), identity.Groups[1].Value);
        await SandboxSeedGuard.ValidateTargetAsync(db, target);
        await db.Database.MigrateAsync();
        var catalog = SandboxSeedCatalog.Load("/catalog");
        using var credentials = JsonDocument.Parse(await File.ReadAllTextAsync("/handoff/seed-credentials.json"));
        var passwords = credentials.RootElement.GetProperty("Passwords").EnumerateArray()
            .ToDictionary(x => x.GetProperty("ActorId").GetGuid(), x => x.GetProperty("Password").GetString()!);
        await SandboxSyntheticSeeder.InitializeAsync(db, target, catalog,
            new SandboxCredentialInput(credentials.RootElement.GetProperty("BundleId").GetGuid(), catalog.Version, passwords));
        if (config.GetValue("Anh02:MockEnabled", false))
        {
            // The session bootstrap lock and mandatory target ownership check above still apply.
            // Never rewrite registry data or silently adopt a colliding identifier.
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                var fixture = CreateDemoModel();
                var existing = await db.AIModelVersions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == fixture.Id);
                if (existing is not null && (existing.ModelName != fixture.ModelName || existing.VersionLabel != fixture.VersionLabel ||
                    existing.ArtifactUri != fixture.ArtifactUri || existing.Status != fixture.Status))
                    throw new InvalidOperationException("Synthetic model fixture conflict; no automatic overwrite.");
                if (existing is null) db.AIModelVersions.Add(fixture);
                await db.Database.ExecuteSqlRawAsync("IF NOT EXISTS(SELECT 1 FROM DefectTypes WITH (UPDLOCK,HOLDLOCK) WHERE Code='CRACK') INSERT INTO DefectTypes(Code,Name,IsActive) VALUES ('CRACK','Crack',1)");
                await db.Database.ExecuteSqlRawAsync("IF NOT EXISTS(SELECT 1 FROM DefectTypes WITH (UPDLOCK,HOLDLOCK) WHERE Code='POTHOLE') INSERT INTO DefectTypes(Code,Name,IsActive) VALUES ('POTHOLE','Synthetic pothole',1)");
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            });
        }
    }
}
