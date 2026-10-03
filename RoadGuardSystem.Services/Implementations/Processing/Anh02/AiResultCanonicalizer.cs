using System.Globalization;
using System.Text.Json;
using RoadGuardSystem.DTOs.Processing;

namespace RoadGuardSystem.Services.Processing.Anh02;

public static class AiResultCanonicalizer
{
    public static byte[] Encode(AiResultV1 result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.SchemaVersion != "anh02.ai.v1" || result.Mode != "MOCK" || result.Stage is not ("VIDEO_ANALYSIS" or "DUPLICATE_MATCHING")
            || new[] { result.RunId, result.JobId, result.AttemptId, result.ModelVersionId }.Contains(Guid.Empty)
            || (result.ManifestHash is not { Length: 64 } || result.ManifestHash.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f'))) || !SyntheticAiFixture.Supported(result.FixtureVersion)
            || result.Detections is null || result.Matches is null
            || result.Detections.Any(d => d is null) || result.Matches.Any(m => m is null))
            throw new ArgumentException("AI result identity is invalid.", nameof(result));
        if ((result.Stage == "VIDEO_ANALYSIS" && result.Matches.Count != 0) || (result.Stage == "DUPLICATE_MATCHING" && result.Detections.Count != 0)
            || result.Detections.Select(d => d.DetectionId).Distinct().Count() != result.Detections.Count
            || result.Detections.Any(d => d.DetectionId == Guid.Empty || d.SourceVideoFileId == Guid.Empty || d.FrameFileId == Guid.Empty
                || string.IsNullOrWhiteSpace(d.FrameFileVersion) || string.IsNullOrWhiteSpace(d.TypeCode)
                || d.Confidence is < 0 or > 1 || d.TimestampMs < 0 || d.Bbox is not { Length: 4 }
                || d.Bbox[0] < 0 || d.Bbox[1] < 0 || d.Bbox[2] <= 0 || d.Bbox[3] <= 0
                || d.Bbox[0] + d.Bbox[2] > 1 || d.Bbox[1] + d.Bbox[3] > 1
                || d.PositionStatus != "UNKNOWN")
            || result.Matches.Select(m => (m.DetectionId, m.Rank)).Distinct().Count() != result.Matches.Count
            || result.Matches.Select(m => (m.DetectionId, m.CandidateDefectId)).Distinct().Count() != result.Matches.Count
            || result.Matches.Any(m => m.DetectionId == Guid.Empty || m.CandidateDefectId == Guid.Empty || m.Rank <= 0
                || string.IsNullOrWhiteSpace(m.CandidateVersion) || m.Score is < 0 or > 1 || m.ReasonCodes is null))
            throw new ArgumentException("AI result shape is invalid.", nameof(result));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("schemaVersion", result.SchemaVersion);
            writer.WriteString("runId", result.RunId);
            writer.WriteString("stage", result.Stage);
            writer.WriteString("mode", result.Mode);
            writer.WriteString("jobId", result.JobId);
            writer.WriteString("attemptId", result.AttemptId);
            writer.WriteString("manifestHash", result.ManifestHash);
            writer.WriteString("modelVersionId", result.ModelVersionId);
            writer.WriteString("fixtureVersion", result.FixtureVersion);
            writer.WriteString("completedAt", result.CompletedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
            writer.WriteStartArray("detections");
            foreach (var detection in result.Detections.OrderBy(d => d.DetectionId.ToString("D"), StringComparer.Ordinal))
            {
                writer.WriteStartObject(); writer.WriteString("detectionId", detection.DetectionId);
                writer.WriteString("sourceVideoFileId", detection.SourceVideoFileId); writer.WriteString("frameFileId", detection.FrameFileId);
                writer.WriteString("frameFileVersion", detection.FrameFileVersion); writer.WriteNumber("timestampMs", detection.TimestampMs);
                writer.WriteString("typeCode", detection.TypeCode); writer.WriteNumber("confidence", detection.Confidence);
                writer.WriteStartArray("bbox"); foreach (var value in detection.Bbox) writer.WriteNumberValue(value); writer.WriteEndArray();
                if (detection.SegmentId is { } segment) writer.WriteString("segmentId", segment); else writer.WriteNull("segmentId");
                writer.WriteString("positionStatus", detection.PositionStatus); writer.WriteNull("geometry"); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteStartArray("matches");
            foreach (var match in result.Matches.OrderBy(m => m.DetectionId.ToString("D"), StringComparer.Ordinal).ThenBy(m => m.Rank)
                .ThenBy(m => m.CandidateDefectId.ToString("D"), StringComparer.Ordinal))
            {
                writer.WriteStartObject(); writer.WriteString("detectionId", match.DetectionId); writer.WriteString("candidateDefectId", match.CandidateDefectId);
                writer.WriteString("candidateVersion", match.CandidateVersion); writer.WriteNumber("rank", match.Rank);
                if (match.Score is { } score) writer.WriteNumber("score", score); else writer.WriteNull("score");
                writer.WriteStartArray("reasonCodes"); foreach (var reason in match.ReasonCodes.OrderBy(r => r, StringComparer.Ordinal)) writer.WriteStringValue(reason);
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
