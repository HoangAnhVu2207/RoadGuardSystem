using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.Repositories.Implementations.Defects;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reports;

public sealed partial class Huy01ReporterReportsApiTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(true, 32648)]
    [InlineData(true, 32649)]
    public async Task MatchingConfirmationPreservesEvidenceScopeVersionsReplayAndCurrentAuthority(bool includeGps, int metricSrid)
    {
        var pm = await sql.CreateUserAsync("match-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var sup = await sql.CreateUserAsync("match-sup-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var reporter = await sql.CreateUserAsync("match-reporter-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        var project = Guid.NewGuid();
        await using var db = sql.CreateDbContext();
        db.AddRange(Project.Create(project, project.ToString(), "Matching fixture", null, metricSrid == 0 ? 32648 : metricSrid, null, null, DateTimeOffset.UtcNow),
            ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, pm.Id, new(2000, 1, 1)));
        var type = DefectType.Create("MATCH-" + Guid.NewGuid().ToString("N"), "Matching fixture"); db.Add(type);
        await db.SaveChangesAsync();
        using var logs = new InMemoryLogProvider();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, loggerProvider: logs, configureTestServices: services =>
        { services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage()); });
        using var client = factory.CreateClient(new() { BaseAddress = new("https://localhost") });
        await LoginAsync(client, reporter.UserName!);
        var file = await UploadVerifiedAsync(client, factory);
        var secondFile = await UploadVerifiedAsync(client, factory);
        var created = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", new
        {
            description = "Multiple original positions; PM must select explicitly",
            evidence = new[]
            {
                new { fileId = file.FileId, fileVersion = file.Version, locationSource = includeGps ? "CAPTURE" : "UNKNOWN", location = includeGps ? new { latitude = metricSrid == 0 ? 0m : 10m, longitude = metricSrid == 0 ? 0m : metricSrid == 32648 ? 105m : 111m, accuracyMeters = (decimal?)null } : null },
                new { fileId = secondFile.FileId, fileVersion = secondFile.Version, locationSource = includeGps ? "MANUAL" : "UNKNOWN", location = includeGps ? new { latitude = 20m, longitude = 20m, accuracyMeters = (decimal?)4m } : null }
            }
        }, Guid.NewGuid().ToString());
        Assert.True(created.StatusCode == HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var report = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var incident = (await db.Set<HuyCaseReportLink>().SingleAsync(row => row.ReportId == report)).CaseId;
        var geometry = await CreatePublishedGeometryAsync(client, pm.UserName!, sup.UserName!, project, metricSrid == 0 ? 32648 : metricSrid);
        await LoginAsync(client, sup.UserName!);
        using var caseRead = await client.GetAsync($"/api/v1/cases/{incident}");
        using var triaged = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{incident}/triage", new
        { projectId = project, verificationMethod = "EXISTING_EVIDENCE", reason = "Matching pins", routeVersionId = geometry.Route, segmentSetId = geometry.Set, geometryVersion = geometry.Version },
            Guid.NewGuid().ToString(), caseRead.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.OK, triaged.StatusCode);
        var segments = await db.RoadSegments.Where(row => row.SegmentSetId == geometry.Set).OrderBy(row => row.Sequence).Select(row => row.Id).ToArrayAsync();
        Assert.Equal(3, segments.Length);
        var targetIds = new List<Guid>();
        for (var i = 0; i < segments.Length; i++)
        {
            var targetReport = Report.Create(Guid.NewGuid(), reporter.Id, "Other retained source", DateTimeOffset.UtcNow,
                [VerifiedEvidenceReference.Create(Guid.NewGuid(), file.FileId, file.Version, reporter.Id)]);
            var targetCase = IncidentCase.CreateUnassigned(Guid.NewGuid(), targetReport.Id, DateTimeOffset.UtcNow);
            db.AddRange(targetReport, targetCase); db.Add(new HuyCaseReportLink { Id = Guid.NewGuid(), ReportId = targetReport.Id, CaseId = targetCase.Id, StartedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
            var saved = await new CandidateDecisionRepository(db).SaveAcceptedAsync(pm.Id,
                CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.Report, targetReport.Id, "controlled-fixture"), project, geometry.Version),
                CandidateDecisionKind.KeepNew, CandidateClassification.Create(geometry.Route, type.Code, null, DefectSeverity.Low, segments[i]),
                null, null, null, "Fixture target", null, default);
            targetIds.Add(saved.DefectId!.Value);
        }
        var geographic = Defect.Create(Guid.NewGuid(), project, geometry.Route, null, type.Code, null, DefectSeverity.Low,
            DefectStatus.Open, new Point(1, 0) { SRID = 4326 }, DateTimeOffset.UtcNow); db.Add(geographic); await db.SaveChangesAsync();
        var metricIds = new List<Guid>();
        if (metricSrid != 0)
        {
            const double northing = 1105412.4913010783; // PROJ WGS84 105/111E,10N.
            Geometry[] shapes = [new Point(500010, northing) { SRID = metricSrid },
                new LineString([new(500020, northing - 5), new(500020, northing + 5)]) { SRID = metricSrid },
                new Polygon(new LinearRing([new(499995,northing-5),new(500005,northing-5),new(500005,northing+5),new(499995,northing+5),new(499995,northing-5)])) { SRID = metricSrid },
                new Point(1, 1) { SRID = 0 },
                new Polygon(new LinearRing([new(500000,northing),new(500010,northing+10),new(500000,northing+10),new(500010,northing),new(500000,northing)])) { SRID = metricSrid },
                new Point(0, northing) { SRID = metricSrid }];
            foreach (var shape in shapes)
            {
                var defect = Defect.Create(Guid.NewGuid(), project, geometry.Route, null, type.Code, null,
                    DefectSeverity.Low, DefectStatus.Open, shape, DateTimeOffset.UtcNow);
                db.Add(defect); metricIds.Add(defect.Id);
            }
            await db.SaveChangesAsync();
        }
        using var web = await WebCookieTestClient.Login(factory, pm.Email!);
        var root = $"/api/v1/projects/{project}"; var path = root + $"/reports/{report}/matching-location";
        var initial = await web.GetFromJsonAsync<JsonElement>(path);
        Assert.Equal(includeGps ? "GPS_UNCONFIRMED" : "GPS_MISSING", initial.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, initial.GetProperty("confirmation").ValueKind);
        // Select the zero coordinate explicitly, independently of evidence ordering.
        Guid? evidenceId = includeGps ? initial.GetProperty("evidence").EnumerateArray().Single(e => e.GetProperty("latitude").GetDecimal() == (metricSrid == 0 ? 0 : 10)).GetProperty("evidenceId").GetGuid() : null;
        var input = new
        {
            sourceVersion = initial.GetProperty("sourceVersion").GetString(),
            geometryVersion = geometry.Version,
            evidenceId,
            segmentIds = new[] { segments[0] },
            reason = "PM verifies this evidence and segment; no automatic snapping"
        };
        using (var missing = factory.CreateClient(new() { BaseAddress = new("https://localhost"), HandleCookies = false }))
            Assert.Equal(HttpStatusCode.Unauthorized, (await missing.GetAsync(path)).StatusCode);
        web.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(web, HttpMethod.Post, path, input, Guid.NewGuid().ToString())).StatusCode);
        await WebCookieTestClient.RefreshCsrf(web);
        var key = Guid.NewGuid().ToString();
        var raced = await Task.WhenAll(SendAsync(web, HttpMethod.Post, path, input, key), SendAsync(web, HttpMethod.Post, path, input, key));
        using var confirmed = raced[0];
        using var concurrentReplay = raced[1];
        Assert.True(confirmed.StatusCode == HttpStatusCode.OK, await confirmed.Content.ReadAsStringAsync());
        var confirmedText = await confirmed.Content.ReadAsStringAsync();
        Assert.True(concurrentReplay.StatusCode == HttpStatusCode.OK, await concurrentReplay.Content.ReadAsStringAsync());
        Assert.Equal(confirmedText, await concurrentReplay.Content.ReadAsStringAsync());
        if (metricSrid != 0)
        {
            var measuredPage = await web.GetFromJsonAsync<JsonElement>(root + $"/candidates?sourceKind=REPORT&sourceId={report}&expand=true&pageSize=200");
            var measuredItems = measuredPage.GetProperty("items").EnumerateArray().ToArray();
            for (var n = 0; n < 3; n++)
            {
                var item = measuredItems.Single(i => i.GetProperty("defectId").GetGuid() == metricIds[n]);
                Assert.Equal("MEASURED", item.GetProperty("distanceStatus").GetString());
                Assert.Equal("WGS84_UTM_GRID_MINIMUM", item.GetProperty("distanceMethod").GetString());
                Assert.InRange(item.GetProperty("distanceMeters").GetDouble(), new[] { 10d, 20d, 0d }[n] - .01, new[] { 10d, 20d, 0d }[n] + .01);
                Assert.Equal("SOURCE_UNCERTAINTY_UNAVAILABLE", item.GetProperty("overlapStatus").GetString());
                Assert.False(item.TryGetProperty("accuracyOverlap", out _));
            }
            var unsupported = measuredItems.Single(i => i.GetProperty("defectId").GetGuid() == metricIds[3]);
            Assert.Equal("TARGET_CRS_UNSUPPORTED", unsupported.GetProperty("distanceStatus").GetString());
            Assert.Equal(JsonValueKind.Null, unsupported.GetProperty("distanceMeters").ValueKind);
            foreach (var (index, status) in new[] { (4, "TARGET_GEOMETRY_INVALID"), (5, "TARGET_COORDINATES_OUTSIDE_CRS_AREA") })
            {
                var unavailable = measuredItems.Single(i => i.GetProperty("defectId").GetGuid() == metricIds[index]);
                Assert.Equal(status, unavailable.GetProperty("distanceStatus").GetString());
                Assert.Equal(JsonValueKind.Null, unavailable.GetProperty("distanceMeters").ValueKind);
            }
        }
        using var replay = await SendAsync(web, HttpMethod.Post, path, input, key);
        Assert.True(replay.StatusCode == HttpStatusCode.OK, await replay.Content.ReadAsStringAsync());
        Assert.Equal(confirmedText, await replay.Content.ReadAsStringAsync());
        using var fingerprintConflict = await SendAsync(web, HttpMethod.Post, path, input with { reason = "Different request with same key" }, key);
        Assert.Equal(HttpStatusCode.Conflict, fingerprintConflict.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(web, HttpMethod.Post, path, input with { }, Guid.NewGuid().ToString())).StatusCode);
        var matching = root + $"/candidates?sourceKind=REPORT&sourceId={report}";
        using var scopedResponse = await web.GetAsync(matching);
        Assert.True(scopedResponse.StatusCode == HttpStatusCode.OK, await scopedResponse.Content.ReadAsStringAsync() + logs.CombinedEntries);
        var scoped = await scopedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { targetIds[0], targetIds[1] }, scoped.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("defectId").GetGuid()));
        Assert.Equal("ASSIGNED_SEGMENT", scoped.GetProperty("items")[0].GetProperty("reasonCodes")[0].GetString());
        Assert.All(scoped.GetProperty("items").EnumerateArray(), x => Assert.DoesNotContain("GPS_MISSING", x.GetProperty("reasonCodes").EnumerateArray().Select(v => v.GetString())));
        var expanded = await web.GetFromJsonAsync<JsonElement>(matching + "&expand=true");
        var measured = expanded.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("defectId").GetGuid() == geographic.Id);
        if (includeGps && metricSrid == 0) Assert.InRange(measured.GetProperty("distanceMeters").GetDouble(), 111300, 111340); // WGS84 equator, one degree longitude.
        else if (!includeGps) Assert.Equal(JsonValueKind.Null, measured.GetProperty("distanceMeters").ValueKind);
        Assert.Equal(JsonValueKind.Null, measured.GetProperty("accuracyMeters").ValueKind);
        using var linked = await SendAsync(web, HttpMethod.Post, root + "/candidate-decisions", new
        {
            sourceKind = "REPORT",
            sourceId = report,
            sourceVersion = expanded.GetProperty("source").GetProperty("version").GetString(),
            geometryVersion = geometry.Version,
            decision = "LINK_EXISTING",
            targetDefectId = targetIds[0],
            targetVersion = scoped.GetProperty("items")[0].GetProperty("version").GetString(),
            reason = "Explicit PM link, never automatic matching"
        }, Guid.NewGuid().ToString());
        Assert.True(linked.StatusCode == HttpStatusCode.Created, await linked.Content.ReadAsStringAsync());
        var historical = await web.GetFromJsonAsync<JsonElement>(matching);
        Assert.Contains("HISTORICAL_LINK", historical.GetProperty("items")[0].GetProperty("reasonCodes").EnumerateArray().Select(v => v.GetString()));
        Assert.Equal(2, historical.GetProperty("items")[0].GetProperty("evidenceRefs").GetArrayLength());
        var page = await web.GetFromJsonAsync<JsonElement>(matching + "&expand=true&pageSize=1");
        var cursor = page.GetProperty("nextCursor").GetString(); Assert.NotNull(cursor);
        var latest = await web.GetFromJsonAsync<JsonElement>(path);
        using var replacement = await SendAsync(web, HttpMethod.Post, path, new
        { sourceVersion = latest.GetProperty("sourceVersion").GetString(), geometryVersion = geometry.Version, evidenceId = (Guid?)null, segmentIds = new[] { segments[2] }, reason = "Explicit scope without a representative GPS" }, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.OK, replacement.StatusCode);
        Assert.Equal("SCOPE_CONFIRMED", (await replacement.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await web.GetAsync(matching + "&expand=true&pageSize=1&cursor=" + Uri.EscapeDataString(cursor!))).StatusCode);
        using var supWeb = await WebCookieTestClient.Login(factory, sup.Email!);
        Assert.Equal(HttpStatusCode.Forbidden, (await supWeb.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await supWeb.GetAsync(matching)).StatusCode);
        var original = await db.Reports.AsNoTracking().Include(row => row.Supplements).SingleAsync(row => row.Id == report);
        if (includeGps) Assert.Equal(new[] { metricSrid == 0 ? 0m : 10m, 20m }, original.OriginalEvidence.Select(e => e.CaptureMetadata!.Latitude!.Value).Order());
        else Assert.All(original.OriginalEvidence, e => Assert.Null(e.CaptureMetadata?.Latitude));
        Assert.Equal(2, await db.Set<ReportMatchingLocation>().CountAsync(row => row.ReportId == report));
        var inventory = await new RoadGuardSystem.Repositories.Implementations.Retention.Huy01RetentionInventoryContributor(db).ReadAsync(file.FileId, default);
        Assert.Equal(2, inventory.References.Count(row => row.Kind == "REPORT_MATCHING_LOCATION" && row.ProjectId == project));
        var multiHead = await web.GetFromJsonAsync<JsonElement>(path);
        using var multiScope = await SendAsync(web, HttpMethod.Post, path, new
        {
            sourceVersion = multiHead.GetProperty("sourceVersion").GetString(),
            geometryVersion = geometry.Version,
            evidenceId = (Guid?)null,
            segmentIds = new[] { segments[0], segments[1] },
            reason = "PM confirms two explicit segments without synthesizing a point"
        }, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.OK, multiScope.StatusCode);
        var multiCandidates = await web.GetFromJsonAsync<JsonElement>(matching);
        foreach (var id in targetIds.Take(2))
            Assert.Contains("ASSIGNED_SEGMENT", multiCandidates.GetProperty("items").EnumerateArray()
                .Single(i => i.GetProperty("defectId").GetGuid() == id).GetProperty("reasonCodes").EnumerateArray().Select(x => x.GetString()));
        var currentLocation = await web.GetFromJsonAsync<JsonElement>(path);
        var invalid = new
        {
            sourceVersion = currentLocation.GetProperty("sourceVersion").GetString(),
            geometryVersion = geometry.Version,
            evidenceId = Guid.NewGuid(),
            segmentIds = new[] { segments[0] },
            reason = "Unknown evidence is never promoted"
        };
        using var rejected = await SendAsync(web, HttpMethod.Post, path, invalid, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Equal("matching_evidence_location_unavailable", (await rejected.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await db.ProjectMembers.Where(row => row.ProjectId == project && row.UserId == pm.Id).ExecuteUpdateAsync(s => s.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        Assert.Equal(HttpStatusCode.Forbidden, (await web.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(web, HttpMethod.Post, path, input, key)).StatusCode);
    }
}
