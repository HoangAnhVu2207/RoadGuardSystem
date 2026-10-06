using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.DTOs.Defects;

namespace RoadGuardSystem.Repositories.Defects;

public interface IDefectWorkflowRepository
{
    Task<DefectViewDto> ApplyFieldVerificationAsync(Guid actor, Guid project, Guid defect, string expectedVersion,
        DefectVerificationAction action, RoadGuardSystem.DTOs.Inspections.FieldVerificationSourceFacts source,
        IReadOnlyCollection<Guid> evidenceIds, string reason, Guid? correlation, CancellationToken token)
        => throw new NotSupportedException("FIELD persistence is not available in this adapter.");
    Task<DefectViewDto?> ReadAsync(Guid project, Guid defect,
        Func<CancellationToken, Task> guard, CancellationToken token);
    Task<DefectPageDto> ListAsync(Guid project, DefectStatus? status, string? type,
        Guid? segment, int pageSize, Guid? afterId,
        Func<CancellationToken, Task> guard, CancellationToken token);
    Task<IReadOnlyList<Guid>> LinkedReportsAsync(Guid project, Guid defect, CancellationToken token);
    Task LockTargetAsync(Guid project, Guid defect, CancellationToken token);
    Task<DefectViewDto> ApplyAssessmentAsync(Guid actor, Guid project, Guid defect,
        string expectedVersion, string type, string? cause, DefectSeverity severity,
        IReadOnlyCollection<Guid> evidenceIds, string reason, Guid? correlation, CancellationToken token);
    Task<DefectViewDto> ApplyVerificationAsync(Guid actor, Guid project, Guid defect,
        string expectedVersion, DefectVerificationAction action, IReadOnlyCollection<Guid> evidenceIds,
        IReadOnlyCollection<Guid> verifiedRelatedEvidenceIds, string reason, Guid? correlation,
        CancellationToken token);
}
