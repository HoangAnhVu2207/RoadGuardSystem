using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Cases;
using RoadGuardSystem.Repositories.Implementations.Reports;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Cases;
using RoadGuardSystem.Services.Implementations.Cases;
using RoadGuardSystem.Services.Implementations.Reports;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Reports;
using Xunit;

namespace RoadGuardSystem.ApiTests.Cases;

[Trait("Package", "HUY-01")]
public sealed class Huy01CaseSqlTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    [Theory]
    [InlineData("membership", 403)]
    [InlineData("inactive", 403)]
    [InlineData("must-change", 403)]
    [InlineData("pending-evidence", 409)]
    [InlineData("stale-evidence", 412)]
    public async Task ConclusionReplay_AuthorityChangedAfterRealPreflight_DeniesReceiptWithoutWrites(string mutation, int expectedStatus)
    {
        var setup = await SetupAsync(); await using var createDb = sql.CreateDbContext();
        var read = await Service(createDb).ReadAsync(setup.Pm, UserRoleCode.ProjectManager, setup.CaseA, default);
        var command = new CaseCommand(setup.CaseA, read.Case!.Version, "conclude", "Current facts race", Outcome: CaseConclusionOutcome.NoDefect, DefectIds: [], EvidenceIds: [setup.EvidenceA]);
        var key = Guid.NewGuid().ToString();
        Assert.Equal(200, (await Service(createDb).CommandAsync(setup.Pm, UserRoleCode.ProjectManager, command, key, null, default)).Status);
        var before = await SnapshotAsync(setup);
        await using var replayDb = sql.CreateDbContext();
        async Task Mutate()
        {
            await using var writeDb = sql.CreateDbContext();
            if (mutation == "membership") await writeDb.Database.ExecuteSqlInterpolatedAsync($"UPDATE [ProjectMembers] SET [Status]={(byte)ProjectMemberStatus.Ended} WHERE [ProjectId]={setup.Project} AND [UserId]={setup.Pm}");
            else if (mutation == "inactive") await writeDb.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Users] SET [Status]={(byte)UserStatus.Suspended} WHERE [Id]={setup.Pm}");
            else if (mutation == "must-change") await writeDb.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Users] SET [MustChangePassword]={true} WHERE [Id]={setup.Pm}");
            else
            {
                var file = await writeDb.Reports.Where(r => r.Id == setup.ReportA).SelectMany(r => r.OriginalEvidence).Where(e => e.Id == setup.EvidenceA).Select(e => e.FileId).SingleAsync();
                if (mutation == "stale-evidence") await writeDb.Database.ExecuteSqlInterpolatedAsync($"UPDATE [UploadSessions] SET [StorageUploadId]={Guid.NewGuid().ToString()} WHERE [FileId]={file}");
                else await writeDb.Database.ExecuteSqlInterpolatedAsync($"UPDATE [UploadSessions] SET [Status]={(byte)UploadSessionStatus.Pending} WHERE [FileId]={file}");
            }
        }
        var repository = new MutatingReadRepository(new CaseWorkflowRepository(replayDb), Mutate);
        var service = new CaseWorkflowService(repository, new ProjectScopeGuard(new ProjectMembershipReadModel(replayDb), TimeProvider.System), Producer(replayDb), new IdempotencyOperationService(replayDb));
        var denied = await service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager, command, key, null, default);
        Assert.Equal(expectedStatus, denied.Status); Assert.Null(denied.Write); Assert.Equal(0, repository.ApplyCalls);
        Assert.Equal(1, repository.GuardCalls);
        Assert.Equal(before, await SnapshotAsync(setup));
    }

    [Theory]
    [InlineData(CaseVerificationMethod.Field)]
    [InlineData(CaseVerificationMethod.Drone)]
    public async Task Conclusion_WithoutFieldDroneCompletionProvenance_IsNotReady(CaseVerificationMethod method)
    {
        var setup = await SetupAsync(); await using var db = sql.CreateDbContext(); var service = Service(db);
        var before = await service.ReadAsync(setup.Pm, UserRoleCode.ProjectManager, setup.CaseA, default);
        var selected = await service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager, new(setup.CaseA, before.Case!.Version,
            "triage", "Select actual verification method", ProjectId: setup.Project, Method: method), Guid.NewGuid().ToString(), null, default);
        var snapshot = await SnapshotAsync(setup);
        var result = await service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager, new(setup.CaseA, selected.Write!.Case.Version,
            "conclude", "No fabricated result", Outcome: CaseConclusionOutcome.NoDefect, DefectIds: [], EvidenceIds: [setup.EvidenceA]), Guid.NewGuid().ToString(), null, default);
        Assert.Equal(409, result.Status); Assert.Equal("source_not_ready", result.Code); Assert.Equal(snapshot, await SnapshotAsync(setup));
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConcurrentParentVersion_SameKeyReplays_DifferentKeysHaveOneWinner(bool sameKey)
    {
        var setup = await SetupAsync();
        await using var readDb = sql.CreateDbContext(); var read = await Service(readDb).ReadAsync(setup.Pm, UserRoleCode.ProjectManager, setup.CaseA, default);
        var command = new CaseCommand(setup.CaseA, read.Case!.Version, "triage", "PM method choice", ProjectId: setup.Project, Method: CaseVerificationMethod.Drone);
        var key = Guid.NewGuid().ToString(); var barrier = new ReadBarrier();
        await using var leftDb = sql.CreateDbContext(); await using var rightDb = sql.CreateDbContext();
        CaseWorkflowService Wrapped(RoadGuardDbContext db) => new(new ReadBarrierRepository(new CaseWorkflowRepository(db), barrier),
            new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System), Producer(db), new IdempotencyOperationService(db));
        var results = await Task.WhenAll(Wrapped(leftDb).CommandAsync(setup.Pm, UserRoleCode.ProjectManager, command, key, null, default),
            Wrapped(rightDb).CommandAsync(setup.Pm, UserRoleCode.ProjectManager, command, sameKey ? key : Guid.NewGuid().ToString(), null, default));
        Assert.Equal(sameKey ? 2 : 1, results.Count(r => r.Status == 200));
        if (sameKey) Assert.Equal(JsonSerializer.Serialize(results[0].Write), JsonSerializer.Serialize(results[1].Write));
        else Assert.Single(results.Where(r => r.Status == 412));
        await using var check = sql.CreateDbContext();
        Assert.Equal(1, await check.AuditLogs.CountAsync(a => a.EntityId == setup.CaseA && a.ActorUserId == setup.Pm && a.EventType == "case_triage"));
        Assert.Equal(1, await check.IdempotencyRecords.CountAsync(r => r.ActorUserId == setup.Pm && r.Operation == "huy01.case.triage.v1"));
    }

    [Theory]
    [InlineData("link", false)]
    [InlineData("link", true)]
    [InlineData("conclude", false)]
    [InlineData("conclude", true)]
    [InlineData("publish", false)]
    [InlineData("publish", true)]
    [InlineData("split", false)]
    [InlineData("split", true)]
    public async Task CaseMutation_PrecommitRollsBack_PostcommitRecoversExactly(string action, bool afterCommit)
    {
        var setup = await SetupAsync(); await using var readDb = sql.CreateDbContext(); var service = Service(readDb);
        var target = await service.ReadAsync(setup.Pm, UserRoleCode.ProjectManager, setup.CaseA, default);
        var source = await service.ReadAsync(setup.Pm, UserRoleCode.ProjectManager, setup.CaseB, default);
        if (action == "split")
        {
            var linked = await service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager, new(setup.CaseA, target.Case!.Version,
                "link", "Prepare split", ReportIds: [setup.ReportB], SourceCaseVersions: new Dictionary<Guid, string> { [setup.CaseB] = source.Case!.Version }),
                Guid.NewGuid().ToString(), null, default);
            target = new(200, Case: linked.Write!.Case);
        }
        if (action == "publish")
        {
            var conclusion = await service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager, new(setup.CaseA, target.Case!.Version, "conclude", "No defect",
                Outcome: CaseConclusionOutcome.NoDefect, DefectIds: [], EvidenceIds: [setup.EvidenceA]), Guid.NewGuid().ToString(), null, default);
            target = new(200, Case: conclusion.Write!.Case);
        }
        var command = action == "link" ? new CaseCommand(setup.CaseA, target.Case!.Version, action, "Fault probe link", ReportIds: [setup.ReportB],
            SourceCaseVersions: new Dictionary<Guid, string> { [setup.CaseB] = source.Case!.Version }) : action == "conclude"
            ? new(setup.CaseA, target.Case!.Version, action, "Fault probe conclusion", Outcome: CaseConclusionOutcome.NoDefect, DefectIds: [], EvidenceIds: [setup.EvidenceA])
            : action == "split" ? new(setup.CaseA, target.Case!.Version, action, "Fault probe split", ReportIds: [setup.ReportB])
            : new(setup.CaseA, target.Case!.Version, action, "Fault probe publication", ReportIds: [setup.ReportA], DefectIds: [], EvidenceIds: [setup.EvidenceA]);
        var before = await SnapshotAsync(setup); var key = Guid.NewGuid().ToString(); var probe = new ScopedCommitFailure(key, afterCommit);
        await using var db = new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(sql.ConnectionString, options => options.UseNetTopologySuite())
            .ReplaceService<Microsoft.EntityFrameworkCore.Storage.IExecutionStrategyFactory, CommitFailureExecutionStrategyFactory>().AddInterceptors(probe).Options);
        if (!afterCommit)
        {
            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException>(() => Service(db).CommandAsync(setup.Pm, UserRoleCode.ProjectManager, command, key, null, default));
            Assert.Equal(3, probe.Failures); Assert.Equal(before, await SnapshotAsync(setup));
        }
        else
        {
            var recovered = await Service(db).CommandAsync(setup.Pm, UserRoleCode.ProjectManager, command, key, null, default);
            Assert.Equal(action is "publish" or "split" ? 201 : 200, recovered.Status); Assert.Equal(1, probe.Failures);
            await using var replayDb = sql.CreateDbContext(); var replay = await Service(replayDb).CommandAsync(setup.Pm, UserRoleCode.ProjectManager, command, key, null, default);
            Assert.Equal(JsonSerializer.Serialize(recovered.Write), JsonSerializer.Serialize(replay.Write));
            await using var check = sql.CreateDbContext();
            Assert.Equal(1, await check.IdempotencyRecords.CountAsync(r => r.ActorUserId == setup.Pm && r.IdempotencyKey == key && r.Operation == "huy01.case." + action + ".v1"));
        }
    }

    [Fact]
    public async Task LinkSplit_KeepSnapshotRelationalHistoryAndOneActiveLink_InOneTransaction()
    {
        var setup = await SetupAsync();
        await using var db = sql.CreateDbContext(); var service = Service(db);
        var target = await service.ReadAsync(setup.Pm, UserRoleCode.ProjectManager, setup.CaseA, default);
        var source = await service.ReadAsync(setup.Pm, UserRoleCode.ProjectManager, setup.CaseB, default);
        var command = new CaseCommand(setup.CaseA, target.Case!.Version, "link", "Same project", ReportIds: [setup.ReportB],
            SourceCaseVersions: new Dictionary<Guid, string> { [setup.CaseB] = source.Case!.Version });
        var key = Guid.NewGuid().ToString();
        var linked = await service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager, command, key, null, default);
        Assert.Equal(200, linked.Status);
        Assert.Equal(2, linked.Write!.Case.ReportIds.Count);
        var replay = await service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager, command, key, null, default);
        Assert.Equal(JsonSerializer.Serialize(linked.Write), JsonSerializer.Serialize(replay.Write));
        var stale = await service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager, command, Guid.NewGuid().ToString(), null, default);
        Assert.Equal(412, stale.Status);
        var split = await service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager,
            new(setup.CaseA, linked.Write.Case.Version, "split", "Separate report", ReportIds: [setup.ReportB]), Guid.NewGuid().ToString(), null, default);
        Assert.Equal(201, split.Status);
        Assert.Equal(new[] { setup.ReportB }, split.Write!.Case.ReportIds);
        await using var check = sql.CreateDbContext();
        var active = await check.Set<HuyCaseReportLink>().Where(l => (l.ReportId == setup.ReportA || l.ReportId == setup.ReportB) && l.EndedAt == null).ToArrayAsync();
        Assert.Equal(2, active.Length);
        foreach (var group in active.GroupBy(l => l.CaseId))
        {
            var incident = await check.IncidentCases.SingleAsync(c => c.Id == group.Key);
            Assert.Equal(group.Select(l => l.ReportId).Order(), incident.ActiveReportIds.Order());
        }
        Assert.Equal(2, await check.Set<CaseReportLinkHistory>().CountAsync(h => h.FromCaseId == setup.CaseA || h.ToCaseId == setup.CaseA));
        Assert.Equal(2, await check.Set<HuyLinkHistoryReport>().CountAsync(h => h.ReportId == setup.ReportB));
        Assert.Equal(2, await check.Reports.CountAsync(r => r.Id == setup.ReportA || r.Id == setup.ReportB));
    }

    [Theory]
    [InlineData("stale-source")]
    [InlineData("stale-target")]
    [InlineData("after-close")]
    public async Task Link_InvalidVersionOrFailureAfterClosingLinks_RollsBackAllCasesHistoryAndReceipt(string failure)
    {
        var setup = await SetupAsync(); var before = await SnapshotAsync(setup);
        var probe = new FailReplacementLinkInterceptor();
        await using var db = failure == "after-close" ? new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(sql.ConnectionString, options => options.UseNetTopologySuite()).AddInterceptors(probe).Options) : sql.CreateDbContext();
        var service = Service(db);
        var target = await service.ReadAsync(setup.Pm, UserRoleCode.ProjectManager, setup.CaseA, default);
        var source = await service.ReadAsync(setup.Pm, UserRoleCode.ProjectManager, setup.CaseB, default);
        var command = new CaseCommand(setup.CaseA, failure == "stale-target" ? Convert.ToBase64String(new byte[8]) : target.Case!.Version,
            "link", "Atomic link", ReportIds: [setup.ReportB], SourceCaseVersions: new Dictionary<Guid, string>
            { [setup.CaseB] = failure == "stale-source" ? Convert.ToBase64String(new byte[8]) : source.Case!.Version });
        if (failure == "after-close")
        {
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager, command, Guid.NewGuid().ToString(), null, default));
            Assert.IsType<ReplacementLinkFailure>(exception.InnerException);
        }
        else Assert.Equal(412, (await service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager, command, Guid.NewGuid().ToString(), null, default)).Status);
        Assert.Equal(before, await SnapshotAsync(setup));
        if (failure == "after-close") Assert.Equal(1, probe.Failures);
    }

    [Fact]
    public async Task Publication_TwoRecipientsCannotReceiveEachOthersPrivateEvidence_AndSupplementKeepsSnapshot()
    {
        var setup = await SetupAsync();
        await using var db = sql.CreateDbContext(); var service = Service(db);
        var target = await service.ReadAsync(setup.Pm, UserRoleCode.ProjectManager, setup.CaseA, default);
        var source = await service.ReadAsync(setup.Pm, UserRoleCode.ProjectManager, setup.CaseB, default);
        var linked = await service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager, new(setup.CaseA, target.Case!.Version, "link", "Same project", ReportIds: [setup.ReportB],
            SourceCaseVersions: new Dictionary<Guid, string> { [setup.CaseB] = source.Case!.Version }), Guid.NewGuid().ToString(), null, default);
        var concluded = await service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager, new(setup.CaseA, linked.Write!.Case.Version, "conclude", "No defect",
            Outcome: CaseConclusionOutcome.NoDefect, DefectIds: [], EvidenceIds: [setup.EvidenceA]), Guid.NewGuid().ToString(), null, default);
        var before = await SnapshotAsync(setup);
        var denied = await service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager, new(setup.CaseA, concluded.Write!.Case.Version, "publish", "Private photo",
            ReportIds: [setup.ReportA, setup.ReportB], DefectIds: [], EvidenceIds: [setup.EvidenceA]), Guid.NewGuid().ToString(), null, default);
        Assert.Equal(409, denied.Status); Assert.Equal(before, await SnapshotAsync(setup));
        var published = await service.CommandAsync(setup.Pm, UserRoleCode.ProjectManager, new(setup.CaseA, concluded.Write.Case.Version, "publish", "Own recipient only",
            ReportIds: [setup.ReportA], DefectIds: [], EvidenceIds: [setup.EvidenceA]), Guid.NewGuid().ToString(), null, default);
        Assert.Equal(201, published.Status);
        await using var check = sql.CreateDbContext();
        var snapshot = await check.Set<CasePublication>().SingleAsync(p => p.Id == published.Write!.Publication!.Id);
        var recipient = await check.Set<HuyPublicationRecipient>().SingleAsync(p => p.PublicationId == snapshot.Id);
        var evidence = await check.Set<HuyPublicationEvidence>().SingleAsync(p => p.PublicationId == snapshot.Id);
        Assert.Equal(snapshot.RecipientReportIds.Single(), recipient.ReportId);
        Assert.Equal(snapshot.EvidenceIds.Single(), evidence.EvidenceId);
        Assert.Equal(setup.ReportA, evidence.RecipientReportId);
        var other = await new AnhHuyProducerService(new AnhHuyFactsRepository(check), null!, null!).ResolvePublicationEvidenceAsync(setup.ReporterB,
            UserRoleCode.Reporter, snapshot.Id, setup.ReportB, setup.EvidenceA);
        Assert.Equal(AnhHuyProducerStatus.NotFound, other.Status);
    }

    private CaseWorkflowService Service(RoadGuardDbContext db) => new(new CaseWorkflowRepository(db),
        new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System), Producer(db), new IdempotencyOperationService(db));
    private static AnhHuyProducerService Producer(RoadGuardDbContext db) => new(new AnhHuyFactsRepository(db), null!, null!);

    private async Task<Setup> SetupAsync()
    {
        var reporterA = await sql.CreateUserAsync($"case-a-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var reporterB = await sql.CreateUserAsync($"case-b-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var pm = await sql.CreateUserAsync($"case-p-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await sql.CreateUserAsync($"case-s-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var project = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(Project.Create(project, $"CA-{Guid.NewGuid():N}", "Case SQL test", null, 32648, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, pm.Id, new DateOnly(2026, 1, 1)));
            await db.SaveChangesAsync();
        }
        var a = await IntakeAsync(reporterA.Id); var b = await IntakeAsync(reporterB.Id);
        await using (var db = sql.CreateDbContext())
        {
            var service = Service(db);
            foreach (var id in new[] { a.CaseId, b.CaseId })
            {
                var read = await service.ReadAsync(supervisor.Id, UserRoleCode.Supervisor, id, default);
                var triage = await service.CommandAsync(supervisor.Id, UserRoleCode.Supervisor, new(id, read.Case!.Version, "triage", "SQL triage", ProjectId: project, Method: CaseVerificationMethod.ExistingEvidence), Guid.NewGuid().ToString(), null, default);
                Assert.Equal(200, triage.Status);
            }
        }
        return new(pm.Id, reporterA.Id, reporterB.Id, a.ReportId, b.ReportId, a.CaseId, b.CaseId, a.EvidenceId, b.EvidenceId, project);
    }

    private async Task<(Guid ReportId, Guid CaseId, Guid EvidenceId)> IntakeAsync(Guid reporter)
    {
        await using var db = sql.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        var file = StoredFile.Create(Guid.NewGuid(), $"private/{Guid.NewGuid():N}", "photo.jpg", "image/jpeg", 4, new string('a', 64), reporter, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, reporter, file.StorageUri, "REPORT_PHOTO", "image/jpeg", 4, new string('a', 64), 8388608, now.AddHours(24));
        db.Files.Add(file); db.FileScopes.Add(FileScope.CreatePrivate(Guid.NewGuid(), file.Id, reporter, now)); db.UploadSessions.Add(upload); await db.SaveChangesAsync();
        upload.StartUploading("fixture", now); await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        var result = await new ReporterReportService(Producer(db), new ReporterReportRepository(db), new IdempotencyOperationService(db))
            .CreateAsync(reporter, UserRoleCode.Reporter, new("Case SQL intake", [new(file.Id, Convert.ToBase64String(upload.RowVersion), "UNKNOWN")]), Guid.NewGuid().ToString(), null);
        Assert.Equal(ReporterReportCommandStatus.Created, result.Status);
        var link = await db.Set<HuyCaseReportLink>().SingleAsync(l => l.ReportId == result.Report!.Id && l.EndedAt == null);
        return (result.Report!.Id, link.CaseId, result.Report.EvidenceIds.Single());
    }

    private async Task<string> SnapshotAsync(Setup setup)
    {
        await using var db = sql.CreateDbContext();
        var cases = await db.IncidentCases.Where(c => c.ProjectId == setup.Project).OrderBy(c => c.Id).ToArrayAsync();
        var ids = cases.Select(c => c.Id).ToArray();
        var heads = cases.Select(c => new { c.Id, c.Status, Reports = c.ActiveReportIds.Order().ToArray(), Revision = db.Entry(c).Property<long>("Revision").CurrentValue,
            Version = Convert.ToBase64String(db.Entry(c).Property<byte[]>("RowVersion").CurrentValue!) });
        var links = await db.Set<HuyCaseReportLink>().Where(l => ids.Contains(l.CaseId)).OrderBy(l => l.Id).Select(l => new { l.Id, l.CaseId, l.ReportId, l.EndedAt }).ToArrayAsync();
        var history = await db.Set<CaseReportLinkHistory>().Where(h => ids.Contains(h.FromCaseId) || ids.Contains(h.ToCaseId)).OrderBy(h => h.Id).Select(h => h.Id).ToArrayAsync();
        var audits = await db.AuditLogs.Where(a => ids.Contains(a.EntityId)).OrderBy(a => a.Id).Select(a => a.Id).ToArrayAsync();
        var receipts = await db.IdempotencyRecords.Where(r => r.ActorUserId == setup.Pm && r.Operation.StartsWith("huy01.case.")).OrderBy(r => r.Id).Select(r => r.Id).ToArrayAsync();
        var conclusions = await db.Set<CaseConclusion>().Where(c => ids.Contains(EF.Property<Guid>(c, "CaseId"))).OrderBy(c => c.Id).Select(c => c.Id).ToArrayAsync();
        var publications = await db.Set<CasePublication>().Where(p => ids.Contains(p.CaseId)).OrderBy(p => p.Id).Select(p => p.Id).ToArrayAsync();
        return JsonSerializer.Serialize(new { heads, links, history, audits, receipts, conclusions, publications });
    }
    private sealed record Setup(Guid Pm, Guid ReporterA, Guid ReporterB, Guid ReportA, Guid ReportB, Guid CaseA, Guid CaseB, Guid EvidenceA, Guid EvidenceB, Guid Project);

    private sealed class ScopedCommitFailure(string key, bool afterCommit) : DbTransactionInterceptor
    {
        public int Failures { get; private set; }
        private bool HasReceipt(DbContext? context) => context?.Set<RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord>().Local.Any(r => r.IdempotencyKey == key) == true;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!afterCommit && HasReceipt(eventData.Context)) { Failures++; throw new CommitFailureTransientException("Injected precommit failure after real case graph/receipt writes."); }
            return ValueTask.FromResult(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (afterCommit && Failures == 0 && HasReceipt(eventData.Context)) { Failures++; throw new CommitFailureTransientException("Injected case acknowledgement loss."); }
            return Task.CompletedTask;
        }
    }
    private sealed class ReadBarrier
    {
        private int _arrivals; private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Arrive(CancellationToken ct) { if (Interlocked.Increment(ref _arrivals) >= 2) _release.TrySetResult(); return _release.Task.WaitAsync(TimeSpan.FromSeconds(10), ct); }
    }
    private sealed class ReadBarrierRepository(ICaseWorkflowRepository inner, ReadBarrier barrier) : ICaseWorkflowRepository
    {
        public Task GuardCommandAsync(Guid actor, UserRoleCode role, CaseCommand command, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct) => inner.GuardCommandAsync(actor, role, command, projectAccess, ct);
        public Task GuardAsync(Guid actor, UserRoleCode role, IReadOnlyCollection<Guid> caseIds, Guid? requestedProject, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct) => inner.GuardAsync(actor, role, caseIds, requestedProject, projectAccess, ct);
        public async Task<RoadGuardSystem.DTOs.Cases.InternalCaseDto> ReadAsync(Guid actor, UserRoleCode role, Guid caseId, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct)
        { var value = await inner.ReadAsync(actor, role, caseId, projectAccess, ct); await barrier.Arrive(ct); return value; }
        public Task<RoadGuardSystem.DTOs.Cases.CasePageDto> ListAsync(Guid actor, UserRoleCode role, Guid? project, IncidentCaseStatus? status, int pageSize, string? cursor, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct) => inner.ListAsync(actor, role, project, status, pageSize, cursor, projectAccess, ct);
        public Task<CaseWriteResult> ApplyAsync(Guid actor, UserRoleCode role, CaseCommand command, Func<Guid, CancellationToken, Task<bool>> projectAccess, Func<CancellationToken, Task>? geometryCheck, Guid? correlation, CancellationToken ct) => inner.ApplyAsync(actor, role, command, projectAccess, geometryCheck, correlation, ct);
    }
    private sealed class MutatingReadRepository(ICaseWorkflowRepository inner, Func<Task> mutate) : ICaseWorkflowRepository
    {
        private int _remaining = 1;
        public int ApplyCalls { get; private set; }
        public int GuardCalls { get; private set; }
        public Task GuardAsync(Guid actor, UserRoleCode role, IReadOnlyCollection<Guid> caseIds, Guid? requestedProject, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct) => inner.GuardAsync(actor, role, caseIds, requestedProject, projectAccess, ct);
        public Task GuardCommandAsync(Guid actor, UserRoleCode role, CaseCommand command, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct)
        { GuardCalls++; return inner.GuardCommandAsync(actor, role, command, projectAccess, ct); }
        public async Task<RoadGuardSystem.DTOs.Cases.InternalCaseDto> ReadAsync(Guid actor, UserRoleCode role, Guid caseId, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct)
        { var result = await inner.ReadAsync(actor, role, caseId, projectAccess, ct); if (Interlocked.Exchange(ref _remaining, 0) == 1) await mutate(); return result; }
        public Task<RoadGuardSystem.DTOs.Cases.CasePageDto> ListAsync(Guid actor, UserRoleCode role, Guid? project, IncidentCaseStatus? status, int pageSize, string? cursor, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct) => inner.ListAsync(actor, role, project, status, pageSize, cursor, projectAccess, ct);
        public Task<CaseWriteResult> ApplyAsync(Guid actor, UserRoleCode role, CaseCommand command, Func<Guid, CancellationToken, Task<bool>> projectAccess, Func<CancellationToken, Task>? geometryCheck, Guid? correlation, CancellationToken ct)
        { ApplyCalls++; return inner.ApplyAsync(actor, role, command, projectAccess, geometryCheck, correlation, ct); }
    }
    private sealed class FailReplacementLinkInterceptor : DbCommandInterceptor
    {
        public int Failures { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO [CaseReportLinks]", StringComparison.Ordinal)) { Failures++; throw new ReplacementLinkFailure(); }
            return ValueTask.FromResult(result);
        }
    }
    private sealed class ReplacementLinkFailure : Exception;
}
