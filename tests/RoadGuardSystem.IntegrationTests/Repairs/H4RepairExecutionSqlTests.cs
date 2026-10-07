using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Reporting;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.Repositories.Exports;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Implementations.Reporting;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Exports;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed class H4RepairExecutionSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task BareSameActorSyncCannotReplayAFieldOriginWithoutPersistedSignedAdmission()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var key = Guid.NewGuid().ToString();
        var result = await field.ExecuteAsync(new(state.Source.Project, state.Binding.TaskId, "start",
            new FieldStartInput(state.FirstStart.OriginId, state.FirstStart.ClaimedAt, state.FirstStart.DeviceId), key,
            "AAAAAAAAAAA=", new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Crew, "SYNC", false)),
            _ => Task.FromResult(true), default);
        Assert.Equal(403, result.Status); Assert.Equal("offline_admission_required", result.Code);
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == state.Source.Crew && row.IdempotencyKey == key));
    }
    [Fact]
    public async Task CallerOwnedFieldCoreExposesOnlyKnownFiniteAdmissionRejectionType()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var error = await Assert.ThrowsAsync<FieldCoreRejectedException>(() => field.ApplyInTransactionAsync(
            new(state.Source.Project, state.Binding.TaskId, "start", new FieldStartInput(Guid.NewGuid(), DateTimeOffset.UtcNow),
                null, "AAAAAAAAAAA=", new(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Pm, "DIRECT", true)),
            _ => Task.FromResult(true), default));
        Assert.Equal(403, error.Result.Status); Assert.Equal("access_forbidden", error.Result.Code);
        await transaction.RollbackAsync();
    }
    [Theory]
    [InlineData("cancel")]
    [InlineData("reassign")]
    public async Task GenericFieldMutationCannotBypassActualRepairBindingAndHandoverLifecycle(string action)
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var taskVersion = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking().Where(row => row.Id == state.Binding.TaskId)
            .Select(row => row.RowVersion).SingleAsync());
        var nextCrew = action == "reassign" ? state.Source.Crew : (Guid?)null;
        var input = new FieldTaskActionInput("generic route must not retire repair binding", nextCrew,
            new("NONE", "actual first measurement retained", state.FirstStart.Id, [], nextCrew));
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var result = await field.ExecuteAsync(new(state.Source.Project, state.Binding.TaskId, action, input,
            Guid.NewGuid().ToString(), taskVersion,
            new(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Pm, "DIRECT", true)), _ => Task.FromResult(true), default);
        Assert.Equal(409, result.Status); Assert.Equal("repair_binding_required", result.Code);
        db.ChangeTracker.Clear();
        var current = await db.FieldInspectionAssignments.SingleAsync(row => row.FieldInspectionTaskId == state.Binding.TaskId && row.EndedAt == null);
        Assert.Equal(state.Binding.AssignmentId, current.Id);
        Assert.Equal(RepairItemState.Assigned, (await db.Set<RepairItem>().SingleAsync(row => row.Id == state.Item.Id)).State);
    }
    [Fact]
    public async Task NativeBoundFirstStartPinsOriginalObligationImmediatelyBeforeAnyAssessment()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var obligation = await db.Set<RepairObligation>().AsNoTracking().SingleAsync(row => row.Id == state.Item.ObligationId);
        Assert.Equal(state.FirstStart.Id, obligation.OriginalCrewFirstStartId);
        Assert.False(await db.Set<RepairMeasurementAssessment>().AnyAsync(row => row.ItemId == state.Item.Id));
        Assert.False(await db.Set<DeadlineClock>().AnyAsync(row => row.TargetId == state.Item.Id && row.Kind == DeadlineClockKind.FastTrackExecution));
    }
    [Fact]
    public async Task ActualNormalExecutionPinsAssessmentAndPreservesClaimSeparateFromVerifiedServerEvent()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var assessed = await Assess(db, state); Assert.Equal(201, assessed.Status);
        var assessment = Assert.IsType<RepairAssessmentFact>(assessed.Value);
        var claim = DateTimeOffset.UtcNow.AddDays(-7);
        var started = await Repo(db).StartExecutionAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId, new(Guid.NewGuid(), state.FirstStart.Id, claim, assessment.Id),
            Guid.NewGuid().ToString(), assessed.Version!), default);
        Assert.Equal(201, started.Status); db.ChangeTracker.Clear();
        var actual = await db.Set<RepairExecutionStart>().SingleAsync(row => row.ItemId == state.Item.Id);
        Assert.Equal(assessment.Id, actual.AssessmentId); Assert.Equal(assessment.ContentHash, actual.AssessmentContentHash);
        Assert.Equal(claim, actual.ClaimedAt); Assert.Equal(actual.ServerReceivedAt, actual.VerifiedOriginalAt);
        Assert.Equal(state.FirstStart.Id, actual.FirstStartId);
        var item = await db.Set<RepairItem>().SingleAsync(row => row.Id == state.Item.Id);
        Assert.Equal(RepairItemState.InProgress, item.State); Assert.Equal(actual.VerifiedOriginalAt, item.StartedAt);
        Assert.Equal(actual.Id, item.CurrentExecutionStartId);
        Assert.Equal(state.FirstStart.Id, (await db.Set<RepairObligation>().SingleAsync(row => row.Id == item.ObligationId)).OriginalCrewFirstStartId);
    }
    [Fact]
    public async Task ActualFinishCreatesExactlyOneSyncClockFromServerFinishAndNoFormalReviewClock()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var assessed = await Assess(db, state); Assert.Equal(201, assessed.Status);
        var assessment = Assert.IsType<RepairAssessmentFact>(assessed.Value);
        var started = await Repo(db).StartExecutionAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(Guid.NewGuid(), state.FirstStart.Id, DateTimeOffset.UtcNow, assessment.Id), Guid.NewGuid().ToString(), assessed.Version!), default);
        Assert.Equal(201, started.Status);
        var actualStart = await db.Set<RepairExecutionStart>().AsNoTracking().SingleAsync(row => row.ItemId == state.Item.Id);
        var input = new RepairExecutionFinishData(Guid.NewGuid(), actualStart.Id, DateTimeOffset.UtcNow.AddDays(-5));
        var command = new RepairExecutionFinishCommand(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId, input, Guid.NewGuid().ToString(), started.Version!);
        var finished = await Repo(db).FinishExecutionAsync(command, default); Assert.Equal(201, finished.Status);
        var replay = await Repo(db).FinishExecutionAsync(command, default); Assert.Equal(200, replay.Status);
        db.ChangeTracker.Clear(); var actualFinish = await db.Set<RepairExecutionFinish>().SingleAsync(row => row.ItemId == state.Item.Id);
        Assert.Equal(input.ClaimedAt, actualFinish.ClaimedAt); Assert.Equal(actualFinish.ServerReceivedAt, actualFinish.VerifiedOriginalAt);
        var clock = Assert.Single(await db.Set<DeadlineClock>().Where(row => row.TargetId == state.Item.Id && row.Kind == DeadlineClockKind.FinishedDataSync).ToArrayAsync());
        Assert.Equal(actualFinish.Id, clock.OriginEventId); Assert.Equal(actualFinish.ServerReceivedAt, clock.OriginAt);
        Assert.Equal(clock.OriginAt.AddHours(24), clock.OriginalDueAt);
        Assert.False(await db.Set<DeadlineClock>().AnyAsync(row => row.TargetId == state.Binding.TaskId && row.Kind == DeadlineClockKind.ProjectManagerReview));
        Assert.False(await db.Set<FieldInspectionSubmission>().AnyAsync(row => row.TaskId == state.Binding.TaskId));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VerifiedNormalFinishBreachUsesCurrentCrewRightsAndReplaysOnce(bool revokeCrewMembership)
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var assessed = await Assess(db, state); Assert.Equal(201, assessed.Status);
        var assessment = Assert.IsType<RepairAssessmentFact>(assessed.Value);
        var started = await Repo(db).StartExecutionAsync(new(state.Source.Crew, UserRoleCode.RepairCrew,
            state.Source.Project, state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(Guid.NewGuid(), state.FirstStart.Id, DateTimeOffset.UtcNow, assessment.Id),
            Guid.NewGuid().ToString(), assessed.Version!), default);
        Assert.Equal(201, started.Status);
        var start = await db.Set<RepairExecutionStart>().AsNoTracking().SingleAsync(row => row.ItemId == state.Item.Id);
        var finished = await Repo(db).FinishExecutionAsync(new(state.Source.Crew, UserRoleCode.RepairCrew,
            state.Source.Project, state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(Guid.NewGuid(), start.Id, DateTimeOffset.UtcNow.AddDays(-2)), Guid.NewGuid().ToString(),
            started.Version!), default);
        Assert.Equal(201, finished.Status); db.ChangeTracker.Clear();
        var finish = await db.Set<RepairExecutionFinish>().AsNoTracking().SingleAsync(row => row.ItemId == state.Item.Id);
        var clock = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(row => row.TargetId == state.Item.Id &&
            row.Kind == DeadlineClockKind.FinishedDataSync);
        Assert.Equal(state.Source.Project, clock.ProjectId);
        Assert.Equal(finish.Id, clock.OriginEventId);
        Assert.Equal(finish.VerifiedOriginalAt, clock.OriginAt);
        Assert.Equal(finish.ServerReceivedAt, finish.VerifiedOriginalAt);
        Assert.Equal(clock.OriginAt.AddHours(24), clock.OriginalDueAt);
        Assert.Equal(state.Source.Crew, finish.OriginalActorId);
        Assert.Equal(state.Binding.Id, finish.BindingId);
        Assert.Equal(finish.Id, (await db.Set<RepairItem>().AsNoTracking().SingleAsync(row => row.Id == state.Item.Id)).CurrentExecutionFinishId);
        var now = clock.OriginalDueAt.AddMinutes(1);
        var dispatcher = new H6NotificationDispatchRepository(db, new FixedClock(now));
        Assert.Equal(1, await dispatcher.ObserveClocksAsync(default));
        Assert.Equal(0, await dispatcher.ObserveClocksAsync(default));
        db.ChangeTracker.Clear();
        var breach = await db.Set<DeadlineBreach>().AsNoTracking().SingleAsync(row => row.ClockId == clock.Id);
        Assert.Equal(clock.OriginalDueAt, breach.DueAt);
        var message = await db.OutboxMessages.SingleAsync(row => row.Id == breach.Id);
        Assert.Equal("deadline.breached.v1", message.MessageType);
        var plan = H6NotificationCatalog.Parse(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson);
        Assert.Equal(clock.Id, plan.Source.SourceId);
        Assert.Equal(breach.Id, plan.Source.OriginEventId);
        if (revokeCrewMembership)
        {
            var member = await db.ProjectMembers.SingleAsync(row => row.ProjectId == state.Source.Project &&
                row.UserId == state.Source.Crew);
            member.Status = ProjectMemberStatus.Ended;
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            message = await db.OutboxMessages.SingleAsync(row => row.Id == breach.Id);
        }
        var fence = Guid.NewGuid();
        message.AcquireLease("h6:" + fence.ToString("N"), now, TimeSpan.FromMinutes(2), 32);
        await db.SaveChangesAsync();
        var claim = new H6Claim(message.Id, message.MessageType, message.OccurredAtUtc,
            message.PayloadJson, fence, message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
        var result = await dispatcher.DispatchAsync(claim, plan, default);
        Assert.Equal("COMMITTED", result.Status);
        Assert.Equal(revokeCrewMembership ? 1 : 2, result.Delivered);
        Assert.Equal(revokeCrewMembership ? 1 : 0, result.Unresolved);
        Assert.Equal("COMMITTED", (await dispatcher.DispatchAsync(claim, plan, default)).Status);
        Assert.Equal(revokeCrewMembership ? 0 : 1, await db.Notifications.CountAsync(row =>
            row.SourceEntityId == clock.Id && row.RecipientUserId == state.Source.Crew));
        Assert.Equal(1, await db.Notifications.CountAsync(row =>
            row.SourceEntityId == clock.Id && row.RecipientUserId == state.Source.Supervisor));
        Assert.Equal(1, await db.Set<H6NotificationDeliveryRow>().CountAsync(row => row.RecipientUserId == state.Source.Crew));
    }
    [Fact]
    public Task IncompleteFormalFieldIntakePinsOneAttemptAndOriginalPmReviewClock()
        => RunNormalReviewChain(false, false);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task AcceptedPmReviewFinalDeadlineBreachUsesCurrentSupervisorAndReplaysOnce(bool revokeSupervisorMembership)
        => RunNormalReviewChain(true, revokeSupervisorMembership);

    private async Task RunNormalReviewChain(bool observeFinalBreach, bool revokeSupervisorMembership)
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var assessed = await Assess(db, state); var assessment = Assert.IsType<RepairAssessmentFact>(assessed.Value);
        var started = await Repo(db).StartExecutionAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(Guid.NewGuid(), state.FirstStart.Id, DateTimeOffset.UtcNow, assessment.Id),
            Guid.NewGuid().ToString(), assessed.Version!), default);
        Assert.Equal(201, started.Status);
        var physicalStart = await db.Set<RepairExecutionStart>().AsNoTracking().SingleAsync(row => row.ItemId == state.Item.Id);
        var finished = await Repo(db).FinishExecutionAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(Guid.NewGuid(), physicalStart.Id, DateTimeOffset.UtcNow), Guid.NewGuid().ToString(), started.Version!), default);
        Assert.Equal(201, finished.Status); db.ChangeTracker.Clear();
        var taskVersion = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking()
            .Where(row => row.Id == state.Binding.TaskId).Select(row => row.RowVersion).SingleAsync());
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var submitted = await field.ExecuteAsync(new(state.Source.Project, state.Binding.TaskId, "submit",
            new FieldSubmissionInput(Guid.NewGuid(), state.FirstStart.Id, null, [], [], null, "REPAIR_CLAIM", true, null),
            Guid.NewGuid().ToString(), taskVersion,
            new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Crew, "DIRECT", true)),
            _ => Task.FromResult(true), default);
        Assert.Equal(201, submitted.Status); db.ChangeTracker.Clear();
        var intake = await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleAsync(row => row.TaskId == state.Binding.TaskId);
        Assert.Equal("INCOMPLETE", intake.Readiness);
        var reviewClock = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(row =>
            row.TargetId == state.Binding.TaskId && row.Kind == DeadlineClockKind.ProjectManagerReview);
        var command = new RepairAttemptSubmitCommand(state.Source.Crew, UserRoleCode.RepairCrew,
            state.Source.Project, state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(intake.Id, intake.ContentHash, (await db.Set<RepairExecutionFinish>().AsNoTracking()
                .SingleAsync(row => row.ItemId == state.Item.Id)).Id), Guid.NewGuid().ToString(),
            Convert.ToBase64String(await db.Set<RepairItem>().AsNoTracking().Where(row => row.Id == state.Item.Id)
                .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync()));
        Assert.Equal(403, (await Repo(db).SubmitAttemptAsync(command with
        {
            ActorId = state.Source.Pm,
            Role = UserRoleCode.ProjectManager
        }, default)).Status);
        var linked = await Repo(db).SubmitAttemptAsync(command, default);
        Assert.Equal(201, linked.Status); db.ChangeTracker.Clear();
        Assert.Equal(200, (await Repo(db).SubmitAttemptAsync(command, default)).Status);
        Assert.Equal(409, (await Repo(db).SubmitAttemptAsync(command with
        {
            Key = Guid.NewGuid().ToString(),
            Input = command.Input with { ExpectedContentHash = new string('f', 64) }
        }, default)).Status);
        var attempt = await db.Set<RepairAttempt>().AsNoTracking().SingleAsync(row => row.ItemId == state.Item.Id);
        var link = await db.Set<RepairAttemptSubmissionLink>().AsNoTracking().SingleAsync(row => row.AttemptId == attempt.Id);
        var submittedEvent = await db.Set<RepairItemLifecycleEvent>().AsNoTracking().SingleAsync(row =>
            row.ItemId == state.Item.Id && row.Kind == "SUBMITTED");
        var message = await db.OutboxMessages.SingleAsync(row => row.Id == submittedEvent.Id &&
            row.MessageType == "repair.work.submitted.v1");
        var fence = Guid.NewGuid();
        message.AcquireLease("h6:" + fence.ToString("N"), DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(2), 32);
        await db.SaveChangesAsync();
        var claim = new H6Claim(message.Id, message.MessageType, message.OccurredAtUtc,
            message.PayloadJson, fence, message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
        var dispatched = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim,
            H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal("COMMITTED", dispatched.Status);
        Assert.Equal(1, dispatched.Delivered);
        Assert.Equal(state.Source.Pm, (await db.Notifications.SingleAsync(row =>
            row.SourceEntityId == state.Item.Id)).RecipientUserId);
        db.ChangeTracker.Clear();
        Assert.Equal(intake.Id, link.SubmissionId); Assert.Equal(intake.Id, link.FormalRootSubmissionId);
        Assert.Equal(reviewClock.Id, link.ReviewClockId); Assert.Equal(reviewClock.OriginalDueAt, link.OriginalReviewDueAt);
        Assert.Equal(intake.ServerReceivedAt, reviewClock.OriginAt);
        Assert.Equal(RepairItemState.Submitted, (await db.Set<RepairItem>().SingleAsync(row => row.Id == state.Item.Id)).State);
        Assert.Empty(attempt.Evidence); Assert.True(attempt.Performed);
        Assert.Equal(1, await db.Set<RepairAttempt>().CountAsync(row => row.ItemId == state.Item.Id));
        Assert.Equal(1, await db.Set<DeadlineClock>().CountAsync(row => row.TargetId == state.Binding.TaskId &&
            row.Kind == DeadlineClockKind.ProjectManagerReview));
        var stock = await new CurrentRepairFactsReader(new CurrentRepairFactsRepository(db, TimeProvider.System)).CaptureAsync(state.Source.Pm,
            state.Source.Project, new ReportingFiltersDto(), default);
        Assert.Empty(stock.MissingReasons);
        Assert.Equal("REPORTED_AWAITING_REVIEW", Assert.Single(stock.Items).Presentation);
        var reviewCommand = new RepairAttemptReviewCommand(state.Source.Pm, UserRoleCode.ProjectManager,
            state.Source.Project, state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(intake.Id, "SUPPLEMENT", "missing verified AFTER evidence"), Guid.NewGuid().ToString(),
            Convert.ToBase64String(await db.Set<RepairItem>().AsNoTracking().Where(row => row.Id == state.Item.Id)
                .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync()));
        Assert.Equal(403, (await Repo(db).ReviewAttemptAsync(reviewCommand with
        {
            ActorId = state.Source.Crew,
            Role = UserRoleCode.RepairCrew
        }, default)).Status);
        Assert.Equal(403, (await Repo(db).ReviewAttemptAsync(reviewCommand with
        {
            ProjectId = Guid.NewGuid()
        }, default)).Status);
        var requested = await Repo(db).ReviewAttemptAsync(reviewCommand, default);
        Assert.True(requested.Status == 201, $"Expected H4 supplement request 201, got {requested.Status}/{requested.Code}.");
        db.ChangeTracker.Clear();
        var supplementEvent = await db.Set<RepairItemLifecycleEvent>().AsNoTracking().SingleAsync(row =>
            row.ItemId == state.Item.Id && row.Kind == "REWORK");
        var supplementOutbox = await db.OutboxMessages.SingleAsync(row => row.Id == supplementEvent.Id &&
            row.MessageType == "repair.work.rework_requested.v1");
        fence = Guid.NewGuid();
        supplementOutbox.AcquireLease("h6:" + fence.ToString("N"), DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(2), 32);
        await db.SaveChangesAsync();
        claim = new H6Claim(supplementOutbox.Id, supplementOutbox.MessageType,
            supplementOutbox.OccurredAtUtc, supplementOutbox.PayloadJson, fence,
            supplementOutbox.LeaseExpiresAtUtc!.Value, supplementOutbox.DeliveryAttemptCount);
        dispatched = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim,
            H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal("COMMITTED", dispatched.Status);
        Assert.Equal(1, dispatched.Delivered);
        Assert.Equal(1, await db.Notifications.CountAsync(row => row.SourceEntityId == state.Item.Id &&
            row.RecipientUserId == state.Source.Crew));
        Assert.Equal(0, await db.Set<DeadlineClock>().CountAsync(row => row.ProjectId == state.Source.Project &&
            row.Kind == DeadlineClockKind.CrewSupplement && row.TargetId == state.Item.Id));
        db.ChangeTracker.Clear();
        Assert.Equal(200, (await Repo(db).ReviewAttemptAsync(reviewCommand, default)).Status);
        Assert.Equal(403, (await Repo(db).ReviewAttemptAsync(reviewCommand with
        {
            TaskId = Guid.NewGuid()
        }, default)).Status);
        Assert.Equal(409, (await Repo(db).ReviewAttemptAsync(reviewCommand with
        {
            Key = Guid.NewGuid().ToString()
        }, default)).Status);
        var nativeVersion = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking()
            .Where(row => row.Id == state.Binding.TaskId).Select(row => row.RowVersion).SingleAsync());
        var supplemented = await field.ExecuteAsync(new(state.Source.Project, state.Binding.TaskId, "submit",
            new FieldSubmissionInput(Guid.NewGuid(), state.FirstStart.Id, intake.Id, [], [], null, "REPAIR_CLAIM", true, null),
            Guid.NewGuid().ToString(), nativeVersion,
            new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Crew, "DIRECT", true)),
            _ => Task.FromResult(true), default);
        Assert.Equal(201, supplemented.Status); db.ChangeTracker.Clear();
        var revision = await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleAsync(row =>
            row.TaskId == state.Binding.TaskId && row.Revision == 2);
        var supplementCommand = new RepairAttemptSupplementCommand(state.Source.Crew, UserRoleCode.RepairCrew,
            state.Source.Project, state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(revision.Id, revision.ContentHash), Guid.NewGuid().ToString(),
            Convert.ToBase64String(await db.Set<RepairItem>().AsNoTracking().Where(row => row.Id == state.Item.Id)
                .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync()));
        var supplement = await Repo(db).SupplementAttemptAsync(supplementCommand, default);
        Assert.Equal(201, supplement.Status); db.ChangeTracker.Clear();
        Assert.Equal(200, (await Repo(db).SupplementAttemptAsync(supplementCommand, default)).Status);
        Assert.Equal(409, (await Repo(db).SupplementAttemptAsync(supplementCommand with
        {
            Key = Guid.NewGuid().ToString()
        }, default)).Status);
        Assert.Equal(1, await db.Set<RepairAttempt>().CountAsync(row => row.ItemId == state.Item.Id));
        var latestLink = await db.Set<RepairAttemptSubmissionLink>().AsNoTracking().SingleAsync(row =>
            row.SubmissionId == revision.Id);
        Assert.Equal(link.Id, latestLink.PreviousLinkId);
        Assert.Equal(link.AttemptId, latestLink.AttemptId);
        Assert.Equal(reviewClock.Id, latestLink.ReviewClockId);
        Assert.Equal(reviewClock.OriginalDueAt, latestLink.OriginalReviewDueAt);
        stock = await new CurrentRepairFactsReader(new CurrentRepairFactsRepository(db, TimeProvider.System)).CaptureAsync(state.Source.Pm,
            state.Source.Project, new ReportingFiltersDto(), default);
        Assert.Empty(stock.MissingReasons);
        Assert.Equal("REPORTED_AWAITING_REVIEW", Assert.Single(stock.Items).Presentation);
        var secondReview = reviewCommand with
        {
            Input = new(revision.Id, "SUPPLEMENT", "verified AFTER still required"),
            Key = Guid.NewGuid().ToString(),
            ExpectedItemVersion = Convert.ToBase64String(await db.Set<RepairItem>().AsNoTracking()
                .Where(row => row.Id == state.Item.Id).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync())
        };
        Assert.Equal(201, (await Repo(db).ReviewAttemptAsync(secondReview, default)).Status);
        db.ChangeTracker.Clear();
        var now = DateTimeOffset.UtcNow;
        var file = StoredFile.Create(Guid.NewGuid(), "field/repair-after-" + Guid.NewGuid().ToString("N"),
            "after.jpg", "image/jpeg", 4, new string('c', 64), state.Source.Crew, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, state.Source.Crew, file.StorageUri,
            "AFTER", "image/jpeg", 4, new string('c', 64), 8388608, now.AddHours(24));
        upload.StartUploading("fixture", now);
        db.AddRange(file, FileScope.Create(Guid.NewGuid(), file.Id, state.Source.Project,
            state.Binding.TaskId, state.Source.Crew, "AFTER", now), upload);
        await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
        await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        nativeVersion = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking()
            .Where(row => row.Id == state.Binding.TaskId).Select(row => row.RowVersion).SingleAsync());
        var completedEvidence = await field.ExecuteAsync(new(state.Source.Project, state.Binding.TaskId, "submit",
            new FieldSubmissionInput(Guid.NewGuid(), state.FirstStart.Id, revision.Id,
                [new("depth", "DepressionDepth", 0, "KNOWN", null, "LENGTH", "mm", null, null,
                    "on road", "depth gauge", "depth measurement")],
                [new(Guid.NewGuid(), file.Id, "AFTER", new string('c', 64), "image/jpeg", DateTimeOffset.UtcNow,
                    "POST_REPAIR_CAPTURE")],
                new("POSITION_CHECKLIST", "ROUTE_CHAINAGE_MARKINGS_CONFIRMED", null, null,
                    ObservedRouteVersionId: state.Source.Route, ObservedChainageMeters: 5),
                "REPAIR_CLAIM", true, null), Guid.NewGuid().ToString(), nativeVersion,
            new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Crew, "DIRECT", true)),
            _ => Task.FromResult(true), default);
        Assert.Equal(201, completedEvidence.Status); db.ChangeTracker.Clear();
        var readyRevision = await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleAsync(row =>
            row.TaskId == state.Binding.TaskId && row.Revision == 3);
        Assert.Equal("INCOMPLETE", readyRevision.Readiness);
        var missingReasons = System.Text.Json.JsonSerializer.Deserialize<string[]>(readyRevision.MissingReasonsJson)!;
        Assert.Equal("AFTER_ATTEMPT_BINDING_UNAVAILABLE", Assert.Single(missingReasons));
        var readySupplement = supplementCommand with
        {
            Input = new(readyRevision.Id, readyRevision.ContentHash),
            Key = Guid.NewGuid().ToString(),
            ExpectedItemVersion = Convert.ToBase64String(await db.Set<RepairItem>().AsNoTracking()
                .Where(row => row.Id == state.Item.Id).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync())
        };
        Assert.Equal(201, (await Repo(db).SupplementAttemptAsync(readySupplement, default)).Status);
        db.ChangeTracker.Clear();
        var secondSupplementReview = await db.Set<RepairAttemptReview>().AsNoTracking().SingleAsync(row =>
            row.ItemId == state.Item.Id && row.SubmissionId == revision.Id && row.Decision == "SUPPLEMENT");
        var staleSupplementEvent = await db.Set<RepairItemLifecycleEvent>().AsNoTracking().SingleAsync(row =>
            row.ItemId == state.Item.Id && row.Kind == "REWORK" && row.ReviewId == secondSupplementReview.Id);
        var staleSupplementOutbox = await db.OutboxMessages.SingleAsync(row => row.Id == staleSupplementEvent.Id);
        fence = Guid.NewGuid();
        staleSupplementOutbox.AcquireLease("h6:" + fence.ToString("N"), DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(2), 32);
        await db.SaveChangesAsync();
        claim = new H6Claim(staleSupplementOutbox.Id, staleSupplementOutbox.MessageType,
            staleSupplementOutbox.OccurredAtUtc, staleSupplementOutbox.PayloadJson, fence,
            staleSupplementOutbox.LeaseExpiresAtUtc!.Value, staleSupplementOutbox.DeliveryAttemptCount);
        dispatched = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim,
            H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal("REJECTED", dispatched.Status);
        Assert.Equal(1, await db.Notifications.CountAsync(row => row.SourceEntityId == state.Item.Id &&
            row.RecipientUserId == state.Source.Crew));
        db.ChangeTracker.Clear();
        var acceptedReview = reviewCommand with
        {
            Input = new(readyRevision.Id, "ACCEPT", "verified physical repair and fresh AFTER evidence"),
            Key = Guid.NewGuid().ToString(),
            ExpectedItemVersion = Convert.ToBase64String(await db.Set<RepairItem>().AsNoTracking()
                .Where(row => row.Id == state.Item.Id).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync())
        };
        Assert.Equal(201, (await Repo(db).ReviewAttemptAsync(acceptedReview, default)).Status);
        db.ChangeTracker.Clear();
        Assert.Equal(RepairItemState.Reviewed, (await db.Set<RepairItem>().SingleAsync(row => row.Id == state.Item.Id)).State);
        Assert.NotNull((await db.Set<DeadlineClock>().SingleAsync(row => row.Id == reviewClock.Id)).CompletedAt);
        var acceptedReviewRecord = await db.Set<RepairAttemptReview>().AsNoTracking().SingleAsync(row =>
            row.ItemId == state.Item.Id && row.Decision == "ACCEPT");
        var finalClock = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(row =>
            row.ProjectId == state.Source.Project && row.TargetId == state.Item.Id &&
            row.Kind == DeadlineClockKind.SupervisorFinalConfirmation);
        Assert.Equal(acceptedReviewRecord.Id, finalClock.OriginEventId);
        Assert.Equal(acceptedReviewRecord.At, finalClock.OriginAt);
        Assert.Equal(finalClock.OriginAt.AddHours(48), finalClock.OriginalDueAt);
        Assert.Null(finalClock.CompletedAt);
        if (observeFinalBreach)
        {
            var item = await db.Set<RepairItem>().AsNoTracking().SingleAsync(row => row.Id == state.Item.Id);
            var obligation = await db.Set<RepairObligation>().AsNoTracking().SingleAsync(row => row.Id == item.ObligationId);
            var lifecycle = await db.Set<RepairItemLifecycleEvent>().AsNoTracking().SingleAsync(row =>
                row.ItemId == item.Id && row.Kind == "PM_REVIEWED" && row.ReviewId == acceptedReviewRecord.Id);
            Assert.Equal(acceptedReviewRecord.Id, item.CurrentReviewId);
            Assert.Equal(item.Id, obligation.CurrentRepairItemId);
            Assert.Equal(acceptedReviewRecord.At, lifecycle.At);
            Assert.Equal(state.Source.Pm, acceptedReviewRecord.ActorId);
            Assert.Equal("ACCEPT", acceptedReviewRecord.Decision);
            var observedAt = finalClock.OriginalDueAt.AddMinutes(1);
            var dispatcher = new H6NotificationDispatchRepository(db, new FixedClock(observedAt));
            Assert.True(await dispatcher.ObserveClocksAsync(default) > 0);
            db.ChangeTracker.Clear();
            var breach = await db.Set<DeadlineBreach>().AsNoTracking().SingleAsync(row => row.ClockId == finalClock.Id);
            Assert.Equal(finalClock.OriginalDueAt, breach.DueAt);
            var breachMessage = await db.OutboxMessages.SingleAsync(row => row.Id == breach.Id);
            Assert.Equal("deadline.breached.v1", breachMessage.MessageType);
            var plan = H6NotificationCatalog.Parse(breachMessage.Id, breachMessage.MessageType,
                breachMessage.OccurredAtUtc, breachMessage.PayloadJson);
            Assert.Equal(finalClock.Id, plan.Source.SourceId);
            Assert.Equal(breach.Id, plan.Source.OriginEventId);
            if (revokeSupervisorMembership)
            {
                var member = await db.ProjectMembers.SingleAsync(row => row.ProjectId == state.Source.Project &&
                    row.UserId == state.Source.Supervisor);
                member.Status = ProjectMemberStatus.Ended;
                await db.SaveChangesAsync(); db.ChangeTracker.Clear();
                breachMessage = await db.OutboxMessages.SingleAsync(row => row.Id == breach.Id);
            }
            var breachFence = Guid.NewGuid();
            breachMessage.AcquireLease("h6:" + breachFence.ToString("N"), observedAt, TimeSpan.FromMinutes(2), 32);
            await db.SaveChangesAsync();
            var breachClaim = new H6Claim(breachMessage.Id, breachMessage.MessageType, breachMessage.OccurredAtUtc,
                breachMessage.PayloadJson, breachFence, breachMessage.LeaseExpiresAtUtc!.Value, breachMessage.DeliveryAttemptCount);
            var result = await dispatcher.DispatchAsync(breachClaim, plan, default);
            Assert.Equal("COMMITTED", result.Status);
            Assert.Equal(revokeSupervisorMembership ? 0 : 1, result.Delivered);
            Assert.Equal(revokeSupervisorMembership ? 1 : 0, result.Unresolved);
            Assert.Equal("COMMITTED", (await dispatcher.DispatchAsync(breachClaim, plan, default)).Status);
            Assert.Equal(revokeSupervisorMembership ? 0 : 1, await db.Notifications.CountAsync(row =>
                row.SourceEntityId == finalClock.Id && row.RecipientUserId == state.Source.Supervisor));
            Assert.Equal(1, await db.Set<H6NotificationDeliveryRow>().CountAsync(row =>
                row.OccurrenceId == result.OccurrenceId));
            return;
        }
        var finalReviewEvent = await db.Set<RepairItemLifecycleEvent>().AsNoTracking().SingleAsync(row =>
            row.ItemId == state.Item.Id && row.Kind == "PM_REVIEWED");
        var finalReviewOutbox = await db.OutboxMessages.SingleAsync(row => row.Id == finalReviewEvent.Id &&
            row.MessageType == "review.supervisor_required.v1");
        fence = Guid.NewGuid();
        finalReviewOutbox.AcquireLease("h6:" + fence.ToString("N"), DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(2), 32);
        await db.SaveChangesAsync();
        claim = new H6Claim(finalReviewOutbox.Id, finalReviewOutbox.MessageType,
            finalReviewOutbox.OccurredAtUtc, finalReviewOutbox.PayloadJson, fence,
            finalReviewOutbox.LeaseExpiresAtUtc!.Value, finalReviewOutbox.DeliveryAttemptCount);
        dispatched = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim,
            H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), default);
        Assert.Equal("COMMITTED", dispatched.Status);
        Assert.Equal(1, dispatched.Delivered);
        Assert.Equal(1, await db.Notifications.CountAsync(row => row.SourceEntityId == state.Item.Id &&
            row.RecipientUserId == state.Source.Supervisor));
        db.ChangeTracker.Clear();
        var finalCommand = new RepairFinalConfirmCommand(state.Source.Supervisor, UserRoleCode.Supervisor,
            state.Source.Project, state.Package.Id, state.Item.Id,
            new("verified normal repair"), Guid.NewGuid().ToString(),
            Convert.ToBase64String(await db.Set<RepairItem>().AsNoTracking()
                .Where(row => row.Id == state.Item.Id).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync()));
        Assert.Equal(403, (await Repo(db).ConfirmFinalAsync(finalCommand with
        {
            ActorId = state.Source.Pm,
            Role = UserRoleCode.ProjectManager
        }, default)).Status);
        Assert.Equal(201, (await Repo(db).ConfirmFinalAsync(finalCommand, default)).Status);
        db.ChangeTracker.Clear();
        Assert.Equal(200, (await Repo(db).ConfirmFinalAsync(finalCommand, default)).Status);
        Assert.Equal(409, (await Repo(db).ConfirmFinalAsync(finalCommand with
        {
            Key = Guid.NewGuid().ToString()
        }, default)).Status);
        Assert.Equal(RepairItemState.Confirmed, (await db.Set<RepairItem>().SingleAsync(row => row.Id == state.Item.Id)).State);
        Assert.True((await db.Set<RepairObligation>().SingleAsync(row => row.Id == state.Item.ObligationId)).IsResolved);
        Assert.True((await db.Set<RepairPackage>().Include(row => row.Obligations)
            .SingleAsync(row => row.Id == state.Package.Id)).IsComplete);
        Assert.NotNull((await db.Set<DeadlineClock>().SingleAsync(row => row.Id == finalClock.Id)).CompletedAt);
        await using (var notificationCheck = await db.Database.BeginTransactionAsync())
        {
            var stale = await new H6RepairNotificationSourceAdapter(db).ResolveAsync(
                H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc,
                    claim.PayloadJson), default);
            Assert.Equal("REJECTED", stale.Status);
            await notificationCheck.CommitAsync();
        }
        Assert.Equal(DefectStatus.Open,
            (await db.Defects.SingleAsync(row => row.Id == state.Item.DefectId)).Status);
        Assert.Equal(intake.ContentHash, (await db.Set<FieldInspectionSubmission>().AsNoTracking()
            .SingleAsync(row => row.Id == intake.Id)).ContentHash);
        Assert.Empty((await db.Set<RepairAttempt>().AsNoTracking().SingleAsync(row => row.Id == attempt.Id)).Evidence);
        stock = await new CurrentRepairFactsReader(new CurrentRepairFactsRepository(db, TimeProvider.System)).CaptureAsync(state.Source.Pm,
            state.Source.Project, new ReportingFiltersDto(), default);
        Assert.Empty(stock.MissingReasons);
        Assert.Equal("CONFIRMED", Assert.Single(stock.Items).Presentation);
        var reporting = new ReportingService(new ReportingRepository(db), new IdentityRepository(db),
            new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System), TimeProvider.System,
            currentRepairReaders: [new CurrentRepairFactsReader(new CurrentRepairFactsRepository(db, TimeProvider.System))]);
        var beforeCapture = (await reporting.CaptureAsync(state.Source.Pm, state.Source.Project,
            new ReportingFiltersDto(), default)).Value!;
        Assert.Equal("CONFIRMED", Assert.Single(beforeCapture.Summary.Metrics.Where(row =>
            row.Code == "repairItemsByStatus")).Dimensions.Status);
        var exports = new ExportService(new ExportRepository(db, new IdempotencyOperationService(db), TimeProvider.System),
            new IdentityRepository(db), new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System),
            reporting, [], [], null!, null!, null!, TimeProvider.System, null!);
        var oldExport = await exports.CreateAsync(state.Source.Pm, state.Source.Project,
            new("DOSSIER", "ZIP"), "actual-repair-before-correction", null, default);
        Assert.Equal("success", oldExport.Code);
        var oldSnapshot = await db.Set<ExportSnapshot>().AsNoTracking().SingleAsync(row =>
            row.Id == oldExport.Value!.SnapshotId);
        Assert.Equal("CONFIRMED", Assert.Single(ExportSerialization.Read(oldSnapshot).Dossier!.Summary.Metrics
            .Where(row => row.Code == "repairItemsByStatus")).Dimensions.Status);
        var originalDecision = await db.Set<RepairDecision>().AsNoTracking().SingleAsync(row =>
            row.ItemId == state.Item.Id && row.SupersedesDecisionId == null);
        var correctionCommand = new RepairCorrectionCommand(state.Source.Supervisor, UserRoleCode.Supervisor,
            state.Source.Project, state.Package.Id, state.Item.Id,
            new(originalDecision.Id, "UNREPAIRED", "final confirmation was mistaken",
                new("independent field recheck found repair incomplete", [])),
            Guid.NewGuid().ToString(), Convert.ToBase64String(await db.Set<RepairItem>().AsNoTracking()
                .Where(row => row.Id == state.Item.Id).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync()));
        Assert.Equal(201, (await Repo(db).CorrectAsync(correctionCommand, default)).Status);
        db.ChangeTracker.Clear();
        Assert.Equal(200, (await Repo(db).CorrectAsync(correctionCommand, default)).Status);
        Assert.False((await db.Set<RepairObligation>().SingleAsync(row => row.Id == state.Item.ObligationId)).IsResolved);
        Assert.False((await db.Set<RepairPackage>().Include(row => row.Obligations)
            .SingleAsync(row => row.Id == state.Package.Id)).IsComplete);
        stock = await new CurrentRepairFactsReader(new CurrentRepairFactsRepository(db, TimeProvider.System)).CaptureAsync(state.Source.Pm,
            state.Source.Project, new ReportingFiltersDto(), default);
        Assert.Empty(stock.MissingReasons);
        Assert.Equal("UNREPAIRED", Assert.Single(stock.Items).Presentation);
        var afterCapture = (await reporting.CaptureAsync(state.Source.Pm, state.Source.Project,
            new ReportingFiltersDto(), default)).Value!;
        Assert.Equal("UNREPAIRED", Assert.Single(afterCapture.Summary.Metrics.Where(row =>
            row.Code == "repairItemsByStatus")).Dimensions.Status);
        var newExport = await exports.CreateAsync(state.Source.Pm, state.Source.Project,
            new("DOSSIER", "ZIP"), "actual-repair-after-correction", null, default);
        Assert.Equal("success", newExport.Code);
        var newSnapshot = await db.Set<ExportSnapshot>().AsNoTracking().SingleAsync(row =>
            row.Id == newExport.Value!.SnapshotId);
        var payload = ExportSerialization.Read(newSnapshot);
        Assert.Equal("UNREPAIRED", Assert.Single(payload.Dossier!.Summary.Metrics.Where(row =>
            row.Code == "repairItemsByStatus")).Dimensions.Status);
        var correctedHead = await db.Set<RepairItem>().AsNoTracking().Where(row => row.Id == state.Item.Id)
            .Select(row => row.EffectiveDecisionId).SingleAsync();
        Assert.Contains(payload.Manifest.SourceRevisions, revision => revision.Kind == "RepairDecision" &&
            revision.Id == correctedHead);
        Assert.Contains(ExportSerialization.Read(oldSnapshot).Manifest.SourceRevisions,
            revision => revision.Kind == "RepairDecision" && revision.Id == originalDecision.Id);
        Assert.Equal(oldSnapshot.PayloadJson, (await db.Set<ExportSnapshot>().AsNoTracking()
            .SingleAsync(row => row.Id == oldSnapshot.Id)).PayloadJson);
        Assert.Equal(oldSnapshot.Hash, (await db.Set<ExportSnapshot>().AsNoTracking()
            .SingleAsync(row => row.Id == oldSnapshot.Id)).Hash);
        var inventory = new RoadGuardSystem.Repositories.Implementations.Retention.Huy02InspectionRetentionContributor(db);
        var protectedFile = await inventory.ReadAsync(file.Id, default);
        Assert.True(protectedFile.Complete, string.Join(",", protectedFile.ReasonCodes));
        Assert.Contains(protectedFile.References, row => row.Kind == "REPAIR_ATTEMPT_FIELD_EVIDENCE" && row.ProjectId == state.Source.Project);
        Assert.Contains(file.Id, await inventory.KnownProjectFilesAsync(state.Source.Project, default));
        var retained = await db.Set<RepairDecision>().AsNoTracking().SingleAsync(row => row.Id == originalDecision.Id);
        Assert.Null(retained.SupersedesDecisionId);
        Assert.Equal(RepairPresentationState.Confirmed, retained.Result);
        Assert.Equal(intake.ContentHash, (await db.Set<FieldInspectionSubmission>().AsNoTracking()
            .SingleAsync(row => row.Id == intake.Id)).ContentHash);
        Assert.Equal(1, await db.Set<RepairAttempt>().CountAsync(row => row.ItemId == state.Item.Id));
        (await db.ProjectMembers.SingleAsync(row => row.ProjectId == state.Source.Project &&
            row.UserId == state.Source.Supervisor)).Status = ProjectMemberStatus.Ended;
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(403, (await Repo(db).ConfirmFinalAsync(finalCommand, default)).Status);
        Assert.Equal(403, (await Repo(db).CorrectAsync(correctionCommand, default)).Status);
        (await db.ProjectMembers.SingleAsync(row => row.ProjectId == state.Source.Project && row.UserId == state.Source.Crew))
            .Status = ProjectMemberStatus.Ended;
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(403, (await Repo(db).SubmitAttemptAsync(command, default)).Status);
        Assert.Equal(403, (await Repo(db).SupplementAttemptAsync(supplementCommand, default)).Status);
        Assert.Equal(200, (await Repo(db).ReviewAttemptAsync(reviewCommand, default)).Status);
        (await db.ProjectMembers.SingleAsync(row => row.ProjectId == state.Source.Project && row.UserId == state.Source.Pm))
            .Status = ProjectMemberStatus.Ended;
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(403, (await Repo(db).ReviewAttemptAsync(reviewCommand, default)).Status);
    }
    private static Task<RoadGuardSystem.Repositories.Repairs.RepairWorkflowResult> Assess(RoadGuardDbContext db, Ready state)
        => Repo(db).AssessAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project, state.Package.Id,
            state.Item.Id, state.Binding.TaskId, new(Guid.NewGuid(), state.FirstStart.Id, null, null, null),
            Guid.NewGuid().ToString(), Version(db, state.Item)), default);
    [Fact]
    public async Task ActualCrewAssessmentRetainsUnknownCaptureWithoutFormalIntakeOrPmClock()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var result = await Repo(db).AssessAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(Guid.NewGuid(), state.FirstStart.Id, null, null, null), Guid.NewGuid().ToString(), Version(db, state.Item)), default);
        Assert.Equal(201, result.Status); db.ChangeTracker.Clear();
        var assessment = await db.Set<RepairMeasurementAssessment>().SingleAsync(row => row.ItemId == state.Item.Id);
        Assert.Equal("PRE_EXECUTION", assessment.Stage); Assert.Equal("INCOMPLETE", assessment.Readiness);
        Assert.Equal(state.FirstStart.Id, assessment.FirstStartId); Assert.Null(assessment.FormalSourceSubmissionId);
        var session = await db.FieldInspectionSessions.SingleAsync(row => row.Id == assessment.SessionId);
        Assert.Equal(FieldInspectionPurpose.PreMeasurement, session.Purpose);
        Assert.False(await db.Set<FieldInspectionSubmission>().AnyAsync(row => row.TaskId == state.Binding.TaskId));
        Assert.False(await db.Set<DeadlineClock>().AnyAsync(row => row.TargetId == state.Binding.TaskId && row.Kind == DeadlineClockKind.ProjectManagerReview));
    }
    [Fact]
    public async Task PmCannotCaptureAnAssessmentUsingCrewBindingIdentity()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var result = await Repo(db).AssessAsync(new(state.Source.Pm, UserRoleCode.ProjectManager, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(Guid.NewGuid(), state.FirstStart.Id, null, null, null), Guid.NewGuid().ToString(), Version(db, state.Item)), default);
        Assert.Equal(403, result.Status); Assert.False(await db.Set<RepairMeasurementAssessment>().AnyAsync(row => row.ItemId == state.Item.Id));
    }
    [Fact]
    public async Task FirstStartFromAnotherTaskCannotEstablishAssessmentProvenance()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db); var other = await AssignedAndStarted(db);
        var itemVersion = Convert.ToBase64String(await db.Set<RepairItem>().AsNoTracking().Where(row => row.Id == state.Item.Id)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var result = await Repo(db).AssessAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(Guid.NewGuid(), other.FirstStart.Id, null, null, null), Guid.NewGuid().ToString(), itemVersion), default);
        Assert.Equal(409, result.Status); Assert.False(await db.Set<RepairMeasurementAssessment>().AnyAsync(row => row.ItemId == state.Item.Id));
    }
    [Fact]
    public async Task NormalAssignmentDoesNotLetCrewStartWithoutActualPinnedAssessment()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var result = await Repo(db).StartExecutionAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(Guid.NewGuid(), state.FirstStart.Id, DateTimeOffset.UtcNow, Guid.NewGuid()), Guid.NewGuid().ToString(), Version(db, state.Item)), default);
        Assert.Equal(409, result.Status); Assert.False(await db.Set<RepairExecutionStart>().AnyAsync(row => row.ItemId == state.Item.Id));
    }
    [Fact]
    public async Task FinishCannotFabricateAStartedPhysicalAttemptOrSyncClock()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var result = await Repo(db).FinishExecutionAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package.Id, state.Item.Id, state.Binding.TaskId,
            new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow), Guid.NewGuid().ToString(), Version(db, state.Item)), default);
        Assert.Equal(409, result.Status); Assert.False(await db.Set<RepairExecutionFinish>().AnyAsync(row => row.ItemId == state.Item.Id));
        Assert.False(await db.Set<DeadlineClock>().AnyAsync(row => row.TargetId == state.Item.Id && row.Kind == DeadlineClockKind.FinishedDataSync));
    }

    private sealed record Ready(H4GenuineRepairSource.Source Source, RepairPackage Package, RepairItem Item,
        RepairFieldTaskBinding Binding, FieldTaskStartOrigin FirstStart);
    private async Task<Ready> AssignedAndStarted(RoadGuardDbContext db)
    {
        var source = await H4GenuineRepairSource.Seed(db, sql); var repo = Repo(db);
        var defectVersion = Convert.ToBase64String(await db.Defects.Where(row => row.Id == source.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var input = new RepairPackageCreateData(source.Defect, defectVersion,
            [new("FORMAL_REPAIR", true, new(source.Road, source.Route, source.Set, null, null, 1, 2, 0, 1), "mandatory physical repair")], "package");
        var created = await repo.CreatePackageAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, input,
            Guid.NewGuid().ToString(), defectVersion), default); Assert.Equal(201, created.Status);
        var packageView = Assert.IsType<RepairPackageFact>(created.Value);
        var package = await db.Set<RepairPackage>().Include(row => row.Obligations).SingleAsync(row => row.Id == packageView.Id);
        var proposed = await repo.ProposeItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, package.Id,
            new(package.Obligations[0].Id, "NORMAL", "actual repair plan", "checklist-v1", "proposal"), Guid.NewGuid().ToString(), created.Version!), default);
        Assert.Equal(201, proposed.Status); var itemView = Assert.IsType<RepairItemFact>(proposed.Value);
        var approved = await repo.ApproveItemAsync(new(source.Supervisor, UserRoleCode.Supervisor, source.Project, package.Id,
            itemView.Id, new("approve actual plan"), Guid.NewGuid().ToString(), proposed.Version!), default); Assert.Equal(201, approved.Status);
        var assigned = await repo.AssignItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, package.Id, itemView.Id,
            new(new(source.Defect, defectVersion, null, "REPORTER", source.Route, source.Set, null, null, "POST_REPAIR", 1,
                "{}", null, source.Crew, DateTimeOffset.UtcNow.AddDays(1)), null, "assign actual Crew"), Guid.NewGuid().ToString(), approved.Version!), default);
        Assert.Equal(201, assigned.Status); var bindingView = Assert.IsType<RepairTaskBindingFact>(assigned.Value);
        var native = await db.FieldInspectionTasks.SingleAsync(row => row.Id == bindingView.TaskId);
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var admission = new FieldAdmissionContext(source.Crew, UserRoleCode.RepairCrew, source.Crew, "DIRECT", true);
        var accepted = await field.ExecuteAsync(new(source.Project, native.Id, "accept", new FieldTaskActionInput("accept assigned work"),
            Guid.NewGuid().ToString(), Convert.ToBase64String(native.RowVersion), admission), _ => Task.FromResult(true), default);
        Assert.Equal(201, accepted.Status);
        var acceptedVersion = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking().Where(row => row.Id == native.Id)
            .Select(row => row.RowVersion).SingleAsync());
        var started = await field.ExecuteAsync(new(source.Project, native.Id, "start", new FieldStartInput(Guid.NewGuid(), DateTimeOffset.UtcNow),
            Guid.NewGuid().ToString(), acceptedVersion, admission), _ => Task.FromResult(true), default);
        Assert.True(started.Status == 201, $"Expected native start 201, actual {started.Status}/{started.Code}.");
        var item = await db.Set<RepairItem>().SingleAsync(row => row.Id == itemView.Id);
        var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(row => row.Id == item.CurrentBindingId);
        var first = await db.Set<FieldTaskStartOrigin>().SingleAsync(row => row.TaskId == native.Id);
        return new(source, package, item, binding, first);
    }
    private static RepairWorkflowRepository Repo(RoadGuardDbContext db) => new(db, new IdempotencyOperationService(db), TimeProvider.System);
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => now; }
    private static string Version<T>(RoadGuardDbContext db, T entity) where T : class
        => Convert.ToBase64String(db.Entry(entity).Property<byte[]>("RowVersion").CurrentValue!);
}
