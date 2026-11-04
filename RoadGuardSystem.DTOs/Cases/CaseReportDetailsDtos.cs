using RoadGuardSystem.DTOs.Reports;

namespace RoadGuardSystem.DTOs.Cases;

public sealed record CaseReportEvidenceDto(Guid EvidenceId, Guid FileId, string FileVersion, Guid? SupplementId,
    DateTimeOffset? CapturedAt, string LocationSource, ReportEvidenceLocationDto? Location, string ContentPath);

public sealed record CaseReportSupplementDto(Guid Id, string Description, DateTimeOffset ReceivedAt,
    IReadOnlyList<CaseReportEvidenceDto> Evidence);

/// <summary>
/// Reporter contact fields come from the current account and can change after the Report was received.
/// A missing phone number is null; confirmation is the current server-held account value.
/// </summary>
public sealed record CaseReportDetailDto(Guid CaseId, Guid ReportId, Guid ReporterUserId, string ReporterDisplayName,
    string? ReporterPhoneNumber, bool ReporterPhoneNumberConfirmed, string Description, DateTimeOffset ReceivedAt,
    IReadOnlyList<CaseReportEvidenceDto> OriginalEvidence, IReadOnlyList<CaseReportSupplementDto> Supplements);

public sealed record CaseReportDetailPageDto(IReadOnlyList<CaseReportDetailDto> Items, string? NextCursor);
