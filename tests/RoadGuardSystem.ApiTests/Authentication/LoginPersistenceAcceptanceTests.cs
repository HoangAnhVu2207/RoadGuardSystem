using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Collection(AuthenticationApiFixture.Name)]
public sealed class LoginPersistenceAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("API-002", "/api/v1/auth/android/login")]
    [InlineData("API-006", "/api/v1/auth/login")]
    [InlineData("API-014", "/api/v1/auth/web/login")]
    public async Task ConcurrentLoginsHaveCompleteIndependentCredentialsAndCurrentLifecycle(string operation, string route)
    {
        var actor = await sql.CreateUserAsync("LOGIN-SQL-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var first = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        using var second = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        if (operation == "API-014")
            foreach (var client in new[] { first, second })
            {
                var csrf = await client.GetAsync("/api/v1/auth/web/csrf");
                Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
                client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
            }
        var started = DateTimeOffset.UtcNow;
        var responses = await Task.WhenAll(first.PostAsJsonAsync(route, new { email = actor.Email, password = "Current1!" }),
            second.PostAsJsonAsync(route, new { email = actor.Email, password = "Current1!" }));
        var conflictStatus = operation == "API-014" ? HttpStatusCode.Unauthorized : HttpStatusCode.Conflict;
        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, conflictStatus }));
        var successes = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        Assert.InRange(successes, 1, 2);
        foreach (var response in responses.Where(r => r.StatusCode == conflictStatus))
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((int)conflictStatus, problem.GetProperty("status").GetInt32());
            Assert.Equal(operation == "API-014" ? "auth_unauthorized" : "auth_concurrency_conflict", problem.GetProperty("code").GetString());
            Assert.Equal(route, problem.GetProperty("instance").GetString());
        }
        await using var db = sql.CreateDbContext();
        var sessions = await db.Sessions.AsNoTracking().Where(s => s.UserId == actor.Id).ToArrayAsync();
        Assert.Equal(successes, sessions.Length);
        var tokens = await db.RefreshTokens.AsNoTracking().Where(t => t.Session.UserId == actor.Id).ToArrayAsync();
        Assert.Equal(successes, tokens.Length);
        Assert.All(sessions, s => Assert.Single(tokens.Where(t => t.SessionId == s.Id)));
        Assert.Equal(successes, tokens.Select(t => t.TokenHash).Distinct().Count());
        var current = await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id);
        Assert.NotNull(current.LastLoginAt);
        Assert.InRange(current.LastLoginAt!.Value, started.AddSeconds(-2), DateTimeOffset.UtcNow.AddSeconds(2));
        Assert.NotEqual(Convert.ToBase64String(actor.RowVersion), Convert.ToBase64String(current.RowVersion));
        Assert.Empty(await db.AuditLogs.Where(a => a.ActorUserId == actor.Id).ToArrayAsync());
        Assert.Empty(await db.IdempotencyRecords.Where(r => r.ActorUserId == actor.Id).ToArrayAsync());
        // Revoking old credentials does not disable a new credential exchange.
        var revokedAt = DateTimeOffset.UtcNow;
        await db.Sessions.Where(s => s.UserId == actor.Id).ExecuteUpdateAsync(s => s.SetProperty(v => v.RevokedAt, revokedAt));
        await db.RefreshTokens.Where(t => t.Session.UserId == actor.Id).ExecuteUpdateAsync(t => t.SetProperty(v => v.RevokedAt, revokedAt));
        await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u.SetProperty(v => v.MustChangePassword, true));
        using var next = await first.PostAsJsonAsync(route, new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, next.StatusCode);
        Assert.True((await next.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mustChangePassword").GetBoolean());
        var allSessions = await db.Sessions.AsNoTracking().Where(s => s.UserId == actor.Id).ToArrayAsync();
        Assert.Equal(successes + 1, allSessions.Length);
        Assert.Single(allSessions.Where(s => s.RevokedAt == null));
        Assert.All(allSessions.Where(s => sessions.Select(x => x.Id).Contains(s.Id)), s => Assert.NotNull(s.RevokedAt));
        Assert.Equal(successes + 1, await db.RefreshTokens.CountAsync(t => t.Session.UserId == actor.Id));
        Assert.Single(await db.RefreshTokens.Where(t => t.Session.UserId == actor.Id && t.RevokedAt == null).ToArrayAsync());
        Assert.Empty(await db.AuditLogs.Where(a => a.ActorUserId == actor.Id).ToArrayAsync());
        Assert.Empty(await db.IdempotencyRecords.Where(r => r.ActorUserId == actor.Id).ToArrayAsync());
        foreach (var response in responses) response.Dispose();
    }
}
