using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Processing;

public sealed class ProcessingJob
{
    private ProcessingJob()
    {
    }

    public Guid Id { get; private set; }
    public Guid ProcessingBlockId { get; private set; }
    public Guid ModelVersionId { get; private set; }
    public ProcessingJobStatus Status { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static ProcessingJob Create(
        Guid id,
        Guid processingBlockId,
        Guid modelVersionId,
        ProcessingJobStatus status,
        DateTimeOffset? startedAt,
        DateTimeOffset? completedAt,
        string? errorCode,
        string? errorMessage)
    {
        if (id == Guid.Empty || processingBlockId == Guid.Empty || modelVersionId == Guid.Empty)
        {
            throw new ArgumentException("Processing job, block, and model ids must not be empty.");
        }

        if (!Enum.IsDefined(status) || status == ProcessingJobStatus.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Processing job status must be specified.");
        }

        var normalizedStartedAt = startedAt?.ToUniversalTime();
        var normalizedCompletedAt = completedAt?.ToUniversalTime();
        if (normalizedCompletedAt is not null && normalizedStartedAt is not null && normalizedCompletedAt < normalizedStartedAt)
        {
            throw new ArgumentException("Processing completion must not precede start.", nameof(completedAt));
        }

        return new ProcessingJob
        {
            Id = id,
            ProcessingBlockId = processingBlockId,
            ModelVersionId = modelVersionId,
            Status = status,
            StartedAt = normalizedStartedAt,
            CompletedAt = normalizedCompletedAt,
            ErrorCode = NormalizeOptional(errorCode, nameof(errorCode), 80),
            ErrorMessage = NormalizeOptional(errorMessage, nameof(errorMessage), 4000)
        };
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maxLength}.", parameterName);
        }

        return normalized;
    }
}
