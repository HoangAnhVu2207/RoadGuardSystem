using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories.Seeding.Sandbox;
using Xunit;

namespace RoadGuardSystem.ApiTests.Sandbox;

// Prepared only while STORAGE_GATE=CLOSED. Every case owns/disposes one SQL fixture.
// No collection SQL fixture is added on top of the method-owned fixture.
[Collection(SandboxMethodOwnedSqlGroup.Name)]
public sealed class SandboxProjectReplayTests
{
    [Fact]
    public async Task CurrentVersionSucceedsThenStaleAndChangedReplayCannotMutateProject()
    {
        await using var context = new SandboxScenarioContext();
        await context.InitializeCoreAsync();
        context.Fixture.GetOwnedSeedDatabaseIdentity();
        using var supervisor = await context.LoginAsync("supervisor");
        await using var db = context.Fixture.CreateDbContext();
        var original = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == SandboxFixtureIds.ProjectA);
        var originalB = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == SandboxFixtureIds.ProjectB);
        var receiptsBefore = await db.IdempotencyRecords.CountAsync(r => r.ProjectId == original.Id && r.Operation == "ProjectUpdated");
        var operation = Guid.NewGuid();
        var request = new
        {
            name = "Synthetic replay boundary",
            description = "Synthetic",
            expectedRowVersion = Convert.ToBase64String(original.RowVersion),
            operationId = operation
        };
        var url = $"/api/v1/projects/{original.Id}";
        using var current = await supervisor.PutAsJsonAsync(url, request);
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        var committed = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == original.Id);
        Assert.NotEqual(original.RowVersion, committed.RowVersion);
        using var replay = await supervisor.PutAsJsonAsync(url, request);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var conflict = await supervisor.PutAsJsonAsync(url, new
        {
            name = "Changed command",
            request.description,
            request.expectedRowVersion,
            request.operationId
        });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using var stale = await supervisor.PutAsJsonAsync(url, new
        {
            request.name,
            request.description,
            request.expectedRowVersion,
            operationId = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var final = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == original.Id);
        Assert.Equal(committed.Name, final.Name);
        Assert.Equal(committed.RowVersion, final.RowVersion);
        var other = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == SandboxFixtureIds.ProjectB);
        Assert.Equal(originalB.Name, other.Name);
        Assert.Equal(originalB.RowVersion, other.RowVersion);
        Assert.Equal(receiptsBefore + 1, await db.IdempotencyRecords.CountAsync(r => r.ProjectId == original.Id && r.Operation == "ProjectUpdated"));
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SandboxMethodOwnedSqlGroup
{
    public const string Name = "Sandbox method-owned SQL";
}
