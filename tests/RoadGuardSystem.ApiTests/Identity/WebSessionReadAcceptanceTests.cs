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
public sealed class WebSessionReadAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(UserRoleCode.Supervisor, "SUPERVISOR")]
    [InlineData(UserRoleCode.ProjectManager, "PM")]
    [InlineData(UserRoleCode.DroneOperator, "OPERATOR")]
    [InlineData(UserRoleCode.RepairCrew, "CREW")]
    [InlineData(UserRoleCode.Reporter, "REPORTER")]
    public async Task ConcurrentSessionReadsReflectCurrentActorAndOnlyTouchItsSession(UserRoleCode role, string wireRole)
    {
        var actor = await sql.CreateUserAsync("SESSION-" + Guid.NewGuid().ToString("N"), "Current1!", role);
        var other = await sql.CreateUserAsync("OTHER-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(clock); });
        using var client = await WebCookieTestClient.Login(factory, actor.Email!);
        using var outsider = await WebCookieTestClient.Login(factory, other.Email!);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        await using var db = sql.CreateDbContext();
        var before = await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == actor.Id);
        var otherBefore = await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == other.Id);
        var sessions = await db.Sessions.CountAsync();
        var tokens = await db.RefreshTokens.CountAsync();
        var audits = await db.AuditLogs.CountAsync();
        var receipts = await db.IdempotencyRecords.CountAsync();
        await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u
            .SetProperty(v => v.DisplayName, "Current session projection")
            .SetProperty(v => v.MustChangePassword, true));
        var current = await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id);
        clock.Now += TimeSpan.FromMinutes(2);
        var responses = await Task.WhenAll(client.GetAsync("/api/v1/auth/web/session"), client.GetAsync("/api/v1/auth/web/session"));
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
                var user = body.GetProperty("user");
                Keys(user, "id", "displayName", "role", "version");
                Assert.Equal(actor.Id, user.GetProperty("id").GetGuid());
                Assert.Equal(current.DisplayName, user.GetProperty("displayName").GetString());
                Assert.Equal(wireRole, user.GetProperty("role").GetString());
                Assert.Equal(Convert.ToBase64String(current.RowVersion), user.GetProperty("version").GetString());
                Assert.True(body.GetProperty("mustChangePassword").GetBoolean());
                Assert.Equal(before.IssuedAt, body.GetProperty("issuedAt").GetDateTimeOffset());
                Assert.True(body.GetProperty("renewable").GetBoolean());
                Assert.Equal(43200, body.GetProperty("ticketLifetimeSeconds").GetInt32());
                Assert.Equal(JsonValueKind.Null, body.GetProperty("absoluteExpiresAt").ValueKind);
                Assert.Equal(JsonValueKind.Null, body.GetProperty("idleExpiresAt").ValueKind);
                Assert.DoesNotContain(other.Email!, body.GetRawText());
                Assert.DoesNotContain(actor.Email!, body.GetRawText());
                Assert.DoesNotContain("passwordHash", body.GetRawText());
                Assert.DoesNotContain("securityStamp", body.GetRawText());
            }
        }
        var after = await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == before.Id);
        Assert.Equal(clock.Now, after.LastActivityAt);
        Assert.NotEqual(Convert.ToBase64String(before.RowVersion), Convert.ToBase64String(after.RowVersion));
        Assert.Equal(before.IssuedAt, after.IssuedAt);
        Assert.Equal(before.RevokedAt, after.RevokedAt);
        Assert.Equal(otherBefore.RowVersion, (await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == otherBefore.Id)).RowVersion);
        Assert.Equal(current.RowVersion, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id)).RowVersion);
        Assert.Equal(sessions, await db.Sessions.CountAsync());
        Assert.Equal(tokens, await db.RefreshTokens.CountAsync());
        Assert.Equal(audits, await db.AuditLogs.CountAsync());
        Assert.Equal(receipts, await db.IdempotencyRecords.CountAsync());
    }

    private static void Keys(JsonElement body, params string[] names) => Assert.Equal(names.OrderBy(x => x), body.EnumerateObject().Select(x => x.Name).OrderBy(x => x));
    private sealed class MutableClock(DateTimeOffset initial) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = initial;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
