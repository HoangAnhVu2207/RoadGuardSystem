namespace RoadGuardSystem.DTOs.Reports;

public sealed record OwnReportEvidenceDto(Guid Id, Guid FileId, string FileVersion, Guid? SupplementId,
    DateTimeOffset? CapturedAt, string LocationSource, ReportEvidenceLocationDto? Location);
public sealed record OwnReportSupplementDto(Guid Id, string Description, DateTimeOffset CreatedAt);
public sealed record OwnReportUpdateDto(Guid Id, string Summary, DateTimeOffset PublishedAt, IReadOnlyList<Guid> EvidenceIds);
public sealed record OwnReportDto(Guid Id, string Description, DateTimeOffset CreatedAt, string Version,
    IReadOnlyList<OwnReportEvidenceDto> Evidence, string RoutingStatus,
    IReadOnlyList<OwnReportUpdateDto> PublicUpdates, IReadOnlyList<OwnReportSupplementDto> Supplements);
public sealed record OwnReportPageDto(IReadOnlyList<OwnReportDto> Items, string? NextCursor);
