using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class LogoutKeyScopeAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(UserRoleCode.Supervisor)]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.DroneOperator)]
    [InlineData(UserRoleCode.RepairCrew)]
    [InlineData(UserRoleCode.Reporter)]
    public async Task LogoutConcurrentReplayConflictsForAnotherSessionButKeyIsActorScoped(UserRoleCode role)
    {
        var actor = await sql.CreateUserAsync("LOGOUT-" + Guid.NewGuid().ToString("N"), "Current1!", role);
        var other = await sql.CreateUserAsync("LOGOUT-OTHER-" + Guid.NewGuid().ToString("N"), "Current1!", role);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var first = factory.CreateClient(); using var second = factory.CreateClient(); using var outsider = factory.CreateClient();
        async Task Login(HttpClient client, string email)
        {
            using var result = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var body = await result.Content.ReadFromJsonAsync<JsonElement>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        }
        await Login(first, actor.Email!); await Login(second, actor.Email!); await Login(outsider, other.Email!);
        var key = Guid.NewGuid().ToString("N");
        foreach (var client in new[] { first, second, outsider }) client.DefaultRequestHeaders.Add("Idempotency-Key", key);
        var results = await Task.WhenAll(first.PostAsync("/api/v1/auth/logout", null), first.PostAsync("/api/v1/auth/logout", null));
        foreach (var response in results)
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                Assert.Empty(await response.Content.ReadAsByteArrayAsync()); Assert.Null(response.Headers.ETag);
            }
        }
        using var conflict = await second.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("application/problem+json", conflict.Content.Headers.ContentType?.MediaType);
        var problem = await conflict.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, problem.GetProperty("status").GetInt32());
        Assert.Equal("idempotency_key_reused", problem.GetProperty("code").GetString());
        Assert.DoesNotContain(actor.Email!, problem.GetRawText()); Assert.DoesNotContain("refreshToken", problem.GetRawText());
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.GetAsync("/api/v1/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await second.GetAsync("/api/v1/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await outsider.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        await using var db = sql.CreateDbContext();
        var sessions = await db.Sessions.AsNoTracking().Where(x => x.UserId == actor.Id).ToListAsync();
        Assert.Equal(2, sessions.Count); var revoked = Assert.Single(sessions, x => x.RevokedAt != null);
        var active = Assert.Single(sessions, x => x.RevokedAt == null);
        Assert.All(await db.RefreshTokens.AsNoTracking().Where(x => x.SessionId == revoked.Id).ToListAsync(), x => Assert.Equal(revoked.RevokedAt, x.RevokedAt));
        Assert.All(await db.RefreshTokens.AsNoTracking().Where(x => x.SessionId == active.Id).ToListAsync(), x => Assert.Null(x.RevokedAt));
        var receipt = Assert.Single(await db.IdempotencyRecords.AsNoTracking().Where(x => x.ActorUserId == actor.Id && x.Operation == "Logout").ToListAsync());
        Assert.Equal(key, receipt.IdempotencyKey);
        var audit = Assert.Single(await db.AuditLogs.AsNoTracking().Where(x => x.ActorUserId == actor.Id && x.EventType == "auth_logout").ToListAsync());
        Assert.Equal(revoked.Id, audit.EntityId); Assert.Null(audit.BeforeSnapshot); Assert.Null(audit.AfterSnapshot);
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(x => x.ActorUserId == other.Id && x.Operation == "Logout"));
    }
}
