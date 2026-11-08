using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Services.Authentication;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class StepTwoWebWritesHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task StaffWritesRequireCurrentSupervisorAndCsrfAndKeepBearerReplay()
    {
        var supervisor = await User(UserRoleCode.Supervisor);
        var manager = await User(UserRoleCode.ProjectManager);
        var target = await User(UserRoleCode.DroneOperator);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services => services.Replace(
                ServiceDescriptor.Singleton<IIdentityMessageSender, FixtureInvitationSender>()));
        using var web = await WebCookieTestClient.Login(factory, supervisor.Email!);
        using var denied = await WebCookieTestClient.Login(factory, manager.Email!);
        using var bearer = await LoginBearer(factory, supervisor.Email!);

        using (var invitation = await Write(web, denied, bearer, HttpMethod.Post, "/api/v1/invitations",
            new { email = $"invite-{Guid.NewGuid():N}@example.test", role = "OPERATOR", projectIds = Array.Empty<Guid>() },
            HttpStatusCode.Created, key: Guid.NewGuid().ToString("N")))
        {
            Assert.NotNull(invitation.Headers.ETag);
            Assert.NotEqual(Guid.Empty, (await invitation.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
        }

        var accountPath = $"/api/v1/users/{target.Id}";
        var update = new { role = "CREW", status = "ACTIVE", reason = "Fixture role change" };
        var oldVersion = $"\"{Convert.ToBase64String(target.RowVersion)}\"";
        using (var changed = await Write(web, denied, bearer, HttpMethod.Patch, accountPath, update,
            HttpStatusCode.OK, key: Guid.NewGuid().ToString("N"), etag: oldVersion))
        {
            Assert.NotNull(changed.Headers.ETag);
            Assert.NotEqual(oldVersion, changed.Headers.ETag.Tag);
        }
        await Problem(await Send(web, HttpMethod.Patch, accountPath, update, Guid.NewGuid().ToString("N"), oldVersion),
            HttpStatusCode.PreconditionFailed, "auth_concurrency_conflict");

        using var reset = await Write(web, denied, bearer, HttpMethod.Post, accountPath + "/password-reset",
            new { temporaryPassword = "Replacement2!", reason = "Fixture reset" },
            HttpStatusCode.NoContent, key: Guid.NewGuid().ToString("N"));
        using var unchanged = await web.PostAsJsonAsync($"/api/v1/admin/users/{target.Id}/password-reset", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, unchanged.StatusCode);
    }

    [Fact]
    public async Task ProjectUpdateUsesBodyVersionAndOperationId()
    {
        var supervisor = await User(UserRoleCode.Supervisor);
        var manager = await User(UserRoleCode.ProjectManager);
        var project = await SeedProject();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var web = await WebCookieTestClient.Login(factory, supervisor.Email!);
        using var denied = await WebCookieTestClient.Login(factory, manager.Email!);
        using var bearer = await LoginBearer(factory, supervisor.Email!);
        var path = $"/api/v1/projects/{project.Id}";
        var payload = new
        {
            name = "Updated via Web",
            expectedRowVersion = Convert.ToBase64String(project.RowVersion),
            operationId = Guid.NewGuid()
        };
        using var updated = await Write(web, denied, bearer, HttpMethod.Put, path, payload, HttpStatusCode.OK);
        await Problem(await Send(web, HttpMethod.Put, path, payload with { operationId = Guid.NewGuid() }),
            HttpStatusCode.Conflict, "concurrency_conflict");
        await using var db = sql.CreateDbContext();
        Assert.Equal(payload.name, (await db.Projects.FindAsync(project.Id))!.Name);
    }

    [Fact]
    public async Task RoadWritesUseBodyOperationIdAndVersionPin()
    {
        var supervisor = await User(UserRoleCode.Supervisor);
        var manager = await User(UserRoleCode.ProjectManager);
        var project = await SeedProject();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var web = await WebCookieTestClient.Login(factory, supervisor.Email!);
        using var denied = await WebCookieTestClient.Login(factory, manager.Email!);
        using var bearer = await LoginBearer(factory, supervisor.Email!);
        var path = $"/api/v1/projects/{project.Id}/road-sections";
        var coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500100d, y = 1100100d } };
        var effectiveFrom = DateTimeOffset.UtcNow.AddDays(-2);
        using var created = await Write(web, denied, bearer, HttpMethod.Post, path, new
        {
            code = "WEB-ROAD",
            name = "Web road",
            srid = 32648,
            coordinates,
            effectiveFrom,
            changeReason = "Initial alignment",
            operationId = Guid.NewGuid()
        }, HttpStatusCode.Created);
        var initial = await created.Content.ReadFromJsonAsync<JsonElement>();
        var roadId = initial.GetProperty("roadSectionId").GetGuid();
        var versionPath = $"{path}/{roadId}/versions";
        var version = new
        {
            srid = 32648,
            coordinates,
            effectiveFrom = effectiveFrom.AddDays(1),
            changeReason = "Revision from Web",
            expectedCurrentVersionId = initial.GetProperty("roadSectionVersionId").GetGuid(),
            operationId = Guid.NewGuid()
        };
        using var revised = await Write(web, denied, bearer, HttpMethod.Post, versionPath, version, HttpStatusCode.Created);
        Assert.Equal(2, (await revised.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("versionNo").GetInt32());
        await Problem(await Send(web, HttpMethod.Post, versionPath, version with { operationId = Guid.NewGuid() }),
            HttpStatusCode.Conflict, "road_section_concurrency_conflict");
    }

    private async Task<Project> SeedProject()
    {
        var project = Project.Create(Guid.NewGuid(), $"WEB-{Guid.NewGuid():N}", "Web test project",
            null, 32648, null, null, DateTimeOffset.UtcNow);
        await using var db = sql.CreateDbContext();
        db.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private Task<RoadGuardSystem.BusinessObjects.Identity.ApplicationUser> User(UserRoleCode role) =>
        sql.CreateUserAsync($"STEP2-{Guid.NewGuid():N}", "Current1!", role);

    private static async Task<HttpClient> LoginBearer(AuthenticationWebApplicationFactory factory, string email)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<HttpResponseMessage> Write(HttpClient web, HttpClient denied, HttpClient bearer,
        HttpMethod method, string path, object payload, HttpStatusCode expected, string? key = null, string? etag = null)
    {
        web.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        await Problem(await Send(web, method, path, payload, key, etag), HttpStatusCode.Forbidden, "csrf_failed");
        web.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid");
        await Problem(await Send(web, method, path, payload, key, etag), HttpStatusCode.Forbidden, "csrf_failed");
        await Problem(await Send(denied, method, path, payload, key, etag), HttpStatusCode.Forbidden, "access_forbidden");
        await WebCookieTestClient.RefreshCsrf(web);
        var response = await Send(web, method, path, payload, key, etag);
        Assert.Equal(expected, response.StatusCode);
        using var replay = await Send(bearer, method, path, payload, key, etag);
        Assert.Equal(expected, replay.StatusCode);
        Assert.Equal(await response.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
        return response;
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path,
        object payload, string? key = null, string? etag = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(payload) };
        if (key is not null) request.Headers.Add("Idempotency-Key", key);
        if (etag is not null) request.Headers.Add("If-Match", etag);
        return await client.SendAsync(request);
    }

    private static async Task Problem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        using (response)
        {
            Assert.Equal(status, response.StatusCode);
            Assert.Equal(code, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
    }

    private sealed class FixtureInvitationSender : IIdentityMessageSender
    {
        public Task<bool> SendInvitationAsync(string email, string displayName, string roleName, string invitationToken,
            DateTimeOffset expiresAt, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> SendReporterOtpAsync(string email, string otp, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
