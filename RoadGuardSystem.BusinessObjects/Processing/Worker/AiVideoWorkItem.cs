using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Processing.Worker;

public enum AiVideoWorkState : byte
{
    Pending = 1, Admitted = 2, Running = 3, ArtifactsReady = 4,
    Ingested = 5, RetryableFailed = 6, TerminalFailed = 7, Cancelled = 8
}

/// <summary>Mutable transport ledger; ProcessingAttempt remains an append-only business fact.</summary>
public sealed class AiVideoWorkItem
{
    private AiVideoWorkItem() { }

    public Guid Id { get; private set; }
    public Guid DatasetId { get; private set; }
    public Guid SourceVideoFileId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid ProcessingJobId { get; private set; }
    public Guid CurrentAttemptId { get; private set; }
    public string LogicalConfigurationIdentity { get; private set; } = string.Empty;
    public string DispatchKey { get; private set; } = string.Empty;
    public string AdmissionJson { get; private set; } = "{}";
    public AiVideoWorkState State { get; private set; }
    public string? LeaseOwner { get; private set; }
    public DateTimeOffset? LeaseUntil { get; private set; }
    public DateTimeOffset NextPollAt { get; private set; }
    public string? ErrorCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static AiVideoWorkItem Create(Guid id, Guid datasetId, Guid sourceVideoFileId,
        Guid projectId, Guid processingJobId, Guid currentAttemptId,
        string logicalConfigurationIdentity, string admissionJson, DateTimeOffset now)
    {
        if (new[] { id, datasetId, sourceVideoFileId, projectId, processingJobId, currentAttemptId }.Contains(Guid.Empty))
            throw new ArgumentException("All work item identities are required.");
        var config = Bounded(logicalConfigurationIdentity, 2048);
        var identity = JsonSerializer.Serialize(new[] { datasetId.ToString("D"), sourceVideoFileId.ToString("D"), config });
        return new AiVideoWorkItem
        {
            Id = id,
            DatasetId = datasetId,
            SourceVideoFileId = sourceVideoFileId,
            ProjectId = projectId,
            ProcessingJobId = processingJobId,
            CurrentAttemptId = currentAttemptId,
            LogicalConfigurationIdentity = config,
            DispatchKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))).ToLowerInvariant(),
            AdmissionJson = CanonicalAdmission(admissionJson),
            State = AiVideoWorkState.Pending,
            CreatedAt = now.ToUniversalTime(),
            UpdatedAt = now.ToUniversalTime(),
            NextPollAt = now.ToUniversalTime()
        };
    }

    public void AcquireLease(string owner, DateTimeOffset now, DateTimeOffset until)
    {
        var normalized = Bounded(owner, 120);
        if (!IsActive() || until <= now || now < UpdatedAt || NextPollAt > now ||
            (LeaseUntil > now && LeaseOwner != normalized))
            throw new InvalidOperationException("Work item is not available for this lease.");
        LeaseOwner = normalized; LeaseUntil = until.ToUniversalTime(); UpdatedAt = now.ToUniversalTime();
    }

    public void Transition(string owner, Guid attemptId, AiVideoWorkState target, DateTimeOffset now,
        DateTimeOffset? nextPollAt = null, string? errorCode = null)
    {
        Fence(owner, attemptId, now);
        var failure = target is AiVideoWorkState.RetryableFailed or AiVideoWorkState.TerminalFailed;
        var permitted = IsActive() && (failure || target == AiVideoWorkState.Cancelled ||
            (State, target) is (AiVideoWorkState.Pending, AiVideoWorkState.Admitted)
                or (AiVideoWorkState.Admitted, AiVideoWorkState.Running)
                or (AiVideoWorkState.Admitted, AiVideoWorkState.ArtifactsReady)
                or (AiVideoWorkState.Running, AiVideoWorkState.ArtifactsReady)
                or (AiVideoWorkState.ArtifactsReady, AiVideoWorkState.Ingested));
        if (!permitted) throw new InvalidOperationException("Invalid work item transition.");
        var code = failure ? Bounded(errorCode, 80) : null;
        if (!failure && errorCode is not null) throw new ArgumentException("Only failures carry an error code.", nameof(errorCode));
        ValidatePoll(now, nextPollAt);
        State = target; ErrorCode = code; UpdatedAt = now.ToUniversalTime();
        NextPollAt = (nextPollAt ?? now).ToUniversalTime();
        if (!IsActive()) { LeaseOwner = null; LeaseUntil = null; }
    }

    public void ReleaseLease(string owner, Guid attemptId, DateTimeOffset now, DateTimeOffset? nextPollAt = null)
    {
        Fence(owner, attemptId, now); ValidatePoll(now, nextPollAt);
        LeaseOwner = null; LeaseUntil = null; UpdatedAt = now.ToUniversalTime();
        NextPollAt = (nextPollAt ?? now).ToUniversalTime();
    }

    public void AdvanceAttempt(Guid newAttemptId, string newAdmissionJson, DateTimeOffset now)
    {
        if (State != AiVideoWorkState.RetryableFailed || now < UpdatedAt)
            throw new InvalidOperationException("Only explicitly retryable failed work can advance an attempt.");
        if (newAttemptId == Guid.Empty || newAttemptId == CurrentAttemptId)
            throw new ArgumentException("Retry requires a new attempt identity.", nameof(newAttemptId));
        var admission = CanonicalAdmission(newAdmissionJson);
        CurrentAttemptId = newAttemptId; AdmissionJson = admission; State = AiVideoWorkState.Pending;
        LeaseOwner = null; LeaseUntil = null; ErrorCode = null;
        NextPollAt = now.ToUniversalTime(); UpdatedAt = now.ToUniversalTime();
    }

    private bool IsActive() => State is AiVideoWorkState.Pending or AiVideoWorkState.Admitted or AiVideoWorkState.Running or AiVideoWorkState.ArtifactsReady;
    private void Fence(string owner, Guid attemptId, DateTimeOffset now)
    {
        if (CurrentAttemptId != attemptId || LeaseOwner != owner || LeaseUntil is null || LeaseUntil <= now || now < UpdatedAt)
            throw new InvalidOperationException("Expired lease, stale attempt, or incorrect lease owner.");
    }
    private static void ValidatePoll(DateTimeOffset now, DateTimeOffset? poll)
    {
        if (poll < now) throw new ArgumentException("Next poll must not be in the past.", nameof(poll));
    }
    private static string Bounded(string? value, int length)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > length || value != value.Trim())
            throw new ArgumentException("Value must be nonempty, bounded and have no surrounding whitespace.");
        return value;
    }
    private static string CanonicalAdmission(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 262144) throw new ArgumentException("Admission payload is missing or oversized.");
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("Admission must be an object.");
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream)) WriteCanonical(writer, document.RootElement);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException exception) { throw new ArgumentException("Admission must be valid JSON.", nameof(value), exception); }
    }
    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject(); var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                if (!names.Add(property.Name)) throw new ArgumentException("Duplicate admission property.");
                writer.WritePropertyName(property.Name); WriteCanonical(writer, property.Value);
            }
            writer.WriteEndObject();
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray(); foreach (var child in element.EnumerateArray()) WriteCanonical(writer, child); writer.WriteEndArray();
        }
        else element.WriteTo(writer);
    }
}
