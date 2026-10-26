using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class GeometryCatalogReadAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task CatalogReadsPreserveExactValuesIsolationPreconditionsAndCurrentAuthority()
    {
        var pm = await sql.CreateUserAsync("gcat-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await sql.CreateUserAsync("gcat-sup-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var project = Guid.NewGuid(); var other = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            foreach (var projectId in new[] { project, other })
            {
                db.Projects.Add(Project.Create(projectId, projectId.ToString(), "Catalog acceptance", null, null, null, null, DateTimeOffset.UtcNow));
                db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), projectId, pm.Id, new DateOnly(2000, 1, 1)));
            }
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, pm.UserName!);
        var root = $"/api/v1/projects/{project}";
        var input = new { code = "CAT", sourceSrid = 0, datum = "CANDIDATE_SAMPLE_DATUM", projection = "CANDIDATE_SAMPLE_PROJECTION", axisOrder = "EN", metresPerUnit = 1, sourceReference = "sample-only acceptance", sourceChecksum = new string('a', 64), sampleOnly = true };
        using var create = new HttpRequestMessage(HttpMethod.Post, root + "/crs-profiles") { Content = JsonContent.Create(input) };
        create.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var created = await client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var expected = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = expected.GetProperty("id").GetGuid();
        using var systemCreate = new HttpRequestMessage(HttpMethod.Post, root + "/route-systems") { Content = JsonContent.Create(new { code = "CAT", name = "Catalog route" }) };
        systemCreate.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var systemCreated = await client.SendAsync(systemCreate);
        Assert.Equal(HttpStatusCode.Created, systemCreated.StatusCode);
        var system = await systemCreated.Content.ReadFromJsonAsync<JsonElement>();
        var paths = new[] { root + "/crs-profiles", root + $"/crs-profiles/{id}", root + "/route-systems" };
        await using var before = sql.CreateDbContext();
        var auditCount = await before.AuditLogs.CountAsync();
        var receiptCount = await before.IdempotencyRecords.CountAsync();
        foreach (var user in new[] { pm.UserName!, supervisor.UserName! })
        {
            await Login(client, user);
            var list = await client.GetAsync(paths[0]);
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            Assert.Equal("application/json", list.Content.Headers.ContentType!.MediaType);
            Assert.True(Guid.TryParse(Assert.Single(list.Headers.GetValues("X-Correlation-ID")), out _));
            var profile = Assert.Single((await list.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
            Assert.Equal(new[] { "code", "createdAt", "createdBy", "definition", "id", "projectId", "revision", "status" }, profile.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
            Assert.Equal(expected.GetRawText(), profile.GetRawText());
            Assert.Equal(project, profile.GetProperty("projectId").GetGuid());
            Assert.Equal(pm.Id, profile.GetProperty("createdBy").GetGuid());
            Assert.Equal("CANDIDATE", profile.GetProperty("status").GetString());
            Assert.Equal(1, profile.GetProperty("revision").GetInt32());
            Assert.Equal("CAT", profile.GetProperty("code").GetString());
            Assert.Equal("CANDIDATE_SAMPLE_DATUM", profile.GetProperty("definition").GetProperty("datum").GetString());
            Assert.Equal(new string('a', 64), profile.GetProperty("definition").GetProperty("sourceChecksum").GetString());
            var detail = await client.GetAsync(paths[1]);
            Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
            Assert.Equal(expected.GetRawText(), (await detail.Content.ReadFromJsonAsync<JsonElement>()).GetRawText());
            Assert.Equal(created.Headers.ETag, detail.Headers.ETag);
            Assert.Matches("^\"[0-9a-f]{64}\"$", detail.Headers.ETag!.Tag);
            Assert.Equal("application/json", detail.Content.Headers.ContentType!.MediaType);
            Assert.True(Guid.TryParse(Assert.Single(detail.Headers.GetValues("X-Correlation-ID")), out _));
            var systems = await client.GetAsync(paths[2]);
            Assert.Equal(HttpStatusCode.OK, systems.StatusCode);
            Assert.Equal("application/json", systems.Content.Headers.ContentType!.MediaType);
            Assert.True(Guid.TryParse(Assert.Single(systems.Headers.GetValues("X-Correlation-ID")), out _));
            var actualSystem = Assert.Single((await systems.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
            Assert.Equal(new[] { "code", "id", "name", "projectId" }, actualSystem.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
            Assert.Equal(system.GetRawText(), actualSystem.GetRawText());
            Assert.Equal(project, actualSystem.GetProperty("projectId").GetGuid());
            Assert.Equal("CAT", actualSystem.GetProperty("code").GetString());
            Assert.Equal("Catalog route", actualSystem.GetProperty("name").GetString());
            Assert.Empty((await (await client.GetAsync($"/api/v1/projects/{other}/crs-profiles")).Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
            Assert.Empty((await (await client.GetAsync($"/api/v1/projects/{other}/route-systems")).Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
            await Error(client, $"/api/v1/projects/{other}/crs-profiles/{id}", HttpStatusCode.NotFound, "crs_profile_not_found");
            await Error(client, root + $"/crs-profiles/{Guid.NewGuid()}", HttpStatusCode.NotFound, "crs_profile_not_found");
            foreach (var path in paths)
            {
                using var bad = new HttpRequestMessage(HttpMethod.Get, path);
                bad.Headers.TryAddWithoutValidation("If-Match", "unquoted");
                var response = await client.SendAsync(bad);
                Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
                Assert.Equal("concurrency_conflict", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
            }
        }
        foreach (var role in new[] { UserRoleCode.Reporter, UserRoleCode.DroneOperator, UserRoleCode.RepairCrew })
        {
            var denied = await sql.CreateUserAsync("gcat-denied-" + Guid.NewGuid().ToString("N"), "Current1!", role);
            await Login(client, denied.UserName!);
            foreach (var path in paths) await Error(client, path, HttpStatusCode.Forbidden, "access_forbidden");
        }
        var outsider = await sql.CreateUserAsync("gcat-outside-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        await Login(client, outsider.UserName!);
        foreach (var path in paths) await Error(client, path, HttpStatusCode.Forbidden, "access_forbidden");
        await Login(client, pm.UserName!);
        await using (var revoke = sql.CreateDbContext())
            await revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE ProjectId={project} AND UserId={pm.Id}");
        foreach (var path in paths) await Error(client, path, HttpStatusCode.Forbidden, "access_forbidden");
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        foreach (var path in paths) Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        await using var after = sql.CreateDbContext();
        Assert.Equal(auditCount, await after.AuditLogs.CountAsync());
        Assert.Equal(receiptCount, await after.IdempotencyRecords.CountAsync());
        Assert.Equal(1, await after.Set<CrsProfileRevision>().CountAsync(x => x.ProjectId == project));
        Assert.Equal(1, await after.Set<RoadRouteSystem>().CountAsync(x => x.ProjectId == project));
    }

    private static async Task Error(HttpClient client, string path, HttpStatusCode status, string code)
    {
        var response = await client.GetAsync(path);
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.Equal(path, body.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("correlationId").GetString()));
        Assert.Equal(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), body.GetProperty("correlationId").GetString());
    }
    private static async Task Login(HttpClient client, string user)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(user), password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
}
