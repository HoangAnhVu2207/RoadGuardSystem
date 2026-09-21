namespace RoadGuardSystem.Services.Projects;

public sealed record CreateRoadSectionVersionCommand(
    Guid ProjectId,
    Guid RoadSectionId,
    int Srid,
    IReadOnlyList<RoadSectionCoordinate> Coordinates,
    DateTimeOffset EffectiveFrom,
    string ChangeReason,
    Guid ExpectedCurrentVersionId,
    Guid OperationId,
    Guid? CorrelationId);
