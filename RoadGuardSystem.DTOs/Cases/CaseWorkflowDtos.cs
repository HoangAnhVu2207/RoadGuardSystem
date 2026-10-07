using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Cases;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CaseTriageDto(Guid ProjectId, string? VerificationMethod, string? Reason,
    Guid? RouteVersionId = null, Guid? SegmentSetId = null, string? GeometryVersion = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CaseLinkDto(IReadOnlyList<Guid>? ReportIds, IReadOnlyDictionary<Guid, string>? SourceCaseVersions, string? Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CaseSplitDto(IReadOnlyList<Guid>? ReportIds, string? Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CaseConclusionDto(string? Outcome, IReadOnlyList<Guid>? DefectIds, IReadOnlyList<Guid>? EvidenceIds, string? Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CasePublicationDto(IReadOnlyList<Guid>? ReportIds, IReadOnlyList<Guid>? DefectIds, IReadOnlyList<Guid>? EvidenceIds, string? Summary);
public sealed record InternalCaseDto(Guid Id, Guid? ProjectId, string Status, string Version,
    IReadOnlyList<Guid> ReportIds, IReadOnlyList<Guid> DefectIds, string? VerificationMethod, string WarrantyRouting,
    CaseConclusionViewDto? Conclusion, DateTimeOffset CreatedAt)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Cases.InternalCaseFact?(InternalCaseDto? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.Status, value.Version, value.ReportIds, value.DefectIds, value.VerificationMethod, value.WarrantyRouting, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Cases.CaseConclusionViewFact?)value.Conclusion, value.CreatedAt);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator InternalCaseDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Cases.InternalCaseFact? value)
        => value is null ? null! : new(value.Id, value.ProjectId, value.Status, value.Version, value.ReportIds, value.DefectIds, value.VerificationMethod, value.WarrantyRouting, (global::RoadGuardSystem.DTOs.Cases.CaseConclusionViewDto?)value.Conclusion, value.CreatedAt);
}
public sealed record CaseConclusionViewDto(Guid Id, string Outcome, string Reason, DateTimeOffset ConcludedAt,
    IReadOnlyList<Guid> DefectIds, IReadOnlyList<Guid> EvidenceIds)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Cases.CaseConclusionViewFact?(CaseConclusionViewDto? value)
        => value is null ? null! : new(value.Id, value.Outcome, value.Reason, value.ConcludedAt, value.DefectIds, value.EvidenceIds);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CaseConclusionViewDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Cases.CaseConclusionViewFact? value)
        => value is null ? null! : new(value.Id, value.Outcome, value.Reason, value.ConcludedAt, value.DefectIds, value.EvidenceIds);
}
public sealed record CasePublicationResponseDto(Guid Id, Guid CaseId, string Status, string Version)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Cases.CasePublicationResponseFact?(CasePublicationResponseDto? value)
        => value is null ? null! : new(value.Id, value.CaseId, value.Status, value.Version);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CasePublicationResponseDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Cases.CasePublicationResponseFact? value)
        => value is null ? null! : new(value.Id, value.CaseId, value.Status, value.Version);
}
public sealed record CasePageDto(IReadOnlyList<InternalCaseDto> Items, string? NextCursor)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Cases.CasePageFact?(CasePageDto? value)
        => value is null ? null! : new(value.Items?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Cases.InternalCaseFact)item).ToArray()!, value.NextCursor);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator CasePageDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Cases.CasePageFact? value)
        => value is null ? null! : new(value.Items?.Select(item => (global::RoadGuardSystem.DTOs.Cases.InternalCaseDto)item).ToArray()!, value.NextCursor);
}
