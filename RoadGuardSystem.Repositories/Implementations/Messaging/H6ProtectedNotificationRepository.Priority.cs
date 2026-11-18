using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed partial class H6ProtectedNotificationRepository
{
    private async Task<Dictionary<Guid, NotificationPriorityFact>> ReadPrioritiesAsync(Guid[] ids, DateTimeOffset now, CancellationToken token)
    {
        if (ids.Length == 0) return [];
        var inputs = await (from notification in db.Notifications.AsNoTracking()
                            where ids.Contains(notification.Id)
                            join sidecar in db.Set<H6NotificationScopeRow>().AsNoTracking()
                                on notification.Id equals sidecar.NotificationId into sidecars
                            from scope in sidecars.DefaultIfEmpty()
                            join source in db.Set<H6NotificationOccurrenceRow>().AsNoTracking()
                                on scope.OccurrenceId equals (Guid?)source.Id into occurrences
                            from occurrence in occurrences.DefaultIfEmpty()
                            select new
                            {
                                notification.Id,
                                Project = scope.ProjectId,
                                ScopeSource = (Guid?)scope.SourceId,
                                ScopeKind = (string?)scope.SourceKind,
                                Occurrence = (Guid?)occurrence.Id,
                                Origin = (Guid?)occurrence.OriginEventId,
                                OccurrenceProject = (Guid?)occurrence.ProjectId,
                                OccurrenceSource = (Guid?)occurrence.SourceId,
                                OccurrenceKind = (string?)occurrence.SourceKind
                            }).ToArrayAsync(token);
        var projects = inputs.Where(row => row.Project.HasValue).Select(row => row.Project!.Value).Distinct().ToArray();
        var sourceIds = inputs.Where(row => row.ScopeSource.HasValue).Select(row => row.ScopeSource!.Value).Distinct().ToArray();
        var origins = inputs.Where(row => row.Origin.HasValue).Select(row => row.Origin!.Value).Distinct().ToArray();
        var candidates = await db.Set<DeadlineClock>().AsNoTracking().Where(row => projects.Contains(row.ProjectId) &&
            (sourceIds.Contains(row.Id) || origins.Contains(row.OriginEventId))).ToArrayAsync(token);
        var adapter = new H6DeadlineNotificationSourceAdapter(db, clock);
        var states = new Dictionary<Guid, string>();
        var result = new Dictionary<Guid, NotificationPriorityFact>();
        foreach (var input in inputs)
        {
            var missing = input.Project is null || input.Occurrence is null || input.OccurrenceProject != input.Project ||
                input.OccurrenceSource != input.ScopeSource || input.OccurrenceKind != input.ScopeKind;
            if (missing)
            {
                result.Add(input.Id, new("UNKNOWN", "DEADLINE_SOURCE_INCOMPLETE", now));
                continue;
            }
            var high = false;
            var linked = candidates.Where(row => row.ProjectId == input.Project &&
                (input.ScopeKind == "DeadlineClock" ? row.Id == input.ScopeSource : row.OriginEventId == input.Origin)).ToArray();
            if (input.ScopeKind == "DeadlineClock" && linked.Length == 0) missing = true;
            foreach (var deadline in linked)
            {
                if (deadline.CompletedAt is not null) continue;
                if (!states.TryGetValue(deadline.Id, out var state))
                {
                    state = await ReadDeadlineStateAsync(deadline, adapter, token);
                    states.Add(deadline.Id, state);
                }
                if (state == "UNKNOWN") missing = true;
                if (state == "CURRENT" && deadline.IsOverdueAt(now)) high = true;
            }
            result.Add(input.Id, missing ? new("UNKNOWN", "DEADLINE_SOURCE_INCOMPLETE", now)
                : high ? new("HIGH", "MANDATORY_DEADLINE_OVERDUE", now)
                : new("NORMAL", "NO_CURRENT_OVERDUE_DEADLINE", now));
        }
        return result;
    }

    private async Task<string> ReadDeadlineStateAsync(DeadlineClock deadline, H6DeadlineNotificationSourceAdapter adapter, CancellationToken token)
    {
        if (deadline.Kind == DeadlineClockKind.ProjectManagerReview && await db.FieldInspectionTasks.AnyAsync(row =>
            row.Id == deadline.TargetId && row.ProjectId == deadline.ProjectId &&
            (row.Status == FieldInspectionTaskStatus.Cancelled || row.Status == FieldInspectionTaskStatus.Completed), token)) return "INACTIVE";
        if (deadline.Kind is DeadlineClockKind.SupervisorInitialApproval or DeadlineClockKind.SupervisorFinalConfirmation &&
            await db.RepairItems.AnyAsync(row => row.Id == deadline.TargetId && row.ProjectId == deadline.ProjectId &&
                (row.SupersededByItemId != null || row.State == RepairItemState.Cancelled ||
                    deadline.Kind == DeadlineClockKind.SupervisorFinalConfirmation && row.CurrentReviewId != null && row.CurrentReviewId != deadline.OriginEventId), token))
            return "INACTIVE";
        return (await adapter.ResolveDutyAsync(deadline, token)).Status == "VERIFIED" ? "CURRENT" : "UNKNOWN";
    }
}
