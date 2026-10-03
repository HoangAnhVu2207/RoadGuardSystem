using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Data.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
    public async Task ProductionRoot_BindsReporterIntakeOnceWithoutActivatingOtherModuleRoutes()
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
        Assert.DoesNotContain(descriptors, d => d.ServiceType == typeof(RoadGuardSystem.Services.Cases.ICaseWorkflowService));
        Assert.DoesNotContain(descriptors, d => d.ServiceType == typeof(RoadGuardSystem.Services.Defects.ICandidateDecisionService));
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
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/v1/reports")).StatusCode);
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
    public async Task NonIntakeModuleRoutes_WithoutProductionComposition_ReturnDependencyUnavailable()
    {
        var reporter = await sql.CreateUserAsync($"unbound-r-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var pm = await sql.CreateUserAsync($"unbound-p-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await LoginAsync(client, reporter.UserName!);
        var reports = await client.GetAsync("/api/v1/reports");
        reports.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await reports.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("dependency_unavailable");
        await LoginAsync(client, pm.UserName!);
        (await client.GetAsync("/api/v1/cases")).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/candidate-decisions/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task Candidate_ReportRejectCorrectionReplay_UsesRealGeometrySourceAndHead()
    {
        var reporter = await sql.CreateUserAsync($"candidate-r-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var supervisor = await sql.CreateUserAsync($"candidate-s-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var pm = await sql.CreateUserAsync($"candidate-p-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var project = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(RoadGuardSystem.BusinessObjects.Projects.Project.Create(project, $"CD-{Guid.NewGuid():N}", "Candidate project", null, 32648, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(RoadGuardSystem.BusinessObjects.Projects.ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, pm.Id, new DateOnly(2026, 1, 1)));
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
            services.AddHuy01ReporterPersistence(); services.AddHuy01ReporterServices();
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
        await using (var db = sql.CreateDbContext()) { await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [ProjectMembers] SET [Status]=2 WHERE [ProjectId]={project} AND [UserId]={pm.Id}"); }
        (await SendAsync(client, HttpMethod.Post, path, payload, key)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
            services.AddHuy01ReporterPersistence(); services.AddHuy01ReporterServices();
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
    public async Task OwnReport_ReadSupplementReplayPrivacyAndDownload_UseRealSqlGraph()
    {
        var reporter = await sql.CreateUserAsync($"reporter-lifecycle-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var other = await sql.CreateUserAsync($"reporter-other-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IUploadObjectStorage>();
            services.AddSingleton<IUploadObjectStorage>(new VerifiedPhotoStorage());
            services.AddHuy01ReporterPersistence(); services.AddHuy01ReporterServices();
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
