using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.Repositories.Messaging;

namespace RoadGuardSystem.Services.Messaging;

public sealed class H6NotificationReadService(IH6NotificationOperationsRepository repository)
{
    public async Task<H6NotificationScopeDto?> ScopeAsync(Guid actorId, UserRoleCode role, Guid notificationId,
        CancellationToken cancellationToken)
    {
        if (actorId == Guid.Empty || notificationId == Guid.Empty || role == UserRoleCode.Unknown) return null;
        var fact = await repository.ScopeAsync(actorId, role, notificationId, cancellationToken);
        return fact is null ? null : new(fact.NotificationId, fact.ProjectId, fact.OccurrenceId,
            fact.Classification, fact.SourceKind, fact.SourceId, fact.EventType, fact.ResolverVersion);
    }

    public async Task<H6ClockPage> ClocksPageAsync(Guid actorId, UserRoleCode role, Guid projectId,
        H6ClockCursor? cursor, int limit, CancellationToken cancellationToken)
    {
        if (actorId == Guid.Empty || projectId == Guid.Empty || role == UserRoleCode.Unknown || limit is < 1 or > 100)
            return new("CURSOR_INVALID", []);
        var fact = await repository.ClocksPageAsync(actorId, role, projectId,
            cursor is null ? null : new(cursor.ActorId, cursor.ProjectId, cursor.Role, cursor.AfterDueAtUtc, cursor.AfterClockId),
            limit, cancellationToken);
        var views = fact.Items.Select(row => new H6ClockView(row.Id, row.ProjectId, row.Kind, row.TargetId,
            row.OriginEventId, row.OriginAt, row.OriginalDueAt, row.CurrentDueAt, row.Overdue, row.CompletedAt,
            row.AcknowledgedAt, row.Version,
            row.Extensions.Select(extension => new H6ClockExtensionView(extension.Id, extension.ActorId, extension.At,
                extension.PreviousDueAt, extension.NewDueAt, extension.Reason, extension.PreviousDeadlineBreached)).ToArray(),
            row.Breaches.Select(breach => new H6ClockBreachView(breach.Id, breach.DueAt, breach.ObservedAt)).ToArray(),
            row.PendingCapabilities)).ToArray();
        var next = fact.Continuation;
        return new(fact.Status, views, next is null ? null : new(next.ActorId, next.ProjectId, next.Role,
            next.AfterDueAtUtc, next.AfterClockId));
    }
}
