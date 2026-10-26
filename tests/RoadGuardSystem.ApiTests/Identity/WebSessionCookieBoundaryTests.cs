using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class WebSessionCookieBoundaryTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task SessionCookieAloneBindsIdentityAndTamperingCannotBorrowRenewalAuthority()
    {
        var a = await sql.CreateUserAsync("COOKIE-A-" + Guid.NewGuid().ToString("N"), "Current1!");
        var b = await sql.CreateUserAsync("COOKIE-B-" + Guid.NewGuid().ToString("N"), "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        var first = await LoginCookies(a.Email!);
        var second = await LoginCookies(b.Email!);
        await using var db = sql.CreateDbContext();
        var audits = await db.AuditLogs.CountAsync();
        var receipts = await db.IdempotencyRecords.CountAsync();
        using var reader = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        foreach (var (session, renewal, expected) in new[] { (first.Session, second.Renewal, a.Id), (second.Session, first.Renewal, b.Id) })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/web/session");
            request.Headers.Add("Cookie", session + "; " + renewal);
            using var response = await reader.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("user").GetProperty("id").GetGuid());
        }
        var before = await db.Sessions.AsNoTracking().Where(s => s.UserId == a.Id || s.UserId == b.Id).OrderBy(s => s.Id).ToArrayAsync();
        foreach (var cookies in new[] { second.Renewal, "__Host-RoadGuardSession=invalid; " + second.Renewal, first.Session[..^8] + "tampered; " + second.Renewal })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/web/session");
            request.Headers.Add("Cookie", cookies);
            using var response = await reader.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(401, body.GetProperty("status").GetInt32());
            Assert.DoesNotContain(a.Email!, body.GetRawText());
            Assert.DoesNotContain(b.Email!, body.GetRawText());
            Assert.Null(response.Headers.ETag);
        }
        var after = await db.Sessions.AsNoTracking().Where(s => s.UserId == a.Id || s.UserId == b.Id).OrderBy(s => s.Id).ToArrayAsync();
        Assert.Equal(before.Select(s => Convert.ToBase64String(s.RowVersion)), after.Select(s => Convert.ToBase64String(s.RowVersion)));
        Assert.All(after, s => Assert.Null(s.RevokedAt));
        Assert.Equal(audits, await db.AuditLogs.CountAsync());
        Assert.Equal(receipts, await db.IdempotencyRecords.CountAsync());

        async Task<(string Session, string Renewal)> LoginCookies(string email)
        {
            using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
            await WebCookieTestClient.RefreshCsrf(client);
            using var login = await client.PostAsJsonAsync("/api/v1/auth/web/login", new { email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var cookies = login.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0]).ToArray();
            return (Assert.Single(cookies, c => c.StartsWith("__Host-RoadGuardSession=", StringComparison.Ordinal)),
                Assert.Single(cookies, c => c.StartsWith("__Host-RoadGuardRenewal=", StringComparison.Ordinal)));
        }
    }
}
