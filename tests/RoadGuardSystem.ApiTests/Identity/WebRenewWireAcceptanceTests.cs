using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class WebRenewWireAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(UserRoleCode.Supervisor, "SUPERVISOR")]
    [InlineData(UserRoleCode.ProjectManager, "PM")]
    [InlineData(UserRoleCode.DroneOperator, "OPERATOR")]
    [InlineData(UserRoleCode.RepairCrew, "CREW")]
    [InlineData(UserRoleCode.Reporter, "REPORTER")]
    public async Task ConcurrentRenewalsKeepOneCredentialAndReturnCurrentPrivateWire(UserRoleCode role, string wireRole)
    {
        var actor = await sql.CreateUserAsync("RENEW-" + Guid.NewGuid().ToString("N"), "Current1!", role);
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(clock); });
        using var client = await WebCookieTestClient.Login(factory, actor.Email!);
        await using var db = sql.CreateDbContext();
        var before = await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == actor.Id);
        var tokenBefore = await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.SessionId == before.Id);
        var sessions = await db.Sessions.CountAsync(); var tokens = await db.RefreshTokens.CountAsync();
        var audits = await db.AuditLogs.CountAsync(); var receipts = await db.IdempotencyRecords.CountAsync();
        await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u.SetProperty(v => v.DisplayName, "Current renewal identity"));
        var current = await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id);
        clock.Now += TimeSpan.FromMinutes(2);
        var responses = await Task.WhenAll(client.PostAsync("/api/v1/auth/web/renew", null), client.PostAsync("/api/v1/auth/web/renew", null));
        var renewalCookies = new List<string>();
        foreach (var response in responses)
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(response.Headers.CacheControl?.NoStore);
                Assert.Null(response.Headers.ETag);
                Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                Keys(body, "user", "mustChangePassword", "issuedAt", "absoluteExpiresAt", "idleExpiresAt", "renewable", "ticketLifetimeSeconds");
                var user = body.GetProperty("user"); Keys(user, "id", "displayName", "role", "version");
                Assert.Equal(actor.Id, user.GetProperty("id").GetGuid());
                Assert.Equal(current.DisplayName, user.GetProperty("displayName").GetString());
                Assert.Equal(wireRole, user.GetProperty("role").GetString());
                Assert.Equal(Convert.ToBase64String(current.RowVersion), user.GetProperty("version").GetString());
                Assert.False(body.GetProperty("mustChangePassword").GetBoolean());
                Assert.Equal(before.IssuedAt, body.GetProperty("issuedAt").GetDateTimeOffset());
                Assert.True(body.GetProperty("renewable").GetBoolean());
                Assert.Equal(43200, body.GetProperty("ticketLifetimeSeconds").GetInt32());
                Assert.Equal(JsonValueKind.Null, body.GetProperty("absoluteExpiresAt").ValueKind);
                Assert.Equal(JsonValueKind.Null, body.GetProperty("idleExpiresAt").ValueKind);
                Assert.DoesNotContain(actor.Email!, body.GetRawText());
                var setCookies = response.Headers.GetValues("Set-Cookie").ToArray();
                foreach (var prefix in new[] { "__Host-RoadGuardSession=", "__Host-RoadGuardRenewal=" })
                {
                    var cookie = Assert.Single(setCookies, c => c.StartsWith(prefix, StringComparison.Ordinal));
                    Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("path=/", cookie, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("domain=", cookie, StringComparison.OrdinalIgnoreCase);
                    if (prefix.Contains("Renewal", StringComparison.Ordinal)) renewalCookies.Add(cookie.Split(';')[0]);
                }
            }
        }
        Assert.Equal(renewalCookies[0], renewalCookies[1]);
        var after = await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == before.Id);
        Assert.Equal(clock.Now, after.LastActivityAt);
        Assert.Equal(before.IssuedAt, after.IssuedAt); Assert.Null(after.RevokedAt);
        Assert.NotEqual(Convert.ToBase64String(before.RowVersion), Convert.ToBase64String(after.RowVersion));
        var tokenAfter = await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.SessionId == before.Id);
        Assert.Equal(tokenBefore.RowVersion, tokenAfter.RowVersion);
        Assert.Equal(tokenBefore.TokenHash, tokenAfter.TokenHash);
        Assert.Equal(current.RowVersion, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id)).RowVersion);
        Assert.Equal(sessions, await db.Sessions.CountAsync()); Assert.Equal(tokens, await db.RefreshTokens.CountAsync());
        Assert.Equal(audits, await db.AuditLogs.CountAsync()); Assert.Equal(receipts, await db.IdempotencyRecords.CountAsync());
    }

    private static void Keys(JsonElement body, params string[] names) => Assert.Equal(names.OrderBy(x => x), body.EnumerateObject().Select(x => x.Name).OrderBy(x => x));
    private sealed class MutableClock(DateTimeOffset initial) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = initial;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
