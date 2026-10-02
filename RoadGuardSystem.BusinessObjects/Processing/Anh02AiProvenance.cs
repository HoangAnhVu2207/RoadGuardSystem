namespace RoadGuardSystem.BusinessObjects.Processing;

// ProcessingJob/Attempt/AIDetection remain the authoritative processing identities.
// These sidecars hold stage claims and immutable trusted source provenance only.
public sealed class AiMockRun
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid DatasetVersionId { get; set; }
    public Guid ProcessingJobId { get; set; }
    public Guid AttemptId { get; set; }
    public Guid ModelVersionId { get; set; }
    public Guid RouteVersionId { get; set; }
    public Guid SegmentSetId { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Stage { get; set; } = "VIDEO_ANALYSIS";
    public string Status { get; set; } = "QUEUED";
    public string FixtureVersion { get; set; } = "";
    public string GeometryVersion { get; set; } = "";
    public string ManifestHash { get; set; } = "";
    public string CanonicalManifest { get; set; } = "";
    public Guid? AnalysisRunId { get; set; }
    public Guid? ResultId { get; set; }
    public string? ErrorCode { get; set; }
    public Guid? LeaseOwner { get; set; }
    public DateTimeOffset? LeaseUntil { get; set; }
    public DateTimeOffset? ResultTimestamp { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class AiResultProvenance
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public Guid ProcessingJobId { get; set; }
    public Guid AttemptId { get; set; }
    public string ManifestHash { get; set; } = "";
    public string ResultHash { get; set; } = "";
    public string CanonicalResult { get; set; } = "";
    public DateTimeOffset CompletedAt { get; set; }
}

public sealed class AiDetectionProvenance
{
    public Guid DetectionId { get; set; }
    public Guid RunId { get; set; }
    public Guid ResultId { get; set; }
    public Guid SourceVideoFileId { get; set; }
    public string SourceVideoFileVersion { get; set; } = "";
    public Guid FrameFileId { get; set; }
    public string FrameFileVersion { get; set; } = "";
    public string DerivationHash { get; set; } = "";
    public long TimestampMilliseconds { get; set; }
    public long SourceDurationMilliseconds { get; set; }
    public Guid? SegmentId { get; set; }
    public decimal BoxX { get; set; }
    public decimal BoxY { get; set; }
    public decimal BoxWidth { get; set; }
    public decimal BoxHeight { get; set; }
}

public sealed class AiManifestFileReference
{
    public Guid RunId { get; set; }
    public Guid FileId { get; set; }
    public string FileVersion { get; set; } = "";
}
