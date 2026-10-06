using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Services.Projects;

public sealed class GeometryWorkflowService(IGeometryWorkflowRepository repository,IProjectScopeGuard guard) : IGeometryWorkflowService
{
    public async Task<GeometryWorkflowResult> ExecuteAsync(UserRoleCode role,GeometryWorkflowCommand command,CancellationToken cancellationToken)
    {
        var c=command with { Role = role };var ct=cancellationToken;
        var read=c.Action is "draft-get" or "draft-readiness" or "draft-preview" or "set-preview" or "geometry-get" or "set-get" or "package" or "profile-get" or "profile-list" or "system-list";
        var operatorPackage=c.Action=="package" && role==UserRoleCode.DroneOperator;
        if (c.ActorId==Guid.Empty || (c.Action=="confirm" ? role!=UserRoleCode.Supervisor : read ? !operatorPackage && role is not (UserRoleCode.Supervisor or UserRoleCode.ProjectManager) : role!=UserRoleCode.ProjectManager)) return new(403,"access_forbidden");
        if(await guard.AuthorizeAsync(c.ActorId,role,c.ProjectId,ct) is null) return new(403,"access_forbidden");
        if(operatorPackage && (c.RouteVersionId is not Guid routeId || c.SetId is not Guid setId || !await repository.CanReadAssignedGeometryAsync(c.ActorId,c.ProjectId,routeId,setId,ct))) return new(403,"access_forbidden");
        var write=c.Action is "draft-create" or "draft-edit" or "confirm" or "set-create" or "set-edit" or "publish" or "profile-create" or "system-create";
        if(write && (string.IsNullOrWhiteSpace(c.Key) || c.Key.Length>200)) return new(428,"validation_error");
        if(c.Action is "draft-edit" or "confirm" or "set-edit" or "publish" && string.IsNullOrWhiteSpace(c.ExpectedVersion)) return new(428,"validation_error");
        try {
            if(c.Action == "profile-create") CrsProfileEngine.Validate((RoadGuardSystem.DTOs.Projects.CrsProfileInput)c.Input!);
            var result = await repository.ExecuteAsync(c,GeometryEngine.Preview,GeometryEngine.Segments,ct);
            if (c.Action == "draft-readiness" && result.Value is RoadGuardSystem.DTOs.Projects.GeometryDraftInput draft)
                return result with { Value = CrsProfileEngine.Readiness(draft, draft.ResolvedCrsProfile) };
            return result;
        }
        catch(GeometryValidationException e) { return new(422,e.Code); }
    }
}
