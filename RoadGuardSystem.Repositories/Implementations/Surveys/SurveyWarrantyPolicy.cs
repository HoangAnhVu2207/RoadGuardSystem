using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Surveys;

internal static class SurveyWarrantyPolicy
{
    public static async Task<SurveyWarrantyWindow> ValidateAsync(
        RoadGuardDbContext context, Guid projectId, Guid roadSectionId, Guid? warrantyId,
        DateTimeOffset start, DateTimeOffset end, string field, CancellationToken token, bool historical = false)
    {
        if (start == default || end <= start)
            throw new SurveyScheduleException(new("survey_time_range_invalid", field,
                warrantyId, RequestedStartAt: start, RequestedEndAt: end));

        var warranties = await context.Warranties.AsNoTracking()
            .Where(w => w.ProjectId == projectId && (warrantyId == null || w.Id == warrantyId) &&
                (w.Status == WarrantyStatus.Active || w.Status == WarrantyStatus.Planned ||
                    (historical && w.Status == WarrantyStatus.Expired)) &&
                (w.Scope == WarrantyScope.Project ||
                    w.Scope == WarrantyScope.RoadSection && w.RoadSectionId == roadSectionId))
            .OrderBy(w => w.Id)
            .Select(w => new SurveyWarrantyWindow(w.Id, w.WarrantyStartDate, w.WarrantyEndDate))
            .ToListAsync(token);

        if (warranties.Count == 0)
            throw new SurveyScheduleException(new("survey_warranty_required", "warrantyId",
                warrantyId, RequestedStartAt: start, RequestedEndAt: end));

        var warranty = warranties[0];
        if (warranties.Any(w => w.StartDate != warranty.StartDate || w.EndDate != warranty.EndDate))
            throw new SurveyScheduleException(new("survey_warranty_selection_required", "warrantyId",
                RequestedStartAt: start, RequestedEndAt: end));

        if (!SurveyWarrantyCalendar.Contains(warranty, start, end))
            throw new SurveyScheduleException(new("survey_outside_warranty", field,
                warranty.Id, warranty.StartDate, warranty.EndDate, start, end));

        return warranty;
    }
}

internal sealed class SurveyScheduleException(SurveyScheduleError error) : Exception(error.Code)
{
    public SurveyScheduleError Error { get; } = error;
}
