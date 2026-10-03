using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace RoadGuardSystem.Services.Processing.Anh02;

// Internal canonical payload builder. Admission resolves every source field from current trusted producers.
public sealed record AiManifestFileDraft(Guid FileId, string FileVersion, string Sha256, long SizeBytes, string MediaType, string Purpose);
public sealed record AiManifestPairDraft(Guid VideoFileId, Guid TelemetryFileId);
public sealed record AiMatchingItemDraft(Guid DefectId, string Version, Guid? SegmentId, Guid RouteVersionId);
public sealed record AiManifestDraft(Guid RunId, string Stage, Guid ProjectId, Guid JobId, Guid AttemptId,
    Guid DatasetVersionId, string DatasetManifestHash, Guid RouteVersionId, Guid SegmentSetId,
    string GeometryVersion, IReadOnlyList<Guid> SegmentIds, string TargetBand, Guid ModelVersionId,
    string PreprocessingVersion, string ConfigVersion, string FixtureVersion, Guid CreatedBy,
    DateTimeOffset CreatedAt, IReadOnlyList<AiManifestFileDraft> SourceFiles, IReadOnlyList<AiManifestPairDraft> Pairs,
    string TelemetryStatus, Guid? AnalysisResultId = null, Guid? CandidateSnapshotId = null,
    string? CandidateSnapshotHash = null, IReadOnlyList<AiMatchingItemDraft>? Items = null);

