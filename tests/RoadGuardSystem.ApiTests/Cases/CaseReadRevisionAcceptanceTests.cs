using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Reports;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Services.Implementations.Reports;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Reports;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Options;
using Xunit;

namespace RoadGuardSystem.ApiTests.Cases;

[Collection(AuthenticationApiFixture.Name)]
public sealed class CaseReadRevisionAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadTracksGenuineCaseTransitionsAndPersistedVersions(bool web)
    {
        var g = await Prepare();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var pm = await Client(factory, g.PmEmail, web);
        async Task<JsonElement> Check(Guid id, string status)
        {
            var before = await Snapshot(g);
            var body = await Read(pm, id, g.Project);
            Assert.Equal(status, body.GetProperty("status").GetString());
            await using var db = sql.CreateDbContext();
            var persisted = await db.IncidentCases.AsNoTracking().Where(c => c.Id == id)
                .Select(c => new { Version = EF.Property<byte[]>(c, "RowVersion") }).SingleAsync();
            Assert.Equal(Convert.ToBase64String(persisted.Version), body.GetProperty("version").GetString());
            Assert.Equal(before, await Snapshot(g));
            Assert.Equal(body.GetRawText(), (await Read(pm, id, g.Project)).GetRawText());
            return body;
        }
        var initial = await Check(g.CaseA, "OPEN");
        Assert.False(initial.TryGetProperty("conclusion", out _));
        var source = await Check(g.CaseB, "OPEN");
        using (var linked = await Send(pm, g, "link", initial, source, Guid.NewGuid().ToString(), "Read revision link"))
            Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
        var current = await Check(g.CaseA, "OPEN");
        Assert.NotEqual(initial.GetProperty("version").GetString(), current.GetProperty("version").GetString());
        var linkedSource = await Check(g.CaseB, "LINKED");
        Assert.Empty(linkedSource.GetProperty("reportIds").EnumerateArray());
        Assert.NotEqual(source.GetProperty("version").GetString(), linkedSource.GetProperty("version").GetString());
        async Task Conclude(string outcome, JsonElement version)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/cases/{g.CaseA}/conclusions")
            { Content = JsonContent.Create(new { outcome, defectIds = Array.Empty<Guid>(), evidenceIds = new[] { g.EvidenceA }, reason = "Read lifecycle source" }) };
            request.Headers.Add("If-Match", '"' + version.GetProperty("version").GetString() + '"');
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            using var response = await pm.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        await Conclude("NEEDS_EVIDENCE", current);
        var awaiting = await Check(g.CaseA, "AWAITING_EVIDENCE");
        Assert.Equal("NEEDS_EVIDENCE", awaiting.GetProperty("conclusion").GetProperty("outcome").GetString());
        Assert.NotEqual(current.GetProperty("version").GetString(), awaiting.GetProperty("version").GetString());
        await Conclude("NO_DEFECT", awaiting);
        var final = await Check(g.CaseA, "CONCLUDED");
        Assert.Equal("NO_DEFECT", final.GetProperty("conclusion").GetProperty("outcome").GetString());
        Assert.NotEqual(awaiting.GetProperty("version").GetString(), final.GetProperty("version").GetString());
        Assert.Equal(linkedSource.GetRawText(), (await Check(g.CaseB, "LINKED")).GetRawText());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnassignedAndAbsentCaseReadsPreserveSourceAndExactErrors(bool web)
    {
        var reporter = await sql.CreateUserAsync("CASE-READ-R-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        var supervisor = await sql.CreateUserAsync("CASE-READ-S-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var intake = await Intake(reporter.Id);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Client(factory, supervisor.Email!, web);
        await using var db = sql.CreateDbContext();
        var original = await db.IncidentCases.AsNoTracking().Where(c => c.Id == intake.Case)
            .Select(c => new { c.Status, Version = EF.Property<byte[]>(c, "RowVersion") }).SingleAsync();
        var auditCount = await db.AuditLogs.CountAsync(a => a.EntityId == intake.Case);
        using var success = await client.GetAsync($"/api/v1/cases/{intake.Case}");
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        var body = await success.Content.ReadFromJsonAsync<JsonElement>(); CaseFields(body);
        Assert.Equal("UNASSIGNED", body.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("projectId").ValueKind);
        Assert.Equal(intake.Report, Assert.Single(body.GetProperty("reportIds").EnumerateArray()).GetGuid());
        Assert.Equal(Convert.ToBase64String(original.Version), body.GetProperty("version").GetString());
        Assert.Equal('"' + body.GetProperty("version").GetString() + '"', success.Headers.ETag?.Tag);
        foreach (var id in new[] { Guid.Empty, Guid.NewGuid() })
        {
            using var absent = await client.GetAsync($"/api/v1/cases/{id}");
            await Problem(absent, 404, "not_found");
        }
        // The :guid route constraint handles malformed IDs before the Case action.
        using var malformed = await client.GetAsync("/api/v1/cases/not-a-uuid");
        Assert.Equal(HttpStatusCode.NotFound, malformed.StatusCode);
        Assert.Null(malformed.Headers.ETag);
        Assert.Equal(original.Version, await db.IncidentCases.AsNoTracking().Where(c => c.Id == intake.Case)
            .Select(c => EF.Property<byte[]>(c, "RowVersion")).SingleAsync());
        Assert.Equal(auditCount, await db.AuditLogs.CountAsync(a => a.EntityId == intake.Case));
        Assert.False(await db.IdempotencyRecords.AnyAsync(r => r.ActorUserId == supervisor.Id && r.Operation.StartsWith("huy01.case.")));
    }

    private static async Task<JsonElement> Read(HttpClient client, Guid id, Guid project)
    {
        using var response = await client.GetAsync($"/api/v1/cases/{id}"); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(); CaseFields(body);
        Assert.Equal(id, body.GetProperty("id").GetGuid()); Assert.Equal(project, body.GetProperty("projectId").GetGuid());
        Assert.Equal('"' + body.GetProperty("version").GetString() + '"', response.Headers.ETag?.Tag);
        Assert.Matches("^\"[A-Za-z0-9+/]{11}=\"$", response.Headers.ETag?.Tag ?? "");
        Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), out _));
        Assert.NotEqual(default, body.GetProperty("createdAt").GetDateTimeOffset()); return body;
    }

    private static Task<HttpResponseMessage> Send(HttpClient c, Graph g, string action, JsonElement target, JsonElement source, string key, string reason)
    {
        object body = action switch
        {
            "triage" => new { projectId = g.Project, verificationMethod = "DRONE", reason },
            "link" => new { reportIds = new[] { g.ReportB }, sourceCaseVersions = new Dictionary<Guid, string> { [g.CaseB] = source.GetProperty("version").GetString()! }, reason },
            "split" => new { reportIds = new[] { g.ReportB }, reason },
            "conclude" => new { outcome = "NO_DEFECT", defectIds = Array.Empty<Guid>(), evidenceIds = new[] { g.EvidenceA }, reason },
            _ => new { reportIds = new[] { g.ReportA }, defectIds = Array.Empty<Guid>(), evidenceIds = new[] { g.EvidenceA }, summary = reason }
        };
        var route = action switch { "link" => "report-links", "split" => "report-splits", "conclude" => "conclusions", "publish" => "publications", _ => "triage" };
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/cases/{g.CaseA}/{route}") { Content = JsonContent.Create(body) };
        request.Headers.Add("If-Match", '"' + target.GetProperty("version").GetString() + '"'); request.Headers.Add("Idempotency-Key", key);
        return c.SendAsync(request);
    }

    private static void CaseFields(JsonElement e)
    {
        string[] names = ["id", "projectId", "status", "version", "reportIds", "defectIds", "verificationMethod", "warrantyRouting", "createdAt"];
        if (e.TryGetProperty("conclusion", out var conclusion))
        {
            Assert.Equal(JsonValueKind.Object, conclusion.ValueKind);
            names = [.. names, "conclusion"];
        }
        Fields(e, names);
    }
    private static void Fields(JsonElement e, params string[] names) => Assert.Equal(names.Order(), e.EnumerateObject().Select(p => p.Name).Order());
    private static async Task Problem(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal((HttpStatusCode)status, response.StatusCode); Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var b = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(b.TryGetProperty("status", out var actualStatus), "Missing status: " + b.GetRawText()); Assert.Equal(status, actualStatus.GetInt32());
        Assert.True(b.TryGetProperty("code", out var actualCode), "Missing code: " + b.GetRawText()); Assert.Equal(code, actualCode.GetString());
        Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location); Assert.DoesNotContain("private/", b.GetRawText());
        Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), out _));
    }

    private static async Task<HttpClient> Client(AuthenticationWebApplicationFactory factory, string email, bool web)
    {
        if (web) return await WebCookieTestClient.Login(factory, email);
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()); return client;
    }

    private async Task<Graph> Prepare()
    {
        var pm = await sql.CreateUserAsync("CASE-WIRE-PM-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await sql.CreateUserAsync("CASE-WIRE-SUP-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var reporter = await sql.CreateUserAsync("CASE-WIRE-REPORTER-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        var project = Guid.NewGuid(); await using var db = sql.CreateDbContext();
        db.Projects.Add(Project.Create(project, "CW-" + Guid.NewGuid().ToString("N"), "Case contract", null, 32648, new(2026, 1, 1), new(2027, 1, 1), DateTimeOffset.UtcNow));
        db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, pm.Id, new(2026, 1, 1))); await db.SaveChangesAsync();
        var a = await Intake(reporter.Id); var b = await Intake(reporter.Id);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString); using var sup = await Client(factory, supervisor.Email!, false);
        foreach (var id in new[] { a.Case, b.Case })
        {
            using var read = await sup.GetAsync($"/api/v1/cases/{id}"); Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/cases/{id}/triage") { Content = JsonContent.Create(new { projectId = project, verificationMethod = "EXISTING_EVIDENCE", reason = "Source-backed setup" }) };
            request.Headers.Add("If-Match", read.Headers.ETag!.Tag); request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            using var triage = await sup.SendAsync(request); Assert.Equal(HttpStatusCode.OK, triage.StatusCode);
        }
        return new(pm.Id, pm.Email!, project, a.Report, b.Report, a.Case, b.Case, a.Evidence);
    }

    private async Task<(Guid Report, Guid Case, Guid Evidence)> Intake(Guid reporter)
    {
        await using var db = sql.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        var file = StoredFile.Create(Guid.NewGuid(), "private/" + Guid.NewGuid().ToString("N"), "photo.jpg", "image/jpeg", 4, new string('a', 64), reporter, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, reporter, file.StorageUri, "REPORT_PHOTO", "image/jpeg", 4, new string('a', 64), 8388608, now.AddHours(24));
        db.Files.Add(file); db.FileScopes.Add(FileScope.CreatePrivate(Guid.NewGuid(), file.Id, reporter, now)); db.UploadSessions.Add(upload); await db.SaveChangesAsync();
        upload.StartUploading("controlled-upload-provenance", now); await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        var producer = new AnhHuyProducerService(new AnhHuyFactsRepository(db), null!, null!);
        var result = await new ReporterReportService(producer, new ReporterReportRepository(db), new IdempotencyOperationService(db)).CreateAsync(reporter, UserRoleCode.Reporter,
            new("Case contract source", [new(file.Id, Convert.ToBase64String(upload.RowVersion), "UNKNOWN")]), Guid.NewGuid().ToString(), null);
        Assert.Equal(ReporterReportCommandStatus.Created, result.Status);
        return (result.Report!.Id, (await db.Set<HuyCaseReportLink>().SingleAsync(l => l.ReportId == result.Report.Id && l.EndedAt == null)).CaseId, result.Report.EvidenceIds.Single());
    }

    private async Task<string> Snapshot(Graph g)
    {
        await using var db = sql.CreateDbContext(); var ids = new[] { g.CaseA, g.CaseB };
        var cases = await db.IncidentCases.Where(c => c.ProjectId == g.Project).OrderBy(c => c.Id).Select(c => new { c.Id, c.Status, Version = EF.Property<byte[]>(c, "RowVersion") }).ToArrayAsync();
        var receipts = await db.IdempotencyRecords.Where(r => r.ActorUserId == g.Pm && r.Operation.StartsWith("huy01.case.")).OrderBy(r => r.Id).Select(r => r.Id).ToArrayAsync();
        var audits = await db.AuditLogs.Where(a => ids.Contains(a.EntityId)).OrderBy(a => a.Id).Select(a => a.Id).ToArrayAsync();
        return JsonSerializer.Serialize(new { cases, receipts, audits });
    }
    private sealed record Graph(Guid Pm, string PmEmail, Guid Project, Guid ReportA, Guid ReportB, Guid CaseA, Guid CaseB, Guid EvidenceA);
    private sealed class MutableClock(DateTimeOffset now) : TimeProvider
    { public DateTimeOffset Now { get; set; } = now; public override DateTimeOffset GetUtcNow() => Now; }
}
