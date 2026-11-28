namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed class SurveyPlanOccurrence
{
    private SurveyPlanOccurrence() { }

    public Guid Id { get; private set; }
    public Guid SeriesPlanId { get; private set; }
    public Guid? SurveyPlanId { get; private set; }
    public DateTimeOffset PlannedStartAt { get; private set; }
    public DateTimeOffset PlannedEndAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static SurveyPlanOccurrence Create(Guid seriesPlanId, SurveyScheduleSlot slot, Guid? surveyPlanId)
        => new()
        {
            Id = Guid.NewGuid(),
            SeriesPlanId = seriesPlanId,
            SurveyPlanId = surveyPlanId,
            PlannedStartAt = slot.Start.ToUniversalTime(),
            PlannedEndAt = slot.End.ToUniversalTime()
        };

    public void LinkPlan(Guid planId) => SurveyPlanId = planId;

    public void Reschedule(DateTimeOffset start, DateTimeOffset end)
    {
        PlannedStartAt = start.ToUniversalTime();
        PlannedEndAt = end.ToUniversalTime();
    }
}
