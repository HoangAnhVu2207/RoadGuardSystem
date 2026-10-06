using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Projects;
namespace RoadGuardSystem.API.Controllers;

[ApiController,ApiVersion("1.0"),Authorize]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}")]
public sealed class GeometryWorkflowController(IGeometryWorkflowService service):ControllerBase
{
    [HttpPost("crs-profiles")]
    public Task<IActionResult> CreateProfile(Guid projectId,CrsProfileInput input,CancellationToken ct)=>Run(projectId,"profile-create",ct,input:input);
    [HttpGet("crs-profiles")]
    public Task<IActionResult> Profiles(Guid projectId,CancellationToken ct)=>Run(projectId,"profile-list",ct);
    [HttpGet("crs-profiles/{profileId:guid}")]
    public Task<IActionResult> Profile(Guid projectId,Guid profileId,CancellationToken ct)=>Run(projectId,"profile-get",ct,draft:profileId);
    [HttpPost("route-systems")]
    public Task<IActionResult> CreateSystem(Guid projectId,RouteSystemInput input,CancellationToken ct)=>Run(projectId,"system-create",ct,input:input);
    [HttpGet("route-systems")]
    public Task<IActionResult> Systems(Guid projectId,CancellationToken ct)=>Run(projectId,"system-list",ct);
    [HttpGet("road-geometry-drafts/{draftId:guid}/readiness")]
    public Task<IActionResult> Readiness(Guid projectId,Guid draftId,CancellationToken ct)=>Run(projectId,"draft-readiness",ct,draft:draftId);
    [HttpPost("road-geometry-drafts")]
    [HttpPost("road-sections/{roadSectionId:guid}/geometry-drafts")]
    public Task<IActionResult> CreateDraft(Guid projectId,GeometryDraftInput input,CancellationToken ct,Guid? roadSectionId=null)=>Run(projectId,"draft-create",ct,roadSectionId,input:input);
    [HttpPut("road-geometry-drafts/{draftId:guid}")]
    [HttpPut("road-sections/{roadSectionId:guid}/geometry-drafts/{draftId:guid}")]
    public Task<IActionResult> EditDraft(Guid projectId,Guid draftId,GeometryDraftInput input,CancellationToken ct,Guid? roadSectionId=null)=>Run(projectId,"draft-edit",ct,roadSectionId,draftId,input:input);
    [HttpGet("road-geometry-drafts/{draftId:guid}")]
    [HttpGet("road-sections/{roadSectionId:guid}/geometry-drafts/{draftId:guid}")]
    public Task<IActionResult> Draft(Guid projectId,Guid draftId,CancellationToken ct,Guid? roadSectionId=null)=>Run(projectId,"draft-get",ct,roadSectionId,draftId);
    [HttpPost("road-geometry-drafts/{draftId:guid}/preview")]
    [HttpPost("road-sections/{roadSectionId:guid}/geometry-drafts/{draftId:guid}/preview")]
    public Task<IActionResult> Preview(Guid projectId,Guid draftId,CancellationToken ct,Guid? roadSectionId=null)=>Run(projectId,"draft-preview",ct,roadSectionId,draftId);
    [HttpPost("road-geometry-drafts/{draftId:guid}/confirm")]
    [HttpPost("road-sections/{roadSectionId:guid}/geometry-drafts/{draftId:guid}/confirm")]
    public Task<IActionResult> Confirm(Guid projectId,Guid draftId,GeometryConfirmInput input,CancellationToken ct,Guid? roadSectionId=null)=>Run(projectId,"confirm",ct,roadSectionId,draftId,input:input);
    [HttpGet("road-sections/{roadSectionId:guid}/versions/{routeVersionId:guid}/geometry")]
    public Task<IActionResult> Geometry(Guid projectId,Guid roadSectionId,Guid routeVersionId,CancellationToken ct)=>Run(projectId,"geometry-get",ct,roadSectionId,route:routeVersionId);
    [HttpPost("road-sections/{roadSectionId:guid}/versions/{routeVersionId:guid}/segment-sets/preview")]
    public Task<IActionResult> PreviewSet(Guid projectId,Guid roadSectionId,Guid routeVersionId,SegmentDefinition input,CancellationToken ct)=>Run(projectId,"set-preview",ct,roadSectionId,route:routeVersionId,input:input);
    [HttpPost("road-sections/{roadSectionId:guid}/versions/{routeVersionId:guid}/segment-sets")]
    public Task<IActionResult> CreateSet(Guid projectId,Guid roadSectionId,Guid routeVersionId,SegmentDefinition input,CancellationToken ct)=>Run(projectId,"set-create",ct,roadSectionId,route:routeVersionId,input:input);
    [HttpPut("road-sections/{roadSectionId:guid}/versions/{routeVersionId:guid}/segment-sets/{setId:guid}")]
    public Task<IActionResult> EditSet(Guid projectId,Guid roadSectionId,Guid routeVersionId,Guid setId,SegmentDefinition input,CancellationToken ct)=>Run(projectId,"set-edit",ct,roadSectionId,route:routeVersionId,set:setId,input:input);
    [HttpPost("road-sections/{roadSectionId:guid}/versions/{routeVersionId:guid}/segment-sets/{setId:guid}/publish")]
    public Task<IActionResult> Publish(Guid projectId,Guid roadSectionId,Guid routeVersionId,Guid setId,SegmentPublishInput input,CancellationToken ct)=>Run(projectId,"publish",ct,roadSectionId,route:routeVersionId,set:setId,input:input);
    [HttpGet("road-sections/{roadSectionId:guid}/versions/{routeVersionId:guid}/segment-sets/{setId:guid}")]
    public Task<IActionResult> Set(Guid projectId,Guid roadSectionId,Guid routeVersionId,Guid setId,CancellationToken ct)=>Run(projectId,"set-get",ct,roadSectionId,route:routeVersionId,set:setId);
    [HttpGet("geometry-package")]
    public Task<IActionResult> Package(Guid projectId,[FromQuery]Guid routeVersionId,[FromQuery]Guid segmentSetId,CancellationToken ct)
        => routeVersionId == Guid.Empty || segmentSetId == Guid.Empty ? Task.FromResult<IActionResult>(Error(400,"validation_error")) : Run(projectId,"package",ct,route:routeVersionId,set:segmentSetId);
    private async Task<IActionResult> Run(Guid project,string action,CancellationToken ct,Guid? section=null,Guid? draft=null,Guid? route=null,Guid? set=null,object? input=null)
    {
        if(!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub),out var actor)||!ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"),out var role)) return Error(401,"unauthorized");
        var etag=Request.Headers.IfMatch.ToString();
        if(!string.IsNullOrEmpty(etag) && (etag.Length<3 || etag[0]!='"' || etag[^1]!='"')) return Error(412,"concurrency_conflict");
        var result=await service.ExecuteAsync(role,new(actor,project,action,section,draft,route,set,input,Request.Headers["Idempotency-Key"].ToString(),string.IsNullOrEmpty(etag)?null:etag[1..^1]),ct);
        if(result.Code is not null) return Error(result.Status,result.Code);
        if(result.Version is not null) Response.Headers.ETag='"'+result.Version+'"';
        if(result.Status==201) {
            if(result.Value is GeometryDraftView d) Response.Headers.Location=$"/api/v1/projects/{project}/road-geometry-drafts/{d.Id}";
            else if(result.Value is RoadGeometryVersionView r) Response.Headers.Location=$"/api/v1/projects/{project}/road-sections/{r.RoadSectionId}/versions/{r.RouteVersionId}/geometry";
            else if(result.Value is SegmentSetView s) Response.Headers.Location=$"/api/v1/projects/{project}/road-sections/{section}/versions/{s.RouteVersionId}/segment-sets/{s.Id}";
            else if(result.Value is System.Text.Json.JsonElement json && json.TryGetProperty("Id",out var id)) Response.Headers.Location=action=="draft-create"?$"/api/v1/projects/{project}/road-geometry-drafts/{id}":$"/api/v1/projects/{project}/road-sections/{section}/versions/{route}/segment-sets/{id}";
        }
        return StatusCode(result.Status,result.Value);
    }
    private ObjectResult Error(int status,string code) {
        var p=new ProblemDetails {Status=status,Title="Geometry request failed",Detail="The geometry request could not be completed.",Instance=Request.Path};
        p.Extensions["code"]=code;p.Extensions["correlationId"]=HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString()??Guid.NewGuid().ToString();
        return new ObjectResult(p){StatusCode=status,ContentTypes={"application/problem+json"}};
    }
}
