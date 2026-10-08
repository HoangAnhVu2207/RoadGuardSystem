using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Seeding;
using System.Text.Json;

namespace RoadGuardSystem.Seeder;

public static class Program
{
    public const string ConnectionStringEnvVarName = "ROADGUARD_CONNECTION_STRING";
    public const string DevelopmentProfileEnvVarName = "ROADGUARD_SEED_PROFILE";
    public const string ManifestPathEnvVarName = "ROADGUARD_SEED_MANIFEST_PATH";
    public const string PasswordEnvVarName = "ROADGUARD_SEED_PASSWORD";
    public const string StorageConfigPathEnvVarName = "ROADGUARD_SEED_STORAGE_CONFIG_PATH";
    private static readonly JsonSerializerOptions ManifestJson = new() { WriteIndented = true };

    public static Task<int> Main(string[] args) => RunAsync(args, Environment.GetEnvironmentVariable);

    public static async Task<int> RunAsync(
        string[] args,
        Func<string, string?>? envLookup = null,
        CancellationToken cancellationToken = default)
    {
        envLookup ??= Environment.GetEnvironmentVariable;

        Console.WriteLine("=== RoadGuard Database Seeder ===");

        // Validate command-line arguments: only --help / -h are supported
        foreach (var arg in args)
        {
            if (string.IsNullOrWhiteSpace(arg))
            {
                Console.Error.WriteLine("ERROR: Argument cannot be empty or whitespace.");
                PrintUsage();
                return 1;
            }

            if (arg is "--help" or "-h")
            {
                PrintUsage();
                return 0;
            }

            Console.Error.WriteLine($"ERROR: Unsupported argument '{arg}'. The Seeder CLI does not accept connection strings or flags via command-line arguments. Set the {ConnectionStringEnvVarName} environment variable.");
            PrintUsage();
            return 1;
        }

        var connectionString = envLookup(ConnectionStringEnvVarName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine($"ERROR: Missing required environment variable {ConnectionStringEnvVarName}.");
            PrintUsage();
            return 1;
        }

        var profile = envLookup(DevelopmentProfileEnvVarName);
        if (!string.IsNullOrEmpty(profile) && profile != "Development")
        {
            Console.Error.WriteLine($"ERROR: {DevelopmentProfileEnvVarName} supports only Development; omit it for canonical roles only.");
            return 1;
        }
        var development = profile == "Development";
        var manifestPath = envLookup(ManifestPathEnvVarName);
        var password = envLookup(PasswordEnvVarName);
        var storagePath = envLookup(StorageConfigPathEnvVarName);
        if (development && string.IsNullOrWhiteSpace(manifestPath))
        {
            Console.Error.WriteLine($"ERROR: Development scenarios require {ManifestPathEnvVarName} for the safe scenario/coverage manifest.");
            return 1;
        }
        if (development && (string.IsNullOrWhiteSpace(password) || password.Length < 12 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || password.All(char.IsLetterOrDigit)))
        {
            Console.Error.WriteLine($"ERROR: Development requires a private {PasswordEnvVarName} with at least 12 characters including upper/lower/digit/symbol.");
            return 1;
        }

        Console.WriteLine("Connecting to target database for readiness check...");

        var optionsBuilder = new DbContextOptionsBuilder<RoadGuardDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOpts =>
        {
            sqlOpts.UseNetTopologySuite();
            sqlOpts.CommandTimeout(30);
        });

        await using var context = new RoadGuardDbContext(optionsBuilder.Options);

        // Supported seeder composition: executes registered foundational seed steps
        DevelopmentSeedManifest? manifest = null;
        var steps = new List<ISeedStep> { new IdentityRoleSeedStep() };
        if (development)
        {
            steps.Add(new DroneDeviceSeedStep());
            steps.Add(new PostmanUserSeedStep(password));
            steps.Add(new PostmanScenarioSeedStep(async (db, token) =>
                manifest = await DevelopmentScenarios.SeedAsync(db, storagePath, token)));
        }
        var seeder = new DatabaseSeeder(steps);

        try
        {
            var result = await seeder.SeedAsync(context, cancellationToken);
            if (manifest is not null)
            {
                var fullPath = Path.GetFullPath(manifestPath!);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await File.WriteAllTextAsync(fullPath, JsonSerializer.Serialize(manifest, ManifestJson), cancellationToken);
                Console.WriteLine($"Development manifest exported: {manifest.Tables.Count} tables assessed; {manifest.Gaps.Count} explicit GAPs. Synthetic dev/test evidence only.");
            }
            Console.WriteLine($"SUCCESS: Seeding completed successfully. {result.StepsExecuted} steps executed in {result.Elapsed.TotalMilliseconds:F1}ms.");
            return 0;
        }
        catch (DatabaseNotReadyException ex)
        {
            Console.Error.WriteLine($"FATAL: Database readiness verification failed: {ex.Message}");
            return 2;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.Error.WriteLine("FATAL: Seeding execution was canceled.");
            throw;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FATAL: Unexpected seeding execution error: {ex.Message}");
            if (development) Console.Error.WriteLine(ex.StackTrace);
            return 3;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine($@"
Usage:
  dotnet run --project tools/RoadGuardSystem.Seeder -- [options]

Options:
  -h, --help                       Show this help message.

Environment Variables:
  {ConnectionStringEnvVarName}      SQL Server database connection string (required).
  {DevelopmentProfileEnvVarName}   Development enables synthetic service-produced scenarios (optional).
  {ManifestPathEnvVarName}          Safe JSON scenario/coverage manifest path (required for Development).
  {PasswordEnvVarName}              Private dev/test password for NEW accounts (required for Development).
  {StorageConfigPathEnvVarName}     Private task-owned MinioStorage JSON options; enables real storage producers.

Use only an explicitly owned isolated dev/test database. This command never migrates or resets a database.
Dev/test login accounts use example.test addresses and the configured private development password.
");
    }
}
