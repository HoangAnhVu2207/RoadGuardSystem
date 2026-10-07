using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Options;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Collection(AuthenticationApiFixture.Name)]
public sealed class H1PersistentIdentityTests(AuthenticationSqlServerFixture fixture)
{
    [Theory]
    [InlineData(31)]
    [InlineData(365)]
    public async Task PersistentRefresh_AfterOldAbsoluteBoundaries_NoIdleLogout(int elapsedDays)
    {
        var user = await fixture.CreateUserAsync($"h1-days-{Guid.NewGuid():N}", "Current1!");
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var factory = Factory(clock);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var token = await AndroidLogin(client, user.Email!);
        clock.Now += TimeSpan.FromDays(elapsedDays);
        var refreshed = await Refresh(client, token, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Equal(900, (await refreshed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("expiresIn").GetInt32());
        await using var db = fixture.CreateDbContext();
        Assert.Null((await db.Sessions.SingleAsync(s => s.UserId == user.Id)).ExpiresAt);
        Assert.All(await db.RefreshTokens.Where(t => t.Session.UserId == user.Id).ToListAsync(), t => Assert.Null(t.ExpiresAt));
    }

    [Fact]
    public async Task Rotation_SameOperationRaceAndLostAck_ReturnSameSuccessor_ThenDifferentReuseRevokes()
    {
        var user = await fixture.CreateUserAsync($"h1-race-{Guid.NewGuid():N}", "Current1!");
        await using var factory = Factory(new MutableClock(DateTimeOffset.UtcNow));
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var old = await AndroidLogin(client, user.Email!);
        var key = Guid.NewGuid().ToString("N");
        var attempts = await Task.WhenAll(Refresh(client, old, key), Refresh(client, old, key));
        Assert.All(attempts, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var first = (await attempts[0].Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString();
        Assert.Equal(first, (await attempts[1].Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString());
        var retry = await Refresh(client, old, key);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(first, (await retry.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString());
        await using (var db = fixture.CreateDbContext())
        {
            Assert.Equal(2, await db.RefreshTokens.CountAsync(t => t.Session.UserId == user.Id));
            var receipt = await db.IdempotencyRecords.SingleAsync(r => r.ActorUserId == user.Id && r.Operation == "RefreshRotation");
            Assert.DoesNotContain(first!, receipt.OutcomeJson);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(client, old, Guid.NewGuid().ToString("N"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(client, first!, key)).StatusCode);
        await using var verification = fixture.CreateDbContext();
        Assert.NotNull((await verification.Sessions.SingleAsync(s => s.UserId == user.Id)).RevokedAt);
    }

    [Theory]
    [InlineData("revoke")]
    [InlineData("disable")]
    [InlineData("role")]
    public async Task ProtectedRotationReceipt_CurrentAuthorityRechecked(string change)
    {
        var user = await fixture.CreateUserAsync($"h1-replay-{Guid.NewGuid():N}", "Current1!");
        await using var factory = Factory(new MutableClock(DateTimeOffset.UtcNow));
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var old = await AndroidLogin(client, user.Email!); var key = Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.OK, (await Refresh(client, old, key)).StatusCode);
        await using (var db = fixture.CreateDbContext())
        {
            if (change == "revoke") await db.Sessions.Where(s => s.UserId == user.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow));
            else
            {
                var current = await db.Users.SingleAsync(u => u.Id == user.Id);
                if (change == "disable") current.Status = UserStatus.Suspended;
                else { await new IdentityRepository(db).ChangeUserRoleAtomicAsync(current.Id, UserRoleCode.ProjectManager, current.RowVersion, current.Id, Guid.NewGuid()); }
                if (change == "disable") await db.SaveChangesAsync();
            }
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(client, old, key)).StatusCode);
    }

    [Fact]
    public async Task WebExpiredTicket_RenewWithIndependentCredential_CsrfRequired_ThenRevocationDenies()
    {
        var user = await fixture.CreateUserAsync($"h1-web-{Guid.NewGuid():N}", "Current1!");
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var factory = Factory(clock);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        await WebLogin(client, user.Email!);
        clock.Now += TimeSpan.FromHours(13);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/web/session")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/auth/web/renew", null)).StatusCode);
        await SetCsrf(client);
        var renew = await client.PostAsync("/api/v1/auth/web/renew", null);
        Assert.Equal(HttpStatusCode.OK, renew.StatusCode);
        var json = await renew.Content.ReadAsStringAsync();
        Assert.DoesNotContain("refreshToken", json); Assert.DoesNotContain("accessToken", json);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/web/session")).StatusCode);
        await SetCsrf(client);
        await using (var db = fixture.CreateDbContext()) await db.Sessions.Where(s => s.UserId == user.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, clock.Now));
        // Revoked ticket loses its antiforgery identity; obtain an anonymous token before renewal.
        await SetCsrf(client);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/auth/web/renew", null)).StatusCode);
    }

