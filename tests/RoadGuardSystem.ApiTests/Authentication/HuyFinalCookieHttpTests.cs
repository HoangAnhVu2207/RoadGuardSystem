using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Messaging;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Collection(AuthenticationApiFixture.Name)]
public sealed class HuyFinalCookieHttpTests(AuthenticationSqlServerFixture fixture)
{
    [Fact]
    public async Task CookieInbox_WriteCsrfReplayBearerPrecedenceAndMixedActors()
    {
        var actor = await fixture.CreateUserAsync($"final-cookie-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var notification = Notification.Create(Guid.NewGuid(), actor.Id, "Test", Guid.NewGuid(), "created", "Test", "Inbox", DateTimeOffset.UtcNow);
        await using (var db = fixture.CreateDbContext()) { db.Add(notification); await db.SaveChangesAsync(); }
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var csrf = await LoginCookieAsync(client, actor.Email!);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var list = await client.GetAsync("/api/v1/notifications/");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var detail = await client.GetAsync($"/api/v1/notifications/{notification.Id}/");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        client.DefaultRequestHeaders.Add("If-Match", detail.Headers.ETag!.ToString());
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var denied = await client.PostAsync($"/api/v1/notifications/{notification.Id}/read/", null);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("csrf_failed", (await denied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using (var db = fixture.CreateDbContext()) Assert.Null((await db.Notifications.SingleAsync(x => x.Id == notification.Id)).ReadAt);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        var first = await client.PostAsync($"/api/v1/notifications/{notification.Id}/read/", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var replay = await client.PostAsync($"/api/v1/notifications/{notification.Id}/read/", null);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid");
        var invalid = await client.GetAsync("/api/v1/notifications");
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        Assert.Equal("application/problem+json", invalid.Content.Headers.ContentType?.MediaType);
        var other = await fixture.CreateUserAsync($"final-other-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        using var bearer = factory.CreateClient();
        var login = await bearer.PostAsJsonAsync("/api/v1/auth/login", new { email = other.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/notifications")).StatusCode);
        await using (var db = fixture.CreateDbContext()) Assert.Equal(1, await db.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor.Id));
    }

    [Fact]
    public async Task CrewCookie_InspectionListIsAuthorizedAndRevocationDenies()
    {
        var crew = await fixture.CreateUserAsync($"final-crew-{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        await LoginCookieAsync(client, crew.Email!);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/me/inspection-tasks/")).StatusCode);
        await using (var db = fixture.CreateDbContext())
            await db.Sessions.Where(x => x.UserId == crew.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/me/inspection-tasks")).StatusCode);
    }

    private static async Task<string> LoginCookieAsync(HttpClient client, string email)
    {
        var csrfResponse = await client.GetAsync("/api/v1/auth/web/csrf");
        var csrf = (await csrfResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString()!;
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login", new { email, password = "Current1!" })).StatusCode);
        // Login changes the antiforgery identity; obtain the token bound to the authenticated actor.
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        return (await (await client.GetAsync("/api/v1/auth/web/csrf")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString()!;
    }
}
