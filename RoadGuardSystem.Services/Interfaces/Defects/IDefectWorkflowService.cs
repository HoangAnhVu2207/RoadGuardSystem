using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Defects;

namespace RoadGuardSystem.Services.Defects;

public interface IDefectWorkflowService
{
    Task<DefectWorkflowResult> ReadAsync(Guid actor, UserRoleCode role, Guid project,
        Guid defect, CancellationToken token);
    Task<DefectWorkflowResult> ListAsync(Guid actor, UserRoleCode role, Guid project,
        string? status, string? type, Guid? segment, int pageSize, string? cursor,
        CancellationToken token);
    Task<DefectWorkflowResult> AssessAsync(Guid actor, UserRoleCode role, Guid project, Guid defect,
        DefectAssessmentRequestDto request, string key, string? ifMatch, Guid? correlation,
        CancellationToken token);
    Task<DefectWorkflowResult> VerifyAsync(Guid actor, UserRoleCode role, Guid project, Guid defect,
        DefectVerificationRequestDto request, string key, string? ifMatch, Guid? correlation,
        CancellationToken token);
}
