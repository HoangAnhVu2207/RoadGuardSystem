namespace RoadGuardSystem.BusinessObjects.Exports;

public sealed class ExportJob
{
    private ExportJob() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid RequestedBy { get; private set; }
    public Guid SnapshotId { get; private set; }
    public string Kind { get; private set; } = "";
    public string Format { get; private set; } = "";
    public string Status { get; private set; } = "QUEUED";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public Guid? ArtifactId { get; private set; }
    public string? ErrorCode { get; private set; }
    public Guid? LeaseToken { get; private set; }
    public DateTimeOffset? LeaseUntil { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static ExportJob Create(Guid id, Guid projectId, Guid actorId, Guid snapshotId, string kind, string format, DateTimeOffset now)
    {
        if (new[] { id, projectId, actorId, snapshotId }.Contains(Guid.Empty) || kind is not ("DOSSIER" or "TRAINING") || format is not ("PDF" or "ZIP")) throw new ArgumentException("Invalid export identity or format.");
        return new() { Id = id, ProjectId = projectId, RequestedBy = actorId, SnapshotId = snapshotId, Kind = kind, Format = format, CreatedAt = now.ToUniversalTime() };
    }
    public bool TryClaim(Guid token, DateTimeOffset now, TimeSpan duration)
    {
        if (token == Guid.Empty || duration <= TimeSpan.Zero) throw new ArgumentException("Invalid lease.");
        if (Status is "SUCCEEDED" or "FAILED" || LeaseUntil > now || NextAttemptAt > now) return false;
        Status = "RUNNING"; LeaseToken = token; LeaseUntil = now + duration; NextAttemptAt = null; return true;
    }
    public bool OwnsLease(Guid token, DateTimeOffset now) => LeaseToken == token && LeaseUntil > now && Status == "RUNNING";
    public void Complete(Guid token, Guid artifactId, DateTimeOffset now)
    {
        if (!OwnsLease(token, now) || artifactId == Guid.Empty) throw new InvalidOperationException("Export lease is not current.");
        Status = "SUCCEEDED"; ArtifactId = artifactId; CompletedAt = now.ToUniversalTime(); ExpiresAt = CompletedAt + TimeSpan.FromDays(30); LeaseToken = null; LeaseUntil = null; ErrorCode = null;
    }
    public void Fail(Guid token, DateTimeOffset now, string code, bool permanent, TimeSpan? retryBackoff = null)
    {
        if (!OwnsLease(token, now)) throw new InvalidOperationException("Export lease is not current.");
        var backoff = retryBackoff ?? TimeSpan.FromMinutes(1); if (backoff <= TimeSpan.Zero) throw new ArgumentException("Retry backoff must be positive.");
        Status = permanent ? "FAILED" : "QUEUED"; ErrorCode = code; CompletedAt = permanent ? now.ToUniversalTime() : null; LeaseToken = null; LeaseUntil = null; NextAttemptAt = permanent ? null : now + backoff;
    }
}

// JSON is the canonical immutable serialized typed ExportSnapshotPayloadDto; bytes are never reconstructed for verification.
public sealed class ExportSnapshot
{
    private ExportSnapshot() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string PayloadJson { get; private set; } = "";
    public string Hash { get; private set; } = "";
    public DateTimeOffset CapturedAt { get; private set; }
    public static ExportSnapshot Create(Guid id, Guid projectId, string json, string hash, DateTimeOffset now)
    {
        if (id == Guid.Empty || projectId == Guid.Empty || string.IsNullOrWhiteSpace(json) || hash.Length != 64) throw new ArgumentException("Invalid snapshot.");
        return new() { Id = id, ProjectId = projectId, PayloadJson = json, Hash = hash, CapturedAt = now.ToUniversalTime() };
    }
}

public sealed class ExportSnapshotFile
{
    private ExportSnapshotFile() { }
    public Guid Id { get; private set; }
    public Guid SnapshotId { get; private set; }
    public Guid FileId { get; private set; }
    public string FileVersion { get; private set; } = "";
    public string Sha256 { get; private set; } = "";
    public long SizeBytes { get; private set; }
    public string MediaType { get; private set; } = "";
    public bool Included { get; private set; }
    public string? ArchivePath { get; private set; }
    public static ExportSnapshotFile Create(Guid snapshotId, Guid fileId, string version, string hash, long size, string mediaType, bool included, string? path) => new()
    { Id = Guid.NewGuid(), SnapshotId = snapshotId, FileId = fileId, FileVersion = version, Sha256 = hash, SizeBytes = size, MediaType = mediaType, Included = included, ArchivePath = path };
}

public sealed class GeneratedArtifact
{
    private GeneratedArtifact() { }
    public Guid Id { get; private set; }
    public Guid ExportJobId { get; private set; }
    public Guid SnapshotId { get; private set; }
    public Guid FileId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public static GeneratedArtifact Create(Guid id, Guid exportId, Guid snapshotId, Guid fileId, DateTimeOffset now) => new() { Id = id, ExportJobId = exportId, SnapshotId = snapshotId, FileId = fileId, CreatedAt = now.ToUniversalTime() };
}
