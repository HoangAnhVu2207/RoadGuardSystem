using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.API.Extensions;

internal static class SurveyScheduleProblemDetailsExtensions
{
    public static void AddSurveyScheduleDetails(this ProblemDetails problem, SurveyScheduleError error)
    {
        problem.Extensions["field"] = error.Field;
        if (error.WarrantyId is Guid warrantyId) problem.Extensions["warrantyId"] = warrantyId;
        if (error.WarrantyStartDate is DateOnly startDate) problem.Extensions["warrantyStartDate"] = startDate;
        if (error.WarrantyEndDate is DateOnly endDate) problem.Extensions["warrantyEndDate"] = endDate;
        if (error.RequestedStartAt is DateTimeOffset start) problem.Extensions["requestedStartAt"] = start;
        if (error.RequestedEndAt is DateTimeOffset end) problem.Extensions["requestedEndAt"] = end;
    }
}
