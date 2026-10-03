using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Defects;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DefectAssessmentRequestDto(string? DefectTypeCode, string? CauseCategoryCode,
    string? Severity, string? Reason, Guid[]? EvidenceIds);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DefectVerificationRequestDto(string? Decision, string? VerificationMethod,
    Guid[]? EvidenceIds, string? Reason);

public sealed record DefectGeometryDto(int Srid, string Wkt);
public sealed record DefectViewDto(Guid Id, Guid ProjectId, Guid? RoadSectionVersionId,
    Guid? SegmentId, string DefectTypeCode, string? CauseCategoryCode, string Severity,
    string Status, DefectGeometryDto? Geometry, string Version);
public sealed record DefectPageDto(IReadOnlyList<DefectViewDto> Items, string? NextCursor);
public sealed record DefectWorkflowResult(int Status, string? Code = null, DefectViewDto? Defect = null,
    DefectPageDto? Page = null, IReadOnlyDictionary<string, string[]>? Errors = null);
