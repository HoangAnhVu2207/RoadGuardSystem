using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class CsrfBootstrapContractHttpTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnonymousBootstrapPreservesPrivateStateAndDeclaresExactWire(bool invalidCredential)
    {
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        if (invalidCredential)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-bootstrap-token");
            client.DefaultRequestHeaders.Add("Cookie", "__Host-RoadGuardSession=invalid-cookie");
        }
        await using var db = sql.CreateDbContext();
        var sessions = await db.Sessions.CountAsync();
        var audits = await db.AuditLogs.CountAsync();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            var response = await client.GetAsync("/api/v1/auth/web/csrf");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
            Assert.True(response.Headers.CacheControl!.NoStore);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(new[] { "headerName", "requestToken" }, body.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
            Assert.Equal("X-CSRF-TOKEN", body.GetProperty("headerName").GetString());
            Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("requestToken").GetString()));
            var cookies = response.Headers.GetValues("Set-Cookie").ToArray();
            Assert.NotEmpty(cookies);
            Assert.All(cookies, cookie =>
            {
                Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("__Host-RoadGuardSession=", cookie);
                Assert.DoesNotContain("__Host-RoadGuardRenewal=", cookie);
            });
            Assert.Null(response.Headers.ETag);
        }
        Assert.Equal(sessions, await db.Sessions.CountAsync());
        Assert.Equal(audits, await db.AuditLogs.CountAsync());
    }
}