    [Fact]
    public async Task RotationReceipt_BoundedRetryDoesNotDisableReuseDefense()
    {
        var user = await fixture.CreateUserAsync($"h1-window-{Guid.NewGuid():N}", "Current1!");
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var factory = Factory(clock);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var old = await AndroidLogin(client, user.Email!); var key = Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.OK, (await Refresh(client, old, key)).StatusCode);
        clock.Now += TimeSpan.FromMinutes(2);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(client, old, key)).StatusCode);
        await using var db = fixture.CreateDbContext();
        Assert.NotNull((await db.Sessions.SingleAsync(s => s.UserId == user.Id)).RevokedAt);
    }

    [Theory]
    [InlineData("bad key")]
    [InlineData("duplicate")]
    public async Task RefreshOperationKey_MalformedRejectedBeforeCredentialEffect(string key)
    {
        var user = await fixture.CreateUserAsync($"h1-key-{Guid.NewGuid():N}", "Current1!");
        await using var factory = Factory(new MutableClock(DateTimeOffset.UtcNow));
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var old = await AndroidLogin(client, user.Email!);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/android/refresh") { Content = JsonContent.Create(new { refreshToken = old }) };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", key == "duplicate" ? new[] { "one", "two" } : new[] { key });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(request)).StatusCode);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.RefreshTokens.CountAsync(t => t.Session.UserId == user.Id));
    }

    [Fact]
    public async Task WebTicketAndRenewalCredentialForDifferentActors_AreRejected()
    {
        var a = await fixture.CreateUserAsync($"h1-web-a-{Guid.NewGuid():N}", "Current1!");
        var b = await fixture.CreateUserAsync($"h1-web-b-{Guid.NewGuid():N}", "Current1!");
        await using var factory = Factory(new MutableClock(DateTimeOffset.UtcNow));
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        async Task<HttpResponseMessage> Login(string email)
        {
            var csrf = await client.GetAsync("/api/v1/auth/web/csrf");
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/web/login") { Content = JsonContent.Create(new { email, password = "Current1!" }) };
            req.Headers.Add("Cookie", Cookie(csrf, "__Host-RoadGuardCsrf"));
            req.Headers.Add("X-CSRF-TOKEN", (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
            var response = await client.SendAsync(req); Assert.Equal(HttpStatusCode.OK, response.StatusCode); return response;
        }
        var loginA = await Login(a.Email!); var loginB = await Login(b.Email!);
        using var csrfRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/web/csrf");
        csrfRequest.Headers.Add("Cookie", Cookie(loginA, "__Host-RoadGuardSession"));
        var actorCsrf = await client.SendAsync(csrfRequest);
        using var renew = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/web/renew");
        renew.Headers.Add("Cookie", Cookie(loginA, "__Host-RoadGuardSession") + "; " + Cookie(loginB, "__Host-RoadGuardRenewal") + "; " + Cookie(actorCsrf, "__Host-RoadGuardCsrf"));
        renew.Headers.Add("X-CSRF-TOKEN", (await actorCsrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(renew)).StatusCode);
    }

    [Fact]
    public async Task ExpiredSignedAccessJwt_DeniedWhilePersistentSessionRemainsActive()
    {
        var user = await fixture.CreateUserAsync($"h1-finite-{Guid.NewGuid():N}", "Current1!");
        await using var factory = Factory(new MutableClock(DateTimeOffset.UtcNow));
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await AndroidLogin(client, user.Email!);
        await using var db = fixture.CreateDbContext(); var session = await db.Sessions.SingleAsync(x => x.UserId == user.Id);
        var options = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var expired = new AccessTokenFactory(options).Create(user.Id, session.Id, user.RoleCode, DateTimeOffset.UtcNow.AddMinutes(-20), 15);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expired);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/probe/protected")).StatusCode);
        Assert.True(session.IsActiveAt(DateTimeOffset.UtcNow));
    }
    private static string Cookie(HttpResponseMessage response, string name) => response.Headers.GetValues("Set-Cookie").Single(v => v.StartsWith(name + "=", StringComparison.Ordinal)).Split(';', 2)[0];

    [Fact]
    public async Task SwappedProtectedReceipt_ReturnsNoOtherFamilyCredential()
    {
        var a = await fixture.CreateUserAsync($"h1-swap-a-{Guid.NewGuid():N}", "Current1!");
        var b = await fixture.CreateUserAsync($"h1-swap-b-{Guid.NewGuid():N}", "Current1!");
        await using var factory = Factory(new MutableClock(DateTimeOffset.UtcNow));
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var oldA = await AndroidLogin(client, a.Email!); var oldB = await AndroidLogin(client, b.Email!);
        var keyA = Guid.NewGuid().ToString("N"); var keyB = Guid.NewGuid().ToString("N");
        Assert.Equal(HttpStatusCode.OK, (await Refresh(client, oldA, keyA)).StatusCode);
        var responseB = await Refresh(client, oldB, keyB); Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
        var secretB = (await responseB.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString()!;
        await using (var db = fixture.CreateDbContext())
        {
            var receiptA = await db.IdempotencyRecords.SingleAsync(r => r.ActorUserId == a.Id && r.Operation == "RefreshRotation");
            var receiptB = await db.IdempotencyRecords.SingleAsync(r => r.ActorUserId == b.Id && r.Operation == "RefreshRotation");
            var jsonA = System.Text.Json.Nodes.JsonNode.Parse(receiptA.OutcomeJson)!;
            jsonA["ProtectedCredential"] = System.Text.Json.Nodes.JsonNode.Parse(receiptB.OutcomeJson)!["ProtectedCredential"]!.GetValue<string>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE IdempotencyRecords SET OutcomeJson={jsonA.ToJsonString()} WHERE Id={receiptA.Id}");
        }
        var denied = await Refresh(client, oldA, keyA);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode); Assert.DoesNotContain(secretB, await denied.Content.ReadAsStringAsync());
    }

    private AuthenticationWebApplicationFactory Factory(MutableClock clock) => new(fixture.ConnectionString,
        configureTestServices: services => { services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(clock); });
    private static async Task<string> AndroidLogin(HttpClient client, string email)
    {
        var result = await client.PostAsJsonAsync("/api/v1/auth/android/login", new { email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        return (await result.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString()!;
    }
    private static async Task<HttpResponseMessage> Refresh(HttpClient client, string credential, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/android/refresh") { Content = JsonContent.Create(new { refreshToken = credential }) };
        request.Headers.Add("Idempotency-Key", key); return await client.SendAsync(request);
    }
    private static async Task SetCsrf(HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var result = await client.GetAsync("/api/v1/auth/web/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await result.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
    }
    private static async Task WebLogin(HttpClient client, string email)
    {
        await SetCsrf(client);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login", new { email, password = "Current1!" })).StatusCode);
        await SetCsrf(client);
    }
    private sealed class MutableClock(DateTimeOffset initial) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = initial;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
