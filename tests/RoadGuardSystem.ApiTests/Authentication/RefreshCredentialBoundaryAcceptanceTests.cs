using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Collection(AuthenticationApiFixture.Name)]
public sealed class RefreshCredentialBoundaryAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("API-003", "/api/v1/auth/android/")]
    [InlineData("API-009", "/api/v1/auth/")]
    public async Task RefreshDeniesInvalidInputAndCurrentCredentialStateWithoutRotation(string operation, string prefix)
    {
        Assert.StartsWith("API-", operation);
        var actor = await sql.CreateUserAsync("REFRESH-DENY-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var login = await client.PostAsJsonAsync(prefix + "login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var credential = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString()!;
        await using var db = sql.CreateDbContext();
        var tokenId = await db.RefreshTokens.Where(t => t.Session.UserId == actor.Id).Select(t => t.Id).SingleAsync();
        var route = prefix + "refresh";
        foreach (var json in new[] { "{}", "{", "{\"refreshToken\":null}", "{\"refreshToken\":\"\"}",
            "{\"refreshToken\":\"short\"}", JsonSerializer.Serialize(new { refreshToken = new string('x', 2049) }),
            JsonSerializer.Serialize(new { refreshToken = credential, userId = actor.Id }) })
            await Reject(() => client.PostAsync(route, new StringContent(json, Encoding.UTF8, "application/json")), 400, "validation_error");
        await Reject(() => client.PostAsync(route, new StringContent("not-json", Encoding.UTF8, "text/plain")), 415, "unsupported_media_type");
        // HttpClient omits a zero-length header value; whitespace is the actual transmitted blank boundary.
        foreach (var key in new[] { "   ", new string('x', 151), "has space", "nonascii-é" })
        {
            await Reject(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(new { refreshToken = credential }) };
                request.Headers.TryAddWithoutValidation("Idempotency-Key", key);
                return await client.SendAsync(request);
            }, 400, "validation_error");
        }
        await Reject(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(new { refreshToken = credential }) };
            request.Headers.TryAddWithoutValidation("Idempotency-Key", new[] { "key-one", "key-two" });
            return await client.SendAsync(request);
        }, 400, "validation_error");
        await Reject(() => client.PostAsJsonAsync(route, new { refreshToken = new string('z', 32) }), 401, "refresh_token_invalid");
        await db.RefreshTokens.Where(t => t.Id == tokenId).ExecuteUpdateAsync(t => t.SetProperty(v => v.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
        await Reject(() => client.PostAsJsonAsync(route, new { refreshToken = credential }), 401, "refresh_token_expired");
        await db.RefreshTokens.Where(t => t.Id == tokenId).ExecuteUpdateAsync(t => t.SetProperty(v => v.ExpiresAt, (DateTimeOffset?)null));
        foreach (var state in new[] { UserStatus.Pending, UserStatus.Suspended })
        {
            await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u.SetProperty(v => v.Status, state));
            await Reject(() => client.PostAsJsonAsync(route, new { refreshToken = credential }), 401, "auth_unauthorized");
        }
        await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u.SetProperty(v => v.Status, UserStatus.Active).SetProperty(v => v.MustChangePassword, true));
        await Reject(() => client.PostAsJsonAsync(route, new { refreshToken = credential }), 401, "auth_unauthorized");
        await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u.SetProperty(v => v.MustChangePassword, false));
        await db.Sessions.Where(s => s.UserId == actor.Id).ExecuteUpdateAsync(s => s.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
        await Reject(() => client.PostAsJsonAsync(route, new { refreshToken = credential }), 401, "auth_session_revoked");
        Assert.Equal(1, await db.RefreshTokens.CountAsync(t => t.Session.UserId == actor.Id));

        async Task Reject(Func<Task<HttpResponseMessage>> action, int status, string code)
        {
            var before = await Snapshot();
            using var response = await action();
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location); Assert.False(response.Headers.Contains("Set-Cookie"));
            var raw = await response.Content.ReadAsStringAsync(); var body = JsonSerializer.Deserialize<JsonElement>(raw);
            Assert.Equal(status, body.GetProperty("status").GetInt32()); Assert.Equal(code, body.GetProperty("code").GetString());
            Assert.Equal(route, body.GetProperty("instance").GetString());
            var correlation = Assert.Single(response.Headers.GetValues("X-Correlation-ID")); Assert.True(Guid.TryParse(correlation, out _));
            Assert.Equal(correlation, body.GetProperty("correlationId").GetString());
            foreach (var secret in new[] { actor.Email!, "Current1!", credential }) Assert.DoesNotContain(secret, raw, StringComparison.Ordinal);
            foreach (var field in new[] { "accessToken", "refreshToken", "passwordHash", "user" }) Assert.False(body.TryGetProperty(field, out _));
            Assert.Equal(before, await Snapshot());
        }
        async Task<string> Snapshot() => JsonSerializer.Serialize(new
        {
            sessions = await db.Sessions.AsNoTracking().Where(s => s.UserId == actor.Id).OrderBy(s => s.Id).ToArrayAsync(),
            tokens = await db.RefreshTokens.AsNoTracking().Where(t => t.Session.UserId == actor.Id).OrderBy(t => t.Id).ToArrayAsync(),
            receipts = await db.IdempotencyRecords.Where(r => r.ActorUserId == actor.Id).Select(r => r.Id).OrderBy(id => id).ToArrayAsync(),
            audits = await db.AuditLogs.Where(a => a.ActorUserId == actor.Id).Select(a => a.Id).OrderBy(id => id).ToArrayAsync()
        });
    }
}
