using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Warranties;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Repositories.Implementations.Retention;
using RoadGuardSystem.Services.Implementations.Repairs;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed class H4RepairLifecycleSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly System.Text.Json.JsonSerializerOptions WebJson = new(System.Text.Json.JsonSerializerDefaults.Web);

    [Fact]
    public async Task OfflineRepairBridgeRequiresPersistedSignedAdmissionAndCannotChangeDirectOnlineBehavior()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db); var repo = Repo(db);
        var origin = Guid.NewGuid(); var device = Guid.NewGuid(); var snapshot = Guid.NewGuid();
        var data = new RepairMeasurementAssessmentData(origin, state.First.Id, null, null, null, device);
        var operation = new OfflineOperationData(1, origin, origin, "REPAIR_ASSESSMENT", state.Source.Crew,
            device, snapshot, state.Binding.TaskId, state.Binding.AssignmentId, "AAAAAAAAAAA=", "", [], null,
            Repair: new(state.Item.Id, "assessment", Assessment: data));
        operation = operation with { CorePayloadHash = repo.ComputeCoreHash(operation) };
        Assert.Equal(RepairCommandCoreHash.Assessment(state.Binding.TaskId, state.Item.Id, state.Source.Crew, data), operation.CorePayloadHash);
        var forged = new OfflineFieldAdmissionFacts(Guid.NewGuid(), origin, state.Source.Project, state.Binding.TaskId,
            state.Binding.AssignmentId, state.Source.Crew, device, state.Source.Crew, UserRoleCode.RepairCrew, null,
            "REPAIR_ASSESSMENT", operation.CorePayloadHash, snapshot, null, "UNCERTAIN");
        var command = new OfflineRepairCoreCommand(operation, state.Source.Project, state.Source.Crew,
            UserRoleCode.RepairCrew, forged, Version(db, state.Item));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.ApplyInTransactionAsync(command, default));
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            var rejected = await Assert.ThrowsAsync<OfflineAdmissionRejectedException>(() => repo.ApplyInTransactionAsync(command, default));
            Assert.Equal(403, rejected.Status);
            Assert.False(await db.Set<RepairMeasurementAssessment>().AnyAsync(row => row.ItemId == state.Item.Id));
            await tx.RollbackAsync();
        }
        // A rejected offline call cannot leave a context that relabels later direct commands or fixes their identities.
        var assessment = await repo.AssessAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId, data, Guid.NewGuid().ToString(), Version(db, state.Item)), default);
        Assert.Equal(201, assessment.Status); var assessed = Assert.IsType<RepairAssessmentFact>(assessment.Value);
        Assert.NotEqual(origin, assessed.Id);
        var started = await repo.StartExecutionAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId, new(Guid.NewGuid(), state.First.Id, DateTimeOffset.UtcNow, assessed.Id),
            Guid.NewGuid().ToString(), assessment.Version!), default);
        Assert.Equal(201, started.Status); var start = Assert.IsType<RepairExecutionStartFact>(started.Value);
        var finish = await repo.FinishExecutionAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId, new(Guid.NewGuid(), start.Id, DateTimeOffset.UtcNow),
            Guid.NewGuid().ToString(), started.Version!), default);
        Assert.Equal(201, finish.Status); var finished = Assert.IsType<RepairExecutionFinishFact>(finish.Value);
        Assert.Equal("SERVER_ONLINE", start.TimeProvenance); Assert.Equal(start.ServerReceivedAt, start.VerifiedOriginalAt);
        Assert.Equal("SERVER_ONLINE", finished.TimeProvenance); Assert.Equal(finished.ServerReceivedAt, finished.VerifiedOriginalAt);
        Assert.Equal(finished.VerifiedOriginalAt!.Value.AddHours(24), finished.OriginalSyncDueAt);
    }

    [Fact]
    public async Task OfflineRepairSnapshotUsesActualCurrentBindingPlanAndItemVersionWithoutExecuteGrant()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db); var repo = Repo(db);
        await using var tx = await db.Database.BeginTransactionAsync();
        var snapshot = await repo.ReadSnapshotInTransactionAsync(new(state.Source.Project, state.Binding.TaskId,
            state.Source.Crew, UserRoleCode.RepairCrew), default);
        Assert.NotNull(snapshot); Assert.Equal(state.Item.Id, snapshot.ItemId);
        Assert.Equal(state.Binding.AssignmentId, snapshot.AssignmentId); Assert.Equal(state.Source.Crew, snapshot.OriginalActorId);
        Assert.Equal(Version(db, state.Item), snapshot.ItemVersion); Assert.Null(snapshot.AuthorizationId);
        var payload = System.Text.Json.JsonSerializer.SerializeToElement(snapshot.SafePayload, WebJson);
        Assert.Equal("Normal", payload.GetProperty("mode").GetString());
        Assert.Equal("actual plan", payload.GetProperty("repairPlan").GetString());
        Assert.Equal("UNKNOWN_OWNER_MAPPING", payload.GetProperty("eligibility").GetString());
        Assert.Null(await repo.ReadSnapshotInTransactionAsync(new(state.Source.Project, state.Binding.TaskId,
            state.Source.Pm, UserRoleCode.ProjectManager), default));
        await tx.RollbackAsync();
    }

    [Fact]
    public async Task RejectedUnstartedAssignmentCanContinueNormallyWithoutRewritingRejection()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db, rejectBeforeStart: true);
        var old = await db.FieldInspectionAssignments.AsNoTracking().SingleAsync(row => row.Id == state.Binding.AssignmentId);
        var rejection = await db.Set<FieldInspectionTaskEvent>().AsNoTracking().SingleAsync(row =>
            row.TaskId == state.Binding.TaskId && row.Kind == "REJECTED");
        var result = await Repo(db).ContinueNormallyAsync(new(state.Source.Pm, UserRoleCode.ProjectManager,
            state.Source.Project, state.Package.Id, state.Item.Id,
            new("new normal plan after rejection", "checklist-v2", "reassign through approved normal flow", null),
            Guid.NewGuid().ToString(), Version(db, state.Item)), default);
        Assert.True(result.Status == 201, $"{result.Status}/{result.Code}");
        var fact = Assert.IsType<RepairLifecycleFact>(result.Value); db.ChangeTracker.Clear();
        var retained = await db.FieldInspectionAssignments.AsNoTracking().SingleAsync(row => row.Id == old.Id);
        Assert.Equal(FieldInspectionAssignmentStatus.Rejected, retained.Status);
        Assert.Equal(old.EndedAt, retained.EndedAt); Assert.Equal(old.Reason, retained.Reason);
        Assert.True(await db.Set<FieldInspectionTaskEvent>().AnyAsync(row => row.Id == rejection.Id && row.Kind == "REJECTED"));
        Assert.False(await db.Set<FieldTaskStartOrigin>().AnyAsync(row => row.TaskId == state.Binding.TaskId));
        Assert.Equal(RepairItemState.AwaitingApproval, (await db.Set<RepairItem>().SingleAsync(row => row.Id == fact.SuccessorItemId)).State);
        Assert.Null((await db.Set<RepairObligation>().SingleAsync(row => row.Id == state.Item.ObligationId)).OriginalCrewFirstStartId);
    }

    [Fact]
    public async Task EligibilityReadsActualScopedSourcesWithoutInferringCoverageAndRechecksCurrentRights()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db);
        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        var handover = HandoverDocument.Create(Guid.NewGuid(), state.Source.Project, "actual scoped handover", day,
            state.Source.Pm, null, null);
        var warranty = Warranty.Create(Guid.NewGuid(), state.Source.Project, state.Source.Road, handover.Id, day,
            day, day.AddDays(30), null, WarrantyScope.RoadSection, null, null, WarrantyStatus.Active);
        db.AddRange(handover, warranty); await db.SaveChangesAsync();
        var repository = new RepairEligibilityRepository(db, TimeProvider.System);
        var query = new RepairEligibilityQuery(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project,
            state.Package.Id, state.Item.Id);
        var read = await repository.ReadAsync(query, default);
        Assert.Equal(200, read.Status); Assert.NotNull(read.Value);
        Assert.Equal(warranty.Id, Assert.Single(read.Value.Sources.Warranties).Id);
        var capturedHandover = Assert.Single(read.Value.Sources.Handovers);
        Assert.Equal(handover.Id, capturedHandover.Id); Assert.Equal(state.Source.Pm, capturedHandover.AcceptedByUserId);
        Assert.Equal(Convert.ToBase64String(handover.RowVersion), capturedHandover.RowVersion);
        Assert.Equal(RepairFactState.Unknown, read.Value.Sources.Coverage);
        Assert.Equal(RepairFactState.Unknown, read.Value.Sources.RoadHandover);
        var service = new RepairEligibilityService(repository);
        var crew = await service.ReadAsync(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, default);
        Assert.Equal(200, crew.Status); Assert.False(crew.Value!.Eligible);
        Assert.Equal("OWNER_SOURCE_ACTIVATION_PENDING", crew.Value.Activation);
        Assert.Contains("COVERAGE_MAPPING_UNKNOWN", crew.Value.MissingReasons);
        Assert.Equal(403, (await repository.ReadAsync(query with { Role = UserRoleCode.RepairCrew }, default)).Status);
        Assert.Equal(403, (await repository.ReadAsync(query with { ProjectId = Guid.NewGuid() }, default)).Status);
        var membership = await db.ProjectMembers.SingleAsync(row => row.ProjectId == state.Source.Project && row.UserId == state.Source.Pm);
        membership.ValidTo = new DateOnly(2001, 1, 1); await db.SaveChangesAsync();
        Assert.Equal(403, (await repository.ReadAsync(query, default)).Status);
    }

    [Fact]
    public async Task ActualAssessmentEvidenceRemainsInRetentionInventoryAfterBindingRetirement()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db); var now = DateTimeOffset.UtcNow;
        var checksum = new string('d', 64);
        var file = StoredFile.Create(Guid.NewGuid(), "field/lifecycle-before-" + Guid.NewGuid().ToString("N"),
            "before.jpg", "image/jpeg", 4, checksum, state.Source.Crew, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, state.Source.Crew, file.StorageUri,
            "BEFORE", "image/jpeg", 4, checksum, 8388608, now.AddHours(24));
        upload.StartUploading("fixture", now);
        db.AddRange(file, FileScope.Create(Guid.NewGuid(), file.Id, state.Source.Project, state.Binding.TaskId,
            state.Source.Crew, "BEFORE", now), upload);
        await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
        await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        var assessed = await Repo(db).AssessAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId, new(Guid.NewGuid(), state.First.Id, null,
                [new(Guid.NewGuid(), file.Id, "BEFORE", checksum, "image/jpeg", now, null)], null),
            Guid.NewGuid().ToString(), Version(db, state.Item)), default);
        Assert.Equal(201, assessed.Status);
        var inventory = new Huy02InspectionRetentionContributor(db);
        var before = await inventory.ReadAsync(file.Id, default);
        Assert.True(before.Complete);
        var reference = Assert.Single(before.References.Where(row => row.Kind == "REPAIR_ASSESSMENT"));
        Assert.Equal(state.Source.Project, reference.ProjectId);
        var cancelled = await Repo(db).CancelItemAsync(Cancel(db, state, Handover(state)), default);
        Assert.Equal(201, cancelled.Status); db.ChangeTracker.Clear();
        var after = await inventory.ReadAsync(file.Id, default);
        Assert.True(after.Complete);
        var retained = Assert.Single(after.References.Where(row => row.Kind == "REPAIR_ASSESSMENT"));
        Assert.Equal(reference, retained);
        Assert.Contains(file.Id, await inventory.KnownProjectFilesAsync(state.Source.Project, default));
        Assert.Empty(await inventory.KnownProjectFilesAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task StartedMeasurementRequiresActualHandoverBeforeRetiringRepairBinding()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db);
        var result = await Repo(db).CancelItemAsync(Cancel(db, state, null), default);
        Assert.Equal(400, result.Status); Assert.Equal("handover_facts_required", result.Code);
        db.ChangeTracker.Clear();
        var item = await db.Set<RepairItem>().SingleAsync(row => row.Id == state.Item.Id);
        Assert.Equal(state.Binding.Id, item.CurrentBindingId);
        Assert.Equal(RepairItemState.Assigned, item.State);
        Assert.False(await db.Set<RepairItemLifecycleEvent>().AnyAsync(row => row.ItemId == item.Id && row.Kind == "CANCELLED"));
    }

    [Fact]
    public async Task CancellationRetainsPerformedFactsAndObligationAndProtectedReplay()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db);
        var command = Cancel(db, state, Handover(state));
        var result = await Repo(db).CancelItemAsync(command, default);
        Assert.True(result.Status == 201, $"{result.Status}/{result.Code}");
        var fact = Assert.IsType<RepairLifecycleFact>(result.Value);
        db.ChangeTracker.Clear();
        var item = await db.Set<RepairItem>().SingleAsync(row => row.Id == state.Item.Id);
        Assert.Equal(RepairItemState.Cancelled, item.State); Assert.Null(item.CurrentBindingId);
        Assert.Equal("measurement retained; no repair claimed", item.Handover!.PerformedScope);
        Assert.Equal(state.Source.Crew, item.Handover.FromActorId); Assert.Equal(state.Source.Pm, item.Handover.ToActorId);
        Assert.NotNull((await db.FieldInspectionAssignments.SingleAsync(row => row.Id == state.Binding.AssignmentId)).EndedAt);
        Assert.Equal(FieldInspectionTaskStatus.Cancelled, (await db.FieldInspectionTasks.SingleAsync(row => row.Id == state.Binding.TaskId)).Status);
        var obligation = await db.Set<RepairObligation>().SingleAsync(row => row.Id == item.ObligationId);
        Assert.False(obligation.IsResolved); Assert.Equal(state.First.Id, obligation.OriginalCrewFirstStartId);
        Assert.Equal(item.Id, obligation.CurrentRepairItemId);
        Assert.True(await db.Set<FieldInspectionTaskEvent>().AnyAsync(row => row.Id == fact.HandoverEventId && row.Kind == "CANCELLED"));
        Assert.True(await db.Set<RepairFieldTaskBinding>().AnyAsync(row => row.Id == state.Binding.Id));
        var replay = await Repo(db).CancelItemAsync(command, default);
        Assert.Equal(200, replay.Status); Assert.True(replay.Replayed); Assert.Equal(result.Version, replay.Version);
        var forbiddenOldBinding = await Repo(db).AssessAsync(new(state.Source.Crew, UserRoleCode.RepairCrew,
            state.Source.Project, state.Package.Id, item.Id, state.Binding.TaskId,
            new(Guid.NewGuid(), state.First.Id, null, null, null), Guid.NewGuid().ToString(), result.Version!), default);
        Assert.Equal(403, forbiddenOldBinding.Status); Assert.Equal("repair_binding_not_current", forbiddenOldBinding.Code);
        var member = await db.ProjectMembers.SingleAsync(row => row.ProjectId == state.Source.Project && row.UserId == state.Source.Pm);
        member.ValidTo = new DateOnly(2001, 1, 1); await db.SaveChangesAsync();
        Assert.Equal(403, (await Repo(db).CancelItemAsync(command, default)).Status);
    }

    [Fact]
    public async Task NormalContinuationRetainsOriginalStartAndRequiresNewApprovalBeforeAssignment()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db);
        var result = await Repo(db).ContinueNormallyAsync(Continue(db, state), default);
        Assert.True(result.Status == 201, $"{result.Status}/{result.Code}");
        var fact = Assert.IsType<RepairLifecycleFact>(result.Value);
        db.ChangeTracker.Clear();
        var successor = await db.Set<RepairItem>().SingleAsync(row => row.Id == fact.SuccessorItemId);
        var old = await db.Set<RepairItem>().SingleAsync(row => row.Id == state.Item.Id);
        Assert.Equal(RepairMode.Normal, successor.Mode); Assert.Equal(RepairItemState.AwaitingApproval, successor.State);
        Assert.Null(successor.ApprovedBy); Assert.Equal(old.Id, successor.PredecessorItemId);
        Assert.Equal(successor.Id, old.SupersededByItemId); Assert.Equal(RepairItemState.Cancelled, old.State);
        var obligation = await db.Set<RepairObligation>().SingleAsync(row => row.Id == old.ObligationId);
        Assert.Equal(successor.Id, obligation.CurrentRepairItemId); Assert.Equal(state.First.Id, obligation.OriginalCrewFirstStartId);
        Assert.False(obligation.IsResolved); Assert.Equal(state.Source.Defect, successor.DefectId);
        var link = await db.Set<RepairNormalSuccessor>().SingleAsync(row => row.Id == fact.ContinuationId);
        Assert.Equal(fact.CancellationEventId, link.SourceCancellationEventId); Assert.Equal(fact.HandoverEventId, link.SourceHandoverEventId);
        Assert.True(await db.Set<DeadlineClock>().AnyAsync(row => row.TargetId == successor.Id && row.Kind == DeadlineClockKind.SupervisorInitialApproval));
        Assert.False(await db.Set<RepairExecutionAuthorization>().AnyAsync(row => row.DefectId == state.Source.Defect));
        var assign = new RepairItemAssignCommand(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project,
            state.Package.Id, successor.Id, await Assignment(db, state.Source), Guid.NewGuid().ToString(), fact.SuccessorVersion!);
        Assert.Equal(409, (await Repo(db).AssignItemAsync(assign, default)).Status);
        var approved = await Repo(db).ApproveItemAsync(new(state.Source.Supervisor, UserRoleCode.Supervisor,
            state.Source.Project, state.Package.Id, successor.Id, new("approve continuation plan"), Guid.NewGuid().ToString(),
            fact.SuccessorVersion!), default);
        Assert.Equal(201, approved.Status);
        var assigned = await Repo(db).AssignItemAsync(assign with { ExpectedItemVersion = approved.Version! }, default);
        Assert.True(assigned.Status == 201, $"{assigned.Status}/{assigned.Code}");
        var binding = Assert.IsType<RepairTaskBindingFact>(assigned.Value);
        Assert.NotEqual(state.Binding.TaskId, binding.TaskId); Assert.Null(binding.AuthorizationId);
    }

    [Fact]
    public async Task InvalidSuccessorPlanRollsBackEarlierCancellationAndAllRetirementFacts()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db);
        var command = Continue(db, state);
        command = command with { Input = command.Input with { RepairPlan = "" } };
        var result = await Repo(db).ContinueNormallyAsync(command, default);
        Assert.Equal(400, result.Status); db.ChangeTracker.Clear();
        var item = await db.Set<RepairItem>().SingleAsync(row => row.Id == state.Item.Id);
        Assert.Equal(RepairItemState.Assigned, item.State); Assert.Equal(state.Binding.Id, item.CurrentBindingId);
        Assert.Null(item.Handover); Assert.Null(item.SupersededByItemId);
        Assert.Null((await db.FieldInspectionAssignments.SingleAsync(row => row.Id == state.Binding.AssignmentId)).EndedAt);
        Assert.False(await db.Set<RepairNormalSuccessor>().AnyAsync(row => row.SourceItemId == item.Id));
        Assert.False(await db.Set<RepairItemLifecycleEvent>().AnyAsync(row => row.ItemId == item.Id && row.Kind == "CANCELLED"));
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.IdempotencyKey == command.Key));
    }

    [Fact]
    public async Task ArbitraryHandoverRecipientCannotAppointAReplacementCrew()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db);
        var result = await Repo(db).CancelItemAsync(Cancel(db, state,
            Handover(state) with { RecipientUserId = state.Source.Supervisor }), default);
        Assert.Equal(400, result.Status); Assert.Equal("handover_facts_required", result.Code);
    }

    [Fact]
    public async Task InProgressCancellationRetainsActualExecutionAndPerformedHandover()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db);
        var assessed = await Repo(db).AssessAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId, new(Guid.NewGuid(), state.First.Id, null, null, null),
            Guid.NewGuid().ToString(), Version(db, state.Item)), default);
        Assert.Equal(201, assessed.Status); var assessment = Assert.IsType<RepairAssessmentFact>(assessed.Value);
        var started = await Repo(db).StartExecutionAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(Guid.NewGuid(), state.First.Id, DateTimeOffset.UtcNow, assessment.Id), Guid.NewGuid().ToString(), assessed.Version!), default);
        Assert.Equal(201, started.Status); var start = Assert.IsType<RepairExecutionStartFact>(started.Value);
        var result = await Repo(db).CancelItemAsync(Cancel(db, state,
            Handover(state) with { PerformedPortion = "first two meters repaired; rest stopped" }), default);
        Assert.True(result.Status == 201, $"{result.Status}/{result.Code}"); db.ChangeTracker.Clear();
        var item = await db.Set<RepairItem>().SingleAsync(row => row.Id == state.Item.Id);
        Assert.Equal(RepairItemState.Cancelled, item.State); Assert.Equal(start.Id, item.CurrentExecutionStartId);
        Assert.Equal("first two meters repaired; rest stopped", item.Handover!.PerformedScope);
        Assert.True(await db.Set<RepairExecutionStart>().AnyAsync(row => row.Id == start.Id));
        Assert.False(await db.Set<RepairAttempt>().AnyAsync(row => row.ItemId == item.Id));
        Assert.False(await db.Set<RepairObligation>().Where(row => row.Id == item.ObligationId)
            .AnyAsync(row => row.EffectiveResolutionDecisionId != null));
    }

    [Fact]
    public async Task FinishedWorkCannotBeRetiredBeforeItsOriginalIntakeAndReview()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db);
        var assessed = await Repo(db).AssessAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId, new(Guid.NewGuid(), state.First.Id, null, null, null),
            Guid.NewGuid().ToString(), Version(db, state.Item)), default);
        Assert.Equal(201, assessed.Status); var assessment = Assert.IsType<RepairAssessmentFact>(assessed.Value);
        var started = await Repo(db).StartExecutionAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(Guid.NewGuid(), state.First.Id, DateTimeOffset.UtcNow, assessment.Id), Guid.NewGuid().ToString(), assessed.Version!), default);
        Assert.Equal(201, started.Status); var start = Assert.IsType<RepairExecutionStartFact>(started.Value);
        var finished = await Repo(db).FinishExecutionAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId, new(Guid.NewGuid(), start.Id, DateTimeOffset.UtcNow),
            Guid.NewGuid().ToString(), started.Version!), default);
        Assert.Equal(201, finished.Status);
        var result = await Repo(db).CancelItemAsync(Cancel(db, state, Handover(state)), default);
        Assert.Equal(409, result.Status); Assert.Equal("submitted_or_finished_history_requires_intake_review", result.Code);
        db.ChangeTracker.Clear(); var item = await db.Set<RepairItem>().SingleAsync(row => row.Id == state.Item.Id);
        Assert.Equal(state.Binding.Id, item.CurrentBindingId); Assert.NotNull(item.CurrentExecutionFinishId);
        Assert.True(await db.Set<DeadlineClock>().AnyAsync(row => row.TargetId == item.Id && row.Kind == DeadlineClockKind.FinishedDataSync));
    }

    private static RepairLifecycleHandoverData Handover(Ready state) => new(state.First.Id, state.Source.Pm,
        "measurement retained; no repair claimed", "barriers retained; area safe");
    private static RepairCancellationCommand Cancel(RoadGuardDbContext db, Ready state, RepairLifecycleHandoverData? handover)
        => new(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project, state.Package.Id,
            state.Item.Id, new("cancel unfinished work", handover), Guid.NewGuid().ToString(), Version(db, state.Item));
    private static RepairNormalContinuationCommand Continue(RoadGuardDbContext db, Ready state)
        => new(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project, state.Package.Id, state.Item.Id,
            new("new actual normal plan", "checklist-v2", "continue same obligation normally", Handover(state)),
            Guid.NewGuid().ToString(), Version(db, state.Item));
    private sealed record Ready(H4GenuineRepairSource.Source Source, RepairPackage Package, RepairItem Item,
        RepairFieldTaskBinding Binding, FieldTaskStartOrigin First);
    private async Task<Ready> Seed(RoadGuardDbContext db, bool rejectBeforeStart = false)
    {
        var source = await H4GenuineRepairSource.Seed(db, sql); var repo = Repo(db);
        var assignment = await Assignment(db, source); var defectVersion = assignment.Task.DefectVersion;
        var created = await repo.CreatePackageAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            new(source.Defect, defectVersion, [new("FORMAL_REPAIR", true,
                new(source.Road, source.Route, source.Set, null, null, 1, 2, 0, 1), "mandatory repair")], "package"),
            Guid.NewGuid().ToString(), defectVersion), default);
        Assert.Equal(201, created.Status); var packageFact = Assert.IsType<RepairPackageFact>(created.Value);
        var package = await db.Set<RepairPackage>().Include(row => row.Obligations).SingleAsync(row => row.Id == packageFact.Id);
        var proposed = await repo.ProposeItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, package.Id,
            new(package.Obligations[0].Id, "NORMAL", "actual plan", "checklist-v1", "proposal"), Guid.NewGuid().ToString(), created.Version!), default);
        Assert.Equal(201, proposed.Status); var itemFact = Assert.IsType<RepairItemFact>(proposed.Value);
        var approved = await repo.ApproveItemAsync(new(source.Supervisor, UserRoleCode.Supervisor, source.Project,
            package.Id, itemFact.Id, new("approve plan"), Guid.NewGuid().ToString(), proposed.Version!), default);
        Assert.Equal(201, approved.Status);
        var assigned = await repo.AssignItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, package.Id,
            itemFact.Id, assignment, Guid.NewGuid().ToString(), approved.Version!), default);
        Assert.Equal(201, assigned.Status); var bound = Assert.IsType<RepairTaskBindingFact>(assigned.Value);
        var task = await db.FieldInspectionTasks.SingleAsync(row => row.Id == bound.TaskId);
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var admission = new FieldAdmissionContext(source.Crew, UserRoleCode.RepairCrew, source.Crew, "DIRECT", true);
        if (rejectBeforeStart)
        {
            var rejected = await field.ExecuteAsync(new(source.Project, task.Id, "reject", new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskActionInputFact("actual Crew rejects before starting"),
                Guid.NewGuid().ToString(), Convert.ToBase64String(task.RowVersion), admission), _ => Task.FromResult(true), default);
            Assert.Equal(201, rejected.Status);
            var rejectedItem = await db.Set<RepairItem>().SingleAsync(row => row.Id == itemFact.Id);
            var rejectedBinding = await db.Set<RepairFieldTaskBinding>().SingleAsync(row => row.Id == rejectedItem.CurrentBindingId);
            return new(source, package, rejectedItem, rejectedBinding, null!); // This fixture branch deliberately has no first-start source.
        }
        var accepted = await field.ExecuteAsync(new(source.Project, task.Id, "accept", new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskActionInputFact("accept"),
            Guid.NewGuid().ToString(), Convert.ToBase64String(task.RowVersion), admission), _ => Task.FromResult(true), default);
        Assert.True(accepted.Status == 201, $"Native accept expected 201, actual {accepted.Status}/{accepted.Code}.");
        var currentNativeVersion = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking()
            .Where(row => row.Id == task.Id).Select(row => row.RowVersion).SingleAsync());
        var started = await field.ExecuteAsync(new(source.Project, task.Id, "start", new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldStartInputFact(Guid.NewGuid(), DateTimeOffset.UtcNow),
            Guid.NewGuid().ToString(), currentNativeVersion, admission), _ => Task.FromResult(true), default);
        Assert.True(started.Status == 201, $"Native start expected 201, actual {started.Status}/{started.Code}.");
        var item = await db.Set<RepairItem>().SingleAsync(row => row.Id == itemFact.Id);
        var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(row => row.Id == item.CurrentBindingId);
        var first = await db.Set<FieldTaskStartOrigin>().SingleAsync(row => row.TaskId == task.Id);
        return new(source, package, item, binding, first);
    }
    private static async Task<RepairItemAssignData> Assignment(RoadGuardDbContext db, H4GenuineRepairSource.Source source)
    {
        var version = Convert.ToBase64String(await db.Defects.Where(row => row.Id == source.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        return new(new(source.Defect, version, null, "REPORTER", source.Route, source.Set, null, null, "POST_REPAIR", 1,
            "{}", null, source.Crew, DateTimeOffset.UtcNow.AddDays(1)), null, "assign actual current Crew");
    }
    private static RepairWorkflowRepository Repo(RoadGuardDbContext db) => new(db, new IdempotencyOperationService(db), TimeProvider.System);
    private static string Version<T>(RoadGuardDbContext db, T entity) where T : class
        => Convert.ToBase64String((db.Entry(entity).GetDatabaseValues()
            ?? throw new InvalidOperationException("Fixture source row no longer exists.")).GetValue<byte[]>("RowVersion"));
}
