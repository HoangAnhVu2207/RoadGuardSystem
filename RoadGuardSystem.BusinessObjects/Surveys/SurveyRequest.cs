using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed class SurveyRequest
{
    private SurveyRequest()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid RoadSectionId { get; private set; }

    public Guid? SurveyPlanId { get; private set; }

    public Guid RequestedByUserId { get; private set; }

    public SurveyType SurveyType { get; private set; }

    public SurveyRequestStatus Status { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public string? CancellationReason { get; private set; }

    public static SurveyRequest Create(
        Guid id,
        Guid projectId,
        Guid roadSectionId,
        Guid? surveyPlanId,
        Guid requestedByUserId,
        SurveyType surveyType,
        SurveyRequestStatus status,
        DateTimeOffset requestedAt)
    {
        if (id == Guid.Empty || projectId == Guid.Empty || roadSectionId == Guid.Empty || requestedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Survey request, project, road section, and requester ids must not be empty.");
        }

        if (surveyType == SurveyType.Unknown)
        {
            throw new ArgumentException("Survey request type must be specified.", nameof(surveyType));
        }

        if (status == SurveyRequestStatus.Unknown)
        {
            throw new ArgumentException("Survey request status must be specified.", nameof(status));
        }

        return new SurveyRequest
        {
            Id = id,
            ProjectId = projectId,
            RoadSectionId = roadSectionId,
            SurveyPlanId = surveyPlanId,
            RequestedByUserId = requestedByUserId,
            SurveyType = surveyType,
            Status = status,
            RequestedAt = requestedAt.ToUniversalTime()
        };
    }
}
