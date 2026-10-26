using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Projects;
using Xunit;

namespace RoadGuardSystem.UnitTests.Projects;

// Pure service characterization. No EF, SQL, Docker, HTTP or session factory.
public sealed class SandboxProjectUpdateBoundaryTests
{
    private static UpdateProjectCommand Command(string version) =>
        new("Synthetic", null, null, null, null, version, Guid.NewGuid(), Guid.NewGuid());

    [Theory]
    [InlineData(UserRoleCode.Unknown)]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.DroneOperator)]
    [InlineData(UserRoleCode.RepairCrew)]
    [InlineData(UserRoleCode.Reporter)]
    public async Task NonSupervisorNeverReachesPersistence(UserRoleCode role)
    {
        var repo = new Probe();
        var result = await new ProjectUpdateService(repo).UpdateAsync(Guid.NewGuid(), role,
            Guid.NewGuid(), Command(Convert.ToBase64String(new byte[8])));
        Assert.Equal(ProjectUpdateStatus.Forbidden, result.Status);
        Assert.Null(repo.Request);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-base64")]
    [InlineData("AQ==")]
    [InlineData("AAAAAAAAAAAA")]
    public async Task MalformedOrNonEightByteVersionCannotWrite(string version)
    {
        var repo = new Probe();
        var result = await new ProjectUpdateService(repo).UpdateAsync(Guid.NewGuid(), UserRoleCode.Supervisor,
            Guid.NewGuid(), Command(version));
        Assert.Equal(ProjectUpdateStatus.InvalidInput, result.Status);
        Assert.Null(repo.Request);
    }

    [Theory]
    [InlineData(ProjectUpdatePersistenceStatus.StaleConcurrency, ProjectUpdateStatus.StaleConcurrency)]
    [InlineData(ProjectUpdatePersistenceStatus.IdempotentConflict, ProjectUpdateStatus.IdempotentConflict)]
    [InlineData(ProjectUpdatePersistenceStatus.ActorNotAuthorized, ProjectUpdateStatus.Forbidden)]
    [InlineData(ProjectUpdatePersistenceStatus.NotFound, ProjectUpdateStatus.NotFound)]
    [InlineData(ProjectUpdatePersistenceStatus.Replayed, ProjectUpdateStatus.Replayed)]
    public async Task RepositoryOutcomeIsNotPromotedAndExactConcurrencyContextIsForwarded(
        ProjectUpdatePersistenceStatus persisted, ProjectUpdateStatus expected)
    {
        var repo = new Probe { Status = persisted };
        var actor = Guid.NewGuid(); var project = Guid.NewGuid();
        byte[] version = [1, 2, 3, 4, 5, 6, 7, 8];
        var command = Command(Convert.ToBase64String(version));
        using var cancellation = new CancellationTokenSource();
        var result = await new ProjectUpdateService(repo).UpdateAsync(actor, UserRoleCode.Supervisor,
            project, command, cancellation.Token);
        Assert.Equal(expected, result.Status);
        Assert.Null(result.Project);
        Assert.NotNull(repo.Request);
        Assert.Equal(actor, repo.Request.ActorUserId);
        Assert.Equal(project, repo.Request.ProjectId);
        Assert.Equal(command.OperationId, repo.Request.OperationId);
        Assert.Equal(command.CorrelationId, repo.Request.CorrelationId);
        Assert.Equal(version, repo.Request.ExpectedRowVersion);
        Assert.Equal(cancellation.Token, repo.Token);
    }

    private sealed class Probe : IProjectUpdateRepository
    {
        public ProjectUpdatePersistenceStatus Status { get; init; }
        public ProjectUpdatePersistenceRequest? Request { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task<ProjectUpdatePersistenceResult> UpdateAsync(ProjectUpdatePersistenceRequest request,
            CancellationToken cancellationToken = default)
        {
            Assert.Null(Request); Request = request; Token = cancellationToken;
            return Task.FromResult(new ProjectUpdatePersistenceResult(Status));
        }
    }
}
