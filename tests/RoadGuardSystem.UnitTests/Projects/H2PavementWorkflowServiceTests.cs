using FluentAssertions;
using NetTopologySuite.Geometries;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;
namespace RoadGuardSystem.UnitTests.Projects;

public sealed class H2PavementWorkflowServiceTests
{
    [Theory]
    [InlineData(UserRoleCode.Supervisor, "plan-create", 403)]
    [InlineData(UserRoleCode.RepairCrew, "manifest", 403)]
    [InlineData(UserRoleCode.ProjectManager, "unknown", 400)]
    [InlineData(UserRoleCode.ProjectManager, "plan-create", 428)]
    [InlineData(UserRoleCode.ProjectManager, "page", 428)]
    public async Task InvalidAuthorityOrTransport_DoesNotReachRepository(UserRoleCode role, string action, int expected)
    {
        var repository = new Repository();
        var service = new PavementWorkflowService(repository, new Scope());
        var result = await service.ExecuteAsync(role, new(Guid.NewGuid(), Guid.NewGuid(), action));
        result.Status.Should().Be(expected); repository.Calls.Should().Be(0);
    }
    [Fact]
    public async Task AuthorizedRead_DelegatesCurrentScopeGuard()
    {
        var repository = new Repository(); var scope = new Scope();
        var result = await new PavementWorkflowService(repository, scope).ExecuteAsync(UserRoleCode.ProjectManager,
            new(Guid.NewGuid(), Guid.NewGuid(), "layout-get"));
        result.Status.Should().Be(200); repository.Calls.Should().Be(1); scope.Calls.Should().Be(1);
    }
    private sealed class Scope : IProjectScopeGuard
    {
        public int Calls;
        public Task<ProjectAccessScope?> AuthorizeAsync(Guid user, UserRoleCode role, Guid project, CancellationToken cancellationToken = default)
        { Calls++; return Task.FromResult<ProjectAccessScope?>(new(project, role, null)); }
    }
    private sealed class Repository : IPavementWorkflowRepository
    {
        public int Calls;
        public async Task<GeometryWorkflowResult> ExecuteAsync(UserRoleCode role, PavementWorkflowCommand command,
            Func<CancellationToken, Task<bool>> scopeGuard,
            Func<GeometryDraftInputFact?, LineString, PavementPlanCreateInputFact, PavementGeometryPreviewFact> plan,
            Func<PavementGeometryPreviewFact, AsBuiltLayoutInputFact, PavementGeometryPreviewFact> asBuilt,
            Func<GeometryDraftInputFact, int, GeometryPreviewFact> geometryPreview,
            Func<GeometryMapSnapshotFact, PavementLayerQuery, string, GeometryMapPageFact> page, CancellationToken cancellationToken)
        { Calls++; return new(await scopeGuard(cancellationToken) ? 200 : 403); }
    }
}
