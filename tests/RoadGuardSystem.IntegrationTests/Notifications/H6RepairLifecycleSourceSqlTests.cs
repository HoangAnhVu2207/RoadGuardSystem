using RoadGuardSystem.Repositories.Repairs;
using System.Data;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.IntegrationTests.Repairs;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class H6RepairLifecycleSourceSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task RemovedCurrentRepairBindingCannotDeliverUsingAnOtherwiseActiveNativeAssignment()
    {
        var seed = await Seed(true); await using var db = sql.CreateDbContext(); var claim = await Lease(db, seed.Event);
        // Controlled external projection revocation fixture, not a production unassignment command.
        await db.RepairItems.Where(row => row.Id == seed.Item).ExecuteUpdateAsync(update => update.SetProperty(row => row.CurrentBindingId, (Guid?)null));
        var result = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim, Plan(claim), default);
        Assert.Equal("COMMITTED", result.Status); Assert.Equal(0, result.Delivered); Assert.Equal(1, result.Unresolved);
        Assert.False(await db.Notifications.AnyAsync(row => row.SourceEntityId == seed.Item));
    }
    [Fact]
    public async Task ActualNormalProposalCreatesOnlyCurrentScopedSupervisorNotification()
    {
        var seed = await Seed(false); await using var db = sql.CreateDbContext(); var claim = await Lease(db, seed.Event);
        var result = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim, Plan(claim), default);
        Assert.Equal("COMMITTED", result.Status); Assert.Equal(1, result.Delivered); Assert.Equal(0, result.Unresolved);
        Assert.Equal(seed.Supervisor, (await db.Notifications.SingleAsync(row => row.SourceEntityId == seed.Item)).RecipientUserId);
    }
    [Fact]
    public async Task ActualAssignedSourceDispatchAndInboxUseLiveNativeBindingBeforeServerPaging()
    {
        var seed = await Seed(true); await using var db = sql.CreateDbContext(); var claim = await Lease(db, seed.Event);
        var result = await new H6NotificationDispatchRepository(db, TimeProvider.System).DispatchAsync(claim, Plan(claim), default);
        Assert.Equal("COMMITTED", result.Status); Assert.Equal(1, result.Delivered);
        var adapter = new H6RepairNotificationSourceAdapter(db);
        Assert.True(await adapter.ScopeQuery().AnyAsync(row => row.SourceId == seed.Item && row.AssignedUserId == seed.Crew));
        var notification = await db.Notifications.SingleAsync(row => row.SourceEntityId == seed.Item);
        var inbox = new H6ProtectedNotificationRepository(db, new IdempotencyOperationService(db), TimeProvider.System, [adapter]);
        Assert.Equal(notification.Id, Assert.Single((await inbox.ListAsync(seed.Crew, null, null, 1)).Items).Id);
        var assignment = await db.FieldInspectionAssignments.SingleAsync(row => row.Id == seed.Assignment);
        assignment.End(DateTimeOffset.UtcNow, "actual later handover"); await db.SaveChangesAsync();
        Assert.Null(await inbox.GetAsync(seed.Crew, notification.Id)); Assert.Empty((await inbox.ListAsync(seed.Crew, null, null, 1)).Items);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        Assert.Equal("VERIFIED", (await adapter.ResolveAsync(Plan(claim), default)).Status); await transaction.CommitAsync();
    }
    private sealed record Source(Guid Item, Guid Event, Guid Crew, Guid Supervisor, Guid? Assignment);
    private async Task<Source> Seed(bool assigned)
    {
        await using var db = sql.CreateDbContext(); var actual = await H4GenuineRepairSource.Seed(db, sql);
        var version = Convert.ToBase64String(await db.Defects.Where(row => row.Id == actual.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var repository = new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var created = await repository.CreatePackageAsync(new(actual.Pm, UserRoleCode.ProjectManager, actual.Project,
            new(actual.Defect, version, [new("FORMAL_REPAIR", true, new(actual.Road, actual.Route, actual.Set, null, null, 1, 2, 0, 1), "actual repair")], "actual package"),
            Guid.NewGuid().ToString(), version), default);
        Assert.Equal(201, created.Status); var packageView = Assert.IsType<RepairPackageFact>(created.Value);
        var package = await db.Set<RepairPackage>().Include(row => row.Obligations).SingleAsync(row => row.Id == packageView.Id);
        var proposed = await repository.ProposeItemAsync(new(actual.Pm, UserRoleCode.ProjectManager, actual.Project, package.Id,
            new(package.Obligations[0].Id, "NORMAL", "actual proposal", "checklist-v1", "actual proposal"), Guid.NewGuid().ToString(), packageView.Version), default);
        Assert.Equal(201, proposed.Status); var itemView = Assert.IsType<RepairItemFact>(proposed.Value);
        if (!assigned)
        {
            var proposal = await db.Set<RepairItemLifecycleEvent>().SingleAsync(row => row.ItemId == itemView.Id && row.Kind == "PROPOSED");
            return new(itemView.Id, proposal.Id, actual.Crew, actual.Supervisor, null);
        }
        var approved = await repository.ApproveItemAsync(new(actual.Supervisor, UserRoleCode.Supervisor, actual.Project, package.Id,
            itemView.Id, new("actual approval"), Guid.NewGuid().ToString(), itemView.Version), default);
        Assert.Equal(201, approved.Status); var approvalView = Assert.IsType<RepairItemFact>(approved.Value);
        var currentDefectVersion = Convert.ToBase64String(await db.Defects.Where(row => row.Id == actual.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var task = new RepairFieldTaskData(actual.Defect, currentDefectVersion, null, "REPORTER", actual.Route, actual.Set, null, null,
            "POST_REPAIR", 1, "{}", null, actual.Crew, DateTimeOffset.UtcNow.AddDays(1));
        var assignedResult = await repository.AssignItemAsync(new(actual.Pm, UserRoleCode.ProjectManager, actual.Project, package.Id,
            itemView.Id, new(task, null, "actual assignment"), Guid.NewGuid().ToString(), approvalView.Version), default);
        Assert.Equal(201, assignedResult.Status); var bindingView = Assert.IsType<RepairTaskBindingFact>(assignedResult.Value);
        var source = await db.Set<RepairItemLifecycleEvent>().SingleAsync(row => row.ItemId == itemView.Id && row.Kind == "ASSIGNED");
        return new(itemView.Id, source.Id, actual.Crew, actual.Supervisor, bindingView.AssignmentId);
    }
    private static H6DispatchPlan Plan(H6Claim claim) => H6NotificationCatalog.Parse(claim.Id, claim.MessageType, claim.OccurredAtUtc, claim.PayloadJson);
    private static async Task<H6Claim> Lease(RoadGuardDbContext db, Guid id)
    {
        var message = await db.OutboxMessages.SingleAsync(row => row.Id == id); var fence = Guid.NewGuid();
        message.AcquireLease("h6:" + fence.ToString("N"), DateTimeOffset.UtcNow, TimeSpan.FromMinutes(2), 32); await db.SaveChangesAsync();
        return new(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson, fence, message.LeaseExpiresAtUtc!.Value, message.DeliveryAttemptCount);
    }
}
