namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class RoadGeometryMetadata
{
    private RoadGeometryMetadata() { }
    public Guid RoadSectionVersionId { get; private set; }
    public Guid SourceDraftId { get; private set; }
    public string InputJson { get; private set; } = "";
    public string GeometryHash { get; private set; } = "";
    public string? Wgs84GeometryJson { get; private set; }
    public Guid ApprovedBy { get; private set; }
    public DateTimeOffset ApprovedAt { get; private set; }
    public static RoadGeometryMetadata Create(Guid versionId, Guid draftId, string input, string hash, Guid actor, string? wgs84GeometryJson = null, DateTimeOffset? now = null) => new() {
        RoadSectionVersionId = versionId, SourceDraftId = draftId, InputJson = input,
        GeometryHash = hash, Wgs84GeometryJson = wgs84GeometryJson, ApprovedBy = actor, ApprovedAt = now ?? DateTimeOffset.UtcNow };
}
