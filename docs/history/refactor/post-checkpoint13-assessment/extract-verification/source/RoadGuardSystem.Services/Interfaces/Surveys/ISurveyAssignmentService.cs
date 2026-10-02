using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Surveys;

namespace RoadGuardSystem.Services.Surveys;

public interface ISurveyAssignmentService
{
    Task<SurveyAssignmentReassignmentResult> ReassignAsync(
        Guid surveyRequestId,
        SurveyAssignment replacementAssignment,
        Guid actorUserId,
        DateTimeOffset occurredAt,
        string reassignmentReason,
        string idempotencyKey,
        string requestFingerprint,
        CancellationToken cancellationToken = default);
}
