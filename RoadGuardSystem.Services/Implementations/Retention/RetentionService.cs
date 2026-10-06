using RoadGuardSystem.DTOs.Retention;
using RoadGuardSystem.Repositories.Retention;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Services.Retention;

public sealed class RetentionService(IRetentionRepository repository) : IRetentionService
{
    public Task AuthorizePrincipalAsync(Guid actor, UserRoleCode role, CancellationToken token) => repository.AuthorizePrincipalAsync(actor, role, token);
    public Task AuthorizeAsync(Guid actor, Guid? project, bool supervisorOnly, CancellationToken token) => repository.AuthorizeAsync(actor, project, supervisorOnly, token);
    public Task<RetentionFileView> GetFileAsync(Guid actor, Guid project, Guid file, CancellationToken token) => repository.GetFileAsync(actor, project, file, token);
    public Task<RetentionBasisView> ConfirmBasisAsync(Guid actor, Guid project, Guid file, ConfirmRetentionBasisRequest request, string key, string expected, CancellationToken token)
    {
        Reason(request.Reason); Key(key); Version(expected);
        if (request.WarrantyIds is null || request.WarrantyIds.Distinct().Count() != request.WarrantyIds.Length || request.WarrantyIds.Contains(Guid.Empty) || string.IsNullOrWhiteSpace(request.ExpectedReferenceInventoryVersion)) throw new RetentionRequestException(422, "basis_invalid");
        return repository.ConfirmBasisAsync(actor, project, file, request, key, expected, token);
    }
    public Task<RetentionHoldView> CreateHoldAsync(Guid actor, CreateRetentionHoldRequest request, string key, CancellationToken token)
    {
        Reason(request.Reason); Key(key);
        if (request.ScopeType is not ("PROJECT" or "FILE") || request.ScopeId == Guid.Empty) throw new RetentionRequestException(422, "validation_error");
        return repository.CreateHoldAsync(actor, request, key, token);
    }
    public Task<RetentionHoldView> GetHoldAsync(Guid actor, Guid holdId, CancellationToken token) => repository.GetHoldAsync(actor, holdId, token);
    public Task<RetentionHoldView> ReleaseHoldAsync(Guid actor, Guid holdId, ReleaseRetentionHoldRequest request, string key, string expected, CancellationToken token)
    {
        Reason(request.Reason); Key(key); Version(expected);
        return repository.ReleaseHoldAsync(actor, holdId, request, key, expected, token);
    }
    public Task<RetentionEvaluationView> AdmitEvaluationAsync(Guid actor, Guid project, CreateRetentionEvaluationRequest request, string key, CancellationToken token)
    {
        Key(key);
        if (request.FileIds is { } ids && (ids.Length == 0 || ids.Length > 500 || ids.Distinct().Count() != ids.Length || ids.Contains(Guid.Empty))) throw new RetentionRequestException(422, "validation_error");
        return repository.AdmitEvaluationAsync(actor, project, request, key, token);
    }
    public Task<RetentionEvaluationView> GetEvaluationAsync(Guid actor, Guid project, Guid id, Guid? afterFile, int pageSize, CancellationToken token)
    {
        if (pageSize is < 1 or > 200) throw new RetentionRequestException(422, "validation_error");
        return repository.GetEvaluationAsync(actor, project, id, afterFile, pageSize, token);
    }
    public Task<bool> ProcessOneAsync(CancellationToken token) => repository.ProcessOneAsync(token);
    private static void Reason(string? reason) { if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000) throw new RetentionRequestException(422, "validation_error"); }
    private static void Key(string key) { if (string.IsNullOrWhiteSpace(key)) throw new RetentionRequestException(428, "validation_error"); if (key.Length > 128) throw new RetentionRequestException(422, "validation_error"); }
    private static void Version(string version) { if (string.IsNullOrWhiteSpace(version)) throw new RetentionRequestException(428, "validation_error"); }
}
