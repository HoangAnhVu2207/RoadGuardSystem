using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using RoadGuardSystem.TestFixtures;
using Xunit;
namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed class FastTrackReviewNotificationSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(false, true, true, true)]
    [InlineData(false, true, true, false, true)]
    [InlineData(false, false, true, false, false, true)]
    [InlineData(true, false, true, false, false, true)]
    public async Task GenuineSubmittedEventSnapshotsAllManagersAndRechecksRevocationWithoutAddingNewRecipients(bool revoke, bool complete, bool assigned, bool deferSupervisor, bool raceFinal = false, bool delayedIntake = false)
    {
        await using var db = sql.CreateDbContext(); var source = await H4GenuineRepairSource.Seed(db, sql);
        var state = await ND01OriginalWindowFixture.Prepare(db, new(source.Pm, source.Crew, source.Supervisor, source.Project, source.Road, source.Route, source.Set, source.Defect));
        using var keys = state.Keys;
        var eventClock = new IntakeClock();
        var intakeDay = DateOnly.FromDateTime(eventClock.GetUtcNow().UtcDateTime).AddDays(1);
        var second = Manager();
        db.Users.Add(second); db.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = source.Project,
            UserId = second.Id,
            RoleCode = UserRoleCode.ProjectManager,
            Status = ProjectMemberStatus.Active,
            ValidFrom = delayedIntake ? intakeDay : new(2000, 1, 1),
            IsPrimary = false
        });
        var expired = Manager();
        if (delayedIntake)
        {
            db.Users.Add(expired);
            db.ProjectMembers.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = source.Project,
                UserId = expired.Id,
                RoleCode = UserRoleCode.ProjectManager,
                Status = ProjectMemberStatus.Active,
                ValidFrom = new(2000, 1, 1),
                ValidTo = intakeDay.AddDays(-1),
                IsPrimary = false
            });
        }
        await db.SaveChangesAsync();
        var repairs = new RepairWorkflowRepository(db, new IdempotencyOperationService(db), eventClock);
        var assignments = new NotificationSupervisorRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        NotificationSupervisorResult? assignedA = null;
        if (assigned)
        {
            // Genuine global Supervisor with no ProjectMember row.
            await db.ProjectMembers.Where(x => x.ProjectId == source.Project && x.UserId == source.Supervisor).ExecuteDeleteAsync();
            assignedA = await assignments.ExecuteAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
                state.Package, state.Item, "PUT", "assign-A", IfNoneMatch: "*", Body: JsonSerializer.Serialize(new { supervisorId = source.Supervisor })), default);
            Assert.Equal(201, assignedA.Status);
        }
        var itemVersion = Convert.ToBase64String(await db.RepairItems.Where(x => x.Id == state.Item).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
        var start = await repairs.StartExecutionAsync(new(source.Crew, UserRoleCode.RepairCrew, source.Project, state.Package, state.Item, state.First.TaskId,
            new(Guid.NewGuid(), state.First.Id, DateTimeOffset.UtcNow, state.Operation.Repair!.Start!.AssessmentId), Guid.NewGuid().ToString(), itemVersion), default); Assert.Equal(201, start.Status);
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        Guid? finishId = null;
        StoredFile? after = null;
        if (complete)
        {
            var physicalStart = await db.Set<RepairExecutionStart>().AsNoTracking().SingleAsync(x => x.ItemId == state.Item);
            var finished = await repairs.FinishExecutionAsync(new(source.Crew, UserRoleCode.RepairCrew, source.Project, state.Package, state.Item,
                state.First.TaskId, new(Guid.NewGuid(), physicalStart.Id, DateTimeOffset.UtcNow), Guid.NewGuid().ToString(), start.Version!), default);
            Assert.Equal(201, finished.Status);
            db.ChangeTracker.Clear();
            finishId = (await db.Set<RepairExecutionFinish>().AsNoTracking().SingleAsync(x => x.ItemId == state.Item)).Id;
            // Controlled verified upload lineage; does not claim real MinIO bytes or field accuracy.
            var now = DateTimeOffset.UtcNow;
            after = StoredFile.Create(Guid.NewGuid(), "private/closeout/" + Guid.NewGuid(), "after.jpg", "image/jpeg", 4,
                new string('c', 64), source.Crew, now, null);
            var upload = UploadSession.Create(Guid.NewGuid(), after.Id, source.Crew, after.StorageUri, "AFTER", "image/jpeg", 4,
                after.Checksum, 8388608, now.AddHours(24));
            upload.StartUploading("fixture", now);
            db.AddRange(after, upload, FileScope.Create(Guid.NewGuid(), after.Id, source.Project, state.First.TaskId, source.Crew, "AFTER", now));
            await db.SaveChangesAsync();
            upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
            await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        }
        var taskVersion = Convert.ToBase64String(await db.FieldInspectionTasks.Where(x => x.Id == state.First.TaskId).Select(x => x.RowVersion).SingleAsync());
        var submitted = await field.ExecuteAsync(new(source.Project, state.First.TaskId, "submit",
            new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldSubmissionInputFact(Guid.NewGuid(), state.First.Id, null,
                complete ? [new("depth", "DepressionDepth", 0, "KNOWN", null, "LENGTH", "mm", null, null, "on road", "depth gauge", "depth measurement")] : [],
                complete ? [new(Guid.NewGuid(), after!.Id, "AFTER", after.Checksum, after.MimeType, DateTimeOffset.UtcNow, "POST_REPAIR_CAPTURE")] : [],
                complete ? new("POSITION_CHECKLIST", "ROUTE_CHAINAGE_MARKINGS_CONFIRMED", null, null, ObservedRouteVersionId: source.Route, ObservedChainageMeters: 1) : null,
                "REPAIR_CLAIM", complete, complete ? null : "Needs further work"),
            Guid.NewGuid().ToString(), taskVersion, new(source.Crew, UserRoleCode.RepairCrew, source.Crew, "DIRECT", true)), _ => Task.FromResult(true), default);
        Assert.Equal(201, submitted.Status); db.ChangeTracker.Clear();
        var intake = await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleAsync(x => x.TaskId == state.First.TaskId);
        if (delayedIntake)
        {
            // Advance the genuine intake producer, never rewrite the immutable H3 submission.
            eventClock.Now = new DateTimeOffset(intakeDay.ToDateTime(new TimeOnly(0, 10)), TimeSpan.Zero);
            Assert.True(intake.ServerReceivedAt < eventClock.GetUtcNow());
        }
        itemVersion = Convert.ToBase64String(await db.RepairItems.Where(x => x.Id == state.Item).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
        var command = new RoadGuardSystem.Repositories.Repairs.RepairAttemptSubmitCommand(source.Crew, UserRoleCode.RepairCrew, source.Project, state.Package, state.Item, state.First.TaskId,
            new(intake.Id, intake.ContentHash, finishId), Guid.NewGuid().ToString(), itemVersion);
        Assert.Equal(201, (await repairs.SubmitAttemptAsync(command, default)).Status);
        Assert.Equal(200, (await repairs.SubmitAttemptAsync(command, default)).Status); db.ChangeTracker.Clear();
        var lifecycle = await db.Set<RepairItemLifecycleEvent>().SingleAsync(x => x.ItemId == state.Item && x.Kind == "SUBMITTED");
        var message = await db.OutboxMessages.SingleAsync(x => x.Id == lifecycle.Id && x.MessageType == "repair.work.submitted.v1");
        var fact = H6NotificationCatalog.Parse(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson);
        Assert.NotNull(fact.Source.NotificationAssignmentSnapshot); Assert.Equal(new[] { source.Pm, second.Id }.Order(), fact.Source.NotificationAssignmentSnapshot.ProjectManagerIds.Order());
        Assert.DoesNotContain(expired.Id, fact.Source.NotificationAssignmentSnapshot.ProjectManagerIds);
        Assert.Equal(lifecycle.At, fact.Source.NotificationAssignmentSnapshot.CapturedAt);
        Assert.Equal(lifecycle.At, message.OccurredAtUtc);
        if (delayedIntake)
        {
            Assert.Equal(eventClock.GetUtcNow(), lifecycle.At);
            var persistedSubmission = await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleAsync(x => x.Id == intake.Id);
            var attempt = await db.Set<RepairAttempt>().AsNoTracking().SingleAsync(x => x.ItemId == state.Item);
            var link = await db.Set<RepairAttemptSubmissionLink>().AsNoTracking().SingleAsync(x => x.AttemptId == attempt.Id);
            var reviewClock = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(x => x.Id == link.ReviewClockId);
            Assert.Equal(intake.ServerReceivedAt, persistedSubmission.ServerReceivedAt);
            Assert.Equal(intake.ServerReceivedAt, attempt.ServerReceivedAt);
            Assert.Equal(intake.ServerReceivedAt, link.FormalRootServerReceivedAt);
            Assert.Equal(intake.ServerReceivedAt, reviewClock.OriginAt);
            Assert.Equal(link.OriginalReviewDueAt, reviewClock.OriginalDueAt);
        }
        Assert.Equal(assigned ? source.Supervisor : (Guid?)null, fact.Source.NotificationAssignmentSnapshot.SupervisorId);
        var late = Manager();
        db.Users.Add(late); db.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = source.Project,
            UserId = late.Id,
            RoleCode = UserRoleCode.ProjectManager,
            Status = ProjectMemberStatus.Active,
            ValidFrom = new(2000, 1, 1),
            IsPrimary = false
        });
        if (revoke) await db.ProjectMembers.Where(x => x.ProjectId == source.Project && x.UserId == second.Id).ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, ProjectMemberStatus.Ended));
        var fence = Guid.NewGuid(); message.AcquireLease("h6:" + fence.ToString("N"), eventClock.GetUtcNow(), TimeSpan.FromMinutes(2), 32); await db.SaveChangesAsync();
        var claim = new H6Claim(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson, fence, message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
        var dispatcher = new H6NotificationDispatchRepository(db, delayedIntake ? eventClock : TimeProvider.System);
        if (deferSupervisor) await db.Users.Where(x => x.Id == source.Supervisor).ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, UserStatus.Suspended));
        var result = await dispatcher.DispatchAsync(claim, fact, default); Assert.Equal("COMMITTED", result.Status); Assert.Equal((revoke ? 1 : 2) + (assigned && !deferSupervisor ? 1 : 0), result.Delivered);
        if (deferSupervisor)
        {
            Assert.Equal(1, result.Unresolved);
            await db.Users.Where(x => x.Id == source.Supervisor).ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, UserStatus.Active));
            await db.Set<H6NotificationDeliveryRow>().Where(x => x.RecipientUserId == source.Supervisor && x.Status == "UNRESOLVED").ExecuteUpdateAsync(x => x.SetProperty(v => v.NextAttemptAtUtc, DateTimeOffset.UtcNow.AddMinutes(-1)));
            Assert.Equal(1, await dispatcher.RetryUnresolvedAsync(default));
            Assert.Equal(0, await dispatcher.RetryUnresolvedAsync(default));
        }
        var recipients = await db.Notifications.Where(x => x.SourceEntityId == state.Item).Select(x => x.RecipientUserId).ToArrayAsync();
        Assert.Contains(source.Pm, recipients); Assert.DoesNotContain(late.Id, recipients); Assert.Equal(!revoke, recipients.Contains(second.Id));
        Assert.Equal(assigned, recipients.Contains(source.Supervisor)); if (!assigned) Assert.True(result.Unresolved > 0);
        await dispatcher.DispatchAsync(claim, fact, default);
        Assert.Equal((revoke ? 1 : 2) + (assigned ? 1 : 0), await db.Notifications.CountAsync(x => x.SourceEntityId == state.Item));
        Assert.Equal(1, await db.Set<RepairItemLifecycleEvent>().CountAsync(x => x.ItemId == state.Item && x.Kind == "SUBMITTED"));
        ApplicationUser? replacement = null;
        NotificationSupervisorResult? assignedB = null;
        var inbox = new H6ProtectedNotificationRepository(db, new IdempotencyOperationService(db), TimeProvider.System, [new H6RepairNotificationSourceAdapter(db)]);
        Guid? reviewNotification = null;
        string? readVersion = null;
        if (assigned)
        {
            reviewNotification = await db.Notifications.Where(x => x.SourceEntityId == state.Item && x.RecipientUserId == source.Supervisor).Select(x => x.Id).SingleAsync();
            var original = await inbox.GetAsync(source.Supervisor, reviewNotification.Value);
            Assert.NotNull(original); readVersion = Convert.ToBase64String(original.RowVersion);
            var marked = await inbox.MarkReadAsync(source.Supervisor, reviewNotification.Value, "read-A", new string('a', 64), readVersion);
            Assert.Equal(NotificationMarkReadPersistenceStatus.Success, marked.Status);
            replacement = new ApplicationUser { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString(), DisplayName = "Global B", PasswordHash = "fixture", RoleCode = UserRoleCode.Supervisor, Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow };
            db.Users.Add(replacement); await db.SaveChangesAsync();
            assignedB = await assignments.ExecuteAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, state.Package, state.Item,
                "PUT", "assign-B", IfMatch: '"' + assignedA!.Version + '"', Body: JsonSerializer.Serialize(new { supervisorId = replacement.Id })), default);
            Assert.Equal(200, assignedB.Status);
            Assert.Null(await inbox.GetAsync(source.Supervisor, reviewNotification.Value));
            Assert.Empty((await inbox.ListAsync(source.Supervisor, null, null, 1)).Items);
            Assert.Equal(NotificationMarkReadPersistenceStatus.NotFound,
                (await inbox.MarkReadAsync(source.Supervisor, reviewNotification.Value, "read-A", new string('a', 64), readVersion)).Status);
            Assert.Empty((await inbox.ListAsync(replacement.Id, null, null, 25)).Items); // no retroactive SUBMITTED
            Assert.NotNull(await inbox.GetAsync(source.Pm, await db.Notifications.Where(x => x.SourceEntityId == state.Item && x.RecipientUserId == source.Pm).Select(x => x.Id).SingleAsync()));
        }
        if (complete)
        {
            itemVersion = Convert.ToBase64String(await db.RepairItems.Where(x => x.Id == state.Item).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
            var review = new RepairAttemptReviewCommand(source.Pm, UserRoleCode.ProjectManager, source.Project, state.Package, state.Item,
                state.First.TaskId, new(intake.Id, "ACCEPT", "verified repair with source-backed AFTER evidence"), Guid.NewGuid().ToString(), itemVersion);
            Assert.Equal(403, (await repairs.ReviewAttemptAsync(review with { ActorId = source.Crew, Role = UserRoleCode.RepairCrew }, default)).Status);
            Assert.Equal(201, (await repairs.ReviewAttemptAsync(review, default)).Status);
            Assert.Equal(200, (await repairs.ReviewAttemptAsync(review, default)).Status);
            db.ChangeTracker.Clear();
            var final = new RepairFinalConfirmCommand(source.Pm, UserRoleCode.ProjectManager, source.Project, state.Package, state.Item,
                new("PM final for genuine FastTrack"), Guid.NewGuid().ToString(),
                Convert.ToBase64String(await db.RepairItems.Where(x => x.Id == state.Item).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync()));
            Assert.Equal(403, (await repairs.ConfirmFinalAsync(final with { ActorId = source.Supervisor, Role = UserRoleCode.Supervisor }, default)).Status);
            if (deferSupervisor)
            {
                await using var failing = sql.CreateDbContext(new FailFinalOutbox());
                await Assert.ThrowsAsync<InvalidOperationException>(() => new RepairWorkflowRepository(failing,
                    new IdempotencyOperationService(failing), TimeProvider.System).ConfirmFinalAsync(final, default));
                await using var check = sql.CreateDbContext();
                Assert.Equal(RepairItemState.Reviewed, (await check.RepairItems.SingleAsync(x => x.Id == state.Item)).State);
                var obligationId = await check.RepairItems.Where(x => x.Id == state.Item).Select(x => x.ObligationId).SingleAsync();
                Assert.False((await check.RepairObligations.SingleAsync(x => x.Id == obligationId)).IsResolved);
                Assert.False(await check.OutboxMessages.AnyAsync(x => x.MessageType == "repair.work.fast_track_confirmed.v1" && x.PayloadJson.Contains(state.Item.ToString())));
                Assert.False(await check.IdempotencyRecords.AnyAsync(x => x.IdempotencyKey == final.Key));
            }
            var extraGeneration = 0;
            if (raceFinal)
            {
                await using var finalDb = sql.CreateRetryingDbContext();
                await using var assignDb = sql.CreateRetryingDbContext();
                var raceAssignment = new NotificationSupervisorRepository(assignDb, new IdempotencyOperationService(assignDb), TimeProvider.System)
                    .ExecuteAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, state.Package, state.Item,
                        "PUT", "racing-final", IfMatch: '"' + assignedB!.Version + '"', Body: JsonSerializer.Serialize(new { supervisorId = replacement!.Id })), default);
                var raceDecision = new RepairWorkflowRepository(finalDb, new IdempotencyOperationService(finalDb), TimeProvider.System)
                    .ConfirmFinalAsync(final, default);
                await Task.WhenAll(raceAssignment, raceDecision);
                var finalResult = await raceDecision;
                var assignmentResult = await raceAssignment;
                Assert.Equal(201, finalResult.Status);
                Assert.Contains(assignmentResult.Status, new[] { 200, 409 });
                if (assignmentResult.Status == 200) { assignedB = assignmentResult; extraGeneration = 1; }
                db.ChangeTracker.Clear();
            }
            else Assert.Equal(201, (await repairs.ConfirmFinalAsync(final, default)).Status);
            Assert.Equal(200, (await repairs.ConfirmFinalAsync(final, default)).Status);
            db.ChangeTracker.Clear();
            var finalMessage = await db.OutboxMessages.SingleAsync(x => x.MessageType == "repair.work.fast_track_confirmed.v1" &&
                x.Id == db.RepairItems.Where(i => i.Id == state.Item).Select(i => i.EffectiveDecisionId).First());
            var finalPlan = H6NotificationCatalog.Parse(finalMessage.Id, finalMessage.MessageType, finalMessage.OccurredAtUtc, finalMessage.PayloadJson);
            Assert.NotEqual(message.Id, finalMessage.Id);
            Assert.Empty(finalPlan.Source.NotificationAssignmentSnapshot!.ProjectManagerIds);
            Assert.Equal(replacement?.Id, finalPlan.Source.NotificationAssignmentSnapshot.SupervisorId);
            if (assigned) Assert.Equal(assignedB!.Version, finalPlan.Source.NotificationAssignmentSnapshot.AssignmentVersion);
            var finalFence = Guid.NewGuid(); finalMessage.AcquireLease("h6:" + finalFence.ToString("N"), DateTimeOffset.UtcNow, TimeSpan.FromMinutes(2), 32);
            await db.SaveChangesAsync();
            var finalClaim = new H6Claim(finalMessage.Id, finalMessage.MessageType, finalMessage.OccurredAtUtc, finalMessage.PayloadJson,
                finalFence, finalMessage.LeaseExpiresAtUtc!.Value, finalMessage.DeliveryAttemptCount);
            var finalDelivery = await dispatcher.DispatchAsync(finalClaim, finalPlan, default);
            Assert.Equal("COMMITTED", finalDelivery.Status); Assert.Equal(assigned ? 1 : 0, finalDelivery.Delivered);
            await dispatcher.DispatchAsync(finalClaim, finalPlan, default);
            if (assigned)
            {
                var deliveredId = await db.Notifications.Where(x => x.SourceEntityId == state.Item && x.RecipientUserId == replacement!.Id).Select(x => x.Id).SingleAsync();
                Assert.NotNull(await inbox.GetAsync(replacement!.Id, deliveredId));
                Assert.Equal(409, (await assignments.ExecuteAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, state.Package, state.Item,
                    "PUT", "after-final", IfMatch: '"' + assignedB!.Version + '"', Body: JsonSerializer.Serialize(new { supervisorId = replacement.Id })), default)).Status);
                var revokeB = new NotificationSupervisorRequest(source.Pm, UserRoleCode.ProjectManager, source.Project, state.Package, state.Item,
                    "DELETE", "revoke-B", IfMatch: '"' + assignedB!.Version + '"');
                Assert.Equal(204, (await assignments.ExecuteAsync(revokeB, default)).Status);
                Assert.Equal(204, (await assignments.ExecuteAsync(revokeB, default)).Status);
                Assert.Null(await inbox.GetAsync(replacement.Id, deliveredId));
                Assert.Empty((await inbox.ListAsync(replacement.Id, null, null, 25)).Items);
                Assert.Equal(2 + extraGeneration, await db.Set<NotificationSupervisorGeneration>().CountAsync(x => x.ItemId == state.Item));
                Assert.Equal(3 + extraGeneration, await db.Set<NotificationSupervisorAudit>().CountAsync(x => x.ItemId == state.Item));
            }
            // Reopen a fresh SQL context: resolution must survive the producer context.
            await using var persisted = sql.CreateDbContext();
            var item = await persisted.RepairItems.SingleAsync(x => x.Id == state.Item);
            Assert.Equal(RepairItemState.Confirmed, item.State);
            Assert.True((await persisted.RepairObligations.SingleAsync(x => x.Id == item.ObligationId)).IsResolved);
            Assert.True((await persisted.RepairPackages.Include(x => x.Obligations).SingleAsync(x => x.Id == state.Package)).IsComplete);
            Assert.False(await persisted.Set<DeadlineClock>().AnyAsync(x => x.TargetId == state.Item &&
                (x.Kind == DeadlineClockKind.SupervisorInitialApproval || x.Kind == DeadlineClockKind.SupervisorFinalConfirmation)));
            Assert.Equal(assigned, (await persisted.Notifications.Where(x => x.SourceEntityId == state.Item).Select(x => x.RecipientUserId).ToArrayAsync()).Contains(source.Supervisor));
        }
    }
    private static ApplicationUser Manager()
    { var name = Guid.NewGuid().ToString(); return new() { Id = Guid.NewGuid(), UserName = name, NormalizedUserName = name.ToUpperInvariant(), DisplayName = "B2 fixture PM", PasswordHash = "fixture", RoleCode = UserRoleCode.ProjectManager, Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow }; }

    private sealed class IntakeClock : TimeProvider
    {
        public DateTimeOffset? Now { get; set; }
        public override DateTimeOffset GetUtcNow() => Now ?? DateTimeOffset.UtcNow;
    }

    private sealed class FailFinalOutbox : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
    {
        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> SavingChangesAsync(
            Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData eventData,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<OutboxMessage>().Any(x => x.State == EntityState.Added && x.Entity.MessageType == "repair.work.fast_track_confirmed.v1"))
                throw new InvalidOperationException("Controlled final event persistence failure");
            return ValueTask.FromResult(result);
        }
    }

}
