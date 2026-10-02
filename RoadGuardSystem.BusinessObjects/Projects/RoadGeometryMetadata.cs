namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class RoadGeometryMetadata
{
    private RoadGeometryMetadata() { }
    public Guid RoadSectionVersionId { get; private set; }
    public Guid SourceDraftId { get; private set; }
    public string InputJson { get; private set; } = "";
    public string GeometryHash { get; private set; } = "";
    public Guid ApprovedBy { get; private set; }
    public DateTimeOffset ApprovedAt { get; private set; }
    public static RoadGeometryMetadata Create(Guid versionId, Guid draftId, string input, string hash, Guid actor) => new() {
        RoadSectionVersionId = versionId, SourceDraftId = draftId, InputJson = input,
        GeometryHash = hash, ApprovedBy = actor, ApprovedAt = DateTimeOffset.UtcNow };
}
