using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
namespace RoadGuardSystem.Repositories.Implementations.Files;
public sealed partial class UploadPersistenceService
{
    private readonly TimeProvider _fieldClock=TimeProvider.System;
    public UploadPersistenceService(RoadGuardDbContext context,IdempotencyOperationService idempotency,IUploadObjectStorage storage,
        IOptions<UploadSessionOptions> options,TimeProvider clock):this(context,idempotency,storage,options)=>_fieldClock=clock;
    private static bool FieldPurpose(string purpose)=>purpose is "BEFORE" or "AFTER" or "MEASUREMENT";
    private async Task<bool> IsCurrentFieldActorCoreAsync(Guid actorUserId,UserRoleCode role,Guid projectId,Guid taskId,string purpose,bool forUpload,CancellationToken cancellationToken=default)
    {
        var task=await _context.FieldInspectionTasks.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==taskId && x.ProjectId==projectId,cancellationToken);
        if(task is null || !FieldPurpose(purpose) || forUpload && (task.LifecycleVersion!=2 || purpose=="AFTER" && task.Purpose!=FieldInspectionPurpose.PostRepair))return false;
        if(!forUpload && role is UserRoleCode.ProjectManager or UserRoleCode.Supervisor)return true;
        return role==UserRoleCode.RepairCrew && (!forUpload || task.Status is FieldInspectionTaskStatus.Accepted or FieldInspectionTaskStatus.InProgress or FieldInspectionTaskStatus.Submitted or FieldInspectionTaskStatus.SupplementRequired) &&
            await _context.FieldInspectionAssignments.AnyAsync(x=>x.FieldInspectionTaskId==taskId && x.AssignedToUserId==actorUserId && x.Status==FieldInspectionAssignmentStatus.Active && x.EndedAt==null,cancellationToken);
    }
    public async Task<bool> IsCurrentFieldActorAsync(Guid actorUserId,UserRoleCode role,Guid projectId,Guid taskId,string purpose,bool forUpload,CancellationToken cancellationToken=default)
    {
        async Task<bool> Read(CancellationToken token)
        {
            try
            {
                await GuardFieldScopeAsync(actorUserId,projectId,taskId,purpose,forUpload,token);
                return await new RoadGuardSystem.Repositories.Integration.AnhHuyFactsRepository(_context).IsCurrentActorAsync(actorUserId,role,token) &&
                    await IsCurrentFieldActorCoreAsync(actorUserId,role,projectId,taskId,purpose,forUpload,token);
            }
            catch(UploadNotFoundException){return false;}
        }
        if(_context.Database.CurrentTransaction is not null)return await Read(cancellationToken);
        return await MultipartTransactionAsync(Read,cancellationToken);
    }
    public async Task<bool> IsCurrentLegacyFieldFileReaderAsync(Guid actorUserId,UserRoleCode role,Guid projectId,Guid fileId,string purpose,CancellationToken cancellationToken=default)
    {
        if(role is not(UserRoleCode.ProjectManager or UserRoleCode.Supervisor) || !FieldPurpose(purpose))return false;
        async Task<bool> Read(CancellationToken token)
        {
            await MultipartReceiptAuthority.LockAsync(_context,actorUserId,projectId,token);
            if(!await new RoadGuardSystem.Repositories.Integration.AnhHuyFactsRepository(_context).IsCurrentActorAsync(actorUserId,role,token))return false;
            await _context.Files.FromSqlInterpolated($"SELECT * FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={fileId}").AsNoTracking().ToListAsync(token);
            await _context.UploadSessions.FromSqlInterpolated($"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={fileId}").AsNoTracking().ToListAsync(token);
            var scope=await _context.FileScopes.AsNoTracking().SingleOrDefaultAsync(x=>x.FileId==fileId,token);
            if(scope is null || scope.ProjectId!=projectId || scope.Purpose!=purpose)return false;
            await _context.FieldInspectionSessions.FromSqlInterpolated($"SELECT * FROM [FieldInspectionSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [EvidenceFileId]={fileId} OR [Id] IN (SELECT [FieldInspectionSessionId] FROM [GroundTruthMeasurements] WHERE [EvidenceFileId]={fileId})").AsNoTracking().ToListAsync(token);
            await _context.GroundTruthMeasurements.FromSqlInterpolated($"SELECT * FROM [GroundTruthMeasurements] WITH (UPDLOCK,HOLDLOCK) WHERE [EvidenceFileId]={fileId}").AsNoTracking().ToListAsync(token);
            var tasks=await (from session in _context.FieldInspectionSessions.AsNoTracking() join task in _context.FieldInspectionTasks.AsNoTracking() on session.FieldInspectionTaskId equals task.Id
                where session.ProjectId==projectId && task.ProjectId==projectId && task.LifecycleVersion==1 && session.Purpose==FieldInspectionPurpose.DefectVerification &&
                    session.RoadSectionVersionId==task.RoadSectionVersionId && session.SurveyId==task.SurveyId && (scope.TargetId==null || scope.TargetId==task.Id) &&
                    (session.EvidenceFileId==fileId || _context.GroundTruthMeasurements.Any(x=>x.FieldInspectionSessionId==session.Id && x.EvidenceFileId==fileId && x.DefectId==task.DefectId && x.RoadSectionVersionId==task.RoadSectionVersionId && x.SurveyId==task.SurveyId))
                select task.Id).Distinct().ToArrayAsync(token);
            foreach(var task in tasks)
            {
                try{await GuardFieldScopeAsync(actorUserId,projectId,task,purpose,false,token);return true;}
                catch(UploadNotFoundException){return false;}
            }
            return false;
        }
        if(_context.Database.CurrentTransaction is not null)return await Read(cancellationToken);
        return await MultipartTransactionAsync(Read,cancellationToken);
    }
    private async Task GuardFieldScopeAsync(Guid actor,Guid project,Guid? task,string purpose,bool forUpload,CancellationToken token)
    {
        await MultipartReceiptAuthority.LockAsync(_context,actor,project,token);
        var user=await _context.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==actor,token);
        if(user is null || !await new RoadGuardSystem.Repositories.Integration.AnhHuyFactsRepository(_context).IsCurrentActorAsync(actor,user.RoleCode,token))throw new UploadNotFoundException();
        var today=DateOnly.FromDateTime(_fieldClock.GetUtcNow().UtcDateTime);
        if(user.RoleCode!=UserRoleCode.Supervisor && !await _context.ProjectMembers.AnyAsync(x=>x.ProjectId==project && x.UserId==actor && x.RoleCode==user.RoleCode &&
            x.Status==ProjectMemberStatus.Active && x.ValidFrom<=today && (x.ValidTo==null || x.ValidTo>=today),token))throw new UploadNotFoundException();
        if(task is not Guid id)throw new UploadNotFoundException();
        await _context.FieldInspectionTasks.FromSqlInterpolated($"SELECT * FROM [FieldInspectionTasks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").AsNoTracking().ToListAsync(token);
        await _context.FieldInspectionAssignments.FromSqlInterpolated($"SELECT * FROM [FieldInspectionAssignments] WITH (UPDLOCK,HOLDLOCK) WHERE [FieldInspectionTaskId]={id}").AsNoTracking().ToListAsync(token);
        if(!await IsCurrentFieldActorCoreAsync(actor,user.RoleCode,project,id,purpose,forUpload,token))throw new UploadNotFoundException();
        if(forUpload && !await _context.Projects.AnyAsync(x=>x.Id==project && x.Status==ProjectStatus.Active,token))throw new UploadNotFoundException();
    }
}
