using RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Repositories.Retention;

public sealed record RetentionInventory(Guid FileId, string Version, bool Complete,
    IReadOnlyList<RetentionReferenceViewFact> References, IReadOnlyList<RetentionWarrantyViewFact> Warranties,
    IReadOnlyList<string> ReasonCodes, IReadOnlyList<Guid> PublicProjectIds,
    string Classification = "EVIDENCE", IReadOnlyList<DateTimeOffset?>? AdditionalEligibleAfter = null);
public sealed record RetentionInventoryContribution(string Name, bool Complete,
    IReadOnlyList<RetentionReferenceViewFact> References, IReadOnlyList<string> ReasonCodes,
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
    Task<RetentionFileViewFact> GetFileAsync(Guid actor, Guid project, Guid file, CancellationToken token);
    Task<RetentionBasisViewFact> ConfirmBasisAsync(Guid actor, Guid project, Guid file, ConfirmRetentionBasisRequestFact request, string key, string expected, CancellationToken token);
    Task<RetentionHoldViewFact> CreateHoldAsync(Guid actor, CreateRetentionHoldRequestFact request, string key, CancellationToken token);
    Task<RetentionHoldViewFact> GetHoldAsync(Guid actor, Guid holdId, CancellationToken token);
    Task<RetentionHoldViewFact> ReleaseHoldAsync(Guid actor, Guid holdId, ReleaseRetentionHoldRequestFact request, string key, string expected, CancellationToken token);
    Task<RetentionEvaluationViewFact> AdmitEvaluationAsync(Guid actor, Guid project, CreateRetentionEvaluationRequestFact request, string key, CancellationToken token);
    Task<RetentionEvaluationViewFact> GetEvaluationAsync(Guid actor, Guid project, Guid id, Guid? afterFile, int pageSize, CancellationToken token);
    Task<bool> ProcessOneAsync(CancellationToken token);
}
