using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class WebRenewInputBoundaryTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("missing-renewal", 401, "auth_session_revoked")]
    [InlineData("tampered-renewal", 401, "auth_session_revoked")]
    [InlineData("authorization", 400, "validation_error")]
    [InlineData("missing-csrf", 403, "csrf_failed")]
    [InlineData("invalid-csrf", 403, "csrf_failed")]
    public async Task InvalidRenewalEnvelopeHasExactProblemAndNoDurableEffect(string kind, int status, string code)
    {
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var csrf = await client.GetAsync("/api/v1/auth/web/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        var token = (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        var cookie = Assert.Single(csrf.Headers.GetValues("Set-Cookie"), c => c.StartsWith("__Host-RoadGuardCsrf=", StringComparison.Ordinal)).Split(';')[0];
        await using var db = sql.CreateDbContext();
        var sessions = await db.Sessions.CountAsync(); var tokens = await db.RefreshTokens.CountAsync();
        var audits = await db.AuditLogs.CountAsync(); var receipts = await db.IdempotencyRecords.CountAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/web/renew");
        request.Headers.Add("Cookie", cookie + (kind == "tampered-renewal" ? "; __Host-RoadGuardRenewal=tampered" : ""));
        if (kind != "missing-csrf") request.Headers.Add("X-CSRF-TOKEN", kind == "invalid-csrf" ? "invalid" : token);
        if (kind == "authorization") request.Headers.Add("Authorization", "Bearer invalid");
        using var response = await client.SendAsync(request);
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, body.GetProperty("status").GetInt32());
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.Equal("/api/v1/auth/web/renew", body.GetProperty("instance").GetString());
        Assert.DoesNotContain("accessToken", body.GetRawText()); Assert.DoesNotContain("refreshToken", body.GetRawText());
        Assert.Null(response.Headers.ETag); Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.Equal(sessions, await db.Sessions.CountAsync()); Assert.Equal(tokens, await db.RefreshTokens.CountAsync());
        Assert.Equal(audits, await db.AuditLogs.CountAsync()); Assert.Equal(receipts, await db.IdempotencyRecords.CountAsync());
    }
}
