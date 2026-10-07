using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Clocks;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed partial class H6ProtectedNotificationRepository
{
    public Task<H6ScopeFact?> ScopeAsync(Guid actorId, UserRoleCode authenticatedRole, Guid notificationId, CancellationToken cancellationToken)
        => ReadAsync<H6ScopeFact?>(actorId, async (role, token) =>
            await db.Set<H6NotificationScopeRow>().AsNoTracking().Where(scope => scope.NotificationId == notificationId &&
                Visible(actorId, role).Any(notification => notification.Id == scope.NotificationId))
                .Select(scope => new H6ScopeFact(scope.NotificationId, scope.ProjectId, scope.OccurrenceId,
                    scope.Classification, scope.SourceKind, scope.SourceId, scope.EventType, scope.ResolverVersion)).SingleOrDefaultAsync(token),
            () => null, cancellationToken, authenticatedRole);
    public Task<H6ClockPageFact> ClocksPageAsync(Guid actorId, UserRoleCode authenticatedRole, Guid projectId,
        H6ClockCursorFact? cursor, int limit, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1); ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, 100);
        return ReadAsync(actorId, async (role, token) =>
        {
            var now = clock.GetUtcNow().ToUniversalTime(); var day = DateOnly.FromDateTime(now.UtcDateTime);
            if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor) ||
                !await db.ProjectMembers.AnyAsync(member => member.ProjectId == projectId && member.UserId == actorId && member.RoleCode == role &&
                    member.Status == ProjectMemberStatus.Active && member.ValidFrom <= day && (!member.ValidTo.HasValue || member.ValidTo >= day), token))
                return new H6ClockPageFact("DENIED", []);
            var query = db.Set<DeadlineClock>().AsNoTracking().Where(row => row.ProjectId == projectId);
            if (cursor is not null)
            {
                if (cursor.ActorId != actorId || cursor.ProjectId != projectId || cursor.Role != role.ToString() ||
                    cursor.AfterClockId == Guid.Empty || cursor.AfterDueAtUtc == default || cursor.AfterDueAtUtc.Offset != TimeSpan.Zero)
                    return new H6ClockPageFact("CURSOR_INVALID", []);
                if (!await query.AnyAsync(row => row.Id == cursor.AfterClockId && row.CurrentDueAt == cursor.AfterDueAtUtc, token))
                    return new H6ClockPageFact("CURSOR_STALE", []);
                query = query.Where(row => row.CurrentDueAt > cursor.AfterDueAtUtc ||
                    row.CurrentDueAt == cursor.AfterDueAtUtc && row.Id.CompareTo(cursor.AfterClockId) > 0);
            }
            var rows = await query.OrderBy(row => row.CurrentDueAt).ThenBy(row => row.Id).Take(limit + 1)
                .Include(row => row.Extensions).Include(row => row.Breaches).Include(row => row.Appointments).AsSplitQuery().ToArrayAsync(token);
            var more = rows.Length > limit; var selected = rows.Take(limit).ToArray();
            var items = selected.Select(row => new H6ClockFact(row.Id, row.ProjectId, row.Kind.ToString(), row.TargetId, row.OriginEventId,
                row.OriginAt, row.OriginalDueAt, row.CurrentDueAt, row.IsOverdueAt(now), row.CompletedAt, row.AcknowledgedAt,
                Convert.ToBase64String(row.RowVersion), row.Extensions.OrderBy(extension => extension.OccurredAt).ThenBy(extension => extension.Id)
                    .Select(extension => new H6ClockExtensionFact(extension.Id, extension.ActorUserId, extension.OccurredAt,
                        extension.PreviousDueAt, extension.NewDueAt, extension.Reason, extension.PreviousDeadlineBreached)).ToArray(),
                row.Breaches.OrderBy(breach => breach.DueAt).ThenBy(breach => breach.Id)
                    .Select(breach => new H6ClockBreachFact(breach.Id, breach.DueAt, breach.ObservedAt)).ToArray(),
                ["EXTENSION_NUMERICAL_LIMIT_POLICY_PENDING", "ADDITIONAL_FT_OFFLINE_AUTHORIZATION_PENDING", "WEEKLY_CATCHUP_POLICY_PENDING"])
            {
                AppointedActorId = row.AppointedActorId, AppointedRole = row.AppointedRole?.ToString(),
                Appointments = row.Appointments.OrderBy(a => a.EffectiveAt).Select(a => (object)new
                { a.Id, a.PreviousActorId, a.CurrentActorId, role = a.Role.ToString(), a.DecisionActorId, a.Reason, a.EffectiveAt }).ToArray()
            }).ToArray();
            var last = more ? selected[^1] : null;
            return new H6ClockPageFact("READY", items, last is null ? null : new(actorId, projectId, role.ToString(), last.CurrentDueAt, last.Id));
        }, () => new H6ClockPageFact("DENIED", []), cancellationToken, authenticatedRole);
    }
}
