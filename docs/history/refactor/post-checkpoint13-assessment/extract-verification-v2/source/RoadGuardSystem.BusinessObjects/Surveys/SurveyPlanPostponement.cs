namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed class SurveyPlanPostponement
{
    private SurveyPlanPostponement()
    {
    }

    public Guid Id { get; private set; }

    public Guid SurveyPlanId { get; private set; }

    public DateTimeOffset PostponedAt { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public DateTimeOffset? NewPlannedStartAt { get; private set; }

    public static SurveyPlanPostponement Create(
        Guid id,
        Guid surveyPlanId,
        DateTimeOffset postponedAt,
        string reason,
        DateTimeOffset? newPlannedStartAt)
    {
        if (id == Guid.Empty || surveyPlanId == Guid.Empty)
        {
            throw new ArgumentException("Postponement and survey plan ids must not be empty.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new SurveyPlanPostponement
        {
            Id = id,
            SurveyPlanId = surveyPlanId,
            PostponedAt = postponedAt.ToUniversalTime(),
            Reason = reason.Trim(),
            NewPlannedStartAt = newPlannedStartAt?.ToUniversalTime()
        };
    }
}
