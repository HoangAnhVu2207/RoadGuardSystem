using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Reports;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReportEvidenceLocationDto(decimal? Latitude, decimal? Longitude, decimal? AccuracyMeters);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReportEvidenceInputDto(Guid FileId, string? FileVersion, string? LocationSource,
    DateTimeOffset? CapturedAt = null, ReportEvidenceLocationDto? Location = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateReporterReportRequestDto(string? Description, IReadOnlyList<ReportEvidenceInputDto?>? Evidence);

public sealed record ReporterReportResponseDto(Guid Id, string Description, DateTimeOffset CreatedAt, string Version, IReadOnlyList<Guid> EvidenceIds);
