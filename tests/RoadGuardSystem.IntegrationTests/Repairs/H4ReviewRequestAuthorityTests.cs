using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed partial class H4CorrectionAuthorityTests
{
    [Theory]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.RepairCrew)]
    public async Task ActualRequestAppendsHistoryAndReceiptWithoutChangingEffectiveRepair(UserRoleCode role)
    {
        await using var db = Db(); var facts = await Seed(db, RepairMode.Normal, role);
        if (role == UserRoleCode.RepairCrew)
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairItems SET CrewId={facts.Actor} WHERE Id={facts.Item}");
        var version = Convert.ToBase64String(await db.Set<RepairItem>().Where(row => row.Id == facts.Item)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var command = new RepairReviewRequestCommand(facts.Actor, role, facts.Project, facts.Package, facts.Item,
            new("The recorded decision needs reconsideration"), "request-review-1", version);
        var repository = Repository(db); var result = await repository.RequestReviewAsync(command, CancellationToken.None);
        Assert.Equal(201, result.Status); db.ChangeTracker.Clear();
        var item = await db.Set<RepairItem>().Include(row => row.Decisions).Include(row => row.ReviewRequests).SingleAsync(row => row.Id == facts.Item);
        var obligation = await db.Set<RepairObligation>().Include(row => row.ResolutionHistory).SingleAsync(row => row.Id == facts.Obligation);
        Assert.Equal(facts.OriginalDecision, item.EffectiveDecisionId); Assert.Equal(RepairItemState.Confirmed, item.State);
        Assert.Equal(facts.OriginalDecision, obligation.EffectiveResolutionHeadDecisionId); Assert.True(obligation.IsResolved);
        Assert.Single(item.Decisions); Assert.Single(obligation.ResolutionHistory); Assert.Single(item.ReviewRequests);
        Assert.Equal(facts.Actor, item.ReviewRequests.Single().ActorId); Assert.Equal(facts.OriginalDecision, item.ReviewRequests.Single().DecisionId);
        Assert.Equal(version, Convert.ToBase64String(db.Entry(item).Property<byte[]>("RowVersion").CurrentValue!));
        var package = await db.Set<RepairPackage>().Include(row => row.Obligations).SingleAsync(row => row.Id == facts.Package);
        Assert.True(package.IsComplete); Assert.Single(await db.IdempotencyRecords.Where(row => row.ProjectId == facts.Project).ToListAsync());
        Assert.Single(await db.AuditLogs.Where(row => row.EntityId == facts.Item && row.EventType == "repair_review_requested").ToListAsync());
        Assert.False(await db.OutboxMessages.AnyAsync(row => row.MessageType == "repair.decision.corrected.v1"));
        var replay = await repository.RequestReviewAsync(command, CancellationToken.None);
        Assert.Equal(200, replay.Status); Assert.True(replay.Replayed);
        db.ChangeTracker.Clear(); Assert.Single(await db.Set<RepairReviewRequest>().Where(row => row.ItemId == facts.Item).ToListAsync());
        var member = await db.ProjectMembers.SingleAsync(row => row.Id == facts.Membership); member.Status = ProjectMemberStatus.Ended;
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(403, (await repository.RequestReviewAsync(command, CancellationToken.None)).Status);
    }

    [Fact]
    public async Task UnassignedCurrentCrewCannotRequestReviewForAnotherCrewItem()
    {
        await using var db = Db(); var facts = await Seed(db, RepairMode.FastTrack, UserRoleCode.RepairCrew);
        var result = await Repository(db).RequestReviewAsync(new(facts.Actor, facts.Role, facts.Project, facts.Package,
            facts.Item, new("Request on another Crew work"), "request-review-1", facts.Version), CancellationToken.None);
        Assert.Equal(403, result.Status); Assert.Null(result.Value);
        Assert.False(await db.Set<RepairReviewRequest>().AnyAsync(row => row.ItemId == facts.Item));
    }

    [Fact]
    public async Task RequestWithObsoleteHeadVersionCannotBecomeUnpinnedHistory()
    {
        await using var db = Db(); var facts = await Seed(db, RepairMode.FastTrack, UserRoleCode.ProjectManager);
        var repository = Repository(db); Assert.Equal(201, (await repository.CorrectAsync(Command(facts), CancellationToken.None)).Status);
        var result = await repository.RequestReviewAsync(new(facts.Actor, facts.Role, facts.Project, facts.Package,
            facts.Item, new("Old decision request"), "request-review-1", facts.Version), CancellationToken.None);
        Assert.Equal(409, result.Status); Assert.False(await db.Set<RepairReviewRequest>().AnyAsync(row => row.ItemId == facts.Item));
    }
}
