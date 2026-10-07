using System.Text.Json.Serialization;
namespace RoadGuardSystem.BusinessObjects.PersistenceFacts.Labels;

// Materialized persistence commands and snapshots, independent of public transport contracts.
public sealed record LabelAnnotationFact(string? Kind, string? CoordinateSpace, decimal X, decimal Y,
    decimal Width, decimal Height);

public sealed record TrainingLabelPageFact(IReadOnlyList<TrainingLabelViewFact> Items, string? NextCursor);

public sealed record TrainingLabelViewFact(Guid Id, Guid ProjectId, int Revision, string Status,
    string SourceKind, Guid SourceId, string SourceVersion, Guid FileId, string FileVersion,
    LabelAnnotationFact Annotation, string DefectTypeCode, Guid? ReviewedBy, DateTimeOffset? ReviewedAt,
    string? ReviewReason, string Version);
