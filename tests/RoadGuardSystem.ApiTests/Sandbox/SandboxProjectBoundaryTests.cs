using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.Repositories.Seeding.Sandbox;
using RoadGuardSystem.Services.Authorization;
using Xunit;
using System.Net;
using System.Net.Http.Json;

namespace RoadGuardSystem.ApiTests.Sandbox;

public sealed class SandboxProjectBoundaryTests
{
    [Fact]
    public async Task ProjectUpdateRequiresSupervisorAndPreservesDeniedTargets()
    {
        await using var context = new SandboxScenarioContext(); await context.InitializeCoreAsync();
        using var pm = await context.LoginAsync("pm-a");
        await using var db = context.Fixture.CreateDbContext();
        var before = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == SandboxFixtureIds.ProjectB);
        var command = new { name = "Synthetic forbidden edit", description = "Synthetic", expectedRowVersion = Convert.ToBase64String(before.RowVersion), operationId = Guid.NewGuid() };
        using var denied = await pm.PutAsJsonAsync($"/api/v1/projects/{before.Id}", command);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var after = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == before.Id);
        Assert.Equal(before.Name, after.Name); Assert.Equal(before.RowVersion, after.RowVersion);
        using var reporter = await context.LoginAsync("reporter-a");
        using var wrongRole = await reporter.PutAsJsonAsync($"/api/v1/projects/{before.Id}", command);
        Assert.Equal(HttpStatusCode.Forbidden, wrongRole.StatusCode);
        var own = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == SandboxFixtureIds.ProjectA);
        var ownCommand = new { name = "Synthetic developer edit", description = "Synthetic", expectedRowVersion = Convert.ToBase64String(own.RowVersion), operationId = Guid.NewGuid() };
        using var deniedOwn = await pm.PutAsJsonAsync($"/api/v1/projects/{own.Id}", ownCommand);
        Assert.Equal(HttpStatusCode.Forbidden, deniedOwn.StatusCode);
        var unchanged = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == own.Id);
        Assert.Equal(own.Name, unchanged.Name); Assert.Equal(own.RowVersion, unchanged.RowVersion);
        using var supervisor = await context.LoginAsync("supervisor");
        using var allowed = await supervisor.PutAsJsonAsync($"/api/v1/projects/{own.Id}", ownCommand);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal("Synthetic developer edit", (await db.Projects.AsNoTracking().SingleAsync(p => p.Id == own.Id)).Name);
        Assert.Equal(0, await db.OutboxMessages.CountAsync());
        Assert.Equal(0, await db.ConsumerEffectReceipts.CountAsync());
    }
    [Fact]
    public async Task ProjectMembershipSeparatesAAndBAfterRealLogin()
    {
        await using var context = new SandboxScenarioContext(); await context.InitializeCoreAsync();
        foreach (var key in new[] { "pm-a", "operator-a", "crew-a" })
        {
            using var client = await context.LoginAsync(key);
            var actor = context.Catalog.Actors.Single(a => a.Key == key);
            using var scope = context.Factory.Services.CreateScope();
            var guard = scope.ServiceProvider.GetRequiredService<IProjectScopeGuard>();
            Assert.NotNull(await guard.AuthorizeAsync(actor.Id, actor.Role, SandboxFixtureIds.ProjectA));
            Assert.Null(await guard.AuthorizeAsync(actor.Id, actor.Role, SandboxFixtureIds.ProjectB));
        }
        var supervisor = context.Catalog.Actors.Single(a => a.Key == "supervisor");
        using var globalScope = context.Factory.Services.CreateScope();
        var global = globalScope.ServiceProvider.GetRequiredService<IProjectScopeGuard>();
        Assert.NotNull(await global.AuthorizeAsync(supervisor.Id, supervisor.Role, SandboxFixtureIds.ProjectB));
        await using var db = context.Fixture.CreateDbContext();
        Assert.Equal(2, await db.Projects.CountAsync()); Assert.Equal(0, await db.OutboxMessages.CountAsync());
        Assert.Equal(0, await db.ConsumerEffectReceipts.CountAsync());
    }
}
