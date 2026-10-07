using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Reports;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReportEvidenceLocationDto(decimal? Latitude, decimal? Longitude, decimal? AccuracyMeters)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.ReportEvidenceLocationFact?(ReportEvidenceLocationDto? value)
        => value is null ? null! : new(value.Latitude, value.Longitude, value.AccuracyMeters);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ReportEvidenceLocationDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports.ReportEvidenceLocationFact? value)
        => value is null ? null! : new(value.Latitude, value.Longitude, value.AccuracyMeters);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReportEvidenceInputDto(Guid FileId, string? FileVersion, string? LocationSource,
    DateTimeOffset? CapturedAt = null, ReportEvidenceLocationDto? Location = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateReporterReportRequestDto(string? Description, IReadOnlyList<ReportEvidenceInputDto?>? Evidence);

public sealed record ReporterReportResponseDto(Guid Id, string Description, DateTimeOffset CreatedAt, string Version, IReadOnlyList<Guid> EvidenceIds);
