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
public sealed class CsrfSubjectBoundaryAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task AuthenticatedBootstrapBindsSubjectAndRejectsOtherActorsToken()
    {
        var a = await sql.CreateUserAsync("CSRF-A-" + Guid.NewGuid().ToString("N"), "Current1!");
        var b = await sql.CreateUserAsync("CSRF-B-" + Guid.NewGuid().ToString("N"), "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var first = await WebCookieTestClient.Login(factory, a.Email!);
        using var second = await WebCookieTestClient.Login(factory, b.Email!);
        var firstToken = first.DefaultRequestHeaders.GetValues("X-CSRF-TOKEN").Single();
        var secondToken = second.DefaultRequestHeaders.GetValues("X-CSRF-TOKEN").Single();
        await using var db = sql.CreateDbContext();
        var count = await db.Sessions.CountAsync();
        var audit = await db.AuditLogs.CountAsync();
        using var bootstrap = await first.GetAsync("/api/v1/auth/web/csrf");
        Assert.Equal(HttpStatusCode.OK, bootstrap.StatusCode);
        Assert.True(bootstrap.Headers.CacheControl?.NoStore);
        var wire = await bootstrap.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "headerName", "requestToken" }, wire.EnumerateObject().Select(x => x.Name).OrderBy(x => x));
        Assert.Equal("X-CSRF-TOKEN", wire.GetProperty("headerName").GetString());
        Assert.Equal(count, await db.Sessions.CountAsync());
        Assert.Equal(audit, await db.AuditLogs.CountAsync());
        second.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        second.DefaultRequestHeaders.Add("X-CSRF-TOKEN", firstToken);
        second.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var denied = await second.PostAsync("/api/v1/auth/web/logout", null);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("csrf_failed", (await denied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.All(await db.Sessions.Where(s => s.UserId == a.Id || s.UserId == b.Id).ToArrayAsync(), s => Assert.Null(s.RevokedAt));
        second.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        second.DefaultRequestHeaders.Add("X-CSRF-TOKEN", secondToken);
        using var accepted = await second.PostAsync("/api/v1/auth/web/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);
        Assert.NotNull((await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == b.Id)).RevokedAt);
        Assert.Null((await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == a.Id)).RevokedAt);
    }

    [Theory]
    [InlineData("revoked")]
    [InlineData("suspended")]
    [InlineData("role-replaced")]
    [InlineData("role-inactive")]
    [InlineData("expired")]
    public async Task InvalidatedCookieBootstrapsAnonymousTokenWithoutRestoringAuthority(string change)
    {
        var actor = await sql.CreateUserAsync("CSRF-STATE-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(clock); });
        using var client = await WebCookieTestClient.Login(factory, actor.Email!);
        await using var db = sql.CreateDbContext();
        var session = await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == actor.Id);
        var audits = await db.AuditLogs.CountAsync();
        var receipts = await db.IdempotencyRecords.CountAsync();
        try
        {
            switch (change)
            {
                case "revoked": await db.Sessions.Where(s => s.Id == session.Id).ExecuteUpdateAsync(s => s.SetProperty(v => v.RevokedAt, clock.Now)); break;
                case "suspended": await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u.SetProperty(v => v.Status, UserStatus.Suspended)); break;
                case "role-replaced": await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u.SetProperty(v => v.RoleCode, UserRoleCode.RepairCrew)); break;
                case "role-inactive": await db.Roles.Where(r => r.Code == UserRoleCode.Reporter).ExecuteUpdateAsync(r => r.SetProperty(v => v.IsActive, false)); break;
                case "expired": clock.Now += TimeSpan.FromHours(13); break;
            }
            var before = await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == session.Id);
            var userBefore = await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id);
            using var bootstrap = await client.GetAsync("/api/v1/auth/web/csrf");
            Assert.Equal(HttpStatusCode.OK, bootstrap.StatusCode);
            Assert.True(bootstrap.Headers.CacheControl?.NoStore);
            Assert.Null(bootstrap.Headers.ETag);
            var body = await bootstrap.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(new[] { "headerName", "requestToken" }, body.EnumerateObject().Select(x => x.Name).OrderBy(x => x));
            Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("requestToken").GetString()));
            if (bootstrap.Headers.TryGetValues("Set-Cookie", out var cookies))
                Assert.All(cookies, h => { Assert.False(h.StartsWith("__Host-RoadGuardSession=", StringComparison.Ordinal)); Assert.False(h.StartsWith("__Host-RoadGuardRenewal=", StringComparison.Ordinal)); });
            using var status = await client.GetAsync("/api/v1/auth/web/session");
            Assert.Equal(HttpStatusCode.Unauthorized, status.StatusCode);
            var after = await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == session.Id);
            if (change == "role-replaced")
            {
                // Existing validator durably revokes the obsolete issued-role family.
                Assert.NotEqual(Convert.ToBase64String(before.RowVersion), Convert.ToBase64String(after.RowVersion));
                Assert.NotNull(after.RevokedAt);
                Assert.All(await db.RefreshTokens.AsNoTracking().Where(t => t.SessionId == session.Id).ToArrayAsync(), t => Assert.NotNull(t.RevokedAt));
            }
            else
            {
                Assert.Equal(before.RowVersion, after.RowVersion);
                Assert.Equal(before.RevokedAt, after.RevokedAt);
            }
            Assert.Equal(before.LastActivityAt, after.LastActivityAt);
            Assert.Equal(userBefore.RowVersion, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id)).RowVersion);
            Assert.Single(await db.Sessions.Where(s => s.UserId == actor.Id).ToArrayAsync());
            Assert.Single(await db.RefreshTokens.Where(t => t.Session.UserId == actor.Id).ToArrayAsync());
            Assert.Equal(audits, await db.AuditLogs.CountAsync());
            Assert.Equal(receipts, await db.IdempotencyRecords.CountAsync());
        }
        finally
        {
            if (change == "role-inactive")
                await db.Roles.Where(r => r.Code == UserRoleCode.Reporter).ExecuteUpdateAsync(r => r.SetProperty(v => v.IsActive, true));
        }
    }

    private sealed class MutableClock(DateTimeOffset initial) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = initial;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
