namespace RoadGuardSystem.BusinessObjects.Labels;

// Domain-only head; it has no discovered EF mapping. A SQL writer must check the
// actual head rowversion and save revision/head/review/audit/receipt atomically.
public sealed class TrainingLabel
{
    private readonly List<TrainingLabelRevision> _revisions = [];
    private readonly Dictionary<Guid, string> _fileVersions = [];
    private TrainingLabel() { }
    public Guid Id { get; private init; }
    public Guid ProjectId { get; private init; }
    public Guid SourceId { get; private init; }
    public string SourceKind { get; private init; } = "";
    public IReadOnlyList<TrainingLabelRevision> Revisions => _revisions.AsReadOnly();
    public TrainingLabelRevision CurrentRevision => _revisions[^1];
    public TrainingLabelRevision? CurrentApprovedRevision => CurrentRevision.Status == TrainingLabelReviewStatus.Approved ? CurrentRevision : null;
    public string CurrentFileVersion => _fileVersions[CurrentRevision.Id];

    public static TrainingLabel Create(Guid id, Guid project, Guid source, string kind, string sourceVersion, Guid file, string fileVersion,
        decimal x, decimal y, decimal width, decimal height, string type, string reason)
    {
        if (id == Guid.Empty || project == Guid.Empty || source == Guid.Empty) throw new ArgumentException("Label, project and source are required.");
        if (kind is not ("REPORT" or "AI_DETECTION")) throw new ArgumentException("Unsupported source kind.", nameof(kind));
        var label = new TrainingLabel { Id = id, ProjectId = project, SourceId = source, SourceKind = kind };
        label.Append(1, sourceVersion, file, fileVersion, x, y, width, height, type, reason);
        return label;
    }
    public void AppendRevision(int expectedRevision, string sourceVersion, Guid file, string fileVersion,
        decimal x, decimal y, decimal width, decimal height, string type, string reason)
    {
        if (CurrentRevision.Revision != expectedRevision) throw new InvalidOperationException("The current label head changed.");
        Append(checked(expectedRevision + 1), sourceVersion, file, fileVersion, x, y, width, height, type, reason);
    }
    public void Review(int expectedRevision, Guid actor, TrainingLabelReviewStatus decision, string reason, DateTimeOffset now)
    {
        if (CurrentRevision.Revision != expectedRevision) throw new InvalidOperationException("Only the expected current revision may be reviewed.");
        CurrentRevision.Review(actor, decision, reason, now);
    }
    private void Append(int revision, string sourceVersion, Guid file, string fileVersion, decimal x, decimal y, decimal width, decimal height, string type, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileVersion);
        var normalizedFileVersion = fileVersion.Trim();
        if (normalizedFileVersion.Length > 200) throw new ArgumentException("File version exceeds 200 characters.", nameof(fileVersion));
        var value = TrainingLabelRevision.Create(Guid.NewGuid(), Id, revision, SourceId, sourceVersion, file, x, y, width, height, type, reason);
        _fileVersions.Add(value.Id, normalizedFileVersion);
        _revisions.Add(value);
    }
}
