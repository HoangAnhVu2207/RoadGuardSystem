using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Defects;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CandidateClassificationDto(string? DefectTypeCode, string? CauseCategoryCode, string? Severity,
    Guid RoadSectionVersionId, Guid? SegmentId = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CandidateDecisionRequestDto(string? SourceKind, Guid SourceId, string? SourceVersion,
    string? GeometryVersion, string? Decision, Guid? TargetDefectId, string? TargetVersion,
    CandidateClassificationDto? Classification, string? Reason, Guid? SupersedesDecisionId = null, string? PreviousDecisionVersion = null);
public sealed record CandidateDecisionResponseDto(Guid Id, string SourceKind, Guid SourceId, string Decision,
    Guid? DefectId, string Version, Guid? SupersedesDecisionId);
