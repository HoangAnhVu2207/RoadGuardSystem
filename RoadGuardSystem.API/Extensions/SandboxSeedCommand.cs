using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Seeding.Sandbox;

namespace RoadGuardSystem.API.Extensions;

public static class SandboxSeedCommand
{
    public static string[] NormalizeArguments(string[] args)
    {
        if (args.Count(a => a == "--sandbox-seed") > 1) throw new InvalidOperationException("DuplicateSandboxSeedSwitch");
        return args.Where(a => a != "--sandbox-seed").ToArray();
    }
    public static async Task<int?> TryExecuteAsync(WebApplication app, string[] args, CancellationToken ct = default)
    {
        if (!args.Contains("--sandbox-seed", StringComparer.Ordinal)) return null;
        try
        {
            if (!app.Environment.IsDevelopment() || !app.Configuration.GetValue("Sandbox:Enabled", false) ||
                app.Configuration.GetValue("RoadGuardDatabase:InitializeOnStartup", false)) throw new InvalidOperationException("TargetRejected");
            var config = app.Configuration.GetSection("Sandbox:Seed");
            if (!Guid.TryParse(config["OwnerId"], out var owner)) throw new InvalidOperationException("TargetRejected");
            var catalog = SandboxSeedCatalog.Load(config["CatalogPath"] ?? "/seed/catalog");
            var path = config["CredentialPath"] ?? "/seed/private/credentials.sbx02-v1.json";
            var bundle = JsonSerializer.Deserialize<CredentialFile>(await File.ReadAllTextAsync(path, ct)) ?? throw new InvalidOperationException("CredentialMismatch");
            var credentials = new SandboxCredentialInput(bundle.BundleId, bundle.SeedVersion, bundle.Passwords.ToDictionary(p => p.ActorId, p => p.Password));
            var target = new SandboxSeedTarget("Session", config["DatabaseName"] ?? "", config["ExpectedServer"] ?? "", owner, config["ContainerId"] ?? "");
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<RoadGuardDbContext>();
            var outcome = await SandboxSyntheticSeeder.InitializeAsync(db, target, catalog, credentials, ct);
            Console.WriteLine(outcome.Initialized ? "SBX02 Initialized: 11 actors; 2 projects." : "SBX02 AlreadyInitialized: no data changed.");
            return 0;
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // No exception body, SQL detail, credential contents or token is forwarded.
            var safe = new[] { "TargetRejected", "SchemaMismatch", "NonPristine", "VersionMismatch", "CredentialMismatch", "LockTimeout" };
            Console.Error.WriteLine("SBX02 refused: " + (safe.Contains(error.Message, StringComparer.Ordinal) ? error.Message : "ExecutionFailed"));
            return 70;
        }
    }
    private sealed record CredentialFile(Guid BundleId, string SeedVersion, CredentialEntry[] Passwords);
    private sealed record CredentialEntry(Guid ActorId, string Password);
}
