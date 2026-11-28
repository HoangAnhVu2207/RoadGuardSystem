using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Implementations.Surveys;

public sealed partial class SurveyV2PersistenceService
{
    private async Task<Guid?> PinnedTaskWarrantyAsync(SurveyRequest task, CancellationToken token)
    {
        var planId = task.SurveyPlanId;
        var parentId = task.ParentTaskId;
        var visited = new HashSet<Guid> { task.Id };
        while (planId is null && parentId is Guid id && visited.Add(id))
        {
            var parent = await _context.SurveyRequests.AsNoTracking().Where(row => row.Id == id && row.ProjectId == task.ProjectId)
                .Select(row => new { row.SurveyPlanId, row.ParentTaskId }).SingleOrDefaultAsync(token);
            planId = parent?.SurveyPlanId;
            parentId = parent?.ParentTaskId;
        }

        return planId is null ? null : await _context.SurveyPlans.AsNoTracking()
            .Where(row => row.Id == planId && row.ProjectId == task.ProjectId)
            .Select(row => row.WarrantyId).SingleOrDefaultAsync(token);
    }

    private async Task ValidatePinnedTaskInstantAsync(
        SurveyRequest task, DateTimeOffset instant, string field, CancellationToken token, bool historical = false)
    {
        var warrantyId = await PinnedTaskWarrantyAsync(task, token);
        if (warrantyId is null) return;

        var roads = await (from scope in _context.SurveyRequestScopes
                           join version in _context.RoadSectionVersions on scope.RouteSectionVersionId equals version.Id
                           where scope.SurveyRequestId == task.Id
                           select version.RoadSectionId).Distinct().ToArrayAsync(token);
        await ValidateBandScheduleAsync(task.ProjectId, roads.Length > 0 ? roads : [task.RoadSectionId],
            warrantyId, instant, ScheduledEnd(instant, TimeSpan.FromTicks(1), field), token, field, historical);
    }

    private async Task<SurveyWarrantyWindow> ValidateBandScheduleAsync(
        Guid projectId, IEnumerable<Guid> roads, Guid? warrantyId,
        DateTimeOffset start, DateTimeOffset end, CancellationToken token,
        string field = "plannedAt", bool historical = false)
    {
        SurveyWarrantyWindow? selected = null;
        foreach (var road in roads.Distinct())
        {
            var warranty = await SurveyWarrantyPolicy.ValidateAsync(_context, projectId, road, warrantyId,
                start, end, field, token, historical);
            if (selected is not null && selected.Id != warranty.Id)
                throw new SurveyScheduleException(new("survey_warranty_selection_required", "warrantyId",
                    RequestedStartAt: start, RequestedEndAt: end));
            selected = warranty;
        }

        return selected ?? throw new ScopeConflictException();
    }

    private static DateTimeOffset ScheduledEnd(DateTimeOffset start, TimeSpan duration, string field)
    {
        if (start == default || duration <= TimeSpan.Zero || start.UtcTicks > DateTime.MaxValue.Ticks - duration.Ticks)
            throw new SurveyScheduleException(new("survey_time_range_invalid", field, RequestedStartAt: start));
        return start.ToUniversalTime().Add(duration);
    }

    private static SurveyV2OccurrenceView ToOccurrenceView(SurveyPlanOccurrence occurrence, SurveyPlanStatus? status)
        => new(occurrence.Id, occurrence.PlannedStartAt, occurrence.PlannedEndAt,
            status?.ToString() ?? "Scheduled", occurrence.SurveyPlanId);

    private async Task<IReadOnlyList<SurveyV2OccurrenceView>?> ReadOccurrencesAsync(Guid planId, CancellationToken token)
    {
        var rows = await (from occurrence in _context.SurveyPlanOccurrences.AsNoTracking()
                          join plan in _context.SurveyPlans.AsNoTracking() on occurrence.SurveyPlanId equals (Guid?)plan.Id into plans
                          from plan in plans.DefaultIfEmpty()
                          where occurrence.SeriesPlanId == planId
                          orderby occurrence.PlannedStartAt
                          select new { occurrence, Status = (SurveyPlanStatus?)plan.Status }).ToListAsync(token);
        return rows.Count == 0 ? null : rows.Select(row => ToOccurrenceView(row.occurrence, row.Status)).ToArray();
    }

    private async Task<SurveyPlan> ResolveOccurrencePlanAsync(SurveyPlan series, Guid occurrenceId, CancellationToken token)
    {
        var occurrence = await _context.SurveyPlanOccurrences.SingleOrDefaultAsync(
            row => row.SeriesPlanId == series.Id && row.Id == occurrenceId, token) ?? throw new PlanMismatchException();
        if (occurrence.SurveyPlanId is Guid existingPlanId)
            return existingPlanId == series.Id ? series : await _context.SurveyPlans.SingleAsync(row => row.Id == existingPlanId, token);

        var scopeIsBusy = await _context.SurveyPlans.AnyAsync(plan =>
            plan.ProjectId == series.ProjectId && plan.RoadSectionId == series.RoadSectionId &&
            plan.SurveyType == series.SurveyType && (plan.Status == SurveyPlanStatus.Planned ||
                plan.Status == SurveyPlanStatus.InProgress || plan.Status == SurveyPlanStatus.Postponed), token);
        if (scopeIsBusy)
            throw new SurveyScheduleException(new("survey_schedule_conflict", "occurrenceId", series.WarrantyId));

        var plan = SurveyPlan.Create(Guid.NewGuid(), series.ProjectId, series.RoadSectionId, occurrence.PlannedStartAt,
            occurrence.PlannedEndAt, series.SurveyType, SurveyPlanStatus.Planned, series.OutputRequirements, series.RoadSectionVersionId);
        plan.SetBandScope();
        if (series.WarrantyId is Guid warrantyId)
            plan.SetWarranty(warrantyId);
        occurrence.LinkPlan(plan.Id);
        _context.SurveyPlans.Add(plan);

        var scopes = await _context.SurveyPlanScopes.AsNoTracking().Where(row => row.SurveyPlanId == series.Id).ToListAsync(token);
        _context.SurveyPlanScopes.AddRange(scopes.Select(scope => SurveyPlanScope.Create(
            Guid.NewGuid(), plan.Id, scope.RouteSectionVersionId, scope.SegmentSetId, scope.SegmentIdsJson, scope.TargetBand)));
        return plan;
    }
}
