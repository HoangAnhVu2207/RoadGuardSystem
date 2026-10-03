using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Data.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Repositories.Extensions;
using RoadGuardSystem.Services.Extensions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Cases;
using RoadGuardSystem.Repositories.Implementations.Defects;
using RoadGuardSystem.Repositories.Implementations.Reports;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Implementations.Defects;
using RoadGuardSystem.Services.Implementations.Reports;
using RoadGuardSystem.Services.Integration;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reports;

[Collection(AuthenticationApiFixture.Name)]
public sealed class Huy01ReporterReportsApiTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task ProductionRoot_BindsReporterCaseAndCandidateServicesOnce()
    {
        var reporter = await sql.CreateUserAsync($"binding-r-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        Microsoft.Extensions.DependencyInjection.ServiceDescriptor[] descriptors = [];
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            descriptors = services.ToArray();
            services.RemoveAll<IUploadObjectStorage>();
            services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
        });
        using (var scope = factory.Services.CreateScope())
        {
            Assert.IsType<ReporterReportService>(scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Reports.IReporterReportService>());
            Assert.IsType<ReporterReportRepository>(scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Repositories.Reports.IReporterReportRepository>());
        }
        Assert.Equal(ServiceLifetime.Scoped, Assert.Single(descriptors.Where(d => d.ServiceType == typeof(RoadGuardSystem.Services.Reports.IReporterReportService))).Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, Assert.Single(descriptors.Where(d => d.ServiceType == typeof(RoadGuardSystem.Repositories.Reports.IReporterReportRepository))).Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, Assert.Single(descriptors.Where(d => d.ServiceType == typeof(RoadGuardSystem.Services.Cases.ICaseWorkflowService))).Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, Assert.Single(descriptors.Where(d => d.ServiceType == typeof(RoadGuardSystem.Services.Reports.IReporterLifecycleService))).Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, Assert.Single(descriptors.Where(d => d.ServiceType == typeof(RoadGuardSystem.Services.Defects.ICandidateDecisionService))).Lifetime);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);
        var evidence = await UploadVerifiedAsync(client, factory);
        var key = Guid.NewGuid().ToString("N");
        var payload = new { description = "Production intake", evidence = new[] { new { fileId = evidence.FileId, fileVersion = evidence.Version, locationSource = "UNKNOWN" } } };
        var created = await SendAsync(client, HttpMethod.Post, "/api/v1/reports",
            payload, key);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadAsStringAsync();
        var reportId = JsonDocument.Parse(body).RootElement.GetProperty("id").GetGuid();
        Assert.Equal($"/api/v1/reports/{reportId}", created.Headers.Location?.OriginalString);
        Assert.NotNull(created.Headers.ETag);
        var counts = await CountIntakeRowsAsync(sql, reporter.Id, reportId, key);
        Assert.Equal(new ReporterIntakeRowCounts(1, 1, 1, 1, 1, 1, 1, 1, 1, 1), counts);
        var replay = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", payload, key);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal(body, await replay.Content.ReadAsStringAsync());
        Assert.Equal(created.Headers.Location, replay.Headers.Location);
        Assert.Equal(created.Headers.ETag, replay.Headers.ETag);
        Assert.Equal(counts, await CountIntakeRowsAsync(sql, reporter.Id, reportId, key));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/reports")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/reports/{reportId}")).StatusCode);
    }

    [Theory]
    [Trait("Package", "HUY-01")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProductionIntake_InactiveRoleAfterPreflight_DeniesReceiptBeforeReplayOrConflict(bool changedPayload)
    {
        var reporter = await sql.CreateUserAsync($"role-r-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var barrier = new ReporterReceiptRoleBarrier(async token =>
        {
            await using var db = sql.CreateDbContext();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Roles] SET [IsActive]={false} WHERE [Code]={UserRoleCode.Reporter.ToDbCode()}", token);
        });
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>();
            services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
            services.RemoveAll<RoadGuardDbContext>();
            services.AddScoped(sp => new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>(
                sp.GetRequiredService<DbContextOptions<RoadGuardDbContext>>()).AddInterceptors(barrier).Options));
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);
        var evidence = await UploadVerifiedAsync(client, factory);
        var key = Guid.NewGuid().ToString("N");
        object Payload(bool changed) => new
        {
            description = changed ? "Changed after role revoke" : "Role revoke report",
            evidence = new[] { new { fileId = evidence.FileId, fileVersion = evidence.Version, locationSource = "UNKNOWN" } }
        };
        var first = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", Payload(false), key);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var reportId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var before = await CountIntakeRowsAsync(sql, reporter.Id, reportId, key);
        HttpResponseMessage denied;
        try
        {
            barrier.Armed = true;
            denied = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", Payload(changedPayload), key);
        }
        finally
        {
            await using var db = sql.CreateDbContext();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Roles] SET [IsActive]={true} WHERE [Code]={UserRoleCode.Reporter.ToDbCode()}");
        }
        Assert.Equal(1, barrier.Calls);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("access_forbidden", (await denied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Null(denied.Headers.ETag);
        Assert.Null(denied.Headers.Location);
        Assert.Equal(before, await CountIntakeRowsAsync(sql, reporter.Id, reportId, key));
    }

    private sealed class ReporterReceiptRoleBarrier(Func<CancellationToken, Task> deactivate) : DbCommandInterceptor
    {
        public bool Armed { get; set; }
        public int Calls { get; private set; }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (Armed && command.CommandText.Contains("FROM [IdempotencyRecords]", StringComparison.Ordinal))
            {
                Armed = false;
                Calls++;
                await deactivate(cancellationToken);
            }
            return result;
        }
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task ProductionCaseReporterAndCandidateRoutes_AreBound()
    {
        var reporter = await sql.CreateUserAsync($"unbound-r-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var pm = await sql.CreateUserAsync($"unbound-p-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);
        var reports = await client.GetAsync("/api/v1/reports");
        reports.StatusCode.Should().Be(HttpStatusCode.OK);
        await LoginAsync(client, pm.UserName!);
        (await client.GetAsync("/api/v1/cases")).StatusCode.Should().NotBe(HttpStatusCode.ServiceUnavailable);
        (await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/candidate-decisions/{Guid.NewGuid()}")).StatusCode.Should().NotBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task Candidate_ReportRejectCorrectionKeepNewAndLinkExisting_UseRealGeometrySourceAndHead()
    {
        var reporter = await sql.CreateUserAsync($"candidate-r-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var supervisor = await sql.CreateUserAsync($"candidate-s-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var pm = await sql.CreateUserAsync($"candidate-p-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var project = Guid.NewGuid();
        var defectTypeCode = $"HUY-{Guid.NewGuid():N}";
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(RoadGuardSystem.BusinessObjects.Projects.Project.Create(project, $"CD-{Guid.NewGuid():N}", "Candidate project", null, 32648, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(RoadGuardSystem.BusinessObjects.Projects.ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, pm.Id, new DateOnly(2026, 1, 1)));
            db.DefectTypes.Add(RoadGuardSystem.BusinessObjects.Catalogs.DefectType.Create(defectTypeCode, "Candidate fixture type"));
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);
        var file = await UploadVerifiedAsync(client, factory);
        var created = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", new { description = "Candidate source", evidence = new[] { new { fileId = file.FileId, fileVersion = file.Version, locationSource = "UNKNOWN" } } }, Guid.NewGuid().ToString());
        var report = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Guid incident;
        await using (var db = sql.CreateDbContext()) incident = (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>().SingleAsync(l => l.ReportId == report && l.EndedAt == null)).CaseId;
        var geometry = await CreatePublishedGeometryAsync(client, pm.UserName!, supervisor.UserName!, project);
        await LoginAsync(client, supervisor.UserName!);
        var read = await client.GetAsync($"/api/v1/cases/{incident}");
        var triage = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{incident}/triage", new { projectId = project, verificationMethod = "EXISTING_EVIDENCE", reason = "Route provenance",
            routeVersionId = geometry.Route, segmentSetId = geometry.Set, geometryVersion = geometry.Version }, Guid.NewGuid().ToString(), read.Headers.ETag!.ToString());
        triage.StatusCode.Should().Be(HttpStatusCode.OK);
        using var scope = factory.Services.CreateScope();
        var producer = scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Integration.IAnhHuyProducerService>();
        var source = await producer.ResolveCandidateSourceAsync(pm.Id, UserRoleCode.ProjectManager, project, RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind.Report, report);
        source.Status.Should().Be(RoadGuardSystem.Services.Integration.AnhHuyProducerStatus.Ready);
        await LoginAsync(client, pm.UserName!);
        var payload = new { sourceKind = "REPORT", sourceId = report, sourceVersion = source.Facts!.DomainFacts.Source.SourceVersion,
            geometryVersion = source.Facts.DomainFacts.GeometryVersion, decision = "REJECT", reason = "No defect in source" };
        var key = Guid.NewGuid().ToString(); var path = $"/api/v1/projects/{project}/candidate-decisions";
        var decisions = await Task.WhenAll(SendAsync(client, HttpMethod.Post, path, payload, key),
            SendAsync(client, HttpMethod.Post, path, payload, key));
        var decided = decisions[0];
        decisions[1].StatusCode.Should().Be(HttpStatusCode.Created);
        (await decisions[1].Content.ReadAsStringAsync()).Should().Be(await decided.Content.ReadAsStringAsync());
        decisions[1].Headers.Location.Should().Be(decided.Headers.Location);
        decisions[1].Headers.ETag.Should().Be(decided.Headers.ETag);
        decided.StatusCode.Should().Be(HttpStatusCode.Created);
        var replay = await SendAsync(client, HttpMethod.Post, path, payload, key);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await replay.Content.ReadAsStringAsync()).Should().Be(await decided.Content.ReadAsStringAsync());
        replay.Headers.Location.Should().Be(decided.Headers.Location); replay.Headers.ETag.Should().Be(decided.Headers.ETag);
        var freshStale = await SendAsync(client, HttpMethod.Post, path, payload, Guid.NewGuid().ToString());
        freshStale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var after = await producer.ResolveCandidateSourceAsync(pm.Id, UserRoleCode.ProjectManager, project, RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind.Report, report);
        var corrected = await SendAsync(client, HttpMethod.Post, path, new { sourceKind = "REPORT", sourceId = report,
            sourceVersion = after.Facts!.DomainFacts.Source.SourceVersion, geometryVersion = after.Facts.DomainFacts.GeometryVersion,
            decision = "REJECT", reason = "Corrected reason", supersedesDecisionId = after.Facts.DomainFacts.ActiveDisposition!.DecisionId,
            previousDecisionVersion = after.Facts.DomainFacts.ActiveDisposition.Version }, Guid.NewGuid().ToString());
        corrected.StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.GetAsync(corrected.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
        await using (var db = sql.CreateDbContext())
        {
            (await db.SourceDecisions.CountAsync(d => EF.Property<Guid>(d, "ReportSourceId") == report)).Should().Be(2);
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCandidateSourceHead>().CountAsync(h => h.SourceId == report)).Should().Be(1);
            (await db.Defects.CountAsync(d => d.ProjectId == project)).Should().Be(0);
        }
        var thirdSource = await producer.ResolveCandidateSourceAsync(pm.Id, UserRoleCode.ProjectManager, project,
            RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind.Report, report);
        var third = new CandidateDecisionRequestDto("REPORT", report, thirdSource.Facts!.DomainFacts.Source.SourceVersion,
            thirdSource.Facts.DomainFacts.GeometryVersion, "REJECT", null, null, null, "Fault-tested correction",
            thirdSource.Facts.DomainFacts.ActiveDisposition!.DecisionId, thirdSource.Facts.DomainFacts.ActiveDisposition.Version);
        var failedKey = Guid.NewGuid().ToString();
        var precommit = new CommandCommitFailure(failedKey, false);
        await using (var db = ModuleFaultContext(precommit))
            await Assert.ThrowsAsync<RetryLimitExceededException>(() => CandidateService(db).DecideAsync(pm.Id, UserRoleCode.ProjectManager,
                project, third, failedKey, null, default));
        precommit.Failures.Should().Be(3);
        await using (var db = sql.CreateDbContext())
        {
            (await db.SourceDecisions.CountAsync(d => EF.Property<Guid>(d, "ReportSourceId") == report)).Should().Be(2);
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCandidateSourceHead>().SingleAsync(h => h.SourceId == report)).DecisionId
                .Should().Be(third.SupersedesDecisionId!.Value);
            (await db.AuditLogs.CountAsync(a => a.ActorUserId == pm.Id && a.EventType == "candidate_decided" &&
                db.SourceDecisions.Where(d => EF.Property<Guid>(d, "ReportSourceId") == report).Select(d => d.Id).Contains(a.EntityId))).Should().Be(2);
            (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == pm.Id && r.IdempotencyKey == failedKey)).Should().Be(0);
        }
        var recoveredKey = Guid.NewGuid().ToString();
        var postcommit = new CommandCommitFailure(recoveredKey, true);
        RoadGuardSystem.Services.Defects.CandidateDecisionResult recovered;
        await using (var db = ModuleFaultContext(postcommit))
            recovered = await CandidateService(db).DecideAsync(pm.Id, UserRoleCode.ProjectManager, project, third, recoveredKey, null, default);
        recovered.Status.Should().Be(201); postcommit.Failures.Should().Be(1);
        await using (var db = sql.CreateDbContext())
        {
            var replayed = await CandidateService(db).DecideAsync(pm.Id, UserRoleCode.ProjectManager, project, third, recoveredKey, null, default);
            JsonSerializer.Serialize(replayed.Decision).Should().Be(JsonSerializer.Serialize(recovered.Decision));
            (await db.SourceDecisions.CountAsync(d => EF.Property<Guid>(d, "ReportSourceId") == report)).Should().Be(3);
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCandidateSourceHead>().SingleAsync(h => h.SourceId == report)).DecisionId
                .Should().Be(recovered.Decision!.Id);
            (await db.AuditLogs.CountAsync(a => a.ActorUserId == pm.Id && a.EventType == "candidate_decided" && a.EntityId == recovered.Decision.Id)).Should().Be(1);
            (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == pm.Id && r.IdempotencyKey == recoveredKey)).Should().Be(1);
        }
        var keepSource = await producer.ResolveCandidateSourceAsync(pm.Id, UserRoleCode.ProjectManager, project,
            RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind.Report, report);
        var failedKeepKey = Guid.NewGuid().ToString();
        var failedKeep = new CandidateDecisionRequestDto("REPORT", report,
            keepSource.Facts!.DomainFacts.Source.SourceVersion, keepSource.Facts.DomainFacts.GeometryVersion,
            "KEEP_NEW", null, null, new(defectTypeCode, null, "LOW", geometry.Route, null),
            "Precommit rollback of accepted decision", keepSource.Facts.DomainFacts.ActiveDisposition!.DecisionId,
            keepSource.Facts.DomainFacts.ActiveDisposition.Version);
        var failedKeepCommit = new CommandCommitFailure(failedKeepKey, false);
        await using (var db = ModuleFaultContext(failedKeepCommit))
            await Assert.ThrowsAsync<RetryLimitExceededException>(() => CandidateService(db).DecideAsync(pm.Id,
                UserRoleCode.ProjectManager, project, failedKeep, failedKeepKey, null, default));
        failedKeepCommit.Failures.Should().Be(3);
        await using (var db = sql.CreateDbContext())
        {
            (await db.SourceDecisions.CountAsync(d => EF.Property<Guid>(d, "ReportSourceId") == report)).Should().Be(3);
            (await db.Defects.CountAsync(d => d.ProjectId == project)).Should().Be(0);
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink>()
                .CountAsync(link => link.SourceId == report)).Should().Be(0);
            (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == pm.Id && r.IdempotencyKey == failedKeepKey)).Should().Be(0);
        }
        var keepPayload = new
        {
            sourceKind = "REPORT", sourceId = report,
            sourceVersion = keepSource.Facts!.DomainFacts.Source.SourceVersion,
            geometryVersion = keepSource.Facts.DomainFacts.GeometryVersion,
            decision = "KEEP_NEW", reason = "Verified source needs a distinct defect",
            classification = new { defectTypeCode, severity = "LOW", roadSectionVersionId = geometry.Route },
            supersedesDecisionId = keepSource.Facts.DomainFacts.ActiveDisposition!.DecisionId,
            previousDecisionVersion = keepSource.Facts.DomainFacts.ActiveDisposition.Version
        };
        var keepKey = Guid.NewGuid().ToString();
        var keep = await SendAsync(client, HttpMethod.Post, path, keepPayload, keepKey);
        keep.StatusCode.Should().Be(HttpStatusCode.Created);
        var keepReplay = await SendAsync(client, HttpMethod.Post, path, keepPayload, keepKey);
        keepReplay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await keepReplay.Content.ReadAsStringAsync()).Should().Be(await keep.Content.ReadAsStringAsync());
        keepReplay.Headers.Location.Should().Be(keep.Headers.Location);
        keepReplay.Headers.ETag.Should().Be(keep.Headers.ETag);
        var keepBody = await keep.Content.ReadFromJsonAsync<JsonElement>();
        var createdDefectId = keepBody.GetProperty("defectId").GetGuid();
        await using (var db = sql.CreateDbContext())
        {
            (await db.Defects.CountAsync(d => d.Id == createdDefectId && d.ProjectId == project)).Should().Be(1);
            (await db.FieldInspectionTasks.CountAsync(task => task.ProjectId == project && task.DefectId == createdDefectId)).Should().Be(0);
            (await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [DefectSourceLinks] WHERE [SourceId]={report} AND [DefectId]={createdDefectId}").SingleAsync()).Should().Be(1);
        }
        await LoginAsync(client, reporter.UserName!);
        var secondFile = await UploadVerifiedAsync(client, factory);
        var secondReportResponse = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", new
        {
            description = "Second candidate source",
            evidence = new[] { new { fileId = secondFile.FileId, fileVersion = secondFile.Version, locationSource = "UNKNOWN" } }
        }, Guid.NewGuid().ToString());
        secondReportResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var secondReport = (await secondReportResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Guid secondCase;
        await using (var db = sql.CreateDbContext())
            secondCase = (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
                .SingleAsync(link => link.ReportId == secondReport && link.EndedAt == null)).CaseId;
        await LoginAsync(client, supervisor.UserName!);
        var secondCaseRead = await client.GetAsync($"/api/v1/cases/{secondCase}");
        var secondTriage = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{secondCase}/triage", new
        {
            projectId = project, verificationMethod = "EXISTING_EVIDENCE", reason = "Same route",
            routeVersionId = geometry.Route, segmentSetId = geometry.Set, geometryVersion = geometry.Version
        }, Guid.NewGuid().ToString(), secondCaseRead.Headers.ETag!.ToString());
        secondTriage.StatusCode.Should().Be(HttpStatusCode.OK);
        await LoginAsync(client, pm.UserName!);
        var secondSource = await producer.ResolveCandidateSourceAsync(pm.Id, UserRoleCode.ProjectManager, project,
            RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind.Report, secondReport);
        secondSource.Status.Should().Be(RoadGuardSystem.Services.Integration.AnhHuyProducerStatus.Ready);
        string targetVersion;
        await using (var db = sql.CreateDbContext())
            targetVersion = Convert.ToBase64String(await db.Defects.Where(item => item.Id == createdDefectId)
                .Select(item => EF.Property<byte[]>(item, "RowVersion")).SingleAsync());
        object LinkPayload(string version) => new
        {
            sourceKind = "REPORT", sourceId = secondReport,
            sourceVersion = secondSource.Facts!.DomainFacts.Source.SourceVersion,
            geometryVersion = secondSource.Facts.DomainFacts.GeometryVersion,
            decision = "LINK_EXISTING", targetDefectId = createdDefectId,
            targetVersion = version, reason = "Same verified defect"
        };
        (await SendAsync(client, HttpMethod.Post, path, LinkPayload(Convert.ToBase64String(new byte[8])),
            Guid.NewGuid().ToString())).StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        var linkKey = Guid.NewGuid().ToString();
        var linkAttempts = await Task.WhenAll(
            SendAsync(client, HttpMethod.Post, path, LinkPayload(targetVersion), linkKey),
            SendAsync(client, HttpMethod.Post, path, LinkPayload(targetVersion), linkKey));
        var linked = linkAttempts[0];
        linkAttempts[1].StatusCode.Should().Be(HttpStatusCode.Created);
        (await linkAttempts[1].Content.ReadAsStringAsync()).Should().Be(await linked.Content.ReadAsStringAsync());
        linkAttempts[1].Headers.Location.Should().Be(linked.Headers.Location);
        linkAttempts[1].Headers.ETag.Should().Be(linked.Headers.ETag);
        linked.StatusCode.Should().Be(HttpStatusCode.Created);
        (await linked.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("defectId").GetGuid().Should().Be(createdDefectId);
        await using (var db = sql.CreateDbContext())
        {
            (await db.Defects.CountAsync(item => item.ProjectId == project)).Should().Be(1);
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink>()
                .CountAsync(link => link.DefectId == createdDefectId && link.EndedAt == null)).Should().Be(2);
            (await db.FieldInspectionTasks.CountAsync(task => task.ProjectId == project && task.DefectId == createdDefectId)).Should().Be(0);
            var dossier = await new RoadGuardSystem.Services.Implementations.Integration.CaseDefectReadReader(db)
                .CaptureAsync(pm.Id, UserRoleCode.ProjectManager, project,
                    new RoadGuardSystem.DTOs.Reporting.ReportingFiltersDto());
            dossier.Should().NotBeNull();
            dossier!.Hash.Should().Be(RoadGuardSystem.Services.Reporting.CaseDefectCaptureConsumer.Hash(dossier));
            var defectFact = dossier.Defects.Single(fact => fact.DefectId == createdDefectId);
            defectFact.SourceId.Should().Be(report);
            defectFact.SourceVersion.Should().NotBeNullOrWhiteSpace();
            defectFact.Version.Should().NotBe("UNAVAILABLE");
            defectFact.PositionStatus.Should().Be("UNKNOWN");
            dossier.AuthorizedEvidence.Should().Contain(fact => fact.SourceReportId == report && fact.FileId == file.FileId);
            dossier.AuthorizedEvidence.Should().Contain(fact => fact.SourceReportId == secondReport && fact.FileId == secondFile.FileId);
            var inventory = await new RoadGuardSystem.Repositories.Implementations.Retention.Huy01RetentionInventoryContributor(db)
                .ReadAsync(file.FileId, default);
            inventory.References.Should().Contain(reference => reference.Kind == "DEFECT_SOURCE_LINK" &&
                reference.ProjectId == project);
        }
        using (var reportingScope = factory.Services.CreateScope())
        {
            var reporting = reportingScope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Reporting.IReportingService>();
            var captured = await reporting.CaptureAsync(pm.Id, project,
                new RoadGuardSystem.DTOs.Reporting.ReportingFiltersDto(), default);
            captured.Code.Should().Be("success");
            captured.Value!.CaseDefectFacts.Should().NotBeNull();
            captured.Value.CaseDefectFacts!.Defects.Should().Contain(fact => fact.DefectId == createdDefectId);
            captured.Value.CaseDefectFacts.AuthorizedEvidence.Should().Contain(fact => fact.SourceReportId == report);
            (await reporting.CaptureAsync(pm.Id, Guid.NewGuid(),
                new RoadGuardSystem.DTOs.Reporting.ReportingFiltersDto(), default)).Code
                .Should().Be("access_forbidden");
            var retention = reportingScope.ServiceProvider.GetRequiredService<RoadGuardSystem.Repositories.Retention.IRetentionInventoryRepository>();
            var composite = await retention.ReadAsync(file.FileId, default);
            composite.Should().NotBeNull();
            composite!.Complete.Should().BeFalse();
            composite.References.Should().Contain(reference => reference.Kind == "DEFECT_SOURCE_LINK" &&
                reference.ProjectId == project);
            composite.ReasonCodes.Should().Contain("HUY_INVENTORY_INCOMPLETE");
        }
        var reportingSummary = await client.GetAsync($"/api/v1/projects/{project}/reports/summary");
        reportingSummary.StatusCode.Should().Be(HttpStatusCode.OK);
        reportingSummary.Headers.ETag.Should().NotBeNull();
        var matches = await client.GetAsync($"/api/v1/projects/{project}/candidates?sourceKind=REPORT&sourceId={secondReport}&expand=true");
        matches.StatusCode.Should().Be(HttpStatusCode.OK);
        var matchBody = await matches.Content.ReadFromJsonAsync<JsonElement>();
        matchBody.GetProperty("source").GetProperty("id").GetGuid().Should().Be(secondReport);
        matchBody.GetProperty("algorithmVersion").GetString().Should().Be("huy01-1");
        var item = matchBody.GetProperty("items").EnumerateArray().Single(row => row.GetProperty("defectId").GetGuid() == createdDefectId);
        item.GetProperty("distanceMeters").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetProperty("reasonCodes").EnumerateArray().Select(code => code.GetString()).Should().Contain("GPS_MISSING");
        var defectPath = $"/api/v1/projects/{project}/defects/{createdDefectId}";
        var defectRead = await client.GetAsync(defectPath);
        defectRead.StatusCode.Should().Be(HttpStatusCode.OK);
        var assessment = await SendAsync(client, HttpMethod.Post, defectPath + "/assessments", new
        {
            defectTypeCode, severity = "MEDIUM", reason = "PM assessment",
            evidenceIds = source.Facts.EvidenceIds
        }, Guid.NewGuid().ToString(), defectRead.Headers.ETag!.ToString());
        assessment.StatusCode.Should().Be(HttpStatusCode.OK);
        await using (var db = sql.CreateDbContext())
        {
            var log = await db.DefectVerificationLogs.AsNoTracking()
                .SingleAsync(value => value.DefectId == createdDefectId && value.Action == DefectVerificationAction.Adjust);
            using var snapshot = JsonDocument.Parse(log.AfterSnapshot!);
            snapshot.RootElement.GetProperty("evidenceIds").EnumerateArray()
                .Select(value => value.GetGuid()).Should().BeEquivalentTo(source.Facts.EvidenceIds);
        }
        var verification = await SendAsync(client, HttpMethod.Post, defectPath + "/verification-decisions", new
        {
            decision = "CONFIRM", verificationMethod = "EXISTING_EVIDENCE",
            evidenceIds = source.Facts.EvidenceIds, reason = "Verified report image"
        }, Guid.NewGuid().ToString(), assessment.Headers.ETag!.ToString());
        verification.StatusCode.Should().Be(HttpStatusCode.OK);
        (await verification.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("VERIFIED");
        var verifiedList = await client.GetAsync($"/api/v1/projects/{project}/defects?status=VERIFIED&type={defectTypeCode}&pageSize=1");
        verifiedList.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifiedItems = (await verifiedList.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items");
        verifiedItems.GetArrayLength().Should().Be(1);
        verifiedItems[0].GetProperty("id").GetGuid().Should().Be(createdDefectId);
        (await SendAsync(client, HttpMethod.Post, defectPath + "/verification-decisions", new
        {
            decision = "CONFIRM", verificationMethod = "EXISTING_EVIDENCE",
            evidenceIds = source.Facts.EvidenceIds, reason = "Stale verification"
        }, Guid.NewGuid().ToString(), assessment.Headers.ETag!.ToString())).StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        await using (var db = sql.CreateDbContext())
        {
            var currentCase = await db.IncidentCases.AsNoTracking().SingleAsync(value => value.Id == incident);
            currentCase.VerificationMethod.Should().Be(RoadGuardSystem.BusinessObjects.Cases.CaseVerificationMethod.ExistingEvidence);
            (await db.Defects.AsNoTracking().SingleAsync(value => value.Id == createdDefectId)).Status
                .Should().Be(DefectStatus.Verified);
        }
        var conclusionCase = await client.GetAsync($"/api/v1/cases/{incident}");
        var confirmed = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{incident}/conclusions", new
        {
            outcome = "CONFIRMED", defectIds = new[] { createdDefectId },
            evidenceIds = source.Facts.EvidenceIds, reason = "Verified report evidence"
        }, Guid.NewGuid().ToString(), conclusionCase.Headers.ETag!.ToString());
        confirmed.StatusCode.Should().Be(HttpStatusCode.OK, await confirmed.Content.ReadAsStringAsync());
        var published = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{incident}/publications", new
        {
            reportIds = new[] { report }, defectIds = new[] { createdDefectId },
            evidenceIds = source.Facts.EvidenceIds, summary = "Verified defect on report"
        }, Guid.NewGuid().ToString(), confirmed.Headers.ETag!.ToString());
        published.StatusCode.Should().Be(HttpStatusCode.Created);
        await LoginAsync(client, reporter.UserName!);
        var ownPublished = await client.GetFromJsonAsync<JsonElement>($"/api/v1/reports/{report}");
        ownPublished.GetProperty("publicUpdates").GetArrayLength().Should().Be(1);
        await LoginAsync(client, pm.UserName!);
        var acceptedHead = await producer.ResolveCandidateSourceAsync(pm.Id, UserRoleCode.ProjectManager,
            project, RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind.Report, report);
        var forbiddenCorrection = await SendAsync(client, HttpMethod.Post, path, new
        {
            sourceKind = "REPORT", sourceId = report,
            sourceVersion = acceptedHead.Facts!.DomainFacts.Source.SourceVersion,
            geometryVersion = acceptedHead.Facts.DomainFacts.GeometryVersion,
            decision = "REJECT", reason = "Cannot detach an accepted source without downstream policy",
            supersedesDecisionId = acceptedHead.Facts.DomainFacts.ActiveDisposition!.DecisionId,
            previousDecisionVersion = acceptedHead.Facts.DomainFacts.ActiveDisposition.Version
        }, Guid.NewGuid().ToString());
        forbiddenCorrection.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await using (var db = sql.CreateDbContext())
        {
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink>()
                .CountAsync(link => link.SourceId == report && link.EndedAt == null)).Should().Be(1);
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCandidateSourceHead>()
                .SingleAsync(head => head.SourceId == report)).DecisionId.Should().Be(acceptedHead.Facts.DomainFacts.ActiveDisposition!.DecisionId);
        }
        await using (var db = sql.CreateDbContext())
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink>()
                .CountAsync(link => link.DefectId == createdDefectId)).Should().Be(2);
        await using (var db = sql.CreateDbContext()) { await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [ProjectMembers] SET [Status]=2 WHERE [ProjectId]={project} AND [UserId]={pm.Id}"); }
        (await SendAsync(client, HttpMethod.Post, path, payload, key)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using (var deniedScope = factory.Services.CreateScope())
        {
            var reporting = deniedScope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Reporting.IReportingService>();
            (await reporting.CaptureAsync(pm.Id, project,
                new RoadGuardSystem.DTOs.Reporting.ReportingFiltersDto(), default)).Code.Should().Be("access_forbidden");
            var reader = deniedScope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Integration.ICaseDefectReadReader>();
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => reader.CaptureAsync(pm.Id,
                UserRoleCode.ProjectManager, project, new RoadGuardSystem.DTOs.Reporting.ReportingFiltersDto()));
        }
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task ManualReportLabel_ApprovalAndRevision_KeepCurrentEligibilitySeparateFromHistory()
    {
        var reporter = await sql.CreateUserAsync($"label-r-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var supervisor = await sql.CreateUserAsync($"label-s-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var pm = await sql.CreateUserAsync($"label-p-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var project = Guid.NewGuid();
        var type = $"HUY-{Guid.NewGuid():N}";
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(RoadGuardSystem.BusinessObjects.Projects.Project.Create(project, $"LB-{Guid.NewGuid():N}",
                "Label project", null, 32648, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(RoadGuardSystem.BusinessObjects.Projects.ProjectMember.CreatePrimaryProjectManager(
                Guid.NewGuid(), project, pm.Id, new DateOnly(2026, 1, 1)));
            db.DefectTypes.Add(RoadGuardSystem.BusinessObjects.Catalogs.DefectType.Create(type, "Label fixture type"));
            await db.SaveChangesAsync();
        }
        var artifacts = new ExportArtifactStorage();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
            services.RemoveAll<IAnh02ArtifactStore>(); services.AddSingleton<IAnh02ArtifactStore>(artifacts);
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);
        var file = await UploadVerifiedAsync(client, factory);
        var created = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", new
        {
            description = "Training source",
            evidence = new[] { new { fileId = file.FileId, fileVersion = file.Version, locationSource = "UNKNOWN" } }
        }, Guid.NewGuid().ToString());
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var report = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Guid caseId;
        await using (var db = sql.CreateDbContext())
            caseId = (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
                .SingleAsync(link => link.ReportId == report && link.EndedAt == null)).CaseId;
        var geometry = await CreatePublishedGeometryAsync(client, pm.UserName!, supervisor.UserName!, project);
        await LoginAsync(client, supervisor.UserName!);
        var caseRead = await client.GetAsync($"/api/v1/cases/{caseId}");
        (await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{caseId}/triage", new
        {
            projectId = project, verificationMethod = "EXISTING_EVIDENCE", reason = "Known source",
            routeVersionId = geometry.Route, segmentSetId = geometry.Set, geometryVersion = geometry.Version
        }, Guid.NewGuid().ToString(), caseRead.Headers.ETag!.ToString())).StatusCode.Should().Be(HttpStatusCode.OK);
        using var scope = factory.Services.CreateScope();
        var producer = scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Integration.IAnhHuyProducerService>();
        var source = await producer.ResolveCandidateSourceAsync(pm.Id, UserRoleCode.ProjectManager, project,
            RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind.Report, report);
        source.Status.Should().Be(RoadGuardSystem.Services.Integration.AnhHuyProducerStatus.Ready);
        await LoginAsync(client, pm.UserName!);
        var path = $"/api/v1/projects/{project}/labels";
        (await SendAsync(client, HttpMethod.Post, path, new
        {
            sourceKind = "AI_DETECTION", sourceId = Guid.NewGuid(), sourceVersion = "unavailable",
            fileId = file.FileId, annotation = new { kind = "BBOX", coordinateSpace = "NORMALIZED", x = 0.1m, y = 0.2m, width = 0.3m, height = 0.4m },
            defectTypeCode = type, reason = "AI provenance not ready"
        }, Guid.NewGuid().ToString())).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SendAsync(client, HttpMethod.Post, path, new
        {
            sourceKind = "REPORT", sourceId = report, sourceVersion = source.Facts!.DomainFacts.Source.SourceVersion,
            fileId = file.FileId, annotation = new { kind = "BBOX", coordinateSpace = "NORMALIZED", x = 0.9m, y = 0.2m, width = 0.3m, height = 0.4m },
            defectTypeCode = type, reason = "Invalid rectangle"
        }, Guid.NewGuid().ToString())).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SendAsync(client, HttpMethod.Post, path, new
        {
            sourceKind = "REPORT", sourceId = report, sourceVersion = source.Facts.DomainFacts.Source.SourceVersion,
            fileId = Guid.NewGuid(), annotation = new { kind = "BBOX", coordinateSpace = "NORMALIZED", x = 0.1m, y = 0.2m, width = 0.3m, height = 0.4m },
            defectTypeCode = type, reason = "Unrelated file"
        }, Guid.NewGuid().ToString())).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SendAsync(client, HttpMethod.Post, path, new
        {
            sourceKind = "REPORT", sourceId = report, sourceVersion = "stale-version",
            fileId = file.FileId, annotation = new { kind = "BBOX", coordinateSpace = "NORMALIZED", x = 0.1m, y = 0.2m, width = 0.3m, height = 0.4m },
            defectTypeCode = type, reason = "Stale source"
        }, Guid.NewGuid().ToString())).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await SendAsync(client, HttpMethod.Post, path, new
        {
            sourceKind = "REPORT", sourceId = report, sourceVersion = source.Facts.DomainFacts.Source.SourceVersion,
            fileId = file.FileId, annotation = new { kind = "BBOX", coordinateSpace = "NORMALIZED", x = 0.1m, y = 0.2m, width = 0.3m, height = 0.4m },
            defectTypeCode = "INACTIVE", reason = "Unknown type"
        }, Guid.NewGuid().ToString())).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SendAsync(client, HttpMethod.Post, path, new
        {
            sourceKind = "REPORT", sourceId = report, sourceVersion = source.Facts.DomainFacts.Source.SourceVersion,
            fileId = file.FileId, annotation = new { kind = "BBOX", coordinateSpace = "NORMALIZED", x = 0.1m, y = 0.2m, width = 0.3m, height = 0.4m },
            defectTypeCode = type, reason = "Unknown field", unknown = true
        }, Guid.NewGuid().ToString())).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var createPayload = new
        {
            sourceKind = "REPORT", sourceId = report, sourceVersion = source.Facts!.DomainFacts.Source.SourceVersion,
            fileId = file.FileId, annotation = new { kind = "BBOX", coordinateSpace = "NORMALIZED", x = 0.1m, y = 0.2m, width = 0.3m, height = 0.4m },
            defectTypeCode = type, reason = "Manual classification"
        };
        var failedKey = Guid.NewGuid().ToString();
        var precommit = new CommandCommitFailure(failedKey, false);
        await using (var db = ModuleFaultContext(precommit))
            await Assert.ThrowsAsync<RetryLimitExceededException>(() => LabelService(db).CreateAsync(pm.Id,
                UserRoleCode.ProjectManager, project,
                new("REPORT", report, source.Facts.DomainFacts.Source.SourceVersion, file.FileId,
                    new("BBOX", "NORMALIZED", 0.1m, 0.2m, 0.3m, 0.4m), type,
                    "Manual classification"), failedKey, null, default));
        precommit.Failures.Should().Be(3);
        await using (var db = sql.CreateDbContext())
        {
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelHead>()
                .CountAsync(row => row.ProjectId == project && row.SourceId == report)).Should().Be(0);
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelRevision>()
                .CountAsync(row => row.FileId == file.FileId)).Should().Be(0);
            (await db.AuditLogs.CountAsync(row => row.ActorUserId == pm.Id && row.EventType == "training_label_created")).Should().Be(0);
            (await db.IdempotencyRecords.CountAsync(row => row.ActorUserId == pm.Id && row.IdempotencyKey == failedKey)).Should().Be(0);
        }
        var createKey = Guid.NewGuid().ToString();
        var createdPair = await Task.WhenAll(SendAsync(client, HttpMethod.Post, path, createPayload, createKey),
            SendAsync(client, HttpMethod.Post, path, createPayload, createKey));
        var create = createdPair[0];
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var createReplay = createdPair[1];
        createReplay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await createReplay.Content.ReadAsStringAsync()).Should().Be(await create.Content.ReadAsStringAsync());
        createReplay.Headers.Location.Should().Be(create.Headers.Location);
        createReplay.Headers.ETag.Should().Be(create.Headers.ETag);
        (await SendAsync(client, HttpMethod.Post, path, new
        {
            sourceKind = "REPORT", sourceId = report, sourceVersion = source.Facts.DomainFacts.Source.SourceVersion,
            fileId = file.FileId, annotation = createPayload.annotation, defectTypeCode = type,
            reason = "Different same-key payload"
        }, createKey)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        var labelId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var labelPath = $"{path}/{labelId}";
        var list = await client.GetAsync($"{path}?pageSize=1");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await list.Content.ReadFromJsonAsync<JsonElement>();
        page.GetProperty("items").GetArrayLength().Should().Be(1);
        page.GetProperty("items")[0].GetProperty("id").GetGuid().Should().Be(labelId);
        var firstVersion = create.Headers.ETag!.ToString();
        var reviewKey = Guid.NewGuid().ToString();
        var reviewAttempts = await Task.WhenAll(
            SendAsync(client, HttpMethod.Post, $"/api/v1/labels/{labelId}/review",
                new { decision = "APPROVE", reason = "Reviewed source" }, reviewKey, firstVersion),
            SendAsync(client, HttpMethod.Post, $"/api/v1/labels/{labelId}/review",
                new { decision = "APPROVE", reason = "Competing review" }, Guid.NewGuid().ToString(), firstVersion));
        var review = reviewAttempts.Single(response => response.StatusCode == HttpStatusCode.OK);
        reviewAttempts.Single(response => response != review).StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        review.StatusCode.Should().Be(HttpStatusCode.OK);
        (await review.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("APPROVED");
        var approvedReader = scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Integration.IApprovedTrainingLabelReader>();
        var sourceAccess = scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Integration.ITrainingSourceAccessReader>();
        var approved = await approvedReader.CaptureApprovedAsync(pm.Id, UserRoleCode.ProjectManager, project,
            new RoadGuardSystem.Services.Integration.TrainingLabelFilterV1([], [], null, null));
        approved.Should().NotBeNull();
        approved!.Labels.Should().ContainSingle();
        approved.Labels[0].LabelId.Should().Be(labelId);
        approved.Labels[0].FileId.Should().Be(file.FileId);
        approved.Labels[0].FileVersion.Should().Be(file.Version);
        approved.Labels[0].Sha256.Should().HaveLength(64);
        approved.Labels[0].SizeBytes.Should().BeGreaterThan(0);
        (await sourceAccess.CanReadAsync(pm.Id, UserRoleCode.ProjectManager, project, [file.FileId])).Should().BeTrue();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => approvedReader.CaptureApprovedAsync(
            reporter.Id, UserRoleCode.Reporter, project,
            new RoadGuardSystem.Services.Integration.TrainingLabelFilterV1([], [], null, null)));
        (await sourceAccess.CanReadAsync(reporter.Id, UserRoleCode.Reporter, project, [file.FileId])).Should().BeFalse();
        (await sourceAccess.CanReadAsync(pm.Id, UserRoleCode.ProjectManager, Guid.NewGuid(), [file.FileId])).Should().BeFalse();
        (await approvedReader.CaptureApprovedAsync(pm.Id, UserRoleCode.ProjectManager, project,
            new RoadGuardSystem.Services.Integration.TrainingLabelFilterV1([Guid.NewGuid()], [], null, null))).Should().BeNull();
        var exportPath = $"/api/v1/projects/{project}/exports";
        var exportRequest = new { kind = "TRAINING", format = "ZIP", includeOriginalFiles = true };
        var exportKey = Guid.NewGuid().ToString();
        var export = await SendAsync(client, HttpMethod.Post, exportPath, exportRequest, exportKey);
        export.StatusCode.Should().Be(HttpStatusCode.Accepted, await export.Content.ReadAsStringAsync());
        var exportReplay = await SendAsync(client, HttpMethod.Post, exportPath, exportRequest, exportKey);
        exportReplay.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await exportReplay.Content.ReadAsStringAsync()).Should().Be(await export.Content.ReadAsStringAsync());
        exportReplay.Headers.Location.Should().Be(export.Headers.Location);
        exportReplay.Headers.ETag.Should().Be(export.Headers.ETag);
        var exportId = (await export.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var historicalManifest = await client.GetFromJsonAsync<JsonElement>($"{exportPath}/{exportId}/manifest");
        historicalManifest.GetProperty("labels").GetArrayLength().Should().Be(1);
        historicalManifest.GetProperty("labels")[0].GetProperty("labelId").GetGuid().Should().Be(labelId);
        historicalManifest.GetProperty("files")[0].GetProperty("included").GetBoolean().Should().BeTrue();
        using (var workerScope = factory.Services.CreateScope())
            (await workerScope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Exports.IExportService>()
                .ProcessNextAsync(default)).Should().BeTrue();
        var completedExport = await client.GetFromJsonAsync<JsonElement>($"{exportPath}/{exportId}");
        completedExport.GetProperty("status").GetString().Should().Be("SUCCEEDED");
        var zip = await client.GetAsync($"{exportPath}/{exportId}/content");
        zip.StatusCode.Should().Be(HttpStatusCode.OK);
        zip.Content.Headers.ContentType!.MediaType.Should().Be("application/zip");
        (await zip.Content.ReadAsByteArrayAsync()).Take(2).Should().Equal((byte)'P', (byte)'K');
        var pendingExport = await SendAsync(client, HttpMethod.Post, exportPath, exportRequest, Guid.NewGuid().ToString());
        pendingExport.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var pendingExportId = (await pendingExport.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        string historicalRevisionVersion;
        await using (var db = sql.CreateDbContext())
        {
            var before = await new RoadGuardSystem.Repositories.Implementations.Retention.Huy01RetentionInventoryContributor(db)
                .ReadAsync(file.FileId, default);
            historicalRevisionVersion = before.References.Single(reference => reference.Kind == "TRAINING_LABEL_REVISION").SourceVersion;
        }
        var revision = await SendAsync(client, HttpMethod.Post, $"{labelPath}/revisions", new
        {
            fileId = file.FileId, annotation = new { kind = "BBOX", coordinateSpace = "NORMALIZED", x = 0.2m, y = 0.2m, width = 0.3m, height = 0.3m },
            defectTypeCode = type, reason = "Refined outline"
        }, Guid.NewGuid().ToString(), review.Headers.ETag!.ToString());
        revision.StatusCode.Should().Be(HttpStatusCode.Created);
        var current = await client.GetAsync(labelPath);
        current.StatusCode.Should().Be(HttpStatusCode.OK);
        (await current.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("PENDING");
        var afterRevision = await approvedReader.CaptureApprovedAsync(pm.Id, UserRoleCode.ProjectManager, project,
            new RoadGuardSystem.Services.Integration.TrainingLabelFilterV1([], [], null, null));
        afterRevision.Should().NotBeNull();
        afterRevision!.Labels.Should().BeEmpty();
        (await sourceAccess.CanReadAsync(pm.Id, UserRoleCode.ProjectManager, project, [file.FileId])).Should().BeFalse();
        (await SendAsync(client, HttpMethod.Post, exportPath, exportRequest, Guid.NewGuid().ToString()))
            .StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var unchangedManifest = await client.GetFromJsonAsync<JsonElement>($"{exportPath}/{exportId}/manifest");
        unchangedManifest.GetRawText().Should().Be(historicalManifest.GetRawText());
        using (var workerScope = factory.Services.CreateScope())
            (await workerScope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Exports.IExportService>()
                .ProcessNextAsync(default)).Should().BeTrue();
        var failedExport = await client.GetFromJsonAsync<JsonElement>($"{exportPath}/{pendingExportId}");
        failedExport.GetProperty("status").GetString().Should().Be("FAILED");
        failedExport.GetProperty("errorCode").GetString().Should().Be("export_source_access_revoked");
        (await client.GetAsync($"{exportPath}/{exportId}/content")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await using (var db = sql.CreateDbContext())
        {
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelRevision>()
                .CountAsync(row => row.LabelId == labelId)).Should().Be(2);
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyTrainingLabelReview>()
                .CountAsync(row => row.LabelId == labelId && row.Decision == "APPROVED")).Should().Be(1);
            var inventory = await new RoadGuardSystem.Repositories.Implementations.Retention.Huy01RetentionInventoryContributor(db)
                .ReadAsync(file.FileId, default);
            inventory.References.Count(row => row.Kind == "TRAINING_LABEL_REVISION").Should().Be(2);
            inventory.References.Where(row => row.Kind == "TRAINING_LABEL_REVISION")
                .Should().Contain(row => row.SourceVersion == historicalRevisionVersion);
            inventory.References.Count(row => row.Kind == "TRAINING_LABEL_REVIEW").Should().Be(1);
            inventory.Complete.Should().BeFalse();
            inventory.ReasonCodes.Should().Contain("HUY_REPAIR_REFERENCE_UNAVAILABLE");
            (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [TrainingLabelRevisions] SET [Reason]={"Changed history"} WHERE [LabelId]={labelId}")))
                .Number.Should().Be(51001);
            (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [TrainingLabelReviews] SET [Reason]={"Changed review"} WHERE [LabelId]={labelId}")))
                .Number.Should().Be(51001);
            (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [TrainingLabels] SET [CurrentRevision]=[CurrentRevision]+2 WHERE [Id]={labelId}")))
                .Number.Should().Be(51001);
            (await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>()
                .MigrateAsync("20261003180000_Huy01DefectSourceLinks"))).Number.Should().Be(51000);
        }
        using (var retentionScope = factory.Services.CreateScope())
        {
            var composite = await retentionScope.ServiceProvider
                .GetRequiredService<RoadGuardSystem.Repositories.Retention.IRetentionInventoryRepository>()
                .ReadAsync(file.FileId, default);
            composite.Should().NotBeNull();
            composite!.Complete.Should().BeFalse();
            composite.References.Count(reference => reference.Kind == "TRAINING_LABEL_REVISION").Should().Be(2);
            composite.References.Count(reference => reference.Kind == "TRAINING_LABEL_REVIEW").Should().Be(1);
        }
        (await SendAsync(client, HttpMethod.Post, $"/api/v1/labels/{labelId}/review",
            new { decision = "APPROVE", reason = "Stale review" }, Guid.NewGuid().ToString(), firstVersion))
            .StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        await using (var db = sql.CreateDbContext())
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [ProjectMembers] SET [Status]=2 WHERE [ProjectId]={project} AND [UserId]={pm.Id}");
        (await SendAsync(client, HttpMethod.Post, path, createPayload, createKey)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private RoadGuardDbContext ModuleFaultContext(CommandCommitFailure failure)
        => new(new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(sql.ConnectionString, options => options.UseNetTopologySuite())
            .ReplaceService<IExecutionStrategyFactory, CommitFailureExecutionStrategyFactory>().AddInterceptors(failure).Options);

    private static CandidateDecisionService CandidateService(RoadGuardDbContext db)
    {
        var receipt = new IdempotencyOperationService(db);
        var guard = new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System);
        var producer = new AnhHuyProducerService(new AnhHuyFactsRepository(db), new GeometryWorkflowPersistenceService(db, receipt), guard);
        return new(new CandidateDecisionRepository(db), new CaseWorkflowRepository(db), producer, guard, receipt);
    }

    private static RoadGuardSystem.Services.Implementations.Labels.TrainingLabelService LabelService(RoadGuardDbContext db)
    {
        var receipt = new IdempotencyOperationService(db);
        var guard = new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System);
        var producer = new AnhHuyProducerService(new AnhHuyFactsRepository(db),
            new GeometryWorkflowPersistenceService(db, receipt), guard);
        return new(new RoadGuardSystem.Repositories.Implementations.Labels.TrainingLabelRepository(db),
            new CandidateDecisionRepository(db), new CaseWorkflowRepository(db), producer, guard, receipt);
    }

    private static ReporterLifecycleService LifecycleService(RoadGuardDbContext db)
        => new(new ReporterLifecycleRepository(db, new ReporterReportRepository(db)),
            new AnhHuyProducerService(new AnhHuyFactsRepository(db), null!, null!), null!, new IdempotencyOperationService(db));

    private sealed class CommandCommitFailure(string key, bool afterCommit) : DbTransactionInterceptor
    {
        public int Failures { get; private set; }
        private bool HasReceipt(DbContext? context) => context?.Set<RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord>()
            .Local.Any(r => r.IdempotencyKey == key) == true;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!afterCommit && HasReceipt(eventData.Context)) { Failures++; throw new CommitFailureTransientException("Injected module precommit failure."); }
            return ValueTask.FromResult(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (afterCommit && Failures == 0 && HasReceipt(eventData.Context)) { Failures++; throw new CommitFailureTransientException("Injected module acknowledgement loss."); }
            return Task.CompletedTask;
        }
    }

    private static async Task<(Guid Route, Guid Set, string Version)> CreatePublishedGeometryAsync(HttpClient client, string pm, string supervisor, Guid project)
    {
        var prefix = $"/api/v1/projects/{project}";
        await LoginAsync(client, pm);
        var draft = await SendAsync(client, HttpMethod.Post, prefix + "/road-geometry-drafts", new { sourceKind = "COORDINATES", sourceCrs = 32648,
            stationOriginMeters = 0, changeReason = "Candidate fixture", coordinates = new[] { new { x = 500000d, y = 1200000d }, new { x = 500250d, y = 1200000d } },
            widthProfile = new[] { new { fromOffsetMeters = 0d, toOffsetMeters = 250d, widthMeters = 7d } }, surveyWidthMeters = 9d, roadCode = $"R-{Guid.NewGuid():N}" }, Guid.NewGuid().ToString());
        draft.EnsureSuccessStatusCode();
        await LoginAsync(client, supervisor);
        var confirmed = await SendAsync(client, HttpMethod.Post, draft.Headers.Location!.OriginalString + "/confirm",
            new { expectedCurrentVersionId = (Guid?)null, effectiveFrom = "2026-10-02T00:00:00Z", reason = "Approved fixture geometry" }, Guid.NewGuid().ToString(), draft.Headers.ETag!.ToString());
        confirmed.EnsureSuccessStatusCode(); var route = await confirmed.Content.ReadFromJsonAsync<JsonElement>();
        var routeId = route.GetProperty("routeVersionId").GetGuid(); var roadId = route.GetProperty("roadSectionId").GetGuid();
        await LoginAsync(client, pm);
        var setsPath = prefix + $"/road-sections/{roadId}/versions/{routeId}/segment-sets";
        var created = await SendAsync(client, HttpMethod.Post, setsPath, new { targetLengthMeters = 100d, remainderMode = "KEEP" }, Guid.NewGuid().ToString());
        created.EnsureSuccessStatusCode(); var setId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await SendAsync(client, HttpMethod.Post, setsPath + $"/{setId}/publish", new { expectedPublishedSetId = (Guid?)null, reason = "Published fixture" }, Guid.NewGuid().ToString(), created.Headers.ETag!.ToString())).EnsureSuccessStatusCode();
        var package = await client.GetAsync(prefix + $"/geometry-package?routeVersionId={routeId}&segmentSetId={setId}");
        package.EnsureSuccessStatusCode(); return (routeId, setId, package.Headers.ETag!.Tag!.Trim('"'));
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task Case_TriageLinkSplitConclusionPublication_KeepVersionsHistoryAndRecipientPrivacy()
    {
        var reporter = await sql.CreateUserAsync($"case-reporter-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var supervisor = await sql.CreateUserAsync($"case-supervisor-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var pm = await sql.CreateUserAsync($"case-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var projectId = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(RoadGuardSystem.BusinessObjects.Projects.Project.Create(projectId, $"CS-{Guid.NewGuid():N}", "Case project", null, 32648,
                new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(RoadGuardSystem.BusinessObjects.Projects.ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), projectId, pm.Id, new DateOnly(2026, 1, 1)));
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);
        var file = await UploadVerifiedAsync(client, factory);
        var created = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", new { description = "Case workflow",
            evidence = new[] { new { fileId = file.FileId, fileVersion = file.Version, locationSource = "UNKNOWN" } } }, Guid.NewGuid().ToString());
        var reportId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Guid caseId;
        await using (var db = sql.CreateDbContext()) caseId = (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>().SingleAsync(l => l.ReportId == reportId && l.EndedAt == null)).CaseId;
        await LoginAsync(client, pm.UserName!);
        (await client.GetAsync("/api/v1/cases")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync($"/api/v1/cases/{caseId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await LoginAsync(client, supervisor.UserName!);
        var unassigned = await client.GetAsync($"/api/v1/cases/{caseId}");
        unassigned.StatusCode.Should().Be(HttpStatusCode.OK);
        var triage = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{caseId}/triage", new { projectId, verificationMethod = "EXISTING_EVIDENCE", reason = "Initial routing" }, Guid.NewGuid().ToString(), unassigned.Headers.ETag!.ToString());
        triage.StatusCode.Should().Be(HttpStatusCode.OK);
        await LoginAsync(client, pm.UserName!);
        var scoped = await client.GetAsync($"/api/v1/cases/{caseId}");
        scoped.StatusCode.Should().Be(HttpStatusCode.OK);
        Guid evidenceId;
        await using (var db = sql.CreateDbContext()) evidenceId = (await db.Reports.Include(r => r.OriginalEvidence).SingleAsync(r => r.Id == reportId)).OriginalEvidence.Single().Id;
        var conclusion = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{caseId}/conclusions", new { outcome = "NO_DEFECT", defectIds = Array.Empty<Guid>(), evidenceIds = new[] { evidenceId }, reason = "Photo has no defect" }, Guid.NewGuid().ToString(), scoped.Headers.ETag!.ToString());
        conclusion.StatusCode.Should().Be(HttpStatusCode.OK);
        var key = Guid.NewGuid().ToString();
        var body = new { reportIds = new[] { reportId }, defectIds = Array.Empty<Guid>(), evidenceIds = new[] { evidenceId }, summary = "No defect found" };
        var published = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{caseId}/publications", body, key, conclusion.Headers.ETag!.ToString());
        published.StatusCode.Should().Be(HttpStatusCode.Created);
        var replay = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{caseId}/publications", body, key, conclusion.Headers.ETag!.ToString());
        (await replay.Content.ReadAsStringAsync()).Should().Be(await published.Content.ReadAsStringAsync());
        replay.Headers.ETag.Should().Be(published.Headers.ETag); replay.Headers.Location.Should().Be(published.Headers.Location);
        await LoginAsync(client, reporter.UserName!);
        var own = await client.GetFromJsonAsync<JsonElement>($"/api/v1/reports/{reportId}");
        own.GetProperty("publicUpdates").GetArrayLength().Should().Be(1);
        var previousUpdate = own.GetProperty("publicUpdates").GetRawText();
        var extraFile = await UploadVerifiedAsync(client, factory);
        var ownResponse = await client.GetAsync($"/api/v1/reports/{reportId}");
        var supplemented = await SendAsync(client, HttpMethod.Post, $"/api/v1/reports/{reportId}/supplements",
            new { description = "New evidence after conclusion", evidence = new[] { new { fileId = extraFile.FileId, fileVersion = extraFile.Version, locationSource = "UNKNOWN" } } }, Guid.NewGuid().ToString(), ownResponse.Headers.ETag!.ToString());
        supplemented.StatusCode.Should().Be(HttpStatusCode.OK);
        (await supplemented.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("publicUpdates").GetRawText().Should().Be(previousUpdate);
        (await client.GetAsync($"/api/v1/cases/{caseId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await using (var db = sql.CreateDbContext())
        {
            (await db.Set<RoadGuardSystem.BusinessObjects.Cases.CasePublication>().CountAsync(p => p.CaseId == caseId)).Should().Be(1);
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyPublicationRecipient>().CountAsync(r => r.ReportId == reportId)).Should().Be(1);
            (await db.IncidentCases.SingleAsync(c => c.Id == caseId)).Status.Should().Be(RoadGuardSystem.BusinessObjects.Cases.IncidentCaseStatus.Open);
        }
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task Case_LinkSplitAndPublication_KeepRecipientEvidencePrivateAcrossReporters()
    {
        var reporterA = await sql.CreateUserAsync($"case-a-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var reporterB = await sql.CreateUserAsync($"case-b-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var supervisor = await sql.CreateUserAsync($"case-s-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var pm = await sql.CreateUserAsync($"case-p-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var project = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(RoadGuardSystem.BusinessObjects.Projects.Project.Create(project, $"CP-{Guid.NewGuid():N}",
                "Recipient privacy project", null, 32648, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(RoadGuardSystem.BusinessObjects.Projects.ProjectMember.CreatePrimaryProjectManager(
                Guid.NewGuid(), project, pm.Id, new DateOnly(2026, 1, 1)));
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        async Task<(Guid Report, Guid Case, Guid Evidence)> CreateAsync(string user)
        {
            await LoginAsync(client, user);
            var file = await UploadVerifiedAsync(client, factory);
            var created = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", new
            {
                description = "Recipient-scoped report",
                evidence = new[] { new { fileId = file.FileId, fileVersion = file.Version, locationSource = "UNKNOWN" } }
            }, Guid.NewGuid().ToString());
            created.StatusCode.Should().Be(HttpStatusCode.Created);
            var report = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
            await using var db = sql.CreateDbContext();
            var incident = (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
                .SingleAsync(link => link.ReportId == report && link.EndedAt == null)).CaseId;
            var evidence = (await db.Reports.Include(row => row.OriginalEvidence)
                .SingleAsync(row => row.Id == report)).OriginalEvidence.Single().Id;
            return (report, incident, evidence);
        }
        var a = await CreateAsync(reporterA.UserName!);
        var b = await CreateAsync(reporterB.UserName!);
        var foreign = await CreateAsync(reporterA.UserName!);
        var otherProject = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(RoadGuardSystem.BusinessObjects.Projects.Project.Create(otherProject, $"CP-{Guid.NewGuid():N}",
                "Other recipient project", null, 32648, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(RoadGuardSystem.BusinessObjects.Projects.ProjectMember.CreatePrimaryProjectManager(
                Guid.NewGuid(), otherProject, pm.Id, new DateOnly(2026, 1, 1)));
            await db.SaveChangesAsync();
        }
        await LoginAsync(client, supervisor.UserName!);
        foreach (var (incident, assignedProject) in new[] { (a.Case, project), (b.Case, project), (foreign.Case, otherProject) })
        {
            var current = await client.GetAsync($"/api/v1/cases/{incident}");
            (await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{incident}/triage", new
            {
                projectId = assignedProject, verificationMethod = "EXISTING_EVIDENCE", reason = "Assigned project"
            }, Guid.NewGuid().ToString(), current.Headers.ETag!.ToString())).StatusCode.Should().Be(HttpStatusCode.OK);
        }
        await LoginAsync(client, pm.UserName!);
        var source = await client.GetAsync($"/api/v1/cases/{b.Case}");
        var target = await client.GetAsync($"/api/v1/cases/{a.Case}");
        var foreignSource = await client.GetAsync($"/api/v1/cases/{foreign.Case}");
        var crossProject = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{a.Case}/report-links", new
        {
            reportIds = new[] { foreign.Report }, reason = "Must not cross projects",
            sourceCaseVersions = new Dictionary<Guid, string> { [foreign.Case] = foreignSource.Headers.ETag!.Tag!.Trim('"') }
        }, Guid.NewGuid().ToString(), target.Headers.ETag!.ToString());
        crossProject.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var staleSource = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{a.Case}/report-links", new
        {
            reportIds = new[] { b.Report }, reason = "Stale source must roll back",
            sourceCaseVersions = new Dictionary<Guid, string> { [b.Case] = Convert.ToBase64String(new byte[8]) }
        }, Guid.NewGuid().ToString(), target.Headers.ETag!.ToString());
        staleSource.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        await using (var db = sql.CreateDbContext())
        {
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
                .CountAsync(link => new[] { a.Report, b.Report, foreign.Report }.Contains(link.ReportId) && link.EndedAt == null))
                .Should().Be(3);
            (await db.Set<RoadGuardSystem.BusinessObjects.Cases.CaseReportLinkHistory>()
                .CountAsync(history => history.ToCaseId == a.Case)).Should().Be(0);
        }
        var linked = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{a.Case}/report-links", new
        {
            reportIds = new[] { b.Report }, reason = "Same incident",
            sourceCaseVersions = new Dictionary<Guid, string> { [b.Case] = source.Headers.ETag!.Tag!.Trim('"') }
        }, Guid.NewGuid().ToString(), target.Headers.ETag!.ToString());
        linked.StatusCode.Should().Be(HttpStatusCode.OK, await linked.Content.ReadAsStringAsync());
        var split = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{a.Case}/report-splits", new
        {
            reportIds = new[] { b.Report }, reason = "Separate while reviewing"
        }, Guid.NewGuid().ToString(), linked.Headers.ETag!.ToString());
        split.StatusCode.Should().Be(HttpStatusCode.Created, await split.Content.ReadAsStringAsync());
        var splitCase = (await split.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var splitSource = await client.GetAsync($"/api/v1/cases/{splitCase}");
        var activeTarget = await client.GetAsync($"/api/v1/cases/{a.Case}");
        var relinked = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{a.Case}/report-links", new
        {
            reportIds = new[] { b.Report }, reason = "Review completed",
            sourceCaseVersions = new Dictionary<Guid, string> { [splitCase] = splitSource.Headers.ETag!.Tag!.Trim('"') }
        }, Guid.NewGuid().ToString(), activeTarget.Headers.ETag!.ToString());
        relinked.StatusCode.Should().Be(HttpStatusCode.OK, await relinked.Content.ReadAsStringAsync());
        var concluded = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{a.Case}/conclusions", new
        {
            outcome = "NO_DEFECT", defectIds = Array.Empty<Guid>(), evidenceIds = new[] { a.Evidence }, reason = "No defect on A"
        }, Guid.NewGuid().ToString(), relinked.Headers.ETag!.ToString());
        concluded.StatusCode.Should().Be(HttpStatusCode.OK);
        var denied = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{a.Case}/publications", new
        {
            reportIds = new[] { a.Report, b.Report }, defectIds = Array.Empty<Guid>(),
            evidenceIds = new[] { a.Evidence }, summary = "Evidence belongs only to A"
        }, Guid.NewGuid().ToString(), concluded.Headers.ETag!.ToString());
        denied.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var allowed = await SendAsync(client, HttpMethod.Post, $"/api/v1/cases/{a.Case}/publications", new
        {
            reportIds = new[] { a.Report }, defectIds = Array.Empty<Guid>(),
            evidenceIds = new[] { a.Evidence }, summary = "A-only publication"
        }, Guid.NewGuid().ToString(), concluded.Headers.ETag!.ToString());
        allowed.StatusCode.Should().Be(HttpStatusCode.Created);
        await using (var db = sql.CreateDbContext())
        {
            (await db.Set<RoadGuardSystem.BusinessObjects.Cases.CasePublication>()
                .CountAsync(row => row.CaseId == a.Case)).Should().Be(1);
            (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
                .CountAsync(row => row.CaseId == a.Case && row.EndedAt == null)).Should().Be(2);
            (await db.Set<RoadGuardSystem.BusinessObjects.Cases.CaseReportLinkHistory>()
                .CountAsync(row => row.FromCaseId == a.Case || row.ToCaseId == a.Case)).Should().Be(3);
        }
        await LoginAsync(client, reporterA.UserName!);
        (await client.GetFromJsonAsync<JsonElement>($"/api/v1/reports/{a.Report}"))
            .GetProperty("publicUpdates").GetArrayLength().Should().Be(1);
        (await client.GetAsync($"/api/v1/reports/{b.Report}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        await LoginAsync(client, reporterB.UserName!);
        (await client.GetFromJsonAsync<JsonElement>($"/api/v1/reports/{b.Report}"))
            .GetProperty("publicUpdates").GetArrayLength().Should().Be(0);
        (await client.GetAsync($"/api/v1/reports/{a.Report}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task OwnReport_ReadSupplementReplayPrivacyAndDownload_UseRealSqlGraph()
    {
        var reporter = await sql.CreateUserAsync($"reporter-lifecycle-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var other = await sql.CreateUserAsync($"reporter-other-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>();
            services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);
        var original = await UploadVerifiedAsync(client, factory);
        var created = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", new { description = "Original",
            evidence = new[] { new { fileId = original.FileId, fileVersion = original.Version, locationSource = "UNKNOWN" } } }, Guid.NewGuid().ToString());
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var reportId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var detail = await client.GetAsync($"/api/v1/reports/{reportId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        var initial = await detail.Content.ReadFromJsonAsync<JsonElement>();
        initial.GetProperty("evidence").GetArrayLength().Should().Be(1);
        initial.GetProperty("publicUpdates").GetArrayLength().Should().Be(0);
        var evidenceId = initial.GetProperty("evidence")[0].GetProperty("id").GetGuid();
        var download = await client.GetAsync($"/api/v1/reports/{reportId}/evidence/{evidenceId}/download");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        (await download.Content.ReadAsByteArrayAsync()).Should().Equal(1, 2, 3, 4);
        var added = await UploadVerifiedAsync(client, factory);
        var key = "supplement " + Guid.NewGuid();
        var payload = new { description = "Additional", evidence = new[] { new { fileId = added.FileId, fileVersion = added.Version, locationSource = "UNKNOWN" } } };
        var path = $"/api/v1/reports/{reportId}/supplements";
        var supplements = await Task.WhenAll(SendAsync(client, HttpMethod.Post, path, payload, key, detail.Headers.ETag!.ToString()),
            SendAsync(client, HttpMethod.Post, path, payload, key, detail.Headers.ETag!.ToString()));
        var first = supplements[0];
        supplements[1].StatusCode.Should().Be(HttpStatusCode.OK);
        (await supplements[1].Content.ReadAsStringAsync()).Should().Be(await first.Content.ReadAsStringAsync());
        supplements[1].Headers.ETag.Should().Be(first.Headers.ETag);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        first.Headers.ETag.Should().NotBe(detail.Headers.ETag);
        var replay = await SendAsync(client, HttpMethod.Post, path, payload, key, detail.Headers.ETag!.ToString());
        replay.StatusCode.Should().Be(HttpStatusCode.OK);
        (await replay.Content.ReadAsStringAsync()).Should().Be(await first.Content.ReadAsStringAsync());
        replay.Headers.ETag.Should().Be(first.Headers.ETag);
        var faultFile = await UploadVerifiedAsync(client, factory);
        var faultRequest = new CreateReporterReportRequestDto("Fault-tested supplement",
            [new ReportEvidenceInputDto(faultFile.FileId, faultFile.Version, "UNKNOWN")]);
        Guid activeCase;
        byte[] beforeCaseVersion;
        await using (var db = sql.CreateDbContext())
        {
            activeCase = (await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
                .SingleAsync(l => l.ReportId == reportId && l.EndedAt == null)).CaseId;
            beforeCaseVersion = await db.IncidentCases.Where(c => c.Id == activeCase)
                .Select(c => EF.Property<byte[]>(c, "RowVersion")).SingleAsync();
        }
        var failedKey = Guid.NewGuid().ToString();
        var precommit = new CommandCommitFailure(failedKey, false);
        await using (var db = ModuleFaultContext(precommit))
            await Assert.ThrowsAsync<RetryLimitExceededException>(() => LifecycleService(db).SupplementAsync(reporter.Id,
                UserRoleCode.Reporter, reportId, faultRequest, failedKey, first.Headers.ETag!.Tag!.Trim('"'), null, default));
        precommit.Failures.Should().Be(3);
        await using (var db = sql.CreateDbContext())
        {
            (await db.ReportSupplements.CountAsync(s => s.ReportId == reportId)).Should().Be(1);
            (await db.Reports.Where(r => r.Id == reportId).Select(r => EF.Property<byte[]>(r, "RowVersion")).SingleAsync())
                .Should().Equal(Convert.FromBase64String(first.Headers.ETag!.Tag!.Trim('"')));
            (await db.IncidentCases.Where(c => c.Id == activeCase).Select(c => EF.Property<byte[]>(c, "RowVersion")).SingleAsync())
                .Should().Equal(beforeCaseVersion);
            (await db.AuditLogs.CountAsync(a => a.EntityId == reportId && a.EventType == "report_supplemented")).Should().Be(1);
            (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == reporter.Id && r.IdempotencyKey == failedKey)).Should().Be(0);
        }
        var recoveredKey = Guid.NewGuid().ToString();
        var postcommit = new CommandCommitFailure(recoveredKey, true);
        RoadGuardSystem.Services.Reports.ReporterLifecycleResult recovered;
        await using (var db = ModuleFaultContext(postcommit))
            recovered = await LifecycleService(db).SupplementAsync(reporter.Id, UserRoleCode.Reporter, reportId, faultRequest,
                recoveredKey, first.Headers.ETag!.Tag!.Trim('"'), null, default);
        recovered.Status.Should().Be(200); postcommit.Failures.Should().Be(1);
        await using (var db = sql.CreateDbContext())
        {
            var replayed = await LifecycleService(db).SupplementAsync(reporter.Id, UserRoleCode.Reporter, reportId, faultRequest,
                recoveredKey, first.Headers.ETag!.Tag!.Trim('"'), null, default);
            JsonSerializer.Serialize(replayed.Report).Should().Be(JsonSerializer.Serialize(recovered.Report));
            (await db.ReportSupplements.CountAsync(s => s.ReportId == reportId)).Should().Be(2);
            (await db.AuditLogs.CountAsync(a => a.EntityId == reportId && a.EventType == "report_supplemented")).Should().Be(2);
            (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == reporter.Id && r.IdempotencyKey == recoveredKey)).Should().Be(1);
        }
        var stale = await SendAsync(client, HttpMethod.Post, path, payload, Guid.NewGuid().ToString(), detail.Headers.ETag!.ToString());
        stale.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        var secondFile = await UploadVerifiedAsync(client, factory);
        var secondReport = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", new { description = "Second report",
            evidence = new[] { new { fileId = secondFile.FileId, fileVersion = secondFile.Version, locationSource = "UNKNOWN" } } }, Guid.NewGuid().ToString());
        secondReport.StatusCode.Should().Be(HttpStatusCode.Created);
        var secondReportId = (await secondReport.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var firstPage = await client.GetFromJsonAsync<JsonElement>("/api/v1/reports?pageSize=1");
        firstPage.GetProperty("items").GetArrayLength().Should().Be(1);
        var cursor = firstPage.GetProperty("nextCursor").GetString();
        cursor.Should().NotBeNullOrWhiteSpace();
        var nextPage = await client.GetFromJsonAsync<JsonElement>($"/api/v1/reports?pageSize=1&cursor={Uri.EscapeDataString(cursor!)}");
        nextPage.GetProperty("items").GetArrayLength().Should().Be(1);
        new[] { firstPage.GetProperty("items")[0].GetProperty("id").GetGuid(), nextPage.GetProperty("items")[0].GetProperty("id").GetGuid() }
            .Should().BeEquivalentTo(new[] { reportId, secondReportId });
        nextPage.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
        await using (var db = sql.CreateDbContext())
        {
            var link = await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>().SingleAsync(l => l.ReportId == reportId && l.EndedAt == null);
            (await db.ReportSupplements.CountAsync(s => s.ReportId == reportId)).Should().Be(2);
            (await db.IncidentCases.CountAsync(c => c.Id == link.CaseId)).Should().Be(1);
            (await db.AuditLogs.CountAsync(a => a.EntityId == reportId && a.EventType == "report_supplemented")).Should().Be(2);
        }
        await LoginAsync(client, other.UserName!);
        (await client.GetAsync($"/api/v1/reports?pageSize=1&cursor={Uri.EscapeDataString(cursor!)}")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetAsync($"/api/v1/reports/{reportId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/v1/reports/{reportId}/evidence/{evidenceId}/download")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await SendAsync(client, HttpMethod.Post, path, payload, key, detail.Headers.ETag!.ToString())).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [Trait("Package", "HUY-01")]
    [InlineData("{}", "evidence[0].location.latitude")]
    [InlineData("{\"latitude\":10}", "evidence[0].location.longitude")]
    [InlineData("{\"longitude\":106}", "evidence[0].location.latitude")]
    public async Task CreateReport_LocationMissingCoordinates_ReturnsFieldValidationProblem(string location, string field)
    {
        var reporter = await sql.CreateUserAsync($"reporter-location-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.AddHuy01ReporterPersistence();
            services.AddHuy01ReporterServices();
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);

        var json = """
            {
              "description":"missing coordinate",
              "evidence":[{
                "fileId":"11111111-1111-1111-1111-111111111111",
                "fileVersion":"version",
                "locationSource":"CAPTURE",
                "location":LOCATION_VALUE
              }]
            }
            """.Replace("LOCATION_VALUE", location, StringComparison.Ordinal);
        var response = await SendRawAsync(client, json, "location-key");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("validation_error");
        problem.GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue();
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task CreateReport_ExplicitZeroCoordinates_ReplaysWithNormalizedKeyAndExactOutcome()
    {
        var reporter = await sql.CreateUserAsync($"reporter-key-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>();
            services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
            services.AddHuy01ReporterPersistence();
            services.AddHuy01ReporterServices();
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);
        var file = await UploadVerifiedAsync(client, factory);
        var payload = new
        {
            description = "Zero coordinate report",
            evidence = new[] { new { fileId = file.FileId, fileVersion = file.Version, locationSource = "CAPTURE", location = new { latitude = 0, longitude = 0 } } }
        };

        var first = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", payload, "  normalized-key  ");
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = await first.Content.ReadAsStringAsync();
        var firstLocation = first.Headers.Location;
        var firstEtag = first.Headers.ETag;

        var replay = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", payload, "normalized-key");
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await replay.Content.ReadAsStringAsync()).Should().Be(firstBody);
        replay.Headers.Location.Should().Be(firstLocation);
        replay.Headers.ETag.Should().Be(firstEtag);
    }

    [Theory]
    [Trait("Package", "HUY-01")]
    [InlineData(" \t ")]
    [InlineData("k\u00e9y")]
    public async Task CreateReport_InvalidIdempotencyKey_ReturnsHeaderFieldValidationProblem(string key)
    {
        var reporter = await sql.CreateUserAsync($"reporter-invalid-key-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.AddHuy01ReporterPersistence();
            services.AddHuy01ReporterServices();
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);

        var response = await SendRawAsync(client, """
            {"description":"invalid key","evidence":[{"fileId":"11111111-1111-1111-1111-111111111111","fileVersion":"version","locationSource":"UNKNOWN"}]}
            """, key);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("validation_error");
        problem.GetProperty("errors").TryGetProperty("Idempotency-Key", out _).Should().BeTrue();
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task CreateReport_WithVerifiedOwnedEvidence_CreatesOneIntakeCase_AndReplaysExactly()
    {
        var reporter = await sql.CreateUserAsync($"reporter-report-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>();
            services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
            services.AddHuy01ReporterPersistence();
            services.AddHuy01ReporterServices();
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await LoginAsync(client, reporter.UserName!);
        var file = await UploadVerifiedAsync(client, factory);
        var key = Guid.NewGuid().ToString();
        var request = new
        {
            description = "Pothole beside the lane",
            evidence = new[]
            {
                new { fileId = file.FileId, fileVersion = file.Version, locationSource = "UNKNOWN" }
            }
        };

        var first = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", request, key);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var reportId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var afterFirst = await CountIntakeRowsAsync(sql, reporter.Id, reportId, key);
        afterFirst.Should().Be(new ReporterIntakeRowCounts(1, 1, 1, 1, 1, 1, 1, 1, 1, 1));

        var replay = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", request, key);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await CountIntakeRowsAsync(sql, reporter.Id, reportId, key)).Should().Be(afterFirst);

        var conflictingReplay = await SendAsync(client, HttpMethod.Post, "/api/v1/reports", new
        {
            description = "Different request payload",
            evidence = request.evidence
        }, key);
        conflictingReplay.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CountIntakeRowsAsync(sql, reporter.Id, reportId, key)).Should().Be(afterFirst);
    }

    private static async Task<ReporterIntakeRowCounts> CountIntakeRowsAsync(AuthenticationSqlServerFixture sql, Guid actorUserId,
        Guid reportId, string idempotencyKey)
    {
        await using var db = sql.CreateDbContext();
        var activeLink = await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
            .SingleAsync(link => link.ReportId == reportId && link.EndedAt == null);
        var actorReports = db.Reports.Where(report => report.ReporterUserId == actorUserId).Select(report => report.Id);
        var actorActiveLinks = db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
            .Where(link => actorReports.Contains(link.ReportId) && link.EndedAt == null);
        var actorIntakeCases = actorActiveLinks.Select(link => link.CaseId);

        return new ReporterIntakeRowCounts(
            await db.Reports.CountAsync(report => report.Id == reportId && report.ReporterUserId == actorUserId),
            await db.IncidentCases.CountAsync(@case => @case.Id == activeLink.CaseId),
            await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
                .CountAsync(link => link.CaseId == activeLink.CaseId && link.ReportId == reportId && link.EndedAt == null),
            await db.AuditLogs.CountAsync(audit => audit.ActorUserId == actorUserId && audit.EventType == "report_received" &&
                audit.EntityType == "Report" && audit.EntityId == reportId),
            await db.IdempotencyRecords.CountAsync(record => record.ActorUserId == actorUserId && record.ProjectId == null &&
                record.Operation == "huy01.report.create.v1" && record.IdempotencyKey == idempotencyKey && record.OperationId == reportId),
            await actorReports.CountAsync(),
            await db.IncidentCases.CountAsync(@case => actorIntakeCases.Contains(@case.Id)),
            await actorActiveLinks.CountAsync(),
            await db.AuditLogs.CountAsync(audit => audit.ActorUserId == actorUserId && audit.EventType == "report_received" &&
                audit.EntityType == "Report" && audit.Source == "huy01.reporter-intake"),
            await db.IdempotencyRecords.CountAsync(record => record.ActorUserId == actorUserId && record.ProjectId == null &&
                record.Operation == "huy01.report.create.v1"));
    }

    private sealed record ReporterIntakeRowCounts(
        int TargetReports, int TargetIntakeCases, int TargetActiveLinks, int TargetAudits, int TargetReceipts,
        int ActorReports, int ActorIntakeCases, int ActorActiveLinks, int ActorAudits, int ActorReceipts);

    private static async Task<(Guid FileId, string Version)> UploadVerifiedAsync(HttpClient client, AuthenticationWebApplicationFactory factory)
    {
        var hash = Convert.ToHexString(SHA256.HashData([1, 2, 3, 4])).ToLowerInvariant();
        var created = await SendAsync(client, HttpMethod.Post, "/api/v1/reporter-evidence/uploads", new
        {
            fileName = "report.jpg", mediaType = "image/jpeg", sizeBytes = 4L, checksumSha256 = hash
        }, Guid.NewGuid().ToString());
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var session = await created.Content.ReadFromJsonAsync<JsonElement>();
        var uploadId = session.GetProperty("id").GetGuid();
        var fileId = session.GetProperty("fileId").GetGuid();

        var parts = await SendAsync(client, HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{uploadId}/part-urls", new { partNumbers = new[] { 1 } }, Guid.NewGuid().ToString());
        parts.StatusCode.Should().Be(HttpStatusCode.OK);
        var current = await client.GetAsync($"/api/v1/reporter-evidence/uploads/{uploadId}");
        var complete = await SendAsync(client, HttpMethod.Post, $"/api/v1/reporter-evidence/uploads/{uploadId}/complete", new
        {
            checksumSha256 = hash, parts = new[] { new { partNumber = 1, eTag = "part" } }
        }, Guid.NewGuid().ToString(), current.Headers.ETag!.ToString());
        complete.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Files.IUploadService>().ProcessOneVerificationAsync();
        var metadata = await client.GetAsync($"/api/v1/reporter-evidence/files/{fileId}");
        metadata.StatusCode.Should().Be(HttpStatusCode.OK);
        return (fileId, metadata.Headers.ETag!.Tag!.Trim('"'));
    }

    private static async Task LoginAsync(HttpClient client, string userName)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(userName), password = "Current1!"
        });
        response.EnsureSuccessStatusCode();
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object body, string idempotencyKey, string? etag = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        if (etag is not null) request.Headers.TryAddWithoutValidation("If-Match", etag);
        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> SendRawAsync(HttpClient client, string json, string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reports")
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request);
    }

    private sealed class ExportArtifactStorage : IAnh02ArtifactStore
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (byte[] Bytes, Anh02ArtifactMetadata Metadata)> _objects = new();

        public async Task<Anh02ArtifactMetadata> WriteAsync(string key, Stream content, long? sizeBytes,
            string mediaType, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            if (sizeBytes is not null && sizeBytes != bytes.LongLength) throw new IOException("Artifact size mismatch.");
            var metadata = new Anh02ArtifactMetadata(key, bytes.LongLength,
                Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), mediaType);
            _objects.TryAdd(key, (bytes, metadata));
            return _objects[key].Metadata;
        }

        public Task<Anh02ArtifactRead> OpenReadAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!_objects.TryGetValue(key, out var value)) throw new FileNotFoundException(key);
            return Task.FromResult(new Anh02ArtifactRead(new MemoryStream(value.Bytes, writable: false), value.Metadata));
        }
    }

    private sealed class VerifiedPhotoStorage : IUploadObjectStorage
    {
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default)
            => Task.FromResult("upload");
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PresignedUploadPart>>(partNumbers.Select(number => new PresignedUploadPart(number, "https://example.test/part", expiresAt)).ToArray());
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default)
            => Task.FromResult(new UploadObjectVerification(4, Convert.ToHexString(SHA256.HashData([1, 2, 3, 4])).ToLowerInvariant(), "image/jpeg"));
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream([1, 2, 3, 4]));
    }
}
