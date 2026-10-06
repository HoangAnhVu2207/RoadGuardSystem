using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.SqlClient;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed class H4SafetyRuntimeSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task ApprovedBoundNormalSafetyCreatesOnceAndInstallationPinsExactFirstCheckClock()
    {
        var source = await Seed();
        await using var db = sql.CreateDbContext();
        var repository = new RepairSafetyRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var key = Guid.NewGuid().ToString();
        var command = new SafetyCreateCommand(source.Pm, UserRoleCode.ProjectManager, source.Project,
            source.Package, source.Item, source.Formal, source.Safety, source.Crew,
            "each shift", "replace if displaced", "remove after formal repair", "approved temporary barrier",
            key, source.ItemVersion);
        var created = await repository.CreateAsync(command, default);
        Assert.Equal(201, created.Status);
        var measure = Assert.IsType<SafetyFact>(created.Value);
        Assert.Equal(source.Crew, measure.ResponsibleActorId);
        Assert.Equal(source.Safety, measure.SafetyObligationId);
        Assert.Null(measure.InstalledAt);
        Assert.Equal(measure.Id, Assert.IsType<SafetyFact>((await repository.CreateAsync(command, default)).Value).Id);
        Assert.Equal(1, await db.Set<RepairSafetyMonitoring>().CountAsync(row => row.SafetyObligationId == source.Safety));
        var assigned = await db.Set<RepairSafetyActionSource>().SingleAsync(row => row.MeasureId == measure.Id &&
            row.Kind == "ASSIGNED");
        Assert.True(await db.OutboxMessages.AnyAsync(row => row.Id == assigned.Id &&
            row.MessageType == "safety.measure_assigned.v1"));
        var now = DateTimeOffset.UtcNow;
        var due = now.AddHours(12);
        var install = new SafetyInstallCommand(source.Crew, UserRoleCode.RepairCrew,
            source.Project, source.Package, source.Item, measure.Id, due, "barrier installed",
            Guid.NewGuid().ToString(), measure.Version);
        var installed = await repository.InstallAsync(install, default);
        Assert.Equal(201, installed.Status);
        var installedView = Assert.IsType<SafetyFact>(installed.Value);
        Assert.Equal(due, installedView.FirstCheckDueAt);
        var clock = await db.Set<DeadlineClock>().SingleAsync(row => row.TargetId == measure.Id &&
            row.Kind == DeadlineClockKind.FirstSafetyCheck);
        Assert.Equal(due, clock.OriginalDueAt);
        Assert.Equal(installedView.InstalledAt, clock.OriginAt);
        var sourceAction = await db.Set<RepairSafetyActionSource>().SingleAsync(row =>
            row.MeasureId == measure.Id && row.Kind == "INSTALLED");
        var immutable = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE RepairSafetyActionSources SET Reason={"tampered"} WHERE Id={sourceAction.Id}"));
        Assert.Equal(51271, immutable.Number);
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.EndsWith("H4SafetySourceAdmission", applied[^1]);
        var downgrade = await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>().MigrateAsync(applied[^2]));
        Assert.Equal(51273, downgrade.Number);
        Assert.True(await db.Set<RepairSafetyActionSource>().AnyAsync(row => row.Id == sourceAction.Id));
        Assert.False(await db.Set<RepairObligation>().Where(row => row.Id == source.Safety)
            .Select(row => row.EffectiveResolutionDecisionId != null).SingleAsync());
        await db.ProjectMembers.Where(row => row.ProjectId == source.Project && row.UserId == source.Crew)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        Assert.Equal(403, (await repository.InstallAsync(install, default)).Status);
    }

    [Fact]
    public async Task AssignedSafetyOutboxDispatchesCurrentSupervisor()
    {
        var source = await Seed();
        await using var db = sql.CreateDbContext();
        var repository = new RepairSafetyRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var created = await repository.CreateAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            source.Package, source.Item, source.Formal, source.Safety, source.Crew, "each shift",
            "replace if displaced", "remove after formal repair", "approved temporary barrier",
            Guid.NewGuid().ToString(), source.ItemVersion), default);
        Assert.Equal(201, created.Status);
        var measure = Assert.IsType<SafetyFact>(created.Value);
        var action = await db.Set<RepairSafetyActionSource>().SingleAsync(row => row.MeasureId == measure.Id &&
            row.Kind == "ASSIGNED");
        var outbox = await db.OutboxMessages.SingleAsync(row => row.Id == action.Id);
        var fence = Guid.NewGuid(); outbox.AcquireLease("h6:" + fence.ToString("N"), DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(2), 32); await db.SaveChangesAsync();
        var claim = new H6Claim(outbox.Id, outbox.MessageType, outbox.OccurredAtUtc, outbox.PayloadJson,
            fence, outbox.LeaseExpiresAtUtc!.Value, outbox.DeliveryAttemptCount);
        var dispatch = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim,
            H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal("COMMITTED", dispatch.Status);
        Assert.Equal(1, dispatch.Delivered);
        Assert.Equal(source.Supervisor, await db.Set<H6NotificationDeliveryRow>()
            .Where(row => row.OccurrenceId == dispatch.OccurrenceId).Select(row => row.RecipientUserId).SingleAsync());
    }

    [Fact]
    public async Task InstalledFirstCheckDeadlineBreachUsesCurrentResponsibleAndSupervisor()
    {
        var source = await Seed();
        await using var db = sql.CreateDbContext();
        var safety = new RepairSafetyRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var created = await safety.CreateAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            source.Package, source.Item, source.Formal, source.Safety, source.Crew, "each shift",
            "replace if displaced", "remove after formal repair", "approved temporary barrier",
            Guid.NewGuid().ToString(), source.ItemVersion), default);
        Assert.Equal(201, created.Status);
        var measure = Assert.IsType<SafetyFact>(created.Value);
        var due = DateTimeOffset.UtcNow.AddHours(12);
        Assert.Equal(201, (await safety.InstallAsync(new(source.Crew, UserRoleCode.RepairCrew,
            source.Project, source.Package, source.Item, measure.Id, due, "barrier installed",
            Guid.NewGuid().ToString(), measure.Version), default)).Status);
        var first = await db.Set<DeadlineClock>().SingleAsync(row => row.TargetId == measure.Id &&
            row.Kind == DeadlineClockKind.FirstSafetyCheck);
        var time = new FixedClock(due.AddMinutes(1));
        var dispatcher = new H6NotificationDispatchRepository(db, time);
        await dispatcher.ObserveClocksAsync(default);
        db.ChangeTracker.Clear();
        var breach = await db.Set<DeadlineBreach>().SingleAsync(row => row.ClockId == first.Id);
        Assert.Equal(due, breach.DueAt);
        var outbox = await db.OutboxMessages.SingleAsync(row => row.Id == breach.Id);
        var fence = Guid.NewGuid(); outbox.AcquireLease("h6:" + fence.ToString("N"), time.GetUtcNow(),
            TimeSpan.FromMinutes(2), 32); await db.SaveChangesAsync();
        var claim = new H6Claim(outbox.Id, outbox.MessageType, outbox.OccurredAtUtc, outbox.PayloadJson,
            fence, outbox.LeaseExpiresAtUtc!.Value, outbox.DeliveryAttemptCount);
        var result = await dispatcher.DispatchAsync(claim,
            H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal("COMMITTED", result.Status);
        Assert.Equal(2, result.Delivered);
        var recipients = await db.Set<H6NotificationDeliveryRow>().Where(row => row.OccurrenceId == result.OccurrenceId)
            .Select(row => row.RecipientUserId).ToArrayAsync();
        Assert.Contains(source.Crew, recipients);
        Assert.Contains(source.Supervisor, recipients);
        Assert.Null((await db.Set<RepairObligation>().SingleAsync(row => row.Id == source.Safety))
            .EffectiveResolutionDecisionId);
    }

    [Fact]
    public async Task ActualFieldMeasurementAdmitsDangerWarningBreachAndLateAcknowledgement()
    {
        var source = await Seed();
        await using var db = sql.CreateDbContext();
        var safety = new RepairSafetyRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var created = await safety.CreateAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            source.Package, source.Item, source.Formal, source.Safety, source.Crew, "each shift",
            "replace if displaced", "remove after formal repair", "approved temporary barrier",
            Guid.NewGuid().ToString(), source.ItemVersion), default);
        Assert.Equal(201, created.Status);
        var measure = Assert.IsType<SafetyFact>(created.Value);
        var due = DateTimeOffset.UtcNow.AddHours(12);
        var installed = await safety.InstallAsync(new(source.Crew, UserRoleCode.RepairCrew, source.Project,
            source.Package, source.Item, measure.Id, due, "barrier installed",
            Guid.NewGuid().ToString(), measure.Version), default);
        Assert.Equal(201, installed.Status);
        var taskId = await db.Set<RepairFieldTaskBinding>().Where(row => row.ItemId == source.Item)
            .Select(row => row.TaskId).SingleAsync();
        var task = await db.FieldInspectionTasks.SingleAsync(row => row.Id == taskId);
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var admission = new FieldAdmissionContext(source.Crew, UserRoleCode.RepairCrew, source.Crew, "DIRECT", true);
        var accepted = await field.ExecuteAsync(new(source.Project, taskId, "accept",
            new FieldTaskActionInput("accept approved repair"), Guid.NewGuid().ToString(),
            Convert.ToBase64String(task.RowVersion), admission), _ => Task.FromResult(true), default);
        Assert.Equal(201, accepted.Status);
        var version = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking()
            .Where(row => row.Id == taskId).Select(row => row.RowVersion).SingleAsync());
        var started = await field.ExecuteAsync(new(source.Project, taskId, "start",
            new FieldStartInput(Guid.NewGuid(), DateTimeOffset.UtcNow), Guid.NewGuid().ToString(),
            version, admission), _ => Task.FromResult(true), default);
        Assert.Equal(201, started.Status);
        var first = await db.Set<FieldTaskStartOrigin>().SingleAsync(row => row.TaskId == taskId);
        var fileId = await VerifiedFieldFile(db, source, taskId);
        version = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking()
            .Where(row => row.Id == taskId).Select(row => row.RowVersion).SingleAsync());
        var submitted = await field.ExecuteAsync(new(source.Project, taskId, "submit",
            new FieldSubmissionInput(Guid.NewGuid(), first.Id, null,
                [new("area", "Area", 0, "KNOWN", null, "AREA", "m²", null, null,
                    "route markers", "manual area gauge", "area measurement")],
                [new(Guid.NewGuid(), fileId, "MEASUREMENT", new string('a', 64), "image/jpeg",
                    DateTimeOffset.UtcNow, null)], new("UNKNOWN", null, null, null), "MEASUREMENT", null, null),
            Guid.NewGuid().ToString(), version, admission), _ => Task.FromResult(true), default);
        Assert.Equal(201, submitted.Status);
        var link = await db.Set<FieldInspectionEvidenceLink>().SingleAsync(row => row.TaskId == taskId);
        var checkTime = DateTimeOffset.UtcNow.AddHours(2);
        var checkRepo = new RepairSafetyRepository(db, new IdempotencyOperationService(db), new FixedClock(checkTime));
        var check = await checkRepo.CheckAsync(new(source.Crew, UserRoleCode.RepairCrew, source.Project,
            source.Package, source.Item, measure.Id, "Danger", "barrier displaced", [link.Id],
            Guid.NewGuid().ToString(), Assert.IsType<SafetyFact>(installed.Value).Version), default);
        Assert.Equal(201, check.Status);
        var checkedFact = Assert.IsType<SafetyFact>(check.Value);
        var warning = Assert.Single(checkedFact.Warnings);
        Assert.Equal(checkTime.AddHours(1), warning.OriginalDueAt);
        var evidence = await db.Set<RepairSafetyCheck>().AsNoTracking()
            .Where(row => row.Id == checkedFact.CurrentCheckId)
            .SelectMany(row => row.Evidence).SingleAsync();
        Assert.Equal(source.Crew, evidence.ActualUploaderId);
        Assert.Equal(link.Id, evidence.SourceId);
        Assert.Equal("FIELD_MEASUREMENT", evidence.SourceKind);
        var warningAction = await db.Set<RepairSafetyActionSource>().SingleAsync(row =>
            row.MeasureId == measure.Id && row.Kind == "WARNING" && row.OriginId == warning.Id);
        var warningOutbox = await db.OutboxMessages.SingleAsync(row => row.Id == warningAction.Id);
        var warningDispatch = await Dispatch(db, new FixedClock(checkTime), warningOutbox);
        Assert.Equal("COMMITTED", warningDispatch.Status);
        Assert.Equal(2, warningDispatch.Delivered);
        var dangerClock = await db.Set<DeadlineClock>().SingleAsync(row => row.Kind ==
            DeadlineClockKind.DangerAcknowledgment && row.OriginEventId == warning.Id);
        var late = new FixedClock(warning.OriginalDueAt.AddMinutes(1));
        await new H6NotificationDispatchRepository(db, late).ObserveClocksAsync(default);
        db.ChangeTracker.Clear();
        var breach = await db.Set<DeadlineBreach>().SingleAsync(row => row.ClockId == dangerClock.Id);
        Assert.Equal(warning.OriginalDueAt, breach.DueAt);
        var breachOutbox = await db.OutboxMessages.SingleAsync(row => row.Id == breach.Id);
        var breached = await Dispatch(db, late, breachOutbox);
        Assert.Equal("COMMITTED", breached.Status);
        Assert.Equal(2, breached.Delivered);
        var ack = await new RepairSafetyRepository(db, new IdempotencyOperationService(db), late)
            .AcknowledgeAsync(new(source.Crew, UserRoleCode.RepairCrew, source.Project,
                source.Package, source.Item, measure.Id, warning.Id, "barrier isolated for replacement",
                Guid.NewGuid().ToString(), checkedFact.Version), default);
        Assert.Equal(201, ack.Status);
        Assert.True(Assert.Single(Assert.IsType<SafetyFact>(ack.Value).Acknowledgements).AfterOriginalDue);
        Assert.Null((await db.Set<RepairObligation>().SingleAsync(row => row.Id == source.Safety))
            .EffectiveResolutionDecisionId);
    }

    private static async Task<Guid> VerifiedFieldFile(RoadGuardDbContext db, Source source, Guid taskId)
    {
        var now = DateTimeOffset.UtcNow;
        var file = StoredFile.Create(Guid.NewGuid(), "field/safety-" + Guid.NewGuid().ToString("N"),
            "barrier.jpg", "image/jpeg", 4, new string('a', 64), source.Crew, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, source.Crew, file.StorageUri,
            "MEASUREMENT", "image/jpeg", 4, new string('a', 64), 8388608, now.AddHours(24));
        upload.StartUploading("fixture", now);
        db.AddRange(file, FileScope.Create(Guid.NewGuid(), file.Id, source.Project, taskId,
            source.Crew, "MEASUREMENT", now), upload);
        await db.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
        await db.SaveChangesAsync();
        upload.MarkVerified();
        await db.SaveChangesAsync();
        return file.Id;
    }

    private static async Task<H6DispatchOutcome> Dispatch(RoadGuardDbContext db, TimeProvider time,
        OutboxMessage outbox)
    {
        var fence = Guid.NewGuid();
        outbox.AcquireLease("h6:" + fence.ToString("N"), time.GetUtcNow(), TimeSpan.FromMinutes(2), 32);
        await db.SaveChangesAsync();
        var claim = new H6Claim(outbox.Id, outbox.MessageType, outbox.OccurredAtUtc, outbox.PayloadJson,
            fence, outbox.LeaseExpiresAtUtc!.Value, outbox.DeliveryAttemptCount);
        return await new H6NotificationDispatchRepository(db, time).DispatchAsync(claim,
            H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc,
                claim.PayloadJson), default);
    }

    private async Task<Source> Seed()
    {
        await using var db = sql.CreateDbContext();
        var actual = await H4GenuineRepairSource.Seed(db, sql);
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
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
