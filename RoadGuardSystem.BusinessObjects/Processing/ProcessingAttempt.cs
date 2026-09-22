using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Processing;

public sealed class ProcessingAttempt
{
    private ProcessingAttempt()
    {
    }

    public Guid Id { get; private set; }
    public Guid ProcessingJobId { get; private set; }
    public int AttemptNo { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public ProcessingAttemptErrorType? ErrorType { get; private set; }
    public string? WorkerReference { get; private set; }

    public static ProcessingAttempt Create(
        Guid id,
        Guid processingJobId,
        int attemptNo,
        DateTimeOffset startedAt,
        DateTimeOffset? endedAt,
        ProcessingAttemptErrorType? errorType,
        string? workerReference)
    {
        if (id == Guid.Empty || processingJobId == Guid.Empty || attemptNo <= 0)
        {
            throw new ArgumentException("Processing attempt, job, and positive attempt number are required.");
        }

        if (errorType is { } definedErrorType &&
            (!Enum.IsDefined(definedErrorType) || definedErrorType == ProcessingAttemptErrorType.Unknown))
        {
            throw new ArgumentOutOfRangeException(nameof(errorType), "Processing attempt error type is invalid.");
        }

        var normalizedStartedAt = startedAt.ToUniversalTime();
        var normalizedEndedAt = endedAt?.ToUniversalTime();
        if (normalizedEndedAt is not null && normalizedEndedAt < normalizedStartedAt)
        {
            throw new ArgumentException("Processing attempt end must not precede start.", nameof(endedAt));
        }

        return new ProcessingAttempt
        {
            Id = id,
            ProcessingJobId = processingJobId,
            AttemptNo = attemptNo,
            StartedAt = normalizedStartedAt,
            EndedAt = normalizedEndedAt,
            ErrorType = errorType,
            WorkerReference = NormalizeWorkerReference(workerReference)
        };
    }

    private static string? NormalizeWorkerReference(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > 120)
        {
            throw new ArgumentException("Worker reference exceeds maximum length 120.", nameof(value));
        }

        return normalized;
    }
}
