using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Defects;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReportMatchingLocationInput(string SourceVersion, string GeometryVersion,
    Guid? EvidenceId, Guid[] SegmentIds, string Reason);
public sealed record ReportMatchingLocationView(Guid ReportId, string SourceVersion, string GeometryVersion,
    string Status, ReportMatchingConfirmationView? Confirmation, ReportMatchingEvidenceView[] Evidence);
public sealed record ReportMatchingConfirmationView(Guid Id, int Sequence, Guid ActorId, DateTimeOffset ConfirmedAt,
    string Reason, Guid? EvidenceId, Guid[] SegmentIds, decimal? Latitude, decimal? Longitude, decimal? AccuracyMeters);
public sealed record ReportMatchingEvidenceView(Guid EvidenceId, decimal? Latitude, decimal? Longitude,
    decimal? AccuracyMeters, string? LocationSource);
public sealed record ReportMatchingLocationResult(int Status, string? Code = null, ReportMatchingLocationView? Value = null);
