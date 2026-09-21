using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Surveys;

public interface ISurveyAssignmentRepository
{
    Task<SurveyAssignmentReassignmentFacts?> GetReassignmentFactsAsync(
        Guid surveyRequestId,
        CancellationToken cancellationToken = default);

    Task<SurveyAssignmentReassignmentResult> ReassignAsync(
        Guid surveyRequestId,
        Guid projectId,
        Guid expectedPreviousAssignmentId,
        SurveyAssignment replacementAssignment,
        Guid actorUserId,
        DateTimeOffset occurredAt,
        string reassignmentReason,
        string idempotencyKey,
        string requestFingerprint,
        CancellationToken cancellationToken = default);
}
