using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Messaging;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class SupervisorGlobalAuthorityHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task ActualWebLoginCookieAndBearerListProjectsWithoutMembershipAndNoAuthIsDenied()
    {
        var actor = await sql.CreateUserAsync("global-cookie-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var project = await ProjectAsync();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var cookie = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        await CookieLogin(cookie, actor.Email!);
        Assert.Null(cookie.DefaultRequestHeaders.Authorization);
        var response = await cookie.GetAsync("/api/v1/projects?search=" + project.ProjectCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(project.Id, Assert.Single((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray()).GetProperty("projectId").GetGuid());
        using var bearer = await Bearer(factory, actor.Email!);
        Assert.Equal(HttpStatusCode.OK, (await bearer.GetAsync("/api/v1/projects")).StatusCode);
        using var anonymous = factory.CreateClient(new() { HandleCookies = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/projects")).StatusCode);
        // Method-only cookie opt-in must not silently opt in project mutations.
        Assert.Equal(HttpStatusCode.Unauthorized, (await cookie.PostAsJsonAsync("/api/v1/projects", new { })).StatusCode);
        await using var db = sql.CreateDbContext();
        await db.Sessions.Where(x => x.UserId == actor.Id).ExecuteUpdateAsync(x => x.SetProperty(s => s.RevokedAt, DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.Unauthorized, (await cookie.GetAsync("/api/v1/projects")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await bearer.GetAsync("/api/v1/projects")).StatusCode);
    }

    [Fact]
    public async Task SupervisorReadsForeignProjectSurfacesButCannotExecutePmCommandAndSuspensionDeniesSession()
    {
        var actor = await sql.CreateUserAsync("global-http-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var project = await ProjectAsync();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Bearer(factory, actor.Email!);
        foreach (var surface in new[] { "lifecycle", "road-coverage", "defect-statistics", "reports/summary", "receiving-requests", "clocks" })
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/projects/{project.Id}/{surface}")).StatusCode);
        foreach (var missing in new[] { "lifecycle", "road-coverage", "defect-statistics" })
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/{missing}")).StatusCode);
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"" + new string('a', 64) + "\"");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/v1/projects/{project.Id}/lifecycle/construction-declarations",
            new { reason = "PM only", evidenceFileIds = Array.Empty<Guid>() })).StatusCode);
        await using var db = sql.CreateDbContext();
        Assert.False(await db.ProjectMembers.AnyAsync(x => x.UserId == actor.Id));
        await db.Users.Where(x => x.Id == actor.Id).ExecuteUpdateAsync(x => x.SetProperty(u => u.Status, UserStatus.Suspended));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/projects/{project.Id}/lifecycle")).StatusCode);
    }

    [Fact]
    public async Task GlobalSupervisorInboxStillReadsOnlyOwnRecipientAndDoesNotBroadcast()
    {
        var one = await sql.CreateUserAsync("recipient-one-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var two = await sql.CreateUserAsync("recipient-two-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var project = await ProjectAsync(); var now = DateTimeOffset.UtcNow;
        var own = Notification.Create(Guid.NewGuid(), one.Id, "Project", project.Id, "created", "Test", "Own", now);
        var other = Notification.Create(Guid.NewGuid(), two.Id, "Project", project.Id, "created", "Test", "Other", now);
        await using (var db = sql.CreateDbContext()) { db.AddRange(own, other); await db.SaveChangesAsync(); }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Bearer(factory, one.Email!);
        var ownRead = await client.GetAsync($"/api/v1/notifications/{own.Id}");
        Assert.Equal(HttpStatusCode.OK, ownRead.StatusCode);
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", ownRead.Headers.ETag!.ToString());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/notifications/{own.Id}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/notifications/{other.Id}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/notifications/{other.Id}")).StatusCode);
        var list = await client.GetAsync("/api/v1/notifications"); Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var body = await list.Content.ReadAsStringAsync(); Assert.Contains(own.Id.ToString(), body); Assert.DoesNotContain(other.Id.ToString(), body);
        await using (var db = sql.CreateDbContext()) Assert.Equal(2, await db.Notifications.CountAsync(x => x.SourceEntityId == project.Id));
    }

    private async Task<Project> ProjectAsync()
    {
        var project = Project.Create(Guid.NewGuid(), "GLOBAL-" + Guid.NewGuid().ToString("N"), "Foreign project", null, null, null, null, DateTimeOffset.UtcNow);
        await using var db = sql.CreateDbContext(); db.Add(project); await db.SaveChangesAsync(); return project;
    }
    private static async Task CookieLogin(HttpClient client, string email)
    {
        var csrf = await (await client.GetAsync("/api/v1/auth/web/csrf")).Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.GetProperty("requestToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login", new { email, password = "Current1!" })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
    }
    private static async Task<HttpClient> Bearer(AuthenticationWebApplicationFactory factory, string email)
    {
        var client = factory.CreateClient(new() { HandleCookies = false });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        return client;
    }
}
