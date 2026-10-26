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
public sealed class W04DefectReadWireAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PopulatedReadHasExactWirePaginationAndCurrentProjectAuthority(bool cookie)
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
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
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
        using var foreign = await Login(outsider.Email!, cookie);
        var root = $"/api/v1/projects/{project.Id}/defects";
        var detail = root + "/" + ids[0];
        await using var db = sql.CreateDbContext();
        var before = await Snapshot();
        using (var read = await client.GetAsync(detail))
        {
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            Assert.Equal("application/json", read.Content.Headers.ContentType!.MediaType);
            var body = await read.Content.ReadFromJsonAsync<JsonElement>();
            CheckDefect(body);
            Assert.Equal(ids[0], body.GetProperty("id").GetGuid());
            Assert.NotNull(read.Headers.ETag);
            Assert.False(read.Headers.ETag!.IsWeak);
            Assert.Equal('"' + body.GetProperty("version").GetString() + '"', read.Headers.ETag.Tag);
            var stored = await db.Defects.AsNoTracking().Where(x => x.Id == ids[0])
                .Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync();
            Assert.Equal(Convert.ToBase64String(stored), body.GetProperty("version").GetString());
            Assert.Null(read.Headers.Location);
            Assert.True(Guid.TryParse(Assert.Single(read.Headers.GetValues("X-Correlation-ID")), out _));
        }
        var seen = new List<Guid>();
        string? cursor = null;
        for (var page = 0; page < 3; page++)
        {
            using var response = await client.GetAsync(root + "?pageSize=1&status=OPEN&type=" + type +
                (cursor is null ? "" : "&cursor=" + Uri.EscapeDataString(cursor)));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
            Assert.Null(response.Headers.ETag);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(new[] { "items", "nextCursor" }, body.EnumerateObject().Select(x => x.Name).Order().ToArray());
            var item = Assert.Single(body.GetProperty("items").EnumerateArray());
            CheckDefect(item);
            seen.Add(item.GetProperty("id").GetGuid());
            cursor = body.GetProperty("nextCursor").GetString();
            if (page < 2) Assert.Equal(seen[^1], new Guid(Convert.FromBase64String(Assert.IsType<string>(cursor))));
            else Assert.Null(cursor);
        }
        Assert.Equal(ids.Order().ToArray(), seen.Order().ToArray());
        Assert.Equal(3, seen.Distinct().Count());
        using (var empty = await client.GetAsync(root + "?status=VERIFIED"))
        {
            Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
            var body = await empty.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Empty(body.GetProperty("items").EnumerateArray());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("nextCursor").ValueKind);
        }
        foreach (var path in new[] { root, detail })
            await Problem(foreign, path, 403, "access_forbidden");
        foreach (var role in Enum.GetValues<UserRoleCode>().Where(x => x is not (UserRoleCode.Unknown or UserRoleCode.ProjectManager)))
        {
            var actor = await sql.CreateUserAsync("w04-role-" + Guid.NewGuid().ToString("N"), "Current1!", role);
            using var wrongRole = await Login(actor.Email!, cookie);
            foreach (var path in new[] { root, detail }) await Problem(wrongRole, path, 403, "access_forbidden");
        }
        await Problem(client, $"/api/v1/projects/{other.Id}/defects/{ids[0]}", 403, "access_forbidden");
        await Problem(client, root + "/" + Guid.NewGuid(), 404, "not_found");
        foreach (var query in new[] { "pageSize=0", "pageSize=201", "status=UNKNOWN", "cursor=bad", "cursor=YWJj" })
            await Problem(client, root + "?" + query, 400, "validation_error");
        await db.ProjectMembers.Where(x => x.Id == membershipId).ExecuteUpdateAsync(x =>
            x.SetProperty(v => v.ValidTo, DateOnly.FromDateTime(now.UtcDateTime.AddDays(-1))));
        await Problem(client, root, 403, "access_forbidden");
        await Problem(client, detail, 403, "access_forbidden");
        Assert.Equal(before, await Snapshot());

        void CheckDefect(JsonElement body)
        {
            Assert.Equal(new[] { "causeCategoryCode", "defectTypeCode", "geometry", "id", "projectId", "roadSectionVersionId", "segmentId", "severity", "status", "version" },
                body.EnumerateObject().Select(x => x.Name).Order().ToArray());
            Assert.Contains(body.GetProperty("id").GetGuid(), ids);
            Assert.Equal(project.Id, body.GetProperty("projectId").GetGuid());
            Assert.Equal(road.Id, body.GetProperty("roadSectionVersionId").GetGuid());
            Assert.Equal(type, body.GetProperty("defectTypeCode").GetString());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("causeCategoryCode").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("segmentId").ValueKind);
            Assert.Equal("HIGH", body.GetProperty("severity").GetString());
            Assert.Equal("OPEN", body.GetProperty("status").GetString());
            var shape = body.GetProperty("geometry");
            Assert.Equal(new[] { "srid", "wkt" }, shape.EnumerateObject().Select(x => x.Name).Order().ToArray());
            Assert.Equal(32648, shape.GetProperty("srid").GetInt32());
            Assert.Equal(geometry.AsText(), shape.GetProperty("wkt").GetString());
            Assert.Equal(8, Convert.FromBase64String(body.GetProperty("version").GetString()!).Length);
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
        var correlation = Assert.Single(response.Headers.GetValues("X-Correlation-ID"));
        Assert.True(Guid.TryParse(correlation, out _));
        Assert.Equal(correlation, body.GetProperty("correlationId").GetString());
        Assert.Null(response.Headers.ETag);
        Assert.Null(response.Headers.Location);
    }
}
