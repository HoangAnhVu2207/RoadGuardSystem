using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Identity;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

// Separate owned database: changing the role catalog must not affect other HTTP fixtures.
public sealed class RefreshLifecycleAcceptanceTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    [Theory]
    [InlineData("API-003", "/api/v1/auth/android/")]
    [InlineData("API-009", "/api/v1/auth/")]
    public async Task RefreshRechecksRoleTransportAndBoundedSessionWithoutExtendingOriginalExpiry(string operation, string prefix)
    {
        var actor = await sql.CreateUserAsync("REFRESH-LIFE-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var now = DateTimeOffset.UtcNow;
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services => { services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(new FixedClock(now)); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var login = await client.PostAsJsonAsync(prefix + "login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var credential = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString()!;
        await using var db = sql.CreateDbContext();
        var session = await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == actor.Id);
        try
        {
            await db.Roles.Where(r => r.Code == actor.RoleCode).ExecuteUpdateAsync(r => r.SetProperty(v => v.IsActive, false));
            await Denied("auth_unauthorized");
        }
        finally { await db.Roles.Where(r => r.Code == actor.RoleCode).ExecuteUpdateAsync(r => r.SetProperty(v => v.IsActive, true)); }
        await Fixture(SessionLifecycle.PersistentRenewable, SessionTransport.Android, null, null, UserRoleCode.Reporter);
        await Denied("auth_unauthorized");
        if (operation == "API-003")
        {
            foreach (var transport in new[] { SessionTransport.LegacyBearer, SessionTransport.Web })
            {
                await Fixture(SessionLifecycle.PersistentRenewable, transport, null, null, actor.RoleCode);
                await Denied("auth_session_revoked");
            }
        }
        await Fixture(SessionLifecycle.LegacyBounded, SessionTransport.Android, now, now.AddDays(1), actor.RoleCode);
        await Denied("auth_session_revoked");
        var cap = now.AddMinutes(5);
        var beforeInvalidFixture = await Snapshot();
        var invalidFixture = await Assert.ThrowsAsync<DbUpdateException>(() =>
            Fixture(SessionLifecycle.LegacyBounded, SessionTransport.Android, cap, null, actor.RoleCode));
        Assert.Contains("Non-expiring credentials require a persistent session", invalidFixture.InnerException!.Message);
        db.ChangeTracker.Clear(); Assert.Equal(beforeInvalidFixture, await Snapshot());
        await Fixture(SessionLifecycle.LegacyBounded, SessionTransport.Android, cap, now.AddDays(1), actor.RoleCode);
        using var response = await client.PostAsJsonAsync(prefix + "refresh", new { refreshToken = credential });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(actor.Id, body.GetProperty("user").GetProperty("id").GetGuid());
        Assert.Equal(900, body.GetProperty("expiresIn").GetInt32()); // Android access lifetime; current parent authority is checked separately.
        var retained = await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == session.Id);
        Assert.Equal(session.IssuedAt, retained.IssuedAt); Assert.Equal(cap, retained.ExpiresAt); Assert.Null(retained.RevokedAt);
        var tokens = await db.RefreshTokens.AsNoTracking().Where(t => t.SessionId == session.Id).ToArrayAsync();
        Assert.Equal(2, tokens.Length); Assert.Equal(cap, Assert.Single(tokens, t => t.RevokedAt == null).ExpiresAt);

        async Task Fixture(SessionLifecycle lifecycle, SessionTransport transport, DateTimeOffset? expiry, DateTimeOffset? tokenExpiry, UserRoleCode issuedRole)
        {
            // Initial domain fixtures, never mutate immutable session lifecycle/role or disable SQL guards.
            credential = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            session = new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = actor.Id,
                IssuedAt = now.AddMinutes(-1),
                Lifecycle = lifecycle,
                Transport = transport,
                ExpiresAt = expiry,
                IssuedRole = issuedRole
            };
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                ExpiresAt = tokenExpiry,
                TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(credential))).ToLowerInvariant()
            };
            db.AddRange(session, token); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        }

        async Task Denied(string code)
        {
            var before = await Snapshot();
            using var denied = await client.PostAsJsonAsync(prefix + "refresh", new { refreshToken = credential });
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
            Assert.Equal("application/problem+json", denied.Content.Headers.ContentType!.MediaType);
            var raw = await denied.Content.ReadAsStringAsync(); var error = JsonSerializer.Deserialize<JsonElement>(raw);
            Assert.Equal(code, error.GetProperty("code").GetString()); Assert.Equal(401, error.GetProperty("status").GetInt32());
            Assert.Equal(prefix + "refresh", error.GetProperty("instance").GetString());
            Assert.Equal(Assert.Single(denied.Headers.GetValues("X-Correlation-ID")), error.GetProperty("correlationId").GetString());
            Assert.DoesNotContain(credential, raw, StringComparison.Ordinal); Assert.DoesNotContain(actor.Email!, raw, StringComparison.Ordinal);
            Assert.False(denied.Headers.Contains("Set-Cookie")); Assert.Null(denied.Headers.ETag);
            Assert.Equal(before, await Snapshot());
        }
        async Task<string> Snapshot() => JsonSerializer.Serialize(new
        {
            sessions = await db.Sessions.AsNoTracking().Where(s => s.UserId == actor.Id).OrderBy(s => s.Id).ToArrayAsync(),
            tokens = await db.RefreshTokens.AsNoTracking().Where(t => t.Session.UserId == actor.Id).OrderBy(t => t.Id).ToArrayAsync(),
            receipts = await db.IdempotencyRecords.AsNoTracking().OrderBy(r => r.Id).ToArrayAsync(),
            audits = await db.AuditLogs.AsNoTracking().OrderBy(a => a.Id).ToArrayAsync()
        });
    }
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => now; }
}
