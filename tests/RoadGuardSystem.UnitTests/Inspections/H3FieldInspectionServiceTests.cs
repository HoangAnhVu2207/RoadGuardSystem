using FluentAssertions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Implementations.Inspections;
using Xunit;

namespace RoadGuardSystem.UnitTests.Inspections;
public sealed class H3FieldInspectionServiceTests
{

    [Fact]
    public async Task ReporterCannotCreateFieldTasks()
    {
        var result=await Service().ExecuteAsync(Guid.NewGuid(),UserRoleCode.Reporter,Guid.NewGuid(),null,"create",null,"key",null,default);
        result.Status.Should().Be(403);
    }

    [Fact]
    public async Task CrewCannotReviewFieldResults()
    {
        var result=await Service().ExecuteAsync(Guid.NewGuid(),UserRoleCode.RepairCrew,Guid.NewGuid(),Guid.NewGuid(),"review",null,"key","version",default);
        result.Status.Should().Be(403);
    }

    [Fact]
    public async Task MutationRequiresIdempotencyKey()
    {
        var result=await Service().ExecuteAsync(Guid.NewGuid(),UserRoleCode.ProjectManager,Guid.NewGuid(),null,"create",null,null,null,default);
        result.Status.Should().Be(428);
    }

    [Fact]
    public async Task MutationRejectsNonVisibleOperationKey()
    {
        var result=await Service().ExecuteAsync(Guid.NewGuid(),UserRoleCode.ProjectManager,Guid.NewGuid(),null,"create",null,"key space",null,default);
        result.Status.Should().Be(400);
    }

    [Fact]
    public async Task TaskMutationRequiresVersion()
    {
        var result=await Service().ExecuteAsync(Guid.NewGuid(),UserRoleCode.RepairCrew,Guid.NewGuid(),Guid.NewGuid(),"start",null,"key",null,default);
        result.Status.Should().Be(428);
    }

    [Fact]
    public async Task ReadDelegatesCurrentAdmissionToRepository()
    {
        var repo=new Repository();var actor=Guid.NewGuid();
        var result=await new FieldInspectionWorkflowService(repo,new Scope()).ExecuteAsync(actor,UserRoleCode.RepairCrew,
            Guid.NewGuid(),Guid.NewGuid(),"get",null,null,null,default);
        result.Status.Should().Be(200);
        repo.Command!.Admission.CallerId.Should().Be(actor);
        repo.Command.Admission.OriginalActorId.Should().Be(actor);
        repo.Command.Admission.Mode.Should().Be("DIRECT");
    }
    private static FieldInspectionWorkflowService Service()=>new(new Repository(),new Scope());
    private sealed class Repository:IFieldInspectionWorkflowRepository
    {
        public FieldWorkflowCommand? Command {get;private set;}
        public Task<FieldWorkflowResult> ExecuteAsync(FieldWorkflowCommand command,Func<CancellationToken,Task<bool>> projectGuard,CancellationToken cancellationToken)
        {Command=command;return Task.FromResult(new FieldWorkflowResult(200));}
        public Task<FieldWorkflowResult> ApplyInTransactionAsync(FieldWorkflowCommand command,Func<CancellationToken,Task<bool>> projectGuard,CancellationToken cancellationToken)=>ExecuteAsync(command,projectGuard,cancellationToken);
    }
    private sealed class Scope:IProjectScopeGuard
    {
        public Task<ProjectAccessScope?> AuthorizeAsync(Guid userId,UserRoleCode authoritativeRole,Guid projectId,CancellationToken cancellationToken=default)
            =>Task.FromResult<ProjectAccessScope?>(new(projectId,authoritativeRole,null));
    }
}
