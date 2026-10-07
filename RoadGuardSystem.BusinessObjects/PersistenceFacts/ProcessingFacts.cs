using System.Text.Json.Serialization;
namespace RoadGuardSystem.BusinessObjects.PersistenceFacts.Processing;

// Materialized persistence commands and snapshots, independent of public transport contracts.
public sealed record AiMockRunViewFact(Guid Id, Guid ProjectId, string Stage, string Mode, string Status,
    Guid ProcessingJobId, Guid AttemptId, string ManifestHash, Guid? ResultId, string FixtureVersion,
    string? ErrorCode, string Version);

public sealed record AiMockScopeRequestFact(Guid RouteVersionId, Guid SegmentSetId, Guid[] SegmentIds, string TargetBand);

public sealed record CreateAiMockRunRequestFact(Guid DatasetId, AiMockScopeRequestFact Scope, Guid ModelVersionId,
    string PreprocessingVersion, string ConfigVersion, string FixtureVersion, string Stage,
    string ExpectedGeometryVersion, Guid? AnalysisRunId = null, Guid? CandidateSnapshotId = null);
