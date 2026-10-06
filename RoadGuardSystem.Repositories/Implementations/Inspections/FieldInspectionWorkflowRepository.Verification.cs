using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.Repositories.Inspections;
namespace RoadGuardSystem.Repositories.Implementations.Inspections;
public sealed partial class FieldInspectionWorkflowRepository
{
    private async Task<FieldWorkflowResult> VerificationSourceAsync(FieldWorkflowCommand c,FieldInspectionTask task,CancellationToken token)
    {
        var query=c.Input as FieldVerificationSourceQuery ?? throw new ArgumentException("FIELD source binding required.");
        if(task.DefectId!=query.DefectId)Deny(404,"not_found");
        var row=await db.Set<FieldInspectionSubmission>().FromSqlInterpolated($"SELECT * FROM [FieldInspectionSubmissions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={query.SubmissionId}").AsNoTracking().SingleOrDefaultAsync(token);
        if(row is null || row.TaskId!=task.Id || row.ProjectId!=c.ProjectId)Deny(404,"not_found");
        if(!string.Equals(row.ContentHash,query.ExpectedContentHash,StringComparison.Ordinal))Deny(409,"field_source_version_mismatch");
        if(task.Status!=FieldInspectionTaskStatus.Completed || task.LifecycleVersion!=2 || task.TaskMode!="MEASURE_ONLY" ||
            await db.Set<FieldInspectionSubmission>().AnyAsync(x=>x.TaskId==task.Id && x.Revision>row.Revision,token))Deny(409,"field_result_insufficient");
        var review=await db.Set<FieldInspectionReview>().FromSqlInterpolated($"SELECT * FROM [FieldInspectionReviews] WITH (UPDLOCK,HOLDLOCK) WHERE [SubmissionId]={row.Id}").AsNoTracking().Where(x=>x.Decision!="SUPPLEMENT").SingleOrDefaultAsync(token);
        if(review is null)Deny(409,"field_result_insufficient");
        var payload=Decode<FieldSubmissionInput>(row.PayloadJson);
        var start=await db.Set<FieldTaskStartOrigin>().AsNoTracking().SingleAsync(x=>x.Id==row.StartOriginId,token);
        if(payload.CaptureType!="MEASUREMENT" || (await ReadinessAsync(task,payload,start,token,row.OriginalActorId)).Count!=0)Deny(409,"field_result_insufficient");
        var evidence=await db.Set<FieldInspectionEvidenceLink>().AsNoTracking().Where(x=>x.SubmissionId==row.Id && x.FileId!=null).Select(x=>x.Id).OrderBy(x=>x).ToArrayAsync(token);
        if(evidence.Length==0)Deny(409,"field_result_insufficient");
        return new(200,Value:new FieldVerificationSourceFacts(task.Id,task.DefectId,row.Id,row.ContentHash,review.Decision,evidence,
            task.RoadSectionVersionId,task.SegmentSetId,task.LayoutRevisionId),Version:row.ContentHash);
    }
}
