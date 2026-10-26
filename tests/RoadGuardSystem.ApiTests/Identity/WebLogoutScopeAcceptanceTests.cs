using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class WebLogoutScopeAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(UserRoleCode.Supervisor)]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.DroneOperator)]
    [InlineData(UserRoleCode.RepairCrew)]
    [InlineData(UserRoleCode.Reporter)]
    public async Task OriginalCookieReplaysCommittedLogoutAndDifferentSessionConflictsWithoutRevocation(UserRoleCode role)
    {
        var actor = await sql.CreateUserAsync("WEB-LOGOUT-" + Guid.NewGuid().ToString("N"), "Current1!", role);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var first = await Login(factory, actor.Email!); using var second = await Login(factory, actor.Email!);
        var key = Guid.NewGuid().ToString("N");
        first.DefaultRequestHeaders.Add("Idempotency-Key", key); second.DefaultRequestHeaders.Add("Idempotency-Key", key);
        var responses = await Task.WhenAll(first.PostAsync("/api/v1/auth/web/logout", null), first.PostAsync("/api/v1/auth/web/logout", null));
        foreach (var response in responses.Append(await first.PostAsync("/api/v1/auth/web/logout", null)))
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                Assert.Empty(await response.Content.ReadAsByteArrayAsync()); Assert.True(response.Headers.CacheControl?.NoStore);
                Assert.Null(response.Headers.ETag);
                var cookies = response.Headers.GetValues("Set-Cookie").ToArray();
                foreach (var name in new[] { "__Host-RoadGuardSession=", "__Host-RoadGuardRenewal=" })
                {
                    var deleted = Assert.Single(cookies, c => c.StartsWith(name, StringComparison.Ordinal));
                    Assert.StartsWith(name + ";", deleted); Assert.Contains("expires=", deleted, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("secure", deleted, StringComparison.OrdinalIgnoreCase); Assert.Contains("path=/", deleted, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        using var conflict = await second.PostAsync("/api/v1/auth/web/logout", null);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode); Assert.True(conflict.Headers.CacheControl?.NoStore);
        Assert.Equal("application/problem+json", conflict.Content.Headers.ContentType?.MediaType);
        var problem = await conflict.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, problem.GetProperty("status").GetInt32()); Assert.Equal("idempotency_key_reused", problem.GetProperty("code").GetString());
        Assert.DoesNotContain(actor.Email!, problem.GetRawText()); Assert.False(conflict.Headers.Contains("Set-Cookie"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.GetAsync("/api/v1/auth/web/session")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await second.GetAsync("/api/v1/auth/web/session")).StatusCode);
        await using var db = sql.CreateDbContext();
        var sessions = await db.Sessions.AsNoTracking().Where(x => x.UserId == actor.Id).ToListAsync();
        Assert.Equal(2, sessions.Count); var revoked = Assert.Single(sessions, x => x.RevokedAt != null);
        var active = Assert.Single(sessions, x => x.RevokedAt == null);
        Assert.All(await db.RefreshTokens.AsNoTracking().Where(x => x.SessionId == revoked.Id).ToListAsync(), x => Assert.Equal(revoked.RevokedAt, x.RevokedAt));
        Assert.All(await db.RefreshTokens.AsNoTracking().Where(x => x.SessionId == active.Id).ToListAsync(), x => Assert.Null(x.RevokedAt));
        Assert.Single(await db.IdempotencyRecords.AsNoTracking().Where(x => x.ActorUserId == actor.Id && x.Operation == "Logout").ToListAsync());
        var audit = Assert.Single(await db.AuditLogs.AsNoTracking().Where(x => x.ActorUserId == actor.Id && x.EventType == "auth_logout").ToListAsync());
        Assert.Equal(revoked.Id, audit.EntityId); Assert.Null(audit.BeforeSnapshot); Assert.Null(audit.AfterSnapshot);
    }

    private static async Task<HttpClient> Login(AuthenticationWebApplicationFactory factory, string email)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var anonymous = await client.GetAsync("/api/v1/auth/web/csrf");
        var csrfCookie = Cookie(anonymous, "__Host-RoadGuardCsrf");
        client.DefaultRequestHeaders.Add("Cookie", csrfCookie);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await anonymous.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
        using var login = await client.PostAsJsonAsync("/api/v1/auth/web/login", new { email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = Cookie(login, "__Host-RoadGuardSession");
        client.DefaultRequestHeaders.Remove("Cookie"); client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("Cookie", session + "; " + csrfCookie);
        using var authorized = await client.GetAsync("/api/v1/auth/web/csrf");
        Assert.Equal(HttpStatusCode.OK, authorized.StatusCode);
        if (authorized.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(c => c.StartsWith("__Host-RoadGuardCsrf=", StringComparison.Ordinal)))
            csrfCookie = Cookie(authorized, "__Host-RoadGuardCsrf");
        client.DefaultRequestHeaders.Remove("Cookie"); client.DefaultRequestHeaders.Add("Cookie", session + "; " + csrfCookie);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await authorized.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
        return client;
    }
    private static string Cookie(HttpResponseMessage response, string name) => Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith(name + "=", StringComparison.Ordinal)).Split(';')[0];
}
