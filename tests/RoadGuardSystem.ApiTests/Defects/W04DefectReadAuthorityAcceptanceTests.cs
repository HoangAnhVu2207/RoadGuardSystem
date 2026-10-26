using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Geometries;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Projects;
using Xunit;

namespace RoadGuardSystem.ApiTests.Defects;

// Owned persisted read fixtures only. No verified-Defect producer/eligibility claim.
[Collection(AuthenticationApiFixture.Name)]
public sealed class W04DefectReadAuthorityAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false, "suspended")]
    [InlineData(true, "suspended")]
    [InlineData(false, "role-replaced")]
    [InlineData(true, "role-replaced")]
    [InlineData(false, "session-revoked")]
    [InlineData(true, "session-revoked")]
    [InlineData(true, "ticket-expired")]
    [InlineData(false, "catalog-inactive")]
    [InlineData(true, "catalog-inactive")]
    [InlineData(false, "membership-ended")]
    [InlineData(true, "membership-ended")]
    public async Task PopulatedDefectReadsRecheckCurrentAuthority(bool cookie, string change)
    {
        var manager = await sql.CreateUserAsync("defect-authority-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), "READ-AUTH-" + Guid.NewGuid().ToString("N"), "Owned read fixture", null, 32648, null, null, now);
        var section = RoadSection.Create(Guid.NewGuid(), project.Id, "READ-ROAD");
        var geometry = new LineString([new Coordinate(500000, 1200000), new Coordinate(500100, 1200000)]) { SRID = 32648 };
        var road = RoadSectionVersion.Create(Guid.NewGuid(), section.Id, 1, true, geometry, now, "TEST_ONLY sample");
        var type = "READ_AUTH_" + Guid.NewGuid().ToString("N");
        var id = Guid.NewGuid(); var membership = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.AddRange(project, section, road, DefectType.Create(type, "Owned type"));
            db.Add(Defect.Create(id, project.Id, road.Id, null, type, null, DefectSeverity.High, DefectStatus.Open, geometry, now));
            db.Add(new ProjectMember
            {
                Id = membership,
                ProjectId = project.Id,
                UserId = manager.Id,
                RoleCode = UserRoleCode.ProjectManager,
                Status = ProjectMemberStatus.Active,
                ValidFrom = DateOnly.FromDateTime(now.UtcDateTime.AddDays(-1))
            });
            await db.SaveChangesAsync();
        }
        var clock = new MutableClock(now);
        await using var host = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(clock);
            });
        using var client = cookie ? await WebCookieTestClient.Login(host, manager.Email!) : host.CreateClient(new()
        { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        if (!cookie)
        {
            using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = manager.Email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        }
        var paths = new[] { $"/api/v1/projects/{project.Id}/defects", $"/api/v1/projects/{project.Id}/defects/{id}" };
        foreach (var path in paths)
        {
            using var admitted = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, admitted.StatusCode);
            Assert.Contains(id.ToString(), await admitted.Content.ReadAsStringAsync());
        }
        var before = await Snapshot();
        try
        {
            await using (var db = sql.CreateDbContext())
            {
                if (change == "suspended") await db.Users.Where(x => x.Id == manager.Id).ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, UserStatus.Suspended));
                if (change == "role-replaced") await db.Users.Where(x => x.Id == manager.Id).ExecuteUpdateAsync(x => x.SetProperty(v => v.RoleCode, UserRoleCode.RepairCrew));
                if (change == "session-revoked") await db.Sessions.Where(x => x.UserId == manager.Id).ExecuteUpdateAsync(x => x.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
                if (change == "catalog-inactive") await db.Roles.Where(x => x.Code == UserRoleCode.ProjectManager).ExecuteUpdateAsync(x => x.SetProperty(v => v.IsActive, false));
                if (change == "membership-ended") await db.ProjectMembers.Where(x => x.Id == membership).ExecuteUpdateAsync(x => x.SetProperty(v => v.ValidTo, DateOnly.FromDateTime(now.UtcDateTime.AddDays(-1))));
            }
            if (change == "ticket-expired") clock.Now += TimeSpan.FromHours(13);
            var status = change == "membership-ended" ? 403 : 401;
            var code = status == 403 ? "access_forbidden" : !cookie && change is "role-replaced" or "session-revoked" ? "auth_session_revoked" : "auth_unauthorized";
            foreach (var path in paths)
            {
                using var denied = await client.GetAsync(path);
                Assert.Equal(status, (int)denied.StatusCode);
                Assert.Equal("application/problem+json", denied.Content.Headers.ContentType!.MediaType);
                var body = await denied.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(status, body.GetProperty("status").GetInt32());
                Assert.Equal(code, body.GetProperty("code").GetString());
                Assert.Equal(path, body.GetProperty("instance").GetString());
                Assert.Equal(Assert.Single(denied.Headers.GetValues("X-Correlation-ID")), body.GetProperty("correlationId").GetString());
                Assert.True(Guid.TryParse(body.GetProperty("correlationId").GetString(), out _));
                Assert.Null(denied.Headers.ETag); Assert.Null(denied.Headers.Location);
                foreach (var secret in new[] { type, manager.Email!, "TEST_ONLY sample", "coordinates", "ROAD_SURFACE_DAMAGE" })
                    Assert.DoesNotContain(secret, body.GetRawText());
            }
            Assert.Equal(before, await Snapshot());
        }
        finally
        {
            if (change == "catalog-inactive")
            {
                await using var db = sql.CreateDbContext();
                await db.Roles.Where(x => x.Code == UserRoleCode.ProjectManager).ExecuteUpdateAsync(x => x.SetProperty(v => v.IsActive, true));
            }
        }

        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            var defects = await db.Defects.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new { x.Id, x.Status, x.DefectTypeCode, Version = EF.Property<byte[]>(x, "RowVersion") }).ToArrayAsync();
            var logs = await db.DefectVerificationLogs.AsNoTracking().Where(x => x.DefectId == id).OrderBy(x => x.Id).ToArrayAsync();
            var audits = await db.AuditLogs.AsNoTracking().Where(x => x.EntityId == id || x.EntityId == project.Id).OrderBy(x => x.Id).ToArrayAsync();
            var receipts = await db.IdempotencyRecords.AsNoTracking().Where(x => x.ProjectId == project.Id).OrderBy(x => x.Id).ToArrayAsync();
            return JsonSerializer.Serialize(new { defects, logs, audits, receipts });
        }
    }
    private sealed class MutableClock(DateTimeOffset initial) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = initial;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
