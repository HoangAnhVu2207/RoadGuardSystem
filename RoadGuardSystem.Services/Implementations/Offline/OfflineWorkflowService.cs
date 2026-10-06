using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Services.Authorization;

namespace RoadGuardSystem.Services.Offline;

public sealed class OfflineWorkflowService(IOfflineWorkflowRepository repository, IProjectScopeGuard guard) : IOfflineWorkflowService
{
    public async Task<OfflineWorkflowResult> ExecuteAsync(Guid actorId, UserRoleCode role, Guid projectId, string action,
        object? input, Guid? resourceId, string? operationKey, string? expectedVersion, CancellationToken cancellationToken)
    {
        var read=action is "device-get" or "snapshot-get" or "grant-get" or "package-get" or "batch-get" or "origin-get" or "artifact-get";
        var write=action is "device-register" or "device-revoke" or "snapshot-create" or "grant-issue" or "grant-revoke" or
            "package-export" or "package-prepare" or "artifact-register" or "sync" or "import" or "start-reconcile";
        if(!read && !write)return new(400,"validation_error");
        var supervisor=action is "grant-issue" or "grant-revoke" or "package-prepare";
        if(actorId==Guid.Empty || projectId==Guid.Empty || (read ? role is not(UserRoleCode.Supervisor or UserRoleCode.ProjectManager or UserRoleCode.RepairCrew) :
            supervisor ? role!=UserRoleCode.Supervisor : action=="artifact-register" ? role is not(UserRoleCode.Supervisor or UserRoleCode.ProjectManager or UserRoleCode.RepairCrew) :
            role is not(UserRoleCode.ProjectManager or UserRoleCode.RepairCrew)))return new(403,"access_forbidden");
        if(write && string.IsNullOrWhiteSpace(operationKey))return new(428,"idempotency_key_required");
        if(write && (operationKey!.Length>150 || operationKey.Any(x=>x is < '!' or > '~')))return new(400,"validation_error");
        if(read && (resourceId is null || resourceId==Guid.Empty))return new(400,"validation_error");
        try
        {
            var result = await repository.ExecuteAsync(new(actorId,role,projectId,action,
                    OfflineContractMapping.ToData(action,input),resourceId,operationKey,expectedVersion),
                OfflineContractMapping.RepositoryAlgorithms,
                async token=>await guard.AuthorizeAsync(actorId,role,projectId,token) is not null,cancellationToken);
            return OfflineContractMapping.ToWire(result);
        }
        catch(ArgumentException){return new(400,"validation_error");}
        catch(System.Text.Json.JsonException){return new(400,"validation_error");}
        catch(System.Security.Cryptography.CryptographicException){return new(400,"offline_signature_invalid");}
    }
}
