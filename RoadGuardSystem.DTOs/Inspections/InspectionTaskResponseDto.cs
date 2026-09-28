namespace RoadGuardSystem.DTOs.Inspections;

public sealed record InspectionTaskResponseDto(
    Guid Id,
    Guid ProjectId,
    IReadOnlyList<Guid> DefectIds,
    string Mode,
    Guid CrewId,
    Guid? PolicyVersionId,
    string Status,
    string Version);
