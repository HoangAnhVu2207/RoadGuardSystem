using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class WebRenewAuthorityAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("session-revoked")]
    [InlineData("credential-revoked")]
    [InlineData("suspended")]
    [InlineData("role-replaced")]
    [InlineData("role-inactive")]
    public async Task RenewalRechecksCurrentAuthorityAndNeverCreatesReplacementOnDenial(string change)
    {
        var actor = await sql.CreateUserAsync("RENEW-STATE-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await WebCookieTestClient.Login(factory, actor.Email!);
        await using var db = sql.CreateDbContext();
        var session = await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == actor.Id);
        var audits = await db.AuditLogs.CountAsync(); var receipts = await db.IdempotencyRecords.CountAsync();
        try
        {
            switch (change)
            {
                case "session-revoked": await db.Sessions.Where(s => s.Id == session.Id).ExecuteUpdateAsync(s => s.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow)); break;
                case "credential-revoked": await db.RefreshTokens.Where(t => t.SessionId == session.Id).ExecuteUpdateAsync(t => t.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow)); break;
                case "suspended": await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u.SetProperty(v => v.Status, UserStatus.Suspended)); break;
                case "role-replaced": await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u.SetProperty(v => v.RoleCode, UserRoleCode.RepairCrew)); break;
                case "role-inactive": await db.Roles.Where(r => r.Code == UserRoleCode.Reporter).ExecuteUpdateAsync(r => r.SetProperty(v => v.IsActive, false)); break;
            }
            await WebCookieTestClient.RefreshCsrf(client);
            var before = await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == session.Id);
            var current = await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id);
            using var response = await client.PostAsync("/api/v1/auth/web/renew", null);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("auth_session_revoked", body.GetProperty("code").GetString());
            Assert.Equal(401, body.GetProperty("status").GetInt32());
            Assert.DoesNotContain(actor.Email!, body.GetRawText());
            Assert.DoesNotContain("refreshToken", body.GetRawText());
            Assert.DoesNotContain("accessToken", body.GetRawText());
            Assert.Null(response.Headers.ETag);
            Assert.False(response.Headers.Contains("Set-Cookie"));
            var after = await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == session.Id);
            Assert.Equal(before.RowVersion, after.RowVersion);
            Assert.Equal(before.LastActivityAt, after.LastActivityAt);
            Assert.Equal(current.RowVersion, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id)).RowVersion);
            Assert.Single(await db.Sessions.Where(s => s.UserId == actor.Id).ToArrayAsync());
            Assert.Single(await db.RefreshTokens.Where(t => t.SessionId == session.Id).ToArrayAsync());
            Assert.Equal(audits, await db.AuditLogs.CountAsync()); Assert.Equal(receipts, await db.IdempotencyRecords.CountAsync());
        }
        finally
        {
            if (change == "role-inactive") await db.Roles.Where(r => r.Code == UserRoleCode.Reporter).ExecuteUpdateAsync(r => r.SetProperty(v => v.IsActive, true));
        }
    }
}
