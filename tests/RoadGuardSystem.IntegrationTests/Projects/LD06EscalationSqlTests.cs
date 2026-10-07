using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.IntegrationTests.Repairs;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Repositories.Projects;
using Xunit;
using Xunit.Abstractions;

namespace RoadGuardSystem.IntegrationTests.Projects;

public sealed class LD06EscalationSqlTests(IdentitySqlServerFixture sql, ITestOutputHelper output) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public Task TransferredPendingReviewEscalatesToReceivingSupervisorAndOriginalAckClockDoesNotReset()
        => new H4RepairExecutionSqlTests(sql, output).WithPendingReview(async (db, source, itemId, reviewClockId) =>
        {
            var receiver = await H4GenuineRepairSource.Seed(db, sql);
            var item = await db.Set<RepairItem>().AsNoTracking().SingleAsync(i => i.Id == itemId);
            var repository = new ProjectLifecycleRepository(db, TimeProvider.System);
            var sourceView = (await repository.ReadAsync(source.Supervisor, source.Project, default))!;
            var scope = sourceView.TransferableObligations!.Single(o => o.Id == item.ObligationId).ScopeHash;
            Assert.Equal(201, (await repository.ExecuteAsync(new(source.Supervisor, UserRoleCode.Supervisor, source.Project,
                LD06ActionKind.IssueTransfer, new("TEST_ONLY pending review transfer", [], ObligationId: item.ObligationId,
                    ReceivingProjectId: receiver.Project, ScopeHash: scope), Guid.NewGuid().ToString(), sourceView.Version), default)).Status);
            var issue = await db.Set<LD06LifecycleAction>().SingleAsync(a => a.ProjectId == source.Project && a.Kind == LD06ActionKind.IssueTransfer);
            var receiverView = (await repository.ReadAsync(receiver.Supervisor, receiver.Project, default))!;
            Assert.Equal(201, (await repository.ExecuteAsync(new(receiver.Supervisor, UserRoleCode.Supervisor, receiver.Project,
                LD06ActionKind.AcceptTransfer, new("TEST_ONLY accept review duty", [], issue.Id, ObligationId: item.ObligationId,
                    ScopeHash: scope), Guid.NewGuid().ToString(), receiverView.Version), default)).Status);
            var stock = new RoadGuardSystem.Repositories.Implementations.Reporting.CurrentRepairFactsRepository(db, TimeProvider.System);
            var receivingStock = await stock.CaptureAsync(receiver.Supervisor, receiver.Project, new(), default);
            // Receiver seed includes a separate genuine Report Defect without an obligation.
            // That inventory remains UNKNOWN while the accepted source obligation is readable.
            Assert.Equal(new[] { "REPAIR_OBLIGATION_INVENTORY_NOT_VERIFIED" }, receivingStock.MissingReasons);
            Assert.Equal("REPORTED_AWAITING_REVIEW", Assert.Single(receivingStock.Items).Presentation);
            Assert.Empty((await stock.CaptureAsync(source.Supervisor, source.Project, new(), default)).Items);
            var original = await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(c => c.Id == reviewClockId);
            var at = original.OriginalDueAt.AddMinutes(1); var time = new FixedClock(at);
            Assert.True(await new H6NotificationDispatchRepository(db, time).ObserveClocksAsync(default) > 0);
            db.ChangeTracker.Clear();
            var request = await db.Set<BusinessReceivingRequest>().SingleAsync(r => r.SourceKind == "ReviewBreach" && r.ScopeId == reviewClockId);
            Assert.Equal(source.Project, request.ProjectId); Assert.Equal(receiver.Supervisor, request.ResponsibleActorId);
            var duties = new BusinessDutyRepository(db, new IdempotencyOperationService(db), time);
            Assert.Equal(404, (await duties.ReadAsync(source.Supervisor, UserRoleCode.Supervisor, source.Project, request.Id, default)).Status);
            var read = await duties.ReadAsync(receiver.Supervisor, UserRoleCode.Supervisor, receiver.Project, request.Id, default);
            Assert.Equal(200, read.Status);
            var command = new BusinessDutyCommand(receiver.Supervisor, UserRoleCode.Supervisor, receiver.Project, request.Id,
                "ack", Guid.NewGuid().ToString(), read.Version!, null, null, null);
            Assert.Equal(201, (await duties.ExecuteAsync(command, default)).Status);
            Assert.Equal(200, (await duties.ExecuteAsync(command, default)).Status);
            db.ChangeTracker.Clear();
            request = await db.Set<BusinessReceivingRequest>().SingleAsync(r => r.Id == request.Id);
            var escalation = await db.Set<DeadlineClock>().SingleAsync(c => c.Id == request.ClockId);
            Assert.Equal(source.Project, escalation.ProjectId); Assert.Equal(at, escalation.OriginAt);
            Assert.Equal(at.AddHours(24), escalation.OriginalDueAt);
            var retained = await db.Set<DeadlineClock>().SingleAsync(c => c.Id == reviewClockId);
            Assert.Equal(original.OriginAt, retained.OriginAt); Assert.Equal(original.OriginalDueAt, retained.OriginalDueAt);
            await using var tx = await db.Database.BeginTransactionAsync();
            Assert.Equal(receiver.Project, await new H6DeadlineNotificationSourceAdapter(db, time).ResponsibilityProjectAsync(escalation, default));
            await tx.CommitAsync();
        });
    private sealed class FixedClock(DateTimeOffset at) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => at; }
}
