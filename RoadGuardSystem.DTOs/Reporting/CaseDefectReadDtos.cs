namespace RoadGuardSystem.DTOs.Reporting;

public sealed record CaseReportRefV1(Guid ReportId, string Version, DateTimeOffset ReceivedAt);
public sealed record CaseEvidenceRefV1(Guid CaseId, Guid SourceReportId, Guid EvidenceId,
    Guid FileId, string FileVersion, string Sha256, long SizeBytes, string MediaType);
public sealed record CasePublicationRefV1(Guid PublicationId, string Version, Guid[] RecipientReportIds, Guid[] EvidenceIds);
public sealed record CaseReadFactV1(Guid CaseId, Guid ProjectId, string Version, string Status,
    CaseReportRefV1[] CurrentReports, ReportingSourceRefDto[] Conclusions, CasePublicationRefV1[] Publications);
public sealed record DefectReadFactV1(Guid DefectId, Guid ProjectId, string Version, string Status,
    string SourceKind, Guid SourceId, string SourceVersion, Guid? RouteVersionId, Guid? SegmentSetId,
    Guid? SegmentId, string? GeometryVersion, string PositionStatus);
public sealed record CaseDefectSnapshotV1(string SchemaVersion, Guid SnapshotId, Guid ProjectId,
    DateTimeOffset CapturedAt, string Hash, CaseReadFactV1[] Cases, DefectReadFactV1[] Defects,
    CaseEvidenceRefV1[] AuthorizedEvidence, string[] MissingReasons);
