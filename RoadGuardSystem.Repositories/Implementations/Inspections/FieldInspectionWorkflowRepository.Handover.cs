using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Repositories.Inspections;
namespace RoadGuardSystem.Repositories.Implementations.Inspections;
public sealed partial class FieldInspectionWorkflowRepository
{
    private async Task<object> HandoverFactsAsync(FieldInspectionTask task,FieldTaskActionInput? action,CancellationToken token)
    {
        if(action is null)throw new ArgumentException("Structured action required.");
        var start=await db.Set<FieldTaskStartOrigin>().AsNoTracking().SingleOrDefaultAsync(x=>x.TaskId==task.Id,token);
        if(start is null)return new{performedPortionState="NOT_STARTED",firstStartOriginId=(Guid?)null};
        var facts=action.Handover;
        if(facts is null || facts.StartOriginId!=start.Id || facts.PerformedPortionState is not("NONE" or "PARTIAL" or "CAPTURED_INTAKE" or "UNKNOWN") ||
            string.IsNullOrWhiteSpace(facts.Summary) || facts.Summary.Trim().Length>2000 || facts.SubmissionIds is null || facts.SubmissionIds.Length>100 ||
            facts.SubmissionIds.Any(x=>x==Guid.Empty) || facts.SubmissionIds.Distinct().Count()!=facts.SubmissionIds.Length ||
            facts.PerformedPortionState=="CAPTURED_INTAKE" && facts.SubmissionIds.Length==0 || facts.RecipientUserId!=action.AssignedToUserId)
            Deny(400,"handover_facts_required");
        if(await db.Set<FieldInspectionSubmission>().CountAsync(x=>facts.SubmissionIds.Contains(x.Id) && x.TaskId==task.Id,token)!=facts.SubmissionIds.Length)Deny(403,"handover_history_scope_invalid");
        return new{facts.PerformedPortionState,summary=facts.Summary.Trim(),facts.SubmissionIds,facts.RecipientUserId,
            firstStartOriginId=start.Id,start.OriginId,start.ContentHash,start.VerifiedOriginalAt,task.RoadSectionVersionId,task.SegmentSetId,task.LayoutRevisionId,task.SlabId};
    }
    private async Task<GeometryLocationImpact> ImpactAsync(FieldWorkflowCommand c,FieldInspectionTask task,FieldLocationImpactActionInput input,CancellationToken token)
    {
        var row=await db.Set<GeometryLocationImpact>().FromSqlInterpolated($"SELECT * FROM [GeometryLocationImpacts] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={input.ImpactId}").SingleOrDefaultAsync(token);
        if(row is null || row.ProjectId!=c.ProjectId)Deny(404,"not_found");
        if(row.PreviousRouteVersionId!=task.RoadSectionVersionId || !Decode<GeometryAffectedReference[]>(row.AffectedReferencesJson).Any(x=>x.Kind=="FIELD_TASK" && x.Id==task.Id && x.RouteVersionId==task.RoadSectionVersionId && x.SegmentSetId==task.SegmentSetId))Deny(409,"geometry_version_mismatch");
        if(input.Action is not("STOP" or "REASSIGN" or "CONTINUE" or "VERIFY") || string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length>2000)Deny(400,"validation_error");
        return row;
    }
    private async Task<FieldWorkflowResult> ApplyImpactAsync(FieldWorkflowCommand c,FieldInspectionTask task,FieldInspectionAssignment? assignment,CancellationToken token)
    {
        var input=c.Input as FieldLocationImpactActionInput ?? throw new ArgumentException("Impact action required.");
        var impact=await ImpactAsync(c,task,input,token);
        if(task.Status is FieldInspectionTaskStatus.Completed or FieldInspectionTaskStatus.Cancelled)Deny(409,"invalid_state_transition");
        var now=clock.GetUtcNow();object facts;
        if(input.Action is "STOP" or "REASSIGN")
        {
            var action=new FieldTaskActionInput(input.Reason,input.Action=="REASSIGN"?input.AssignedToUserId:null,input.Handover);
            facts=await HandoverFactsAsync(task,action,token);
            if(input.Action=="STOP"){task.Transition(FieldInspectionTaskStatus.Cancelled);assignment?.End(now,input.Reason);}
            else
            {
                var recipient=input.AssignedToUserId.GetValueOrDefault();if(recipient==Guid.Empty)Deny(400,"validation_error");
                await GuardCrewAsync(c.ProjectId,recipient,token);assignment?.End(now,input.Reason);task.Reassign();
                assignment=FieldInspectionAssignment.Create(Guid.NewGuid(),task.Id,recipient,c.Admission.CallerId,now,null,FieldInspectionAssignmentStatus.Active,null);
                db.FieldInspectionAssignments.Add(assignment);
            }
        }
        else
        {
            // This re-evaluates immutable old pins. It never adopts replacement location or an official accuracy claim.
            if(task.SegmentSetId is not null)await GeometryAsync(task,token);
            facts=new{executionStatus="APPLIED",locationReadiness=task.SegmentSetId is null?"LEGACY_SEGMENT_SET_UNPINNED":"PINNED_REFERENCE",
                task.RoadSectionVersionId,task.SegmentSetId,task.LayoutRevisionId,task.MapPublicationId,task.CrsProfileRevisionId,task.SlabId,
                impact.NewRouteVersionId,replacementAdopted=false};
        }
        var decision=new GeometryLocationImpactDecision{Id=Guid.NewGuid(),ImpactId=impact.Id,TaskId=task.Id,Action=input.Action,
            Reason=input.Reason.Trim(),ActorId=c.Admission.CallerId,OccurredAt=now};db.Set<GeometryLocationImpactDecision>().Add(decision);
        Emit(c,task,assignment,input.Action switch{"STOP"=>"CANCELLED","REASSIGN"=>"REASSIGNED","CONTINUE"=>"IMPACT_CONTINUE",_=>"IMPACT_VERIFY"},input.Reason,now,
            facts:facts,impactId:impact.Id,impactDecisionId:decision.Id);
        return new(201,Value:new{decision.Id,decision.ImpactId,taskId=task.Id,decision.Action,executionStatus="APPLIED",facts});
    }
}
