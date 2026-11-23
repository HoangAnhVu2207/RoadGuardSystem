using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Repositories.Storage;

namespace RoadGuardSystem.Services.Processing.Worker;

public sealed record AiTelemetryAppendRequest(string Kind, string? SrtText, long OffsetMs,
    AiDroneInterval? Estimate, string? Reason, string? CorrectionReason);
public sealed record AiTelemetryRevisionView(Guid Id, int Number, Guid? PreviousId, string Kind, Guid ActorId, DateTimeOffset CreatedAt, string? Note);
public sealed record AiTelemetryView(Guid WorkId, Guid AttemptId, Guid SourceVideoFileId, Guid HeadRevisionId,
    string Status, string PositionMeaning, IReadOnlyList<AiDroneInterval> Intervals, IReadOnlyList<AiTelemetryRevisionView> Revisions,
    string MeasuredTelemetryStatus = "MISSING_TELEMETRY");

/// <summary>Small telemetry-only workflow. Revisions do not re-run inference or change candidate/PM/crew facts.</summary>
public sealed class AiVideoTelemetryService(IAiVideoTelemetryRepository repository, IAnh02ArtifactStore artifacts, IUploadObjectStorage? uploads = null)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task<AiTelemetryView> GetAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid workId, CancellationToken ct)
    {
        var scope = await repository.ReadScopeAsync(actorId, role, projectId, workId, ct);
        var revisions = await repository.ListAsync(actorId, role, projectId, workId, ct);
        // A head observed in another transaction must never be combined with an inconsistent revision list.
        if ((revisions.Count == 0 ? Guid.Empty : revisions[^1].Id) != scope.HeadRevisionId) throw new AiRequestException(409, "telemetry_changed_retry_read");
        var measured = revisions.LastOrDefault(x => x.Kind == "EXTERNAL_SRT");
        var paired = measured is null && scope.PairedSrt is { } pair ? await ReadPairedAsync(pair, scope.DurationMs, ct) : null;
        IReadOnlyList<AiDroneInterval>? intervals = measured is not null ? Decode(measured.IntervalsJson) : paired ?? await EmbeddedAsync(scope, ct);
        var status = intervals is null ? "MISSING_TELEMETRY" : measured is not null ? "EXTERNAL_SRT" : paired is not null ? "PAIRED_SRT" : "EMBEDDED_SRT";
        var measuredStatus = status;
        if (intervals is null && revisions.LastOrDefault(x => x.Kind == "OPERATOR_ESTIMATE") is { } estimate)
        { intervals = Decode(estimate.IntervalsJson); status = "OPERATOR_ESTIMATE"; }
        return new(workId, scope.CurrentAttemptId, scope.SourceVideoFileId, scope.HeadRevisionId, status, "DRONE_REFERENCE_NOT_GROUND_LOCATION",
            intervals ?? [], revisions.Select(x => new AiTelemetryRevisionView(x.Id, x.Number, x.PreviousId, x.Kind, x.ActorId, x.CreatedAt, x.Note)).ToArray(), measuredStatus);
    }

    public async Task<Guid> AppendAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid workId,
        AiTelemetryAppendRequest request, string key, Guid expectedHeadId, CancellationToken ct)
    {
        if (role != UserRoleCode.DroneOperator) throw new AiRequestException(403, "access_forbidden");
        if (string.IsNullOrWhiteSpace(key) || key.Length > 200 || key.Any(char.IsControl)) throw new AiRequestException(422, "invalid_idempotency_key");
        var scope = await repository.ReadScopeAsync(actorId, role, projectId, workId, ct);
        IReadOnlyList<AiDroneInterval> intervals;
        byte[]? bytes = null;
        try
        {
            if (request.Kind == "EXTERNAL_SRT" && request.SrtText is not null && request.Estimate is null && request.Reason is null)
            {
                intervals = AiVideoTelemetry.ParseSrt(request.SrtText, scope.DurationMs, request.OffsetMs);
                bytes = new UTF8Encoding(false, true).GetBytes(request.SrtText);
            }
            else if (request.Kind == "OPERATOR_ESTIMATE" && request.Estimate is { } e && request.SrtText is null && request.OffsetMs == 0)
                intervals = [AiVideoTelemetry.Estimate(e.StartMs, e.EndMs, e.Latitude, e.Longitude, request.Reason ?? "", scope.DurationMs)];
            else throw new ArgumentException("Mixed or unsupported telemetry input.");
            if (request.CorrectionReason is { } reason && (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000 || reason.Any(char.IsControl))) throw new ArgumentException("Invalid correction reason.");
            if (expectedHeadId != Guid.Empty && string.IsNullOrWhiteSpace(request.CorrectionReason)) throw new ArgumentException("A correction requires its reason.");
        }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
        { throw new AiRequestException(422, "invalid_video_telemetry"); }
        var embedded = await EmbeddedAsync(scope, ct);
        var paired = scope.PairedSrt is { } pair ? await ReadPairedAsync(pair, scope.DurationMs, ct) : null;
        if (expectedHeadId == Guid.Empty && (paired is not null || embedded is not null)) throw new AiRequestException(409, "measured_telemetry_already_available");
        var intervalsJson = JsonSerializer.Serialize(intervals, Json);
        var fingerprint = Hash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { scope.SourceVideoHash, request.Kind, intervalsJson,
            request.OffsetMs, request.Reason, request.CorrectionReason, srtSha256 = bytes is null ? null : Hash(bytes) }, Json)));
        string? objectKey = null; string? srtHash = null;
        if (bytes is not null)
        {
            srtHash = Hash(bytes); objectKey = $"anh02/real/telemetry/{workId:D}/{fingerprint}/{srtHash}.srt";
            await using var input = new MemoryStream(bytes, false);
            var saved = await artifacts.WriteAsync(objectKey, input, bytes.Length, "application/x-subrip", ct);
            if (saved.Key != objectKey || saved.Sha256 != srtHash || saved.SizeBytes != bytes.Length) throw new AiRequestException(409, "telemetry_artifact_mismatch");
        }
        var appended = await repository.AppendAsync(actorId, role, projectId, workId,
            new(Guid.NewGuid(), expectedHeadId, scope.CurrentAttemptId, scope.SourceVideoHash, request.Kind, intervalsJson,
                objectKey, srtHash, bytes?.LongLength, request.OffsetMs, request.CorrectionReason ?? request.Reason,
                Hash(Encoding.UTF8.GetBytes(key)), fingerprint, scope.EmbeddedSrt?.FileId, paired is not null || embedded is not null), ct);
        return appended.Id;
    }

    private async Task<IReadOnlyList<AiDroneInterval>?> EmbeddedAsync(AiVideoTelemetryScope scope, CancellationToken ct)
    {
        return scope.EmbeddedSrt is { } file ? await ReadSrtAsync(file, scope.DurationMs, 0, ct) : null;
    }
    private async Task<IReadOnlyList<AiDroneInterval>?> ReadPairedAsync(AiPairedTelemetryFile pair, long durationMs, CancellationToken ct)
    {
        if (pair.File.Bytes is <= 0 or > AiVideoTelemetry.MaximumSrtBytes) return null;
        if (uploads is null) throw new AiRequestException(503, "telemetry_storage_unavailable");
        // Uploaded survey files do not carry generated-artifact S3 metadata.
        // Their committed upload proof is checked in SQL; verify actual bytes here.
        await using var stream = await uploads.OpenReadAsync(pair.File.ObjectKey, ct);
        var bytes = new byte[checked((int)pair.File.Bytes)];
        try { await stream.ReadExactlyAsync(bytes, ct); }
        catch (EndOfStreamException) { throw new AiRequestException(409, "telemetry_artifact_mismatch"); }
        if (await stream.ReadAsync(new byte[1], ct) != 0 || Hash(bytes) != pair.File.Sha256) throw new AiRequestException(409, "telemetry_artifact_mismatch");
        try { return AiVideoTelemetry.ParseSrt(new UTF8Encoding(false, true).GetString(bytes), durationMs, pair.OffsetMs); }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException) { return null; }
    }
    private async Task<IReadOnlyList<AiDroneInterval>?> ReadSrtAsync(AiEmbeddedTelemetryFile file, long durationMs, long offsetMs, CancellationToken ct)
    {
        if (file.Bytes is <= 0 or > AiVideoTelemetry.MaximumSrtBytes) return null;
        await using var read = await artifacts.OpenReadAsync(file.ObjectKey, ct);
        var bytes = new byte[checked((int)file.Bytes)];
        try { await read.Content.ReadExactlyAsync(bytes, ct); }
        catch (EndOfStreamException) { throw new AiRequestException(409, "telemetry_artifact_mismatch"); }
        if (await read.Content.ReadAsync(new byte[1], ct) != 0 || Hash(bytes) != file.Sha256 || read.Metadata.SizeBytes != file.Bytes || read.Metadata.Sha256 != file.Sha256)
            throw new AiRequestException(409, "telemetry_artifact_mismatch");
        try { return AiVideoTelemetry.ParseSrt(new UTF8Encoding(false, true).GetString(bytes), durationMs, offsetMs); }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException) { return null; }
    }
    private static AiDroneInterval[] Decode(string json) => JsonSerializer.Deserialize<AiDroneInterval[]>(json, Json) ?? [];
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
