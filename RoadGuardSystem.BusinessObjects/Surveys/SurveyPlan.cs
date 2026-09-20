using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed class SurveyPlan
{
    private SurveyPlan()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid RoadSectionId { get; private set; }

    public DateTimeOffset PlannedStartAt { get; private set; }

    public DateTimeOffset PlannedEndAt { get; private set; }

    public SurveyType SurveyType { get; private set; }

    public SurveyPlanStatus Status { get; private set; }

    public static SurveyPlan Create(
        Guid id,
        Guid projectId,
        Guid roadSectionId,
        DateTimeOffset plannedStartAt,
        DateTimeOffset plannedEndAt,
        SurveyType surveyType,
        SurveyPlanStatus status)
    {
        if (id == Guid.Empty || projectId == Guid.Empty || roadSectionId == Guid.Empty)
        {
            throw new ArgumentException("Survey plan, project, and road section ids must not be empty.");
        }

        if (plannedEndAt < plannedStartAt)
        {
            throw new ArgumentException("Survey plan end time must not be before its start time.", nameof(plannedEndAt));
        }

        if (surveyType == SurveyType.Unknown)
        {
            throw new ArgumentException("Survey plan type must be specified.", nameof(surveyType));
        }

        if (status == SurveyPlanStatus.Unknown)
        {
            throw new ArgumentException("Survey plan status must be specified.", nameof(status));
        }

        return new SurveyPlan
        {
            Id = id,
            ProjectId = projectId,
            RoadSectionId = roadSectionId,
            PlannedStartAt = plannedStartAt.ToUniversalTime(),
            PlannedEndAt = plannedEndAt.ToUniversalTime(),
            SurveyType = surveyType,
            Status = status
        };
    }
}
