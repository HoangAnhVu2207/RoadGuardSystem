using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Repositories.Models.Huy01;
using Xunit;
namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class AnhHuyGeometrySourceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task Production_geometry_and_report_facts_are_scoped_versioned_and_fail_closed()
    {
        var supervisor = await sql.CreateUserAsync($"geometry_supervisor_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await sql.CreateUserAsync($"geometry_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, supervisor.UserName!);
        var projectResponse = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = $"P-{Guid.NewGuid():N}",
            name = "Geometry workflow test",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            primaryProjectManagerUserId = manager.Id,
            handover = new { documentNo = $"HD-{Guid.NewGuid():N}", handoverDate = "2026-08-31" },
            operationId = Guid.NewGuid()
        });
        projectResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = (await projectResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();
        var otherProjectResponse = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = $"P-OTHER-{Guid.NewGuid():N}",
            name = "Wrong project fixture",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            primaryProjectManagerUserId = manager.Id,
            handover = new { documentNo = $"HD-{Guid.NewGuid():N}", handoverDate = "2026-08-31" },
            operationId = Guid.NewGuid()
        });
        otherProjectResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var otherProject = (await otherProjectResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();
        var prefix = $"/api/v1/projects/{project}";
        await Login(client, manager.UserName!);
        var input = new
        {
            sourceKind = "COORDINATES",
            sourceCrs = 32648,
            stationOriginMeters = 1000d,
            changeReason = "surveyed alignment",
            coordinates = new[] { new { x = 500000d, y = 1200000d }, new { x = 500250d, y = 1200000d } },
            widthProfile = new[] { new { fromOffsetMeters = 0d, toOffsetMeters = 250d, widthMeters = 7d } },
            surveyWidthMeters = 9d,
            roadCode = "ANH-GEOM"
        };

        var created = await Send(client, HttpMethod.Post, prefix + "/road-geometry-drafts", input, Guid.NewGuid().ToString());
        created.EnsureSuccessStatusCode();
        var draft = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Login(client, supervisor.UserName!);
        var confirmed = await Send(client, HttpMethod.Post, prefix + $"/road-geometry-drafts/{draft}/confirm",
            new { expectedCurrentVersionId = (Guid?)null, effectiveFrom = "2026-10-02T00:00:00Z", reason = "Fixture alignment" }, Guid.NewGuid().ToString(), created.Headers.ETag!.ToString());
        confirmed.EnsureSuccessStatusCode();
        var confirmedJson = await confirmed.Content.ReadFromJsonAsync<JsonElement>();
        var route = confirmedJson.GetProperty("routeVersionId").GetGuid();
        var roadSection = confirmedJson.GetProperty("roadSectionId").GetGuid();
        await Login(client, manager.UserName!);
        var setPath = prefix + $"/road-sections/{roadSection}/versions/{route}/segment-sets";
        var segmentSet = await Send(client, HttpMethod.Post, setPath, new { targetLengthMeters = 100d, remainderMode = "KEEP" }, Guid.NewGuid().ToString());
        segmentSet.EnsureSuccessStatusCode();
        var setId = (await segmentSet.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await Send(client, HttpMethod.Post, setPath + $"/{setId}/publish", new { expectedPublishedSetId = (Guid?)null, reason = "Fixture set" }, Guid.NewGuid().ToString(), segmentSet.Headers.ETag!.ToString())).EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope();
        var producer = scope.ServiceProvider.GetRequiredService<IAnhHuyProducerService>();
        var context = await producer.ResolveGeometryAsync(manager.Id, UserRoleCode.ProjectManager, project, route, setId);
        context.Status.Should().Be(AnhHuyProducerStatus.Ready);
        context.Facts!.Segments.Should().HaveCount(3);
        context.Facts.SchemaVersion.Should().Be("anh-huy.geometry.v1");
        var consumerDb = scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Repositories.RoadGuardDbContext>();
        await consumerDb.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var consumerTransaction = await consumerDb.Database.BeginTransactionAsync();
            // Actual producer and geometry repository share the candidate command's scoped context.
            var inCommand = await producer.ResolveGeometryAsync(manager.Id, UserRoleCode.ProjectManager, project, route, setId);
            inCommand.Status.Should().Be(AnhHuyProducerStatus.Ready);
            scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Repositories.RoadGuardDbContext>().Database.CurrentTransaction.Should().BeSameAs(consumerTransaction);
            await consumerTransaction.RollbackAsync();
        });
        context.Facts.Segments[0].NextId.Should().Be(context.Facts.Segments[1].Id);
        (await producer.ResolveGeometryAsync(manager.Id, UserRoleCode.ProjectManager, project, route, setId, "stale")).Status.Should().Be(AnhHuyProducerStatus.StaleGeometry);
        (await producer.ResolveGeometryAsync(supervisor.Id, UserRoleCode.Supervisor, Guid.NewGuid(), route, setId)).Status.Should().Be(AnhHuyProducerStatus.NotFound);
        (await producer.ResolveGeometryAsync(manager.Id, UserRoleCode.Reporter, project, route, setId)).Status.Should().Be(AnhHuyProducerStatus.Forbidden);
        (await producer.ResolveCandidateSourceAsync(manager.Id, UserRoleCode.ProjectManager, project, CandidateSourceKind.AiDetection, Guid.NewGuid())).Status.Should().Be(AnhHuyProducerStatus.SourceNotReady);
        (await producer.ResolveCandidateSourceAsync(manager.Id, UserRoleCode.ProjectManager, project, CandidateSourceKind.FieldObservation, Guid.NewGuid())).Status.Should().Be(AnhHuyProducerStatus.SourceNotReady);
        var reporter = await sql.CreateUserAsync($"producer_reporter_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        // Explicit SQL fixture creation, not a Huy command: producer reads real rows.
        await using var db = sql.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var file = StoredFile.Create(Guid.NewGuid(), "s3://fixture/private", "private.jpg", "image/jpeg", 4, new string('a', 64), reporter.Id, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, reporter.Id, "fixture/private", "REPORT_PHOTO", "image/jpeg", 4, new string('a', 64), 8388608, now.AddHours(24));
        upload.StartUploading("fixture", now);
        db.Files.Add(file); db.FileScopes.Add(FileScope.CreatePrivate(Guid.NewGuid(), file.Id, reporter.Id, now)); db.UploadSessions.Add(upload); await db.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        var evidenceId = Guid.NewGuid();
        var report = Report.Create(Guid.NewGuid(), reporter.Id, "Producer fixture report", now, [VerifiedEvidenceReference.Create(evidenceId, file.Id, Convert.ToBase64String(upload.RowVersion), reporter.Id)]);
        var incident = IncidentCase.CreateUnassigned(Guid.NewGuid(), report.Id, now);
        db.Reports.Add(report); db.IncidentCases.Add(incident);
        db.Set<HuyCaseReportLink>().Add(new() { Id = Guid.NewGuid(), CaseId = incident.Id, ReportId = report.Id, StartedAt = now }); await db.SaveChangesAsync();
        (await producer.ResolveCandidateSourceAsync(manager.Id, UserRoleCode.ProjectManager, project, CandidateSourceKind.Report, report.Id)).Status.Should().Be(AnhHuyProducerStatus.SourceNotReady);
        incident.Triage(otherProject, CaseVerificationMethod.ExistingEvidence, "Fixture wrong-project triage", now);
        db.Entry(incident).Property<long>("Revision").CurrentValue++; await db.SaveChangesAsync();
        // Wrong project is hidden even when geometry provenance is incomplete.
        (await producer.ResolveCandidateSourceAsync(manager.Id, UserRoleCode.ProjectManager, project, CandidateSourceKind.Report, report.Id)).Status.Should().Be(AnhHuyProducerStatus.NotFound);
        db.Entry(incident).Property(x => x.ProjectId).CurrentValue = project;
        db.Entry(incident).Property<Guid?>("GeometryRouteVersionId").CurrentValue = route;
        db.Entry(incident).Property<Guid?>("GeometrySegmentSetId").CurrentValue = setId;
        db.Entry(incident).Property<long>("Revision").CurrentValue++; await db.SaveChangesAsync();
        var source = await producer.ResolveCandidateSourceAsync(manager.Id, UserRoleCode.ProjectManager, project, CandidateSourceKind.Report, report.Id);
        source.Status.Should().Be(AnhHuyProducerStatus.Ready);
        source.Facts!.Evidence.Single().ChecksumSha256.Should().Be(new string('a', 64));
        source.Facts.Evidence.Single().Reference.CaptureMetadata.Should().BeNull();
        (await producer.ResolveCandidateSourceAsync(manager.Id, UserRoleCode.ProjectManager, project, CandidateSourceKind.Report, report.Id, "stale")).Status.Should().Be(AnhHuyProducerStatus.StaleSource);
        (await producer.ResolveCandidateSourceAsync(manager.Id, UserRoleCode.ProjectManager, project, CandidateSourceKind.Report, report.Id, expectedDispositionVersion: "stale")).Status.Should().Be(AnhHuyProducerStatus.StaleDisposition);
        var decision = CandidateDecision.Create(Guid.NewGuid(), source.Facts.DomainFacts, CandidateDecisionKind.Reject, null, Guid.Empty, null, null, manager.Id, "Fixture reject", now);
        db.SourceDecisions.Add(decision); db.Entry(decision).Property<CandidateSourceKind>("SourceKind").CurrentValue = CandidateSourceKind.Report;
        db.Entry(decision).Property<Guid>("SourceId").CurrentValue = report.Id; db.Entry(decision).Property<Guid?>("ReportSourceId").CurrentValue = report.Id;
        db.Set<HuyCandidateSourceHead>().Add(new() { SourceKind = CandidateSourceKind.Report, SourceId = report.Id, ProjectId = project, DecisionId = decision.Id }); await db.SaveChangesAsync();
        var after = await producer.ResolveCandidateSourceAsync(manager.Id, UserRoleCode.ProjectManager, project, CandidateSourceKind.Report, report.Id);
        after.Status.Should().Be(AnhHuyProducerStatus.Ready);
        after.Facts!.DomainFacts.ActiveDisposition!.DecisionId.Should().Be(decision.Id);
        (await producer.ResolveCandidateSourceAsync(manager.Id, UserRoleCode.ProjectManager, project, CandidateSourceKind.Report, report.Id, source.Facts.DomainFacts.Source.SourceVersion)).Status.Should().Be(AnhHuyProducerStatus.StaleSource);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RoadSegmentSets SET Status='SUPERSEDED' WHERE Id={setId}");
        (await producer.ResolveGeometryAsync(manager.Id, UserRoleCode.ProjectManager, project, route, setId)).Status.Should().Be(AnhHuyProducerStatus.StaleGeometry);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RoadSectionVersions SET IsCurrent=0 WHERE Id={route}");
        var historical = await producer.ResolveGeometryAsync(manager.Id, UserRoleCode.ProjectManager, project, route, setId, requireCurrent: false);
        historical.Status.Should().Be(AnhHuyProducerStatus.Ready);
        historical.Facts!.Package.Route.IsCurrent.Should().BeFalse();
        (await producer.ResolveCandidateSourceAsync(manager.Id, UserRoleCode.ProjectManager, project, CandidateSourceKind.Report, report.Id)).Status.Should().Be(AnhHuyProducerStatus.StaleGeometry);
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ProjectMembers WHERE ProjectId={project} AND UserId={manager.Id}");
        (await producer.ResolveCandidateSourceAsync(manager.Id, UserRoleCode.ProjectManager, project, CandidateSourceKind.Report, report.Id)).Status.Should().Be(AnhHuyProducerStatus.Forbidden);
    }
    private static async Task Login(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(name), password = "Current1!" }); response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, object input, string key, string? etag = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(input) }; request.Headers.Add("Idempotency-Key", key); if (etag is not null) request.Headers.TryAddWithoutValidation("If-Match", etag); return await client.SendAsync(request);
    }
}
