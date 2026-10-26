using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Services.Processing.Anh02;
using Xunit;

namespace RoadGuardSystem.ApiTests.Processing;

[Collection(AuthenticationApiFixture.Name)]
public sealed class AiReadRouteValidationAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("ai-candidates", false)]
    [InlineData("ai-mock-runs", false)]
    [InlineData("ai-mock-runs", true)]
    public async Task ExactReadGuidRoutesAndFeatureBoundary(string resource, bool result)
    {
        var actor = await sql.CreateUserAsync("ai-route-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), "AI-ROUTE-" + Guid.NewGuid().ToString("N"), "Owned route fixture", null, 32648, null, null, now);
        var otherProject = Project.Create(Guid.NewGuid(), "AI-OTHER-" + Guid.NewGuid().ToString("N"), "Unassigned route fixture", null, 32648, null, null, now);
        await using (var db = sql.CreateDbContext())
        {
            db.Add(project);
            db.Add(otherProject);
            db.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = actor.Id,
                RoleCode = UserRoleCode.ProjectManager,
                Status = ProjectMemberStatus.Active,
                ValidFrom = DateOnly.FromDateTime(now.UtcDateTime.AddDays(-1))
            });
            await db.SaveChangesAsync();
        }
        var suffix = result ? "/result" : "";
        await using var factory = Host(true);
        using var client = await Login(factory);
        var before = await Snapshot();
        foreach (var missing in new[] { Guid.Empty, Guid.NewGuid() })
            await Problem(client, $"/api/v1/projects/{project.Id}/{resource}/{missing}" + suffix);
        foreach (var path in new[] { $"/api/v1/projects/{project.Id}/{resource}/invalid" + suffix,
            $"/api/v1/projects/invalid/{resource}/{Guid.NewGuid()}" + suffix })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location);
            Assert.DoesNotContain(actor.Email!, await response.Content.ReadAsStringAsync());
        }
        if (resource == "ai-mock-runs")
        {
            var unavailableProjectPath = $"/api/v1/projects/{otherProject.Id}/{resource}/{Guid.NewGuid()}" + suffix;
            await Problem(client, unavailableProjectPath, 403, "access_forbidden");
            await using var disabled = Host(false);
            using var disabledClient = await Login(disabled);
            await Problem(disabledClient, unavailableProjectPath);
        }
        Assert.Equal(before, await Snapshot());

        AuthenticationWebApplicationFactory Host(bool enabled) => new(sql.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<IHostedService>();
                services.Configure<Anh02AiOptions>(options => options.MockEnabled = enabled);
            });
        async Task<HttpClient> Login(AuthenticationWebApplicationFactory host)
        {
            var http = host.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
            using var response = await http.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
            return http;
        }
        async Task Problem(HttpClient http, string path, int status = 404, string code = "not_found")
        {
            using var response = await http.GetAsync(path);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(new[] { "code", "correlationId", "instance", "status", "title" }, body.EnumerateObject().Select(x => x.Name).Order().ToArray());
            Assert.Equal(status, body.GetProperty("status").GetInt32());
            Assert.Equal(code, body.GetProperty("code").GetString());
            Assert.Equal(path, body.GetProperty("instance").GetString());
            var correlation = Assert.Single(response.Headers.GetValues("X-Correlation-ID"));
            Assert.True(Guid.TryParse(correlation, out _));
            Assert.Equal(correlation, body.GetProperty("correlationId").GetString());
            Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location);
            Assert.DoesNotContain(actor.Email!, body.GetRawText());
        }
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new
            {
                jobs = await db.ProcessingJobs.AsNoTracking().Where(x => x.ProjectId == project.Id).Select(x => x.Id).ToArrayAsync(),
                receipts = await db.IdempotencyRecords.AsNoTracking().Where(x => x.ProjectId == project.Id).OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(),
                audits = await db.AuditLogs.AsNoTracking().Where(x => x.ActorUserId == actor.Id).OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync()
            });
        }
    }
}
