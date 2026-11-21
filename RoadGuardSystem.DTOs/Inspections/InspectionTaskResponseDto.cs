namespace RoadGuardSystem.DTOs.Inspections;

public sealed record InspectionTaskResponseDto(
    Guid Id,
    Guid ProjectId,
    IReadOnlyList<Guid> DefectIds,
    string? Mode,
    Guid CrewId,
    Guid? PolicyVersionId,
    string? Status,
    string Version, string? TaskCode = null, DateTimeOffset? DueAt = null, Guid? AssignmentId = null, string? AssignmentStatus = null, string? AcceptanceOutcome = null, DateTimeOffset? AcceptedAt = null, DateTimeOffset? RejectedAt = null, bool IsHistory = false);
