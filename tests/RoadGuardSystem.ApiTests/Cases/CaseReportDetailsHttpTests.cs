using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.ApiTests.Cases;

[Collection(AuthenticationApiFixture.Name)]
public sealed class CaseReportDetailsHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task RuntimeOpenApiListsCaseReportDetailAndPrivateContentRoutes()
    {
        await using var factory = new CustomWebApplicationFactory("Development");
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var list = paths.GetProperty("/api/v1/cases/{caseId}/reports").GetProperty("get");
        Assert.True(list.GetProperty("responses").TryGetProperty("200", out _));
        Assert.True(list.GetProperty("responses").TryGetProperty("409", out _));
        Assert.True(list.GetProperty("responses").TryGetProperty("412", out _));
        Assert.Contains("current account", list.GetProperty("description").GetString());
        var content = paths.GetProperty("/api/v1/cases/{caseId}/reports/{reportId}/evidence/{evidenceId}/content")
            .GetProperty("get");
        Assert.True(content.GetProperty("responses").TryGetProperty("200", out _));
        Assert.Equal("binary", content.GetProperty("responses").GetProperty("200").GetProperty("content")
            .GetProperty("image/jpeg").GetProperty("schema").GetProperty("format").GetString());
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.TryGetProperty("CaseReportDetailPageDto", out _));
        Assert.True(schemas.TryGetProperty("CaseReportEvidenceDto", out _));
    }

    [Fact]
    public async Task CurrentReporterContactEvidenceAndCaseAuthorityFollowActiveLinks()
    {
        var sup = await User(UserRoleCode.Supervisor);
        var pm = await User(UserRoleCode.ProjectManager);
        var outsider = await User(UserRoleCode.ProjectManager);
        var reporterA = await User(UserRoleCode.Reporter);
        var reporterB = await User(UserRoleCode.Reporter);
        var project = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(Project.Create(project, "DETAIL-" + Guid.NewGuid().ToString("N"), "Case details", null,
                32648, new(2026, 1, 1), new(2027, 1, 1), DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, pm.Id, new(2026, 1, 1)));
            await db.SaveChangesAsync();
        }
        var a = await Intake(reporterA.Id, true);
        var b = await Intake(reporterB.Id, false);
        var storage = new PhotoStorage();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: services => { services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(storage); });
        using var supervisor = await Client(factory, sup.Email!);
        using var manager = await Client(factory, pm.Email!);
        using var wrongPm = await Client(factory, outsider.Email!);
        using var reporter = await Client(factory, reporterA.Email!);
        using var web = await WebCookieTestClient.Login(factory, pm.Email!);
        var aPath = $"/api/v1/cases/{a.CaseId}/reports";
        var bPath = $"/api/v1/cases/{b.CaseId}/reports";
        var unassigned = await Get(supervisor, aPath);
        Assert.Equal(a.ReportId, Assert.Single(unassigned.GetProperty("items").EnumerateArray()).GetProperty("reportId").GetGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync(aPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reporter.GetAsync(aPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await web.GetAsync(aPath)).StatusCode);
        await Triage(supervisor, a.CaseId, project);
        await Triage(supervisor, b.CaseId, project);
        Assert.Equal(HttpStatusCode.Forbidden, (await wrongPm.GetAsync(aPath)).StatusCode);
        var original = await Get(manager, aPath);
        Assert.Single((await Get(web, aPath)).GetProperty("items").EnumerateArray());
        var first = Assert.Single(original.GetProperty("items").EnumerateArray());
        Assert.Equal(reporterA.Id, first.GetProperty("reporterUserId").GetGuid());
        Assert.Equal(JsonValueKind.Null, first.GetProperty("reporterPhoneNumber").ValueKind);
        Assert.Equal("Original A", first.GetProperty("description").GetString());
        Assert.Equal(2, first.GetProperty("supplements").GetArrayLength());
        var supplements = first.GetProperty("supplements").EnumerateArray().ToArray();
        Assert.Equal("Supplement 1", supplements[0].GetProperty("description").GetString());
        Assert.Equal("Supplement 2", supplements[1].GetProperty("description").GetString());
        Assert.True(supplements[0].GetProperty("receivedAt").GetDateTimeOffset() <
            supplements[1].GetProperty("receivedAt").GetDateTimeOffset());
        foreach (var supplement in supplements)
        {
            var supplementEvidence = Assert.Single(supplement.GetProperty("evidence").EnumerateArray());
            Assert.Equal(supplement.GetProperty("id").GetGuid(), supplementEvidence.GetProperty("supplementId").GetGuid());
            Assert.Equal("UNKNOWN", supplementEvidence.GetProperty("locationSource").GetString());
            Assert.Equal(JsonValueKind.Null, supplementEvidence.GetProperty("location").ValueKind);
        }
        var evidence = Assert.Single(first.GetProperty("originalEvidence").EnumerateArray());
        Assert.Equal("CAPTURE", evidence.GetProperty("locationSource").GetString());
        Assert.Equal(10.5m, evidence.GetProperty("location").GetProperty("latitude").GetDecimal());
        Assert.Equal(106.25m, evidence.GetProperty("location").GetProperty("longitude").GetDecimal());
        Assert.Equal(5m, evidence.GetProperty("location").GetProperty("accuracyMeters").GetDecimal());
        Assert.Equal(a.CapturedAt, evidence.GetProperty("capturedAt").GetDateTimeOffset());
        var contentPath = evidence.GetProperty("contentPath").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync(contentPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await wrongPm.GetAsync(contentPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reporter.GetAsync(contentPath)).StatusCode);
        Assert.Equal(1, storage.Reads);
        await using (var db = sql.CreateDbContext())
            await db.Users.Where(u => u.Id == reporterA.Id).ExecuteUpdateAsync(u =>
                u.SetProperty(x => x.DisplayName, "Current Reporter A").SetProperty(x => x.PhoneNumber, "0901234567"));
        var current = Assert.Single((await Get(manager, aPath)).GetProperty("items").EnumerateArray());
        Assert.Equal("Current Reporter A", current.GetProperty("reporterDisplayName").GetString());
        Assert.Equal("0901234567", current.GetProperty("reporterPhoneNumber").GetString());
        var target = await manager.GetAsync($"/api/v1/cases/{a.CaseId}");
        var source = await manager.GetAsync($"/api/v1/cases/{b.CaseId}");
        using var link = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/cases/{a.CaseId}/report-links")
        {
            Content = JsonContent.Create(new
            {
                reportIds = new[] { b.ReportId },
                sourceCaseVersions = new Dictionary<Guid, string>
                { [b.CaseId] = (await source.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetString()! },
                reason = "Merge details"
            })
        };
        link.Headers.Add("If-Match", target.Headers.ETag!.Tag);
        link.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.OK, (await manager.SendAsync(link)).StatusCode);
        Assert.Equal(2, (await Get(manager, aPath)).GetProperty("items").GetArrayLength());
        var page1 = await Get(manager, aPath + "?limit=1");
        var nextCursor = page1.GetProperty("nextCursor").GetString();
        Assert.False(string.IsNullOrWhiteSpace(nextCursor));
        var page2 = await Get(manager, aPath + "?limit=1&cursor=" + Uri.EscapeDataString(nextCursor));
        Assert.NotEqual(Assert.Single(page1.GetProperty("items").EnumerateArray()).GetProperty("reportId").GetGuid(),
            Assert.Single(page2.GetProperty("items").EnumerateArray()).GetProperty("reportId").GetGuid());
        Assert.Equal(JsonValueKind.Null, page2.GetProperty("nextCursor").ValueKind);
        Assert.Empty((await Get(manager, bPath)).GetProperty("items").EnumerateArray());
        var merged = await Get(manager, aPath);
        var second = Assert.Single(merged.GetProperty("items").EnumerateArray(), x => x.GetProperty("reportId").GetGuid() == b.ReportId);
        Assert.Equal(reporterB.Id, second.GetProperty("reporterUserId").GetGuid());
        Assert.Equal("Original B", second.GetProperty("description").GetString());
        Assert.Equal(JsonValueKind.Null, second.GetProperty("reporterPhoneNumber").ValueKind);
        var secondEvidence = Assert.Single(second.GetProperty("originalEvidence").EnumerateArray());
        Assert.Equal("UNKNOWN", secondEvidence.GetProperty("locationSource").GetString());
        Assert.Equal(JsonValueKind.Null, secondEvidence.GetProperty("location").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, (await manager.GetAsync(secondEvidence.GetProperty("contentPath").GetString()!
            .Replace(a.CaseId.ToString("D"), b.CaseId.ToString("D"), StringComparison.Ordinal))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync(secondEvidence.GetProperty("contentPath").GetString())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await web.GetAsync(contentPath)).StatusCode);
        var splitSource = await manager.GetAsync($"/api/v1/cases/{a.CaseId}");
        using var split = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/cases/{a.CaseId}/report-splits")
        { Content = JsonContent.Create(new { reportIds = new[] { b.ReportId }, reason = "Split details" }) };
        split.Headers.Add("If-Match", splitSource.Headers.ETag!.Tag);
        split.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var splitResponse = await manager.SendAsync(split);
        Assert.Equal(HttpStatusCode.Created, splitResponse.StatusCode);
        var newCaseId = (await splitResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await manager.GetAsync(secondEvidence.GetProperty("contentPath").GetString())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync(secondEvidence.GetProperty("contentPath").GetString()!
            .Replace(a.CaseId.ToString("D"), newCaseId.ToString("D"), StringComparison.Ordinal))).StatusCode);
    }

    private async Task<(Guid CaseId, Guid ReportId, DateTimeOffset CapturedAt)> Intake(Guid owner, bool gps)
    {
        await using var db = sql.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var file = StoredFile.Create(Guid.NewGuid(), "private/" + Guid.NewGuid().ToString("N"), "photo.jpg",
            "image/jpeg", 4, new string('a', 64), owner, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, owner, file.StorageUri, "REPORT_PHOTO",
            "image/jpeg", 4, new string('a', 64), 8388608, now.AddHours(24));
        db.Files.Add(file); db.FileScopes.Add(FileScope.CreatePrivate(Guid.NewGuid(), file.Id, owner, now));
        db.UploadSessions.Add(upload); await db.SaveChangesAsync();
        upload.StartUploading("fixture", now); await db.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync();
        upload.MarkVerified(); await db.SaveChangesAsync();
        var metadata = gps ? EvidenceCaptureMetadata.Create(now.AddMinutes(-3), EvidenceLocationSource.Capture,
            10.5m, 106.25m, 5m) : EvidenceCaptureMetadata.Create(null, EvidenceLocationSource.Unknown);
        var report = Report.Create(Guid.NewGuid(), owner, gps ? "Original A" : "Original B", now,
            [VerifiedEvidenceReference.Create(Guid.NewGuid(), file.Id, Convert.ToBase64String(upload.RowVersion), owner, metadata)]);
        if (gps)
            for (var i = 0; i < 2; i++)
                report.AddSupplement(Guid.NewGuid(), $"Supplement {i + 1}",
                    [VerifiedEvidenceReference.Create(Guid.NewGuid(), file.Id, Convert.ToBase64String(upload.RowVersion), owner)], now.AddMinutes(i + 1));
        var incident = IncidentCase.CreateUnassigned(Guid.NewGuid(), report.Id, now);
        db.Reports.Add(report); db.IncidentCases.Add(incident);
        db.Set<HuyCaseReportLink>().Add(new() { Id = Guid.NewGuid(), CaseId = incident.Id, ReportId = report.Id, StartedAt = now });
        db.Entry(report).Property<long>("Revision").CurrentValue = 1;
        db.Entry(incident).Property<long>("Revision").CurrentValue = 1;
        await db.SaveChangesAsync();
        return (incident.Id, report.Id, now.AddMinutes(-3));
    }

    private async Task Triage(HttpClient supervisor, Guid caseId, Guid project)
    {
        var read = await supervisor.GetAsync($"/api/v1/cases/{caseId}");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/cases/{caseId}/triage")
        { Content = JsonContent.Create(new { projectId = project, verificationMethod = "EXISTING_EVIDENCE", reason = "Detail test" }) };
        request.Headers.Add("If-Match", read.Headers.ETag!.Tag);
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.OK, (await supervisor.SendAsync(request)).StatusCode);
    }

    private Task<RoadGuardSystem.BusinessObjects.Identity.ApplicationUser> User(UserRoleCode role) =>
        sql.CreateUserAsync("DETAIL-" + Guid.NewGuid().ToString("N"), "Current1!", role);

    private static async Task<HttpClient> Client(AuthenticationWebApplicationFactory factory, string email)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<JsonElement> Get(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private sealed class PhotoStorage : IUploadObjectStorage
    {
        public int Reads { get; private set; }
        public Task<string> InitiateAsync(string key, string mediaType, CancellationToken token = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string key, string uploadId,
            IReadOnlyList<int> parts, DateTimeOffset expiresAt, CancellationToken token = default) => throw new NotSupportedException();
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string key, string uploadId,
            IReadOnlyList<CompletedStoragePart> parts, CancellationToken token = default) => throw new NotSupportedException();
        public Task<Stream> OpenReadAsync(string key, CancellationToken token = default)
        {
            Reads++;
            return Task.FromResult<Stream>(new MemoryStream([1, 2, 3, 4], false));
        }
    }
}
