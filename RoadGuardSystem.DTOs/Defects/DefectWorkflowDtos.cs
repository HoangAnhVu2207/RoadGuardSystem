using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Defects;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DefectAssessmentRequestDto(string? DefectTypeCode, string? CauseCategoryCode,
    string? Severity, string? Reason, Guid[]? EvidenceIds);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DefectVerificationRequestDto(string? Decision, string? VerificationMethod,
    Guid[]? EvidenceIds, string? Reason, Guid? FieldTaskId = null, Guid? FieldSubmissionId = null, string? FieldContentHash = null);

public sealed record DefectGeometryDto(int Srid, string Wkt)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Defects.DefectGeometryFact?(DefectGeometryDto? value)
        => value is null ? null! : new(value.Srid, value.Wkt);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator DefectGeometryDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Defects.DefectGeometryFact? value)
        => value is null ? null! : new(value.Srid, value.Wkt);
}
public sealed record DefectViewDto(Guid Id, Guid ProjectId, Guid? RoadSectionVersionId,
    Guid? SegmentId, string DefectTypeCode, string? CauseCategoryCode, string Severity,
    string Status, DefectGeometryDto? Geometry, string Version)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Defects.DefectViewFact?(DefectViewDto? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.RoadSectionVersionId, value.SegmentId, value.DefectTypeCode, value.CauseCategoryCode, value.Severity, value.Status, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Defects.DefectGeometryFact?)value.Geometry, value.Version);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator DefectViewDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Defects.DefectViewFact? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.RoadSectionVersionId, value.SegmentId, value.DefectTypeCode, value.CauseCategoryCode, value.Severity, value.Status, (global::RoadGuardSystem.DTOs.Defects.DefectGeometryDto?)value.Geometry, value.Version);
}
public sealed record DefectPageDto(IReadOnlyList<DefectViewDto> Items, string? NextCursor)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Defects.DefectPageFact?(DefectPageDto? value)
        => value is null ? null! : new(value.Items?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Defects.DefectViewFact)item).ToArray()!, value.NextCursor);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator DefectPageDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Defects.DefectPageFact? value)
        => value is null ? null! : new(value.Items?.Select(item => (global::RoadGuardSystem.DTOs.Defects.DefectViewDto)item).ToArray()!, value.NextCursor);
}
public sealed record DefectWorkflowResult(int Status, string? Code = null, DefectViewDto? Defect = null,
    DefectPageDto? Page = null, IReadOnlyDictionary<string, string[]>? Errors = null);
