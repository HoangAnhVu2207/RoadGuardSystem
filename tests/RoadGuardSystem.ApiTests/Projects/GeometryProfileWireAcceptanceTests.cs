using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class GeometryProfileWireAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactNestedProfileWireAndQuotedReadPreconditionsPreserveClosedProjectReads(bool cookie)
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
            await Create(client, root + "/crs-profiles", ProfileInput(code));
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
            Assert.Equal(new[] { "code", "createdAt", "createdBy", "definition", "id", "projectId", "revision", "status" }, detail.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
            Assert.Equal("CANDIDATE", detail.GetProperty("status").GetString());
            var definition = detail.GetProperty("definition");
            Assert.Equal(new[] { "axisOrder", "code", "datum", "ellipsoid", "independentControls", "metresPerUnit", "operation", "projection", "projectionDefinition", "sampleOnly", "sourceChecksum", "sourcedToleranceMeters", "sourceReference", "sourceSrid", "toleranceSource" }, definition.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(ProfileInput(profile.Code), WebJson)), JsonNode.Parse(definition.GetRawText())));
            if (profile.Code == "Z")
                foreach (var key in new[] { "operation", "independentControls", "sourcedToleranceMeters", "toleranceSource", "ellipsoid", "projectionDefinition" }) Assert.Equal(JsonValueKind.Null, definition.GetProperty(key).ValueKind);
            else
            {
                Assert.Equal(new[] { "accuracySource", "areaOfUse", "convention", "direction", "method", "parameters", "publishedAccuracyMeters", "sourceReference" }, definition.GetProperty("operation").EnumerateObject().Select(p => p.Name).OrderBy(n => n));
                Assert.Equal(new[] { "inverseFlattening", "semiMajorAxisMeters", "sourceReference" }, definition.GetProperty("ellipsoid").EnumerateObject().Select(p => p.Name).OrderBy(n => n));
                Assert.Equal(new[] { "parameters", "sourceReference" }, definition.GetProperty("projectionDefinition").EnumerateObject().Select(p => p.Name).OrderBy(n => n));
                var control = Assert.Single(definition.GetProperty("independentControls").EnumerateArray());
                Assert.Equal(new[] { "independentSource", "native", "wgs84" }, control.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
                foreach (var key in new[] { "native", "wgs84" }) Assert.Equal(new[] { "x", "y" }, control.GetProperty(key).EnumerateObject().Select(p => p.Name).OrderBy(n => n));
            }
            foreach (var tag in new[] { detailResponse.Headers.ETag!.Tag, "\"different-hash\"" })
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, root + $"/crs-profiles/{profile.Id}"); request.Headers.TryAddWithoutValidation("If-Match", tag);
                var quoted = await client.SendAsync(request); Assert.Equal(HttpStatusCode.OK, quoted.StatusCode);
                Assert.Equal(detailResponse.Headers.ETag, quoted.Headers.ETag);
                Assert.Equal(detail.GetRawText(), (await quoted.Content.ReadFromJsonAsync<JsonElement>()).GetRawText());
            }

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
        await using (var update = sql.CreateDbContext())
            await update.Projects.Where(p => p.Id == project).ExecuteUpdateAsync(p => p.SetProperty(x => x.Status, ProjectStatus.Closed));
        Assert.Equal(actualProfiles.Select(p => p.GetRawText()), (await Read(client, root + "/crs-profiles")).EnumerateArray().Select(p => p.GetRawText()));
        Assert.Equal(actualSystems.Select(p => p.GetRawText()), (await Read(client, root + "/route-systems")).EnumerateArray().Select(p => p.GetRawText()));
        foreach (var profile in profiles) Assert.Equal(actualProfiles.Single(p => p.GetProperty("id").GetGuid() == profile.Id).GetRawText(), (await Read(client, root + $"/crs-profiles/{profile.Id}")).GetRawText());
        await using var after = sql.CreateDbContext();
        Assert.Equal(JsonSerializer.Serialize(profiles), JsonSerializer.Serialize(await after.Set<CrsProfileRevision>().AsNoTracking().Where(x => x.ProjectId == project).OrderBy(x => x.Code).ThenBy(x => x.Revision).ToArrayAsync()));
        Assert.Equal(JsonSerializer.Serialize(systems), JsonSerializer.Serialize(await after.Set<RoadRouteSystem>().AsNoTracking().Where(x => x.ProjectId == project).OrderBy(x => x.Code).ToArrayAsync()));
        Assert.Equal(auditCount, await after.AuditLogs.CountAsync());
        Assert.Equal(receipts, await after.IdempotencyRecords.CountAsync());
    }

    private static object ProfileInput(string code) => new
    {
        code,
        sourceSrid = 0,
        datum = "CANDIDATE_SAMPLE_DATUM",
        projection = "CANDIDATE_SAMPLE_PROJECTION",
        axisOrder = "EN",
        metresPerUnit = 1d,
        sourceReference = "sample-only ordering",
        sourceChecksum = new string('b', 64),
        sampleOnly = true,
        operation = code == "Z" ? null : new { method = "SAMPLE_AFFINE_TO_WGS84", convention = "sample convention", direction = "NATIVE_TO_WGS84", parameters = new[] { 1d, 0, 0, 0, 1, 0 }, sourceReference = "synthetic source", areaOfUse = "synthetic area", publishedAccuracyMeters = 2d, accuracySource = "synthetic accuracy" },
        independentControls = code == "Z" ? null : new[] { new { native = new { x = 1d, y = 2d }, wgs84 = new { x = 1d, y = 2d }, independentSource = "synthetic independent source" } },
        sourcedToleranceMeters = code == "Z" ? (double?)null : 3d,
        toleranceSource = code == "Z" ? null : "synthetic tolerance",
        ellipsoid = code == "Z" ? null : new { semiMajorAxisMeters = 6378137d, inverseFlattening = 298.257223563, sourceReference = "synthetic ellipsoid" },
        projectionDefinition = code == "Z" ? null : new { parameters = new Dictionary<string, double> { ["sample"] = 1d }, sourceReference = "synthetic projection" }
    };

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
