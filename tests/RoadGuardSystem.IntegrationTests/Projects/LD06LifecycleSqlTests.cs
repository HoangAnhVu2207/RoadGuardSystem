using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.IntegrationTests.Repairs;
using RoadGuardSystem.Repositories.Projects;
using Xunit;
using Xunit.Abstractions;

namespace RoadGuardSystem.IntegrationTests.Projects;

public sealed class LD06LifecycleSqlTests(IdentitySqlServerFixture sql, ITestOutputHelper output) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public Task GenuineNewReporterDefectAndSuccessfulPredecessorLinkCommitInOneCandidateReceipt()
        => new H4RepairExecutionSqlTests(sql, output).WithSuccessfulRepair(async (db, source, itemId, decisionId) =>
        {
            var project = await db.Projects.SingleAsync(x => x.Id == source.Project);
            db.Entry(project).Property(x => x.EngineeringUtmSrid).CurrentValue = 32648;
            await db.SaveChangesAsync();
            var receipts = new RoadGuardSystem.Repositories.Idempotency.IdempotencyOperationService(db);
            var geometry = new GeometryWorkflowPersistenceService(db, receipts, TimeProvider.System);
            async Task<GeometryWorkflowResult> Geometry(GeometryWorkflowCommand command) => await geometry.ExecuteAsync(command,
                (input, srid) => RoadGuardSystem.Services.Projects.GeometryEngine.Preview(input, srid),
                (id, line, origin, definition) => RoadGuardSystem.Services.Projects.GeometryEngine.Segments(id, line, origin, definition), default);
            var input = new RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryDraftInputFact("COORDINATES", 32648, 0,
                "TEST_ONLY recurrence source; no real CRS verification", [new(500000, 1200000), new(500020, 1200000)], null, null, null,
                [new(0, 20, 4)], 4, RoadCode: "LD06" + Guid.NewGuid().ToString("N"));
            var draftResult = await Geometry(new(source.Pm, source.Project, "draft-create", null, null, null, null, input,
                Guid.NewGuid().ToString(), null, UserRoleCode.ProjectManager));
            Assert.True(draftResult.Status == 201, $"Draft actual {draftResult.Status}/{draftResult.Code}");
            var draft = Assert.IsType<RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryDraftViewFact>(draftResult.Value);
            var confirmedResult = await Geometry(new(source.Supervisor, source.Project, "confirm", null, draft.Id, null, null,
                new RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryConfirmInputFact(null, DateTimeOffset.UtcNow, "TEST_ONLY source"),
                Guid.NewGuid().ToString(), draftResult.Version, UserRoleCode.Supervisor));
            Assert.True(confirmedResult.Status == 201, $"Confirm actual {confirmedResult.Status}/{confirmedResult.Code}");
            var route = Assert.IsType<RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.RoadGeometryVersionViewFact>(confirmedResult.Value);
            var setResult = await Geometry(new(source.Pm, source.Project, "set-create", null, null, route.RouteVersionId, null,
                new RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentDefinitionFact(20), Guid.NewGuid().ToString(), null, UserRoleCode.ProjectManager));
            Assert.Equal(201, setResult.Status);
            var set = Assert.IsType<RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentSetViewFact>(setResult.Value);
            Assert.Equal(200, (await Geometry(new(source.Pm, source.Project, "publish", null, null, route.RouteVersionId, set.Id,
                new RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentPublishInputFact(null, "TEST_ONLY source publication"),
                Guid.NewGuid().ToString(), setResult.Version, UserRoleCode.ProjectManager))).Status);
            var reporter = await db.Users.FirstAsync(x => x.RoleCode == UserRoleCode.Reporter);
            var original = await db.Set<RoadGuardSystem.BusinessObjects.Reports.Report>().FirstAsync(x => x.ReporterUserId == reporter.Id);
            var originalEvidence = await db.Set<RoadGuardSystem.BusinessObjects.Reports.Report>().AsNoTracking().Where(x => x.Id == original.Id)
                .SelectMany(x => x.OriginalEvidence).FirstAsync();
            var report = RoadGuardSystem.BusinessObjects.Reports.Report.Create(Guid.NewGuid(), reporter.Id, "TEST_ONLY genuine recurrence Report", DateTimeOffset.UtcNow,
                [RoadGuardSystem.BusinessObjects.Reports.VerifiedEvidenceReference.Create(Guid.NewGuid(), originalEvidence.FileId, originalEvidence.FileVersion, reporter.Id)]);
            var incident = RoadGuardSystem.BusinessObjects.Cases.IncidentCase.CreateUnassigned(Guid.NewGuid(), report.Id, DateTimeOffset.UtcNow);
            incident.Triage(source.Project, RoadGuardSystem.BusinessObjects.Cases.CaseVerificationMethod.ExistingEvidence, "TEST_ONLY source", DateTimeOffset.UtcNow);
            db.AddRange(report, incident); db.Entry(incident).Property<Guid?>("GeometryRouteVersionId").CurrentValue = route.RouteVersionId;
            db.Entry(incident).Property<Guid?>("GeometrySegmentSetId").CurrentValue = set.Id;
            db.Add(new RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink { Id = Guid.NewGuid(), CaseId = incident.Id, ReportId = report.Id, StartedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
            var scope = new RoadGuardSystem.Services.Authorization.ProjectScopeGuard(new RoadGuardSystem.Repositories.Projects.ProjectMembershipReadModel(db), TimeProvider.System);
            var producer = new RoadGuardSystem.Services.Integration.AnhHuyProducerService(new RoadGuardSystem.Repositories.Integration.AnhHuyFactsRepository(db), geometry, scope);
            var resolved = await producer.ResolveCandidateSourceAsync(source.Pm, UserRoleCode.ProjectManager, source.Project,
                RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind.Report, report.Id);
            Assert.Equal(RoadGuardSystem.Services.Integration.AnhHuyProducerStatus.Ready, resolved.Status);
            var lifecycle = new ProjectLifecycleRepository(db, TimeProvider.System);
            var candidate = new RoadGuardSystem.Services.Implementations.Defects.CandidateDecisionService(
                new RoadGuardSystem.Repositories.Implementations.Defects.CandidateDecisionRepository(db),
                new RoadGuardSystem.Repositories.Implementations.Cases.CaseWorkflowRepository(db), producer, [], scope, receipts, lifecycle);
            var service = new RoadGuardSystem.Services.Projects.ProjectLifecycleService(lifecycle, candidate);
            var priorDefect = await db.Defects.SingleAsync(x => x.Id == source.Defect);
            var evidence = await VerifiedFile(db, source.Project, source.Pm);
            var version = (await lifecycle.ReadAsync(source.Pm, source.Project, default))!.Version;
            var request = new RoadGuardSystem.DTOs.Projects.LD06LifecycleInputDto("TEST_ONLY true recurrence", [evidence], DefectId: source.Defect,
                PriorRepairDecisionId: decisionId, NewDefect: new("REPORT", report.Id, resolved.Facts!.DomainFacts.Source.SourceVersion,
                    resolved.Facts.Geometry.Version, "KEEP_NEW", null, null, new(priorDefect.DefectTypeCode, null, "LOW", route.RouteVersionId), "TEST_ONLY new recurrence"));
            var key = Guid.NewGuid().ToString();
            var result = await service.ExecuteAsync(source.Pm, UserRoleCode.ProjectManager, source.Project, "recurrences", request, key, '"' + version + '"', default);
            Assert.True(result.Status == 201, $"Recurrence actual {result.Status}/{result.Code}");
            var link = await db.Set<LD06LifecycleAction>().SingleAsync(x => x.ProjectId == source.Project && x.Kind == LD06ActionKind.LinkRecurrence);
            Assert.Equal(source.Defect, link.DefectId); Assert.NotEqual(source.Defect, link.LinkedDefectId); Assert.Equal(decisionId, link.PriorRepairDecisionId);
            Assert.True((await db.Set<RepairObligation>().SingleAsync(x => x.DefectId == source.Defect)).IsResolved);
            Assert.Single(await db.IdempotencyRecords.Where(x => x.ActorUserId == source.Pm && x.IdempotencyKey == key).ToArrayAsync());
            Assert.Equal(201, (await service.ExecuteAsync(source.Pm, UserRoleCode.ProjectManager, source.Project, "recurrences", request, key, '"' + version + '"', default)).Status);
            Assert.Single(await db.Set<LD06LifecycleAction>().Where(x => x.ProjectId == source.Project && x.Kind == LD06ActionKind.LinkRecurrence).ToArrayAsync());
        });

    [Fact]
    public Task ExplicitSupervisorCloseUsesActualSuccessfulRepairAndCorrectionReopensSameObligation()
        => new H4RepairExecutionSqlTests(sql, output).WithSuccessfulRepair(async (db, source, itemId, decisionId) =>
        {
            var repo = new ProjectLifecycleRepository(db, TimeProvider.System);
            var view = await repo.ReadAsync(source.Supervisor, source.Project, default);
            var command = new LD06LifecycleCommand(source.Supervisor, UserRoleCode.Supervisor, source.Project, LD06ActionKind.CloseDefect,
                new("TEST_ONLY explicit close after genuine repair", [], DefectId: source.Defect), Guid.NewGuid().ToString(), view!.Version);
            Assert.Equal(201, (await repo.ExecuteAsync(command, default)).Status);
            Assert.Equal(DefectStatus.Closed, (await db.Defects.AsNoTracking().SingleAsync(x => x.Id == source.Defect)).Status);
            var item = await db.Set<RepairItem>().AsNoTracking().SingleAsync(x => x.Id == itemId);
            var packageId = await db.Set<RepairPackage>().Where(x => x.DefectId == source.Defect).Select(x => x.Id).SingleAsync();
            var version = Convert.ToBase64String(await db.Set<RepairItem>().Where(x => x.Id == itemId).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
            var repair = new RoadGuardSystem.Repositories.Implementations.Repairs.RepairWorkflowRepository(db,
                new RoadGuardSystem.Repositories.Idempotency.IdempotencyOperationService(db), TimeProvider.System);
            var correction = new RoadGuardSystem.Repositories.Repairs.RepairCorrectionCommand(source.Supervisor, UserRoleCode.Supervisor,
                source.Project, packageId, itemId, new(decisionId, "UNREPAIRED", "TEST_ONLY actual correction",
                    new("TEST_ONLY independent recheck", [])), Guid.NewGuid().ToString(), version);
            Assert.Equal(201, (await repair.CorrectAsync(correction, default)).Status); db.ChangeTracker.Clear();
            Assert.Equal(DefectStatus.Open, (await db.Defects.SingleAsync(x => x.Id == source.Defect)).Status);
            Assert.Null((await db.Set<RepairObligation>().SingleAsync(x => x.Id == item.ObligationId)).EffectiveResolutionDecisionId);
            Assert.Single(await db.Set<LD06LifecycleAction>().Where(x => x.ProjectId == source.Project && x.Kind == LD06ActionKind.CloseDefect).ToArrayAsync());
        });

    [Fact]
    public async Task DeclarationRequiresVerifiedEvidenceAndConfirmationRetainsExactSource()
    {
        await using var db = sql.CreateDbContext(); var state = await H4GenuineRepairSource.Seed(db, sql);
        var repo = new ProjectLifecycleRepository(db, TimeProvider.System);
        async Task<ProjectLifecycleWriteResult> Run(Guid actor, UserRoleCode role, LD06ActionKind kind, LD06LifecycleInput input)
        {
            var view = await repo.ReadAsync(actor, state.Project, default);
            return await repo.ExecuteAsync(new(actor, role, state.Project, kind, input, Guid.NewGuid().ToString(), view!.Version), default);
        }
        Assert.Equal(409, (await Run(state.Supervisor, UserRoleCode.Supervisor, LD06ActionKind.ConfirmConstruction,
            new("TEST_ONLY confirm without declaration", []))).Status);
        Assert.Equal(409, (await Run(state.Pm, UserRoleCode.ProjectManager, LD06ActionKind.DeclareConstruction,
            new("TEST_ONLY missing evidence", []))).Status);
        var file = await VerifiedFile(db, state.Project, state.Pm);
        Assert.Equal(201, (await Run(state.Pm, UserRoleCode.ProjectManager, LD06ActionKind.DeclareConstruction,
            new("TEST_ONLY actual declaration producer", [file]))).Status);
        var declaration = await db.Set<LD06LifecycleAction>().SingleAsync(x => x.ProjectId == state.Project && x.Kind == LD06ActionKind.DeclareConstruction);
        var confirmed = await Run(state.Supervisor, UserRoleCode.Supervisor, LD06ActionKind.ConfirmConstruction,
            new("TEST_ONLY confirm exact declaration", [], declaration.Id));
        Assert.Equal(201, confirmed.Status); Assert.Equal("CONFIRMED", confirmed.Value!.ConstructionCompletion);
        Assert.Equal("UNKNOWN", confirmed.Value.OperationalClosure); Assert.True(confirmed.Value.AcceptsNewReports);
        Assert.Single(await db.Set<LD06ActionEvidence>().Where(x => x.ActionId == declaration.Id).ToArrayAsync());
        Assert.Equal(2, await db.OutboxMessages.CountAsync(x => db.Set<LD06LifecycleAction>().Any(a => a.ProjectId == state.Project && a.Id == x.Id)));
    }

    [Fact]
    public async Task ExactReceivingProjectAcceptanceTransfersResponsibilityWithoutResolvingOrClosingDefect()
    {
        await using var db = sql.CreateDbContext(); var source = await H4GenuineRepairSource.Seed(db, sql);
        var receiver = await H4GenuineRepairSource.Seed(db, sql);
        // TEST_ONLY scope inventory; production transfer admission is exercised below.
        var obligation = RepairObligation.Create(Guid.NewGuid(), source.Project, source.Defect, RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), source.Road, "TEST_ONLY_SOURCE_V1", "TEST_ONLY", 0, 10, -1, 1));
        db.Add(RepairPackage.Create(Guid.NewGuid(), source.Project, source.Defect, [obligation])); await db.SaveChangesAsync();
        var repo = new ProjectLifecycleRepository(db, TimeProvider.System);
        async Task<ProjectLifecycleWriteResult> Run(Guid actor, Guid project, LD06ActionKind kind, LD06LifecycleInput input)
        {
            var view = await repo.ReadAsync(actor, project, default);
            return await repo.ExecuteAsync(new(actor, UserRoleCode.Supervisor, project, kind, input, Guid.NewGuid().ToString(), view!.Version), default);
        }
        Assert.Equal(409, (await Run(source.Supervisor, source.Project, LD06ActionKind.OperationalClose, new("unresolved", []))).Status);
        var ownView = await repo.ReadAsync(source.Supervisor, source.Project, default);
        var scope = Assert.Single(ownView!.TransferableObligations!).ScopeHash;
        var issued = await Run(source.Supervisor, source.Project, LD06ActionKind.IssueTransfer,
            new("TEST_ONLY exact transfer", [], ObligationId: obligation.Id, ReceivingProjectId: receiver.Project, ScopeHash: scope));
        Assert.True(issued.Status == 201, $"Transfer issue actual {issued.Status}/{issued.Code}");
        var issue = await db.Set<LD06LifecycleAction>().SingleAsync(x => x.ProjectId == source.Project && x.Kind == LD06ActionKind.IssueTransfer);
        var receiverOffer = await repo.ReadAsync(receiver.Supervisor, receiver.Project, default);
        var receiverJson = System.Text.Json.JsonSerializer.Serialize(receiverOffer);
        Assert.DoesNotContain(source.Defect.ToString(), receiverJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("defectIds", receiverJson, StringComparison.OrdinalIgnoreCase);
        Assert.False(await db.Set<ObligationResponsibility>().AnyAsync(x => x.ObligationId == obligation.Id));
        var acceptInput = new LD06LifecycleInput("TEST_ONLY accept exact scope", [], issue.Id, ObligationId: obligation.Id, ScopeHash: scope);
        Assert.Equal(409, (await Run(receiver.Supervisor, receiver.Project, LD06ActionKind.AcceptTransfer, acceptInput with { ScopeHash = new string('0', 64) })).Status);
        Assert.Equal(201, (await Run(receiver.Supervisor, receiver.Project, LD06ActionKind.AcceptTransfer, acceptInput)).Status);
        db.ChangeTracker.Clear();
        var owner = await db.Set<ObligationResponsibility>().SingleAsync(x => x.ObligationId == obligation.Id);
        Assert.Equal(receiver.Project, owner.CurrentProjectId); Assert.Equal(source.Project, owner.OriginProjectId);
        Assert.Null((await db.Set<RepairObligation>().SingleAsync(x => x.Id == obligation.Id)).EffectiveResolutionDecisionId);
        Assert.Equal(409, (await Run(source.Supervisor, source.Project, LD06ActionKind.CloseDefect,
            new("transfer does not resolve", [], DefectId: source.Defect))).Status);
        var closed = await Run(source.Supervisor, source.Project, LD06ActionKind.OperationalClose, new("all source duties accepted", []));
        Assert.Equal(201, closed.Status); Assert.Equal("CONFIRMED", closed.Value!.OperationalClosure);
        Assert.True(closed.Value.AcceptsNewReports);
        var closure = await db.Set<LD06LifecycleAction>().SingleAsync(x => x.ProjectId == source.Project && x.Kind == LD06ActionKind.OperationalClose);
        var renewed = await repo.RenewAsync(new(source.Supervisor, UserRoleCode.Supervisor, source.Project,
            new(closure.Id, "TEST_ONLY continue handling after actual closure", "new intake and outstanding receiver work", "actual committed operational closure"),
            Guid.NewGuid().ToString(), closed.Value.Version), default);
        Assert.Equal(201, renewed.Status); Assert.True(renewed.Value!.AcceptsNewReports);
        Assert.Contains(renewed.Value.History, row => row.OperationalClosureId == closure.Id && row.Kind == "RenewedHandlingScope");
        Assert.Null((await db.Set<RepairObligation>().AsNoTracking().SingleAsync(x => x.Id == obligation.Id)).EffectiveResolutionDecisionId);
        Assert.Equal(409, (await Run(receiver.Supervisor, receiver.Project, LD06ActionKind.OperationalClose,
            new("receiver still owes unresolved work", []))).Status);
    }

    [Fact]
    public async Task ConcurrentAcceptanceHasOneOwnerAndClosureNeverPassesAnUnacceptedMandatoryObligation()
    {
        await using var seed = sql.CreateDbContext();
        var source = await H4GenuineRepairSource.Seed(seed, sql);
        var receiver = await H4GenuineRepairSource.Seed(seed, sql);
        var obligation = RepairObligation.Create(Guid.NewGuid(), source.Project, source.Defect, RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), source.Road, "TEST_ONLY_RACE", "TEST_ONLY", 0, 10, -1, 1));
        seed.Add(RepairPackage.Create(Guid.NewGuid(), source.Project, source.Defect, [obligation])); await seed.SaveChangesAsync();
        var repository = new ProjectLifecycleRepository(seed, TimeProvider.System);
        var sourceView = (await repository.ReadAsync(source.Supervisor, source.Project, default))!;
        var scope = Assert.Single(sourceView.TransferableObligations!).ScopeHash;
        Assert.Equal(201, (await repository.ExecuteAsync(new(source.Supervisor, UserRoleCode.Supervisor, source.Project,
            LD06ActionKind.IssueTransfer, new("TEST_ONLY race offer", [], ObligationId: obligation.Id,
                ReceivingProjectId: receiver.Project, ScopeHash: scope), Guid.NewGuid().ToString(), sourceView.Version), default)).Status);
        var issue = await seed.Set<LD06LifecycleAction>().SingleAsync(a => a.ProjectId == source.Project && a.Kind == LD06ActionKind.IssueTransfer);
        var receiverView = (await repository.ReadAsync(receiver.Supervisor, receiver.Project, default))!;
        var closureView = (await repository.ReadAsync(source.Supervisor, source.Project, default))!;
        async Task<ProjectLifecycleWriteResult> Accept()
        {
            await using var db = sql.CreateDbContext();
            return await new ProjectLifecycleRepository(db, TimeProvider.System).ExecuteAsync(new(receiver.Supervisor,
                UserRoleCode.Supervisor, receiver.Project, LD06ActionKind.AcceptTransfer,
                new("TEST_ONLY concurrent acceptance", [], issue.Id, ObligationId: obligation.Id, ScopeHash: scope),
                Guid.NewGuid().ToString(), receiverView.Version), default);
        }
        async Task<ProjectLifecycleWriteResult> Close()
        {
            await using var db = sql.CreateDbContext();
            return await new ProjectLifecycleRepository(db, TimeProvider.System).ExecuteAsync(new(source.Supervisor,
                UserRoleCode.Supervisor, source.Project, LD06ActionKind.OperationalClose,
                new("TEST_ONLY concurrent closure", []), Guid.NewGuid().ToString(), closureView.Version), default);
        }
        var outcomes = await Task.WhenAll(Accept(), Accept(), Close());
        Assert.Single(outcomes.Take(2).Where(r => r.Status == 201));
        Assert.Single(outcomes.Take(2).Where(r => r.Status == 409));
        // Closure's stale captured projection or still-unaccepted obligation must refuse in either ordering.
        Assert.Equal(409, outcomes[2].Status);
        Assert.Single(await seed.Set<ObligationResponsibility>().Where(r => r.ObligationId == obligation.Id).ToArrayAsync());
        Assert.Single(await seed.Set<LD06LifecycleAction>().Where(a => a.SourceActionId == issue.Id && a.Kind == LD06ActionKind.AcceptTransfer).ToArrayAsync());
        Assert.False(await seed.Set<LD06LifecycleAction>().AnyAsync(a => a.ProjectId == source.Project && a.Kind == LD06ActionKind.OperationalClose));
        var fresh = (await repository.ReadAsync(source.Supervisor, source.Project, default))!;
        Assert.Equal(201, (await repository.ExecuteAsync(new(source.Supervisor, UserRoleCode.Supervisor, source.Project,
            LD06ActionKind.OperationalClose, new("TEST_ONLY fresh accepted inventory", []), Guid.NewGuid().ToString(), fresh.Version), default)).Status);
    }

    [Fact]
    public async Task ReceiptReplayRechecksCurrentMembership()
    {
        await using var db = sql.CreateDbContext(); var state = await H4GenuineRepairSource.Seed(db, sql);
        var repo = new ProjectLifecycleRepository(db, TimeProvider.System);
        var file = await VerifiedFile(db, state.Project, state.Pm);
        var view = await repo.ReadAsync(state.Pm, state.Project, default);
        var command = new LD06LifecycleCommand(state.Pm, UserRoleCode.ProjectManager, state.Project,
            LD06ActionKind.DeclareConstruction, new("TEST_ONLY declaration", [file]), Guid.NewGuid().ToString(), view!.Version);
        Assert.Equal(201, (await repo.ExecuteAsync(command, default)).Status);
        Assert.Equal(200, (await repo.ExecuteAsync(command, default)).Status);
        await db.ProjectMembers.Where(x => x.ProjectId == state.Project && x.UserId == state.Pm)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.Status, ProjectMemberStatus.Ended));
        Assert.Equal(403, (await repo.ExecuteAsync(command, default)).Status);
        Assert.Single(await db.IdempotencyRecords.Where(x => x.IdempotencyKey == command.Key).ToArrayAsync());
    }

    [Fact]
    public async Task AuditFailureRollsBackDeclarationEvidenceOutboxAndReceipt()
    {
        await using var db = sql.CreateDbContext(); var state = await H4GenuineRepairSource.Seed(db, sql);
        var repo = new ProjectLifecycleRepository(db, TimeProvider.System);
        var file = await VerifiedFile(db, state.Project, state.Pm);
        var view = await repo.ReadAsync(state.Pm, state.Project, default);
        var reason = "TEST_ONLY_LD06_ROLLBACK_" + Guid.NewGuid().ToString("N");
        var command = new LD06LifecycleCommand(state.Pm, UserRoleCode.ProjectManager, state.Project,
            LD06ActionKind.DeclareConstruction, new(reason, [file]), Guid.NewGuid().ToString(), view!.Version);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER TR_TEST_LD06_AuditRollback ON AuditLogs AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE EventType='ld06_DeclareConstruction' AND Reason LIKE 'TEST_ONLY_LD06_ROLLBACK_%') THROW 51699, 'TEST_ONLY audit failure', 1; END");
        try { await Assert.ThrowsAsync<DbUpdateException>(() => repo.ExecuteAsync(command, default)); }
        finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER TR_TEST_LD06_AuditRollback"); db.ChangeTracker.Clear(); }
        Assert.False(await db.Set<LD06LifecycleAction>().AnyAsync(x => x.ProjectId == state.Project));
        Assert.False(await db.Set<LD06ActionEvidence>().AnyAsync(x => x.FileId == file));
        Assert.False(await db.IdempotencyRecords.AnyAsync(x => x.IdempotencyKey == command.Key));
        Assert.Equal(view.Version, (await repo.ReadAsync(state.Pm, state.Project, default))!.Version);
    }

    private static async Task<Guid> VerifiedFile(RoadGuardSystem.Repositories.RoadGuardDbContext db, Guid project, Guid actor)
    {
        var now = DateTimeOffset.UtcNow;
        var file = StoredFile.Create(Guid.NewGuid(), "TEST_ONLY/ld06/" + Guid.NewGuid(), "construction.jpg", "image/jpeg", 4,
            new string('c', 64), actor, now, null);
        db.AddRange(file, FileScope.Create(Guid.NewGuid(), file.Id, project, project, actor, "CONSTRUCTION", now));
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, actor, file.StorageUri, "CONSTRUCTION", "image/jpeg", 4,
            file.Checksum, 8388608, now.AddHours(24)); upload.StartUploading("TEST_ONLY", now); db.Add(upload); await db.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync();
        upload.MarkVerified(); await db.SaveChangesAsync(); return file.Id;
    }
}
