using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Collection(AuthenticationApiFixture.Name)]
public sealed class RefreshOwnerAuditAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("API-003", "/api/v1/auth/android/")]
    [InlineData("API-009", "/api/v1/auth/")]
    public async Task SharedKeyStaysCredentialScopedAndOldTokenReplayAuditsOnlyItsFamily(string operation, string prefix)
    {
        Assert.StartsWith("API-", operation);
        var actor = await sql.CreateUserAsync("OWNER-A-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var other = await sql.CreateUserAsync("OWNER-B-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var original = await Login(actor.Email!); var sibling = await Login(actor.Email!); var foreign = await Login(other.Email!);
        await using var db = sql.CreateDbContext();
        var key = Guid.NewGuid().ToString("N");
        var first = await Rotate(original, actor.Id); var second = await Rotate(sibling, actor.Id); var third = await Rotate(foreign, other.Id);
        Assert.Equal(3, new[] { first.Session, second.Session, third.Session }.Distinct().Count());
        Assert.Equal(3, new[] { first.Credential, second.Credential, third.Credential }.Distinct().Count());
        var receipts = await db.IdempotencyRecords.AsNoTracking().Where(r => r.Operation == "RefreshRotation" &&
            (r.ActorUserId == actor.Id || r.ActorUserId == other.Id)).ToArrayAsync();
        Assert.Equal(3, receipts.Length); Assert.Equal(2, receipts.Count(r => r.ActorUserId == actor.Id));
        Assert.Equal(3, receipts.Select(r => r.IdempotencyKey).Distinct().Count());
        Assert.All(receipts, r => { Assert.EndsWith(":" + key, r.IdempotencyKey); Assert.Null(r.ProjectId); });
        var before = await Snapshot();
        Assert.Equal(first, await Rotate(original, actor.Id));
        Assert.Equal(second, await Rotate(sibling, actor.Id)); Assert.Equal(third, await Rotate(foreign, other.Id));
        Assert.Equal(before, await Snapshot());
        var protectedFamilies = await OtherFamilies();
        var started = DateTimeOffset.UtcNow; var correlation = Guid.NewGuid().ToString();
        await Reuse(correlation);
        Assert.Equal(protectedFamilies, await OtherFamilies());
        var revoked = await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == first.Session);
        Assert.NotNull(revoked.RevokedAt); Assert.InRange(revoked.RevokedAt.Value, started, DateTimeOffset.UtcNow);
        var familyTokens = await db.RefreshTokens.AsNoTracking().Where(t => t.SessionId == first.Session).ToArrayAsync();
        Assert.Equal(2, familyTokens.Length); Assert.All(familyTokens, t => Assert.NotNull(t.RevokedAt));
        var audit = Assert.Single(await db.AuditLogs.AsNoTracking().Where(a => a.EntityId == first.Session).ToArrayAsync());
        Assert.Equal("auth_token_replay_detected", audit.EventType); Assert.Null(audit.ActorUserId);
        Assert.Equal("Session", audit.EntityType); Assert.Equal("IdentityRepository", audit.Source);
        Assert.Equal(Guid.Parse(correlation), audit.CorrelationId); Assert.InRange(audit.OccurredAtUtc, started, DateTimeOffset.UtcNow);
        Assert.Null(audit.BeforeSnapshot); Assert.Null(audit.AfterSnapshot); Assert.Null(audit.Reason);
        var receipt = Assert.Single(await db.IdempotencyRecords.AsNoTracking().Where(r => r.Operation == "RefreshTokenReplay" &&
            r.OutcomeJson.Contains(first.Session.ToString("N"))).ToArrayAsync());
        Assert.Null(receipt.ActorUserId); Assert.Null(receipt.ProjectId);
        var outcome = JsonSerializer.Deserialize<JsonElement>(receipt.OutcomeJson);
        Assert.Equal("family_revoked", outcome.GetProperty("status").GetString());
        Assert.Equal(first.Session.ToString("N"), outcome.GetProperty("session_id").GetString());
        var committed = await Snapshot();
        await Reuse(Guid.NewGuid().ToString()); Assert.Equal(committed, await Snapshot());
        Assert.Equal(protectedFamilies, await OtherFamilies());
        Assert.Equal(second, await Rotate(sibling, actor.Id)); Assert.Equal(third, await Rotate(foreign, other.Id));

        async Task Reuse(string requestId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, prefix + "refresh") { Content = JsonContent.Create(new { refreshToken = original }) };
            request.Headers.Add("X-Correlation-ID", requestId);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            var text = await response.Content.ReadAsStringAsync(); var body = JsonSerializer.Deserialize<JsonElement>(text);
            Assert.Equal(401, body.GetProperty("status").GetInt32()); Assert.Equal("auth_session_revoked", body.GetProperty("code").GetString());
            Assert.Equal(prefix + "refresh", body.GetProperty("instance").GetString());
            Assert.Equal(requestId, body.GetProperty("correlationId").GetString());
            Assert.Equal(requestId, Assert.Single(response.Headers.GetValues("X-Correlation-ID")));
            Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location); Assert.False(response.Headers.Contains("Set-Cookie"));
            foreach (var secret in new[] { original, sibling, foreign, first.Credential, second.Credential, third.Credential, actor.Email!, other.Email! })
                Assert.DoesNotContain(secret, text, StringComparison.Ordinal);
            Assert.False(body.TryGetProperty("accessToken", out _)); Assert.False(body.TryGetProperty("refreshToken", out _));
        }
        async Task<(Guid Session, string Credential)> Rotate(string credential, Guid owner)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, prefix + "refresh") { Content = JsonContent.Create(new { refreshToken = credential }) };
            request.Headers.Add("Idempotency-Key", key);
            using var response = await client.SendAsync(request); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(owner, body.GetProperty("user").GetProperty("id").GetGuid());
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.GetProperty("accessToken").GetString());
            Assert.Equal(owner.ToString(), jwt.Claims.Single(c => c.Type == "sub").Value);
            return (Guid.Parse(jwt.Claims.Single(c => c.Type == "sid").Value), body.GetProperty("refreshToken").GetString()!);
        }
        async Task<string> Login(string email)
        {
            using var response = await client.PostAsJsonAsync(prefix + "login", new { email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString()!;
        }
        async Task<string> OtherFamilies() => JsonSerializer.Serialize(new
        {
            sessions = await db.Sessions.AsNoTracking().Where(s => s.Id == second.Session || s.Id == third.Session).OrderBy(s => s.Id).ToArrayAsync(),
            tokens = await db.RefreshTokens.AsNoTracking().Where(t => t.SessionId == second.Session || t.SessionId == third.Session).OrderBy(t => t.Id).ToArrayAsync()
        });
        async Task<string> Snapshot() => JsonSerializer.Serialize(new
        {
            sessions = await db.Sessions.AsNoTracking().Where(s => s.UserId == actor.Id || s.UserId == other.Id).OrderBy(s => s.Id).ToArrayAsync(),
            tokens = await db.RefreshTokens.AsNoTracking().Where(t => t.Session.UserId == actor.Id || t.Session.UserId == other.Id).OrderBy(t => t.Id).ToArrayAsync(),
            receipts = await db.IdempotencyRecords.AsNoTracking().Where(r => r.ActorUserId == actor.Id || r.ActorUserId == other.Id ||
                r.Operation == "RefreshTokenReplay" && r.OutcomeJson.Contains(first.Session.ToString("N"))).OrderBy(r => r.Id).ToArrayAsync(),
            audits = await db.AuditLogs.AsNoTracking().Where(a => a.EntityId == first.Session || a.EntityId == second.Session || a.EntityId == third.Session).OrderBy(a => a.Id).ToArrayAsync()
        });
    }
}
