using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Repositories.Projects;

namespace RoadGuardSystem.Services.Projects;

public sealed class ProjectLifecycleService(IProjectLifecycleRepository repository):IProjectLifecycleService
{
    public async Task<ProjectLifecycleMutationResult> ReadAsync(Guid actor,UserRoleCode role,Guid project,CancellationToken cancellationToken)
    {
        if(role is not(UserRoleCode.ProjectManager or UserRoleCode.Supervisor))return new(403,"access_forbidden");
        if(actor==Guid.Empty || project==Guid.Empty)return new(400,"validation_error");
        try
        {
            var view=await repository.ReadAsync(actor,project,cancellationToken);
            return view is null?new(404,"not_found"):new(200,Value:Map(view));
        }
        catch(UnauthorizedAccessException){return new(403,"access_forbidden");}
    }
    public Task<ProjectLifecycleMutationResult> RenewAsync(Guid actor,UserRoleCode role,Guid project,RenewedHandlingScopeInput input,
        string? key,string? expectedVersion,CancellationToken cancellationToken)
    {
        if(role!=UserRoleCode.Supervisor)return Error(403,"access_forbidden");
        if(string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(expectedVersion))return Error(428,"precondition_required");
        if(actor==Guid.Empty || project==Guid.Empty || key.Length>150 || key.Any(value=>value is < '!' or > '~') ||
            expectedVersion.Length!=66 || expectedVersion[0]!='"' || expectedVersion[^1]!='"' ||
            expectedVersion[1..^1].Any(value=>value is not(>= '0' and <= '9') and not(>= 'a' and <= 'f')) ||
            input is null || input.OperationalClosureId==Guid.Empty || !Text(input.Reason,2000) ||
            !Text(input.Basis,2000) || !Text(input.HandlingScope,4000))return Error(400,"validation_error");
        return RenewCoreAsync(new(actor,role,project,
            new(input.OperationalClosureId,input.Reason,input.HandlingScope,input.Basis),key,expectedVersion[1..^1]),cancellationToken);
    }
    private async Task<ProjectLifecycleMutationResult> RenewCoreAsync(ProjectRenewedHandlingCommand command,CancellationToken token)
    {
        var result=await repository.RenewAsync(command,token);
        return new(result.Status,result.Code,result.Value is null?null:Map(result.Value));
    }
    private static ProjectLifecycleViewDto Map(ProjectLifecycleFacts value)
        =>new(value.ProjectId,value.ConstructionCompletion,value.OperationalClosure,value.WarrantyRecorded,
            value.AcceptsNewReports,value.ObligationInventory,value.OperationalClosureEligibility,
            value.OutstandingMandatoryObligationIds,value.MissingReasons,value.History.Select(row=>new ProjectLifecycleHistoryDto(
                row.Id,row.Kind,row.ActorId,row.RecordedAtUtc,row.ObligationId,row.GrantId,row.ReceiverId,
                row.OperationalClosureId,row.Reason,row.BasisReference,row.AuthoritySourceReference,row.SourceDisposition,
                row.DefectId,row.LinkedDefectId,row.HandlingScope)).ToArray(),value.Version);
    private static bool Text(string? value,int maximum)=>!string.IsNullOrWhiteSpace(value) && value.Length<=maximum;
    private static Task<ProjectLifecycleMutationResult> Error(int status,string code)=>Task.FromResult(new ProjectLifecycleMutationResult(status,code));
}
