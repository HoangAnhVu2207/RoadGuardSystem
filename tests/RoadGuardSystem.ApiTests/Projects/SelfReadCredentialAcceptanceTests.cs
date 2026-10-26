using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Options;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

// Credential admission only. Random resources deliberately do not prove business success.
public sealed class SelfReadCredentialAcceptanceTests(AuthenticationSqlServerFixture sql)
    : IClassFixture<AuthenticationSqlServerFixture>
{
    [Theory]
    [InlineData("API-035", "GET", "/api/v1/me", 200)]
    [InlineData("API-047", "GET", "/api/v1/profile", 200)]
    public async Task BothSelfReadsCheckCredentialLifetimeRevocationAndPrecedence(
        string operation, string method, string route, int admittedStatus)
    {
        Assert.False(string.IsNullOrWhiteSpace(operation));
        var actor = await sql.CreateUserAsync("INBOX-AUTH-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        var initial = clock.Now;
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services => { services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(clock); });
        using var web = await WebCookieTestClient.Login(factory, actor.Email!);
        using var bearer = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var malformedCookie = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        malformedCookie.DefaultRequestHeaders.Add("Cookie", "__Host-RoadGuardSession=malformed");
        var login = await bearer.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
        await using var db = sql.CreateDbContext();
        var session = await db.Sessions.AsNoTracking().Where(s => s.UserId == actor.Id).OrderByDescending(s => s.IssuedAt).FirstAsync();
        var expired = new AccessTokenFactory(factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value)
            .Create(actor.Id, session.Id, actor.RoleCode, initial.AddMinutes(-20), 15);
        var path = System.Text.RegularExpressions.Regex.Replace(route, @"\{[^}]+\}", _ => Guid.NewGuid().ToString());
        var audits = await db.AuditLogs.CountAsync(a => a.ActorUserId == actor.Id);
        bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using (var response = await Send(bearer)) Assert.Equal(admittedStatus, (int)response.StatusCode);
        using (var response = await Send(web)) Assert.Equal(admittedStatus, (int)response.StatusCode);
        foreach (var client in new[] { anonymous, malformedCookie }) await Denied(client);
        foreach (var value in new[] { "malformed", expired })
        {
            bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", value);
            await Denied(bearer);
        }
        web.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "malformed");
        await Denied(web);
        web.DefaultRequestHeaders.Authorization = null;
        web.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        using (var admitted = await Send(web)) Assert.Equal(admittedStatus, (int)admitted.StatusCode);
        clock.Now += TimeSpan.FromHours(13);
        await Denied(web);
        clock.Now = initial;
        await db.Sessions.Where(s => s.UserId == actor.Id).ExecuteUpdateAsync(s => s.SetProperty(v => v.RevokedAt, initial));
        bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await Denied(bearer); await Denied(web);
        Assert.Equal(audits, await db.AuditLogs.CountAsync(a => a.ActorUserId == actor.Id));
        Assert.Empty(await db.IdempotencyRecords.Where(r => r.ActorUserId == actor.Id).ToArrayAsync());

        Task<HttpResponseMessage> Send(HttpClient client) => client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
        async Task Denied(HttpClient client)
        {
            using var response = await Send(client);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.Null(response.Headers.ETag);
        }

    }

    private sealed class MutableClock(DateTimeOffset initial) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = initial;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
