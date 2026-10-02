namespace RoadGuardSystem.Services.Projects;

public sealed record CreateRoadSectionCommand(
    Guid ProjectId,
    string Code,
    string? Name,
    int Srid,
    IReadOnlyList<RoadSectionCoordinate> Coordinates,
    DateTimeOffset EffectiveFrom,
    string ChangeReason,
    Guid OperationId,
    Guid? CorrelationId);
