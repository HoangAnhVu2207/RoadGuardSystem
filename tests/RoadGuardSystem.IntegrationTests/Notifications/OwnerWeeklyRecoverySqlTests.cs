using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.IntegrationTests.Repairs;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class OwnerWeeklyRecoverySqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly DateTimeOffset Monday = new(2026, 10, 12, 2, 0, 0, TimeSpan.Zero);
    [Fact]
    public async Task Completed_source_records_only_periods_when_review_was_pending_and_emits_no_digest()
    {
        var seed = await new H6DeadlineProducerSqlTests(sql).Intake();
        await using var db = sql.CreateDbContext();
        var dispatcher = new H6NotificationDispatchRepository(db, new FixedClock(Monday.AddMinutes(-1)));
        await dispatcher.ObserveCalendarAsync(Guid.NewGuid(), null, default);
        var duty = await db.Set<DeadlineClock>().SingleAsync(c => c.Id == seed.Clock);
        // Controlled persisted completion boundary, not a new business completion command.
        duty.Complete(Monday.AddDays(1)); await db.SaveChangesAsync();
        dispatcher = new H6NotificationDispatchRepository(db, new FixedClock(Monday.AddDays(21).AddMinutes(2)));
        await dispatcher.ObserveCalendarAsync(Guid.NewGuid(), null, default);
        Assert.False(await db.Set<WeeklyReviewDigest>().AnyAsync(d => d.ProjectId == duty.ProjectId));
        Assert.Equal(Monday, await db.Set<WeeklyReviewRecoveryPeriod>().Where(p => p.ProjectId == duty.ProjectId)
            .Select(p => p.ScheduledAtUtc).SingleAsync());
        await dispatcher.ObserveCalendarAsync(Guid.NewGuid(), null, default);
        Assert.Equal(1, await db.Set<WeeklyReviewRecoveryPeriod>().CountAsync(p => p.ProjectId == duty.ProjectId));
    }
    [Fact]
    public async Task Missing_current_reviewer_remains_unresolved_then_same_occurrence_resolves_current_authority()
    {
        var seed = await new H6DeadlineProducerSqlTests(sql).Intake();
        await using var db = sql.CreateDbContext();
        var duty = await db.Set<DeadlineClock>().SingleAsync(c => c.Id == seed.Clock);
        await db.ProjectMembers.Where(m => m.ProjectId == duty.ProjectId && m.UserId == seed.Manager)
            .ExecuteUpdateAsync(m => m.SetProperty(x => x.Status, ProjectMemberStatus.Ended));
        var time = new FixedClock(Monday.AddMinutes(2));
        var dispatcher = new H6NotificationDispatchRepository(db, time);
        await dispatcher.ObserveCalendarAsync(Guid.NewGuid(), null, default);
        var digest = await db.Set<WeeklyReviewDigest>().SingleAsync(d => d.ProjectId == duty.ProjectId);
        Assert.Null(digest.RecipientId);
        var message = await db.OutboxMessages.SingleAsync(m => m.Id == digest.Id);
        var fence = Guid.NewGuid(); message.AcquireLease("h6:" + fence.ToString("N"), time.Now, TimeSpan.FromMinutes(2), 32);
        await db.SaveChangesAsync();
        var claim = new H6Claim(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson, fence,
            message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
        var outcome = await dispatcher.DispatchAsync(claim, H6NotificationCatalog.Parse(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson), default);
        Assert.Equal("COMMITTED", outcome.Status); Assert.Equal(1, outcome.Unresolved); Assert.Equal(0, outcome.Delivered);
        await db.ProjectMembers.Where(m => m.ProjectId == duty.ProjectId && m.UserId == seed.Manager)
            .ExecuteUpdateAsync(m => m.SetProperty(x => x.Status, ProjectMemberStatus.Active));
        time.Now = time.Now.AddMinutes(6);
        await dispatcher.RetryUnresolvedAsync(default);
        var delivered = await db.Notifications.SingleAsync(n => n.SourceEntityId == digest.Id);
        Assert.Equal(seed.Manager, delivered.RecipientUserId);
        Assert.Equal(1, await db.Set<H6NotificationDeliveryRow>().CountAsync(d => d.OccurrenceId == outcome.OccurrenceId));
        var reader = new H6ProtectedNotificationRepository(db, new IdempotencyOperationService(db), time, [new H6WeeklyReviewSourceAdapter(db, time)]);
        Assert.NotNull(await reader.WeeklyDigestAsync(seed.Manager, UserRoleCode.ProjectManager, duty.ProjectId, digest.Id, default));
        Assert.NotNull(await reader.GetAsync(seed.Manager, delivered.Id));
        Assert.Equal(0, await dispatcher.ObserveCalendarAsync(Guid.NewGuid(), null, default));
    }
    [Fact]
    public async Task Populated_upgrade_adopts_pending_policy_history_without_rewriting_sources()
    {
        var owned = new IdentitySqlServerFixture(); await owned.InitializeAsync();
        try
        {
            var seed = await new H6DeadlineProducerSqlTests(owned).Intake();
            await using var db = owned.CreateDbContext();
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261007053131_OwnerClockDutyAppointments");
            var pending = new H6NotificationCalendarRow { Id = Guid.NewGuid(), ClockId = seed.Clock,
                PlannedAtUtc = Monday.AddDays(-1), ScheduledAtUtc = Monday, SchedulerRunId = Guid.NewGuid(),
                ObservedAtUtc = Monday.AddMinutes(2), Status = "PENDING_POLICY" };
            db.Add(pending); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var original = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(c => c.Id == seed.Clock);
            await migrator.MigrateAsync(); Assert.False(db.Database.HasPendingModelChanges());
            var now = new FixedClock(Monday.AddDays(14).AddMinutes(2));
            Assert.Equal(1, await new H6NotificationDispatchRepository(db, now).ObserveCalendarAsync(Guid.NewGuid(), null, default));
            Assert.Equal(0, await new H6NotificationDispatchRepository(db, now).ObserveCalendarAsync(Guid.NewGuid(), null, default));
            var stored = await db.Set<H6NotificationCalendarRow>().AsNoTracking().SingleAsync(p => p.Id == pending.Id);
            Assert.Equal(pending.Status, stored.Status); Assert.Equal(pending.ObservedAtUtc, stored.ObservedAtUtc);
            Assert.Equal(pending.PlannedAtUtc, stored.PlannedAtUtc); Assert.Null(stored.OutboxMessageId);
            var after = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(c => c.Id == seed.Clock);
            Assert.Equal(original.OriginAt, after.OriginAt); Assert.Equal(original.OriginalDueAt, after.OriginalDueAt);
            var digest = await db.Set<WeeklyReviewDigest>().Include(d => d.Duties).SingleAsync(d => d.ProjectId == after.ProjectId);
            Assert.Equal(Monday.AddDays(14), digest.ScheduledAtUtc); Assert.Single(digest.Duties);
            Assert.Equal(3, await db.Set<WeeklyReviewRecoveryPeriod>().CountAsync(p => p.ProjectId == after.ProjectId));
            await Assert.ThrowsAnyAsync<Exception>(() => migrator.MigrateAsync("20261007053131_OwnerClockDutyAppointments"));
        }
        finally { await owned.DisposeAsync(); }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Actual_pending_proposals_aggregate_latest_recovery_with_concurrency_or_atomic_rollback(bool rollback)
    {
        await using var db = sql.CreateDbContext();
        var source = await H4GenuineRepairSource.Seed(db, sql);
        var repo = new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var defectVersion = Convert.ToBase64String(await db.Defects.Where(d => d.Id == source.Defect)
            .Select(d => EF.Property<byte[]>(d, "RowVersion")).SingleAsync());
        var created = await repo.CreatePackageAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            new(source.Defect, defectVersion, Enumerable.Range(0, 3).Select(i => new RepairObligationData(
                "FORMAL_REPAIR", true, new(source.Road, source.Route, source.Set, null, null, i + 1, i + 2, 0, 1), "TEST_ONLY weekly source")).ToArray(), "TEST_ONLY proposals"),
            Guid.NewGuid().ToString(), defectVersion), default);
        Assert.Equal(201, created.Status); var package = Assert.IsType<RepairPackageFact>(created.Value);
        var obligations = (await db.Set<RepairPackage>().Include(p => p.Obligations).SingleAsync(p => p.Id == package.Id)).Obligations;
        var clocks = new List<Guid>(); Guid lastItem = default; string lastVersion = "";
        foreach (var obligation in obligations)
        {
            var packageVersion = Convert.ToBase64String(await db.Set<RepairPackage>().Where(p => p.Id == package.Id)
                .Select(p => EF.Property<byte[]>(p, "RowVersion")).SingleAsync());
            var proposal = await repo.ProposeItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, package.Id,
                new(obligation.Id, "NORMAL", "actual plan", "checklist-v1", "TEST_ONLY weekly source"), Guid.NewGuid().ToString(), packageVersion), default);
            Assert.Equal(201, proposal.Status); lastItem = Assert.IsType<RepairItemFact>(proposal.Value).Id; lastVersion = proposal.Version!;
            clocks.Add(await db.Set<DeadlineClock>().Where(c => c.TargetId == lastItem && c.Kind == DeadlineClockKind.SupervisorInitialApproval).Select(c => c.Id).SingleAsync());
        }
        var time = new FixedClock(Monday.AddMinutes(-1));
        await new H6NotificationDispatchRepository(db, time).ObserveCalendarAsync(Guid.NewGuid(), null, default);
        var old = await db.Set<H6NotificationCalendarRow>().SingleAsync(c => c.ClockId == clocks[0]);
        old.Status = "PENDING_POLICY"; old.ObservedAtUtc = Monday.AddMinutes(2); await db.SaveChangesAsync();
        var oldId = old.Id;
        Assert.Equal(201, (await repo.ApproveItemAsync(new(source.Supervisor, UserRoleCode.Supervisor, source.Project,
            package.Id, lastItem, new("actual completed approval omitted at recovery"), Guid.NewGuid().ToString(), lastVersion), default)).Status);
        var recoveryTime = new FixedClock(Monday.AddDays(21).AddMinutes(2));
        if (rollback)
        {
            await using var failing = sql.CreateDbContext(new FailRecoverySave());
            await Assert.ThrowsAsync<InvalidOperationException>(() => new H6NotificationDispatchRepository(failing, recoveryTime)
                .ObserveCalendarAsync(Guid.NewGuid(), null, default));
            Assert.False(await db.Set<WeeklyReviewRecoveryPeriod>().AnyAsync(p => p.ProjectId == source.Project));
            Assert.False(await db.Set<WeeklyReviewDigest>().AnyAsync(p => p.ProjectId == source.Project));
            Assert.False(await db.OutboxMessages.AnyAsync(m => m.MessageType == "review.weekly_pending.v1" &&
                EF.Functions.Like(m.PayloadJson, "%" + source.Project.ToString() + "%")));
            await new H6NotificationDispatchRepository(db, recoveryTime).ObserveCalendarAsync(Guid.NewGuid(), null, default);
        }
        else
        {
            var before = await db.Set<WeeklyReviewDigest>().CountAsync();
            await using var left = sql.CreateDbContext(); await using var right = sql.CreateDbContext();
            var counts = await Task.WhenAll(new H6NotificationDispatchRepository(left, recoveryTime).ObserveCalendarAsync(Guid.NewGuid(), null, default),
                new H6NotificationDispatchRepository(right, recoveryTime).ObserveCalendarAsync(Guid.NewGuid(), null, default));
            Assert.Equal(await db.Set<WeeklyReviewDigest>().CountAsync() - before, counts.Sum());
        }
        db.ChangeTracker.Clear();
        var digest = await db.Set<WeeklyReviewDigest>().Include(d => d.Duties).SingleAsync(d => d.ProjectId == source.Project);
        Assert.Equal(source.Supervisor, digest.RecipientId); Assert.Equal(Monday.AddDays(21), digest.ScheduledAtUtc);
        Assert.Equal(2, digest.Duties.Count); Assert.DoesNotContain(digest.Duties, d => d.ClockId == clocks[^1]);
        Assert.Equal(4, await db.Set<WeeklyReviewRecoveryPeriod>().CountAsync(p => p.ProjectId == source.Project));
        Assert.Equal("PENDING_POLICY", (await db.Set<H6NotificationCalendarRow>().SingleAsync(p => p.Id == oldId)).Status);
        Assert.Equal(Monday.AddMinutes(2), (await db.Set<H6NotificationCalendarRow>().SingleAsync(p => p.Id == oldId)).ObservedAtUtc);
        Assert.Equal(0, await new H6NotificationDispatchRepository(db, recoveryTime).ObserveCalendarAsync(Guid.NewGuid(), null, default));
        Assert.False(db.Database.HasPendingModelChanges());
        await db.ProjectMembers.Where(m => m.ProjectId == source.Project && m.UserId == source.Supervisor)
            .ExecuteUpdateAsync(m => m.SetProperty(x => x.Status, ProjectMemberStatus.Ended));
        var message = await db.OutboxMessages.SingleAsync(m => m.Id == digest.Id);
        var fence = Guid.NewGuid(); message.AcquireLease("h6:" + fence.ToString("N"), recoveryTime.Now, TimeSpan.FromMinutes(2), 32);
        await db.SaveChangesAsync();
        var claim = new H6Claim(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson, fence,
            message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
        var dispatch = new H6NotificationDispatchRepository(db, recoveryTime);
        var result = await dispatch.DispatchAsync(claim, H6NotificationCatalog.Parse(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson), default);
        Assert.Equal("COMMITTED", result.Status); Assert.Equal(0, result.Delivered); Assert.Equal(1, result.Unresolved);
        Assert.Equal("COMMITTED", (await dispatch.DispatchAsync(claim, H6NotificationCatalog.Parse(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson), default)).Status);
        await db.ProjectMembers.Where(m => m.ProjectId == source.Project && m.UserId == source.Supervisor)
            .ExecuteUpdateAsync(m => m.SetProperty(x => x.Status, ProjectMemberStatus.Active));
        recoveryTime.Now = recoveryTime.Now.AddMinutes(6);
        Assert.Equal(1, await dispatch.RetryUnresolvedAsync(default)); Assert.Equal(0, await dispatch.RetryUnresolvedAsync(default));
        Assert.Single(await db.Notifications.Where(n => n.SourceEntityId == digest.Id).ToArrayAsync());
        var read = new H6ProtectedNotificationRepository(db, new IdempotencyOperationService(db), recoveryTime, [new H6WeeklyReviewSourceAdapter(db, recoveryTime)]);
        var persisted = await read.WeeklyDigestAsync(source.Supervisor, UserRoleCode.Supervisor, source.Project, digest.Id, default);
        Assert.NotNull(persisted); Assert.Equal(2, persisted.PendingAtRecovery.Length); Assert.Equal(4, persisted.RecoveryPeriods.Length);
        Assert.Null(await read.WeeklyDigestAsync(source.Pm, UserRoleCode.ProjectManager, source.Project, digest.Id, default));
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [WeeklyReviewDigestDuties] SET [TargetId]={Guid.NewGuid()} WHERE [DigestId]={digest.Id}"));
    }
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    { public DateTimeOffset Now { get; set; } = now; public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class FailRecoverySave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<WeeklyReviewDigest>().Any(e => e.State == EntityState.Added))
                throw new InvalidOperationException("Injected weekly aggregate save failure.");
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
