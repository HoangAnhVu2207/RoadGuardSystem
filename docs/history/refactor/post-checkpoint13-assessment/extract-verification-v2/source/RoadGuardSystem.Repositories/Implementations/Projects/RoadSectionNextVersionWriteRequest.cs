using NetTopologySuite.Geometries;

namespace RoadGuardSystem.Repositories.Projects;

public sealed record RoadSectionNextVersionWriteRequest(
    Guid ActorUserId,
    Guid ProjectId,
    Guid RoadSectionId,
    Guid ExpectedCurrentVersionId,
    LineString Geometry,
    DateTimeOffset EffectiveFrom,
    string ChangeReason,
    Guid OperationId,
    Guid? CorrelationId);
