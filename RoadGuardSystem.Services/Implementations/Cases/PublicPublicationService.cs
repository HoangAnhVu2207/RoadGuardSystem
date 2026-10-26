using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.DTOs.Cases;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Cases;
using RoadGuardSystem.Services.Reports;
namespace RoadGuardSystem.Services.Implementations.Cases;

public sealed class PublicPublicationService(IPublicPublicationRepository repository, IProjectScopeGuard scope, IdempotencyOperationService idempotency) : IPublicPublicationService
{
    public Task<PublicPublicationResult> CreateAsync(Guid actor, UserRoleCode role, Guid project, Guid defect, string key, CancellationToken ct)
        => Command(actor, role, project, defect, key, false, ct);
    public Task<PublicPublicationResult> WithdrawAsync(Guid actor, UserRoleCode role, Guid project, Guid id, string key, CancellationToken ct)
        => Command(actor, role, project, id, key, true, ct);
    public async Task<PublicPublicationDto?> ReadAsync(Guid id, CancellationToken ct)
    {
        var row = await repository.ReadAsync(id, ct); return row is null ? null : Project(row);
    }
    private async Task<PublicPublicationResult> Command(Guid actor, UserRoleCode role, Guid project, Guid target, string key, bool withdraw, CancellationToken ct)
    {
        if (project == Guid.Empty || target == Guid.Empty || key.Length is < 1 or > 200 || key.Any(c => c < ' ' || c > '~')) return new(400, "validation_error");
        try
        {
            Task Guard(CancellationToken token) => repository.GuardAsync(actor, role, project, target, withdraw,
                async (p, c) => await scope.AuthorizeAsync(actor, role, p, c) is not null, token);
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{project:D}/{target:D}/{withdraw}"))).ToLowerInvariant();
            var operation = withdraw ? "c1.publication.withdraw.v1" : "c1.publication.create.v1";
            var receipt = await Huy01CommandExecution.ExecuteAsync(handler => idempotency.ExecuteSerializableAsync(actor, project, operation, key, fingerprint, handler, ct, Guard),
                async token =>
                {
                    await Guard(token);
                    var row = withdraw ? await repository.WithdrawAsync(actor, project, target, token) : await repository.CreateAsync(actor, project, target, token);
                    return (row.Id, JsonSerializer.Serialize(Project(row)));
                }, exception => exception is CaseWorkflowException { Status: 412 });
            if (receipt.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
            return new(withdraw ? 200 : 201, Publication: JsonSerializer.Deserialize<PublicPublicationDto>(receipt.OutcomeJson));
        }
        catch (CaseWorkflowException e) { return new(e.Status, e.Code); }
    }
    private static PublicPublicationDto Project(PublicPublication row) => new(row.Id, row.DamageCategory, row.WithdrawnAt is null ? "PUBLISHED" : "WITHDRAWN", row.PublishedAt);
}
