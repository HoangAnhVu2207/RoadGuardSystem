using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class GeometryReadHeaderAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactCatalogReadsValidateHeaderShapeWithoutInventingWritePreconditions(bool cookie)
    {
        var pm = await sql.CreateUserAsync("GCAT-HEADER-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var project = Guid.NewGuid();
        await using var db = sql.CreateDbContext();
        db.Projects.Add(Project.Create(project, project.ToString(), "Header fixture", null, null, null, null, DateTimeOffset.UtcNow));
        db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, pm.Id, new DateOnly(2000, 1, 1)));
        await db.SaveChangesAsync();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = cookie ? await WebCookieTestClient.Login(factory, pm.Email!) : factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        if (!cookie)
        {
            var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = pm.Email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        }
        var root = $"/api/v1/projects/{project}";
        using var create = new HttpRequestMessage(HttpMethod.Post, root + "/crs-profiles") { Content = JsonContent.Create(new { code = "HEADER", sourceSrid = 0, datum = "CANDIDATE_SAMPLE_DATUM", projection = "CANDIDATE_SAMPLE_PROJECTION", axisOrder = "EN", metresPerUnit = 1, sourceReference = "sample-only header fixture", sourceChecksum = new string('c', 64), sampleOnly = true }) };
        create.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, (await client.SendAsync(create)).StatusCode);
        var profile = await db.Set<CrsProfileRevision>().AsNoTracking().SingleAsync(p => p.ProjectId == project);
        var original = JsonSerializer.Serialize(profile);
        var audit = await db.AuditLogs.CountAsync(); var receipts = await db.IdempotencyRecords.CountAsync();
        var paths = new[] { root + "/crs-profiles", root + $"/crs-profiles/{profile.Id}", root + "/route-systems" };
        foreach (var path in paths)
        {
            var baseline = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, baseline.StatusCode);
            var payload = await baseline.Content.ReadAsStringAsync();
            foreach (var tag in new[] { "\"matching-shaped\"", "\"different-shaped\"", "\"x\"" })
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                request.Headers.TryAddWithoutValidation("If-Match", tag);
                var read = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, read.StatusCode);
                Assert.Equal(payload, await read.Content.ReadAsStringAsync());
                Assert.Equal(baseline.Headers.ETag, read.Headers.ETag);
            }
            foreach (var tag in new[] { "bad", "*", "\"\"", "W/\"weak\"", "\"unclosed", "unopened\"" })
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                request.Headers.TryAddWithoutValidation("If-Match", tag);
                var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
                Assert.Null(response.Headers.ETag);
                var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(412, problem.GetProperty("status").GetInt32());
                Assert.Equal("concurrency_conflict", problem.GetProperty("code").GetString());
                Assert.Equal("Geometry request failed", problem.GetProperty("title").GetString());
                Assert.Equal(path, problem.GetProperty("instance").GetString());
                Assert.Equal(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), problem.GetProperty("correlationId").GetString());
                Assert.False(problem.TryGetProperty("definition", out _));
            }
        }
        foreach (var path in new[] { "/api/v1/projects/not-a-guid/crs-profiles", root + "/crs-profiles/not-a-guid", "/api/v1/projects/not-a-guid/route-systems" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Null(response.Headers.ETag);
        }
        await using var after = sql.CreateDbContext();
        Assert.Equal(original, JsonSerializer.Serialize(await after.Set<CrsProfileRevision>().AsNoTracking().SingleAsync(p => p.Id == profile.Id)));
        Assert.Equal(audit, await after.AuditLogs.CountAsync());
        Assert.Equal(receipts, await after.IdempotencyRecords.CountAsync());
    }
}
