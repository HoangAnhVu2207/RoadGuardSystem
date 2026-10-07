using System.Text.Json.Serialization;
using RoadGuardSystem.DTOs.Reporting;

namespace RoadGuardSystem.DTOs.Exports;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateExportRequestDto(string Kind, string Format, Guid[]? SegmentIds = null, Guid[]? DefectIds = null, DateTimeOffset? From = null, DateTimeOffset? To = null, bool IncludeOriginalFiles = false)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.CreateExportRequestFact?(CreateExportRequestDto? value)
        => value is null ? null! : new(value.Kind, value.Format, value.SegmentIds, value.DefectIds, value.From, value.To, value.IncludeOriginalFiles);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CreateExportRequestDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.CreateExportRequestFact? value)
        => value is null ? null! : new(value.Kind, value.Format, value.SegmentIds, value.DefectIds, value.From, value.To, value.IncludeOriginalFiles);
}
public sealed record ExportJobViewDto(Guid Id, Guid ProjectId, string Kind, string Format, string Status, Guid SnapshotId, string SnapshotHash, Guid? ArtifactId, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, DateTimeOffset? ExpiresAt, string? ErrorCode, string Completeness, string[] MissingSections, string Version);
public sealed record ExportSourceRevisionDto(string Kind, Guid Id, string Version)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportSourceRevisionFact?(ExportSourceRevisionDto? value)
        => value is null ? null! : new(value.Kind, value.Id, value.Version);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ExportSourceRevisionDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportSourceRevisionFact? value)
        => value is null ? null! : new(value.Kind, value.Id, value.Version);
}
public sealed record ExportFileDto(Guid FileId, string FileVersion, string Sha256, long SizeBytes, string MediaType, string? ArchivePath, bool Included, string? ReasonCode)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportFileFact?(ExportFileDto? value)
        => value is null ? null! : new(value.FileId, value.FileVersion, value.Sha256, value.SizeBytes, value.MediaType, value.ArchivePath, value.Included, value.ReasonCode);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ExportFileDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportFileFact? value)
        => value is null ? null! : new(value.FileId, value.FileVersion, value.Sha256, value.SizeBytes, value.MediaType, value.ArchivePath, value.Included, value.ReasonCode);
}
public sealed record ExportSectionDto(string Name, string Availability, string[] ReasonCodes)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportSectionFact?(ExportSectionDto? value)
        => value is null ? null! : new(value.Name, value.Availability, value.ReasonCodes);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ExportSectionDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportSectionFact? value)
        => value is null ? null! : new(value.Name, value.Availability, value.ReasonCodes);
}
public sealed record ExportLabelDto(Guid LabelId, int Revision, Guid RevisionId, Guid ProjectId, string TypeCode, decimal X, decimal Y, decimal Width, decimal Height, Guid FileId, string FileVersion, string Sha256, long SizeBytes, string MediaType, string SourceKind, Guid SourceId, string SourceVersion, Guid ApprovalId, Guid ApprovedBy, DateTimeOffset ApprovedAt, Guid? ProcessingJobId, Guid? ModelVersionId, Guid? DatasetVersionId, string Mode, Guid? SegmentId)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportLabelFact?(ExportLabelDto? value)
        => value is null ? null! : new(value.LabelId, value.Revision, value.RevisionId, value.ProjectId, value.TypeCode, value.X, value.Y, value.Width, value.Height, value.FileId, value.FileVersion, value.Sha256, value.SizeBytes, value.MediaType, value.SourceKind, value.SourceId, value.SourceVersion, value.ApprovalId, value.ApprovedBy, value.ApprovedAt, value.ProcessingJobId, value.ModelVersionId, value.DatasetVersionId, value.Mode, value.SegmentId);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ExportLabelDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportLabelFact? value)
        => value is null ? null! : new(value.LabelId, value.Revision, value.RevisionId, value.ProjectId, value.TypeCode, value.X, value.Y, value.Width, value.Height, value.FileId, value.FileVersion, value.Sha256, value.SizeBytes, value.MediaType, value.SourceKind, value.SourceId, value.SourceVersion, value.ApprovalId, value.ApprovedBy, value.ApprovedAt, value.ProcessingJobId, value.ModelVersionId, value.DatasetVersionId, value.Mode, value.SegmentId);
}
public sealed record ExportManifestV1Dto(string SchemaVersion, Guid SnapshotId, Guid ProjectId, string Kind, string Format, Guid RequestedBy, DateTimeOffset CreatedAt, DateTimeOffset SnapshotAt, string[] DefinitionVersions, CreateExportRequestDto Filters, ExportSourceRevisionDto[] SourceRevisions, ExportFileDto[] Files, ExportSectionDto[] Sections, ExportLabelDto[]? Labels, string SnapshotHash)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportManifestV1Fact?(ExportManifestV1Dto? value)
        => value is null ? null! : new(value.SchemaVersion, value.SnapshotId, value.ProjectId, value.Kind, value.Format, value.RequestedBy, value.CreatedAt, value.SnapshotAt, value.DefinitionVersions, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.CreateExportRequestFact)value.Filters, value.SourceRevisions?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportSourceRevisionFact)item).ToArray()!, value.Files?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportFileFact)item).ToArray()!, value.Sections?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportSectionFact)item).ToArray()!, value.Labels?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportLabelFact)item).ToArray(), value.SnapshotHash);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ExportManifestV1Dto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportManifestV1Fact? value)
        => value is null ? null! : new(value.SchemaVersion, value.SnapshotId, value.ProjectId, value.Kind, value.Format, value.RequestedBy, value.CreatedAt, value.SnapshotAt, value.DefinitionVersions, (global::RoadGuardSystem.DTOs.Exports.CreateExportRequestDto)value.Filters, value.SourceRevisions?.Select(item => (global::RoadGuardSystem.DTOs.Exports.ExportSourceRevisionDto)item).ToArray()!, value.Files?.Select(item => (global::RoadGuardSystem.DTOs.Exports.ExportFileDto)item).ToArray()!, value.Sections?.Select(item => (global::RoadGuardSystem.DTOs.Exports.ExportSectionDto)item).ToArray()!, value.Labels?.Select(item => (global::RoadGuardSystem.DTOs.Exports.ExportLabelDto)item).ToArray(), value.SnapshotHash);
}
public sealed record ExportSnapshotPayloadDto(ExportManifestV1Dto Manifest, ReportingCaptureDto? Dossier)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportSnapshotPayloadFact?(ExportSnapshotPayloadDto? value)
        => value is null ? null! : new((global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportManifestV1Fact)value.Manifest, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingCaptureFact?)value.Dossier);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ExportSnapshotPayloadDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports.ExportSnapshotPayloadFact? value)
        => value is null ? null! : new((global::RoadGuardSystem.DTOs.Exports.ExportManifestV1Dto)value.Manifest, (global::RoadGuardSystem.DTOs.Reporting.ReportingCaptureDto?)value.Dossier);
}
