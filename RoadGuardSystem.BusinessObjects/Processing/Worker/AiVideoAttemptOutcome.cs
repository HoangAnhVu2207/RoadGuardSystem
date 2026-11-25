namespace RoadGuardSystem.BusinessObjects.Processing.Worker;

/// <summary>One immutable terminal transport fact per REAL attempt, retained across retry.</summary>
public sealed class AiVideoAttemptOutcome
{
    private AiVideoAttemptOutcome() { }
    public Guid AttemptId { get; private set; }
    public Guid WorkId { get; private set; }
    public Guid ProcessingJobId { get; private set; }
    public AiVideoWorkState State { get; private set; }
    public string? ErrorCode { get; private set; }
    public string AdmissionHash { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }

    public static AiVideoAttemptOutcome Create(Guid attemptId, Guid workId, Guid processingJobId,
        AiVideoWorkState state, string? errorCode, string admissionHash, DateTimeOffset occurredAt)
    {
        if (attemptId == Guid.Empty || workId == Guid.Empty || processingJobId == Guid.Empty ||
            admissionHash is not { Length: 64 } || admissionHash.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')) ||
            occurredAt == default)
            throw new ArgumentException("Terminal outcome requires immutable identities, SHA256 and timestamp.");
        var failure = state is AiVideoWorkState.RetryableFailed or AiVideoWorkState.TerminalFailed;
        if (!failure && state is not (AiVideoWorkState.Ingested or AiVideoWorkState.Cancelled) ||
            (failure ? string.IsNullOrEmpty(errorCode) || errorCode.Length > 80 || errorCode.Any(c => c is not (>= 'A' and <= 'Z' or >= '0' and <= '9' or '_')) : errorCode is not null))
            throw new ArgumentException("Terminal outcome state and safe error code must agree.");
        return new()
        {
            AttemptId = attemptId,
            WorkId = workId,
            ProcessingJobId = processingJobId,
            State = state,
            ErrorCode = errorCode,
            AdmissionHash = admissionHash,
            OccurredAt = occurredAt.ToUniversalTime()
        };
    }
}
