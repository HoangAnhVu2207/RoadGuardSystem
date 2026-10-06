using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineWorkflowServiceTests
{
    [Theory]
    [InlineData(UserRoleCode.Reporter,"device-register",403)]
    [InlineData(UserRoleCode.ProjectManager,"grant-issue",403)]
    [InlineData(UserRoleCode.RepairCrew,"grant-revoke",403)]
    [InlineData(UserRoleCode.ProjectManager,"unknown",400)]
    [InlineData(UserRoleCode.RepairCrew,"device-register",428)]
    public async Task InvalidRoleActionOrMissingKeyCannotReachProtectedRepository(UserRoleCode role,string action,int status)
    {
        var repository=new Repository();var service=new OfflineWorkflowService(repository,new Scope());
        var result=await service.ExecuteAsync(Guid.NewGuid(),role,Guid.NewGuid(),action,null,null,null,null,default);
        Assert.Equal(status,result.Status);Assert.Equal(0,repository.Calls);
    }
    [Fact]
    public async Task ReadUsesCurrentProjectGuardFromInsideRepository()
    {
        var repository=new Repository();var scope=new Scope();var caller=Guid.NewGuid();var project=Guid.NewGuid();
        var result=await new OfflineWorkflowService(repository,scope).ExecuteAsync(caller,UserRoleCode.RepairCrew,project,
            "snapshot-get",null,Guid.NewGuid(),null,null,default);
        Assert.Equal(200,result.Status);Assert.Equal(1,repository.Calls);Assert.Equal(caller,scope.Caller);Assert.Equal(project,scope.Project);
    }
    [Fact]
    public async Task InternalSpacesInNewOperationKeyAreRejectedBeforeAnyRepositoryCall()
    {
        var repository=new Repository();var result=await new OfflineWorkflowService(repository,new Scope()).ExecuteAsync(Guid.NewGuid(),
            UserRoleCode.RepairCrew,Guid.NewGuid(),"device-register",new OfflineDeviceRegisterInput(Guid.NewGuid(),"a","b"),null,"key with spaces",null,default);
        Assert.Equal(400,result.Status);Assert.Equal(0,repository.Calls);
    }
    private sealed class Scope:IProjectScopeGuard
    {
        public Guid Caller {get;private set;}
        public Guid Project {get;private set;}
        public Task<ProjectAccessScope?> AuthorizeAsync(Guid user,UserRoleCode role,Guid project,CancellationToken cancellationToken=default)
        {Caller=user;Project=project;return Task.FromResult<ProjectAccessScope?>(new(project,role,null));}
    }
    private sealed class Repository:IOfflineWorkflowRepository
    {
        public int Calls {get;private set;}
        public async Task<OfflineWorkflowFact> ExecuteAsync(OfflineWorkflowCommand command,OfflineWorkflowAlgorithms algorithms,
            Func<CancellationToken,Task<bool>> currentProjectGuard,CancellationToken cancellationToken)
        {Calls++;return new(await currentProjectGuard(cancellationToken)?200:403);}
    }
}
