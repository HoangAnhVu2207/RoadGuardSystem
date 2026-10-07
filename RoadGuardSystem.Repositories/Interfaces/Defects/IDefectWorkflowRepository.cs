using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Defects;

namespace RoadGuardSystem.Repositories.Defects;

public interface IDefectWorkflowRepository
{
    Task<DefectViewFact> ApplyFieldVerificationAsync(Guid actor, Guid project, Guid defect, string expectedVersion,
        DefectVerificationAction action, RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldVerificationSourceFactsFact source,
        IReadOnlyCollection<Guid> evidenceIds, string reason, Guid? correlation, CancellationToken token)
        => throw new NotSupportedException("FIELD persistence is not available in this adapter.");
    Task<DefectViewFact?> ReadAsync(Guid project, Guid defect,
        Func<CancellationToken, Task> guard, CancellationToken token);
    Task<DefectPageFact> ListAsync(Guid project, DefectStatus? status, string? type,
        Guid? segment, int pageSize, Guid? afterId,
        Func<CancellationToken, Task> guard, CancellationToken token);
    Task<IReadOnlyList<Guid>> LinkedReportsAsync(Guid project, Guid defect, CancellationToken token);
    Task LockTargetAsync(Guid project, Guid defect, CancellationToken token);
    Task<DefectViewFact> ApplyAssessmentAsync(Guid actor, Guid project, Guid defect,
        string expectedVersion, string type, string? cause, DefectSeverity severity,
        IReadOnlyCollection<Guid> evidenceIds, string reason, Guid? correlation, CancellationToken token);
    Task<DefectViewFact> ApplyVerificationAsync(Guid actor, Guid project, Guid defect,
        string expectedVersion, DefectVerificationAction action, IReadOnlyCollection<Guid> evidenceIds,
        IReadOnlyCollection<Guid> verifiedRelatedEvidenceIds, string reason, Guid? correlation,
        CancellationToken token);
}
