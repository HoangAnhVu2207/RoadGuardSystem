using RoadGuardSystem.DTOs.Retention;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Services.Retention;

public interface IRetentionService
{
    Task AuthorizePrincipalAsync(Guid actor, UserRoleCode role, CancellationToken token);
    Task AuthorizeAsync(Guid actor, Guid? project, bool supervisorOnly, CancellationToken token);
    Task<RetentionFileView> GetFileAsync(Guid actor, Guid project, Guid file, CancellationToken token);
    Task<RetentionBasisView> ConfirmBasisAsync(Guid actor, Guid project, Guid file, ConfirmRetentionBasisRequest request, string key, string expected, CancellationToken token);
    Task<RetentionHoldView> CreateHoldAsync(Guid actor, CreateRetentionHoldRequest request, string key, CancellationToken token);
    Task<RetentionHoldView> GetHoldAsync(Guid actor, Guid holdId, CancellationToken token);
    Task<RetentionHoldView> ReleaseHoldAsync(Guid actor, Guid holdId, ReleaseRetentionHoldRequest request, string key, string expected, CancellationToken token);
    Task<RetentionEvaluationView> AdmitEvaluationAsync(Guid actor, Guid project, CreateRetentionEvaluationRequest request, string key, CancellationToken token);
    Task<RetentionEvaluationView> GetEvaluationAsync(Guid actor, Guid project, Guid id, Guid? afterFile, int pageSize, CancellationToken token);
    Task<bool> ProcessOneAsync(CancellationToken token);
}