namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed record SurveyScheduleError(
    string Code,
    string Field,
    Guid? WarrantyId = null,
    DateOnly? WarrantyStartDate = null,
    DateOnly? WarrantyEndDate = null,
    DateTimeOffset? RequestedStartAt = null,
    DateTimeOffset? RequestedEndAt = null);
