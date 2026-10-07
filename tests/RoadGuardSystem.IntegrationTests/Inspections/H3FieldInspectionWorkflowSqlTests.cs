using System.Text.Json;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.SqlClient;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Repositories.Implementations.Files;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Implementations.Defects;
using RoadGuardSystem.Repositories.Implementations.Cases;
using RoadGuardSystem.Repositories.Implementations.Retention;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Implementations.Defects;
using RoadGuardSystem.Services.Projects;
using Xunit;
namespace RoadGuardSystem.IntegrationTests.Inspections;

public sealed class H3FieldInspectionWorkflowSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [Fact]
    public async Task Populated_receiving_upgrade_preserves_real_intake_and_backfills_only_unacknowledged_request()
    {
        var isolated = new IdentitySqlServerFixture(); await isolated.InitializeAsync();
        try
        {
            var helper = new H3FieldInspectionWorkflowSqlTests(isolated);
            var s = await helper.Seed(); await using var db = isolated.CreateDbContext();
            var task = (await helper.Create(db, s, 4)).GetProperty("id").GetGuid(); await helper.Accept(db, s, task);
            var start = (await helper.Start(db, s, task)).GetProperty("id").GetGuid();
            var intake = (await helper.Submit(db, s, task, Input(start, null, true, null))).GetProperty("id").GetGuid();
            var original = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(x => x.TargetId == task);
            var migration = db.GetService<IMigrator>();
            await migration.MigrateAsync("20261006163740_H4SafetySourceAdmission");
            db.ChangeTracker.Clear();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE FieldInspectionTasks SET Status=5 WHERE Id={task}");
            var source = FieldInspectionReview.Create(Guid.NewGuid(), s.Project, task, intake, s.Pm, "SUPPLEMENT", "Historical actual supplement request", DateTimeOffset.UtcNow);
            db.Add(source); db.Entry(source).Property(x => x.ReceiptActivation).CurrentValue = "AWAITING_OWNER_RECEIPT_PROTOCOL";
            await db.SaveChangesAsync();
            await migration.MigrateAsync(); db.ChangeTracker.Clear();
            var request = await db.Set<BusinessReceivingRequest>().SingleAsync(x => x.ScopeId == task);
            Assert.Equal(source.Id, request.SourceId); Assert.Equal(source.OccurredAt, request.RequestedAt);
            Assert.Equal(s.Crew, request.ResponsibleActorId); Assert.Null(request.AcknowledgedAt); Assert.Null(request.ClockId);
            Assert.Equal("AWAITING_OWNER_RECEIPT_PROTOCOL", (await db.Set<FieldInspectionReview>().SingleAsync(x => x.Id == source.Id)).ReceiptActivation);
            var preserved = await db.Set<DeadlineClock>().SingleAsync(x => x.Id == original.Id);
            Assert.Equal(original.OriginEventId, preserved.OriginEventId); Assert.Equal(original.OriginalDueAt, preserved.OriginalDueAt);
            Assert.False(db.Database.HasPendingModelChanges());
            await Assert.ThrowsAsync<SqlException>(() => migration.MigrateAsync("20261006163740_H4SafetySourceAdmission"));
            Assert.True(await db.Set<BusinessReceivingRequest>().AnyAsync(x => x.Id == request.Id));
            await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE BusinessReceivingRequests SET SourceVersion='rewritten' WHERE Id={request.Id}"));
        }
        finally { await isolated.DisposeAsync(); }
    }
    [Theory]
    [InlineData(false, "extend")]
    [InlineData(true, "extend")]
    [InlineData(false, "appoint")]
    [InlineData(true, "appoint")]
    public async Task Production_extension_rollback_and_concurrency_preserve_one_original_clock(bool concurrency, string action)
    {
        var s = await Seed(); await using var db = sql.CreateDbContext();
        var task = (await Create(db, s, 4)).GetProperty("id").GetGuid(); await Accept(db, s, task);
        var start = (await Start(db, s, task)).GetProperty("id").GetGuid(); await Submit(db, s, task, Input(start, null, true, null));
        var supervisor = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = Guid.NewGuid().ToString(),
            DisplayName = "TEST_ONLY extension Supervisor",
            PasswordHash = "fixture",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.AddRange(supervisor, new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = s.Project,
            UserId = supervisor.Id,
            RoleCode = UserRoleCode.Supervisor,
            ValidFrom = new(2000, 1, 1),
            Status = ProjectMemberStatus.Active
        });
        await db.SaveChangesAsync(); var clock = await db.Set<DeadlineClock>().SingleAsync(x => x.TargetId == task);
        var command = new RoadGuardSystem.Repositories.Messaging.ClockDutyCommand(supervisor.Id, UserRoleCode.Supervisor, s.Project,
            clock.Id, action, Guid.NewGuid().ToString(), Convert.ToBase64String(clock.RowVersion), "Actual authorized clock decision",
            action == "extend" ? clock.OriginalDueAt.AddHours(4) : null, action == "appoint" ? s.Pm : null);
        async Task<RoadGuardSystem.Repositories.Messaging.BusinessDutyResult> Extend(string key)
        {
            await using var context = sql.CreateDbContext();
            return await new RoadGuardSystem.Repositories.Messaging.ClockDutyRepository(context, new IdempotencyOperationService(context),
                TimeProvider.System).ExecuteAsync(command with { Key = key }, default);
        }
        if (concurrency)
        {
            var outcomes = await Task.WhenAll(Extend(command.Key), Extend(Guid.NewGuid().ToString()));
            Assert.Contains(outcomes, x => x.Status == 201); Assert.Contains(outcomes, x => x.Status == 409);
        }
        else
        {
            await using var faulty = sql.CreateDbContext(new FailDutyReceipt());
            await Assert.ThrowsAsync<InvalidOperationException>(() => new RoadGuardSystem.Repositories.Messaging.ClockDutyRepository(faulty,
                new IdempotencyOperationService(faulty), TimeProvider.System).ExecuteAsync(command, default));
        }
        db.ChangeTracker.Clear(); var persisted = await db.Set<DeadlineClock>().Include(x => x.Extensions).Include(x => x.Appointments).SingleAsync(x => x.Id == clock.Id);
        Assert.Equal(clock.OriginalDueAt, persisted.OriginalDueAt);
        Assert.Equal(concurrency && action == "extend" ? clock.OriginalDueAt.AddHours(4) : clock.OriginalDueAt, persisted.CurrentDueAt);
        Assert.Equal(concurrency && action == "extend" ? 1 : 0, persisted.Extensions.Count);
        Assert.Equal(concurrency && action == "appoint" ? 1 : 0, persisted.Appointments.Count);
        Assert.Equal(concurrency && action == "appoint" ? s.Pm : (Guid?)null, persisted.AppointedActorId);
        Assert.Equal(concurrency ? 1 : 0, await db.AuditLogs.CountAsync(x => x.EntityId == clock.Id && x.EventType == "clock_" + action));
        Assert.Equal(concurrency ? 1 : 0, await db.IdempotencyRecords.CountAsync(x => x.ActorUserId == supervisor.Id && x.Operation == "activation.clock." + action + ".v1"));
        Assert.Equal(concurrency ? 1 : 0, await db.OutboxMessages.CountAsync(x => x.MessageType == "clock." + action + ".v1" && x.PayloadJson.Contains(clock.Id.ToString())));
        foreach (var prohibited in new[] { DeadlineClockKind.DangerAcknowledgment, DeadlineClockKind.FirstSafetyCheck,
            DeadlineClockKind.DeviceHandover, DeadlineClockKind.FastTrackExecution })
        {
            var fixtureClock = DeadlineClock.Create(Guid.NewGuid(), s.Project, prohibited, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
            db.Add(fixtureClock); await db.SaveChangesAsync();
            var denied = await new RoadGuardSystem.Repositories.Messaging.ClockDutyRepository(db, new IdempotencyOperationService(db), TimeProvider.System)
                .ExecuteAsync(command with { ClockId = fixtureClock.Id, ExpectedVersion = Convert.ToBase64String(fixtureClock.RowVersion), Key = Guid.NewGuid().ToString() }, default);
            Assert.Equal(409, denied.Status); Assert.Equal("clock_extension_prohibited", denied.Code);
        }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Concurrent_business_ack_persists_one_first_ack_and_one_clock(bool sameKey)
    {
        var s = await Seed(); await using var db = sql.CreateDbContext();
        var task = (await Create(db, s, 4)).GetProperty("id").GetGuid(); await Accept(db, s, task);
        var start = (await Start(db, s, task)).GetProperty("id").GetGuid();
        var submission = (await Submit(db, s, task, Input(start, null, true, null))).GetProperty("id").GetGuid();
        Assert.Equal(201, (await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, task, "review",
            new FieldReviewInput(submission, "SUPPLEMENT", "Concurrent actual request"))).Status);
        db.ChangeTracker.Clear(); var request = await db.Set<BusinessReceivingRequest>().SingleAsync(x => x.ScopeId == task);
        var command = new RoadGuardSystem.Repositories.Messaging.BusinessDutyCommand(s.Crew, UserRoleCode.RepairCrew, s.Project,
            request.Id, "ack", Guid.NewGuid().ToString(), Convert.ToBase64String(request.RowVersion), null, null, null);
        async Task<RoadGuardSystem.Repositories.Messaging.BusinessDutyResult> Ack(string key)
        {
            await using var context = sql.CreateDbContext();
            return await new RoadGuardSystem.Repositories.Messaging.BusinessDutyRepository(context, new IdempotencyOperationService(context),
                TimeProvider.System).ExecuteAsync(command with { Key = key }, default);
        }
        var results = await Task.WhenAll(Ack(command.Key), Ack(sameKey ? command.Key : Guid.NewGuid().ToString()));
        Assert.Contains(results, x => x.Status == 201);
        Assert.Contains(results, x => x.Status == (sameKey ? 200 : 409));
        db.ChangeTracker.Clear(); request = await db.Set<BusinessReceivingRequest>().Include(x => x.Clock).SingleAsync(x => x.Id == request.Id);
        Assert.Equal(1, await db.Set<DeadlineClock>().CountAsync(x => x.TargetId == request.Id));
        Assert.Equal(1, await db.AuditLogs.CountAsync(x => x.EntityId == request.Id && x.EventType == "business_request_received"));
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(x => x.ActorUserId == s.Crew && x.Operation == "activation.duty.ack.v1"));
        Assert.Equal(request.AcknowledgedAt!.Value.AddHours(48), request.Clock!.CurrentDueAt);
    }

    [Fact]
    public async Task Business_ack_receipt_failure_rolls_back_ack_clock_outbox_and_audit()
    {
        var s = await Seed(); await using var db = sql.CreateDbContext();
        var task = (await Create(db, s, 4)).GetProperty("id").GetGuid(); await Accept(db, s, task);
        var start = (await Start(db, s, task)).GetProperty("id").GetGuid();
        var submission = (await Submit(db, s, task, Input(start, null, true, null))).GetProperty("id").GetGuid();
        Assert.Equal(201, (await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, task, "review",
            new FieldReviewInput(submission, "SUPPLEMENT", "Actual request for rollback"))).Status);
        db.ChangeTracker.Clear(); var request = await db.Set<BusinessReceivingRequest>().SingleAsync(x => x.ScopeId == task);
        await using (var faulty = sql.CreateDbContext(new FailDutyReceipt()))
        {
            var command = new RoadGuardSystem.Repositories.Messaging.BusinessDutyCommand(s.Crew, UserRoleCode.RepairCrew,
                s.Project, request.Id, "ack", Guid.NewGuid().ToString(), Convert.ToBase64String(request.RowVersion), null, null, null);
            await Assert.ThrowsAsync<InvalidOperationException>(() => new RoadGuardSystem.Repositories.Messaging.BusinessDutyRepository(faulty,
                new IdempotencyOperationService(faulty), TimeProvider.System).ExecuteAsync(command, default));
        }
        db.ChangeTracker.Clear(); request = await db.Set<BusinessReceivingRequest>().SingleAsync(x => x.Id == request.Id);
        Assert.Null(request.AcknowledgedAt); Assert.Null(request.ClockId);
        Assert.False(await db.Set<DeadlineClock>().AnyAsync(x => x.TargetId == request.Id));
        Assert.False(await db.AuditLogs.AnyAsync(x => x.EntityId == request.Id));
        Assert.False(await db.OutboxMessages.AnyAsync(x => x.MessageType == "business.duty.ack.v1" && x.PayloadJson.Contains(request.Id.ToString())));
        Assert.False(await db.IdempotencyRecords.AnyAsync(x => x.ActorUserId == s.Crew && x.Operation == "activation.duty.ack.v1"));
    }
    private sealed class FailDutyReceipt : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<IdempotencyRecord>().Any(x => x.State == EntityState.Added &&
                x.Entity.Operation.StartsWith("activation.", StringComparison.Ordinal)))
                throw new InvalidOperationException("Injected activation receipt failure before commit.");
            return ValueTask.FromResult(result);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Genuine_review_breach_produces_Supervisor_request_then_server_ack_starts_24h(bool appointAfterAck)
    {
        var s = await Seed(); await using var db = sql.CreateDbContext();
        var task = (await Create(db, s, 4)).GetProperty("id").GetGuid(); await Accept(db, s, task);
        var start = (await Start(db, s, task)).GetProperty("id").GetGuid(); await Submit(db, s, task, Input(start, null, true, null));
        var supervisor = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = Guid.NewGuid().ToString(),
            DisplayName = "TEST_ONLY escalation Supervisor",
            PasswordHash = "fixture",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.AddRange(supervisor, new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = s.Project,
            UserId = supervisor.Id,
            RoleCode = UserRoleCode.Supervisor,
            ValidFrom = new(2000, 1, 1),
            Status = ProjectMemberStatus.Active
        });
        await db.SaveChangesAsync(); var review = await db.Set<DeadlineClock>().SingleAsync(x => x.TargetId == task);
        var time = new FixedDutyTime(review.OriginalDueAt.AddMinutes(1));
        var dispatcher = new RoadGuardSystem.Repositories.Messaging.H6NotificationDispatchRepository(db, time);
        await dispatcher.ObserveClocksAsync(default); db.ChangeTracker.Clear();
        var request = await db.Set<BusinessReceivingRequest>().SingleAsync(x => x.ScopeId == review.Id);
        Assert.Equal(DeadlineClockKind.SupervisorEscalation, request.Kind); Assert.Null(request.ClockId);
        Assert.Equal(supervisor.Id, request.ResponsibleActorId);
        Assert.True(await db.Set<DeadlineBreach>().AnyAsync(x => x.Id == request.SourceId && x.ClockId == review.Id));
        var repository = new RoadGuardSystem.Repositories.Messaging.BusinessDutyRepository(db, new IdempotencyOperationService(db), time);
        var command = new RoadGuardSystem.Repositories.Messaging.BusinessDutyCommand(supervisor.Id, UserRoleCode.Supervisor,
            s.Project, request.Id, "ack", Guid.NewGuid().ToString(), Convert.ToBase64String(request.RowVersion), null, null, null);
        if (appointAfterAck)
        {
            Assert.Equal(201, (await repository.ExecuteAsync(command, default)).Status);
            Assert.Equal(200, (await repository.ExecuteAsync(command, default)).Status);
        }
        db.ChangeTracker.Clear(); request = await db.Set<BusinessReceivingRequest>().Include(x => x.Clock).SingleAsync(x => x.Id == request.Id);
        if (appointAfterAck) { Assert.Equal(time.GetUtcNow(), request.AcknowledgedAt); Assert.Equal(time.GetUtcNow().AddHours(24), request.Clock!.OriginalDueAt); }
        else Assert.Null(request.ClockId);
        var replacement = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = Guid.NewGuid().ToString(),
            DisplayName = "TEST_ONLY substitute Supervisor",
            PasswordHash = "fixture",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.AddRange(replacement, new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = s.Project,
            UserId = replacement.Id,
            RoleCode = UserRoleCode.Supervisor,
            ValidFrom = new(2000, 1, 1),
            Status = ProjectMemberStatus.Active
        });
        await db.SaveChangesAsync();
        var appointment = command with
        {
            Action = "appoint",
            Key = Guid.NewGuid().ToString(),
            ExpectedVersion = Convert.ToBase64String(request.RowVersion),
            AssigneeId = replacement.Id,
            Reason = "Current eligible Supervisor substitutes immediately"
        };
        Assert.Equal(403, (await repository.ExecuteAsync(appointment with { ActorId = s.Pm, Role = UserRoleCode.ProjectManager }, default)).Status);
        Assert.Equal(201, (await repository.ExecuteAsync(appointment, default)).Status);
        Assert.Equal(403, (await repository.ExecuteAsync(command, default)).Status);
        db.ChangeTracker.Clear(); request = await db.Set<BusinessReceivingRequest>().Include(x => x.Clock).SingleAsync(x => x.Id == request.Id);
        var next = command with { ActorId = replacement.Id, Key = Guid.NewGuid().ToString(), ExpectedVersion = Convert.ToBase64String(request.RowVersion) };
        Assert.Equal(201, (await repository.ExecuteAsync(next, default)).Status);
        db.ChangeTracker.Clear(); request = await db.Set<BusinessReceivingRequest>().Include(x => x.Clock).SingleAsync(x => x.Id == request.Id);
        Assert.Equal(time.GetUtcNow().AddHours(24), (await db.Set<DeadlineClock>().SingleAsync(x => x.TargetId == request.Id)).OriginalDueAt);
        var extension = new RoadGuardSystem.Repositories.Messaging.ClockDutyCommand(supervisor.Id, UserRoleCode.Supervisor,
            s.Project, request.ClockId!.Value, "extend", Guid.NewGuid().ToString(), Convert.ToBase64String(request.Clock!.RowVersion),
            "Supervisor escalation duty needs more time", time.GetUtcNow().AddHours(25), null);
        Assert.Equal(201, (await new RoadGuardSystem.Repositories.Messaging.ClockDutyRepository(db, new IdempotencyOperationService(db), time)
            .ExecuteAsync(extension, default)).Status);
        await dispatcher.ObserveClocksAsync(default);
        Assert.Equal(1, await db.Set<BusinessReceivingRequest>().CountAsync(x => x.ScopeId == review.Id));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Genuine_task_reassignment_before_or_after_ack_never_resets_receiving_clock(bool acknowledgeFirst)
    {
        var s = await Seed(); await using var db = sql.CreateDbContext();
        var task = (await Create(db, s, 4)).GetProperty("id").GetGuid(); await Accept(db, s, task);
        var start = (await Start(db, s, task)).GetProperty("id").GetGuid();
        var submission = (await Submit(db, s, task, Input(start, null, true, null))).GetProperty("id").GetGuid();
        Assert.Equal(201, (await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, task, "review",
            new FieldReviewInput(submission, "SUPPLEMENT", "Actual supplement"))).Status);
        db.ChangeTracker.Clear(); var request = await db.Set<BusinessReceivingRequest>().SingleAsync(x => x.ScopeId == task);
        var repository = new RoadGuardSystem.Repositories.Messaging.BusinessDutyRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var command = new RoadGuardSystem.Repositories.Messaging.BusinessDutyCommand(s.Crew, UserRoleCode.RepairCrew, s.Project, request.Id,
            "ack", Guid.NewGuid().ToString(), Convert.ToBase64String(request.RowVersion), null, null, null);
        if (acknowledgeFirst) Assert.Equal(201, (await repository.ExecuteAsync(command, default)).Status);
        db.ChangeTracker.Clear(); request = await db.Set<BusinessReceivingRequest>().Include(x => x.Clock).SingleAsync(x => x.Id == request.Id);
        var oldDue = request.Clock?.OriginalDueAt; var oldAck = request.AcknowledgedAt;
        Assert.Equal(201, (await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, task, "reassign",
            new FieldTaskActionInput("Legitimate current Crew replacement", s.OtherCrew,
                new FieldHandoverInput("CAPTURED_INTAKE", "Prior actual intake and unfinished supplement", start, [submission], s.OtherCrew)))).Status);
        Assert.Equal(403, (await repository.ExecuteAsync(command, default)).Status);
        db.ChangeTracker.Clear(); request = await db.Set<BusinessReceivingRequest>().Include(x => x.Clock).Include(x => x.Appointments).SingleAsync(x => x.Id == request.Id);
        Assert.Equal(s.OtherCrew, request.ResponsibleActorId); Assert.Single(request.Appointments);
        var next = command with { ActorId = s.OtherCrew, Key = Guid.NewGuid().ToString(), ExpectedVersion = Convert.ToBase64String(request.RowVersion) };
        Assert.Equal(201, (await repository.ExecuteAsync(next, default)).Status);
        db.ChangeTracker.Clear(); request = await db.Set<BusinessReceivingRequest>().Include(x => x.Clock).SingleAsync(x => x.Id == request.Id);
        Assert.NotNull(request.AcknowledgedAt);
        if (acknowledgeFirst) { Assert.Equal(oldAck, request.AcknowledgedAt); Assert.Equal(oldDue, request.Clock!.OriginalDueAt); }
        else Assert.Equal(s.OtherCrew, request.AcknowledgedBy);
        Assert.Equal(1, await db.Set<DeadlineClock>().CountAsync(x => x.TargetId == request.Id));
    }
    [Fact]
    public async Task Production_extension_and_review_substitute_require_current_Supervisor_and_preserve_original_due()
    {
        var s = await Seed(); await using var db = sql.CreateDbContext();
        var task = (await Create(db, s, 4)).GetProperty("id").GetGuid();
        await Accept(db, s, task); var origin = (await Start(db, s, task)).GetProperty("id").GetGuid();
        await Submit(db, s, task, Input(origin, null, true, null));
        var supervisor = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = Guid.NewGuid().ToString(),
            DisplayName = "TEST_ONLY Supervisor",
            PasswordHash = "fixture",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.AddRange(supervisor, new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = s.Project,
            UserId = supervisor.Id,
            RoleCode = UserRoleCode.Supervisor,
            ValidFrom = new(2000, 1, 1),
            Status = ProjectMemberStatus.Active
        });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var clock = await db.Set<DeadlineClock>().SingleAsync(x => x.TargetId == task);
        var due = clock.OriginalDueAt; var now = due.AddMinutes(1);
        var repository = new RoadGuardSystem.Repositories.Messaging.ClockDutyRepository(db, new IdempotencyOperationService(db), new FixedDutyTime(now));
        var command = new RoadGuardSystem.Repositories.Messaging.ClockDutyCommand(supervisor.Id, UserRoleCode.Supervisor,
            s.Project, clock.Id, "extend", Guid.NewGuid().ToString(), Convert.ToBase64String(clock.RowVersion), "Actual review needs more time",
            due.AddHours(4), null);
        Assert.Equal(403, (await repository.ExecuteAsync(command with { ActorId = s.Pm, Role = UserRoleCode.ProjectManager }, default)).Status);
        Assert.Equal(201, (await repository.ExecuteAsync(command, default)).Status);
        Assert.Equal(200, (await repository.ExecuteAsync(command, default)).Status);
        db.ChangeTracker.Clear(); clock = await db.Set<DeadlineClock>().Include(x => x.Extensions).Include(x => x.Breaches).SingleAsync(x => x.Id == clock.Id);
        Assert.Equal(due, clock.OriginalDueAt); Assert.Equal(due.AddHours(4), clock.CurrentDueAt);
        Assert.Single(clock.Extensions); Assert.Single(clock.Breaches);
        var substitute = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = Guid.NewGuid().ToString(),
            DisplayName = "TEST_ONLY substitute PM",
            PasswordHash = "fixture",
            RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.AddRange(substitute, new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = s.Project,
            UserId = substitute.Id,
            RoleCode = UserRoleCode.ProjectManager,
            ValidFrom = new(2000, 1, 1),
            Status = ProjectMemberStatus.Active
        });
        await db.SaveChangesAsync();
        var appointment = command with
        {
            Action = "appoint",
            Key = Guid.NewGuid().ToString(),
            ExpectedVersion = Convert.ToBase64String(clock.RowVersion),
            AssigneeId = substitute.Id,
            NewDueAt = null,
            Reason = "Current review duty assignment"
        };
        Assert.Equal(201, (await repository.ExecuteAsync(appointment, default)).Status);
        db.ChangeTracker.Clear(); clock = await db.Set<DeadlineClock>().Include(x => x.Appointments).SingleAsync(x => x.Id == clock.Id);
        Assert.Equal(substitute.Id, clock.AppointedActorId); Assert.Single(clock.Appointments); Assert.Equal(due.AddHours(4), clock.CurrentDueAt);
        var intakeId = await db.FieldInspectionSubmissions.Where(x => x.TaskId == task).Select(x => x.Id).SingleAsync();
        Assert.Equal(403, (await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, task, "review",
            new FieldReviewInput(intakeId, "SUPPLEMENT", "Replaced PM cannot review"))).Status);
        Assert.Equal(201, (await Cmd(db, s, substitute.Id, UserRoleCode.ProjectManager, task, "review",
            new FieldReviewInput(intakeId, "SUPPLEMENT", "Current appointed PM requests actual evidence"))).Status);
        Assert.Equal(409, (await repository.ExecuteAsync(command with { Key = Guid.NewGuid().ToString() }, default)).Status);
        var member = await db.ProjectMembers.SingleAsync(x => x.ProjectId == s.Project && x.UserId == supervisor.Id);
        member.Status = ProjectMemberStatus.Ended; await db.SaveChangesAsync();
        Assert.Equal(403, (await repository.ExecuteAsync(command, default)).Status);
    }
    private sealed class FixedDutyTime(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
    [Fact]
    public async Task Genuine_supplement_request_ack_and_reassignment_preserve_first_clock()
    {
        var s = await Seed(); await using var db = sql.CreateDbContext();
        var task = (await Create(db, s, 4)).GetProperty("id").GetGuid();
        await Accept(db, s, task); var origin = (await Start(db, s, task)).GetProperty("id").GetGuid();
        var submission = (await Submit(db, s, task, Input(origin, null, true, null))).GetProperty("id").GetGuid();
        Assert.Equal(201, (await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, task, "review",
            new FieldReviewInput(submission, "SUPPLEMENT", "Actual missing evidence"))).Status);
        db.ChangeTracker.Clear();
        var request = await db.Set<BusinessReceivingRequest>().AsNoTracking().SingleAsync(x => x.ScopeId == task);
        Assert.Null(request.ClockId); Assert.Null(request.AcknowledgedAt);
        var repository = new RoadGuardSystem.Repositories.Messaging.BusinessDutyRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var version = Convert.ToBase64String(request.RowVersion);
        var command = new RoadGuardSystem.Repositories.Messaging.BusinessDutyCommand(s.Crew, UserRoleCode.RepairCrew,
            s.Project, request.Id, "ack", Guid.NewGuid().ToString(), version, null, null, DateTimeOffset.UtcNow.AddDays(-1));
        Assert.Equal(403, (await repository.ExecuteAsync(command with { ActorId = s.Pm, Role = UserRoleCode.ProjectManager }, default)).Status);
        var ack = await repository.ExecuteAsync(command, default); Assert.Equal(201, ack.Status);
        Assert.Equal(200, (await repository.ExecuteAsync(command, default)).Status);
        db.ChangeTracker.Clear();
        var durable = await db.Set<BusinessReceivingRequest>().Include(x => x.Clock).SingleAsync(x => x.Id == request.Id);
        Assert.NotNull(durable.AcknowledgedAt); Assert.Equal(durable.AcknowledgedAt!.Value.AddHours(48), durable.Clock!.OriginalDueAt);
        Assert.True(durable.ClaimedDeviceAt < durable.AcknowledgedAt);
        var originalDue = durable.Clock.OriginalDueAt;
        var extension = new RoadGuardSystem.Repositories.Messaging.ClockDutyCommand(s.Pm, UserRoleCode.ProjectManager, s.Project,
            durable.Clock.Id, "extend", Guid.NewGuid().ToString(), Convert.ToBase64String(durable.Clock.RowVersion),
            "Current PM extends actual Crew supplement", originalDue.AddHours(2), null);
        Assert.Equal(201, (await new RoadGuardSystem.Repositories.Messaging.ClockDutyRepository(db, new IdempotencyOperationService(db), TimeProvider.System)
            .ExecuteAsync(extension, default)).Status);
        Assert.Equal(1, await db.Set<DeadlineClock>().CountAsync(x => x.TargetId == request.Id));
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(x => x.Operation == "activation.duty.ack.v1" && x.ActorUserId == s.Crew));
        Assert.Equal(1, await db.AuditLogs.CountAsync(x => x.EntityId == request.Id && x.EventType == "business_request_received"));
        var read = await repository.ReadAsync(s.Crew, UserRoleCode.RepairCrew, s.Project, request.Id, default);
        Assert.Equal(200, read.Status);
        var file = await File(db, s, task); await Submit(db, s, task, Input(origin, submission, false, file));
        db.ChangeTracker.Clear(); durable = await db.Set<BusinessReceivingRequest>().Include(x => x.Clock).SingleAsync(x => x.Id == request.Id);
        Assert.NotNull(durable.CompletedAt); Assert.NotNull(durable.Clock!.CompletedAt); Assert.Equal(originalDue, durable.Clock.OriginalDueAt);
        Assert.Equal(409, (await repository.ExecuteAsync(command with { Key = Guid.NewGuid().ToString() }, default)).Status);
    }
    [Fact]
    public async Task Reporter_noSurvey_unknown_intake_supplement_zero_AREA_and_real_FIELD_consumer_preserve_original_clock()
    {
        var s = await Seed(); await using var db = sql.CreateDbContext(); var task = await Create(db, s, 4);
        var id = task.GetProperty("id").GetGuid(); Assert.Equal(JsonValueKind.Null, task.GetProperty("surveyId").ValueKind);
        await Accept(db, s, id); var start = await Start(db, s, id); var origin = start.GetProperty("id").GetGuid();
        var first = await Submit(db, s, id, Input(origin, null, true, null)); var firstId = first.GetProperty("id").GetGuid();
        Assert.Equal("INCOMPLETE", first.GetProperty("readiness").GetString());
        var clock = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(x => x.TargetId == id); var due = clock.OriginalDueAt;
        Assert.Equal(first.GetProperty("serverReceivedAt").GetDateTimeOffset().AddHours(24), due);
        Assert.Equal(409, (await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, id, "review", new FieldReviewInput(firstId, "CONFIRM", "cannot invent readiness"))).Status);
        var supplement = await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, id, "review", new FieldReviewInput(firstId, "SUPPLEMENT", "need actual measurements"));
        Assert.Equal(201, supplement.Status); Assert.Equal("BUSINESS_ACK_REQUIRED", AsJson(supplement).GetProperty("receiptActivation").GetString());
        var file = await File(db, s, id); var second = await Submit(db, s, id, Input(origin, firstId, false, file)); var secondId = second.GetProperty("id").GetGuid();
        Assert.Equal(firstId, second.GetProperty("rootId").GetGuid()); Assert.Equal(2, second.GetProperty("revision").GetInt32()); Assert.Equal("READY", second.GetProperty("readiness").GetString());
        Assert.Equal(due, (await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(x => x.TargetId == id)).OriginalDueAt);
        Assert.Equal(1, await db.Set<DeadlineClock>().CountAsync(x => x.TargetId == id));
        Assert.Equal(201, (await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, id, "review", new FieldReviewInput(secondId, "CONFIRM", "actual known zero area with source-bound checklist"))).Status);
        db.ChangeTracker.Clear(); var measurements = await db.GroundTruthMeasurements.AsNoTracking().Where(x => x.DefectId == s.Defect).ToArrayAsync();
        Assert.Contains(measurements, x => x.Value is null && x.ValueState == "UNKNOWN"); Assert.Contains(measurements, x => x.Value == 0 && x.Unit == "m²" && x.Dimension == "AREA");
        var producer = Producer(db); var source = await producer.ResolveFieldSourceAsync(s.Pm, UserRoleCode.ProjectManager, s.Project, s.Defect, id, secondId, second.GetProperty("contentHash").GetString()!);
        Assert.Equal(AnhHuyProducerStatus.Ready, source.Status); var evidence = source.Facts!.EvidenceIds;
        var service = new DefectWorkflowService(new DefectWorkflowRepository(db), new CandidateDecisionRepository(db), new CaseWorkflowRepository(db), producer, Guard(db), new IdempotencyOperationService(db));
        var version = Convert.ToBase64String(await db.Defects.Where(x => x.Id == s.Defect).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
        var result = await service.VerifyAsync(s.Pm, UserRoleCode.ProjectManager, s.Project, s.Defect, new("CONFIRM", "FIELD", evidence, "FIELD completed", id, secondId, source.Facts.ContentHash), Guid.NewGuid().ToString(), "\"" + version + "\"", null, default);
        Assert.Equal(200, result.Status); Assert.Equal("VERIFIED", result.Defect!.Status);
        var log = await db.DefectVerificationLogs.AsNoTracking().SingleAsync(x => x.DefectId == s.Defect); Assert.Equal(id, log.FieldInspectionTaskId); Assert.Contains("FIELD", log.AfterSnapshot!);
        var inventory = new Huy02InspectionRetentionContributor(db); Assert.Contains(file, await inventory.KnownProjectFilesAsync(s.Project, default));
        var retention = await inventory.ReadAsync(file, default); Assert.True(retention.Complete); Assert.Contains(retention.References, x => x.Kind == "FIELD_INSPECTION_EVIDENCE");
        Assert.Equal(retention.References.Select(x => x.SourceVersion), (await inventory.ReadAsync(file, default)).References.Select(x => x.SourceVersion));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Actual_V2_validation_pair_consumer_retains_known_zero_and_excludes_unknown_null(bool unknown)
    {
        var s = await Seed(); await using var db = sql.CreateDbContext(); var task = (await Create(db, s, 1)).GetProperty("id").GetGuid(); await Accept(db, s, task); var start = (await Start(db, s, task)).GetProperty("id").GetGuid();
        var input = Input(start, null, unknown, null) with { Measurements = [new("depth", "DepressionDepth", unknown ? null : 0, unknown ? "UNKNOWN" : "KNOWN", unknown ? "instrument unavailable" : null, "LENGTH", "mm", null, null, "GPS unavailable", "depth gauge", "actual captured measurement")] };
        await Submit(db, s, task, input); var ground = await db.GroundTruthMeasurements.AsNoTracking().SingleAsync(x => x.DefectId == s.Defect); Assert.Equal(unknown ? null : 0, ground.Value);
        var now = DateTimeOffset.UtcNow; var survey = Survey.Create(Guid.NewGuid(), null, s.Project, s.Route, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var data = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.Draft, SurveyDataIntegrityStatus.Pending, null, null, "[]");
        var model = AIModelVersion.Create(Guid.NewGuid(), "measurement-validation", "H3-" + Guid.NewGuid().ToString("N"), "file:///models/validation-fixture", null, null, AIModelVersionStatus.Released, now, s.Pm);
        var derived = DerivedMeasurement.Create(Guid.NewGuid(), data.Id, s.Route, ground.SampleId, MeasurementType.DepressionDepth, 0, "mm", null, DerivedMeasurementSourceType.ManualDerived, "fixture-v1", now, DerivedMeasurementStatus.Published);
        db.AddRange(survey, data, model, derived); await db.SaveChangesAsync();
        var key = Guid.NewGuid().ToString(); var pairs = JsonSerializer.Serialize(new[] { new { groundTruthId = ground.Id, derivedMeasurementId = derived.Id } });
        var request = new RoadGuardSystem.Repositories.Processing.ValidationRunCreateRequest(s.Pm, s.Project, model.Id, "isolated-source-pair", "DEPRESSION_DEPTH", "mm", pairs, key, new string('a', 64), null);
        var consumer = new RoadGuardSystem.Repositories.Implementations.Processing.ProcessingV2PersistenceService(db, new IdempotencyOperationService(db));
        var result = await consumer.CreateValidationAsync(request); Assert.Equal(unknown ? RoadGuardSystem.Repositories.Processing.ProcessingJobPersistenceStatus.InvalidInput : RoadGuardSystem.Repositories.Processing.ProcessingJobPersistenceStatus.Success, result.Status);
        var replay = await consumer.CreateValidationAsync(request); Assert.Equal(unknown ? RoadGuardSystem.Repositories.Processing.ProcessingJobPersistenceStatus.InvalidInput : RoadGuardSystem.Repositories.Processing.ProcessingJobPersistenceStatus.Replayed, replay.Status);
        if (unknown) { Assert.Null(result.Run); Assert.Equal(0, await db.ValidationRuns.CountAsync(x => x.ProjectId == s.Project)); Assert.Equal(0, await db.MeasurementValidationSamples.CountAsync(x => x.GroundTruthMeasurementId == ground.Id)); Assert.Equal(0, await db.AuditLogs.CountAsync(x => x.EventType == "validation_run_created" && x.ActorUserId == s.Pm)); Assert.Equal(0, await db.OutboxMessages.CountAsync(x => x.MessageType == "validation_run.dispatch" && x.PayloadJson.Contains(model.Id.ToString()))); }
        else { Assert.NotNull(result.Run); Assert.Equal(1, await db.ValidationRuns.CountAsync(x => x.ProjectId == s.Project)); var sample = await db.MeasurementValidationSamples.SingleAsync(x => x.GroundTruthMeasurementId == ground.Id); Assert.Equal(derived.Id, sample.DerivedMeasurementId); Assert.Equal(0, sample.SignedError); Assert.Equal(result.Run.Id, replay.Run!.Id); }
    }
    [Fact]
    public async Task Prior_existing_evidence_receipt_shape_replays_and_conflicts_only_under_current_authority()
    {
        var s = await Seed(); await using var db = sql.CreateDbContext(); var project = s.Project; var defect = s.Defect; var expected = Convert.ToBase64String(await db.Defects.Where(x => x.Id == defect).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
        var request = new DefectVerificationRequestDto("CONFIRM", "EXISTING_EVIDENCE", [s.SourceEvidence], "historical receipt"); var reason = request.Reason!; var evidence = request.EvidenceIds!; var key = Guid.NewGuid().ToString();
        var fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { project, defect, expected, request.Decision, request.VerificationMethod, reason, evidence }))).ToLowerInvariant();
        var view = new DefectViewDto(defect, project, s.Route, null, "historic", null, "LOW", "VERIFIED", null, expected);
        db.Add(IdempotencyRecord.Create(s.Pm, project, "huy01.defect.verify.v1", key, fingerprint, Guid.NewGuid(), JsonSerializer.Serialize(view), DateTimeOffset.UtcNow)); await db.SaveChangesAsync();
        // Receipt compatibility is isolated from geometry readiness. SQL current authority/source locks remain real; the already-resolved candidate facts are a narrow producer fixture.
        var link = await db.Set<HuyDefectSourceLink>().AsNoTracking().SingleAsync(x => x.DefectId == defect); var candidate = await new AnhHuyFactsRepository(db).GetReportSourceAsync(link.ReportSourceId!.Value, default);
        var incident = await db.IncidentCases.SingleAsync(x => x.Id == candidate!.CaseId); db.Entry(incident).Property<Guid?>("GeometryRouteVersionId").CurrentValue = s.Route; db.Entry(incident).Property<Guid?>("GeometrySegmentSetId").CurrentValue = s.Set; await db.SaveChangesAsync();
        var source = CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.Report, candidate!.ReportId, "receipt-compatible-fixture"), project, "geometry-fixture");
        var service = new DefectWorkflowService(new DefectWorkflowRepository(db), new CandidateDecisionRepository(db), new CaseWorkflowRepository(db), new ReceiptCandidateProducer(new(source, candidate.CaseId, evidence, [], null!)), Guard(db), new IdempotencyOperationService(db));
        var replay = await service.VerifyAsync(s.Pm, UserRoleCode.ProjectManager, project, defect, request, key, "\"" + expected + "\"", null, default); Assert.True(replay.Status == 200, $"prior receipt {replay.Status}/{replay.Code}"); Assert.Equal("VERIFIED", replay.Defect!.Status);
        Assert.Equal(409, (await service.VerifyAsync(s.Pm, UserRoleCode.ProjectManager, project, defect, request with { Reason = "changed body" }, key, "\"" + expected + "\"", null, default)).Status);
        Assert.Equal(0, await db.DefectVerificationLogs.CountAsync(x => x.DefectId == defect));
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE ProjectId={project} AND UserId={s.Pm}"); db.ChangeTracker.Clear();
        Assert.Equal(403, (await service.VerifyAsync(s.Pm, UserRoleCode.ProjectManager, project, defect, request, key, "\"" + expected + "\"", null, default)).Status);
    }
    [Fact]
    public async Task Concurrent_cross_kind_origin_on_two_actual_tasks_has_one_canonical_winner()
    {
        var s = await Seed(); Guid first, second, start; await using (var db = sql.CreateDbContext()) { first = (await Create(db, s, 4)).GetProperty("id").GetGuid(); second = (await Create(db, s, 4)).GetProperty("id").GetGuid(); await Accept(db, s, first); await Accept(db, s, second); start = (await Start(db, s, second)).GetProperty("id").GetGuid(); }
        var origin = Guid.NewGuid();
        async Task<FieldWorkflowResult> Run(Guid task, string action, object input) { await using var db = sql.CreateDbContext(); return await Cmd(db, s, s.Crew, UserRoleCode.RepairCrew, task, action, input); }
        var results = await Task.WhenAll(Run(first, "start", new FieldStartInput(origin, DateTimeOffset.UtcNow)), Run(second, "submit", Input(start, null, true, null) with { OriginId = origin }));
        Assert.Single(results.Where(x => x.Status == 201)); Assert.Single(results.Where(x => x.Status == 409)); await using var check = sql.CreateDbContext(); Assert.Equal(1, await check.Set<FieldInspectionOperationOrigin>().CountAsync(x => x.ProjectId == s.Project && x.OriginId == origin));
        Assert.Equal(1, await check.Set<FieldTaskStartOrigin>().CountAsync(x => x.TaskId == first) + await check.Set<FieldInspectionSubmission>().CountAsync(x => x.TaskId == second));
    }
    [Fact]
    public async Task Malformed_null_measurement_is_rejected_without_intake_clock_or_history()
    {
        var s = await Seed(); await using var db = sql.CreateDbContext(); var task = await Create(db, s, 4); var id = task.GetProperty("id").GetGuid(); await Accept(db, s, id); var start = await Start(db, s, id);
        var bad = Input(start.GetProperty("id").GetGuid(), null, true, null) with { Measurements = [null!] };
        Assert.Equal(400, (await Cmd(db, s, s.Crew, UserRoleCode.RepairCrew, id, "submit", bad)).Status);
        Assert.Equal(0, await db.Set<FieldInspectionSubmission>().CountAsync(x => x.TaskId == id)); Assert.Equal(0, await db.Set<DeadlineClock>().CountAsync(x => x.TargetId == id));
    }
    [Fact]
    public async Task Current_assignment_and_membership_guard_fresh_replay_conflict_and_geometry()
    {
        var s = await Seed(); await using var db = sql.CreateDbContext(); var task = await Create(db, s, 4); var id = task.GetProperty("id").GetGuid();
        Assert.Equal(403, (await Cmd(db, s, s.OtherCrew, UserRoleCode.RepairCrew, id, "get", null)).Status);
        Assert.Equal(403, (await Cmd(db, s, s.OtherCrew, UserRoleCode.RepairCrew, id, "geometry", null)).Status);
        var key = Guid.NewGuid().ToString(); var version = await Version(db, id); var accept = new FieldTaskActionInput("accept actual task");
        Assert.Equal(201, (await Cmd(db, s, s.Crew, UserRoleCode.RepairCrew, id, "accept", accept, key, version)).Status);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE ProjectId={s.Project} AND UserId={s.Crew}"); db.ChangeTracker.Clear();
        Assert.Equal(403, (await Cmd(db, s, s.Crew, UserRoleCode.RepairCrew, id, "accept", accept, key, version)).Status);
        Assert.Equal(403, (await Cmd(db, s, s.Crew, UserRoleCode.RepairCrew, id, "accept", new FieldTaskActionInput("changed fingerprint"), key, version)).Status);
        Assert.Equal(403, (await Cmd(db, s, s.Crew, UserRoleCode.RepairCrew, id, "start", new FieldStartInput(Guid.NewGuid(), DateTimeOffset.UtcNow))).Status);
        Assert.Equal(1, await db.Set<FieldInspectionTaskEvent>().CountAsync(x => x.TaskId == id && x.Kind == "ACCEPTED"));
    }
    [Fact]
    public async Task Global_origin_same_hash_different_keys_has_one_effect_and_cross_kind_reclassification_is_denied()
    {
        var s = await Seed(); Guid task; string version; await using (var db = sql.CreateDbContext()) { task = (await Create(db, s, 4)).GetProperty("id").GetGuid(); await Accept(db, s, task); version = await Version(db, task); }
        var input = new FieldStartInput(Guid.NewGuid(), DateTimeOffset.UtcNow.AddYears(-2), Guid.NewGuid(), 55, "boot", "offline claimed proof");
        async Task<FieldWorkflowResult> StartOnce() { await using var db = sql.CreateDbContext(); return await Cmd(db, s, s.Crew, UserRoleCode.RepairCrew, task, "start", input, Guid.NewGuid().ToString(), version); }
        var results = await Task.WhenAll(StartOnce(), StartOnce()); Assert.All(results, x => Assert.True(x.Status is 200 or 201)); Assert.Equal(AsJson(results[0]).GetProperty("id").GetGuid(), AsJson(results[1]).GetProperty("id").GetGuid());
        await using var check = sql.CreateDbContext(); var start = await check.Set<FieldTaskStartOrigin>().SingleAsync(x => x.TaskId == task); Assert.Null(start.VerifiedOriginalAt); Assert.Equal(input.ClaimedAt, start.ClaimedAt);
        Assert.Equal(1, await check.Set<FieldInspectionOperationOrigin>().CountAsync(x => x.ProjectId == s.Project && x.OriginId == input.OriginId));
        var reclassified = Input(start.Id, null, true, null) with { OriginId = input.OriginId };
        Assert.Equal(409, (await Cmd(check, s, s.Crew, UserRoleCode.RepairCrew, task, "submit", reclassified)).Status);
        Assert.Equal(0, await check.Set<FieldInspectionSubmission>().CountAsync(x => x.TaskId == task));
        await Assert.ThrowsAsync<SqlException>(() => check.Database.ExecuteSqlInterpolatedAsync($"UPDATE FieldTaskStartOrigins SET ClaimedAt=SYSUTCDATETIME() WHERE Id={start.Id}"));
    }
    [Fact]
    public async Task In_progress_reassignment_requires_performed_facts_preserves_first_origin_and_revokes_old_crew()
    {
        var s = await Seed(); await using var db = sql.CreateDbContext(); var task = await Create(db, s, 4); var id = task.GetProperty("id").GetGuid(); await Accept(db, s, id); var start = await Start(db, s, id); var origin = start.GetProperty("id").GetGuid();
        Assert.Equal(400, (await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, id, "reassign", new FieldTaskActionInput("reason alone inadequate", s.OtherCrew))).Status);
        var action = new FieldTaskActionInput("handover actual performed portion", s.OtherCrew, new("PARTIAL", "measuring route marker only", origin, [], s.OtherCrew));
        Assert.Equal(201, (await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, id, "reassign", action)).Status);
        Assert.Equal(403, (await Cmd(db, s, s.Crew, UserRoleCode.RepairCrew, id, "get", null)).Status);
        Assert.Equal(origin, (await db.Set<FieldTaskStartOrigin>().AsNoTracking().SingleAsync(x => x.TaskId == id)).Id);
        var history = await db.Set<FieldInspectionTaskEvent>().AsNoTracking().SingleAsync(x => x.TaskId == id && x.Kind == "REASSIGNED"); Assert.Contains("PARTIAL", history.FactsJson);
    }
    [Fact]
    public async Task Range_valid_GPS_and_contradictory_checklist_cannot_confirm_accuracy()
    {
        var s = await Seed(); await using var db = sql.CreateDbContext(); var task = await Create(db, s, 4); var id = task.GetProperty("id").GetGuid(); await Accept(db, s, id); var start = await Start(db, s, id); var file = await File(db, s, id);
        var payload = Input(start.GetProperty("id").GetGuid(), null, false, file) with { LocationProof = new("GPS_CAPTURE", "client claims verified", 106, 10) };
        var submission = await Submit(db, s, id, payload); Assert.Equal("INCOMPLETE", submission.GetProperty("readiness").GetString());
        Assert.Contains(submission.GetProperty("missingReasons").EnumerateArray(), x => x.GetString() == "GPS_SOURCE_BINDING_UNAVAILABLE");
        Assert.Equal(409, (await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, id, "review", new FieldReviewInput(submission.GetProperty("id").GetGuid(), "CONFIRM", "range alone is not task binding"))).Status);
    }
    [Fact]
    public async Task Modern_and_legacy_impact_inventory_preserves_actual_pins_and_FIELD_continue_has_real_history()
    {
        var s = await Seed(); await using var db = sql.CreateDbContext(); var modern = (await Create(db, s, 4)).GetProperty("id").GetGuid(); await Accept(db, s, modern); var start = await Start(db, s, modern);
        var survey = RoadGuardSystem.BusinessObjects.Surveys.Survey.Create(Guid.NewGuid(), null, s.Project, s.Route, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var legacy = FieldInspectionTask.Create(Guid.NewGuid(), "LEGACY-" + Guid.NewGuid().ToString("N"), s.Project, s.Defect, survey.Id, s.Route, 1, "{}", null, null, DateTimeOffset.UtcNow.AddDays(1), FieldInspectionTaskStatus.Accepted, s.Pm, null, null, null, null);
        var route = await db.RoadSectionVersions.SingleAsync(x => x.Id == s.Route); var replacement = RoadSectionVersion.Create(Guid.NewGuid(), route.RoadSectionId, 2, false, route.Geometry, DateTimeOffset.UtcNow, "replacement"); db.AddRange(survey, legacy, replacement); await db.SaveChangesAsync();
        var pavement = new PavementWorkflowService(new PavementWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System), Guard(db));
        var impact = await pavement.ExecuteAsync(UserRoleCode.ProjectManager, new(s.Pm, s.Project, "impact-create", Input: new GeometryImpactInput(s.Route, replacement.Id, "new geometry requires explicit reevaluation"), Key: Guid.NewGuid().ToString())); Assert.Equal(201, impact.Status); var json = (JsonElement)impact.Value!;
        var refs = json.GetProperty("references").EnumerateArray().Where(x => x.GetProperty("kind").GetString() == "FIELD_TASK").ToArray();
        var current = refs.Single(x => x.GetProperty("id").GetGuid() == modern); Assert.Equal(s.Set, current.GetProperty("segmentSetId").GetGuid()); Assert.Equal("PINNED_REFERENCE", current.GetProperty("readiness").GetString());
        Assert.Equal("LEGACY_SEGMENT_SET_UNPINNED", refs.Single(x => x.GetProperty("id").GetGuid() == legacy.Id).GetProperty("missingReasonCodes")[0].GetString());
        var action = await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, modern, "impact", new FieldLocationImpactActionInput(json.GetProperty("id").GetGuid(), "CONTINUE", "keep actual old pins")); Assert.Equal(201, action.Status); var applied = AsJson(action); Assert.Equal("APPLIED", applied.GetProperty("executionStatus").GetString());
        Assert.Equal(s.Route, (await db.FieldInspectionTasks.AsNoTracking().SingleAsync(x => x.Id == modern)).RoadSectionVersionId); Assert.Equal(start.GetProperty("id").GetGuid(), (await db.Set<FieldTaskStartOrigin>().AsNoTracking().SingleAsync(x => x.TaskId == modern)).Id);
        var history = await db.Set<FieldInspectionTaskEvent>().AsNoTracking().SingleAsync(x => x.TaskId == modern && x.Kind == "IMPACT_CONTINUE"); Assert.Equal(applied.GetProperty("id").GetGuid(), history.LocationImpactDecisionId); Assert.Equal(json.GetProperty("id").GetGuid(), history.LocationImpactId);
    }
    [Fact]
    public async Task Immutable_submission_SQL_denies_second_root_reused_session_and_wrong_original_inspector()
    {
        var s = await Seed(); Guid task, start, first; await using (var db = sql.CreateDbContext()) { task = (await Create(db, s, 4)).GetProperty("id").GetGuid(); await Accept(db, s, task); start = (await Start(db, s, task)).GetProperty("id").GetGuid(); first = (await Submit(db, s, task, Input(start, null, true, null))).GetProperty("id").GetGuid(); }
        async Task<SqlException> Invalid(bool root, bool wrongActor)
        {
            await using var db = sql.CreateDbContext(); var old = await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleAsync(x => x.Id == first); var id = Guid.NewGuid(); var now = DateTimeOffset.UtcNow; var hash = new string('c', 64); var origin = Guid.NewGuid();
            var session = FieldInspectionSession.Create(Guid.NewGuid(), FieldInspectionPurpose.PreMeasurement, task, s.Project, s.Route, null, "RAW-" + Guid.NewGuid().ToString("N"), wrongActor ? s.OtherCrew : s.Crew, "raw inspector", now, null, "raw fixture", FieldInspectionSessionStatus.Completed, null);
            if (root || wrongActor) db.Add(session);
            var row = FieldInspectionSubmission.Create(id, s.Project, task, root ? id : old.RootId, root ? null : old.Id, root ? 1 : 2, old.AssignmentId, start, root || wrongActor ? session.Id : old.SessionId, origin, hash, s.Crew, now, "{}", "INCOMPLETE", "[]");
            db.AddRange(FieldInspectionOperationOrigin.Create(id, s.Project, origin, "FIELD_SUBMISSION", hash, s.Crew, null, task, id, now), row);
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); return Assert.IsType<SqlException>(error.InnerException);
        }
        Assert.Contains((await Invalid(true, false)).Number, new[] { 2601, 2627 }); Assert.Contains((await Invalid(false, false)).Number, new[] { 2601, 2627 }); Assert.Equal(51040, (await Invalid(false, true)).Number);
        async Task<SqlException> WrongLineage(bool research)
        {
            await using var raw = sql.CreateDbContext(); var old = await raw.Set<FieldInspectionSubmission>().AsNoTracking().SingleAsync(x => x.Id == first); var now = DateTimeOffset.UtcNow; var id = Guid.NewGuid(); var origin = Guid.NewGuid(); var actor = research ? s.Crew : s.OtherCrew; var hash = new string('e', 64);
            var session = FieldInspectionSession.Create(Guid.NewGuid(), research ? FieldInspectionPurpose.ResearchValidation : FieldInspectionPurpose.PreMeasurement, research ? null : task, s.Project, s.Route, null, "LINEAGE-" + Guid.NewGuid().ToString("N"), s.Crew, "valid current inspector", now, null, "valid session independent of invalid submission", FieldInspectionSessionStatus.Completed, null);
            raw.Add(session); await raw.SaveChangesAsync();
            var assignment = old.AssignmentId;
            if (!research) { var historical = FieldInspectionAssignment.Create(Guid.NewGuid(), task, s.OtherCrew, s.Pm, now.AddMinutes(-2), now.AddMinutes(-1), FieldInspectionAssignmentStatus.Ended, "historical actor relation"); raw.Add(historical); await raw.SaveChangesAsync(); assignment = historical.Id; }
            raw.AddRange(FieldInspectionOperationOrigin.Create(id, s.Project, origin, "FIELD_SUBMISSION", hash, actor, null, task, id, now), FieldInspectionSubmission.Create(id, s.Project, task, old.RootId, old.Id, 2, assignment, start, session.Id, origin, hash, actor, now, "{}", "INCOMPLETE", "[]"));
            var failure = await Assert.ThrowsAsync<DbUpdateException>(() => raw.SaveChangesAsync()); return Assert.IsType<SqlException>(failure.InnerException);
        }
        Assert.Equal(51131, (await WrongLineage(false)).Number); Assert.Equal(51131, (await WrongLineage(true)).Number);
        await using var check = sql.CreateDbContext(); Assert.Equal(1, await check.Set<FieldInspectionSubmission>().CountAsync(x => x.TaskId == task)); Assert.Equal(1, await check.Set<DeadlineClock>().CountAsync(x => x.TargetId == task));
    }
    [Fact]
    public async Task Authorized_Before_reuse_keeps_private_Reporter_scope_and_task_metadata_redacted()
    {
        var s = await Seed(); await using var db = sql.CreateDbContext(); var task = (await Create(db, s, 4)).GetProperty("id").GetGuid(); await Accept(db, s, task); var start = (await Start(db, s, task)).GetProperty("id").GetGuid();
        Assert.Equal(403, (await Cmd(db, s, s.Crew, UserRoleCode.RepairCrew, task, "reuse", new FieldEvidenceReuseInput(s.SourceFile, s.SourceEvidence, "REPORTER", new string('b', 64), "Crew cannot decide reuse"))).Status);
        Assert.Equal(403, (await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, task, "reuse", new FieldEvidenceReuseInput(s.SourceFile, Guid.NewGuid(), "REPORTER", new string('b', 64), "unrelated source cannot become reusable"))).Status);
        var reuse = await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, task, "reuse", new FieldEvidenceReuseInput(s.SourceFile, s.SourceEvidence, "REPORTER", new string('b', 64), "explicit actual source decision")); Assert.Equal(201, reuse.Status);
        var input = Input(start, null, false, s.SourceFile) with { Evidence = [new(Guid.NewGuid(), s.SourceFile, "BEFORE", new string('b', 64), "image/jpeg", DateTimeOffset.UtcNow, null)] }; await Submit(db, s, task, input);
        var metadata = await Cmd(db, s, s.Crew, UserRoleCode.RepairCrew, task, "evidence", s.SourceFile); Assert.Equal(200, metadata.Status); var publicJson = AsJson(metadata).GetRawText(); Assert.DoesNotContain("private/reporter", publicJson, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("owner", publicJson, StringComparison.OrdinalIgnoreCase);
        var scope = await db.FileScopes.AsNoTracking().SingleAsync(x => x.FileId == s.SourceFile); Assert.Null(scope.ProjectId); Assert.Equal("REPORT_PHOTO", scope.Purpose);
        var inventory = new Huy02InspectionRetentionContributor(db); Assert.Contains(s.SourceFile, await inventory.KnownProjectFilesAsync(s.Project, default)); Assert.Contains((await inventory.ReadAsync(s.SourceFile, default)).References, x => x.Kind == "FIELD_BEFORE_REUSE");
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Lost_commit_ACK_recovers_only_one_intake_under_current_authority(bool revoke)
    {
        var s = await Seed(); Guid task, start; await using (var setup = sql.CreateDbContext()) { task = (await Create(setup, s, 4)).GetProperty("id").GetGuid(); await Accept(setup, s, task); start = (await Start(setup, s, task)).GetProperty("id").GetGuid(); }
        var interceptor = new AckLoss(async () =>
        {
            await using var check = sql.CreateDbContext(); if (!await check.Set<FieldInspectionSubmission>().AnyAsync(x => x.TaskId == task)) return false;
            if (revoke) await check.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE ProjectId={s.Project} AND UserId={s.Crew}"); return true;
        });
        await using var db = sql.CreateRetryingDbContext(interceptor); var result = await Cmd(db, s, s.Crew, UserRoleCode.RepairCrew, task, "submit", Input(start, null, true, null)); Assert.True(interceptor.Fired); Assert.Equal(revoke ? 403 : 200, result.Status);
        await using var verify = sql.CreateDbContext(); Assert.Equal(1, await verify.Set<FieldInspectionSubmission>().CountAsync(x => x.TaskId == task)); Assert.Equal(1, await verify.Set<DeadlineClock>().CountAsync(x => x.TargetId == task)); Assert.Equal(1, await verify.Set<FieldInspectionTaskEvent>().CountAsync(x => x.TaskId == task && x.Kind == "SUBMITTED"));
    }
    private sealed class AckLoss(Func<Task<bool>> committed) : DbTransactionInterceptor
    {
        public bool Fired;
        public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default) { if (Fired || !await committed()) return; Fired = true; throw new TestTransientException("Injected FIELD commit ACK loss."); }
    }
    [Fact]
    public async Task FIELD_task_completion_during_presign_denies_new_parts_and_fresh_completion_under_SQL_locks()
    {
        var s = await Seed(); await using var db = sql.CreateDbContext(); var task = (await Create(db, s, 4)).GetProperty("id").GetGuid(); await Accept(db, s, task); var start = (await Start(db, s, task)).GetProperty("id").GetGuid(); var verified = await File(db, s, task);
        var submission = (await Submit(db, s, task, Input(start, null, false, verified))).GetProperty("id").GetGuid();
        var storage = new PresignCompletion(async () => { await using var review = sql.CreateDbContext(); var result = await Cmd(review, s, s.Pm, UserRoleCode.ProjectManager, task, "review", new FieldReviewInput(submission, "CONFIRM", "PM completes while presign is outside transaction")); Assert.Equal(201, result.Status); });
        var uploads = new UploadPersistenceService(db, new IdempotencyOperationService(db), storage); var now = DateTimeOffset.UtcNow;
        var created = await uploads.CreateAsync(new(s.Crew, s.Project, task, "MEASUREMENT", "additional.jpg", "image/jpeg", 4, new string('d', 64), 8388608, now.AddHours(24), Guid.NewGuid().ToString(), new string('a', 64), null));
        Assert.Equal(UploadPersistenceStatus.Success, created.Status); var upload = created.Session!;
        var parts = await uploads.GetPartUrlsAsync(s.Crew, s.Project, upload.Id, [1], Guid.NewGuid().ToString(), new string('b', 64), now, now.AddMinutes(10)); Assert.True(storage.Fired); Assert.Equal(UploadPersistenceStatus.NotFound, parts.Status); Assert.Empty(parts.Parts);
        var current = await uploads.GetSessionAsync(upload.Id); var complete = await uploads.CompleteAsync(new(s.Crew, s.Project, upload.Id, current!.Version, [new(1, "part")], new string('d', 64), Guid.NewGuid().ToString(), new string('c', 64), null)); Assert.Equal(UploadPersistenceStatus.NotFound, complete.Status);
        Assert.Equal("UPLOADING", (await uploads.GetSessionAsync(upload.Id))!.Status); Assert.Equal(0, await db.AuditLogs.CountAsync(x => x.EventType == "upload_verification_requested" && x.EntityId == upload.Id));
    }
    private sealed class PresignCompletion(Func<Task> complete) : IUploadObjectStorage
    {
        public bool Fired;
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default) => Task.FromResult("field-multipart");
        public async Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers, DateTimeOffset expiresAt, CancellationToken cancellationToken = default) { Fired = true; await complete(); return partNumbers.Select(x => new PresignedUploadPart(x, "https://storage.invalid/never-return", expiresAt)).ToArray(); }
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No completion may reach storage.");
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No download in this case.");
    }
    [Fact]
    public async Task Precommit_receipt_fault_rolls_back_all_FIELD_intake_business_and_foundation_effects()
    {
        var s = await Seed(); Guid task, start; await using (var setup = sql.CreateDbContext()) { task = (await Create(setup, s, 4)).GetProperty("id").GetGuid(); await Accept(setup, s, task); start = (await Start(setup, s, task)).GetProperty("id").GetGuid(); }
        var input = Input(start, null, true, null); var key = Guid.NewGuid().ToString(); await using (var failed = sql.CreateDbContext(new FailReceipt()))
            await Assert.ThrowsAsync<InvalidOperationException>(() => Cmd(failed, s, s.Crew, UserRoleCode.RepairCrew, task, "submit", input, key));
        await using var check = sql.CreateDbContext(); Assert.Equal(FieldInspectionTaskStatus.InProgress, (await check.FieldInspectionTasks.SingleAsync(x => x.Id == task)).Status);
        Assert.Equal(0, await check.Set<FieldInspectionSubmission>().CountAsync(x => x.TaskId == task)); Assert.Equal(0, await check.FieldInspectionSessions.CountAsync(x => x.FieldInspectionTaskId == task));
        Assert.Equal(0, await check.GroundTruthMeasurements.CountAsync(x => x.DefectId == s.Defect)); Assert.Equal(0, await check.Set<FieldInspectionOperationOrigin>().CountAsync(x => x.ProjectId == s.Project && x.OriginId == input.OriginId));
        Assert.Equal(0, await check.Set<DeadlineClock>().CountAsync(x => x.TargetId == task)); Assert.Equal(0, await check.Set<FieldInspectionTaskEvent>().CountAsync(x => x.TaskId == task && x.Kind == "SUBMITTED"));
        Assert.Equal(0, await check.AuditLogs.CountAsync(x => x.EntityId == task && x.EventType == "field_submitted")); Assert.Equal(0, await check.OutboxMessages.CountAsync(x => x.MessageType == "field.task.submitted.v1" && x.PayloadJson.Contains(task.ToString())));
        Assert.Equal(0, await check.IdempotencyRecords.CountAsync(x => x.ProjectId == s.Project && x.IdempotencyKey == key));
    }
    private sealed class ReceiptCandidateProducer(ResolvedCandidateSourceFacts source) : IAnhHuyProducerService
    {
        public Task<AnhHuyProducerResult<ResolvedCandidateSourceFacts>> ResolveCandidateSourceAsync(Guid actorId, UserRoleCode role, Guid projectId, CandidateSourceKind kind, Guid sourceId, string? expectedSourceVersion = null, string? expectedGeometryVersion = null, string? expectedDispositionVersion = null, CancellationToken cancellationToken = default) => Task.FromResult(new AnhHuyProducerResult<ResolvedCandidateSourceFacts>(AnhHuyProducerStatus.Ready, source));
        public Task<AnhHuyProducerResult<ResolvedEvidenceFacts>> ResolvePrivateEvidenceAsync(Guid actorId, UserRoleCode role, Guid fileId, Guid evidenceId, string? expectedFileVersion = null, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task<AnhHuyProducerResult<ResolvedEvidenceFacts>> ResolvePublicationEvidenceAsync(Guid actorId, UserRoleCode role, Guid publicationId, Guid reportId, Guid evidenceId, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task<AnhHuyProducerResult<ProjectGeometryContext>> ResolveGeometryAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid routeVersionId, Guid segmentSetId, string? expectedVersion = null, bool requireCurrent = true, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
    }
    private sealed class FailReceipt : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<IdempotencyRecord>().Any(x => x.State == EntityState.Added && x.Entity.Operation == "h3.field.submit.v1")) throw new InvalidOperationException("Injected FIELD receipt save fault before commit."); return ValueTask.FromResult(result);
        }
    }
    [Fact]
    public async Task Caller_owned_FIELD_transaction_is_reused_and_rollback_removes_actual_intake()
    {
        var s = await Seed(); await using var db = sql.CreateDbContext(); var task = (await Create(db, s, 4)).GetProperty("id").GetGuid(); await Accept(db, s, task); var start = (await Start(db, s, task)).GetProperty("id").GetGuid(); var version = await Version(db, task); db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable); var repo = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var result = await repo.ApplyInTransactionAsync(new(s.Project, task, "submit", (RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldSubmissionInputFact)Input(start, null, true, null), null, version, new(s.Crew, UserRoleCode.RepairCrew, s.Crew, "DIRECT", true)), async ct => await Guard(db).AuthorizeAsync(s.Crew, UserRoleCode.RepairCrew, s.Project, ct) is not null, default);
        Assert.Equal(201, result.Status); Assert.Same(tx, db.Database.CurrentTransaction); Assert.Equal(1, await db.Set<FieldInspectionSubmission>().CountAsync(x => x.TaskId == task)); await tx.RollbackAsync();
        await using var verify = sql.CreateDbContext(); Assert.Equal(0, await verify.Set<FieldInspectionSubmission>().CountAsync(x => x.TaskId == task)); Assert.Equal(0, await verify.Set<DeadlineClock>().CountAsync(x => x.TargetId == task));
    }
    [Fact]
    public async Task Actual_SURVEY_lineage_requires_PM_KeepNew_and_partitions_legacy_and_modern_session_purposes()
    {
        var s = await Seed(); await using var db = sql.CreateDbContext(); var now = DateTimeOffset.UtcNow; var survey = Survey.Create(Guid.NewGuid(), null, s.Project, s.Route, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var data = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.Draft, SurveyDataIntegrityStatus.Pending, null, null, "[]"); var block = ProcessingBlock.Create(Guid.NewGuid(), data.Id, 1, "{}");
        var model = AIModelVersion.Create(Guid.NewGuid(), "mock", "FIELD-" + Guid.NewGuid().ToString("N"), "file:///models/fixture", null, null, AIModelVersionStatus.Draft, null, null);
        var job = ProcessingJob.Create(Guid.NewGuid(), block.Id, model.Id, ProcessingJobStatus.Completed, now.AddMinutes(-1), now, null, null); var type = DefectType.Create("S" + Guid.NewGuid().ToString("N"), "Survey field"); db.AddRange(survey, data, block, model, job, type); await db.SaveChangesAsync();
        var point = new GeometryFactory(new PrecisionModel(), 32648).CreatePoint(new Coordinate(5, 0)); var detection = AIDetection.Create(Guid.NewGuid(), job.Id, model.Id, s.Route, point, type.Code, .8m, null, null, "{}");
        var unconfirmed = Defect.Create(Guid.NewGuid(), s.Project, s.Route, detection.Id, type.Code, null, DefectSeverity.Low, DefectStatus.Open, point, now); db.AddRange(detection, unconfirmed); await db.SaveChangesAsync();
        async Task<FieldWorkflowResult> CreateSurvey(Guid defect)
        {
            var version = Convert.ToBase64String(await db.Defects.Where(x => x.Id == defect).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
            return await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, null, "create", new FieldTaskCreateInput(defect, version, survey.Id, "SURVEY", s.Route, s.Set, null, null, "PRE_MEASUREMENT", 1, "{}", "physical Survey lineage is not PM confirmation", s.Crew, now.AddDays(1)));
        }
        Assert.Equal(409, (await CreateSurvey(unconfirmed.Id)).Status);
        var confirmedDetection = AIDetection.Create(Guid.NewGuid(), job.Id, model.Id, s.Route, point, type.Code, .8m, null, null, "{}"); db.Add(confirmedDetection); await db.SaveChangesAsync();
        var source = CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.AiDetection, confirmedDetection.Id, "physical-source-fixture"), s.Project, "geometry-fixture");
        var accepted = await new CandidateDecisionRepository(db).SaveAcceptedAsync(s.Pm, source, CandidateDecisionKind.KeepNew, CandidateClassification.Create(s.Route, type.Code, null, DefectSeverity.Low, null), null, null, null, "actual persisted PM keep-new", null, default);
        var created = await CreateSurvey(accepted.DefectId!.Value); Assert.Equal(201, created.Status); var task = AsJson(created).GetProperty("id").GetGuid(); await Accept(db, s, task);
        async Task DeniedPurpose(FieldInspectionPurpose purpose)
        {
            await using var raw = sql.CreateDbContext(); raw.Add(FieldInspectionSession.Create(Guid.NewGuid(), purpose, task, s.Project, s.Route, survey.Id, "BAD-" + Guid.NewGuid().ToString("N"), s.Crew, "actual Crew", now, null, "purpose partition", FieldInspectionSessionStatus.Completed, null));
            var failure = await Assert.ThrowsAsync<DbUpdateException>(() => raw.SaveChangesAsync()); Assert.Equal(51040, Assert.IsType<SqlException>(failure.InnerException).Number);
        }
        await DeniedPurpose(FieldInspectionPurpose.DefectVerification); await DeniedPurpose(FieldInspectionPurpose.Verification);
        var legacy = FieldInspectionTask.Create(Guid.NewGuid(), "LEGACY-" + Guid.NewGuid().ToString("N"), s.Project, accepted.DefectId.Value, survey.Id, s.Route, 1, "{}", null, null, now.AddDays(1), FieldInspectionTaskStatus.Accepted, s.Pm, null, null, null, null);
        db.AddRange(legacy, FieldInspectionAssignment.Create(Guid.NewGuid(), legacy.Id, s.Crew, s.Pm, now, null, FieldInspectionAssignmentStatus.Active, null)); await db.SaveChangesAsync();
        var legacyFile = StoredFile.Create(Guid.NewGuid(), "legacy/field-retained", "before.jpg", "image/jpeg", 4, new string('f', 64), s.Crew, now, null); db.AddRange(legacyFile, FileScope.Create(Guid.NewGuid(), legacyFile.Id, s.Project, null, s.Crew, "BEFORE", now)); await db.SaveChangesAsync();
        db.AddRange(FieldInspectionSession.Create(Guid.NewGuid(), FieldInspectionPurpose.DefectVerification, legacy.Id, s.Project, s.Route, survey.Id, "LEGACY-S-" + Guid.NewGuid().ToString("N"), s.Crew, "legacy Crew", now, null, "legacy compatibility", FieldInspectionSessionStatus.Completed, legacyFile.Id),
            FieldInspectionSession.Create(Guid.NewGuid(), FieldInspectionPurpose.ResearchValidation, null, s.Project, s.Route, null, "RESEARCH-" + Guid.NewGuid().ToString("N"), s.Pm, "research", now, null, "research distinct", FieldInspectionSessionStatus.Completed, null)); await db.SaveChangesAsync();
        var reader = new UploadPersistenceService(db, new IdempotencyOperationService(db), new PresignCompletion(() => Task.CompletedTask));
        Assert.True(await reader.IsCurrentLegacyFieldFileReaderAsync(s.Pm, UserRoleCode.ProjectManager, s.Project, legacyFile.Id, "BEFORE"));
        Assert.False(await reader.IsCurrentLegacyFieldFileReaderAsync(s.OtherCrew, UserRoleCode.RepairCrew, s.Project, legacyFile.Id, "BEFORE"));
        Assert.False(await reader.IsCurrentLegacyFieldFileReaderAsync(s.Pm, UserRoleCode.ProjectManager, s.Project, s.SourceFile, "BEFORE"));
        Assert.False(await reader.IsCurrentFieldActorAsync(s.Crew, UserRoleCode.RepairCrew, s.Project, legacy.Id, "BEFORE", true));
        var unrelated = StoredFile.Create(Guid.NewGuid(), "legacy/unrelated", "unrelated.jpg", "image/jpeg", 4, new string('f', 64), s.Crew, now, null); db.AddRange(unrelated, FileScope.Create(Guid.NewGuid(), unrelated.Id, s.Project, null, s.Crew, "BEFORE", now)); await db.SaveChangesAsync();
        Assert.False(await reader.IsCurrentLegacyFieldFileReaderAsync(s.Pm, UserRoleCode.ProjectManager, s.Project, unrelated.Id, "BEFORE"));
    }
    private static FieldSubmissionInput Input(Guid origin, Guid? parent, bool unknown, Guid? file) => new(Guid.NewGuid(), origin, parent,
        [new("area", "Area", unknown ? null : 0, unknown ? "UNKNOWN" : "KNOWN", unknown ? "surface obscured" : null, "AREA", "m²", null, null, "GPS unavailable", "manual area gauge", "area measurement")],
        [new(Guid.NewGuid(), file, "MEASUREMENT", new string('a', 64), "image/jpeg", DateTimeOffset.UtcNow, null)],
        unknown ? new("UNKNOWN", null, null, null) : new("POSITION_CHECKLIST", "ROUTE_CHAINAGE_MARKINGS_CONFIRMED", null, null, ObservedChainageMeters: 5), "MEASUREMENT", null, null);
    private async Task<JsonElement> Create(RoadGuardDbContext db, Scope s, byte type)
    {
        var defect = await db.Defects.Where(x => x.Id == s.Defect).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync();
        var result = await Cmd(db, s, s.Pm, UserRoleCode.ProjectManager, null, "create", new FieldTaskCreateInput(s.Defect, Convert.ToBase64String(defect), null, "REPORTER", s.Route, s.Set, null, null, "PRE_MEASUREMENT", type, "{}", "measure only", s.Crew, DateTimeOffset.UtcNow.AddDays(1)));
        Assert.True(result.Status == 201, $"create {result.Status}/{result.Code}"); return AsJson(result);
    }
    private async Task Accept(RoadGuardDbContext db, Scope s, Guid task) => Assert.Equal(201, (await Cmd(db, s, s.Crew, UserRoleCode.RepairCrew, task, "accept", new FieldTaskActionInput("accept"))).Status);
    private async Task<JsonElement> Start(RoadGuardDbContext db, Scope s, Guid task) { var result = await Cmd(db, s, s.Crew, UserRoleCode.RepairCrew, task, "start", new FieldStartInput(Guid.NewGuid(), DateTimeOffset.UtcNow)); Assert.Equal(201, result.Status); return AsJson(result); }
    private async Task<JsonElement> Submit(RoadGuardDbContext db, Scope s, Guid task, FieldSubmissionInput input)
    {
        if (input.LocationProof?.Kind == "POSITION_CHECKLIST") input = input with { LocationProof = input.LocationProof with { ObservedRouteVersionId = s.Route } };
        var result = await Cmd(db, s, s.Crew, UserRoleCode.RepairCrew, task, "submit", input); Assert.True(result.Status == 201, $"submit {result.Status}/{result.Code}"); return AsJson(result);
    }
    private async Task<FieldWorkflowResult> Cmd(RoadGuardDbContext db, Scope s, Guid actor, UserRoleCode role, Guid? task, string action, object? input, string? key = null, string? version = null)
    {
        db.ChangeTracker.Clear(); var read = action is "get" or "geometry" or "history";
        if (task is Guid id && !read) version ??= await Version(db, id);
        return await new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System).ExecuteAsync(new(s.Project, task, action, RoadGuardSystem.DTOs.BoundaryFactMappings.ToFacts(input), key ?? Guid.NewGuid().ToString(), version, new(actor, role, actor, "DIRECT", input is not FieldStartInput { OfflineProof: not null })),
            async ct => await Guard(db).AuthorizeAsync(actor, role, s.Project, ct) is not null, default);
    }
    private static Task<string> Version(RoadGuardDbContext db, Guid task) => db.FieldInspectionTasks.AsNoTracking().Where(x => x.Id == task).Select(x => Convert.ToBase64String(x.RowVersion)).SingleAsync();
    private static JsonElement AsJson(FieldWorkflowResult result) => result.Value is JsonElement json ? json : JsonSerializer.SerializeToElement(result.Value, Json);
    private static ProjectScopeGuard Guard(RoadGuardDbContext db) => new(new ProjectMembershipReadModel(db), TimeProvider.System);
    private static AnhHuyProducerService Producer(RoadGuardDbContext db) => new(new AnhHuyFactsRepository(db), new GeometryWorkflowPersistenceService(db, new IdempotencyOperationService(db)), Guard(db), new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System));
    private async Task<Guid> File(RoadGuardDbContext db, Scope s, Guid task)
    {
        var now = DateTimeOffset.UtcNow; var file = StoredFile.Create(Guid.NewGuid(), "field/private-fixture-" + Guid.NewGuid().ToString("N"), "capture.jpg", "image/jpeg", 4, new string('a', 64), s.Crew, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, s.Crew, file.StorageUri, "MEASUREMENT", "image/jpeg", 4, new string('a', 64), 8388608, now.AddHours(24)); upload.StartUploading("fixture", now);
        db.AddRange(file, FileScope.Create(Guid.NewGuid(), file.Id, s.Project, task, s.Crew, "MEASUREMENT", now), upload); await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync(); return file.Id;
    }
    private async Task<Scope> Seed()
    {
        await using var db = sql.CreateDbContext(); await sql.SeedRolesAsync(db); var now = DateTimeOffset.UtcNow;
        ApplicationUser User(UserRoleCode role) => new() { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString(), DisplayName = "FIELD fixture", PasswordHash = "fixture", RoleCode = role, Status = UserStatus.Active, CreatedAt = now };
        var pm = User(UserRoleCode.ProjectManager); var crew = User(UserRoleCode.RepairCrew); var other = User(UserRoleCode.RepairCrew); var reporter = User(UserRoleCode.Reporter);
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "FIELD actual source", null, null, null, null, now); var road = RoadSection.Create(Guid.NewGuid(), project.Id, "fixture");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new(0, 0), new(20, 0)]); var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry, now, "legacy route");
        var set = RoadSegmentSet.Create(Guid.NewGuid(), route.Id); var segment = RoadSegment.Create(Guid.NewGuid(), set.Id, route.Id, 1); segment.SetGeometry(0, 20, 0, geometry);
        var type = DefectType.Create("F" + Guid.NewGuid().ToString("N"), "FIELD defect");
        db.AddRange(pm, crew, other, reporter, project, road, route, set, segment, type, ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm.Id, new(2000, 1, 1)),
            new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, UserId = crew.Id, RoleCode = UserRoleCode.RepairCrew, ValidFrom = new(2000, 1, 1), Status = ProjectMemberStatus.Active },
            new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, UserId = other.Id, RoleCode = UserRoleCode.RepairCrew, ValidFrom = new(2000, 1, 1), Status = ProjectMemberStatus.Active }); await db.SaveChangesAsync();
        var file = StoredFile.Create(Guid.NewGuid(), "private/reporter-fixture-" + Guid.NewGuid().ToString("N"), "source.jpg", "image/jpeg", 4, new string('b', 64), reporter.Id, now, null); db.AddRange(file, FileScope.CreatePrivate(Guid.NewGuid(), file.Id, reporter.Id, now)); await db.SaveChangesAsync();
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, reporter.Id, file.StorageUri, "REPORT_PHOTO", "image/jpeg", 4, new string('b', 64), 8388608, now.AddHours(24)); upload.StartUploading("fixture", now); db.Add(upload); await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        var evidenceId = Guid.NewGuid(); var report = Report.Create(Guid.NewGuid(), reporter.Id, "genuine Reporter without Survey", now, [VerifiedEvidenceReference.Create(evidenceId, file.Id, Convert.ToBase64String(upload.RowVersion), reporter.Id)]);
        var incident = IncidentCase.CreateUnassigned(Guid.NewGuid(), report.Id, now); incident.Triage(project.Id, CaseVerificationMethod.ExistingEvidence, "actual retained source", now); db.AddRange(report, incident); db.Set<HuyCaseReportLink>().Add(new() { Id = Guid.NewGuid(), CaseId = incident.Id, ReportId = report.Id, StartedAt = now }); await db.SaveChangesAsync();
        var source = CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.Report, report.Id, "fixture-source"), project.Id, "fixture-geometry"); var classification = CandidateClassification.Create(route.Id, type.Code, null, DefectSeverity.Low, null);
        var accepted = await new CandidateDecisionRepository(db).SaveAcceptedAsync(pm.Id, source, CandidateDecisionKind.KeepNew, classification, null, null, null, "genuine keep-new source", null, default);
        var defect = await db.Defects.SingleAsync(x => x.Id == accepted.DefectId);
        return new(pm.Id, crew.Id, other.Id, project.Id, route.Id, set.Id, defect.Id, file.Id, evidenceId);
    }
    private sealed record Scope(Guid Pm, Guid Crew, Guid OtherCrew, Guid Project, Guid Route, Guid Set, Guid Defect, Guid SourceFile, Guid SourceEvidence);
}
