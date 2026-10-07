namespace RoadGuardSystem.DTOs.Reporting;

public sealed record CaseReportRefV1(Guid ReportId, string Version, DateTimeOffset ReceivedAt)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CaseReportRefV1Fact?(CaseReportRefV1? value)
        => value is null ? null! : new(value.ReportId, value.Version, value.ReceivedAt);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CaseReportRefV1?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CaseReportRefV1Fact? value)
        => value is null ? null! : new(value.ReportId, value.Version, value.ReceivedAt);
}
public sealed record CaseEvidenceRefV1(Guid CaseId, Guid SourceReportId, Guid EvidenceId,
    Guid FileId, string FileVersion, string Sha256, long SizeBytes, string MediaType)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CaseEvidenceRefV1Fact?(CaseEvidenceRefV1? value)
        => value is null ? null! : new(value.CaseId, value.SourceReportId, value.EvidenceId, value.FileId, value.FileVersion, value.Sha256, value.SizeBytes, value.MediaType);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CaseEvidenceRefV1?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CaseEvidenceRefV1Fact? value)
        => value is null ? null! : new(value.CaseId, value.SourceReportId, value.EvidenceId, value.FileId, value.FileVersion, value.Sha256, value.SizeBytes, value.MediaType);
}
public sealed record CasePublicationRefV1(Guid PublicationId, string Version, Guid[] RecipientReportIds, Guid[] EvidenceIds)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CasePublicationRefV1Fact?(CasePublicationRefV1? value)
        => value is null ? null! : new(value.PublicationId, value.Version, value.RecipientReportIds, value.EvidenceIds);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CasePublicationRefV1?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CasePublicationRefV1Fact? value)
        => value is null ? null! : new(value.PublicationId, value.Version, value.RecipientReportIds, value.EvidenceIds);
}
public sealed record CaseReadFactV1(Guid CaseId, Guid ProjectId, string Version, string Status,
    CaseReportRefV1[] CurrentReports, ReportingSourceRefDto[] Conclusions, CasePublicationRefV1[] Publications)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CaseReadFactV1Fact?(CaseReadFactV1? value)
        => value is null ? null! : new(value.CaseId, value.ProjectId, value.Version, value.Status, value.CurrentReports?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CaseReportRefV1Fact)item).ToArray()!, value.Conclusions?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingSourceRefFact)item).ToArray()!, value.Publications?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CasePublicationRefV1Fact)item).ToArray()!);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CaseReadFactV1?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CaseReadFactV1Fact? value)
        => value is null ? null! : new(value.CaseId, value.ProjectId, value.Version, value.Status, value.CurrentReports?.Select(item => (global::RoadGuardSystem.DTOs.Reporting.CaseReportRefV1)item).ToArray()!, value.Conclusions?.Select(item => (global::RoadGuardSystem.DTOs.Reporting.ReportingSourceRefDto)item).ToArray()!, value.Publications?.Select(item => (global::RoadGuardSystem.DTOs.Reporting.CasePublicationRefV1)item).ToArray()!);
}
public sealed record DefectReadFactV1(Guid DefectId, Guid ProjectId, string Version, string Status,
    string SourceKind, Guid SourceId, string SourceVersion, Guid? RouteVersionId, Guid? SegmentSetId,
    Guid? SegmentId, string? GeometryVersion, string PositionStatus)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.DefectReadFactV1Fact?(DefectReadFactV1? value)
        => value is null ? null! : new(value.DefectId, value.ProjectId, value.Version, value.Status, value.SourceKind, value.SourceId, value.SourceVersion, value.RouteVersionId, value.SegmentSetId, value.SegmentId, value.GeometryVersion, value.PositionStatus);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator DefectReadFactV1?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.DefectReadFactV1Fact? value)
        => value is null ? null! : new(value.DefectId, value.ProjectId, value.Version, value.Status, value.SourceKind, value.SourceId, value.SourceVersion, value.RouteVersionId, value.SegmentSetId, value.SegmentId, value.GeometryVersion, value.PositionStatus);
}
public sealed record CaseDefectSnapshotV1(string SchemaVersion, Guid SnapshotId, Guid ProjectId,
    DateTimeOffset CapturedAt, string Hash, CaseReadFactV1[] Cases, DefectReadFactV1[] Defects,
    CaseEvidenceRefV1[] AuthorizedEvidence, string[] MissingReasons)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CaseDefectSnapshotV1Fact?(CaseDefectSnapshotV1? value)
        => value is null ? null! : new(value.SchemaVersion, value.SnapshotId, value.ProjectId, value.CapturedAt, value.Hash, value.Cases?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CaseReadFactV1Fact)item).ToArray()!, value.Defects?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.DefectReadFactV1Fact)item).ToArray()!, value.AuthorizedEvidence?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CaseEvidenceRefV1Fact)item).ToArray()!, value.MissingReasons);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CaseDefectSnapshotV1?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CaseDefectSnapshotV1Fact? value)
        => value is null ? null! : new(value.SchemaVersion, value.SnapshotId, value.ProjectId, value.CapturedAt, value.Hash, value.Cases?.Select(item => (global::RoadGuardSystem.DTOs.Reporting.CaseReadFactV1)item).ToArray()!, value.Defects?.Select(item => (global::RoadGuardSystem.DTOs.Reporting.DefectReadFactV1)item).ToArray()!, value.AuthorizedEvidence?.Select(item => (global::RoadGuardSystem.DTOs.Reporting.CaseEvidenceRefV1)item).ToArray()!, value.MissingReasons);
}
