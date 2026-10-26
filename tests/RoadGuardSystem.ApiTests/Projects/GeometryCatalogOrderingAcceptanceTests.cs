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
public sealed class GeometryCatalogOrderingAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyAndOrderedCatalogsMatchPersistedValuesWithoutReadWrites(bool cookie)
    {
        var pm = await sql.CreateUserAsync("gcat-order-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var project = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(Project.Create(project, project.ToString(), "Catalog ordering", null, null, null, null, DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, pm.Id, new DateOnly(2000, 1, 1)));
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = cookie ? await WebCookieTestClient.Login(factory, pm.Email!) : factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        if (!cookie)
        {
            var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = pm.Email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        }
        var root = $"/api/v1/projects/{project}";
        foreach (var suffix in new[] { "/crs-profiles", "/route-systems" })
        {
            var empty = await Read(client, root + suffix);
            Assert.Empty(empty.EnumerateArray());
        }
        foreach (var code in new[] { "Z", "A", "A" })
            await Create(client, root + "/crs-profiles", new { code, sourceSrid = 0, datum = "CANDIDATE_SAMPLE_DATUM", projection = "CANDIDATE_SAMPLE_PROJECTION", axisOrder = "EN", metresPerUnit = 1, sourceReference = "sample-only ordering", sourceChecksum = new string('b', 64), sampleOnly = true });
        foreach (var code in new[] { "Z", "A" })
            await Create(client, root + "/route-systems", new { code, name = "Route " + code });
        await using var before = sql.CreateDbContext();
        var profiles = await before.Set<CrsProfileRevision>().AsNoTracking().Where(x => x.ProjectId == project).OrderBy(x => x.Code).ThenBy(x => x.Revision).ToArrayAsync();
        var systems = await before.Set<RoadRouteSystem>().AsNoTracking().Where(x => x.ProjectId == project).OrderBy(x => x.Code).ToArrayAsync();
        var auditCount = await before.AuditLogs.CountAsync();
        var receipts = await before.IdempotencyRecords.CountAsync();
        var actualProfiles = (await Read(client, root + "/crs-profiles")).EnumerateArray().ToArray();
        Assert.Equal(new[] { "A", "A", "Z" }, actualProfiles.Select(p => p.GetProperty("code").GetString()));
        Assert.Equal(new[] { 1, 2, 1 }, actualProfiles.Select(p => p.GetProperty("revision").GetInt32()));
        Assert.Equal(profiles.Select(p => p.Id), actualProfiles.Select(p => p.GetProperty("id").GetGuid()));
        foreach (var profile in profiles)
        {
            var detailResponse = await client.GetAsync(root + $"/crs-profiles/{profile.Id}");
            Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
            Assert.Matches("^\"[0-9a-f]{64}\"$", detailResponse.Headers.ETag!.Tag);
            var detail = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(actualProfiles.Single(x => x.GetProperty("id").GetGuid() == profile.Id).GetRawText(), detail.GetRawText());
            Assert.Equal(project, detail.GetProperty("projectId").GetGuid());
            Assert.Equal(profile.CreatedBy, detail.GetProperty("createdBy").GetGuid());
            Assert.Equal(profile.CreatedAt, detail.GetProperty("createdAt").GetDateTimeOffset());
        }
        var actualSystems = (await Read(client, root + "/route-systems")).EnumerateArray().ToArray();
        Assert.Equal(new[] { "A", "Z" }, actualSystems.Select(s => s.GetProperty("code").GetString()));
        Assert.Equal(systems.Length, actualSystems.Length);
        for (var i = 0; i < systems.Length; i++)
        {
            var actual = actualSystems[i]; var expected = systems[i];
            Assert.Equal(new[] { "code", "id", "name", "projectId" }, actual.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
            Assert.Equal(expected.Id, actual.GetProperty("id").GetGuid());
            Assert.Equal(expected.ProjectId, actual.GetProperty("projectId").GetGuid());
            Assert.Equal(expected.Code, actual.GetProperty("code").GetString());
            Assert.Equal(expected.Name, actual.GetProperty("name").GetString());
        }
        await using var after = sql.CreateDbContext();
        Assert.Equal(JsonSerializer.Serialize(profiles), JsonSerializer.Serialize(await after.Set<CrsProfileRevision>().AsNoTracking().Where(x => x.ProjectId == project).OrderBy(x => x.Code).ThenBy(x => x.Revision).ToArrayAsync()));
        Assert.Equal(JsonSerializer.Serialize(systems), JsonSerializer.Serialize(await after.Set<RoadRouteSystem>().AsNoTracking().Where(x => x.ProjectId == project).OrderBy(x => x.Code).ToArrayAsync()));
        Assert.Equal(auditCount, await after.AuditLogs.CountAsync());
        Assert.Equal(receipts, await after.IdempotencyRecords.CountAsync());
    }

    private static async Task<JsonElement> Read(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), out _));
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    private static async Task Create(HttpClient client, string path, object input)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(input) };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, (await client.SendAsync(request)).StatusCode);
    }
}
