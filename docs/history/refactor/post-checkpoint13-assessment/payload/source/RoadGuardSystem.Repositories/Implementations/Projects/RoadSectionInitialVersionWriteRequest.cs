using NetTopologySuite.Geometries;

namespace RoadGuardSystem.Repositories.Projects;

public sealed record RoadSectionInitialVersionWriteRequest(
    Guid ActorUserId,
    Guid ProjectId,
    string Code,
    string? Name,
    LineString Geometry,
    DateTimeOffset EffectiveFrom,
    string ChangeReason,
    Guid OperationId,
    Guid? CorrelationId);
