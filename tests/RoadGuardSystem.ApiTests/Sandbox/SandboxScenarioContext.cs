using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Repositories.Seeding.Sandbox;
using Xunit;

namespace RoadGuardSystem.ApiTests.Sandbox;

internal sealed class SandboxScenarioContext : IAsyncDisposable
{
    public AuthenticationSqlServerFixture Fixture { get; } = new();
    public AuthenticationWebApplicationFactory Factory { get; private set; } = null!;
    public SandboxSeedCatalog Catalog { get; private set; } = null!;
    public SandboxCredentialInput Credentials { get; private set; } = null!;
    public async Task InitializeCoreAsync()
    {
        await Fixture.InitializeAsync();
        var identity = Fixture.GetOwnedSeedDatabaseIdentity();
        await using var db = Fixture.CreateDbContext();
        await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sys.sp_addextendedproperty @name=N'RoadGuardSandboxOwner',@value={identity.OwnerId.ToString("N")};");
        await db.Database.OpenConnectionAsync();
        await using var cmd = db.Database.GetDbConnection().CreateCommand(); cmd.CommandText = "SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'));";
        var server = (string)(await cmd.ExecuteScalarAsync())!;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AGENTS.md"))) dir = dir.Parent;
        Catalog = SandboxSeedCatalog.Load(Path.Combine(dir!.FullName, "sandbox", "seeds", "v1"));
        Credentials = new(Guid.NewGuid(), Catalog.Version, Catalog.Actors.ToDictionary(a => a.Id, _ => "Sbx1!" + Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))));
        await SandboxSyntheticSeeder.InitializeAsync(db, new("Test", identity.DatabaseName, server, identity.OwnerId, identity.ContainerId), Catalog, Credentials);
        Factory = new(Fixture.ConnectionString);
    }
    public async Task<HttpClient> LoginAsync(string actorKey)
    {
        var actor = Catalog.Actors.Single(a => a.Key == actorKey);
        var client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var response = await client.PostAsJsonAsync("/api/v1/auth/android/login", new { email = actor.Email, password = Credentials.Passwords[actor.Id] });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }
    public async ValueTask DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        await Fixture.DisposeAsync();
    }
}
