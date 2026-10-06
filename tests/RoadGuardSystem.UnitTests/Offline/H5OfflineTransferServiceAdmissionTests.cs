using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineTransferServiceAdmissionTests
{
    [Theory]
    [InlineData("package-prepare", UserRoleCode.Supervisor)]
    [InlineData("artifact-register", UserRoleCode.Supervisor)]
    [InlineData("artifact-register", UserRoleCode.RepairCrew)]
    [InlineData("artifact-get", UserRoleCode.ProjectManager)]
    public async Task Actual_transfer_action_passes_typed_command_to_repository_with_current_project_guard(string action, UserRoleCode role)
    {
        var repository = new Repository();
        var actor = Guid.NewGuid(); var project = Guid.NewGuid();
        var result = await new OfflineWorkflowService(repository, new Scope()).ExecuteAsync(actor, role, project,
            action, new { retainedSignedInput = true }, action == "artifact-get" ? Guid.NewGuid() : null,
            action == "artifact-get" ? null : "transfer-key", null, default);
        Assert.Equal(200, result.Status);
        Assert.NotNull(repository.Command);
        Assert.Equal(actor, repository.Command.ActorId);
        Assert.Equal(project, repository.Command.ProjectId);
        Assert.Equal(action, repository.Command.Action);
        Assert.True(repository.Guarded);
    }

    [Fact]
    public async Task Package_preparation_does_not_grant_Supervisor_self_export_or_Crew_preparation()
    {
        var repository = new Repository(); var service = new OfflineWorkflowService(repository, new Scope());
        Assert.Equal(403, (await service.ExecuteAsync(Guid.NewGuid(), UserRoleCode.RepairCrew, Guid.NewGuid(),
            "package-prepare", null, null, "key", null, default)).Status);
        Assert.Equal(403, (await service.ExecuteAsync(Guid.NewGuid(), UserRoleCode.Supervisor, Guid.NewGuid(),
            "package-export", null, null, "key", null, default)).Status);
        Assert.Null(repository.Command);
    }

    private sealed class Repository : IOfflineWorkflowRepository
    {
        public OfflineWorkflowCommand? Command { get; private set; }
        public bool Guarded { get; private set; }
        public async Task<OfflineWorkflowFact> ExecuteAsync(OfflineWorkflowCommand command, OfflineWorkflowAlgorithms algorithms,
            Func<CancellationToken, Task<bool>> currentProjectGuard, CancellationToken cancellationToken)
        {
            Command = command; Guarded = await currentProjectGuard(cancellationToken); return new(Guarded ? 200 : 403);
        }
    }

    private sealed class Scope : IProjectScopeGuard
    {
        public Task<ProjectAccessScope?> AuthorizeAsync(Guid userId, UserRoleCode authoritativeRole, Guid projectId, CancellationToken cancellationToken = default)
            => Task.FromResult<ProjectAccessScope?>(new(projectId, authoritativeRole, null));
    }
}
