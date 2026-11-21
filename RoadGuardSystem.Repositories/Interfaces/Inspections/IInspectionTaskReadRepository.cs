namespace RoadGuardSystem.Repositories.Inspections;

public interface IInspectionTaskReadRepository
{
    Task<InspectionTaskPagePersistenceResult> ListAssignedAsync(
        Guid assignedToUserId,
        DateTimeOffset? afterDueAt,
        Guid? afterId,
        int limit,
        RoadGuardSystem.Repositories.Mobile.MobileTaskListFilter? filter = null, CancellationToken cancellationToken = default);
}
