namespace RoadGuardSystem.DTOs.Reports;

public sealed record ReportEvidenceInputDto(Guid FileId, string? FileVersion, string LocationSource);

public sealed record CreateReporterReportRequestDto(string Description, IReadOnlyList<ReportEvidenceInputDto> Evidence);

public sealed record ReporterReportResponseDto(Guid Id, string Description, DateTimeOffset CreatedAt, string Version, IReadOnlyList<Guid> EvidenceIds);
