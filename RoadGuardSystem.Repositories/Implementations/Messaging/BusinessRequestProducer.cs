using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;

namespace RoadGuardSystem.Repositories.Messaging;

// Called only inside the genuine source producer's transaction. Notification delivery never calls this.
internal static class BusinessRequestProducer
{
    public static void Supplement(RoadGuardDbContext db, Guid project, string sourceKind, Guid review,
        Guid task, Guid crew, DateTimeOffset at)
        => db.Add(BusinessReceivingRequest.Create(Guid.NewGuid(), project, DeadlineClockKind.CrewSupplement,
            sourceKind, review, review.ToString("N"), task, crew, UserRoleCode.RepairCrew, at));

    public static async Task CompleteSupplements(RoadGuardDbContext db, Guid project, Guid task,
        DateTimeOffset at, CancellationToken token)
    {
        var requests = await db.Set<BusinessReceivingRequest>().FromSqlInterpolated(
            $"SELECT * FROM [BusinessReceivingRequests] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={project} AND [ScopeId]={task} AND [Kind]={(byte)DeadlineClockKind.CrewSupplement} AND [CompletedAt] IS NULL")
            .Include(x => x.Clock).ThenInclude(x => x!.Breaches).ToArrayAsync(token);
        foreach (var request in requests) request.Complete(at);
    }

    public static async Task ReassignSupplements(RoadGuardDbContext db, Guid project, Guid task,
        Guid next, Guid actor, string reason, DateTimeOffset at, CancellationToken token)
    {
        var requests = await db.Set<BusinessReceivingRequest>().FromSqlInterpolated(
            $"SELECT * FROM [BusinessReceivingRequests] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={project} AND [ScopeId]={task} AND [Kind]={(byte)DeadlineClockKind.CrewSupplement} AND [CompletedAt] IS NULL")
            .ToArrayAsync(token);
        foreach (var request in requests) request.Appoint(next, actor, reason, at);
    }
}
