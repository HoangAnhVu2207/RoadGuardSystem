using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Processing;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AiMockScopeRequest(Guid RouteVersionId, Guid SegmentSetId, Guid[] SegmentIds, string TargetBand);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateAiMockRunRequest(Guid DatasetId, AiMockScopeRequest Scope, Guid ModelVersionId,
    string PreprocessingVersion, string ConfigVersion, string FixtureVersion, string Stage,
    string ExpectedGeometryVersion, Guid? AnalysisRunId = null, Guid? CandidateSnapshotId = null);
public sealed record AiMockRunView(Guid Id, Guid ProjectId, string Stage, string Mode, string Status,
    Guid ProcessingJobId, Guid AttemptId, string ManifestHash, Guid? ResultId, string FixtureVersion,
    string? ErrorCode, string Version);
public sealed record AiDetectionResultV1(Guid DetectionId, Guid SourceVideoFileId, Guid FrameFileId,
    string FrameFileVersion, long TimestampMs, string TypeCode, decimal Confidence, decimal[] Bbox,
    Guid? SegmentId, string PositionStatus)
{
    public object? Geometry { get; }
}
public sealed record AiMatchResultV1(Guid DetectionId, Guid CandidateDefectId, string CandidateVersion,
    int Rank, decimal? Score, string[] ReasonCodes);
public sealed record AiResultV1(string SchemaVersion, Guid RunId, string Stage, string Mode, Guid JobId,
    Guid AttemptId, string ManifestHash, Guid ModelVersionId, string FixtureVersion, string ResultHash,
    DateTimeOffset CompletedAt, IReadOnlyList<AiDetectionResultV1> Detections, IReadOnlyList<AiMatchResultV1> Matches);
