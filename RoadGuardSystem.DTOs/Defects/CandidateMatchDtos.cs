namespace RoadGuardSystem.DTOs.Defects;

public sealed record CandidateMatchSourceDto(string Kind, Guid Id, string Version);
public sealed record CandidateMatchHistoryDto(int SourceLinkCount);
public sealed record CandidateMatchItemDto(Guid DefectId, string Version, Guid? SegmentId,
    int PriorityGroup, double? DistanceMeters, double? AccuracyMeters,
    IReadOnlyList<string> ReasonCodes, IReadOnlyList<Guid> EvidenceRefs,
    CandidateMatchHistoryDto HistorySummary);
public sealed record CandidateMatchPageDto(CandidateMatchSourceDto Source, string GeometryVersion,
    string AlgorithmVersion, IReadOnlyList<CandidateMatchItemDto> Items, string? NextCursor);
public sealed record CandidateMatchResult(int Status, string? Code = null, CandidateMatchPageDto? Page = null);
