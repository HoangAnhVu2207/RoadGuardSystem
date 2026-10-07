using RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting;
using System.Text.Json.Serialization;
namespace RoadGuardSystem.BusinessObjects.PersistenceFacts.Exports;

// Materialized persistence commands and snapshots, independent of public transport contracts.
public sealed record CreateExportRequestFact(string Kind, string Format, Guid[]? SegmentIds = null, Guid[]? DefectIds = null, DateTimeOffset? From = null, DateTimeOffset? To = null, bool IncludeOriginalFiles = false);

public sealed record ExportFileFact(Guid FileId, string FileVersion, string Sha256, long SizeBytes, string MediaType, string? ArchivePath, bool Included, string? ReasonCode);

public sealed record ExportLabelFact(Guid LabelId, int Revision, Guid RevisionId, Guid ProjectId, string TypeCode, decimal X, decimal Y, decimal Width, decimal Height, Guid FileId, string FileVersion, string Sha256, long SizeBytes, string MediaType, string SourceKind, Guid SourceId, string SourceVersion, Guid ApprovalId, Guid ApprovedBy, DateTimeOffset ApprovedAt, Guid? ProcessingJobId, Guid? ModelVersionId, Guid? DatasetVersionId, string Mode, Guid? SegmentId);

public sealed record ExportManifestV1Fact(string SchemaVersion, Guid SnapshotId, Guid ProjectId, string Kind, string Format, Guid RequestedBy, DateTimeOffset CreatedAt, DateTimeOffset SnapshotAt, string[] DefinitionVersions, CreateExportRequestFact Filters, ExportSourceRevisionFact[] SourceRevisions, ExportFileFact[] Files, ExportSectionFact[] Sections, ExportLabelFact[]? Labels, string SnapshotHash);

public sealed record ExportSectionFact(string Name, string Availability, string[] ReasonCodes);

public sealed record ExportSnapshotPayloadFact(ExportManifestV1Fact Manifest, ReportingCaptureFact? Dossier);

public sealed record ExportSourceRevisionFact(string Kind, Guid Id, string Version);
