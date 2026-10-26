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
public sealed class LoginCredentialBoundaryAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("API-002", "/api/v1/auth/android/login")]
    [InlineData("API-006", "/api/v1/auth/login")]
    [InlineData("API-014", "/api/v1/auth/web/login")]
    public async Task LoginDeniesInvalidCredentialsAndCurrentInactiveStateWithoutDisclosureOrSession(string operation, string route)
    {
        var actor = await sql.CreateUserAsync("LOGIN-DENY-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var credentialCode = operation == "API-014" ? "auth_unauthorized" : "auth_invalid_credentials";
        if (operation == "API-014")
        {
            await Denied(await client.PostAsJsonAsync(route, new { email = actor.Email, password = "Current1!" }), 403, "csrf_failed");
            var csrf = await client.GetAsync("/api/v1/auth/web/csrf");
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
        }
        await Denied(await client.PostAsJsonAsync(route, new { email = actor.Email, password = "WrongSecret1!" }), 401, credentialCode);
        await Denied(await client.PostAsJsonAsync(route, new { email = "unknown-" + Guid.NewGuid().ToString("N") + "@example.test", password = "WrongSecret1!" }), 401, credentialCode);
        foreach (var json in new[] { "{}", "{\"email\":\"\",\"password\":\"\"}", "{\"email\":\"x\",\"password\":null}", "{", "{\"email\":\"x\",\"password\":\"y\",\"role\":\"SUPERVISOR\"}" })
            await Denied(await client.PostAsync(route, new StringContent(json, Encoding.UTF8, "application/json")), 400, "validation_error");
        await Denied(await client.PostAsync(route, new StringContent("not-json", Encoding.UTF8, "text/plain")), 415, "unsupported_media_type");
        await using var db = sql.CreateDbContext();
        foreach (var state in new[] { UserStatus.Pending, UserStatus.Suspended })
        {
            await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u.SetProperty(v => v.Status, state));
            await Denied(await client.PostAsJsonAsync(route, new { email = actor.Email, password = "Current1!" }), 401, credentialCode);
        }
        Assert.Empty(await db.Sessions.Where(s => s.UserId == actor.Id).ToArrayAsync());
        Assert.Empty(await db.RefreshTokens.Where(t => t.Session.UserId == actor.Id).ToArrayAsync());
        Assert.Empty(await db.IdempotencyRecords.Where(r => r.ActorUserId == actor.Id).ToArrayAsync());
        Assert.Empty(await db.AuditLogs.Where(a => a.ActorUserId == actor.Id).ToArrayAsync());
        Assert.Null((await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id)).LastLoginAt);

        async Task Denied(HttpResponseMessage response, int status, string code)
        {
            using (response)
            {
                Assert.Equal(status, (int)response.StatusCode);
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
                Assert.Null(response.Headers.ETag);
                var raw = await response.Content.ReadAsStringAsync();
                var body = JsonSerializer.Deserialize<JsonElement>(raw);
                Assert.Equal(code, body.GetProperty("code").GetString());
                Assert.Equal(status, body.GetProperty("status").GetInt32());
                Assert.Equal(route, body.GetProperty("instance").GetString());
                Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), out var correlation));
                Assert.Equal(correlation.ToString(), body.GetProperty("correlationId").GetString());
                Assert.DoesNotContain(actor.Email!, raw, StringComparison.Ordinal);
                Assert.DoesNotContain("Current1!", raw, StringComparison.Ordinal);
                Assert.DoesNotContain("WrongSecret1!", raw, StringComparison.Ordinal);
                Assert.False(body.TryGetProperty("accessToken", out _));
                Assert.False(body.TryGetProperty("refreshToken", out _));
                Assert.False(body.TryGetProperty("passwordHash", out _));
            }
        }
    }
}
