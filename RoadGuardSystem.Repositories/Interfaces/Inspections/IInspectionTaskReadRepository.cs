namespace RoadGuardSystem.Repositories.Inspections;

public interface IInspectionTaskReadRepository
{
    Task<InspectionTaskPagePersistenceResult> ListAssignedAsync(
        Guid assignedToUserId,
        DateTimeOffset? afterDueAt,
        Guid? afterId,
        int limit,
        CancellationToken cancellationToken = default);
}
