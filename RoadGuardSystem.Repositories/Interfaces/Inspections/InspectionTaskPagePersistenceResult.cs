namespace RoadGuardSystem.Repositories.Inspections;

public sealed record InspectionTaskPagePersistenceResult(
    IReadOnlyList<InspectionTaskPersistenceView> Items,
    DateTimeOffset? LastDueAt,
    Guid? LastId,
    bool HasMore,
    DateTimeOffset AsOf);
