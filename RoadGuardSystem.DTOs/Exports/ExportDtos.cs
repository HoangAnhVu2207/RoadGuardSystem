using System.Text.Json.Serialization;
using RoadGuardSystem.DTOs.Reporting;

namespace RoadGuardSystem.DTOs.Exports;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateExportRequestDto(string Kind, string Format, Guid[]? SegmentIds = null, Guid[]? DefectIds = null, DateTimeOffset? From = null, DateTimeOffset? To = null, bool IncludeOriginalFiles = false);
public sealed record ExportJobViewDto(Guid Id, Guid ProjectId, string Kind, string Format, string Status, Guid SnapshotId, string SnapshotHash, Guid? ArtifactId, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, DateTimeOffset? ExpiresAt, string? ErrorCode, string Completeness, string[] MissingSections, string Version);
public sealed record ExportSourceRevisionDto(string Kind, Guid Id, string Version);
public sealed record ExportFileDto(Guid FileId, string FileVersion, string Sha256, long SizeBytes, string MediaType, string? ArchivePath, bool Included, string? ReasonCode);
public sealed record ExportSectionDto(string Name, string Availability, string[] ReasonCodes);
public sealed record ExportLabelDto(Guid LabelId, int Revision, Guid RevisionId, Guid ProjectId, string TypeCode, decimal X, decimal Y, decimal Width, decimal Height, Guid FileId, string FileVersion, string Sha256, long SizeBytes, string MediaType, string SourceKind, Guid SourceId, string SourceVersion, Guid ApprovalId, Guid ApprovedBy, DateTimeOffset ApprovedAt, Guid? ProcessingJobId, Guid? ModelVersionId, Guid? DatasetVersionId, string Mode, Guid? SegmentId);
public sealed record ExportManifestV1Dto(string SchemaVersion, Guid SnapshotId, Guid ProjectId, string Kind, string Format, Guid RequestedBy, DateTimeOffset CreatedAt, DateTimeOffset SnapshotAt, string[] DefinitionVersions, CreateExportRequestDto Filters, ExportSourceRevisionDto[] SourceRevisions, ExportFileDto[] Files, ExportSectionDto[] Sections, ExportLabelDto[]? Labels, string SnapshotHash);
public sealed record ExportSnapshotPayloadDto(ExportManifestV1Dto Manifest, ReportingCaptureDto? Dossier);
