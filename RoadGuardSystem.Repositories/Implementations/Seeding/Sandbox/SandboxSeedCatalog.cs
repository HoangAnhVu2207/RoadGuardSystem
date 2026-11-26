using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Seeding.Sandbox;

public sealed record SandboxSeedTarget(string Mode, string DatabaseName, string ExpectedServer, Guid OwnerId, string ExpectedContainerId);
public sealed record SandboxActor(Guid Id, string Key, string Email, UserRoleCode Role, string? Project, bool Suspended = false, bool Ended = false);
public sealed record SandboxSchemaFacts(string[] Tables, string[] Migrations, string Signature);
public sealed record ScenarioDescriptor(string Id, string Workflow, string CoverageKind, string SourceReference, string[] Actors, string[] Preconditions, string[] Expectations);
public sealed record TableClassification(string Table, string Category, string Reason, string Evidence);
public sealed record SandboxSeedOutcome(bool Initialized, string Version, Guid BundleId, int Actors, int Projects);
public sealed class SandboxCredentialInput(Guid bundleId, string seedVersion, IReadOnlyDictionary<Guid, string> passwords)
{
    public Guid BundleId { get; } = bundleId;
    public string SeedVersion { get; } = seedVersion;
    public IReadOnlyDictionary<Guid, string> Passwords { get; } = passwords;
    public override string ToString() => "[PRIVATE CREDENTIAL INPUT]";
}
public sealed class SandboxSeedCatalog
{
    public string Version { get; init; } = "";
    public SandboxActor[] Actors { get; init; } = [];
    public SandboxSchemaFacts Schema { get; init; } = new([], [], "");
    public ScenarioDescriptor[] Scenarios { get; init; } = [];
    public TableClassification[] Classifications { get; init; } = [];
    public static SandboxSeedCatalog Load(string directory, bool realHandoff = false)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var actors = JsonSerializer.Deserialize<SandboxSeedCatalog>(File.ReadAllText(Path.Combine(directory, realHandoff ? "real-accounts.json" : "accounts.json")), options)!;
        var schema = JsonSerializer.Deserialize<SandboxSchemaFacts>(File.ReadAllText(Path.Combine(directory, "table-coverage.json")), options)!;
        using var coverage = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "table-coverage.json")));
        var classifications = coverage.RootElement.GetProperty(nameof(Classifications)).Deserialize<TableClassification[]>(options)!;
        ScenarioDescriptor[] scenarios = realHandoff ? [] : JsonSerializer.Deserialize<ScenarioDescriptor[]>(File.ReadAllText(Path.Combine(directory, "scenarios.json")), options)!;
        var catalog = new SandboxSeedCatalog { Version = actors.Version, Actors = actors.Actors, Schema = schema, Classifications = classifications, Scenarios = scenarios };
        catalog.Validate();
        return catalog;
    }
    public void Validate()
    {
        var legacy = Version == SandboxFixtureIds.Version;
        var real = Version == "real-ai-demo-v1";
        var expectedActors = legacy ? 11 : real ? 14 : 0;
        var expectedDomain = legacy ? "@sandbox.example.test" : "@roadguard.local";
        if ((!legacy && !real) || Actors.Length != expectedActors || Actors.Select(a => a.Id).Distinct().Count() != expectedActors ||
            Actors.Any(a => a.Id == Guid.Empty || !a.Email.EndsWith(expectedDomain, StringComparison.Ordinal) ||
                !Enum.IsDefined(a.Role) || a.Role == UserRoleCode.Unknown || (a.Project is not null and not "A" and not "B")) ||
            Actors.Select(a => a.Email).Distinct(StringComparer.OrdinalIgnoreCase).Count() != expectedActors ||
            Schema.Tables.Length == 0 || Schema.Tables.Distinct().Count() != Schema.Tables.Length || Schema.Signature.Length != 64 || Schema.Migrations.Length == 0)
            throw new InvalidOperationException("SchemaMismatch");
        if (Actors.Select(a => a.Key).Distinct().Count() != expectedActors ||
            (legacy && (Actors.Count(a => a.Suspended) != 1 || Actors.Count(a => a.Ended) != 1)) ||
            (real && (Actors.Any(a => a.Suspended || a.Ended) ||
                Actors.Count(a => a.Role == UserRoleCode.Supervisor) != 2 || Actors.Count(a => a.Role == UserRoleCode.ProjectManager) != 2 ||
                Actors.Count(a => a.Role == UserRoleCode.DroneOperator) != 2 || Actors.Count(a => a.Role == UserRoleCode.RepairCrew) != 4 ||
                Actors.Count(a => a.Role == UserRoleCode.Reporter) != 4)) ||
            (legacy && Actors.Any(a => (a.Role is UserRoleCode.Reporter or UserRoleCode.Supervisor) && a.Project is not null)) ||
            !Classifications.Select(c => c.Table).Order(StringComparer.Ordinal).SequenceEqual(Schema.Tables.Order(StringComparer.Ordinal)) ||
            Classifications.Any(c => c.Category is not "SEEDED" and not "TEST_ONLY" and not "NOT_COVERED" || string.IsNullOrWhiteSpace(c.Reason) || string.IsNullOrWhiteSpace(c.Evidence)) ||
            Scenarios.Length != (legacy ? 10 : 0) || Scenarios.Select(s => s.Workflow).Distinct().Count() != Scenarios.Length ||
            Scenarios.Any(s => s.CoverageKind is not "CORE" and not "NOT_COVERED" || s.Actors.Any(a => !Actors.Any(actor => actor.Key == a)) || string.IsNullOrWhiteSpace(s.SourceReference)))
            throw new InvalidOperationException("SchemaMismatch");
    }
}
