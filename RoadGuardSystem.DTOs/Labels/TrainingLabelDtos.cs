using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Labels;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LabelAnnotationDto(string? Kind, string? CoordinateSpace, decimal X, decimal Y,
    decimal Width, decimal Height);

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
    string? ReviewReason, string Version);

public sealed record TrainingLabelResult(int Status, string? Code = null, TrainingLabelViewDto? Label = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);
public sealed record TrainingLabelPageDto(IReadOnlyList<TrainingLabelViewDto> Items, string? NextCursor);
public sealed record TrainingLabelPageResult(int Status, string? Code = null, TrainingLabelPageDto? Page = null);
