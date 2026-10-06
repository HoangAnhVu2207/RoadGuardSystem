using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Inspections;
public sealed partial class FieldInspectionWorkflowRepository(RoadGuardDbContext db, IdempotencyOperationService receipts,
    TimeProvider clock, IOfflineFieldAdmissionValidator? offlineAdmission = null,
    IOfflineEvidenceAdmissionValidator? evidenceAdmission = null) : IFieldInspectionWorkflowRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed class Denied(int status, string code) : Exception { public FieldWorkflowResult Result { get; } = new(status, code); }
    private sealed class ExistingReceipt : Exception { }
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Deny(int status, string code) => throw new Denied(status, code);
    private static string Hash(object value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value,Json))).ToLowerInvariant();
    private static T Decode<T>(string json) => JsonSerializer.Deserialize<T>(json,Json) ?? throw new ArgumentException("Missing structured facts.");
    private static bool IsRead(string action) => action is "list" or "get" or "start-origin" or "submission" or "history" or "geometry" or "evidence" or "verification-source";

    public async Task<FieldWorkflowResult> ExecuteAsync(FieldWorkflowCommand command,
        Func<CancellationToken,Task<bool>> projectGuard, CancellationToken cancellationToken)
    {
        try
        {
            if(IsRead(command.Action))
            {
                if(db.Database.CurrentTransaction is not null) return await ApplyInTransactionAsync(command,projectGuard,cancellationToken);
                return await db.Database.CreateExecutionStrategy().ExecuteAsync(async()=>{
                    await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,cancellationToken);
                    var result=await ApplyInTransactionAsync(command,projectGuard,cancellationToken);
                    await tx.CommitAsync(cancellationToken);return result;
                });
            }
            var fingerprint=Hash(new{command.ProjectId,command.TaskId,command.Action,command.Input,command.ExpectedVersion,command.Admission.OriginalActorId});
            async Task Guard(CancellationToken token)=>await GuardAsync(command,projectGuard,token);
            var outcome=await receipts.ExecuteSerializableAsync(command.Admission.CallerId,command.ProjectId,"h3.field."+command.Action+".v1",
                command.Key!,fingerprint,async token=>{
                    await Guard(token);
                    if(await db.IdempotencyRecords.AsNoTracking().AnyAsync(x=>x.ProjectId==command.ProjectId && x.ActorUserId==command.Admission.CallerId &&
                        x.Operation=="h3.field."+command.Action+".v1" && x.IdempotencyKey==command.Key,token)) throw new ExistingReceipt();
                    var result=await ApplyBusinessAsync(command,token);
                    await db.SaveChangesAsync(token);
                    var value=await FinalizeResultAsync(command,result,token);
                    return (Guid.NewGuid(),JsonSerializer.Serialize(value.Value,Json));
                },cancellationToken,receiptAccessGuard:Guard);
            if(outcome.Status==IdempotencyOperationStatus.Conflict)return new(409,"idempotency_key_reused");
            return new(outcome.Status==IdempotencyOperationStatus.Replayed?200:201,Value:Decode<JsonElement>(outcome.OutcomeJson),
                Replayed:outcome.Status==IdempotencyOperationStatus.Replayed);
        }
        catch(ExistingReceipt){db.ChangeTracker.Clear();return await ExecuteAsync(command,projectGuard,cancellationToken);}
        catch(Denied error){return error.Result;}
        catch(FieldCoreRejectedException error){return new(error.Result.Status,error.Result.Code,error.Result.Value,error.Result.Version,error.Result.Replayed);}
        catch(OfflineAdmissionRejectedException error){return new(error.Status,error.Code);}
        catch(ArgumentException){return new(400,"validation_error");}
        catch(JsonException){return new(400,"validation_error");}
        catch(InvalidOperationException error) when(error.Message.StartsWith("Invalid FIELD",StringComparison.Ordinal)) {return new(409,"invalid_state_transition");}
    }

    public async Task<FieldWorkflowResult> ApplyInTransactionAsync(FieldWorkflowCommand command,
        Func<CancellationToken,Task<bool>> projectGuard,CancellationToken cancellationToken)
    {
        if(db.Database.CurrentTransaction is null) throw new InvalidOperationException("Caller-owned FIELD transaction required.");
        try
        {
            await GuardAsync(command,projectGuard,cancellationToken);
            if(IsRead(command.Action))return await ReadAsync(command,cancellationToken);
            var result=await ApplyBusinessAsync(command,cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await GuardAsync(command,projectGuard,cancellationToken);
            return await FinalizeResultAsync(command,result,cancellationToken);
        }
        catch(Denied error){throw new FieldCoreRejectedException(CoreOutcome(error.Result));}
        catch(ArgumentException){throw new FieldCoreRejectedException(new(400,"validation_error"));}
        catch(JsonException){throw new FieldCoreRejectedException(new(400,"validation_error"));}
        catch(InvalidOperationException error) when(error.Message.StartsWith("Invalid FIELD",StringComparison.Ordinal))
        {throw new FieldCoreRejectedException(new(409,"invalid_state_transition"));}
    }

    public async Task<FieldCoreOutcome> ApplyInternalInTransactionAsync(FieldWorkflowCommand command,
        Func<CancellationToken,Task<bool>> projectGuard,CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        object input = command.Input switch
        {
            FieldStartData value => new FieldStartInput(value.OriginId,value.ClaimedAt,value.DeviceId,
                value.MonotonicMilliseconds,value.BootId,value.OfflineProof),
            FieldActionData value => new FieldTaskActionInput(value.Reason,value.AssignedToUserId,
                value.Handover is null ? null : new(value.Handover.PerformedPortionState,value.Handover.Summary,
                    value.Handover.StartOriginId,value.Handover.SubmissionIds,value.Handover.RecipientUserId)),
            RepairFieldSubmissionData value => LegacyRepairSubmission(value),
            _ => throw new FieldCoreRejectedException(new(400,"validation_error"))
        };
        // Keep the original canonical representation independently of the compatibility conversion.
        var canonicalInput = JsonSerializer.SerializeToUtf8Bytes(command.Input,Json);
        if (!canonicalInput.AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(input,Json)))
            throw new InvalidOperationException("Internal FIELD input changed its canonical representation.");
        return CoreOutcome(await ApplyInTransactionAsync(command with {Input=input},projectGuard,cancellationToken));
    }

    private static FieldCoreOutcome CoreOutcome(FieldWorkflowResult result)
        => new(result.Status,result.Code,result.Value is null ? null : JsonSerializer.SerializeToElement(result.Value,Json),
            result.Version,result.Replayed);

    private static (Guid OriginId,Guid? DeviceId)? OriginMetadata(object? input) => input switch
    {
        FieldStartInput value => (value.OriginId,value.DeviceId),
        FieldSubmissionInput value => (value.OriginId,value.DeviceId),
        FieldStartData value => (value.OriginId,value.DeviceId),
        RepairFieldSubmissionData value => (value.OriginId,value.DeviceId),
        _ => null
    };

    private async Task GuardAsync(FieldWorkflowCommand c,Func<CancellationToken,Task<bool>> projectGuard,CancellationToken token)
    {
        var a=c.Admission;
        await Anh02ReceiptAuthority.LockAsync(db,a.CallerId,c.ProjectId,token);
        if(c.Action=="verification-source" && a.CallerRole!=UserRoleCode.ProjectManager)Deny(403,"access_forbidden");
        var read=IsRead(c.Action);var pm=c.Action is "create" or "assign" or "cancel" or "reassign" or "review" or "reuse" or "impact";
        var imported=a.Mode is "SYNC" or "HANDOVER";
        if(read ? a.CallerRole is not(UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.RepairCrew) :
            imported ? a.CallerRole is not(UserRoleCode.ProjectManager or UserRoleCode.RepairCrew) :
            pm ? a.CallerRole!=UserRoleCode.ProjectManager : a.CallerRole!=UserRoleCode.RepairCrew)Deny(403,"access_forbidden");
        if(!await new AnhHuyFactsRepository(db).IsCurrentActorAsync(a.CallerId,a.CallerRole,token) || !await projectGuard(token))Deny(403,"access_forbidden");
        if(a.Mode=="DIRECT" && a.OriginalActorId!=a.CallerId || a.Mode is not("DIRECT" or "SYNC" or "HANDOVER"))Deny(403,"access_forbidden");
        var admitted=await ImportedFactsAsync(c,token);
        if(c.Input is FieldTaskCreateInput create)
        {
            var defect=await db.Defects.FromSqlInterpolated($"SELECT * FROM [Defects] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={create.DefectId}").AsNoTracking().SingleOrDefaultAsync(token);
            if(defect is null || defect.ProjectId!=c.ProjectId)Deny(404,"not_found");
        }
        if(c.TaskId is Guid taskId)
        {
            var task=await db.FieldInspectionTasks.FromSqlInterpolated($"SELECT * FROM [FieldInspectionTasks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={taskId}").SingleOrDefaultAsync(token);
            if(task is null || task.ProjectId!=c.ProjectId)Deny(404,"not_found");
            await db.FieldInspectionAssignments.FromSqlInterpolated($"SELECT * FROM [FieldInspectionAssignments] WITH (UPDLOCK,HOLDLOCK) WHERE [FieldInspectionTaskId]={taskId}").AsNoTracking().ToListAsync(token);
            if(admitted is not null)
            {
                if(!await db.FieldInspectionAssignments.AnyAsync(x=>x.Id==admitted.AssignmentId && x.FieldInspectionTaskId==taskId &&
                    x.AssignedToUserId==admitted.OriginalActorId && x.Status==FieldInspectionAssignmentStatus.Active && x.EndedAt==null,token))Deny(409,"stale_snapshot");
            }
            else if(a.CallerRole==UserRoleCode.RepairCrew && !await db.FieldInspectionAssignments.AnyAsync(x=>x.FieldInspectionTaskId==taskId && x.AssignedToUserId==a.CallerId && x.Status==FieldInspectionAssignmentStatus.Active && x.EndedAt==null,token))Deny(403,"access_forbidden");
            if(c.Input is FieldLocationImpactActionInput impact)await ImpactAsync(c,task,impact,token);
            if(c.Input is FieldReviewInput review && !await db.Set<FieldInspectionSubmission>().AnyAsync(x=>x.Id==review.SubmissionId && x.TaskId==taskId,token))Deny(404,"not_found");
            if(c.Input is FieldEvidenceReuseInput)await ReuseAsync(c,task,token,true);
            if(c.Input is FieldSubmissionInput submission)
            {
                if(!await db.Set<FieldTaskStartOrigin>().AnyAsync(x=>x.Id==submission.StartOriginId && x.TaskId==taskId,token))Deny(404,"not_found");
                if(submission.ParentSubmissionId is Guid parent && !await db.Set<FieldInspectionSubmission>().AnyAsync(x=>x.Id==parent && x.TaskId==taskId,token))Deny(404,"not_found");
                if(admitted is null)foreach(var evidence in submission.Evidence??[])if(evidence?.FileId is not null)await EvidenceFileAsync(task,evidence,token,a.OriginalActorId);
            }
        }
    }

    private async Task<FieldWorkflowResult> ReadAsync(FieldWorkflowCommand c,CancellationToken token)
    {
        if(c.Action=="list")
        {
            var query=c.Input as FieldTaskListQuery ?? new(null,50);
            if(query.Limit is <1 or >100)Deny(400,"validation_error");
            var rows=db.FieldInspectionTasks.AsNoTracking().Where(x=>x.ProjectId==c.ProjectId);
            if(c.Admission.CallerRole==UserRoleCode.RepairCrew)rows=rows.Where(x=>db.FieldInspectionAssignments.Any(a=>a.FieldInspectionTaskId==x.Id && a.AssignedToUserId==c.Admission.CallerId && a.Status==FieldInspectionAssignmentStatus.Active && a.EndedAt==null));
            if(query.AfterId is Guid after)rows=rows.Where(x=>x.Id.CompareTo(after)>0);
            var tasks=await rows.OrderBy(x=>x.Id).Take(query.Limit+1).ToArrayAsync(token);
            var views=new List<FieldTaskView>();foreach(var row in tasks.Take(query.Limit))views.Add(await ViewAsync(row,token));
            return new(200,Value:new{items=views,nextCursor=tasks.Length>query.Limit?views[^1].Id:(Guid?)null});
        }
        var task=await TaskAsync(c.TaskId!.Value,token);
        if(c.Action=="start-origin")
        {
            var origin=await db.Set<FieldTaskStartOrigin>().AsNoTracking().SingleOrDefaultAsync(x=>x.TaskId==task.Id,token);
            if(origin is null)Deny(404,"not_found");return await FinalizeResultAsync(c,new(200,Value:origin),token);
        }
        if(c.Action=="get")return new(200,Value:await ViewAsync(task,token),Version:Convert.ToBase64String(task.RowVersion));
        if(c.Action=="history")return new(200,Value:new{
            assignments=await db.FieldInspectionAssignments.AsNoTracking().Where(x=>x.FieldInspectionTaskId==task.Id).OrderBy(x=>x.AssignedAt).Select(x=>new{x.Id,x.AssignedToUserId,x.AssignedByUserId,x.AssignedAt,x.EndedAt,x.Status,x.Reason}).ToArrayAsync(token),
            events=await db.Set<FieldInspectionTaskEvent>().AsNoTracking().Where(x=>x.TaskId==task.Id).OrderBy(x=>x.OccurredAt).ThenBy(x=>x.Id).Select(x=>new{x.Id,x.AssignmentId,x.ActorId,x.Kind,x.Reason,x.OccurredAt,x.FactsJson,x.LocationImpactId,x.LocationImpactDecisionId}).ToArrayAsync(token)});
        if(c.Action=="submission")
        {
            var id=c.Input is Guid input?input:Guid.Empty;
            var row=await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id && x.TaskId==task.Id,token);
            if(row is null)Deny(404,"not_found");return new(200,Value:await SubmissionViewAsync(row,token),Version:row.ContentHash);
        }
        if(c.Action=="verification-source")return await VerificationSourceAsync(c,task,token);
        if(c.Action=="geometry")return await GeometryAsync(task,token);
        if(c.Action=="evidence")return await EvidenceAsync(c,task,token);
        Deny(404,"not_found");return null!;
    }

    private Task<FieldInspectionTask> TaskAsync(Guid id,CancellationToken token)=>db.FieldInspectionTasks.SingleAsync(x=>x.Id==id,token);
    private async Task<FieldInspectionAssignment?> CurrentAssignmentAsync(Guid task,CancellationToken token)=>await db.FieldInspectionAssignments.SingleOrDefaultAsync(x=>x.FieldInspectionTaskId==task && x.Status==FieldInspectionAssignmentStatus.Active && x.EndedAt==null,token);
    private async Task<FieldTaskView> ViewAsync(FieldInspectionTask task,CancellationToken token)
    {
        var assignment=await CurrentAssignmentAsync(task.Id,token);
        var start=await db.Set<FieldTaskStartOrigin>().AsNoTracking().Where(x=>x.TaskId==task.Id).Select(x=>(Guid?)x.Id).SingleOrDefaultAsync(token);
        var latest=await db.Set<FieldInspectionSubmission>().AsNoTracking().Where(x=>x.TaskId==task.Id).OrderByDescending(x=>x.Revision).Select(x=>(Guid?)x.Id).FirstOrDefaultAsync(token);
        return new(task.Id,task.ProjectId,task.DefectId,task.SurveyId,task.SourceKind,task.RoadSectionVersionId,task.SegmentSetId,task.LayoutRevisionId,
            task.SlabId,task.Purpose.ToString(),task.TaskMode,task.Status.ToString(),assignment?.Id,assignment?.AssignedToUserId,
            Convert.ToBase64String(task.RowVersion),start,latest,task.MapPublicationId,task.CrsProfileRevisionId,task.RequiredMeasurementType,task.MeasurementScope,task.Instructions,task.DueAt);
    }
    private async Task<FieldSubmissionView> SubmissionViewAsync(FieldInspectionSubmission row,CancellationToken token)
    {
        var payload=Decode<FieldSubmissionInput>(row.PayloadJson);
        var root=await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleAsync(x=>x.Id==row.RootId,token);
        return new(row.Id,row.TaskId,row.RootId,row.ParentId,row.Revision,row.ServerReceivedAt,root.ServerReceivedAt.AddHours(24),row.Readiness,
            Decode<string[]>(row.MissingReasonsJson),row.ContentHash,payload.Measurements??[],payload.Evidence??[],payload.CaptureType,payload.Repaired,payload.UnrepairedReason);
    }
}
