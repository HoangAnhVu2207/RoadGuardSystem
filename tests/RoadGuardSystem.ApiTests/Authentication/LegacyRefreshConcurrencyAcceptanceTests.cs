using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Collection(AuthenticationApiFixture.Name)]
public sealed class LegacyRefreshConcurrencyAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task SameKeyRaceReturnsOneSuccessorAndUnkeyedRaceRevokesOnlyItsFamily()
    {
        var actor = await sql.CreateUserAsync("REFRESH-RACE-" + Guid.NewGuid().ToString("N"), "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var credential = await Login(); var key = Guid.NewGuid().ToString("N");
        var results = await Task.WhenAll(Refresh(credential, key), Refresh(credential, key));
        string? successor = null;
        foreach (var response in results)
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(actor.Id, body.GetProperty("user").GetProperty("id").GetGuid());
                var returned = body.GetProperty("refreshToken").GetString();
                if (successor is null) successor = returned; else Assert.Equal(successor, returned);
            }
        }
        await using var db = sql.CreateDbContext();
        var firstSession = await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == actor.Id);
        var firstTokens = await db.RefreshTokens.AsNoTracking().Where(t => t.SessionId == firstSession.Id).OrderBy(t => t.Id).ToArrayAsync();
        Assert.Equal(2, firstTokens.Length); Assert.Single(firstTokens, t => t.RevokedAt is null);
        Assert.Single(await db.IdempotencyRecords.Where(r => r.ActorUserId == actor.Id && r.Operation == "RefreshRotation").ToArrayAsync());
        Assert.Empty(await db.AuditLogs.Where(a => a.EntityId == firstSession.Id).ToArrayAsync());
        var secondCredential = await Login();
        results = await Task.WhenAll(Refresh(secondCredential, null), Refresh(secondCredential, null));
        try
        {
            Assert.Single(results, r => r.StatusCode == HttpStatusCode.OK);
            var denied = Assert.Single(results, r => r.StatusCode == HttpStatusCode.Unauthorized);
            Assert.Equal("auth_session_revoked", (await denied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
        finally { foreach (var response in results) response.Dispose(); }
        var secondSession = await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == actor.Id && s.Id != firstSession.Id);
        Assert.NotNull(secondSession.RevokedAt);
        var secondTokens = await db.RefreshTokens.AsNoTracking().Where(t => t.SessionId == secondSession.Id).ToArrayAsync();
        Assert.Equal(2, secondTokens.Length); Assert.All(secondTokens, t => Assert.NotNull(t.RevokedAt));
        Assert.Equal(JsonSerializer.Serialize(firstTokens), JsonSerializer.Serialize(await db.RefreshTokens.AsNoTracking()
            .Where(t => t.SessionId == firstSession.Id).OrderBy(t => t.Id).ToArrayAsync()));
        Assert.Equal(firstSession.RowVersion, (await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == firstSession.Id)).RowVersion);
        Assert.Single(await db.AuditLogs.Where(a => a.EntityId == secondSession.Id && a.EventType == "auth_token_replay_detected").ToArrayAsync());
        Assert.Single(await db.IdempotencyRecords.Where(r => r.Operation == "RefreshTokenReplay" && r.OutcomeJson.Contains(secondSession.Id.ToString("N"))).ToArrayAsync());

        async Task<string> Login()
        {
            using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString()!;
        }
        async Task<HttpResponseMessage> Refresh(string original, string? operationKey)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh") { Content = JsonContent.Create(new { refreshToken = original }) };
            if (operationKey is not null) request.Headers.Add("Idempotency-Key", operationKey);
            return await client.SendAsync(request);
        }
    }
}
