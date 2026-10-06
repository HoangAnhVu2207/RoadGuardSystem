using RoadGuardSystem.Repositories.Inspections;

namespace RoadGuardSystem.Repositories.Offline;

public interface IOfflineRepairAdmissionValidator
{
    Task<OfflineFieldAdmissionFacts?> ValidateRepairAsync(OfflineOperationData operation, Guid projectId,
        FieldAdmissionContext admission, CancellationToken cancellationToken);
}