public static class AiManifestCanonicalizer
{
    public static byte[] Encode(AiManifestDraft manifest)
    {
        Validate(manifest);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("schemaVersion", "anh02.ai.v1");
            writer.WriteString("runId", manifest.RunId);
            writer.WriteString("stage", manifest.Stage);
            writer.WriteString("mode", "MOCK");
            writer.WriteString("projectId", manifest.ProjectId);
            writer.WriteString("jobId", manifest.JobId);
            writer.WriteString("attemptId", manifest.AttemptId);
            writer.WriteString("datasetVersionId", manifest.DatasetVersionId);
            writer.WriteString("datasetManifestHash", manifest.DatasetManifestHash);
            writer.WriteString("routeVersionId", manifest.RouteVersionId);
            writer.WriteString("segmentSetId", manifest.SegmentSetId);
            writer.WriteString("geometryVersion", manifest.GeometryVersion);
            writer.WriteStartArray("segmentIds");
            foreach (var id in manifest.SegmentIds.OrderBy(Id, StringComparer.Ordinal)) writer.WriteStringValue(id);
            writer.WriteEndArray();
            writer.WriteString("targetBand", manifest.TargetBand);
            writer.WriteString("modelVersionId", manifest.ModelVersionId);
            writer.WriteString("preprocessingVersion", manifest.PreprocessingVersion);
            writer.WriteString("configVersion", manifest.ConfigVersion);
            writer.WriteString("fixtureVersion", manifest.FixtureVersion);
            writer.WriteString("createdBy", manifest.CreatedBy);
            writer.WriteString("createdAt", manifest.CreatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
            writer.WriteStartArray("sourceFiles");
            foreach (var file in manifest.SourceFiles.OrderBy(file => Id(file.FileId), StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("fileId", file.FileId);
                writer.WriteString("fileVersion", file.FileVersion);
                writer.WriteString("sha256", file.Sha256);
                writer.WriteNumber("sizeBytes", file.SizeBytes);
                writer.WriteString("mediaType", file.MediaType);
                writer.WriteString("purpose", file.Purpose);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("pairs");
            foreach (var pair in manifest.Pairs.OrderBy(pair => Id(pair.VideoFileId), StringComparer.Ordinal)
                .ThenBy(pair => Id(pair.TelemetryFileId), StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("videoFileId", pair.VideoFileId);
                writer.WriteString("telemetryFileId", pair.TelemetryFileId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteString("telemetryStatus", manifest.TelemetryStatus);
            if (manifest.Stage == "DUPLICATE_MATCHING")
            {
                writer.WriteString("analysisResultId", manifest.AnalysisResultId!.Value);
                writer.WriteString("candidateSnapshotId", manifest.CandidateSnapshotId!.Value);
                writer.WriteString("candidateSnapshotHash", manifest.CandidateSnapshotHash);
                writer.WriteStartArray("items");
                foreach (var item in manifest.Items!.OrderBy(item => Id(item.DefectId), StringComparer.Ordinal))
                {
                    writer.WriteStartObject();
                    writer.WriteString("defectId", item.DefectId);
                    writer.WriteString("version", item.Version);
                    if (item.SegmentId is { } segment) writer.WriteString("segmentId", segment);
                    else writer.WriteNull("segmentId");
                    writer.WriteString("routeVersionId", item.RouteVersionId);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    // Historical verification hashes persisted bytes, never a deserialized reconstruction.
    public static string Hash(ReadOnlySpan<byte> canonicalBytes)
        => Convert.ToHexString(SHA256.HashData(canonicalBytes)).ToLowerInvariant();

    private static string Id(Guid id) => id.ToString("D");
    private static bool IsHash(string? value) => value is { Length: 64 }
        && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static void Validate(AiManifestDraft manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (new[] { manifest.RunId, manifest.ProjectId, manifest.JobId, manifest.AttemptId, manifest.DatasetVersionId,
            manifest.RouteVersionId, manifest.SegmentSetId, manifest.ModelVersionId, manifest.CreatedBy }.Contains(Guid.Empty)
            || !IsHash(manifest.DatasetManifestHash)
            || new[] { manifest.GeometryVersion, manifest.PreprocessingVersion, manifest.ConfigVersion, manifest.FixtureVersion }.Any(string.IsNullOrWhiteSpace)
            || manifest.TargetBand is not ("SURFACE" or "LEFT_EDGE" or "RIGHT_EDGE")
            || manifest.Stage is not ("VIDEO_ANALYSIS" or "DUPLICATE_MATCHING"))
            throw new ArgumentException("Manifest identity or version is invalid.", nameof(manifest));
        if (manifest.SegmentIds is null || manifest.SegmentIds.Count == 0 || manifest.SegmentIds.Contains(Guid.Empty)
            || manifest.SegmentIds.Distinct().Count() != manifest.SegmentIds.Count
            || manifest.SourceFiles is null || manifest.SourceFiles.Count == 0
            || manifest.SourceFiles.Any(f => f is null || f.FileId == Guid.Empty || !IsHash(f.Sha256) || f.SizeBytes <= 0
                || string.IsNullOrWhiteSpace(f.FileVersion) || string.IsNullOrWhiteSpace(f.MediaType) || string.IsNullOrWhiteSpace(f.Purpose))
            || manifest.SourceFiles.Select(f => f.FileId).Distinct().Count() != manifest.SourceFiles.Count
            || !manifest.SourceFiles.Any(f => f.Purpose == "SURVEY_VIDEO"))
            throw new ArgumentException("Manifest source identities must be unique and complete.", nameof(manifest));
        if (manifest.Pairs is null || manifest.Pairs.Any(p => p is null)
            || manifest.Pairs.Select(p => p.VideoFileId).Distinct().Count() != manifest.Pairs.Count
            || manifest.Pairs.Any(p => !manifest.SourceFiles.Any(f => f.FileId == p.VideoFileId && f.Purpose == "SURVEY_VIDEO")
                || !manifest.SourceFiles.Any(f => f.FileId == p.TelemetryFileId && f.Purpose == "TELEMETRY")))
            throw new ArgumentException("Pair references must resolve to distinct video sources and telemetry.", nameof(manifest));
        if (manifest.TelemetryStatus is not ("PRESENT" or "MISSING" or "UNKNOWN")
            || (manifest.TelemetryStatus != "PRESENT" && manifest.Pairs.Count != 0))
            throw new ArgumentException("Telemetry declaration conflicts with pairing.", nameof(manifest));
        if (manifest.Stage == "VIDEO_ANALYSIS")
        {
            if (manifest.AnalysisResultId is not null || manifest.CandidateSnapshotId is not null
                || manifest.CandidateSnapshotHash is not null || manifest.Items is not null)
                throw new ArgumentException("Analysis must not include matching references.", nameof(manifest));
        }
        else if (manifest.AnalysisResultId is null || manifest.AnalysisResultId == Guid.Empty)
        {
            throw new ArgumentException("Matching requires an analysis result.", nameof(manifest));
        }
        else if (manifest.CandidateSnapshotId is null || manifest.CandidateSnapshotId == Guid.Empty
            || !IsHash(manifest.CandidateSnapshotHash) || manifest.Items is null
            || manifest.Items.Any(i => i is null || i.DefectId == Guid.Empty || i.RouteVersionId != manifest.RouteVersionId
                || string.IsNullOrWhiteSpace(i.Version) || (i.SegmentId is { } segment && !manifest.SegmentIds.Contains(segment)))
            || manifest.Items.Select(i => i.DefectId).Distinct().Count() != manifest.Items.Count)
            throw new ArgumentException("Matching snapshot references must be unique and scoped.", nameof(manifest));
    }
}
