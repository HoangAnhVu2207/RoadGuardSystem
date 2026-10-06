namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class RoadGeometryDraft
{
    private RoadGeometryDraft() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? RoadSectionId { get; private set; }
    public string RoadCode { get; private set; } = "";
    public string? RoadName { get; private set; }
    public string Status { get; private set; } = "DRAFT";
    public string InputJson { get; private set; } = "";
    public string OriginalCoordinatesJson { get; private set; } = "";
    public string? SourceChecksum { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static RoadGeometryDraft Create(Guid projectId, Guid? sectionId, string code, string? name,
        string input, string original, string? checksum, Guid actor, DateTimeOffset? now = null) => new() {
        Id = Guid.NewGuid(), ProjectId = projectId, RoadSectionId = sectionId, RoadCode = code,
        RoadName = name, InputJson = input, OriginalCoordinatesJson = original, SourceChecksum = checksum,
        CreatedBy = actor, UpdatedBy = actor, CreatedAt = now ?? DateTimeOffset.UtcNow, UpdatedAt = now ?? DateTimeOffset.UtcNow };
    public void Edit(string input, string original, string? checksum, string code, string? name, Guid actor, DateTimeOffset? now = null)
    {
        if (Status != "DRAFT") throw new InvalidOperationException("Confirmed draft is immutable.");
        InputJson = input; OriginalCoordinatesJson = original; SourceChecksum = checksum;
        RoadCode = code; RoadName = name; UpdatedBy = actor; UpdatedAt = now ?? DateTimeOffset.UtcNow;
    }
    public void Confirm(Guid sectionId) { if (Status != "DRAFT") throw new InvalidOperationException(); Status = "CONFIRMED"; RoadSectionId = sectionId; }
}
