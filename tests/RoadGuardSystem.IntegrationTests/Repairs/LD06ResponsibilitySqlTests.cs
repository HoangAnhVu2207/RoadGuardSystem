using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using Xunit;
namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed class LD06ResponsibilitySqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task AcceptanceReceiptFailureRollsBackOwnerActionAndReceivingAdmission()
    {
        var source = await Seed(awaiting: true); var receiver = await Seed(awaiting: true);
        await using var db = sql.CreateDbContext();
        var issue = await Issue(db, source, receiver, source.Formal);
        await using (var failing = sql.CreateDbContext(new RejectAcceptanceReceipt()))
            await Assert.ThrowsAsync<InvalidOperationException>(() => Accept(failing, receiver, issue));
        Assert.False(await db.Set<ObligationResponsibility>().AnyAsync(o => o.ObligationId == source.Formal));
        Assert.False(await db.Set<LD06LifecycleAction>().AnyAsync(a => a.SourceActionId == issue.Id && a.Kind == LD06ActionKind.AcceptTransfer));
        var repair = Repo(db);
        Assert.Equal(200, (await repair.ReadItemAsync(source.Supervisor, UserRoleCode.Supervisor, source.Project,
            source.Package, source.Item, false, default)).Status);
        Assert.Equal(404, (await repair.ReadItemAsync(receiver.Supervisor, UserRoleCode.Supervisor, receiver.Project,
            source.Package, source.Item, false, default)).Status);
    }

    private sealed class RejectAcceptanceReceipt : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries().Any(e => e.State == EntityState.Added &&
                e.Metadata.ClrType.Name == "IdempotencyRecord" &&
                e.Property("Operation").CurrentValue as string == "ld06.lifecycle.AcceptTransfer.v1"))
                throw new InvalidOperationException("Injected receipt durability failure.");
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    [Fact]
    public async Task PendingIssueKeepsSourceThenAtomicAcceptanceMovesExactApprovalReadAndReplayAuthority()
    {
        var source = await Seed(awaiting: true); var receiver = await Seed(awaiting: true);
        await using var db = sql.CreateDbContext(); var repair = Repo(db);
        var originalClock = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(c => c.TargetId == source.Item);
        var issue = await Issue(db, source, receiver, source.Formal);
        Assert.Equal(200, (await repair.ReadItemAsync(source.Supervisor, UserRoleCode.Supervisor, source.Project, source.Package, source.Item, true, default)).Status);
        Assert.Equal(404, (await repair.ReadItemAsync(receiver.Supervisor, UserRoleCode.Supervisor, receiver.Project, source.Package, source.Item, false, default)).Status);
        await Accept(db, receiver, issue);
        Assert.Equal(404, (await repair.ReadItemAsync(source.Supervisor, UserRoleCode.Supervisor, source.Project, source.Package, source.Item, false, default)).Status);
        var read = await repair.ReadItemAsync(receiver.Supervisor, UserRoleCode.Supervisor, receiver.Project, source.Package, source.Item, false, default);
        Assert.Equal(200, read.Status); Assert.Equal(source.Project, Assert.IsType<RepairItemFact>(read.Value).ProjectId);
        var command = new RepairItemApprovalCommand(receiver.Supervisor, UserRoleCode.Supervisor, receiver.Project, source.Package, source.Item,
            new("current receiving Supervisor approves"), Guid.NewGuid().ToString(), source.ItemVersion);
        var approved = await repair.ApproveItemAsync(command, default);
        Assert.True(approved.Status == 201, $"{approved.Status}/{approved.Code}");
        Assert.Equal(200, (await repair.ApproveItemAsync(command, default)).Status);
        var preserved = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(c => c.Id == originalClock.Id);
        Assert.Equal(originalClock.ProjectId, preserved.ProjectId); Assert.Equal(originalClock.OriginAt, preserved.OriginAt);
        Assert.Equal(originalClock.OriginalDueAt, preserved.OriginalDueAt);
        Assert.True(await db.Set<RepairItemLifecycleEvent>().AnyAsync(e => e.ItemId == source.Item && e.Kind == "APPROVED" && e.ProjectId == source.Project && e.ActorId == receiver.Supervisor));
        await db.ProjectMembers.Where(m => m.ProjectId == receiver.Project && m.UserId == receiver.Supervisor)
            .ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        Assert.Equal(403, (await repair.ApproveItemAsync(command, default)).Status);
        Assert.False(await db.ProjectMembers.AnyAsync(m => m.ProjectId == source.Project && m.UserId == receiver.Supervisor));
    }

    [Fact]
    public async Task ReceivingPmContinuesSameObligationAndReceiverSupervisorApprovesWithoutMovingSourceGraph()
    {
        var source = await Seed(awaiting: true); var receiver = await Seed(awaiting: true);
        await using var db = sql.CreateDbContext(); var repair = Repo(db);
        await Accept(db, receiver, await Issue(db, source, receiver, source.Formal));
        var continued = await repair.ContinueNormallyAsync(new(receiver.Pm, UserRoleCode.ProjectManager, receiver.Project,
            source.Package, source.Item, new("continuing plan", "checklist-v2", "receiver continues unfinished obligation", null),
            Guid.NewGuid().ToString(), source.ItemVersion), default);
        Assert.True(continued.Status == 201, $"{continued.Status}/{continued.Code}");
        var fact = Assert.IsType<RepairLifecycleFact>(continued.Value);
        var successor = await db.RepairItems.AsNoTracking().SingleAsync(i => i.Id == fact.SuccessorItemId);
        Assert.Equal(source.Project, successor.ProjectId); Assert.Equal(source.Formal, successor.ObligationId);
        Assert.False(await db.RepairObligations.Where(o => o.Id == source.Formal).Select(o => o.EffectiveResolutionDecisionId != null).SingleAsync());
        var approval = await repair.ApproveItemAsync(new(receiver.Supervisor, UserRoleCode.Supervisor, receiver.Project,
            source.Package, successor.Id, new("approve continuation"), Guid.NewGuid().ToString(), fact.SuccessorVersion!), default);
        Assert.True(approval.Status == 201, $"{approval.Status}/{approval.Code}");
        Assert.Equal(404, (await repair.ReadItemAsync(source.Supervisor, UserRoleCode.Supervisor, source.Project, source.Package, successor.Id, false, default)).Status);
    }

    [Fact]
    public async Task AcceptedFormalAndSafetyTransfersPreserveInstalledMonitoringCrewAndClockRouteSupervisorToReceiver()
    {
        var source = await Seed(); var receiver = await Seed(awaiting: true);
        await using var db = sql.CreateDbContext(); var safety = new RepairSafetyRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var created = await safety.CreateAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, source.Package, source.Item,
            source.Formal, source.Safety, source.Crew, "each shift", "if displaced", "after formal repair", "temporary barrier",
            Guid.NewGuid().ToString(), source.ItemVersion), default);
        Assert.Equal(201, created.Status); var measure = Assert.IsType<SafetyFact>(created.Value);
        var installed = await safety.InstallAsync(new(source.Crew, UserRoleCode.RepairCrew, source.Project, source.Package,
            source.Item, measure.Id, DateTimeOffset.UtcNow.AddHours(12), "installed barrier", Guid.NewGuid().ToString(), measure.Version), default);
        Assert.Equal(201, installed.Status);
        var before = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(c => c.TargetId == measure.Id);
        await Accept(db, receiver, await Issue(db, source, receiver, source.Formal));
        await Accept(db, receiver, await Issue(db, source, receiver, source.Safety));
        Assert.Equal(404, (await safety.ReadAsync(source.Supervisor, UserRoleCode.Supervisor, source.Project, source.Package, source.Item, measure.Id, default)).Status);
        Assert.Equal(200, (await safety.ReadAsync(receiver.Supervisor, UserRoleCode.Supervisor, receiver.Project, source.Package, source.Item, measure.Id, default)).Status);
        Assert.Equal(200, (await safety.ReadAsync(source.Crew, UserRoleCode.RepairCrew, source.Project, source.Package, source.Item, measure.Id, default)).Status);
        var eligibility = new RepairEligibilityRepository(db, TimeProvider.System);
        Assert.Equal(404, (await eligibility.ReadAsync(new(source.Supervisor, UserRoleCode.Supervisor, source.Project, source.Package, source.Item), default)).Status);
        var receiverEligibility = await eligibility.ReadAsync(new(receiver.Supervisor, UserRoleCode.Supervisor, receiver.Project, source.Package, source.Item), default);
        Assert.Equal(200, receiverEligibility.Status); Assert.Equal(source.Project, receiverEligibility.Value!.Sources.ProjectId);
        var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(b => b.ItemId == source.Item);
        var native = await db.FieldInspectionTasks.SingleAsync(t => t.Id == binding.TaskId);
        var field = new RoadGuardSystem.Repositories.Implementations.Inspections.FieldInspectionWorkflowRepository(db,
            new IdempotencyOperationService(db), TimeProvider.System);
        var admission = new RoadGuardSystem.Repositories.Inspections.FieldAdmissionContext(source.Crew,
            UserRoleCode.RepairCrew, source.Crew, "DIRECT", true);
        var accepted = await field.ExecuteAsync(new(source.Project, native.Id, "accept",
            new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskActionInputFact("accept existing assignment"),
            Guid.NewGuid().ToString(), Convert.ToBase64String(native.RowVersion), admission), _ => Task.FromResult(true), default);
        Assert.Equal(201, accepted.Status);
        var nativeVersion = Convert.ToBase64String(await db.FieldInspectionTasks.Where(t => t.Id == native.Id).Select(t => t.RowVersion).SingleAsync());
        var started = await field.ExecuteAsync(new(source.Project, native.Id, "start",
            new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldStartInputFact(Guid.NewGuid(), DateTimeOffset.UtcNow),
            Guid.NewGuid().ToString(), nativeVersion, admission), _ => Task.FromResult(true), default);
        Assert.Equal(201, started.Status);
        var first = await db.Set<RoadGuardSystem.BusinessObjects.Inspections.FieldTaskStartOrigin>().SingleAsync(f => f.TaskId == native.Id);
        var itemVersion = Convert.ToBase64String(await db.RepairItems.Where(i => i.Id == source.Item)
            .Select(i => EF.Property<byte[]>(i, "RowVersion")).SingleAsync());
        var assessmentCommand = new RepairAssessmentCommand(source.Crew, UserRoleCode.RepairCrew, source.Project,
            source.Package, source.Item, native.Id, new(Guid.NewGuid(), first.Id, null, null, null), Guid.NewGuid().ToString(), itemVersion);
        var assessment = await Repo(db).AssessAsync(assessmentCommand, default);
        Assert.True(assessment.Status == 201, $"{assessment.Status}/{assessment.Code}");
        Assert.Equal(200, (await Repo(db).AssessAsync(assessmentCommand, default)).Status);
        var after = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(c => c.Id == before.Id);
        Assert.Equal(before.OriginEventId, after.OriginEventId); Assert.Equal(before.OriginAt, after.OriginAt); Assert.Equal(before.OriginalDueAt, after.OriginalDueAt);
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            var proof = await new H6DeadlineNotificationSourceAdapter(db).ResolveDutyAsync(after, default);
            Assert.Equal("VERIFIED", proof.Status); Assert.Equal(receiver.Project, proof.ResponsibilityProjectId);
            Assert.Equal(source.Crew, proof.ResponsibleUserId); await tx.CommitAsync();
        }
        var assigned = await db.Set<RepairSafetyActionSource>().SingleAsync(a => a.MeasureId == measure.Id && a.Kind == "ASSIGNED");
        var outbox = await db.OutboxMessages.SingleAsync(o => o.Id == assigned.Id); var fence = Guid.NewGuid();
        outbox.AcquireLease("h6:" + fence.ToString("N"), DateTimeOffset.UtcNow, TimeSpan.FromMinutes(2), 32); await db.SaveChangesAsync();
        var claim = new H6Claim(outbox.Id, outbox.MessageType, outbox.OccurredAtUtc, outbox.PayloadJson, fence, outbox.LeaseExpiresAtUtc!.Value, outbox.DeliveryAttemptCount);
        var dispatched = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim,
            H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal("COMMITTED", dispatched.Status);
        var delivered = await db.Set<H6NotificationDeliveryRow>().Where(d => d.OccurrenceId == dispatched.OccurrenceId && d.Status == "DELIVERED").Select(d => d.RecipientUserId).ToArrayAsync();
        Assert.Contains((Guid?)receiver.Supervisor, delivered); Assert.DoesNotContain((Guid?)source.Supervisor, delivered);
        Assert.False(await db.ProjectMembers.AnyAsync(m => m.ProjectId == source.Project && m.UserId == receiver.Supervisor));
    }

    [Fact]
    public async Task WeeklyDigestForOneSupervisorAcrossTwoReceivingProjectsValidatesEveryCapturedOwner()
    {
        var first = await Seed(awaiting: true); var second = await Seed(awaiting: true, sameProject: first);
        var receiverB = await Seed(awaiting: true); var receiverC = await Seed(awaiting: true);
        await using var db = sql.CreateDbContext();
        db.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = receiverC.Project,
            UserId = receiverB.Supervisor,
            RoleCode = UserRoleCode.Supervisor,
            Status = ProjectMemberStatus.Active,
            ValidFrom = new(2000, 1, 1)
        });
        await db.SaveChangesAsync();
        await Accept(db, receiverB, await Issue(db, first, receiverB, first.Formal));
        await Accept(db, receiverC, await Issue(db, second, receiverC, second.Formal));
        var now = NotificationCalendarPolicy.NextWeeklyReview(DateTimeOffset.UtcNow).AddMinutes(1); var time = new FixedClock(now);
        await new H6NotificationDispatchRepository(db, time).ObserveCalendarAsync(Guid.NewGuid(), null, default);
        var digest = await db.Set<WeeklyReviewDigest>().Include(d => d.Duties).SingleAsync(d => d.ProjectId == first.Project && d.RecipientId == receiverB.Supervisor);
        Assert.Equal(2, digest.Duties.Count);
        var outbox = await db.OutboxMessages.SingleAsync(o => o.Id == digest.Id); var fence = Guid.NewGuid();
        outbox.AcquireLease("h6:" + fence.ToString("N"), now, TimeSpan.FromMinutes(2), 32); await db.SaveChangesAsync();
        var claim = new H6Claim(outbox.Id, outbox.MessageType, outbox.OccurredAtUtc, outbox.PayloadJson, fence, outbox.LeaseExpiresAtUtc!.Value, outbox.DeliveryAttemptCount);
        var result = await new H6NotificationDispatchRepository(db, time).DispatchAsync(claim,
            H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal("COMMITTED", result.Status); Assert.Equal(1, result.Delivered);
        var notification = await db.Set<H6NotificationDeliveryRow>().Where(d => d.OccurrenceId == result.OccurrenceId).Select(d => d.NotificationId).SingleAsync();
        var inbox = new H6ProtectedNotificationRepository(db, new IdempotencyOperationService(db), time, [new H6WeeklyReviewSourceAdapter(db, time)]);
        Assert.NotNull(await inbox.GetAsync(receiverB.Supervisor, notification!.Value));
        Assert.NotNull(await inbox.WeeklyDigestAsync(receiverB.Supervisor, UserRoleCode.Supervisor, first.Project, digest.Id, default));
        await db.ProjectMembers.Where(m => m.ProjectId == receiverC.Project && m.UserId == receiverB.Supervisor)
            .ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        Assert.Null(await inbox.GetAsync(receiverB.Supervisor, notification.Value));
        Assert.Null(await inbox.WeeklyDigestAsync(receiverB.Supervisor, UserRoleCode.Supervisor, first.Project, digest.Id, default));
        Assert.False(await db.ProjectMembers.AnyAsync(m => m.ProjectId == first.Project && m.UserId == receiverB.Supervisor));
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => now; }

    private static RepairWorkflowRepository Repo(RoadGuardDbContext db) => new(db, new IdempotencyOperationService(db), TimeProvider.System);
    private static async Task<LD06ActionFact> Issue(RoadGuardDbContext db, Source source, Source receiver, Guid obligation)
    {
        var repo = new ProjectLifecycleRepository(db, TimeProvider.System);
        var fact = await repo.ReadAsync(source.Supervisor, source.Project, default); Assert.NotNull(fact);
        var row = await db.RepairObligations.Include(o => o.Scope).SingleAsync(o => o.Id == obligation);
        var result = await repo.ExecuteAsync(new(source.Supervisor, UserRoleCode.Supervisor, source.Project, LD06ActionKind.IssueTransfer,
            new("exact obligation transfer", [], ObligationId: obligation, ReceivingProjectId: receiver.Project, ScopeHash: LD06LifecycleAction.HashScope(row)), Guid.NewGuid().ToString(), fact.Version), default);
        Assert.True(result.Status == 201, $"{result.Status}/{result.Code}");
        return result.Value!.Actions!.Single(a => a.Kind == nameof(LD06ActionKind.IssueTransfer) && a.ObligationId == obligation);
    }
    private static async Task Accept(RoadGuardDbContext db, Source receiver, LD06ActionFact issue)
    {
        var repo = new ProjectLifecycleRepository(db, TimeProvider.System);
        var fact = await repo.ReadAsync(receiver.Supervisor, receiver.Project, default); Assert.NotNull(fact);
        var result = await repo.ExecuteAsync(new(receiver.Supervisor, UserRoleCode.Supervisor, receiver.Project, LD06ActionKind.AcceptTransfer,
            new("accept exact outstanding obligation", [], SourceActionId: issue.Id, ObligationId: issue.ObligationId, ScopeHash: issue.ScopeHash),
            Guid.NewGuid().ToString(), fact.Version), default);
        Assert.True(result.Status == 201, $"{result.Status}/{result.Code}");
    }
    private async Task<Source> Seed(bool awaiting = false, Source? sameProject = null)
    {
        var fixture = sql;
        await using var db = fixture.CreateDbContext();
        H4GenuineRepairSource.Source actual;
        if (sameProject is null) actual = await H4GenuineRepairSource.Seed(db, fixture);
        else
        {
            var defect = await db.Defects.SingleAsync(d => d.Id == db.RepairItems.Where(i => i.Id == sameProject.Item).Select(i => i.DefectId).Single());
            var route = await db.RoadSectionVersions.SingleAsync(r => r.Id == defect.RoadSectionVersionId);
            var set = await db.RoadSegmentSets.SingleAsync(r => r.RoadSectionVersionId == route.Id);
            var originalReport = await db.Set<RoadGuardSystem.BusinessObjects.Reports.Report>()
                .Include(r => r.OriginalEvidence).FirstAsync(r => r.OriginalEvidence.Any());
            var evidence = originalReport.OriginalEvidence[0];
            var now = DateTimeOffset.UtcNow;
            var report = RoadGuardSystem.BusinessObjects.Reports.Report.Create(Guid.NewGuid(), originalReport.ReporterUserId,
                "TEST_ONLY distinct reported defect in existing project", now,
                [RoadGuardSystem.BusinessObjects.Reports.VerifiedEvidenceReference.Create(Guid.NewGuid(), evidence.FileId,
                    evidence.FileVersion, originalReport.ReporterUserId)]);
            var incident = RoadGuardSystem.BusinessObjects.Cases.IncidentCase.CreateUnassigned(Guid.NewGuid(), report.Id, now);
            incident.Triage(sameProject.Project, RoadGuardSystem.BusinessObjects.Cases.CaseVerificationMethod.ExistingEvidence,
                "TEST_ONLY distinct retained source", now);
            db.AddRange(report, incident, new RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink
            { Id = Guid.NewGuid(), CaseId = incident.Id, ReportId = report.Id, StartedAt = now });
            await db.SaveChangesAsync();
            var candidate = RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceFacts.Create(
                RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceIdentity.Create(
                    RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind.Report, report.Id, "TEST_ONLY-source"),
                sameProject.Project, "TEST_ONLY-geometry");
            var accepted = await new RoadGuardSystem.Repositories.Implementations.Defects.CandidateDecisionRepository(db)
                .SaveAcceptedAsync(sameProject.Pm, candidate, RoadGuardSystem.BusinessObjects.Candidates.CandidateDecisionKind.KeepNew,
                    RoadGuardSystem.BusinessObjects.Candidates.CandidateClassification.Create(route.Id, defect.DefectTypeCode,
                        null, DefectSeverity.Low, null), null, null, null, "TEST_ONLY distinct confirmed new defect", null, default);
            actual = new(sameProject.Pm, sameProject.Crew, sameProject.Supervisor, sameProject.Project, route.RoadSectionId, route.Id, set.Id, accepted.DefectId!.Value);
        }
        var defectVersion = Convert.ToBase64String(await db.Defects.Where(row => row.Id == actual.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var producer = new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var packageResult = await producer.CreatePackageAsync(new(actual.Pm, UserRoleCode.ProjectManager,
            actual.Project, new(actual.Defect, defectVersion,
                [new("FORMAL_REPAIR", true, new(actual.Road, actual.Route, actual.Set, null, null, 1, 2, 0, 1), "formal repair"),
                 new("TEMPORARY_SAFETY", true, new(actual.Road, actual.Route, actual.Set, null, null, 1, 2, 0, 1), "temporary safety")],
                "actual package"), Guid.NewGuid().ToString(), defectVersion), default);
        Assert.Equal(201, packageResult.Status);
        var packageFact = Assert.IsType<RepairPackageFact>(packageResult.Value);
        var package = await db.Set<RepairPackage>().Include(row => row.Obligations).SingleAsync(row => row.Id == packageFact.Id);
        var formal = package.Obligations.Single(row => row.Kind == RepairObligationKind.FormalRepair);
        var safety = package.Obligations.Single(row => row.Kind == RepairObligationKind.TemporarySafety);
        var proposed = await producer.ProposeItemAsync(new(actual.Pm, UserRoleCode.ProjectManager,
            actual.Project, package.Id, new(formal.Id, "NORMAL", "temporary barrier within approved repair scope",
                "checklist-v1", "actual proposal"), Guid.NewGuid().ToString(), packageFact.Version), default);
        Assert.Equal(201, proposed.Status);
        var item = Assert.IsType<RepairItemFact>(proposed.Value);
        if (awaiting) return new(actual.Pm, actual.Crew, actual.Supervisor, actual.Project, package.Id, item.Id,
            formal.Id, safety.Id, item.Version);
        var approved = await producer.ApproveItemAsync(new(actual.Supervisor, UserRoleCode.Supervisor,
            actual.Project, package.Id, item.Id, new("approve scoped plan"), Guid.NewGuid().ToString(),
            item.Version), default);
        Assert.Equal(201, approved.Status);
        var approvedFact = Assert.IsType<RepairItemFact>(approved.Value);
        defectVersion = Convert.ToBase64String(await db.Defects.Where(row => row.Id == actual.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var task = new RepairFieldTaskData(actual.Defect, defectVersion, null, "REPORTER", actual.Route,
            actual.Set, null, null, "POST_REPAIR", 1, "{}", null, actual.Crew, DateTimeOffset.UtcNow.AddDays(1));
        var assigned = await producer.AssignItemAsync(new(actual.Pm, UserRoleCode.ProjectManager,
            actual.Project, package.Id, item.Id, new(task, null, "assign approved plan"),
            Guid.NewGuid().ToString(), approvedFact.Version), default);
        Assert.Equal(201, assigned.Status);
        var current = await db.Set<RepairItem>().SingleAsync(row => row.Id == item.Id);
        var version = Convert.ToBase64String(db.Entry(current).Property<byte[]>("RowVersion").CurrentValue!);
        return new(actual.Pm, actual.Crew, actual.Supervisor, actual.Project, package.Id, item.Id,
            formal.Id, safety.Id, version);
    }

    private sealed record Source(Guid Pm, Guid Crew, Guid Supervisor, Guid Project, Guid Package,
        Guid Item, Guid Formal, Guid Safety, string ItemVersion);
}
