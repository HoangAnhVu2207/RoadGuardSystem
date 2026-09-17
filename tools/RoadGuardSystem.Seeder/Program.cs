using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Seeding;

namespace RoadGuardSystem.Seeder;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== RoadGuard Database Seeder ===");

        string? connectionString = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is "--connection-string" or "-c" && i + 1 < args.Length)
            {
                connectionString = args[++i];
            }
            else if (args[i] is "--help" or "-h")
            {
                PrintUsage();
                return 0;
            }
        }

        connectionString ??= Environment.GetEnvironmentVariable("ROADGUARD_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine("ERROR: No connection string provided. Use --connection-string <conn> or set ROADGUARD_CONNECTION_STRING environment variable.");
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

        // Wave 0: framework entry point without invented business entities
        var seeder = new DatabaseSeeder(Array.Empty<ISeedStep>());

        try
        {
            var result = await seeder.SeedAsync(context);
            Console.WriteLine($"SUCCESS: Seeding completed successfully. {result.StepsExecuted} steps executed in {result.Elapsed.TotalMilliseconds:F1}ms.");
            return 0;
        }
        catch (DatabaseNotReadyException ex)
        {
            Console.Error.WriteLine($"FATAL: Database readiness verification failed: {ex.Message}");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FATAL: Unexpected seeding execution error: {ex.Message}");
            return 3;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine(@"
Usage:
  dotnet run --project tools/RoadGuardSystem.Seeder -- [options]

Options:
  -c, --connection-string <conn>   SQL Server database connection string.
  -h, --help                       Show this help message.

Environment Variables:
  ROADGUARD_CONNECTION_STRING      Fallback connection string if not specified via CLI.
");
    }
}
