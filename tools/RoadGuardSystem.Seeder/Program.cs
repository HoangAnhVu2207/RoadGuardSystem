using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Seeding;

namespace RoadGuardSystem.Seeder;

public static class Program
{
    public const string ConnectionStringEnvVarName = "ROADGUARD_CONNECTION_STRING";

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

        Console.WriteLine("Connecting to target database for readiness check...");

        var optionsBuilder = new DbContextOptionsBuilder<RoadGuardDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOpts =>
        {
            sqlOpts.UseNetTopologySuite();
            sqlOpts.CommandTimeout(30);
        });

        await using var context = new RoadGuardDbContext(optionsBuilder.Options);

        // Supported seeder composition: executes registered foundational seed steps
        var steps = new ISeedStep[]
        {
            new IdentityRoleSeedStep()
        };
        var seeder = new DatabaseSeeder(steps);

        try
        {
            var result = await seeder.SeedAsync(context, cancellationToken);
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
");
    }
}
