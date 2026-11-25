namespace RoadGuardSystem.BusinessObjects.Processing.Worker;

/// <summary>Attempt-scoped immutable descriptor. ArtifactId is opaque, never a filesystem path.</summary>
public sealed class AiWorkerArtifact
{
    private AiWorkerArtifact() { }
    public Guid Id { get; private set; }
    public Guid WorkItemId { get; private set; }
    public Guid AttemptId { get; private set; }
    public string ArtifactId { get; private set; } = string.Empty;
    public string Sha256 { get; private set; } = string.Empty;
    public long Bytes { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public Guid? FileId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static AiWorkerArtifact Create(Guid id, Guid workItemId, Guid attemptId, string artifactId,
        string sha256, long bytes, string kind, int? width = null, int? height = null)
    {
        if (id == Guid.Empty || workItemId == Guid.Empty || attemptId == Guid.Empty ||
            !Guid.TryParseExact(artifactId, "D", out var opaqueId) || opaqueId == Guid.Empty || artifactId != opaqueId.ToString("D") ||
            sha256 is not { Length: 64 } || sha256.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')) || bytes <= 0)
            throw new ArgumentException("Artifact requires valid immutable identities, lowercase SHA256 and positive bytes.");
        if (kind is not ("RAW_RESULT" or "FRAME" or "TELEMETRY_SRT") ||
            (kind == "FRAME" ? width is not (> 0 and <= 16384) || height is not (> 0 and <= 16384) : width is not null || height is not null))
            throw new ArgumentException("Artifact kind or frame dimensions are invalid.");
        return new AiWorkerArtifact
        {
            Id = id,
            WorkItemId = workItemId,
            AttemptId = attemptId,
            ArtifactId = artifactId,
            Sha256 = sha256,
            Bytes = bytes,
            Kind = kind,
            Width = width,
            Height = height
        };
    }

    // Caller must first verify bytes/hash and durably register the matching StoredFile.
    public void MarkRegistered(Guid fileId)
    {
        if (fileId == Guid.Empty) throw new ArgumentException("Verified file identity is required.", nameof(fileId));
        if (FileId is { } previous && previous != fileId) throw new InvalidOperationException("Artifact file mapping is immutable once registered.");
        FileId = fileId;
    }
}
