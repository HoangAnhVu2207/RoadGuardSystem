using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Options;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class LogoutAuthorityBoundaryTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false, "missing")]
    [InlineData(true, "missing")]
    [InlineData(false, "malformed")]
    [InlineData(true, "malformed")]
    [InlineData(false, "expired")]
    [InlineData(true, "expired")]
    [InlineData(false, "revoked")]
    [InlineData(true, "revoked")]
    [InlineData(false, "suspended")]
    [InlineData(true, "suspended")]
    [InlineData(false, "role-replaced")]
    [InlineData(true, "role-replaced")]
    [InlineData(false, "role-inactive")]
    [InlineData(true, "role-inactive")]
    public async Task UncommittedLogoutKeyCannotBypassCredentialOrCurrentAuthority(bool web, string change)
    {
        var actor = await sql.CreateUserAsync("LOGOUT-DENY-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(clock); });
        using var client = web ? await WebCookieTestClient.Login(factory, actor.Email!) : factory.CreateClient();
        if (!web)
        {
            using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        }
        await using var db = sql.CreateDbContext();
        var original = await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == actor.Id);
        try
        {
            if (change == "revoked") await db.Sessions.Where(s => s.Id == original.Id).ExecuteUpdateAsync(s => s.SetProperty(v => v.RevokedAt, clock.Now));
            if (change == "suspended") await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u.SetProperty(v => v.Status, UserStatus.Suspended));
            if (change == "role-replaced") await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u.SetProperty(v => v.RoleCode, UserRoleCode.RepairCrew));
            if (change == "role-inactive") await db.Roles.Where(r => r.Code == UserRoleCode.Reporter).ExecuteUpdateAsync(r => r.SetProperty(v => v.IsActive, false));
            if (change == "expired")
            {
                if (web) clock.Now += TimeSpan.FromHours(13);
                else
                {
                    var options = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new AccessTokenFactory(options).Create(actor.Id, original.Id, actor.RoleCode, DateTimeOffset.UtcNow.AddMinutes(-20), 15));
                }
            }
            using var bare = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
            var sender = client;
            if (change is "missing" or "malformed")
            {
                sender = bare;
                if (change == "malformed")
                {
                    if (web) bare.DefaultRequestHeaders.Add("Cookie", "__Host-RoadGuardSession=tampered");
                    else bare.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tampered");
                }
            }
            sender.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
            var before = await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == original.Id);
            var tokenBefore = await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.SessionId == original.Id);
            var userBefore = await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id);
            var audits = await db.AuditLogs.CountAsync(); var receipts = await db.IdempotencyRecords.CountAsync();
            using var response = await sender.PostAsync(web ? "/api/v1/auth/web/logout" : "/api/v1/auth/logout", null);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(401, body.GetProperty("status").GetInt32());
            Assert.DoesNotContain(actor.Email!, body.GetRawText()); Assert.DoesNotContain("refreshToken", body.GetRawText());
            var after = await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == original.Id);
            var tokenAfter = await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.SessionId == original.Id);
            if (change == "role-replaced")
            {
                Assert.NotNull(after.RevokedAt); Assert.NotNull(tokenAfter.RevokedAt);
            }
            else
            {
                Assert.Equal(before.RowVersion, after.RowVersion); Assert.Equal(tokenBefore.RowVersion, tokenAfter.RowVersion);
            }
            Assert.Equal(userBefore.RowVersion, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id)).RowVersion);
            Assert.Equal(audits, await db.AuditLogs.CountAsync()); Assert.Equal(receipts, await db.IdempotencyRecords.CountAsync());
        }
        finally
        {
            if (change == "role-inactive") await db.Roles.Where(r => r.Code == UserRoleCode.Reporter).ExecuteUpdateAsync(r => r.SetProperty(v => v.IsActive, true));
        }
    }
    private sealed class MutableClock(DateTimeOffset now) : TimeProvider
    { public DateTimeOffset Now { get; set; } = now; public override DateTimeOffset GetUtcNow() => Now; }
}
