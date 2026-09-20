namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed class SurveyAssignment
{
    private SurveyAssignment()
    {
    }

    public Guid Id { get; private set; }

    public Guid SurveyRequestId { get; private set; }

    public Guid OperatorUserId { get; private set; }

    public Guid AssignedByUserId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    public DateTimeOffset? RejectedAt { get; private set; }

    public string? RejectionReason { get; private set; }

    public string? ReassignmentReason { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    public static SurveyAssignment Create(
        Guid id,
        Guid surveyRequestId,
        Guid operatorUserId,
        Guid assignedByUserId,
        DateTimeOffset assignedAt,
        DateTimeOffset? acceptedAt,
        DateTimeOffset? rejectedAt,
        string? rejectionReason,
        string? reassignmentReason,
        DateTimeOffset? endedAt)
    {
        if (id == Guid.Empty || surveyRequestId == Guid.Empty || operatorUserId == Guid.Empty || assignedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Assignment, request, operator, and assigner ids must not be empty.");
        }

        if (acceptedAt is not null && rejectedAt is not null)
        {
            throw new ArgumentException("An assignment cannot be accepted and rejected.");
        }

        var normalizedRejectionReason = NormalizeOptional(rejectionReason, nameof(rejectionReason));
        var normalizedReassignmentReason = NormalizeOptional(reassignmentReason, nameof(reassignmentReason));
        if ((rejectedAt is null) != (normalizedRejectionReason is null))
        {
            throw new ArgumentException("A rejection timestamp and reason must be supplied together.", nameof(rejectionReason));
        }

        if (rejectedAt is not null && normalizedReassignmentReason is not null)
        {
            throw new ArgumentException("Rejected assignments cannot carry a reassignment reason.", nameof(reassignmentReason));
        }

        if (endedAt is null && normalizedReassignmentReason is not null)
        {
            throw new ArgumentException("Active assignments cannot carry a reassignment reason.", nameof(reassignmentReason));
        }

        if (rejectedAt is not null && endedAt is null)
        {
            throw new ArgumentException("A rejected assignment must be ended.", nameof(endedAt));
        }

        if (acceptedAt is { } accepted && accepted < assignedAt ||
            rejectedAt is { } rejected && rejected < assignedAt ||
            endedAt is { } ended && ended < assignedAt)
        {
            throw new ArgumentException("Assignment timestamps cannot precede assignment.");
        }

        return new SurveyAssignment
        {
            Id = id,
            SurveyRequestId = surveyRequestId,
            OperatorUserId = operatorUserId,
            AssignedByUserId = assignedByUserId,
            AssignedAt = assignedAt.ToUniversalTime(),
            AcceptedAt = acceptedAt?.ToUniversalTime(),
            RejectedAt = rejectedAt?.ToUniversalTime(),
            RejectionReason = normalizedRejectionReason,
            ReassignmentReason = normalizedReassignmentReason,
            EndedAt = endedAt?.ToUniversalTime()
        };
    }

    public void EndForReassignment(DateTimeOffset endedAt, string reassignmentReason)
    {
        if (EndedAt is not null)
        {
            throw new InvalidOperationException("An ended assignment cannot be reassigned again.");
        }

        if (endedAt < AssignedAt)
        {
            throw new ArgumentException("Assignment end cannot precede assignment.", nameof(endedAt));
        }

        ReassignmentReason = NormalizeOptional(reassignmentReason, nameof(reassignmentReason))
            ?? throw new ArgumentException("Reassignment reason is required.", nameof(reassignmentReason));
        EndedAt = endedAt.ToUniversalTime();
    }

    public void Accept(DateTimeOffset acceptedAt)
    {
        if (EndedAt is not null || AcceptedAt is not null || RejectedAt is not null)
        {
            throw new InvalidOperationException("Only an active unaccepted assignment can be accepted.");
        }

        if (acceptedAt < AssignedAt)
        {
            throw new ArgumentException("Assignment acceptance cannot precede assignment.", nameof(acceptedAt));
        }

        AcceptedAt = acceptedAt.ToUniversalTime();
    }

    public void Reject(DateTimeOffset rejectedAt, string rejectionReason)
    {
        if (EndedAt is not null || AcceptedAt is not null || RejectedAt is not null)
        {
            throw new InvalidOperationException("Only an active unaccepted assignment can be rejected.");
        }

        if (rejectedAt < AssignedAt)
        {
            throw new ArgumentException("Assignment rejection cannot precede assignment.", nameof(rejectedAt));
        }

        RejectionReason = NormalizeOptional(rejectionReason, nameof(rejectionReason))
            ?? throw new ArgumentException("Rejection reason is required.", nameof(rejectionReason));
        RejectedAt = rejectedAt.ToUniversalTime();
        EndedAt = RejectedAt;
    }

    private static string? NormalizeOptional(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > 1_000)
        {
            throw new ArgumentException("Assignment reason exceeds maximum length 1000.", parameterName);
        }

        return normalized;
    }
}
