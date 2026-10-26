using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class LogoutInputBoundaryTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false, "missing-key", 400, "validation_error")]
    [InlineData(false, "blank-key", 400, "validation_error")]
    [InlineData(true, "missing-key", 400, "validation_error")]
    [InlineData(true, "blank-key", 400, "validation_error")]
    [InlineData(true, "long-key", 400, "validation_error")]
    [InlineData(true, "tab-key", 400, "validation_error")]
    [InlineData(true, "missing-csrf", 403, "csrf_failed")]
    [InlineData(true, "invalid-csrf", 403, "csrf_failed")]
    public async Task InvalidLogoutEnvelopeDoesNotRevokeOrCreateReceipt(bool web, string kind, int status, string code)
    {
        var actor = await sql.CreateUserAsync("LOGOUT-INPUT-" + Guid.NewGuid().ToString("N"), "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = web ? await WebCookieTestClient.Login(factory, actor.Email!) : factory.CreateClient();
        if (!web)
        {
            using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        }
        if (kind != "missing-key") client.DefaultRequestHeaders.TryAddWithoutValidation("Idempotency-Key", kind switch
        { "blank-key" => "   ", "long-key" => new string('k', 201), "tab-key" => "key\tinvalid", _ => Guid.NewGuid().ToString("N") });
        if (kind is "missing-csrf" or "invalid-csrf") client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        if (kind == "invalid-csrf") client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid");
        await using var db = sql.CreateDbContext();
        var before = await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == actor.Id);
        var token = await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.SessionId == before.Id);
        var audits = await db.AuditLogs.CountAsync(); var receipts = await db.IdempotencyRecords.CountAsync();
        using var response = await client.PostAsync(web ? "/api/v1/auth/web/logout" : "/api/v1/auth/logout", null);
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, problem.GetProperty("status").GetInt32()); Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.DoesNotContain(actor.Email!, problem.GetRawText()); Assert.DoesNotContain("accessToken", problem.GetRawText());
        if (web) Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.False(response.Headers.Contains("Set-Cookie")); Assert.Null(response.Headers.ETag);
        Assert.Equal(before.RowVersion, (await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == before.Id)).RowVersion);
        Assert.Equal(token.RowVersion, (await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.Id == token.Id)).RowVersion);
        Assert.Equal(audits, await db.AuditLogs.CountAsync()); Assert.Equal(receipts, await db.IdempotencyRecords.CountAsync());
    }
}
