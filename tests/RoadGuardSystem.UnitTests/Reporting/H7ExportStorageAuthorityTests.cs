using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Exports;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Exports;
using RoadGuardSystem.Services.Integration;
using Xunit;

namespace RoadGuardSystem.UnitTests.Reporting;

// Controlled storage/authority race; actual SQL source authority has separate integration evidence.
public sealed class H7ExportStorageAuthorityTests
{
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task SnapshotAuthorityQueryPreservesAbsentEmptyAndPrivateSourceScope(int shape)
    {
        var actor = Guid.NewGuid(); var project = Guid.NewGuid(); var caseId = Guid.NewGuid();
        var defectId = Guid.NewGuid(); var repairId = Guid.NewGuid(); var snapshotId = Guid.NewGuid();
        var caseProject = Guid.NewGuid();
        var at = new DateTimeOffset(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
        CaseDefectSnapshotV1? caseFacts = shape is 1 or 2 ? new("case-defect.v1", Guid.NewGuid(), caseProject,
            at, new string('a', 64), shape == 2 ? [new(caseId, caseProject, "old-case-version", "OPEN", [], [], [])] : [],
            shape == 2 ? [new(defectId, caseProject, "old-defect-version", "OPEN", "Case", caseId,
                "old-case-version", null, null, null, null, "UNKNOWN")] : [], [], []) : null;
        ReportingMetricDto[] metrics = [new("repairItemsByStatus", new(), shape == 3 ? 0 : null, "count",
            null, null, false, shape == 3 ? "AVAILABLE" : "UNAVAILABLE", [], [])];
        var dossier = new ReportingCaptureDto(new("reporting.project-summary.v1", project, at, "v1", new(),
            metrics, [], "SERIALIZABLE"), [], [], [], new("PARTIAL", []), caseFacts);
        ExportSourceRevisionDto[] revisions = shape == 4
            ? [new("RepairItem", repairId, "historical-version"), new("RepairUnknown", repairId, "historical-version"),
               new("repairItem", Guid.NewGuid(), "v1")] : [];
        var payload = new ExportSnapshotPayloadDto(new("anh02.export.v1", snapshotId, project, "DOSSIER", "ZIP",
            actor, at, at, [], new("DOSSIER", "ZIP"), revisions, [], [], null, ""), dossier);
        var json = JsonSerializer.Serialize(payload, Json);
        var snapshot = ExportSnapshot.Create(snapshotId, project, json, ExportSerialization.Hash(json), at);
        var job = ExportJob.Create(Guid.NewGuid(), project, actor, snapshotId, "DOSSIER", "ZIP", at);
        var repository = Proxy<IExportRepository>((method, args) => method.Name switch
        {
            nameof(IExportRepository.GetAsync) => Task.FromResult<ExportPersistenceView?>(new(job, snapshot, null)),
            nameof(IExportRepository.CanReadSnapshotSourcesAsync) => CheckAuthority((ExportSourceAuthorityQuery)args![2]!),
            _ => throw new InvalidOperationException("Unexpected repository call: " + method.Name)
        });
        Task<bool> CheckAuthority(ExportSourceAuthorityQuery query)
        {
            Assert.Equal(project, query.ManifestProjectId);
            Assert.Equal(shape == 3, query.HasAvailableRepairMetric);
            Assert.Equal(shape == 4 ? new[] { "RepairItem", "RepairUnknown" } : [], query.RepairSources.Select(value => value.Kind).ToArray());
            Assert.All(query.RepairSources, value => Assert.Equal(repairId, value.Id));
            if (shape is 1 or 2)
            {
                Assert.NotNull(query.CaseFacts); Assert.Equal(caseProject, query.CaseFacts.ProjectId);
                Assert.Equal(shape == 2 ? new[] { caseId } : [], query.CaseFacts.CaseIds);
                Assert.Equal(shape == 2 ? new[] { defectId } : [], query.CaseFacts.DefectIds);
            }
            else Assert.Null(query.CaseFacts);
            return Task.FromResult(true);
        }
        var identity = Proxy<IIdentityRepository>((method, _) => method.Name switch
        {
            nameof(IIdentityRepository.GetUserSecurityStateAsync) => Task.FromResult<UserSecurityState?>(
                new(actor, "fixture", "fixture", UserRoleCode.ProjectManager, UserStatus.Active, false, [])),
            nameof(IIdentityRepository.IsRoleActiveAsync) => Task.FromResult(true),
            _ => throw new InvalidOperationException("Unexpected identity call: " + method.Name)
        });
        var scope = Proxy<IProjectScopeGuard>((_, _) => Task.FromResult<ProjectAccessScope?>(
            new(project, UserRoleCode.ProjectManager, Guid.NewGuid())));
        using var services = new ServiceCollection().BuildServiceProvider();
        var service = new ExportService(repository, identity, scope, null!, [], [], null!, null!, null!,
            TimeProvider.System, services.GetRequiredService<IServiceScopeFactory>());
        var result = await service.ManifestAsync(actor, project, job.Id, default);
        Assert.Equal("success", result.Code); Assert.NotNull(result.Value);
        Assert.Equal(snapshot.Hash, result.Value.SnapshotHash);
        Assert.Equal(json, snapshot.PayloadJson); Assert.Equal(ExportSerialization.Hash(json), snapshot.Hash);
    }

    [Theory]
    [InlineData(false,false)]
    [InlineData(false,true)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public async Task RevocationDuringStorageWaitDoesNotReturnSourceBytesToRenderer(bool frame,bool revokeActor)
    {
        var actor=Guid.NewGuid();var project=Guid.NewGuid();var at=DateTimeOffset.UtcNow;
        var source=StoredFile.Create(Guid.NewGuid(),"private/source","source.jpg","image/jpeg",4,new string('a',64),actor,at,null);
        var file=new ExportFileDto(source.Id,"v1",source.Checksum,4,"image/jpeg","files/source.jpg",true,null);
        ExportLabelDto[]? labels=frame?[new(Guid.NewGuid(),1,Guid.NewGuid(),project,"CRACK",0,0,1,1,
            source.Id,"v1",source.Checksum,4,"image/jpeg","AI_DETECTION",Guid.NewGuid(),"v1",Guid.NewGuid(),
            actor,at,null,null,null,"MOCK",null)]:null;
        var id=Guid.NewGuid();var kind=frame?"TRAINING":"DOSSIER";
        var payload=new ExportSnapshotPayloadDto(new("anh02.export.v1",id,project,kind,"ZIP",actor,at,at,[],
            new(kind,"ZIP",IncludeOriginalFiles:true),[new("RepairItem",Guid.NewGuid(),"v1")],[file],[],labels,""),null);
        var json=JsonSerializer.Serialize(payload,Json);var snapshot=ExportSnapshot.Create(id,project,json,ExportSerialization.Hash(json),at);
        var claim=new ExportClaim(Guid.NewGuid(),Guid.NewGuid(),actor,project,snapshot);
        var sourceAuthority=true;var actorAuthority=true;var reads=0;var writes=0;string? error=null;
        var opened=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var content=new ObservedStream();
        async Task<Stream> Open()
        {
            opened.TrySetResult();await release.Task;return content;
        }
        var repository=Proxy<IExportRepository>((method,args)=>method.Name switch
        {
            nameof(IExportRepository.ClaimAsync)=>Task.FromResult<ExportClaim?>(claim),
            nameof(IExportRepository.RenewAsync)=>Task.FromResult(true),
            nameof(IExportRepository.CanReadSnapshotSourcesAsync)=>Task.FromResult(sourceAuthority),
            nameof(IExportRepository.CanReadSurveySourcesAsync)=>Task.FromResult(true),
            nameof(IExportRepository.GetSourceFileAsync)=>Task.FromResult<StoredFile?>(source),
            nameof(IExportRepository.CompleteAsync)=>Complete((Func<CancellationToken,Task<bool>>)args![2]!),
            nameof(IExportRepository.FailAsync)=>Fail((string)args![1]!),
            _=>throw new InvalidOperationException("Unexpected export repository call: "+method.Name)
        });
        async Task<bool> Complete(Func<CancellationToken,Task<bool>> authorize)=>await authorize(default);
        Task Fail(string code){error=code;return Task.CompletedTask;}
        var identity=Proxy<IIdentityRepository>((method,_)=>method.Name switch
        {
            nameof(IIdentityRepository.GetUserSecurityStateAsync)=>Task.FromResult<UserSecurityState?>(actorAuthority
                ?new(actor,"fixture","fixture",UserRoleCode.ProjectManager,UserStatus.Active,false,[]):null),
            nameof(IIdentityRepository.IsRoleActiveAsync)=>Task.FromResult(true),
            _=>throw new InvalidOperationException("Unexpected identity call: "+method.Name)
        });
        var scope=Proxy<IProjectScopeGuard>((_,_)=>Task.FromResult<ProjectAccessScope?>(new(project,UserRoleCode.ProjectManager,Guid.NewGuid())));
        var sourceAccess=Proxy<ITrainingSourceAccessReader>((_,_)=>Task.FromResult(true));
        var artifacts=Proxy<IAnh02ArtifactStore>((method,args)=>method.Name switch
        {
            nameof(IAnh02ArtifactStore.OpenReadAsync)=>ReadArtifact((string)args![0]!),
            nameof(IAnh02ArtifactStore.WriteAsync)=>WriteArtifact((string)args![0]!),
            _=>throw new InvalidOperationException("Unexpected artifact call: "+method.Name)
        });
        async Task<Anh02ArtifactRead> ReadArtifact(string key)
        {
            if(key!=source.StorageUri)throw new FileNotFoundException();
            return new(await Open(),new(key,4,source.Checksum,"image/jpeg"));
        }
        Task<Anh02ArtifactMetadata> WriteArtifact(string key)
        {writes++;return Task.FromResult(new Anh02ArtifactMetadata(key,1,new string('b',64),"application/zip"));}
        var storage=Proxy<IUploadObjectStorage>((method,_)=>method.Name==nameof(IUploadObjectStorage.OpenReadAsync)
            ?Open():throw new InvalidOperationException("Unexpected source storage call: "+method.Name));
        var renderer=Proxy<IExportRenderer>((_,args)=>Render((Func<ExportFileDto,CancellationToken,Task<Stream>>)args![1]!));
        async Task<Stream> Render(Func<ExportFileDto,CancellationToken,Task<Stream>> open)
        {
            await using var stream=await open(file,default);reads+=stream.ReadByte()>=0?1:0;
            return new MemoryStream([9]);
        }
        using var services=new ServiceCollection().AddScoped<IExportRepository>(_=>repository).BuildServiceProvider();
        var service=new ExportService(repository,identity,scope,null!,[],[sourceAccess],artifacts,storage,renderer,
            TimeProvider.System,services.GetRequiredService<IServiceScopeFactory>());
        var processing=service.ProcessNextAsync(default);
        await opened.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if(revokeActor)actorAuthority=false;else sourceAuthority=false;
        release.TrySetResult();Assert.True(await processing.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(0,reads);Assert.Equal(0,writes);Assert.True(content.Disposed);
        Assert.Equal("export_source_access_revoked",error);
    }

    private static T Proxy<T>(Func<MethodInfo,object?[]?,object?> handler) where T:class
    {
        var result=DispatchProxy.Create<T,ControlledProxy>();
        ((ControlledProxy)(object)result).Handler=handler;return result;
    }
    public class ControlledProxy:DispatchProxy
    {
        public Func<MethodInfo,object?[]?,object?> Handler {get;set;}=null!;
        protected override object? Invoke(MethodInfo? targetMethod,object?[]? args)=>Handler(targetMethod!,args);
    }
    private sealed class ObservedStream:MemoryStream
    {
        public ObservedStream():base([1,2,3,4]){}
        public bool Disposed {get;private set;}
        protected override void Dispose(bool disposing){Disposed=true;base.Dispose(disposing);}
    }
}
