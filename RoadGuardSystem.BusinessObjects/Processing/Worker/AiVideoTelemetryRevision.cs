using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Processing.Worker;

/// <summary>Append-only video telemetry evidence; never a measured ground defect position.</summary>
public sealed class AiVideoTelemetryRevision
{
    private AiVideoTelemetryRevision() { }
    public Guid Id { get; private set; }
    public Guid WorkId { get; private set; }
    public int Number { get; private set; }
    public Guid? PreviousId { get; private set; }
    public Guid ActorId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string SourceVideoHash { get; private set; } = "";
    public string Kind { get; private set; } = "";
    public string IntervalsJson { get; private set; } = "";
    public string? OriginalSrtObjectKey { get; private set; }
    public string? OriginalSrtSha256 { get; private set; }
    public long? OriginalSrtBytes { get; private set; }
    public long OffsetMs { get; private set; }
    public string? Note { get; private set; }
    public string IdempotencyKeyHash { get; private set; } = "";
    public string PayloadFingerprint { get; private set; } = "";

    public static AiVideoTelemetryRevision Create(Guid id, Guid workId, int number, Guid? previousId, Guid actorId,
        DateTimeOffset createdAt, string sourceVideoHash, string kind, string intervalsJson, long durationMs,
        string? originalSrtObjectKey, string? originalSrtSha256, long? originalSrtBytes, long offsetMs,
        string? note, string idempotencyKeyHash, string payloadFingerprint)
    {
        if (id == Guid.Empty || workId == Guid.Empty || actorId == Guid.Empty || number <= 0 ||
            (number == 1 ? previousId is not null : previousId is null || previousId == Guid.Empty) ||
            !Hash(sourceVideoHash) || !Hash(idempotencyKeyHash) || !Hash(payloadFingerprint) || durationMs <= 0 ||
            offsetMs is < -86_400_000 or > 86_400_000 || note?.Length > 1000 || note?.Any(char.IsControl) == true)
            throw new ArgumentException("Invalid telemetry revision metadata.");
        if (kind == "EXTERNAL_SRT")
        {
            if (string.IsNullOrWhiteSpace(originalSrtObjectKey) || originalSrtObjectKey.Length > 512 ||
                originalSrtObjectKey.StartsWith('/') || originalSrtObjectKey.Contains('\\') || originalSrtObjectKey.Contains(':') ||
                originalSrtObjectKey.Split('/').Any(x => x is "" or "." or "..") || !Hash(originalSrtSha256) || originalSrtBytes is not (> 0 and <= 2_097_152))
                throw new ArgumentException("Invalid external telemetry evidence.");
        }
        else if (kind != "OPERATOR_ESTIMATE" || originalSrtObjectKey is not null || originalSrtSha256 is not null || originalSrtBytes is not null || string.IsNullOrWhiteSpace(note) || offsetMs != 0)
            throw new ArgumentException("Invalid operator estimate evidence.");
        if (intervalsJson.Length > 4_194_304) throw new ArgumentException("Telemetry intervals exceed bounds.");
        using var doc = JsonDocument.Parse(intervalsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() is < 1 or > 100_000)
            throw new ArgumentException("Telemetry intervals are required.");
        long previousEnd = 0;
        foreach (var interval in doc.RootElement.EnumerateArray())
        {
            if (interval.ValueKind != JsonValueKind.Object || interval.EnumerateObject().Count() != 4 ||
                !interval.TryGetProperty("startMs", out var start) || !start.TryGetInt64(out var s) ||
                !interval.TryGetProperty("endMs", out var end) || !end.TryGetInt64(out var e) ||
                !interval.TryGetProperty("latitude", out var lat) || !lat.TryGetDecimal(out var latitude) ||
                !interval.TryGetProperty("longitude", out var lon) || !lon.TryGetDecimal(out var longitude) ||
                s < previousEnd || e <= s || e > durationMs || latitude is < -90 or > 90 || longitude is < -180 or > 180)
                throw new ArgumentException("Invalid telemetry interval geometry or timing.");
            previousEnd = e;
        }
        return new()
        {
            Id = id,
            WorkId = workId,
            Number = number,
            PreviousId = previousId,
            ActorId = actorId,
            CreatedAt = createdAt.ToUniversalTime(),
            SourceVideoHash = sourceVideoHash,
            Kind = kind,
            IntervalsJson = intervalsJson,
            OriginalSrtObjectKey = originalSrtObjectKey,
            OriginalSrtSha256 = originalSrtSha256,
            OriginalSrtBytes = originalSrtBytes,
            OffsetMs = offsetMs,
            Note = note,
            IdempotencyKeyHash = idempotencyKeyHash,
            PayloadFingerprint = payloadFingerprint
        };
    }
    private static bool Hash(string? value) => value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
