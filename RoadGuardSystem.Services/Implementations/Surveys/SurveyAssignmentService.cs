using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.Repositories.Surveys;

namespace RoadGuardSystem.Services.Surveys;

public sealed class SurveyAssignmentService : ISurveyAssignmentService
{
    private readonly ISurveyAssignmentRepository _repository;

    public SurveyAssignmentService(ISurveyAssignmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<SurveyAssignmentReassignmentResult> ReassignAsync(
        Guid surveyRequestId,
        SurveyAssignment replacementAssignment,
        Guid actorUserId,
        DateTimeOffset occurredAt,
        string reassignmentReason,
        string idempotencyKey,
        string requestFingerprint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(replacementAssignment);
        if (surveyRequestId == Guid.Empty || actorUserId == Guid.Empty || occurredAt == default ||
            string.IsNullOrWhiteSpace(reassignmentReason) || string.IsNullOrWhiteSpace(idempotencyKey) ||
            string.IsNullOrWhiteSpace(requestFingerprint))
        {
            throw new ArgumentException("Survey reassignment command is incomplete.");
        }

        if (replacementAssignment.SurveyRequestId != surveyRequestId ||
            replacementAssignment.AssignedByUserId != actorUserId ||
            replacementAssignment.EndedAt is not null)
        {
            throw new ArgumentException("Replacement assignment must be active and match the request and assigning actor.");
        }

        IdempotencyRecord.ValidateScope("SurveyRequestReassigned", idempotencyKey, requestFingerprint);
        var facts = await _repository.GetReassignmentFactsAsync(surveyRequestId, cancellationToken)
            ?? throw new InvalidOperationException("Survey request has no active assignment to reassign.");
        if (facts.ActiveAssignmentId == replacementAssignment.Id)
        {
            throw new ArgumentException("Replacement assignment must have a new identity.", nameof(replacementAssignment));
        }

        return await _repository.ReassignAsync(
            surveyRequestId,
            facts.ProjectId,
            facts.ActiveAssignmentId,
            replacementAssignment,
            actorUserId,
            occurredAt,
            reassignmentReason,
            idempotencyKey,
            requestFingerprint,
            cancellationToken);
    }
}
