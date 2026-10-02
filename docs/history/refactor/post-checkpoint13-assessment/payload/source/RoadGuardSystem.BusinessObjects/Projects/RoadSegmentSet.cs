namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class RoadSegmentSet
{
    private RoadSegmentSet() { }

    public Guid Id { get; private set; }
    public Guid RoadSectionVersionId { get; private set; }
    public string Status { get; private set; } = string.Empty;

    public static RoadSegmentSet Create(Guid id, Guid roadSectionVersionId, string status = "PUBLISHED")
    {
        if (id == Guid.Empty || roadSectionVersionId == Guid.Empty)
            throw new ArgumentException("Segment set and route version ids must not be empty.");
        if (status.Trim().ToUpperInvariant() is not ("DRAFT" or "PUBLISHED" or "SUPERSEDED"))
            throw new ArgumentException("Segment set status is invalid.", nameof(status));
        return new RoadSegmentSet { Id = id, RoadSectionVersionId = roadSectionVersionId, Status = status.Trim().ToUpperInvariant() };
    }
}
