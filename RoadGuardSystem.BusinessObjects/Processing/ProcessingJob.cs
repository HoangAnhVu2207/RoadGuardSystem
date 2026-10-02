using RoadGuardSystem.aBusinessObjects.Commons;
using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Processing;

public sealed class ProcessingJob
{
    private ProcessingJob()
    {
    }

    public Guid Id { get; private set; }
    public Guid ProcessingBlockId { get; private set; }
    public Guid ModelVersionId { get; private set; }
    public Guid ProjectId { get; private set; }
    public string ManifestHash { get; private set; } = string.Empty;
    public string ManifestJson { get; private set; } = "{}";
    public string Mode { get; private set; } = string.Empty;
    public byte[] RowVersion { get; private set; } = [];
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

    public static ProcessingJob CreateQueued(
        Guid id,
        Guid processingBlockId,
        Guid modelVersionId,
        Guid projectId,
        string manifestHash,
        string manifestJson,
        string mode)
    {
        if (projectId == Guid.Empty || !IsSha256(manifestHash) || mode is not ("MOCK" or "REAL"))
        {
            throw new ArgumentException("Processing job manifest values are invalid.");
        }

        var job = Create(id, processingBlockId, modelVersionId, ProcessingJobStatus.Queued, null, null, null, null);
        ValidateJsonObject(manifestJson, nameof(manifestJson));
        job.ProjectId = projectId;
        job.ManifestHash = manifestHash;
        job.ManifestJson = manifestJson.Trim();
        job.Mode = mode;
        return job;
    }

    public void Retry()
    {
        if (Status != ProcessingJobStatus.RetryableFailure)
        {
            throw new InvalidOperationException("Only retryable processing jobs can be retried.");
        }

        Status = ProcessingJobStatus.Queued;
        StartedAt = null;
        CompletedAt = null;
        ErrorCode = null;
        ErrorMessage = null;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (Status is not (ProcessingJobStatus.Queued or ProcessingJobStatus.Running))
        {
            throw new InvalidOperationException("Processing job cannot be completed from its current state.");
        }

        StartedAt ??= completedAt.ToUniversalTime();
        CompletedAt = completedAt.ToUniversalTime();
        Status = ProcessingJobStatus.Completed;
        ErrorCode = null;
        ErrorMessage = null;
    }

    // Terminal invalid-data/contract failure for the opt-in ANH-02 mock analysis.
    // Existing retry and legacy processing transitions remain unchanged.
    public void FailData(DateTimeOffset completedAt, string errorCode)
    {
        if (Mode != "MOCK" || Status is not (ProcessingJobStatus.Queued or ProcessingJobStatus.Running))
            throw new InvalidOperationException("Only pending mock analysis can fail terminally.");
        var code = NormalizeOptional(errorCode, nameof(errorCode), 80)
            ?? throw new ArgumentException("Failure code is required.", nameof(errorCode));
        var at = completedAt.ToUniversalTime();
        if (StartedAt is { } started && at < started) throw new ArgumentException("Failure must not precede start.", nameof(completedAt));
        StartedAt ??= at; CompletedAt = at; Status = ProcessingJobStatus.DataFailure;
        ErrorCode = code; ErrorMessage = null;
    }

    private static bool IsSha256(string value)
        => value is { Length: 64 } && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static void ValidateJsonObject(string value, string parameterName)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("Processing manifest must be a JSON object.", parameterName);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Processing manifest must be valid JSON.", parameterName, exception);
        }
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
