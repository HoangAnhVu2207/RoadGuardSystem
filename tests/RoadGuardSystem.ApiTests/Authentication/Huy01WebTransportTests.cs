using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Identity;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Trait("Package", "HUY-01")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class Huy01WebTransportTests(AuthenticationSqlServerFixture fixture)
{
    [Fact]
    public async Task CsrfLoginSessionLogout_UsesHostCookieWithoutTokenBodyAndRevokesSession()
    {
        var username = $"web-{Guid.NewGuid():N}";
        var user = await fixture.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });

        var csrf = await client.GetAsync("/api/v1/auth/web/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        Assert.True(csrf.Headers.CacheControl?.NoStore);
        var token = (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        Assert.False(string.IsNullOrEmpty(token));
        Assert.Equal(0, await CountSessionsAsync(user.Id));

        var denied = await client.PostAsJsonAsync("/api/v1/auth/web/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!"
        });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("application/problem+json", denied.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, await CountSessionsAsync(user.Id));

        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
        var login = await client.PostAsJsonAsync("/api/v1/auth/web/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), header =>
            header.StartsWith("__Host-RoadGuardSession=", StringComparison.Ordinal) &&
            header.Contains("secure", StringComparison.OrdinalIgnoreCase) &&
            header.Contains("httponly", StringComparison.OrdinalIgnoreCase) &&
            header.Contains("path=/", StringComparison.OrdinalIgnoreCase) &&
            header.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase) &&
            !header.Contains("domain=", StringComparison.OrdinalIgnoreCase));
        var body = await login.Content.ReadAsStringAsync();
        Assert.DoesNotContain("accessToken", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", body, StringComparison.OrdinalIgnoreCase);
        Assert.True(login.Headers.CacheControl?.NoStore);
        var view = JsonDocument.Parse(body).RootElement;
        Assert.Equal(user.Id, view.GetProperty("user").GetProperty("id").GetGuid());
        Assert.Equal("OPERATOR", view.GetProperty("user").GetProperty("role").GetString());
        await using (var db = fixture.CreateDbContext())
        {
            var session = await db.Sessions.AsNoTracking().SingleAsync(item => item.UserId == user.Id);
            Assert.Equal(SessionTransport.Web, session.Transport);
            Assert.InRange(session.ExpiresAt - session.IssuedAt,
                TimeSpan.FromHours(12).Add(-TimeSpan.FromSeconds(1)), TimeSpan.FromHours(12).Add(TimeSpan.FromSeconds(1)));
        }

        var active = await client.GetAsync("/api/v1/auth/web/session");
        Assert.Equal(HttpStatusCode.OK, active.StatusCode);
        Assert.Equal(user.Id, (await active.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("user").GetProperty("id").GetGuid());

        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var renewed = await client.GetAsync("/api/v1/auth/web/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await renewed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var logout = await client.PostAsync("/api/v1/auth/web/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/web/session")).StatusCode);
    }

    [Fact]
    public async Task WebCookie_OptsIntoMeButInvalidBearerNeverFallsBack()
    {
        var username = $"web-me-{Guid.NewGuid():N}";
        await fixture.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var csrf = await client.GetAsync("/api/v1/auth/web/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!"
        })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/me")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/me")).StatusCode);
    }

    [Fact]
    public async Task CookieWrite_RequiresCsrf_AndOnlySuccessfulAuthorizedRequestsTouchActivity()
    {
        var username = $"web-csrf-{Guid.NewGuid():N}";
        var user = await fixture.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var csrf = await client.GetAsync("/api/v1/auth/web/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!"
        })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        DateTimeOffset initialActivity;
        await using (var db = fixture.CreateDbContext())
            initialActivity = (await db.Sessions.AsNoTracking().SingleAsync(item => item.UserId == user.Id)).LastActivityAt!.Value;

        var denied = await client.PatchAsJsonAsync("/api/v1/me", new { displayName = "Should Not Change" });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("application/problem+json", denied.Content.Headers.ContentType?.MediaType);
        await using (var db = fixture.CreateDbContext())
        {
            Assert.Equal(initialActivity, (await db.Sessions.AsNoTracking()
                .SingleAsync(item => item.UserId == user.Id)).LastActivityAt);
            Assert.NotEqual("Should Not Change", (await db.Users.AsNoTracking()
                .SingleAsync(item => item.Id == user.Id)).DisplayName);
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/me")).StatusCode);
        await using (var db = fixture.CreateDbContext())
            Assert.True((await db.Sessions.AsNoTracking().SingleAsync(item => item.UserId == user.Id))
                .LastActivityAt > initialActivity);
    }

    [Fact]
    public async Task WebSession_RejectsIdleAndAbsoluteExpiryWithoutRevivingSession()
    {
        var username = $"web-expiry-{Guid.NewGuid():N}";
        var user = await fixture.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var csrf = await client.GetAsync("/api/v1/auth/web/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!"
        })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");

        DateTimeOffset idleAt;
        await using (var db = fixture.CreateDbContext())
        {
            var session = await db.Sessions.SingleAsync(item => item.UserId == user.Id);
            idleAt = DateTimeOffset.UtcNow.AddMinutes(-31);
            session.LastActivityAt = idleAt;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/web/session")).StatusCode);
        await using (var db = fixture.CreateDbContext())
            Assert.Equal(idleAt, (await db.Sessions.AsNoTracking().SingleAsync(item => item.UserId == user.Id)).LastActivityAt);

        await using (var db = fixture.CreateDbContext())
        {
            var session = await db.Sessions.SingleAsync(item => item.UserId == user.Id);
            session.LastActivityAt = DateTimeOffset.UtcNow;
            session.ExpiresAt = session.IssuedAt.AddMilliseconds(1);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/web/session")).StatusCode);
    }

    [Fact]
    public async Task BearerAndCookieForDifferentActors_AreRejectedOnOptedInRoute()
    {
        var webName = $"web-mix-{Guid.NewGuid():N}";
        var bearerName = $"bearer-mix-{Guid.NewGuid():N}";
        await fixture.CreateUserAsync(webName, "Current1!");
        await fixture.CreateUserAsync(bearerName, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var csrf = await client.GetAsync("/api/v1/auth/web/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(webName), password = "Current1!"
        })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var bearerLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(bearerName), password = "Current1!"
        });
        Assert.Equal(HttpStatusCode.OK, bearerLogin.StatusCode);
        var bearer = (await bearerLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        var mixed = await client.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.BadRequest, mixed.StatusCode);
        Assert.Equal("validation_error", (await mixed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task MustChangePasswordCookie_CanReadSessionAndMeButNotWriteMe()
    {
        var username = $"web-forced-{Guid.NewGuid():N}";
        var user = await fixture.CreateUserAsync(username, "Current1!", mustChangePassword: true);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var csrf = await client.GetAsync("/api/v1/auth/web/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!"
        })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");

        var session = await client.GetAsync("/api/v1/auth/web/session");
        Assert.Equal(HttpStatusCode.OK, session.StatusCode);
        Assert.True((await session.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/me")).StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, (await client.PatchAsJsonAsync("/api/v1/me", new { displayName = "Forbidden" })).StatusCode);
        var refreshedCsrf = await client.GetAsync("/api/v1/auth/web/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN",
            (await refreshedCsrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var changed = await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { currentPassword = "Current1!", newPassword = "Changed1!" });
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/web/session")).StatusCode);
        await using var db = fixture.CreateDbContext();
        Assert.NotEqual("Forbidden", (await db.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id)).DisplayName);
    }

    [Fact]
    public async Task CommittedWebLogout_CanReplaySameKeyWithOriginalCookieButNotReadSession()
    {
        var username = $"web-logout-{Guid.NewGuid():N}";
        await fixture.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var anonymousCsrf = await client.GetAsync("/api/v1/auth/web/csrf");
        var csrfCookie = Cookie(anonymousCsrf, "__Host-RoadGuardCsrf");
        var anonymousToken = (await anonymousCsrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/web/login")
        {
            Content = JsonContent.Create(new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!" })
        };
        loginRequest.Headers.Add("Cookie", csrfCookie);
        loginRequest.Headers.Add("X-CSRF-TOKEN", anonymousToken);
        var login = await client.SendAsync(loginRequest);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var sessionCookie = Cookie(login, "__Host-RoadGuardSession");

        using var csrfRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/web/csrf");
        csrfRequest.Headers.Add("Cookie", $"{sessionCookie}; {csrfCookie}");
        var authorizedCsrf = await client.SendAsync(csrfRequest);
        var authorizedToken = (await authorizedCsrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        var updatedCsrf = authorizedCsrf.Headers.TryGetValues("Set-Cookie", out var cookies) &&
            cookies.Any(value => value.StartsWith("__Host-RoadGuardCsrf=", StringComparison.Ordinal))
            ? Cookie(authorizedCsrf, "__Host-RoadGuardCsrf") : csrfCookie;
        var key = Guid.NewGuid().ToString("N");

        async Task<HttpResponseMessage> LogoutAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/web/logout");
            request.Headers.Add("Cookie", $"{sessionCookie}; {updatedCsrf}");
            request.Headers.Add("X-CSRF-TOKEN", authorizedToken);
            request.Headers.Add("Idempotency-Key", key);
            return await client.SendAsync(request);
        }
        Assert.Equal(HttpStatusCode.NoContent, (await LogoutAsync()).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await LogoutAsync()).StatusCode);
        using var sessionRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/web/session");
        sessionRequest.Headers.Add("Cookie", sessionCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(sessionRequest)).StatusCode);
    }

    private static string Cookie(HttpResponseMessage response, string name) => response.Headers
        .GetValues("Set-Cookie")
        .Single(value => value.StartsWith(name + "=", StringComparison.Ordinal))
        .Split(';', 2)[0];

    private async Task<int> CountSessionsAsync(Guid userId)
    {
        await using var db = fixture.CreateDbContext();
        return await db.Sessions.CountAsync(item => item.UserId == userId);
    }
}
