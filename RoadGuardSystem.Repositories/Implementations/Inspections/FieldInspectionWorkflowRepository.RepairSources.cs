using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Repositories.Inspections;

namespace RoadGuardSystem.Repositories.Implementations.Inspections;

public sealed partial class FieldInspectionWorkflowRepository
{
    // Uses the unchanged H3 source/location/assignee validators in the caller's atomic repair transaction.
    // It creates neither a generic FIELD task nor an execution grant.
    public async Task<RepairFieldSourceResult> ValidateRepairTaskSourcesInTransactionAsync(Guid projectId,
        RepairFieldTaskData input, Defect defect, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Caller-owned repair transaction required.");
        try
        {
            await ValidateSourceAsync(projectId, input, defect.SourceAIDetectionId, cancellationToken);
            await ValidatePinsAsync(projectId, input, cancellationToken);
            await GuardCrewAsync(projectId, input.AssignedToUserId, cancellationToken);
            return new(200);
        }
        catch (Denied denial) { return new(denial.Result.Status, denial.Result.Code); }
    }

    public async Task<RepairFieldSourceResult> ValidateRepairLocationPinsInTransactionAsync(Guid projectId,
        RepairFieldTaskData input, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Caller-owned repair transaction required.");
        try { await ValidatePinsAsync(projectId, input, cancellationToken); return new(200); }
        catch (Denied denial) { return new(denial.Result.Status, denial.Result.Code); }
    }
}
