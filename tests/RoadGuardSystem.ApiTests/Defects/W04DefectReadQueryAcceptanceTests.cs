using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Projects;
using Xunit;

namespace RoadGuardSystem.ApiTests.Defects;

// Direct owned SQL read fixtures. These cases do not claim a Case/Defect producer journey.
[Collection(AuthenticationApiFixture.Name)]
public sealed class W04DefectReadQueryAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefectReadQueryDefaultsBoundsAndRouteConstraints(bool cookie)
    {
        var manager = await sql.CreateUserAsync("w04-wire-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var outsider = await sql.CreateUserAsync("w04-outsider-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), "W04-WIRE-" + Guid.NewGuid().ToString("N"), "Owned read fixture", null, 32648, null, null, now);
        var other = Project.Create(Guid.NewGuid(), "W04-OTHER-" + Guid.NewGuid().ToString("N"), "Other owned project", null, 32648, null, null, now);
        var section = RoadSection.Create(Guid.NewGuid(), project.Id, "READ-ROAD");
        var geometry = new LineString([new Coordinate(500000, 1200000), new Coordinate(500100, 1200000)]) { SRID = 32648 };
        var road = RoadSectionVersion.Create(Guid.NewGuid(), section.Id, 1, true, geometry, now, "Non-authoritative sample geometry");
        var type = "W04_READ_" + Guid.NewGuid().ToString("N");
        var ids = Enumerable.Range(0, 52).Select(_ => Guid.NewGuid()).ToArray();
        var defects = ids.Select(id => Defect.Create(id, project.Id, road.Id, null, type, null,
            DefectSeverity.High, DefectStatus.Open, geometry, now)).ToArray();
        var membershipId = Guid.NewGuid();
        await using (var setup = sql.CreateDbContext())
        {
            setup.AddRange(project, other, section, road, DefectType.Create(type, "Read fixture"));
            setup.AddRange(defects);
            setup.Add(new ProjectMember
            {
                Id = membershipId,
                ProjectId = project.Id,
                UserId = manager.Id,
                RoleCode = UserRoleCode.ProjectManager,
                Status = ProjectMemberStatus.Active,
                ValidFrom = DateOnly.FromDateTime(now.UtcDateTime.AddDays(-1))
            });
            setup.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = other.Id,
                UserId = outsider.Id,
                RoleCode = UserRoleCode.ProjectManager,
                Status = ProjectMemberStatus.Active,
                ValidFrom = DateOnly.FromDateTime(now.UtcDateTime.AddDays(-1))
            });
            await setup.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services => services.RemoveAll<IHostedService>());
        using var client = await Login(manager.Email!, cookie);
        var root = $"/api/v1/projects/{project.Id}/defects";
        var detail = root + "/" + ids[0];
        await using var db = sql.CreateDbContext();
        var before = await Snapshot();
        var first = await Page("", 50);
        Assert.Equal(first.GetRawText(), (await Page("?pageSize=50", 50)).GetRawText());
        Assert.Equal(first.GetRawText(), (await Page("?cursor=&status=&type=", 50)).GetRawText());
        var cursor = Assert.IsType<string>(first.GetProperty("nextCursor").GetString());
        var tail = await Page("?cursor=" + Uri.EscapeDataString(cursor), 2);
        Assert.Equal(JsonValueKind.Null, tail.GetProperty("nextCursor").ValueKind);
        var combined = first.GetProperty("items").EnumerateArray().Concat(tail.GetProperty("items").EnumerateArray())
            .Select(x => x.GetProperty("id").GetGuid()).ToArray();
        Assert.Equal(ids.Order().ToArray(), combined.Order().ToArray());
        Assert.Equal(52, combined.Distinct().Count());
        await Page("?pageSize=1", 1);
        var all = await Page("?pageSize=200", 52);
        Assert.Equal(JsonValueKind.Null, all.GetProperty("nextCursor").ValueKind);
        Assert.Equal(all.GetRawText(), (await Page("?pageSize=200&cursor=" +
            Uri.EscapeDataString(Convert.ToBase64String(new byte[16])), 52)).GetRawText());
        Assert.Equal(all.GetRawText(), (await Page("?pageSize=200&status=open&type=" + type, 52)).GetRawText());
        await Page("?type=" + new string('Z', 80), 0);
        await Page("?segmentId=" + Guid.NewGuid(), 0);
        foreach (var query in new[] { "pageSize=0", "pageSize=-1", "pageSize=201", "pageSize=2147483648",
            "pageSize=1.5", "pageSize=invalid", "cursor=bad", "cursor=YWJj", "status=UNKNOWN", "status=INVALID",
            "status=2147483647", "segmentId=invalid", "segmentId=" + Guid.Empty, "type=" + new string('Z', 81) })
            await Problem(client, root + "?" + query, 400, "validation_error");
        foreach (var missing in new[] { Guid.Empty, Guid.NewGuid() })
            await Problem(client, root + "/" + missing, 404, "not_found");
        await Problem(client, $"/api/v1/projects/{Guid.Empty}/defects", 400, "validation_error");
        foreach (var path in new[] { root + "/invalid", "/api/v1/projects/invalid/defects",
            "/api/v1/projects/invalid/defects/" + ids[0] })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location);
            Assert.DoesNotContain(type, await response.Content.ReadAsStringAsync());
        }
        Assert.Equal(before, await Snapshot());

        async Task<JsonElement> Page(string query, int count)
        {
            using var response = await client.GetAsync(root + query);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
            Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(new[] { "items", "nextCursor" }, body.EnumerateObject().Select(x => x.Name).Order().ToArray());
            Assert.Equal(count, body.GetProperty("items").GetArrayLength());
            foreach (var item in body.GetProperty("items").EnumerateArray()) Assert.Contains(item.GetProperty("id").GetGuid(), ids);
            return body;
        }
        async Task<string> Snapshot() => JsonSerializer.Serialize(new
        {
            defects = await db.Defects.AsNoTracking().Where(x => x.ProjectId == project.Id).OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Status, x.DefectTypeCode, version = EF.Property<byte[]>(x, "RowVersion") }).ToArrayAsync(),
            logs = await db.DefectVerificationLogs.Where(x => x.DefectId != null && ids.Contains(x.DefectId.Value)).Select(x => x.Id).ToArrayAsync(),
            audits = await db.AuditLogs.Where(x => x.ActorUserId == manager.Id).OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(),
            receipts = await db.IdempotencyRecords.Where(x => x.ActorUserId == manager.Id).Select(x => x.Id).ToArrayAsync()
        });
        async Task<HttpClient> Login(string email, bool web)
        {
            if (web) return await WebCookieTestClient.Login(factory, email);
            var http = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
            using var response = await http.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
            return http;
        }
    }

    private static async Task Problem(HttpClient client, string path, int status, string code)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, body.GetProperty("status").GetInt32());
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.Equal(path.Split('?')[0], body.GetProperty("instance").GetString());
        var correlation = Assert.Single(response.Headers.GetValues("X-Correlation-ID"));
        Assert.True(Guid.TryParse(correlation, out _));
        Assert.Equal(correlation, body.GetProperty("correlationId").GetString());
        Assert.Null(response.Headers.ETag);
        Assert.Null(response.Headers.Location);
    }
}
