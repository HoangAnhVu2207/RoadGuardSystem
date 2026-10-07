using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Reporting;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.DTOs.Exports;
using Xunit;
namespace RoadGuardSystem.ApiTests.Repairs;

[Collection(AuthenticationApiFixture.Name)]
public sealed class LD08DefectStatisticsHttpTests(AuthenticationSqlServerFixture fixture)
{
    [Fact]
    public async Task ProductionDiRecordsExplicitIdentityAndReportExportFreezesSnapshotThroughSupersession()
    {
        var pm = await fixture.CreateUserAsync("ld08-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await fixture.CreateUserAsync("ld08-sup-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        await using var db = fixture.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        // Controlled TEST_ONLY geometry/obligation isolates the actual production mapping endpoint.
        // The SQL acceptance separately produces the real Report -> repair binding -> eligibility chain.
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "TEST_ONLY mapping HTTP", null, null, null, null, now);
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = supervisor.Id,
            RoleCode = UserRoleCode.Supervisor,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-2)
        };
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "TEST_ONLY road");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry.CreateLineString([new(0, 0), new(10, 0)]), now, "TEST_ONLY; no real CRS claim");
        var type = DefectType.Create("LD08" + Guid.NewGuid().ToString("N"), "TEST_ONLY");
        var defect = Defect.Create(Guid.NewGuid(), project.Id, route.Id, null, type.Code, null, DefectSeverity.Low, DefectStatus.Open,
            geometry.CreatePoint(new Coordinate(5, 0)), now);
        db.AddRange(project, member, ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm.Id, DateOnly.FromDateTime(now.UtcDateTime).AddDays(-2)), road, route, type, defect);
        await db.SaveChangesAsync();
        var obligation = RepairObligation.Create(Guid.NewGuid(), project.Id, defect.Id, RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), road.Id, "TEST_ONLY_FRAME_V1", "TEST_ONLY", 1, 2, 0, 1));
        db.Add(RepairPackage.Create(Guid.NewGuid(), project.Id, defect.Id, [obligation])); await db.SaveChangesAsync();

        var set = RoadSegmentSet.Create(Guid.NewGuid(), route.Id); var segment1 = RoadSegment.Create(Guid.NewGuid(), set.Id, route.Id, 1); var segment2 = RoadSegment.Create(Guid.NewGuid(), set.Id, route.Id, 2);
        db.AddRange(set, segment1, segment2); await db.SaveChangesAsync();
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(); using var supClient = factory.CreateClient(); await Login(client, pm.Email!); await Login(supClient, supervisor.Email!);
        var path = $"/api/v1/projects/{project.Id}/defect-statistics";
        var initial = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        var read = (await initial.Content.ReadFromJsonAsync<DefectStatisticsRead>())!; var scope = Assert.Single(read.Scopes);
        var input = new DefectStatisticsInputDto(obligation.Id, scope.ScopeHash, scope.DefectVersion, Guid.NewGuid(), [segment1.Id, segment2.Id], [], "TEST_ONLY", "TEST_ONLY explicit identity");
        Assert.Equal(HttpStatusCode.PreconditionRequired, (await client.PostAsJsonAsync(path, input)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(supClient, path, input, Guid.NewGuid().ToString(), initial.Headers.ETag!.ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/defect-statistics")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path, input with { DefectVersion = "stale" }, Guid.NewGuid().ToString(), initial.Headers.ETag!.ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, path, input with { Quantities = [null!] }, Guid.NewGuid().ToString(), initial.Headers.ETag!.ToString())).StatusCode);
        var key = Guid.NewGuid().ToString(); var confirmed = await Send(client, path, input, key, initial.Headers.ETag!.ToString()); Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
        var actual = (await confirmed.Content.ReadFromJsonAsync<DefectStatisticsRead>())!; Assert.Equal(1, actual.Statistics.ProjectDistinctDefects);
        Assert.All(actual.Statistics.Segments, row => Assert.Equal(1, row.RelatedDefects)); Assert.Equal(1, actual.Statistics.SharedParts);
        Assert.Equal("UNKNOWN", Assert.Single(actual.Statistics.Quantities).ValueState); Assert.Null(actual.Statistics.Quantities[0].Value);
        Assert.Equal(HttpStatusCode.OK, (await Send(client, path, input, key, initial.Headers.ETag.ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, path, input with { Reason = "changed" }, key, initial.Headers.ETag.ToString())).StatusCode);
        var summaryResponse = await client.GetAsync($"/api/v1/projects/{project.Id}/reports/summary"); Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        var summary = (await summaryResponse.Content.ReadFromJsonAsync<ProjectSummaryV1>())!;
        Assert.Equal(1, Assert.Single(summary.Metrics.Where(row => row.Code == "projectDistinctDefects")).Value);
        Assert.Equal(2, summary.Metrics.Count(row => row.Code == "segmentRelatedDefects"));
        var exportRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{project.Id}/exports") { Content = JsonContent.Create(new CreateExportRequestDto("DOSSIER", "ZIP")) };
        exportRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString()); var exported = await client.SendAsync(exportRequest); Assert.Equal(HttpStatusCode.Accepted, exported.StatusCode);
        var job = (await exported.Content.ReadFromJsonAsync<ExportJobViewDto>())!;
        var snapshot = await db.Set<ExportSnapshot>().AsNoTracking().SingleAsync(row => row.Id == job.SnapshotId); var oldJson = snapshot.PayloadJson; var oldHash = snapshot.Hash;
        using (var json = JsonDocument.Parse(oldJson)) Assert.Equal(1, json.RootElement.GetProperty("dossier").GetProperty("defectStatistics").GetProperty("projectDistinctDefects").GetInt64());
        var fresh = await client.GetAsync(path); var current = (await fresh.Content.ReadFromJsonAsync<DefectStatisticsRead>())!;
        var next = await Send(client, path, input with { SegmentIds = [segment2.Id], SupersedesId = actual.History.Single().Id, Reason = "TEST_ONLY explicit changed association" }, Guid.NewGuid().ToString(), fresh.Headers.ETag!.ToString()); Assert.Equal(HttpStatusCode.Created, next.StatusCode);
        var changed = (await next.Content.ReadFromJsonAsync<DefectStatisticsRead>())!;
        Assert.Equal(0, changed.Statistics.Segments.Single(row => row.SegmentId == segment1.Id).RelatedDefects);
        var retained = await db.Set<ExportSnapshot>().AsNoTracking().SingleAsync(row => row.Id == job.SnapshotId); Assert.Equal(oldJson, retained.PayloadJson); Assert.Equal(oldHash, retained.Hash);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/projects/{project.Id}/exports/{job.Id}/manifest")).StatusCode);
        await db.ProjectMembers.Where(row => row.ProjectId == project.Id && row.UserId == pm.Id).ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(client, path, input, key, initial.Headers.ETag.ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/projects/{project.Id}/exports/{job.Id}/manifest")).StatusCode);
    }
    [Fact]
    public async Task CookieStatisticsWriteRequiresCsrfBeforeSourceOrReceiptEffects()
    {
        var actor = await fixture.CreateUserAsync("ld08-cookie-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var csrf = (await (await client.GetAsync("/api/v1/auth/web/csrf")).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login", new { email = actor.Email, password = "Current1!" })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var response = await client.PostAsJsonAsync($"/api/v1/projects/{Guid.NewGuid()}/defect-statistics", new { });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); Assert.Equal("csrf_failed", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using var db = fixture.CreateDbContext(); Assert.False(await db.Set<DefectStatisticsSource>().AnyAsync(row => row.ActorId == actor.Id));
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
    }
    private static async Task Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
    private static Task<HttpResponseMessage> Send(HttpClient client, string path, object input, string key, string version)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(input) };
        request.Headers.Add("Idempotency-Key", key); request.Headers.TryAddWithoutValidation("If-Match", version); return client.SendAsync(request);
    }
}
