using RoadGuardSystem.BusinessObjects.Repairs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.IntegrationTests.Repairs;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using RoadGuardSystem.TestFixtures;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class H6AuthorityConcurrencySqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task OriginalWindowEligibilityAndRepairDispatchCompleteWithOppositeResourceOrder()
    {
        await using var seed = sql.CreateDbContext();
        var source = await H4GenuineRepairSource.Seed(seed, sql);
        var state = await ND01OriginalWindowFixture.Prepare(seed, new(source.Pm, source.Crew, source.Supervisor,
            source.Project, source.Road, source.Route, source.Set, source.Defect));
        using var keys = state.Keys;
        var message = await seed.OutboxMessages.SingleAsync(m => m.MessageType == "repair.work.assigned.v1" && m.PayloadJson.Contains(state.Item.ToString()));
        var claim = await Lease(seed, message, TimeProvider.System);
        var workerGate = new PauseOnceCommandInterceptor(IsAuthorityRead);
        var commandGate = new PauseOnceCommandInterceptor(text => text.Contains("FROM [ObligationResponsibilities] WITH", StringComparison.Ordinal));
        await using var worker = sql.CreateDbContext(workerGate);
        await using var command = sql.CreateDbContext(commandGate);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        // The real adapter has locked responsibility before reaching recipient authority.
        var dispatch = Dispatch(worker, claim, TimeProvider.System, timeout.Token);
        try
        {
            await workerGate.Reached.WaitAsync(TimeSpan.FromSeconds(15));
            var eligibility = new RepairEligibilityRepository(command, TimeProvider.System).ReadAsync(
                new(source.Crew, UserRoleCode.RepairCrew, source.Project, state.Package, state.Item), timeout.Token);
            try
            {
                // Eligibility owns Users/Roles before trying the worker's responsibility range.
                await commandGate.Reached.WaitAsync(TimeSpan.FromSeconds(15));
                workerGate.Release(); commandGate.Release();
                await Task.WhenAll(dispatch, eligibility);
                Assert.Equal(200, (await eligibility).Status);
                Assert.Empty((await eligibility).Value!.MissingReasons);
                Assert.Equal("COMMITTED", (await dispatch).Status);
                Assert.Equal(1, (await dispatch).Delivered);
            }
            finally { commandGate.Release(); }
        }
        finally { workerGate.Release(); }
        await AssertSingleDelivery(seed, claim);
    }

    [Fact]
    public async Task ClockExactReplayAndDeadlineDispatchCompleteWithoutRepeatingExtension()
    {
        await using var seed = sql.CreateDbContext();
        var source = await H4GenuineRepairSource.Seed(seed, sql);
        var proposal = await ProposeNormal(seed, source);
        var row = await seed.Set<DeadlineClock>().SingleAsync(c => c.TargetId == proposal.Item);
        var originalDue = row.OriginalDueAt;
        // A different appointed Supervisor lets the replay own its User and Role while
        // the worker owns another User and the clock, reproducing Roles <-> DeadlineClocks.
        var recipient = new ApplicationUser
        {
            Id = Guid.Parse("00000001-0000-0000-0000-000000000001"),
            UserName = Guid.NewGuid().ToString(),
            DisplayName = "Concurrency recipient",
            PasswordHash = "fixture",
            RoleCode = UserRoleCode.Supervisor,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        seed.AddRange(recipient, new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = source.Project,
            UserId = recipient.Id,
            RoleCode = UserRoleCode.Supervisor,
            Status = ProjectMemberStatus.Active,
            ValidFrom = new(2000, 1, 1)
        });
        await seed.SaveChangesAsync();
        var clockCommands = new ClockDutyRepository(seed, new IdempotencyOperationService(seed), TimeProvider.System);
        Assert.Equal(201, (await clockCommands.ExecuteAsync(new(source.Supervisor, UserRoleCode.Supervisor, source.Project,
            row.Id, "appoint", Guid.NewGuid().ToString(), Convert.ToBase64String(row.RowVersion), "appointed recipient", null, recipient.Id), default)).Status);
        seed.ChangeTracker.Clear();
        row = await seed.Set<DeadlineClock>().SingleAsync(c => c.Id == row.Id);
        var request = new ClockDutyCommand(source.Supervisor, UserRoleCode.Supervisor, source.Project, row.Id,
            "extend", Guid.NewGuid().ToString(), Convert.ToBase64String(row.RowVersion), "concurrency regression", originalDue.AddHours(4), null);
        Assert.Equal(201, (await new ClockDutyRepository(seed, new IdempotencyOperationService(seed), TimeProvider.System)
            .ExecuteAsync(request, default)).Status);
        var time = new FixedClock(originalDue.AddHours(5));
        await new H6NotificationDispatchRepository(seed, time).ObserveClocksAsync(default);
        seed.ChangeTracker.Clear();
        var breach = await seed.Set<DeadlineBreach>().SingleAsync(b => b.ClockId == row.Id);
        var claim = await Lease(seed, await seed.OutboxMessages.SingleAsync(m => m.Id == breach.Id), time);
        var workerGate = new PauseOnceCommandInterceptor(text => text.Contains("FROM [Roles] WITH", StringComparison.Ordinal));
        var commandGate = new PauseOnceCommandInterceptor(text => text.Contains("FROM [DeadlineClocks] WITH", StringComparison.Ordinal));
        await using var worker = sql.CreateDbContext(workerGate);
        await using var command = sql.CreateDbContext(commandGate);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var dispatch = Dispatch(worker, claim, time, timeout.Token);
        try
        {
            await workerGate.Reached.WaitAsync(TimeSpan.FromSeconds(15));
            var replay = new ClockDutyRepository(command, new IdempotencyOperationService(command), time).ExecuteAsync(request, timeout.Token);
            try
            {
                await commandGate.Reached.WaitAsync(TimeSpan.FromSeconds(15));
                workerGate.Release(); commandGate.Release();
                await Task.WhenAll(dispatch, replay);
                Assert.Equal(200, (await replay).Status);
                Assert.Equal("COMMITTED", (await dispatch).Status);
                Assert.Equal(2, (await dispatch).Delivered);
            }
            finally { commandGate.Release(); }
        }
        finally { workerGate.Release(); }
        seed.ChangeTracker.Clear();
        Assert.Equal(originalDue, (await seed.Set<DeadlineClock>().SingleAsync(c => c.Id == row.Id)).OriginalDueAt);
        Assert.Equal(1, await seed.Set<DeadlineExtension>().CountAsync(e => e.ClockId == row.Id));
        Assert.Equal(1, await seed.OutboxMessages.CountAsync(m => m.MessageType == "clock.extend.v1" && m.CorrelationId == row.Id));
        Assert.Equal(1, await seed.IdempotencyRecords.CountAsync(r => r.IdempotencyKey == request.Key && r.ActorUserId == source.Supervisor));
        await AssertSingleDelivery(seed, claim, expectedDeliveries: 2);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("role")]
    [InlineData("membership")]
    public async Task AuthorityReadLocksBlockRevocationUntilDeliveryCommits(string resource)
    {
        var source = await new H6FieldSourceSqlTests(sql).Seed();
        await using var seed = sql.CreateDbContext();
        var claim = await Lease(seed, await seed.OutboxMessages.SingleAsync(m => m.Id == source.Event), TimeProvider.System);
        var gate = new PauseBeforeSave();
        await using var worker = sql.CreateDbContext(gate);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(40));
        var dispatch = Dispatch(worker, claim, TimeProvider.System, timeout.Token);
        try
        {
            await gate.Reached.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await using var revoke = sql.CreateDbContext();
            await revoke.Database.OpenConnectionAsync();
            await revoke.Database.ExecuteSqlRawAsync("SET LOCK_TIMEOUT 1000");
            Task<int> Revoke() => resource switch
            {
                "user" => revoke.Users.Where(u => u.Id == source.Crew).ExecuteUpdateAsync(s => s.SetProperty(u => u.MustChangePassword, true)),
                "role" => revoke.Roles.Where(r => r.Code == UserRoleCode.RepairCrew).ExecuteUpdateAsync(s => s.SetProperty(r => r.IsActive, false)),
                _ => revoke.ProjectMembers.Where(m => m.UserId == source.Crew).ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, ProjectMemberStatus.Ended))
            };
            var failure = await Assert.ThrowsAnyAsync<Exception>(async () => await Revoke());
            Assert.Equal(1222, Assert.IsType<SqlException>(failure.GetBaseException()).Number);
            gate.Release.TrySetResult();
            Assert.Equal("COMMITTED", (await dispatch).Status);
            // The same write succeeds after commit; rollback keeps shared fixture roles intact.
            await using var revocation = await revoke.Database.BeginTransactionAsync();
            Assert.Equal(1, await Revoke());
            await revocation.RollbackAsync();
        }
        finally { gate.Release.TrySetResult(); }
        await AssertSingleDelivery(seed, claim);
    }

    private static bool IsAuthorityRead(string text) => text.Contains("FROM [Users] WITH", StringComparison.Ordinal);
    private static Task<H6DispatchOutcome> Dispatch(RoadGuardDbContext db, H6Claim claim, TimeProvider clock, CancellationToken token)
        => new H6NotificationDispatchRepository(db, clock).DispatchAsync(claim,
            H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson), token);
    private static async Task<H6Claim> Lease(RoadGuardDbContext db, OutboxMessage message, TimeProvider clock)
    {
        var fence = Guid.NewGuid(); message.AcquireLease("h6:" + fence.ToString("N"), clock.GetUtcNow(), TimeSpan.FromMinutes(2), 32);
        await db.SaveChangesAsync();
        return new(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson, fence,
            message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
    }
    private static async Task AssertSingleDelivery(RoadGuardDbContext db, H6Claim claim, int expectedDeliveries = 1)
    {
        db.ChangeTracker.Clear();
        Assert.Single(await db.Set<H6NotificationEventReceipt>().Where(r => r.OutboxMessageId == claim.Id).ToArrayAsync());
        var occurrence = await db.Set<H6NotificationOccurrenceRow>().SingleAsync(o => o.SourceEventId == claim.Id);
        Assert.Equal(expectedDeliveries, await db.Set<H6NotificationDeliveryRow>().CountAsync(d => d.OccurrenceId == occurrence.Id && d.Status == "DELIVERED"));
        Assert.Equal(OutboxDeliveryStatus.Completed, (await db.OutboxMessages.SingleAsync(m => m.Id == claim.Id)).DeliveryStatus);
        Assert.Equal("COMMITTED", (await Dispatch(db, claim, TimeProvider.System, default)).Status);
        Assert.Single(await db.Set<H6NotificationEventReceipt>().Where(r => r.OutboxMessageId == claim.Id).ToArrayAsync());
    }
    private static async Task<(Guid Item, Guid Package)> ProposeNormal(RoadGuardDbContext db, H4GenuineRepairSource.Source source)
    {
        var obligation = RoadGuardSystem.BusinessObjects.Repairs.RepairObligation.Create(Guid.NewGuid(), source.Project, source.Defect,
            RepairObligationKind.FormalRepair, true, RoadGuardSystem.BusinessObjects.Repairs.RepairActualScope.Create(
                Guid.NewGuid(), source.Road, "actual-route:" + source.Route, "road", 1, 2, 0, 1));
        var package = RoadGuardSystem.BusinessObjects.Repairs.RepairPackage.Create(Guid.NewGuid(), source.Project, source.Defect, [obligation]);
        db.Add(package); db.Entry(package).Property(p => p.DefectStatusAtAnchor).CurrentValue = DefectStatus.Open; await db.SaveChangesAsync();
        var result = await new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System).ProposeItemAsync(
            new(source.Pm, UserRoleCode.ProjectManager, source.Project, package.Id,
                new(obligation.Id, "NORMAL", "concurrent review", "checklist-v1", "Supervisor duty"), Guid.NewGuid().ToString(),
                Convert.ToBase64String(db.Entry(package).Property<byte[]>("RowVersion").CurrentValue!)), default);
        Assert.Equal(201, result.Status);
        return (Assert.IsType<RoadGuardSystem.Repositories.Repairs.RepairItemFact>(result.Value).Id, package.Id);
    }
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
    private sealed class PauseBeforeSave : SaveChangesInterceptor
    {
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Reached.TrySetResult(); await Release.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken); return result;
        }
    }
}
