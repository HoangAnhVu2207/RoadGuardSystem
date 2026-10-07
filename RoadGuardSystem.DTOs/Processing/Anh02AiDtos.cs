using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Processing;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AiMockScopeRequest(Guid RouteVersionId, Guid SegmentSetId, Guid[] SegmentIds, string TargetBand)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Processing.AiMockScopeRequestFact?(AiMockScopeRequest? value)
        => value is null ? null! : new(value.RouteVersionId, value.SegmentSetId, value.SegmentIds, value.TargetBand);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator AiMockScopeRequest?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Processing.AiMockScopeRequestFact? value)
        => value is null ? null! : new(value.RouteVersionId, value.SegmentSetId, value.SegmentIds, value.TargetBand);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateAiMockRunRequest(Guid DatasetId, AiMockScopeRequest Scope, Guid ModelVersionId,
    string PreprocessingVersion, string ConfigVersion, string FixtureVersion, string Stage,
    string ExpectedGeometryVersion, Guid? AnalysisRunId = null, Guid? CandidateSnapshotId = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Processing.CreateAiMockRunRequestFact?(CreateAiMockRunRequest? value)
        => value is null ? null! : new(value.DatasetId, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Processing.AiMockScopeRequestFact)value.Scope, value.ModelVersionId, value.PreprocessingVersion, value.ConfigVersion, value.FixtureVersion, value.Stage, value.ExpectedGeometryVersion, value.AnalysisRunId, value.CandidateSnapshotId);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CreateAiMockRunRequest?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Processing.CreateAiMockRunRequestFact? value)
        => value is null ? null! : new(value.DatasetId, (global::RoadGuardSystem.DTOs.Processing.AiMockScopeRequest)value.Scope, value.ModelVersionId, value.PreprocessingVersion, value.ConfigVersion, value.FixtureVersion, value.Stage, value.ExpectedGeometryVersion, value.AnalysisRunId, value.CandidateSnapshotId);
}
public sealed record AiMockRunView(Guid Id, Guid ProjectId, string Stage, string Mode, string Status,
    Guid ProcessingJobId, Guid AttemptId, string ManifestHash, Guid? ResultId, string FixtureVersion,
    string? ErrorCode, string Version)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Processing.AiMockRunViewFact?(AiMockRunView? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.Stage, value.Mode, value.Status, value.ProcessingJobId, value.AttemptId, value.ManifestHash, value.ResultId, value.FixtureVersion, value.ErrorCode, value.Version);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator AiMockRunView?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Processing.AiMockRunViewFact? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.Stage, value.Mode, value.Status, value.ProcessingJobId, value.AttemptId, value.ManifestHash, value.ResultId, value.FixtureVersion, value.ErrorCode, value.Version);
}
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
