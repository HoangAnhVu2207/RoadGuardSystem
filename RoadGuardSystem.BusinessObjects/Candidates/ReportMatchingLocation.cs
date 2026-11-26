namespace RoadGuardSystem.BusinessObjects.Candidates;

/// <summary>Immutable PM confirmation for matching only; never overwrites evidence GPS.</summary>
public sealed class ReportMatchingLocation
{
    private ReportMatchingLocation() { }
    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public int Sequence { get; private set; }
    public Guid CaseId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid ActorId { get; private set; }
    public DateTimeOffset ConfirmedAt { get; private set; }
    public string Reason { get; private set; } = "";
    public Guid RouteVersionId { get; private set; }
    public Guid SegmentSetId { get; private set; }
    public string GeometryVersion { get; private set; } = "";
    public string ReportVersion { get; private set; } = "";
    public string CaseVersion { get; private set; } = "";
    public Guid? EvidenceId { get; private set; }
    public string SegmentIdsJson { get; private set; } = "[]";
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public decimal? AccuracyMeters { get; private set; }
    public static ReportMatchingLocation Create(Guid report, int sequence, Guid incident, Guid project,
        Guid actor, DateTimeOffset at, string reason, Guid route, Guid set, string geometryVersion,
        string reportVersion, string caseVersion, Guid? evidence, Guid[] segments,
        decimal? latitude, decimal? longitude, decimal? accuracy)
    {
        if (new[] { report, incident, project, actor, route, set }.Any(id => id == Guid.Empty) || sequence < 1 ||
            string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000 ||
            new[] { geometryVersion, reportVersion, caseVersion }.Any(string.IsNullOrWhiteSpace) ||
            segments is null || segments.Length > 200 || segments.Any(id => id == Guid.Empty) ||
            segments.Distinct().Count() != segments.Length || evidence == Guid.Empty ||
            evidence is null && segments.Length == 0 || latitude.HasValue != longitude.HasValue ||
            latitude is < -90 or > 90 || longitude is < -180 or > 180 || accuracy < 0 ||
            evidence is null && (latitude is not null || accuracy is not null))
            throw new ArgumentException("An explicit, versioned PM matching confirmation is required.");
        return new()
        {
            Id = Guid.NewGuid(),
            ReportId = report,
            Sequence = sequence,
            CaseId = incident,
            ProjectId = project,
            ActorId = actor,
            ConfirmedAt = at.ToUniversalTime(),
            Reason = reason.Trim(),
            RouteVersionId = route,
            SegmentSetId = set,
            GeometryVersion = geometryVersion,
            ReportVersion = reportVersion,
            CaseVersion = caseVersion,
            EvidenceId = evidence,
            SegmentIdsJson = System.Text.Json.JsonSerializer.Serialize(segments.Order().ToArray()),
            Latitude = latitude,
            Longitude = longitude,
            AccuracyMeters = accuracy
        };
    }
}
