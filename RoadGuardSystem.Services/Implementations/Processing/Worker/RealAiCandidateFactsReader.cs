using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Services.Integration;

namespace RoadGuardSystem.Services.Processing.Worker;

public sealed class RealAiCandidateFactsReader(IRealAiCandidateRepository repository, IAnh02AiRepository authorization,
    IAnhHuyProducerService geometry) : IAiCandidateFactsReader
{
    private static readonly JsonSerializerOptions SnapshotJson = new() { PropertyNameCaseInsensitive = true };

    public async Task<RealAiDetectionPage> ListAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid jobId,
        int offset, int limit, CancellationToken ct)
    {
        if (projectId == Guid.Empty || jobId == Guid.Empty || offset < 0 || limit is < 1 or > 100)
            throw new AiRequestException(422, "invalid_detection_query");
        await authorization.AuthorizeAsync(actorId, role, projectId, false, ct);
        return await repository.ListAsync(projectId, jobId, offset, limit, ct);
    }

    public async Task<AnhHuyProducerResult<AiCandidateFactsV1>> ResolveAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid detectionId,
        string? expectedSourceVersion = null, string? expectedGeometryVersion = null, string? expectedDispositionVersion = null,
        CancellationToken cancellationToken = default)
    {
        try { await authorization.AuthorizeAsync(actorId, role, projectId, false, cancellationToken); }
        catch (AiRequestException) { return new(AnhHuyProducerStatus.Forbidden); }
        var f = await repository.ReadAsync(projectId, detectionId, cancellationToken);
        if (f is null) return new(await repository.DetectionProjectAsync(detectionId, cancellationToken) == projectId
            ? AnhHuyProducerStatus.SourceNotReady : AnhHuyProducerStatus.NotFound);
        AiCandidateFactsV1 facts;
        try { facts = Validate(f, projectId, detectionId); }
        catch (Exception e) when (e is JsonException or InvalidDataException or InvalidOperationException or FormatException or KeyNotFoundException or OverflowException)
        { return new(AnhHuyProducerStatus.SourceNotReady); }
        if (expectedGeometryVersion is not null && expectedGeometryVersion != facts.GeometryVersion) return new(AnhHuyProducerStatus.StaleGeometry);
        var context = await geometry.ResolveGeometryAsync(actorId, role, projectId, facts.RouteVersionId, facts.SegmentSetId,
            facts.GeometryVersion, true, cancellationToken);
        if (context.Status != AnhHuyProducerStatus.Ready) return new(context.Status);
        if (expectedSourceVersion is not null && expectedSourceVersion != facts.SourceVersion) return new(AnhHuyProducerStatus.StaleSource);
        if (expectedDispositionVersion is not null && expectedDispositionVersion != facts.DispositionVersion) return new(AnhHuyProducerStatus.StaleDisposition);
        return new(AnhHuyProducerStatus.Ready, facts);
    }
    private static AiCandidateFactsV1 Validate(RealAiCandidateReadFacts f, Guid projectId, Guid detectionId)
    {
        using var manifest = JsonDocument.Parse(f.Job.ManifestJson);
        using var payload = JsonDocument.Parse(f.Detection.RawPayload);
        using var admissionDoc = JsonDocument.Parse(f.Work.AdmissionJson);
        var a = AiWorkerWireContract.ParseAdmission(admissionDoc.RootElement);
        var m = manifest.RootElement; var p = payload.RootElement;
        Unique(m); Unique(p);
        Require(Hash(f.Job.ManifestJson) == f.Job.ManifestHash && a.ManifestHash == f.Job.ManifestHash &&
            f.Work.ProjectId == projectId && f.Job.ProjectId == projectId && a.ProjectId == projectId && f.Detection.Id == detectionId &&
            f.Job.Mode == "REAL" && f.Job.Status == ProcessingJobStatus.Completed && f.Work.State == RoadGuardSystem.BusinessObjects.Processing.Worker.AiVideoWorkState.Ingested &&
            f.Work.ProcessingJobId == f.Job.Id && a.JobId == f.Job.Id && f.Detection.ProcessingJobId == f.Job.Id &&
            f.Work.CurrentAttemptId == a.AttemptId && f.Job.ModelVersionId == a.ModelVersionId && f.Detection.ModelVersionId == a.ModelVersionId &&
            f.Work.DatasetId == a.DatasetId && f.Source.DatasetId == a.DatasetId && f.Source.ProjectId == projectId &&
            f.Work.SourceVideoFileId == a.SourceVideoFileId && f.Source.FileId == a.SourceVideoFileId && f.Source.Sha256 == a.InputSha256 && f.Source.Bytes == a.InputBytes &&
            m.GetProperty("sourceVideoVersion").GetString() == f.Source.SourceVideoVersion && !string.IsNullOrWhiteSpace(f.Source.SourceVideoVersion) &&
            m.GetProperty("logicalConfigurationIdentity").GetString() == f.Work.LogicalConfigurationIdentity);
        Require(m.GetProperty("datasetId").GetGuid() == a.DatasetId && m.GetProperty("projectId").GetGuid() == projectId &&
            m.GetProperty("sourceVideoFileId").GetGuid() == a.SourceVideoFileId && m.GetProperty("inputSha256").GetString() == a.InputSha256 &&
            m.GetProperty("inputBytes").GetInt64() == a.InputBytes && m.GetProperty("modelVersionId").GetGuid() == a.ModelVersionId &&
            m.GetProperty("contractVersion").GetString() == a.ContractVersion && m.GetProperty("checkpointSha256").GetString() == a.CheckpointSha256 &&
            m.GetProperty("preprocessingVersion").GetString() == a.PreprocessingVersion && m.GetProperty("configVersion").GetString() == a.ConfigVersion &&
            m.GetProperty("inputWidth").GetInt32() == a.InputWidth && m.GetProperty("inputHeight").GetInt32() == a.InputHeight &&
            m.GetProperty("inputDurationMs").GetInt32() == a.InputDurationMs && m.GetProperty("sampleFps").GetDouble() == a.SampleFps &&
            m.GetProperty("maxFrames").GetInt32() == a.MaxFrames);
        if (m.GetProperty("geometry").ValueKind != JsonValueKind.Null) Unique(m.GetProperty("geometry"));
        var g = m.GetProperty("geometry").Deserialize<AiVideoGeometrySnapshot>(SnapshotJson);
        Require(g is not null && g.RouteVersionId != Guid.Empty && g.SegmentSetId != Guid.Empty && !string.IsNullOrWhiteSpace(g.GeometryVersion) &&
            g.SegmentIds is { Length: > 0 } && !g.SegmentIds.Contains(Guid.Empty) && g.SegmentIds.Distinct().Count() == g.SegmentIds.Length);
        Require(p.GetProperty("workItemId").GetGuid() == f.Work.Id && p.GetProperty("attemptId").GetGuid() == a.AttemptId &&
            p.GetProperty("contractVersion").GetString() == a.ContractVersion && p.GetProperty("sourceVideoFileId").GetGuid() == a.SourceVideoFileId &&
            p.GetProperty("sourceVideoSha256").GetString() == a.InputSha256 && p.GetProperty("proposalOnly").GetBoolean() &&
            p.GetProperty("groundLocationStatus").GetString() == "NOT_MEASURED" && f.Detection.Geometry is null && f.Detection.RoadSectionVersionId is null);
        var resultId = p.GetProperty("resultId").GetGuid(); Require(resultId != Guid.Empty);
        var frameId = p.GetProperty("frameFileId").GetGuid(); var artifactId = p.GetProperty("frameArtifactId").GetGuid();
        var frame = f.Artifacts.Single(x => x.Proof.Kind == "FRAME" && x.Proof.ArtifactId == artifactId.ToString("D"));
        var raw = f.Artifacts.Single(x => x.Proof.Kind == "RAW_RESULT");
        foreach (var artifact in f.Artifacts)
        {
            Require(artifact.Proof.WorkItemId == f.Work.Id && artifact.Proof.AttemptId == a.AttemptId && artifact.Proof.FileId == artifact.File.Id &&
                artifact.Proof.Sha256 == artifact.File.Checksum && artifact.Proof.Bytes == artifact.File.SizeBytes &&
                artifact.File.StorageUri == $"anh02/real/{a.AttemptId:D}/{artifact.Proof.ArtifactId}");
        }
        Require(frame.File.Id == frameId && frame.File.MimeType is "image/png" or "image/jpeg" && raw.File.MimeType == "application/json");
        var timestamp = p.GetProperty("timestampMs").GetInt32(); Require(timestamp >= 0 && timestamp <= a.InputDurationMs);
        var box = p.GetProperty("bbox").EnumerateArray().Select(x => x.GetDecimal()).ToArray();
        Require(box.Length == 4 && box.All(x => x is >= 0 and <= 1) && box[2] > 0 && box[3] > 0 && box[0] + box[2] <= 1 && box[1] + box[3] <= 1);
        var sourceClass = p.GetProperty("sourceClass").GetString()!;
        string[] labels = ["alligator crack", "longitudinal crack", "oblique crack", "pothole", "repair", "transverse crack"];
        var map = m.GetProperty("classMap"); Unique(map);
        Require(map.EnumerateObject().Count() == 6 && labels.All(label => map.TryGetProperty(label, out _)) &&
            map.EnumerateObject().Select(x => x.Value.GetString()).Distinct(StringComparer.Ordinal).Count() == 6 && labels.Contains(sourceClass) &&
            map.GetProperty(sourceClass).GetString() == f.Detection.DefectTypeCode && map.GetProperty("repair").GetString() == "REPAIR_PATCH" &&
            ConfidenceMatches(p.GetProperty("confidence").GetDecimal(), f.Detection.Confidence));
        var telemetry = CaptureTelemetry(f);
        var evidence = Hash($"{f.Job.ManifestHash}:{raw.Proof.Sha256}:{detectionId:D}:{frame.File.Checksum}:{f.Source.SourceVideoVersion}:{Hash(f.Detection.RawPayload)}:{telemetry.EvidenceHash}");
        // CandidateDecisionService persists SourceVersion. Keep explicit immutable references in that
        // existing 200-character field so later telemetry cannot silently reinterpret a saved decision.
        var sourceVersion = $"r1:{evidence}:{telemetry.HeadRevisionId:N}:{telemetry.MeasuredRevisionId:N}:{telemetry.PairedSrtFileId:N}:{telemetry.EmbeddedSrtFileId:N}";
        return new("anh02.ai-candidate.v1", detectionId, projectId, a.DatasetId, a.ModelVersionId, f.Work.Id, a.JobId, a.AttemptId,
            resultId, a.ManifestHash, raw.Proof.Sha256, a.ContractVersion, "REAL", a.SourceVideoFileId, f.Source.SourceVideoVersion!,
            frameId, frame.File.Checksum, frame.File.Checksum, frame.File.SizeBytes, frame.File.MimeType, timestamp,
            f.Detection.DefectTypeCode!, f.Detection.Confidence, new(box[0], box[1], box[2], box[3]),
            g!.RouteVersionId, g.SegmentSetId, null, g.GeometryVersion, "UNKNOWN", sourceVersion, f.Disposition, f.DispositionVersion)
        { TelemetryReview = telemetry };
    }
    private static AiTelemetryReviewSnapshotV1 CaptureTelemetry(RealAiCandidateReadFacts f)
    {
        var revisions = (f.TelemetryRevisions ?? []).OrderBy(x => x.Number).ToArray();
        Guid? previous = null;
        for (var i = 0; i < revisions.Length; i++)
        {
            var r = revisions[i];
            Require(r.WorkId == f.Work.Id && r.SourceVideoHash == f.Source.Sha256 && r.Number == i + 1 && r.PreviousId == previous);
            previous = r.Id;
        }
        var embedded = f.Artifacts.SingleOrDefault(x => x.Proof.Kind == "TELEMETRY_SRT");
        Require(embedded is null || embedded.File.MimeType == "application/x-subrip");
        var paired = f.PairedSrt;
        Require(paired is null || paired.File.FileId != Guid.Empty && paired.File.Sha256.Length == 64 && paired.File.Bytes > 0 && !string.IsNullOrWhiteSpace(f.PairsManifestHash));
        var pairsHash = f.PairsManifestHash ?? Hash("");
        var evidenceHash = Hash(JsonSerializer.Serialize(new
        {
            revisions = revisions.Select(x => new
            {
                x.Id,
                x.Number,
                x.PreviousId,
                x.SourceVideoHash,
                x.Kind,
                x.IntervalsJson,
                x.OriginalSrtSha256,
                x.OriginalSrtBytes,
                x.OffsetMs,
                x.Note,
                x.ActorId,
                x.CreatedAt
            }),
            paired = paired is null ? null : new { paired.File.FileId, paired.File.Sha256, paired.File.Bytes, paired.OffsetMs },
            embedded = embedded is null ? null : new { embedded.File.Id, embedded.Proof.Sha256, embedded.Proof.Bytes },
            pairsHash
        }));
        return new(previous ?? Guid.Empty, revisions.LastOrDefault(x => x.Kind == "EXTERNAL_SRT")?.Id ?? Guid.Empty,
            paired?.File.FileId ?? Guid.Empty, embedded?.File.Id ?? Guid.Empty, pairsHash, evidenceHash);
    }
    // SQL persists Confidence as decimal(6,5); retain the full immutable payload and
    // compare its exact database projection, never an arbitrary tolerance.
    private static bool ConfidenceMatches(decimal raw, decimal stored)
        => raw is >= 0 and <= 1 && decimal.Round(raw, 5, MidpointRounding.AwayFromZero) == stored;
    private static void Unique(JsonElement e) => Require(e.ValueKind == JsonValueKind.Object && e.EnumerateObject().Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == e.EnumerateObject().Count());
    private static void Require(bool valid) { if (!valid) throw new InvalidDataException("REAL_CANDIDATE_PROVENANCE_INVALID"); }
    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}

/// <summary>The sole registered IAiCandidateFactsReader; a denied, stale or incomplete REAL source never falls through.</summary>
public sealed class CompositeAiCandidateFactsReader(IAiCandidateFactsReader real, IAiCandidateFactsReader mock) : IAiCandidateFactsReader
{
    public async Task<AnhHuyProducerResult<AiCandidateFactsV1>> ResolveAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid detectionId,
        string? expectedSourceVersion = null, string? expectedGeometryVersion = null, string? expectedDispositionVersion = null, CancellationToken cancellationToken = default)
    {
        var result = await real.ResolveAsync(actorId, role, projectId, detectionId, expectedSourceVersion, expectedGeometryVersion, expectedDispositionVersion, cancellationToken);
        return result.Status == AnhHuyProducerStatus.NotFound
            ? await mock.ResolveAsync(actorId, role, projectId, detectionId, expectedSourceVersion, expectedGeometryVersion, expectedDispositionVersion, cancellationToken) : result;
    }
}
