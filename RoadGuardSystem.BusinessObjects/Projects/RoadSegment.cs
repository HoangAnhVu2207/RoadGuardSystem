namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class RoadSegment
{
    private RoadSegment() { }

    public Guid Id { get; private set; }
    public Guid SegmentSetId { get; private set; }
    public Guid RoadSectionVersionId { get; private set; }
    public int Sequence { get; private set; }
    public double? FromOffsetMeters { get; private set; }
    public double? ToOffsetMeters { get; private set; }
    public double? StartStationMeters { get; private set; }
    public double? EndStationMeters { get; private set; }
    public NetTopologySuite.Geometries.LineString? Geometry { get; private set; }
    public void SetGeometry(double from, double to, double origin, NetTopologySuite.Geometries.LineString geometry)
    {
        if (!double.IsFinite(from) || !double.IsFinite(to) || from < 0 || to <= from || geometry.Length <= 0) throw new ArgumentException("Invalid segment offsets.");
        FromOffsetMeters = from; ToOffsetMeters = to; StartStationMeters = origin + from; EndStationMeters = origin + to; Geometry = geometry;
    }

    public static RoadSegment Create(Guid id, Guid segmentSetId, Guid roadSectionVersionId, int sequence)
    {
        if (id == Guid.Empty || segmentSetId == Guid.Empty || roadSectionVersionId == Guid.Empty)
            throw new ArgumentException("Segment identifiers must not be empty.");
        if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
        return new RoadSegment { Id = id, SegmentSetId = segmentSetId, RoadSectionVersionId = roadSectionVersionId, Sequence = sequence };
    }
}
