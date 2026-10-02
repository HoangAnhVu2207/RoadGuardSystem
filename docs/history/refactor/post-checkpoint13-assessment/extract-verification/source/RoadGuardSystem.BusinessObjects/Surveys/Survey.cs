using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed class Survey
{
    private Survey()
    {
    }

    public Guid Id { get; private set; }

    public Guid? SurveyRequestId { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid RoadSectionVersionId { get; private set; }

    public SurveyType SurveyType { get; private set; }

    public bool IsBaselineConfirmed { get; private set; }

    public Guid? BaselineConfirmedByUserId { get; private set; }

    public DateTimeOffset? BaselineConfirmedAt { get; private set; }

    public SurveyStatus Status { get; private set; }

    public static Survey Create(
        Guid id,
        Guid? surveyRequestId,
        Guid projectId,
        Guid roadSectionVersionId,
        SurveyType surveyType,
        SurveyStatus status,
        bool isBaselineConfirmed,
        Guid? baselineConfirmedByUserId,
        DateTimeOffset? baselineConfirmedAt)
    {
        if (id == Guid.Empty || projectId == Guid.Empty || roadSectionVersionId == Guid.Empty)
        {
            throw new ArgumentException("Survey, project, and road section version ids must not be empty.");
        }

        if (surveyType == SurveyType.Unknown)
        {
            throw new ArgumentException("Survey type must be specified.", nameof(surveyType));
        }

        if (status == SurveyStatus.Unknown)
        {
            throw new ArgumentException("Survey status must be specified.", nameof(status));
        }

        if (isBaselineConfirmed != (baselineConfirmedByUserId is not null && baselineConfirmedAt is not null))
        {
            throw new ArgumentException(
                "Baseline confirmation flag, actor, and timestamp must be supplied together.",
                nameof(isBaselineConfirmed));
        }

        return new Survey
        {
            Id = id,
            SurveyRequestId = surveyRequestId,
            ProjectId = projectId,
            RoadSectionVersionId = roadSectionVersionId,
            SurveyType = surveyType,
            Status = status,
            IsBaselineConfirmed = isBaselineConfirmed,
            BaselineConfirmedByUserId = baselineConfirmedByUserId,
            BaselineConfirmedAt = baselineConfirmedAt?.ToUniversalTime()
        };
    }
}
