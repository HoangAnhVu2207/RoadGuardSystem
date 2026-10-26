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
public sealed class CaseContractWireAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("triage", false)]
    [InlineData("triage", true)]
    [InlineData("link", false)]
    [InlineData("link", true)]
    [InlineData("split", false)]
    [InlineData("split", true)]
    [InlineData("conclude", false)]
    [InlineData("conclude", true)]
    [InlineData("publish", false)]
    [InlineData("publish", true)]
    public async Task ExactCaseCommandWireReplayConflictAndStaleVersion(string action, bool web)
    {
        var g = await Prepare(); await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var pm = await Client(factory, g.PmEmail, web);
        var expectedCorrelation = Guid.NewGuid();
        pm.DefaultRequestHeaders.Add("X-Correlation-ID", expectedCorrelation.ToString());
        var target = await Read(pm, g.CaseA, g.Project);
        var source = await Read(pm, g.CaseB, g.Project);
        if (action == "split")
            target = await Apply(pm, g, "link", target, source, Guid.NewGuid().ToString(), "Prepare split");
        if (action == "publish")
            target = await Apply(pm, g, "conclude", target, source, Guid.NewGuid().ToString(), "Prepare publication");
        var before = await Snapshot(g); var key = Guid.NewGuid().ToString();
        var result = await Apply(pm, g, action, target, source, key, "Contract acceptance");
        var committed = await Snapshot(g); Assert.NotEqual(before, committed);
        var replay = await Apply(pm, g, action, target, source, key, "Contract acceptance");
        Assert.Equal(result.GetRawText(), replay.GetRawText()); Assert.Equal(committed, await Snapshot(g));
        using var changed = await Send(pm, g, action, target, source, key, "Changed command");
        await Problem(changed, 409, "idempotency_key_reused"); Assert.Equal(committed, await Snapshot(g));
        using var stale = await Send(pm, g, action, target, source, Guid.NewGuid().ToString(), "Stale version");
        await Problem(stale, 412, "concurrency_conflict"); Assert.Equal(committed, await Snapshot(g));
        await using var check = sql.CreateDbContext();
        Assert.Equal(1, await check.IdempotencyRecords.CountAsync(r => r.ActorUserId == g.Pm && r.IdempotencyKey == key && r.Operation == "huy01.case." + action + ".v1"));
        var audit = Assert.Single(await check.AuditLogs.AsNoTracking().Where(a => a.ActorUserId == g.Pm && a.EntityId == g.CaseA && a.EventType == "case_" + action && a.Reason == "Contract acceptance").ToArrayAsync());
        Assert.Equal("IncidentCase", audit.EntityType);
        Assert.Equal("huy01.case", audit.Source);
        Assert.Equal(expectedCorrelation, audit.CorrelationId);
        Assert.Null(audit.BeforeSnapshot);
        var auditBody = JsonDocument.Parse(audit.AfterSnapshot!).RootElement;
        Fields(auditBody, "caseId", "resultCaseId");
        Assert.Equal(g.CaseA, auditBody.GetProperty("caseId").GetGuid());
        Assert.Equal(action == "split" ? result.GetProperty("id").GetGuid() : g.CaseA, auditBody.GetProperty("resultCaseId").GetGuid());
        Assert.DoesNotContain(g.PmEmail, audit.AfterSnapshot!);
        Assert.Equal(action == "publish" ? 1 : 0, await check.Set<CasePublication>().CountAsync(p => p.CaseId == g.CaseA));
        await check.ProjectMembers.Where(m => m.ProjectId == g.Project && m.UserId == g.Pm).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        using var revokedReplay = await Send(pm, g, action, target, source, key, "Contract acceptance");
        await Problem(revokedReplay, 403, "access_forbidden"); Assert.Equal(committed, await Snapshot(g));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactCaseReadSchemaHeadersCurrentScopeAndNoBusinessWrites(bool web)
    {
        var g = await Prepare(); await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var pm = await Client(factory, g.PmEmail, web); var before = await Snapshot(g);
        var body = await Read(pm, g.CaseA, g.Project);
        Assert.Equal("OPEN", body.GetProperty("status").GetString());
        Assert.Equal("EXISTING_EVIDENCE", body.GetProperty("verificationMethod").GetString());
        Assert.Equal(g.ReportA, Assert.Single(body.GetProperty("reportIds").EnumerateArray()).GetGuid());
        Assert.Empty(body.GetProperty("defectIds").EnumerateArray()); Assert.False(body.TryGetProperty("conclusion", out _));
        Assert.Equal(before, await Snapshot(g));
        await using (var db = sql.CreateDbContext())
            await db.ProjectMembers.Where(m => m.ProjectId == g.Project && m.UserId == g.Pm).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        using var denied = await pm.GetAsync($"/api/v1/cases/{g.CaseA}");
        await Problem(denied, 403, "access_forbidden"); Assert.Equal(before, await Snapshot(g));
    }

    [Theory]
    [InlineData("read", false)]
    [InlineData("read", true)]
    [InlineData("triage", false)]
    [InlineData("triage", true)]
    [InlineData("link", false)]
    [InlineData("link", true)]
    [InlineData("split", false)]
    [InlineData("split", true)]
    [InlineData("conclude", false)]
    [InlineData("conclude", true)]
    [InlineData("publish", false)]
    [InlineData("publish", true)]
    public async Task EveryCaseRouteRejectsCredentialRoleProjectAndSessionViolations(string action, bool web)
    {
        var g = await Prepare(); var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString,
            configureTestServices: s => { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(clock); });
        using var pm = await Client(factory, g.PmEmail, web);
        var target = await Read(pm, g.CaseA, g.Project); var source = await Read(pm, g.CaseB, g.Project);
        if (web && action != "read")
        {
            var csrf = pm.DefaultRequestHeaders.GetValues("X-CSRF-TOKEN").Single();
            foreach (var invalid in new string?[] { null, "tampered" })
            {
                pm.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
                if (invalid is not null) pm.DefaultRequestHeaders.Add("X-CSRF-TOKEN", invalid);
                var before = await Snapshot(g);
                using var denied = await Send(pm, g, action, target, source, Guid.NewGuid().ToString(), "CSRF probe");
                await Problem(denied, 403, "csrf_failed");
                Assert.True(denied.Headers.CacheControl?.NoStore);
                Assert.Equal(before, await Snapshot(g));
            }
            pm.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            pm.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        }
        async Task Denied(HttpClient c, int status, string code)
        {
            var before = await Snapshot(g);
            using var response = action == "read" ? await c.GetAsync($"/api/v1/cases/{g.CaseA}") : await Send(c, g, action, target, source, Guid.NewGuid().ToString(), "Denied probe");
            await Problem(response, status, code); Assert.Equal(before, await Snapshot(g));
        }
        using var bare = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        await Denied(bare, 401, "auth_unauthorized");
        if (web) bare.DefaultRequestHeaders.Add("Cookie", "__Host-RoadGuardSession=tampered");
        else bare.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tampered");
        await Denied(bare, 401, "auth_unauthorized");
        var deniedRoles = new List<UserRoleCode> { UserRoleCode.Reporter, UserRoleCode.DroneOperator, UserRoleCode.RepairCrew };
        if (action is not ("read" or "triage")) deniedRoles.Add(UserRoleCode.Supervisor);
        foreach (var role in deniedRoles)
        {
            var wrong = await sql.CreateUserAsync("CASE-WRONG-" + Guid.NewGuid().ToString("N"), "Current1!", role);
            using var wrongClient = await Client(factory, wrong.Email!, web); await Denied(wrongClient, 403, "access_forbidden");
        }
        var outsider = await sql.CreateUserAsync("CASE-OUTSIDE-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        await using (var db = sql.CreateDbContext())
        {
            var other = Guid.NewGuid();
            db.Projects.Add(Project.Create(other, "CW-OTHER-" + Guid.NewGuid().ToString("N"), "Other project", null, 32648, new(2026, 1, 1), new(2027, 1, 1), DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), other, outsider.Id, new(2026, 1, 1))); await db.SaveChangesAsync();
        }
        using var outsiderClient = await Client(factory, outsider.Email!, web); await Denied(outsiderClient, 403, "access_forbidden");
        if (web)
        {
            clock.Now += TimeSpan.FromHours(13); await Denied(pm, 401, "auth_unauthorized"); clock.Now -= TimeSpan.FromHours(13);
        }
        else
        {
            var original = pm.DefaultRequestHeaders.Authorization;
            await using var db = sql.CreateDbContext(); var session = await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == g.Pm);
            pm.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new AccessTokenFactory(factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value).Create(g.Pm, session.Id, UserRoleCode.ProjectManager, DateTimeOffset.UtcNow.AddMinutes(-20), 15));
            await Denied(pm, 401, "auth_unauthorized"); pm.DefaultRequestHeaders.Authorization = original;
        }
        await using (var db = sql.CreateDbContext())
            await db.Sessions.Where(s => s.UserId == g.Pm).ExecuteUpdateAsync(s => s.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
        await Denied(pm, 401, web ? "auth_unauthorized" : "auth_session_revoked");
    }

    private async Task<JsonElement> Apply(HttpClient client, Graph g, string action, JsonElement target, JsonElement source, string key, string reason)
    {
        using var response = await Send(client, g, action, target, source, key, reason);
        Assert.Equal(action is "split" or "publish" ? HttpStatusCode.Created : HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (action == "publish")
        {
            Fields(body, "id", "caseId", "status", "version"); Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
            Assert.Equal(g.CaseA, body.GetProperty("caseId").GetGuid()); Assert.Equal("PUBLISHED", body.GetProperty("status").GetString());
            Assert.Equal($"/api/v1/cases/{g.CaseA}", response.Headers.Location?.OriginalString);
            // Publication wire has its own version; the strong ETag is the parent Case version.
            var parent = await Read(client, g.CaseA, g.Project); Assert.Equal('"' + parent.GetProperty("version").GetString() + '"', response.Headers.ETag?.Tag);
        }
        else
        {
            CaseFields(body); Assert.Equal(g.Project, body.GetProperty("projectId").GetGuid());
            Assert.Equal(action == "split" ? new[] { g.ReportB } : action == "link" ? new[] { g.ReportA, g.ReportB }.Order() : new[] { g.ReportA }, body.GetProperty("reportIds").EnumerateArray().Select(x => x.GetGuid()).Order());
            Assert.Equal('"' + body.GetProperty("version").GetString() + '"', response.Headers.ETag?.Tag);
            if (action == "split") Assert.Equal($"/api/v1/cases/{body.GetProperty("id").GetGuid()}", response.Headers.Location?.OriginalString);
            else { Assert.Equal(g.CaseA, body.GetProperty("id").GetGuid()); Assert.Null(response.Headers.Location); }
            if (action == "conclude")
            {
                Assert.Equal("CONCLUDED", body.GetProperty("status").GetString()); var conclusion = body.GetProperty("conclusion");
                Fields(conclusion, "id", "outcome", "reason", "concludedAt", "defectIds", "evidenceIds");
                Assert.Equal("NO_DEFECT", conclusion.GetProperty("outcome").GetString()); Assert.Equal(reason, conclusion.GetProperty("reason").GetString());
                Assert.Equal(g.EvidenceA, Assert.Single(conclusion.GetProperty("evidenceIds").EnumerateArray()).GetGuid());
            }
        }
        Assert.Matches("^\"[A-Za-z0-9+/]{11}=\"$", response.Headers.ETag?.Tag ?? "");
        Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), out _));
        Assert.DoesNotContain("private/", body.GetRawText()); Assert.DoesNotContain("storageUri", body.GetRawText());
        return body;
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
