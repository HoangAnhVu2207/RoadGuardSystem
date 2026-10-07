using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Labels;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LabelAnnotationDto(string? Kind, string? CoordinateSpace, decimal X, decimal Y,
    decimal Width, decimal Height)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Labels.LabelAnnotationFact?(LabelAnnotationDto? value)
        => value is null ? null! : new(value.Kind, value.CoordinateSpace, value.X, value.Y, value.Width, value.Height);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator LabelAnnotationDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Labels.LabelAnnotationFact? value)
        => value is null ? null! : new(value.Kind, value.CoordinateSpace, value.X, value.Y, value.Width, value.Height);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateTrainingLabelDto(string? SourceKind, Guid SourceId, string? SourceVersion,
    Guid FileId, LabelAnnotationDto? Annotation, string? DefectTypeCode, string? Reason);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReviseTrainingLabelDto(Guid FileId, LabelAnnotationDto? Annotation,
    string? DefectTypeCode, string? Reason);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReviewTrainingLabelDto(string? Decision, string? Reason);

public sealed record TrainingLabelViewDto(Guid Id, Guid ProjectId, int Revision, string Status,
    string SourceKind, Guid SourceId, string SourceVersion, Guid FileId, string FileVersion,
    LabelAnnotationDto Annotation, string DefectTypeCode, Guid? ReviewedBy, DateTimeOffset? ReviewedAt,
    string? ReviewReason, string Version)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Labels.TrainingLabelViewFact?(TrainingLabelViewDto? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.Revision, value.Status, value.SourceKind, value.SourceId, value.SourceVersion, value.FileId, value.FileVersion, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Labels.LabelAnnotationFact)value.Annotation, value.DefectTypeCode, value.ReviewedBy, value.ReviewedAt, value.ReviewReason, value.Version);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator TrainingLabelViewDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Labels.TrainingLabelViewFact? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.Revision, value.Status, value.SourceKind, value.SourceId, value.SourceVersion, value.FileId, value.FileVersion, (global::RoadGuardSystem.DTOs.Labels.LabelAnnotationDto)value.Annotation, value.DefectTypeCode, value.ReviewedBy, value.ReviewedAt, value.ReviewReason, value.Version);
}

public sealed record TrainingLabelResult(int Status, string? Code = null, TrainingLabelViewDto? Label = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);
public sealed record TrainingLabelPageDto(IReadOnlyList<TrainingLabelViewDto> Items, string? NextCursor)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Labels.TrainingLabelPageFact?(TrainingLabelPageDto? value)
        => value is null ? null! : new(value.Items?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Labels.TrainingLabelViewFact)item).ToArray()!, value.NextCursor);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator TrainingLabelPageDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Labels.TrainingLabelPageFact? value)
        => value is null ? null! : new(value.Items?.Select(item => (global::RoadGuardSystem.DTOs.Labels.TrainingLabelViewDto)item).ToArray()!, value.NextCursor);
}
public sealed record TrainingLabelPageResult(int Status, string? Code = null, TrainingLabelPageDto? Page = null);
