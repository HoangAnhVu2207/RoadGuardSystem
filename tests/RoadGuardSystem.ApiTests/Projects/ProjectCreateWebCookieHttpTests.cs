using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class ProjectCreateWebCookieHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task WebCreateUsesCurrentCsrfAndSupervisorAuthorityWhileBearerStillReplays()
    {
        var supervisor = await User(UserRoleCode.Supervisor);
        var bearerSupervisor = await User(UserRoleCode.Supervisor);
        var manager = await User(UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var web = await WebCookieTestClient.Login(factory, supervisor.Email!);
        using var pm = await WebCookieTestClient.Login(factory, manager.Email!);
        using var bearer = await Bearer(factory, bearerSupervisor.Email!);
        var request = Payload(manager.Id);
        var key = Guid.NewGuid().ToString("N");

        using var created = await Post(web, request, key);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Null(web.DefaultRequestHeaders.Authorization);
        var projectId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.NotNull(created.Headers.ETag);
        using var replay = await Post(web, request, key);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal(projectId, (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
        using var changed = await Post(web, request with { Name = "Changed request" }, key);
        await Problem(changed, HttpStatusCode.Conflict, "duplicate_request");
        await using (var db = sql.CreateDbContext())
        {
            Assert.Equal(1, await db.Projects.CountAsync(p => p.ProjectCode == request.Code));
            Assert.Null((await db.HandoverDocuments.SingleAsync(h => h.ProjectId == projectId)).FileId);
            Assert.Null((await db.Warranties.SingleAsync(w => w.ProjectId == projectId)).SourceDocumentId);
            Assert.Equal(1, await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == supervisor.Id &&
                r.Operation == "ProjectCreated"));
        }

        var missingCsrfRequest = Payload(manager.Id);
        web.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        using var missingCsrf = await Post(web, missingCsrfRequest, Guid.NewGuid().ToString("N"));
        await Problem(missingCsrf, HttpStatusCode.Forbidden, "csrf_failed");
        web.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid-token");
        using var wrongCsrf = await Post(web, missingCsrfRequest, Guid.NewGuid().ToString("N"));
        await Problem(wrongCsrf, HttpStatusCode.Forbidden, "csrf_failed");
        await WebCookieTestClient.RefreshCsrf(web);
        using var missingHandoverDate = await Post(web, Payload(manager.Id) with { HandoverDate = null }, Guid.NewGuid().ToString("N"));
        await Problem(missingHandoverDate, HttpStatusCode.BadRequest, "validation_error");
        using var missingWarrantyDate = await Post(web, Payload(manager.Id) with { WarrantyEndDate = null }, Guid.NewGuid().ToString("N"));
        await Problem(missingWarrantyDate, HttpStatusCode.BadRequest, "validation_error");
        using var pmDenied = await Post(pm, Payload(manager.Id), Guid.NewGuid().ToString("N"));
        await Problem(pmDenied, HttpStatusCode.Forbidden, "access_forbidden");

        var bearerPayload = Payload(manager.Id);
        var bearerKey = Guid.NewGuid().ToString("N");
        using var bearerCreated = await Post(bearer, bearerPayload, bearerKey);
        Assert.Equal(HttpStatusCode.Created, bearerCreated.StatusCode);
        var bearerProject = (await bearerCreated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var bearerReplay = await Post(bearer, bearerPayload, bearerKey);
        Assert.Equal(HttpStatusCode.Created, bearerReplay.StatusCode);
        Assert.Equal(bearerProject, (await bearerReplay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());

        await using (var db = sql.CreateDbContext())
            await db.Sessions.Where(s => s.UserId == supervisor.Id).ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow));
        var afterRevocation = Payload(manager.Id);
        using var revoked = await Post(web, afterRevocation, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        await using (var db = sql.CreateDbContext())
        {
            Assert.False(await db.Projects.AnyAsync(p => p.ProjectCode == afterRevocation.Code ||
                p.ProjectCode == missingCsrfRequest.Code));
            Assert.Equal(1, await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == supervisor.Id &&
                r.Operation == "ProjectCreated"));
            Assert.Equal(1, await db.Projects.CountAsync(p => p.ProjectCode == bearerPayload.Code));
        }
    }

    [Fact]
    public async Task RuntimeOpenApiOffersBearerOrCookieWithCsrfForCreate()
    {
        await using var factory = new CustomWebApplicationFactory("Development");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var project = document.RootElement.GetProperty("paths").GetProperty("/api/v1/projects");
        var create = project.GetProperty("post");
        var alternatives = create.GetProperty("security").EnumerateArray().ToArray();
        Assert.Equal(2, alternatives.Length);
        Assert.Contains(alternatives, x => x.TryGetProperty("Bearer", out _) && x.EnumerateObject().Count() == 1);
        Assert.Contains(alternatives, x => x.TryGetProperty("WebSession", out _) &&
            x.TryGetProperty("CsrfToken", out _) && x.TryGetProperty("AntiforgeryCookie", out _));
        Assert.Contains("GET /api/v1/auth/web/csrf", create.GetProperty("description").GetString());
        Assert.Equal(0, create.GetProperty("requestBody").GetProperty("content").GetProperty("application/json")
            .GetProperty("example").GetProperty("handoverFileIds").GetArrayLength());
    }

    private Task<RoadGuardSystem.BusinessObjects.Identity.ApplicationUser> User(UserRoleCode role) =>
        sql.CreateUserAsync("PROJECT-WEB-" + Guid.NewGuid().ToString("N"), "Current1!", role);

    private static ProjectPayload Payload(Guid manager) => new("WEB-" + Guid.NewGuid().ToString("N"),
        "Web project", manager, new(2026, 11, 1), new(2027, 11, 1), []);

    private static async Task<HttpResponseMessage> Post(HttpClient client, ProjectPayload payload, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/projects")
        { Content = JsonContent.Create(payload) };
        request.Headers.Add("Idempotency-Key", key);
        return await client.SendAsync(request);
    }

    private static async Task Problem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    private static async Task<HttpClient> Bearer(AuthenticationWebApplicationFactory factory, string email)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        return client;
    }

    private sealed record ProjectPayload(string Code, string Name, Guid PrimaryPmId,
        DateOnly? HandoverDate, DateOnly? WarrantyEndDate, IReadOnlyList<Guid> HandoverFileIds);
}
