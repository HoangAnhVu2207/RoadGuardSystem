using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Defects;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CandidateClassificationDto(string? DefectTypeCode, string? CauseCategoryCode, string? Severity,
    Guid RoadSectionVersionId, Guid? SegmentId = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CandidateDecisionRequestDto(string? SourceKind, Guid SourceId, string? SourceVersion,
    string? GeometryVersion, string? Decision, Guid? TargetDefectId, string? TargetVersion,
    CandidateClassificationDto? Classification, string? Reason, Guid? SupersedesDecisionId = null, string? PreviousDecisionVersion = null,
    CandidateRecurrenceDto? Recurrence = null);
public sealed record CandidateRecurrenceDto(Guid PreviousDefectId, Guid PriorRepairDecisionId, Guid[] EvidenceFileIds, string? ExpectedLifecycleVersion = null);
public sealed record CandidateDecisionResponseDto(Guid Id, string SourceKind, Guid SourceId, string Decision,
    Guid? DefectId, string Version, Guid? SupersedesDecisionId)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Defects.CandidateDecisionResponseFact?(CandidateDecisionResponseDto? value)
        => value is null ? null! : new(value.Id, value.SourceKind, value.SourceId, value.Decision, value.DefectId, value.Version, value.SupersedesDecisionId);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CandidateDecisionResponseDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Defects.CandidateDecisionResponseFact? value)
        => value is null ? null! : new(value.Id, value.SourceKind, value.SourceId, value.Decision, value.DefectId, value.Version, value.SupersedesDecisionId);
}
