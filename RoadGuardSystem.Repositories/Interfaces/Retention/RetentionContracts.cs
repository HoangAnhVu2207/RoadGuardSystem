using RoadGuardSystem.DTOs.Retention;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Repositories.Retention;

public sealed record RetentionInventory(Guid FileId, string Version, bool Complete,
    IReadOnlyList<RetentionReferenceView> References, IReadOnlyList<RetentionWarrantyView> Warranties,
    IReadOnlyList<string> ReasonCodes, IReadOnlyList<Guid> PublicProjectIds,
    string Classification = "EVIDENCE", IReadOnlyList<DateTimeOffset?>? AdditionalEligibleAfter = null);
public sealed record RetentionInventoryContribution(string Name, bool Complete,
    IReadOnlyList<RetentionReferenceView> References, IReadOnlyList<string> ReasonCodes,
    IReadOnlyList<DateTimeOffset?>? EligibleAfter = null);
// Contributors must use the same scoped DbContext and participate in the caller's SQL transaction.
public interface IRetentionInventoryContributor
{
    string Name { get; }
    Task<RetentionInventoryContribution> ReadAsync(Guid fileId, CancellationToken token);
    Task<IReadOnlyList<Guid>> KnownProjectFilesAsync(Guid projectId, CancellationToken token);
}
public interface IRetentionInventoryRepository
{
    Task<RetentionInventory?> ReadAsync(Guid fileId, CancellationToken token);
    Task<IReadOnlyList<Guid>> KnownProjectFilesAsync(Guid projectId, CancellationToken token);
}
public sealed class RetentionRequestException(int status, string code) : Exception(code)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
public interface IRetentionRepository
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
