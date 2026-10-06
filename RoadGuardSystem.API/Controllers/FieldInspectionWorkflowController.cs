using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Services.Inspections;
namespace RoadGuardSystem.API.Controllers;
[ApiController,ApiVersion("1.0"),Authorize]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/field-inspection-tasks")]
public sealed class FieldInspectionWorkflowController(IFieldInspectionWorkflowService service,IUploadRepository uploads):ControllerBase
{
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    [HttpPost]
    public Task<IActionResult> Create(Guid projectId,FieldTaskCreateInput input,CancellationToken cancellationToken)=>Run(projectId,null,"create",input,cancellationToken);
    [HttpGet]
    public Task<IActionResult> List(Guid projectId,[FromQuery]Guid? afterId=null,[FromQuery]int limit=50,CancellationToken cancellationToken=default)=>Run(projectId,null,"list",new FieldTaskListQuery(afterId,limit),cancellationToken);
    [HttpGet("{taskId:guid}")]
    public Task<IActionResult> Get(Guid projectId,Guid taskId,CancellationToken cancellationToken)=>Run(projectId,taskId,"get",null,cancellationToken);
    [HttpGet("{taskId:guid}/history")]
    public Task<IActionResult> History(Guid projectId,Guid taskId,CancellationToken cancellationToken)=>Run(projectId,taskId,"history",null,cancellationToken);
    [HttpGet("{taskId:guid}/start-origin")]
    public Task<IActionResult> Origin(Guid projectId,Guid taskId,CancellationToken cancellationToken)=>Run(projectId,taskId,"start-origin",null,cancellationToken);
    [HttpGet("{taskId:guid}/geometry")]
    public Task<IActionResult> Geometry(Guid projectId,Guid taskId,CancellationToken cancellationToken)=>Run(projectId,taskId,"geometry",null,cancellationToken);
    [HttpPost("{taskId:guid}/accept")]
    public Task<IActionResult> Accept(Guid projectId,Guid taskId,FieldTaskActionInput input,CancellationToken cancellationToken)=>Run(projectId,taskId,"accept",input,cancellationToken);
    [HttpPost("{taskId:guid}/reject")]
    public Task<IActionResult> Reject(Guid projectId,Guid taskId,FieldTaskActionInput input,CancellationToken cancellationToken)=>Run(projectId,taskId,"reject",input,cancellationToken);
    [HttpPost("{taskId:guid}/assign")]
    public Task<IActionResult> Assign(Guid projectId,Guid taskId,FieldTaskActionInput input,CancellationToken cancellationToken)=>Run(projectId,taskId,"assign",input,cancellationToken);
    [HttpPost("{taskId:guid}/reassign")]
    public Task<IActionResult> Reassign(Guid projectId,Guid taskId,FieldTaskActionInput input,CancellationToken cancellationToken)=>Run(projectId,taskId,"reassign",input,cancellationToken);
    [HttpPost("{taskId:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid projectId,Guid taskId,FieldTaskActionInput input,CancellationToken cancellationToken)=>Run(projectId,taskId,"cancel",input,cancellationToken);
    [HttpPost("{taskId:guid}/start")]
    public Task<IActionResult> Start(Guid projectId,Guid taskId,FieldStartInput input,CancellationToken cancellationToken)=>Run(projectId,taskId,"start",input,cancellationToken);
    [HttpPost("{taskId:guid}/submissions")]
    public Task<IActionResult> Submit(Guid projectId,Guid taskId,FieldSubmissionInput input,CancellationToken cancellationToken)=>Run(projectId,taskId,"submit",input,cancellationToken);
    [HttpGet("{taskId:guid}/submissions/{submissionId:guid}")]
    public Task<IActionResult> Submission(Guid projectId,Guid taskId,Guid submissionId,CancellationToken cancellationToken)=>Run(projectId,taskId,"submission",submissionId,cancellationToken);
    [HttpPost("{taskId:guid}/reviews")]
    public Task<IActionResult> Review(Guid projectId,Guid taskId,FieldReviewInput input,CancellationToken cancellationToken)=>Run(projectId,taskId,"review",input,cancellationToken);
    [HttpPost("{taskId:guid}/location-impact-decisions")]
    public Task<IActionResult> Impact(Guid projectId,Guid taskId,FieldLocationImpactActionInput input,CancellationToken cancellationToken)=>Run(projectId,taskId,"impact",input,cancellationToken);
    [HttpPost("{taskId:guid}/evidence-reuse")]
    public Task<IActionResult> Reuse(Guid projectId,Guid taskId,FieldEvidenceReuseInput input,CancellationToken cancellationToken)=>Run(projectId,taskId,"reuse",input,cancellationToken);
    [HttpGet("{taskId:guid}/evidence/{fileId:guid}")]
    public Task<IActionResult> Evidence(Guid projectId,Guid taskId,Guid fileId,CancellationToken cancellationToken)=>Run(projectId,taskId,"evidence",fileId,cancellationToken);
    [HttpGet("{taskId:guid}/evidence/{fileId:guid}/content")]
    public async Task<IActionResult> DownloadEvidence(Guid projectId,Guid taskId,Guid fileId,CancellationToken cancellationToken)
    {
        if(!Actor(out var actor,out var role))return Error(401,"unauthorized");
        var result=await service.ExecuteAsync(actor,role,projectId,taskId,"evidence",fileId,null,null,cancellationToken);
        if(result.Status>=400)return Error(result.Status,result.Code!);
        if(result.Value is not FieldTaskEvidenceFile file || file.State!="VERIFIED")return Error(409,"source_not_ready");
        Stream stream;
        try{stream=await uploads.OpenFileAsync(file.ObjectKey,cancellationToken);}
        catch(RoadGuardSystem.Repositories.Storage.FileStorageException){return Error(503,"storage_unavailable");}
        try
        {
            var current=await service.ExecuteAsync(actor,role,projectId,taskId,"evidence",fileId,null,null,cancellationToken);
            if(current.Status>=400 || current.Value is not FieldTaskEvidenceFile fresh || fresh.State!="VERIFIED" || fresh.Checksum!=file.Checksum)
            {
                await stream.DisposeAsync();return Error(current.Status>=400?current.Status:409,current.Code??"source_not_ready");
            }
            return File(stream,fresh.MediaType);
        }
        catch{await stream.DisposeAsync();throw;}
    }
    private bool Actor(out Guid actor,out RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode role)
    {
        actor=Guid.Empty;role=default;
        return Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub),out actor) && ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"),out role);
    }
    private async Task<IActionResult> Run(Guid project,Guid? task,string action,object? input,CancellationToken cancellationToken)
    {
        if(!Actor(out var actor,out var role))return Error(401,"unauthorized");
        if(Request.Headers.TryGetValue("Idempotency-Key",out var keys) && keys.Count!=1)return Error(400,"validation_error");
        if(Request.Headers.IfMatch.Count>1)return Error(400,"validation_error");
        var etag=Request.Headers.IfMatch.ToString();
        if(etag.Length>0 && (etag.Length<3 || etag[0]!='"' || etag[^1]!='"' || etag.Contains(',')))return Error(400,"validation_error");
        var result=await service.ExecuteAsync(actor,role,project,task,action,input,keys.Count==1?keys[0]:null,etag.Length==0?null:etag,cancellationToken);
        if(result.Status>=400)return Error(result.Status,result.Code??"validation_error");
        var value=JsonSerializer.SerializeToElement(result.Value,Json);
        string? version=result.Version;
        if(version is null && value.ValueKind==JsonValueKind.Object)
        {if(value.TryGetProperty("version",out var v))version=v.GetString();else if(value.TryGetProperty("contentHash",out var h))version=h.GetString();}
        if(version is not null)Response.Headers.ETag="\""+version+"\"";
        if(result.Status==201 && value.ValueKind==JsonValueKind.Object && value.TryGetProperty("id",out var id))
        {
            var basePath=$"/api/v1/projects/{project}/field-inspection-tasks";
            Response.Headers.Location=action=="create"?$"{basePath}/{id.GetGuid()}":action=="submit"?$"{basePath}/{task}/submissions/{id.GetGuid()}":$"{basePath}/{task}";
        }
        return StatusCode(result.Status,result.Value);
    }
    private ObjectResult Error(int status,string code)
    {
        var problem=new ProblemDetails { Status=status,Title=code };
        problem.Extensions["code"]=code;
        problem.Extensions["correlationId"]=HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString()??Guid.NewGuid().ToString();
        return new(problem){StatusCode=status,ContentTypes={"application/problem+json"}};
    }
}
