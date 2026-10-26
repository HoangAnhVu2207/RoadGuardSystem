using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.ApiTests.Sandbox;
using RoadGuardSystem.Repositories.Seeding.Sandbox;
using RoadGuardSystem.Services.Authorization;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

public sealed class SandboxSyntheticActorTests
{
    [Fact]
    public async Task ActiveActorsAuthenticateAndSuspendedActorCannotCreateSession()
    {
        await using var context = new SandboxScenarioContext(); await context.InitializeCoreAsync();
        foreach (var actor in context.Catalog.Actors.Where(a => !a.Suspended))
        {
            using var client = await context.LoginAsync(actor.Key);
            await using var db = context.Fixture.CreateDbContext();
            var sessions = await db.Sessions.Where(s => s.UserId == actor.Id).ToListAsync();
            Assert.Single(sessions); Assert.Null(sessions[0].RevokedAt);
            Assert.Equal(actor.Role, (await db.Users.SingleAsync(u => u.Id == actor.Id)).RoleCode);
        }
        var suspended = context.Catalog.Actors.Single(a => a.Suspended);
        using var anonymous = context.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var denied = await anonymous.PostAsJsonAsync("/api/v1/auth/android/login", new { email = suspended.Email, password = context.Credentials.Passwords[suspended.Id] });
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        await using (var db = context.Fixture.CreateDbContext()) Assert.False(await db.Sessions.AnyAsync(s => s.UserId == suspended.Id));
        using var ended = await context.LoginAsync("ended-a");
        using var scope = context.Factory.Services.CreateScope();
        var guard = scope.ServiceProvider.GetRequiredService<IProjectScopeGuard>();
        var actorEnded = context.Catalog.Actors.Single(a => a.Ended);
        Assert.Null(await guard.AuthorizeAsync(actorEnded.Id, actorEnded.Role, SandboxFixtureIds.ProjectA));
    }
}
