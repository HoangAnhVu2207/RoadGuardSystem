using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Messaging;

// Every actual source adapter uses the same scoped DbContext/transaction. A missing source producer
// is PENDING_PRODUCER; never a fabricated source fact or successful no-op consumer.
public interface IH6NotificationSourceAdapter
{
    bool Supports(string sourceKind);
    IQueryable<H6SourceScope> ScopeQuery();
    Task<H6SourceResolution> ResolveAsync(H6DispatchPlan plan, CancellationToken cancellationToken);
}
public interface IH6NotificationDispatchRepository
{
    Task<H6Claim?> ClaimAsync(IReadOnlyList<string> registeredTypes, CancellationToken cancellationToken);
    Task<H6DispatchOutcome> DispatchAsync(H6Claim claim, H6DispatchPlan plan, CancellationToken cancellationToken);
    Task<H6DispatchOutcome> RejectAsync(H6Claim claim, string reasonCode, CancellationToken cancellationToken);
    Task<int> RetryUnresolvedAsync(CancellationToken cancellationToken);
    Task<int> ObserveClocksAsync(CancellationToken cancellationToken);
    Task<int> ObserveCalendarAsync(Guid schedulerRunId, NotificationScheduledCallbackWitness? witness, CancellationToken cancellationToken);
}
public interface IH6NotificationOperationsRepository
{
    Task<H6BulkReadFact> MarkManyReadAsync(Guid actor, UserRoleCode role, DateTimeOffset through, int limit, string key, string? snapshotVersion, CancellationToken token)
        => Task.FromResult(new H6BulkReadFact(503, "notification_unavailable", 0, false, through));
    Task<WeeklyDigestReadFact?> WeeklyDigestAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid digestId, CancellationToken token);
    Task<H6ScopeFact?> ScopeAsync(Guid actorId, UserRoleCode authenticatedRole, Guid notificationId, CancellationToken cancellationToken);
    Task<H6ClockPageFact> ClocksPageAsync(Guid actorId, UserRoleCode authenticatedRole, Guid projectId,
        H6ClockCursorFact? cursor, int limit, CancellationToken cancellationToken);
}

public sealed record H6BulkReadFact(int Status, string? Code, int Updated, bool HasMore, DateTimeOffset Through, string? SnapshotVersion = null);
