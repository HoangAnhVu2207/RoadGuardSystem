namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class RoadSegmentSet
{
    private RoadSegmentSet() { }

    public Guid Id { get; private set; }
    public Guid RoadSectionVersionId { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string? DefinitionJson { get; private set; }
    public string? GeometryHash { get; private set; }
    public Guid? PublishedBy { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public void Define(string definition, string hash)
    {
        if (Status != "DRAFT") throw new InvalidOperationException("Only drafts can be edited.");
        DefinitionJson = definition; GeometryHash = hash;
    }
    public void Publish(Guid actor) { if (Status != "DRAFT") throw new InvalidOperationException(); Status = "PUBLISHED"; PublishedBy = actor; PublishedAt = DateTimeOffset.UtcNow; }
    public void Supersede() { if (Status != "PUBLISHED") throw new InvalidOperationException(); Status = "SUPERSEDED"; }

    public static RoadSegmentSet Create(Guid id, Guid roadSectionVersionId, string status = "PUBLISHED")
    {
        if (id == Guid.Empty || roadSectionVersionId == Guid.Empty)
            throw new ArgumentException("Segment set and route version ids must not be empty.");
        if (status.Trim().ToUpperInvariant() is not ("DRAFT" or "PUBLISHED" or "SUPERSEDED"))
            throw new ArgumentException("Segment set status is invalid.", nameof(status));
        return new RoadSegmentSet { Id = id, RoadSectionVersionId = roadSectionVersionId, Status = status.Trim().ToUpperInvariant() };
    }
}
