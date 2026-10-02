using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Exports;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.Repositories.Retention;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Exports;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reports;

public sealed partial class Huy01ReporterReportsApiTests
{
    [Fact]
    public async Task Anh02_reporting_snapshot_survives_actual_intake_reference_change_and_storage_ack_loss()
    {
        var reporter = await sql.CreateUserAsync("snapshot-reporter-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        var manager = await sql.CreateUserAsync("snapshot-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var project = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(Project.Create(project, project.ToString(), "Synthetic snapshot race", null, null, null, null, DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, manager.Id, new DateOnly(2000, 1, 1)));
            await db.SaveChangesAsync();
        }
        var artifacts = new ConsumerArtifactFixture { LoseFirstAcknowledgement = true };
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
            services.RemoveAll<IAnh02ArtifactStore>(); services.AddSingleton<IAnh02ArtifactStore>(artifacts);
            services.Configure<ExportOptions>(o => o.UnicodeFontPath = Environment.GetEnvironmentVariable("ANH02_TEST_FONT_PATH") ?? throw new InvalidOperationException("Set ANH02_TEST_FONT_PATH"));
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!); var file = await UploadVerifiedAsync(client, factory);
        var reportResponse = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", new { description = "Synthetic immutable snapshot source", evidence = new[] { new { fileId = file.FileId, fileVersion = file.Version, locationSource = "UNKNOWN" } } }, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, reportResponse.StatusCode);
        var reportId = (await reportResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Guid linkId;
        await using (var db = sql.CreateDbContext())
        {
            var link = await db.Set<HuyCaseReportLink>().SingleAsync(l => l.ReportId == reportId); linkId = link.Id;
            // Explicit fixture-only project attribution, not acceptance of an unavailable Huy triage command.
            var incident = await db.IncidentCases.SingleAsync(c => c.Id == link.CaseId);
            incident.Triage(project, CaseVerificationMethod.ExistingEvidence, "Synthetic fixture attribution", DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }
        await LoginAsync(client, manager.UserName!);
        var path = $"/api/v1/projects/{project}/exports"; var key = Guid.NewGuid().ToString();
        var response = await SendAsync(client, HttpMethod.Post, path, new { kind = "DOSSIER", format = "PDF" }, key);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        string frozen;
        await using (var db = sql.CreateDbContext())
        {
            var job = await db.Set<ExportJob>().SingleAsync(j => j.Id == id);
            frozen = (await db.Set<ExportSnapshot>().SingleAsync(s => s.Id == job.SnapshotId)).PayloadJson;
            var payload = JsonSerializer.Deserialize<RoadGuardSystem.DTOs.Exports.ExportSnapshotPayloadDto>(frozen, ExportSerialization.Options)!;
            Assert.Equal(1m, Assert.Single(payload.Dossier!.Summary.Metrics.Where(m => m.Code == "reportsReceived")).Value);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE CaseReportLinks SET EndedAt={DateTimeOffset.UtcNow} WHERE Id={linkId}");
        }
        var current = await client.GetFromJsonAsync<JsonElement>($"/api/v1/projects/{project}/reports/summary");
        Assert.Equal(0, Assert.Single(current.GetProperty("metrics").EnumerateArray().Where(m => m.GetProperty("code").GetString() == "reportsReceived")).GetProperty("value").GetInt32());
        var replay = await SendAsync(client, HttpMethod.Post, path, new { kind = "DOSSIER", format = "PDF" }, key);
        Assert.Equal(HttpStatusCode.Accepted, replay.StatusCode);
        for (var i = 0; i < 20; i++)
        {
            using var scope = factory.Services.CreateScope(); var service = scope.ServiceProvider.GetRequiredService<IExportService>();
            if ((await service.GetAsync(manager.Id, project, id, default)).Value!.Status == "SUCCEEDED") break;
            Assert.True(await service.ProcessNextAsync(default));
            await using var retry = sql.CreateDbContext();
            await retry.Database.ExecuteSqlInterpolatedAsync($"UPDATE Anh02ExportJobs SET NextAttemptAt=NULL WHERE Id={id}");
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path + $"/{id}/content")).StatusCode);
        await using (var db = sql.CreateDbContext())
        {
            var job = await db.Set<ExportJob>().SingleAsync(j => j.Id == id);
            Assert.Equal(frozen, (await db.Set<ExportSnapshot>().SingleAsync(s => s.Id == job.SnapshotId)).PayloadJson);
            Assert.Equal(1, await db.Set<GeneratedArtifact>().CountAsync(a => a.ExportJobId == id));
            Assert.Single(await db.Set<ExportJob>().Where(j => j.ProjectId == project).ToArrayAsync());
        }
        Assert.Equal(1, artifacts.Writes);
    }
    [Fact]
    public async Task Anh02_real_intake_is_unassigned_private_and_retained_without_fabricated_project_or_labels()
    {
        var reporter = await sql.CreateUserAsync($"anh02-reporter-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var manager = await sql.CreateUserAsync($"anh02-reader-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await sql.CreateUserAsync($"anh02-control-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var project = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(Project.Create(project, project.ToString(), "Intake integration", null, null, null, null, DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, manager.Id, new DateOnly(2000, 1, 1)));
            await db.SaveChangesAsync();
        }
        var artifacts = new ConsumerArtifactFixture();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
            services.RemoveAll<IAnh02ArtifactStore>(); services.AddSingleton<IAnh02ArtifactStore>(artifacts);
            services.Configure<ExportOptions>(options => options.UnicodeFontPath = Environment.GetEnvironmentVariable("ANH02_TEST_FONT_PATH") ?? throw new InvalidOperationException("Set ANH02_TEST_FONT_PATH to an embedding-licensed Unicode TTF font."));
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);
        var file = await UploadVerifiedAsync(client, factory);
        var intake = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", new { description = "private phone 0123456789 must never leak", evidence = new[] { new { fileId = file.FileId, fileVersion = file.Version, locationSource = "UNKNOWN" } } }, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, intake.StatusCode);
        var report = (await intake.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var inventory = await scope.ServiceProvider.GetRequiredService<IRetentionInventoryRepository>().ReadAsync(file.FileId, default);
            Assert.NotNull(inventory); Assert.False(inventory.Complete);
            Assert.Contains(inventory.References, r => r.Kind == "REPORT" && r.Id == report && r.ProjectId == null);
            Assert.Contains(inventory.References, r => r.Kind == "CASE_REPORT_LINK");
            Assert.Contains("HUY_REFERENCE_INVENTORY_UNAVAILABLE", inventory.ReasonCodes);
            Assert.Empty(inventory.PublicProjectIds);
        }
        await LoginAsync(client, manager.UserName!);
        var summary = await client.GetAsync($"/api/v1/projects/{project}/reports/summary");
        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        var metrics = (await summary.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("metrics").EnumerateArray().ToArray();
        foreach (var code in new[] { "reportsReceived", "casesByStatus" })
        {
            var metric = Assert.Single(metrics.Where(m => m.GetProperty("code").GetString() == code));
            Assert.Equal(0, metric.GetProperty("value").GetInt32()); Assert.Equal("PARTIAL", metric.GetProperty("availability").GetString());
        }
        Assert.DoesNotContain("0123456789", await summary.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/projects/{project}/retention/files/{file.FileId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/reporter-evidence/files/{file.FileId}/content")).StatusCode);
        var exports = $"/api/v1/projects/{project}/exports";
        var admitted = await SendAsync(client, HttpMethod.Post, exports, new { kind = "DOSSIER", format = "PDF" }, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Accepted, admitted.StatusCode);
        var exportId = (await admitted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var manifest = await client.GetAsync(exports + $"/{exportId}/manifest");
        Assert.Equal(HttpStatusCode.OK, manifest.StatusCode);
        var body = await manifest.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty(body.GetProperty("files").EnumerateArray());
        Assert.Contains(body.GetProperty("sections").EnumerateArray(), section => section.GetProperty("name").GetString() == "reporterEvidence" && section.GetProperty("availability").GetString() == "UNAVAILABLE");
        Assert.DoesNotContain("0123456789", await manifest.Content.ReadAsStringAsync());
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var scope = factory.Services.CreateScope(); var service = scope.ServiceProvider.GetRequiredService<IExportService>();
            if ((await service.GetAsync(manager.Id, project, exportId, default)).Value!.Status is "SUCCEEDED" or "FAILED") break;
            Assert.True(await service.ProcessNextAsync(default));
        }
        var pdf = await client.GetAsync(exports + $"/{exportId}/content"); Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
        using (var document = PdfSharp.Pdf.IO.PdfReader.Open(new MemoryStream(await pdf.Content.ReadAsByteArrayAsync()), PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))
        {
            Assert.True(document.PageCount > 0); Assert.Equal("Hồ sơ RoadGuard", document.Info.Title);
        }
        var missingLabels = await SendAsync(client, HttpMethod.Post, exports, new { kind = "TRAINING", format = "ZIP" }, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, missingLabels.StatusCode);
        Assert.Equal("producer_unavailable", (await missingLabels.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await LoginAsync(client, supervisor.UserName!);
        var held = await SendAsync(client, HttpMethod.Post, "/api/v1/retention/holds", new { scopeType = "FILE", scopeId = file.FileId, reason = "Preserve private intake pending triage" }, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, held.StatusCode);
        var fileHold = (await held.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        // Explicit SQL fixture triage: Huy lifecycle command is NOT integrated at the fixed source SHA.
        await using (var db = sql.CreateDbContext())
        {
            var link = await db.Set<HuyCaseReportLink>().SingleAsync(l => l.ReportId == report && l.EndedAt == null);
            var incident = await db.IncidentCases.SingleAsync(c => c.Id == link.CaseId);
            incident.Triage(project, CaseVerificationMethod.ExistingEvidence, "TEST_FIXTURE_TRIAGE_NO_HUY_COMMAND", DateTimeOffset.UtcNow);
            db.Entry(incident).Property<long>("Revision").CurrentValue++;
            await db.SaveChangesAsync();
        }
        var projectHold = await SendAsync(client, HttpMethod.Post, "/api/v1/retention/holds", new { scopeType = "PROJECT", scopeId = project, reason = "Independent project hold" }, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, projectHold.StatusCode);
        var retentionPath = $"/api/v1/projects/{project}/retention/files/{file.FileId}";
        var control = await client.GetAsync(retentionPath); Assert.Equal(HttpStatusCode.OK, control.StatusCode);
        var controls = await control.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, controls.GetProperty("activeHoldCount").GetInt32());
        Assert.False(controls.GetProperty("inventoryComplete").GetBoolean());
        Assert.Equal("BLOCKED_HOLD", controls.GetProperty("evaluation").GetProperty("eligibility").GetString());
        var evaluation = await SendAsync(client, HttpMethod.Post, $"/api/v1/projects/{project}/retention/evaluations", new { fileIds = (Guid[]?)null }, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Accepted, evaluation.StatusCode);
        var evaluationId = (await evaluation.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using (var scope = factory.Services.CreateScope()) Assert.True(await scope.ServiceProvider.GetRequiredService<IRetentionRepository>().ProcessOneAsync(default));
        var evaluated = await client.GetAsync($"/api/v1/projects/{project}/retention/evaluations/{evaluationId}");
        var item = Assert.Single((await evaluated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray().Where(i => i.GetProperty("fileId").GetGuid() == file.FileId));
        Assert.Equal(file.FileId, item.GetProperty("fileId").GetGuid()); Assert.Equal("BLOCKED_HOLD", item.GetProperty("eligibility").GetString());
        var fileHoldView = await client.GetAsync($"/api/v1/retention/holds/{fileHold}");
        var released = await SendAsync(client, HttpMethod.Post, $"/api/v1/retention/holds/{fileHold}/release", new { reason = "Only file hold released" }, Guid.NewGuid().ToString(), fileHoldView.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.OK, released.StatusCode);
        var stillHeld = await client.GetAsync(retentionPath);
        var stillControls = await stillHeld.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, stillControls.GetProperty("activeHoldCount").GetInt32());
        Assert.Equal("BLOCKED_HOLD", stillControls.GetProperty("evaluation").GetProperty("eligibility").GetString());
        await LoginAsync(client, manager.UserName!);
        var assigned = await client.GetAsync($"/api/v1/projects/{project}/reports/summary");
        var assignedMetrics = (await assigned.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("metrics").EnumerateArray();
        Assert.Equal(1, assignedMetrics.Single(m => m.GetProperty("code").GetString() == "reportsReceived").GetProperty("value").GetInt32());
        var drilldown = await client.GetAsync($"/api/v1/projects/{project}/reports/items?metric=reportsReceived");
        Assert.Equal(report, Assert.Single((await drilldown.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var from = DateTimeOffset.UtcNow.AddDays(1).ToString("O"); var to = DateTimeOffset.UtcNow.AddDays(2).ToString("O");
        var period = await client.GetAsync($"/api/v1/projects/{project}/reports/summary?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");
        Assert.Equal(HttpStatusCode.OK, period.StatusCode);
        var periodMetrics = (await period.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("metrics").EnumerateArray();
        Assert.Equal(0, periodMetrics.Single(m => m.GetProperty("code").GetString() == "reportsReceived").GetProperty("value").GetInt32());
        Assert.Equal(1, periodMetrics.Single(m => m.GetProperty("code").GetString() == "casesByStatus").GetProperty("value").GetInt32());
        using (var scope = factory.Services.CreateScope())
        {
            var candidate = await scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Integration.IAnhHuyProducerService>().ResolveCandidateSourceAsync(manager.Id, UserRoleCode.ProjectManager, project, RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind.Report, report);
            Assert.Equal(RoadGuardSystem.Services.Integration.AnhHuyProducerStatus.SourceNotReady, candidate.Status);
            Assert.Null(candidate.Facts); // No case geometry manufactured after fixture-only triage.
        }
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(retentionPath)).StatusCode);
        var hidden = await client.GetAsync($"/api/v1/projects/{project}/retention/evaluations/{evaluationId}");
        var hiddenPage = await hidden.Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(hiddenPage.GetProperty("items").EnumerateArray(), i => i.GetProperty("fileId").GetGuid() == file.FileId);
        Assert.NotEqual(file.FileId.ToString(), hiddenPage.GetProperty("nextCursor").GetString());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Repositories.RoadGuardDbContext>();
            var snapshot = await db.Set<ExportSnapshot>().SingleAsync(snapshot => db.Set<ExportJob>().Any(job => job.Id == exportId && job.SnapshotId == snapshot.Id));
            Assert.Equal(0, ExportSerialization.Read(snapshot).Dossier!.Summary.Metrics.Single(m => m.Code == "reportsReceived").Value);
        }
        await using var verify = sql.CreateDbContext();
        Assert.Equal(1, await verify.Reports.CountAsync(r => r.Id == report));
        Assert.True(await verify.FileScopes.AnyAsync(s => s.FileId == file.FileId && s.ProjectId == null));
    }
    [Fact]
    public async Task Anh02_intake_inventory_retains_supplement_and_closed_link_obligations()
    {
        var reporter = await sql.CreateUserAsync($"intake-inventory-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var project = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(Project.Create(project, project.ToString(), "Historical intake refs", null, null, null, null, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);
        var original = await UploadVerifiedAsync(client, factory); var supplement = await UploadVerifiedAsync(client, factory);
        var received = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", new { description = "Intake inventory source", evidence = new[] { new { fileId = original.FileId, fileVersion = original.Version, locationSource = "UNKNOWN" } } }, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, received.StatusCode);
        var reportId = (await received.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var supplementId = Guid.NewGuid(); Guid linkId; Guid caseId;
        // Fixture only: supplement/triage/close business commands remain Huy-owned/PENDING.
        await using (var db = sql.CreateDbContext())
        {
            var report = await db.Reports.SingleAsync(r => r.Id == reportId);
            report.AddSupplement(supplementId, "Fixture supplement", [RoadGuardSystem.BusinessObjects.Reports.VerifiedEvidenceReference.Create(Guid.NewGuid(), supplement.FileId, supplement.Version, reporter.Id)], DateTimeOffset.UtcNow);
            db.Entry(report).Property<long>("Revision").CurrentValue++;
            var link = await db.Set<HuyCaseReportLink>().SingleAsync(l => l.ReportId == reportId && l.EndedAt == null); linkId = link.Id; caseId = link.CaseId;
            var incident = await db.IncidentCases.SingleAsync(c => c.Id == caseId);
            incident.Triage(project, CaseVerificationMethod.ExistingEvidence, "FIXTURE_ONLY", DateTimeOffset.UtcNow);
            db.Entry(incident).Property<long>("Revision").CurrentValue++;
            await db.SaveChangesAsync();
        }
        async Task<RetentionInventory> Inventory()
        {
            using var scope = factory.Services.CreateScope();
            return (await scope.ServiceProvider.GetRequiredService<IRetentionInventoryRepository>().ReadAsync(supplement.FileId, default))!;
        }
        var before = await Inventory(); Assert.False(before.Complete);
        Assert.Contains(before.References, r => r.Kind == "REPORT_SUPPLEMENT" && r.Id == supplementId);
        Assert.Contains(before.References, r => r.Kind == "REPORT_SUPPLEMENT_EVIDENCE" && r.SourceVersion == supplement.Version);
        Assert.Contains(before.References, r => r.Kind == "CASE_REPORT_LINK" && r.ProjectId == project);
        using (var scope = factory.Services.CreateScope())
        {
            var files = await scope.ServiceProvider.GetRequiredService<IRetentionInventoryRepository>().KnownProjectFilesAsync(project, default);
            Assert.Contains(original.FileId, files); Assert.Contains(supplement.FileId, files);
        }
        await using (var db = sql.CreateDbContext())
        {
            var at = DateTimeOffset.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE CaseReportLinks SET EndedAt={at} WHERE Id={linkId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE IncidentCases SET Revision=Revision+1 WHERE Id={caseId}");
        }
        var after = await Inventory(); Assert.NotEqual(before.Version, after.Version);
        Assert.Contains(after.References, r => r.Kind == "CASE_REPORT_LINK" && r.Id == linkId && r.ProjectId == project);
        Assert.Empty(after.PublicProjectIds); Assert.False(after.Complete);
    }
}
