namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class RoadSegment
{
    private RoadSegment() { }

    public Guid Id { get; private set; }
    public Guid SegmentSetId { get; private set; }
    public Guid RoadSectionVersionId { get; private set; }
    public int Sequence { get; private set; }

    public static RoadSegment Create(Guid id, Guid segmentSetId, Guid roadSectionVersionId, int sequence)
    {
        if (id == Guid.Empty || segmentSetId == Guid.Empty || roadSectionVersionId == Guid.Empty)
            throw new ArgumentException("Segment identifiers must not be empty.");
        if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
        return new RoadSegment { Id = id, SegmentSetId = segmentSetId, RoadSectionVersionId = roadSectionVersionId, Sequence = sequence };
    }
}
