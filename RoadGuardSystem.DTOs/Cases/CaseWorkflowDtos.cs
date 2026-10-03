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
    CaseConclusionViewDto? Conclusion, DateTimeOffset CreatedAt);
public sealed record CaseConclusionViewDto(Guid Id, string Outcome, string Reason, DateTimeOffset ConcludedAt,
    IReadOnlyList<Guid> DefectIds, IReadOnlyList<Guid> EvidenceIds);
public sealed record CasePublicationResponseDto(Guid Id, Guid CaseId, string Status, string Version);
public sealed record CasePageDto(IReadOnlyList<InternalCaseDto> Items, string? NextCursor);
