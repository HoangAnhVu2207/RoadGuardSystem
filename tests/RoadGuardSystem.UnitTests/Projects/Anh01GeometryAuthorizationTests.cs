using FluentAssertions;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;
namespace RoadGuardSystem.UnitTests.Projects;

public sealed class Anh01GeometryAuthorizationTests
{
    [Theory]
    [InlineData(UserRoleCode.ProjectManager,"confirm")]
    [InlineData(UserRoleCode.Supervisor,"draft-create")]
    [InlineData(UserRoleCode.DroneOperator,"set-create")]
    [InlineData(UserRoleCode.Reporter,"package")]
    public async Task Wrong_role_is_forbidden_before_persistence(UserRoleCode role,string action)
    {
        var repo=new Repository();var service=new GeometryWorkflowService(repo,new Guard(true));
        var result=await service.ExecuteAsync(role,Command(action),default);
        result.Status.Should().Be(403);repo.Executed.Should().BeFalse();
    }
    [Fact] public async Task Replay_does_not_bypass_revoked_membership()
    {
        var repo=new Repository();var service=new GeometryWorkflowService(repo,new Guard(false));
        var result=await service.ExecuteAsync(UserRoleCode.ProjectManager,Command("draft-create"),default);
        result.Status.Should().Be(403);repo.Executed.Should().BeFalse();
    }
    [Fact] public async Task Operator_package_requires_current_assignment()
    {
        var repo=new Repository();var service=new GeometryWorkflowService(repo,new Guard(true));
        var result=await service.ExecuteAsync(UserRoleCode.DroneOperator,Command("package"),default);
        result.Status.Should().Be(403);repo.Executed.Should().BeFalse();
        repo.Assigned=true;
        (await service.ExecuteAsync(UserRoleCode.DroneOperator,Command("package"),default)).Status.Should().Be(200);
    }
    [Fact] public async Task Missing_etag_is_precondition_required()
    {
        var repo=new Repository();var service=new GeometryWorkflowService(repo,new Guard(true));
        var result=await service.ExecuteAsync(UserRoleCode.ProjectManager,Command("set-edit") with {ExpectedVersion=null},default);
        result.Status.Should().Be(428);repo.Executed.Should().BeFalse();
    }
    private static GeometryWorkflowCommand Command(string action)=>new(Guid.NewGuid(),Guid.NewGuid(),action,null,null,Guid.NewGuid(),Guid.NewGuid(),null,"key","version");
    private sealed class Guard(bool active):IProjectScopeGuard
    {
        public Task<ProjectAccessScope?> AuthorizeAsync(Guid userId,UserRoleCode authoritativeRole,Guid projectId,CancellationToken cancellationToken=default)=>Task.FromResult(active?new ProjectAccessScope(projectId,authoritativeRole,null):null);
    }
    private sealed class Repository:IGeometryWorkflowRepository
    {
        public bool Assigned {get;set;}public bool Executed{get;private set;}
        public Task<bool> CanReadAssignedGeometryAsync(Guid actorId,Guid projectId,Guid routeVersionId,Guid setId,CancellationToken cancellationToken)=>Task.FromResult(Assigned);
        public Task<GeometryWorkflowResult> ExecuteAsync(GeometryWorkflowCommand command,Func<GeometryDraftInput,int,GeometryPreview> preview,Func<Guid,NetTopologySuite.Geometries.LineString,double,SegmentDefinition,SegmentPreview> segments,CancellationToken cancellationToken){Executed=true;return Task.FromResult(new GeometryWorkflowResult(200));}
    }
}
