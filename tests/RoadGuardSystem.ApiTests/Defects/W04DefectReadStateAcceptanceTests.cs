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

// Read-only state matrix from explicit domain-created SQL fixtures; no claimed lifecycle producer.
[Collection(AuthenticationApiFixture.Name)]
public sealed class W04DefectReadStateAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EveryPersistedStateReadsWithCurrentVersionWithoutTransition(bool cookie)
    {
        var manager = await sql.CreateUserAsync("w04-state-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), "W04-STATE-" + Guid.NewGuid().ToString("N"), "Owned state read fixture", null, 32648, null, null, now);
        var section = RoadSection.Create(Guid.NewGuid(), project.Id, "READ-STATE");
        var geometry = new LineString([new Coordinate(500000, 1200000), new Coordinate(500100, 1200000)]) { SRID = 32648 };
        var road = RoadSectionVersion.Create(Guid.NewGuid(), section.Id, 1, true, geometry, now, "Read fixture only");
        var type = "W04_STATE_" + Guid.NewGuid().ToString("N");
        var states = Enum.GetValues<DefectStatus>().Where(s => s != DefectStatus.Unknown).ToArray();
        var defects = states.Select(s => Defect.Create(Guid.NewGuid(), project.Id, road.Id, null, type, null,
            DefectSeverity.High, s, geometry, now)).ToArray();
        await using (var db = sql.CreateDbContext())
        {
            db.AddRange(project, section, road, DefectType.Create(type, "State fixture")); db.AddRange(defects);
            // Explicit nullable read fixture; not a workflow producer or domain transition.
            db.Entry(defects[0]).Property(d => d.Geometry).CurrentValue = null;
            db.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = manager.Id,
                RoleCode = UserRoleCode.ProjectManager,
                Status = ProjectMemberStatus.Active,
                ValidFrom = DateOnly.FromDateTime(now.UtcDateTime.AddDays(-1))
            });
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services => services.RemoveAll<IHostedService>());
        using var client = cookie ? await WebCookieTestClient.Login(factory, manager.Email!) :
            factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        if (!cookie)
        {
            using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = manager.Email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        }
        var root = $"/api/v1/projects/{project.Id}/defects";
        var before = await Snapshot();
        var versions = await Versions();
        using (var all = await client.GetAsync(root))
        {
            Assert.Equal(HttpStatusCode.OK, all.StatusCode); Assert.Null(all.Headers.ETag);
            var body = await all.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(5, body.GetProperty("items").GetArrayLength());
            foreach (var item in body.GetProperty("items").EnumerateArray()) Check(item);
        }
        foreach (var defect in defects)
        {
            using var page = await client.GetAsync(root + "?status=" + defect.Status.ToString().ToUpperInvariant());
            Assert.Equal(HttpStatusCode.OK, page.StatusCode); Assert.Null(page.Headers.ETag);
            var body = await page.Content.ReadFromJsonAsync<JsonElement>();
            var item = Assert.Single(body.GetProperty("items").EnumerateArray()); Check(item);
            Assert.Equal(defect.Id, item.GetProperty("id").GetGuid());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("nextCursor").ValueKind);
            for (var repeat = 0; repeat < 2; repeat++)
            {
                using var read = await client.GetAsync(root + "/" + defect.Id);
                Assert.Equal(HttpStatusCode.OK, read.StatusCode);
                var value = await read.Content.ReadFromJsonAsync<JsonElement>(); Check(value);
                Assert.Equal(item.GetRawText(), value.GetRawText());
                Assert.NotNull(read.Headers.ETag); Assert.False(read.Headers.ETag!.IsWeak);
                Assert.Equal('"' + versions[defect.Id] + '"', read.Headers.ETag.Tag);
            }
        }
        Assert.Equal(before, await Snapshot());

        void Check(JsonElement value)
        {
            var id = value.GetProperty("id").GetGuid();
            var expected = Assert.Single(defects, d => d.Id == id);
            Assert.Equal(expected.Status.ToString().ToUpperInvariant(), value.GetProperty("status").GetString());
            Assert.Equal(versions[id], value.GetProperty("version").GetString());
            Assert.Equal(project.Id, value.GetProperty("projectId").GetGuid());
            if (expected.Geometry is null) Assert.False(value.TryGetProperty("geometry", out _));
            else
            {
                Assert.Equal(32648, value.GetProperty("geometry").GetProperty("srid").GetInt32());
                Assert.Equal(expected.Geometry.AsText(), value.GetProperty("geometry").GetProperty("wkt").GetString());
            }
        }
        async Task<Dictionary<Guid, string>> Versions()
        {
            await using var db = sql.CreateDbContext();
            var rows = await db.Defects.AsNoTracking().Where(d => d.ProjectId == project.Id)
                .Select(d => new { d.Id, version = EF.Property<byte[]>(d, "RowVersion") }).ToArrayAsync();
            return rows.ToDictionary(d => d.Id, d => Convert.ToBase64String(d.version));
        }
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new
            {
                rows = await db.Defects.AsNoTracking().Where(d => d.ProjectId == project.Id).OrderBy(d => d.Id)
                    .Select(d => new { d.Id, d.Status, d.DefectTypeCode, d.Severity, version = EF.Property<byte[]>(d, "RowVersion") }).ToArrayAsync(),
                logs = await db.DefectVerificationLogs.Where(l => l.DefectId != null && defects.Select(d => d.Id).Contains(l.DefectId.Value)).Select(l => l.Id).OrderBy(id => id).ToArrayAsync(),
                receipts = await db.IdempotencyRecords.Where(r => r.ActorUserId == manager.Id).Select(r => r.Id).OrderBy(id => id).ToArrayAsync(),
                audits = await db.AuditLogs.Where(a => a.ActorUserId == manager.Id).Select(a => a.Id).OrderBy(id => id).ToArrayAsync()
            });
        }
    }
}
