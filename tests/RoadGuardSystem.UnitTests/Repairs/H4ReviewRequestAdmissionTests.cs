using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Implementations.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4ReviewRequestAdmissionTests
{
    private readonly RecordingRepository repository = new();
    private readonly Guid actor = Guid.NewGuid(), project = Guid.NewGuid(), package = Guid.NewGuid(), item = Guid.NewGuid();
    private Task<RepairWorkflowResult> Run(UserRoleCode role = UserRoleCode.ProjectManager, string? key = "review-request-1",
        string? version = "\"AQIDBAUGBwg=\"", string reason = "Please reconsider the recorded result")
        => new RepairWorkflowService(repository).RequestReviewAsync(actor, role, project, package, item,
            new(reason), key, version, CancellationToken.None);

    [Theory]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.RepairCrew)]
    public async Task ConfirmedRequestRolesReachScopedProducerWithoutCorrectionResult(UserRoleCode role)
    {
        Assert.Equal(201, (await Run(role)).Status);
        Assert.Equal(role, repository.Command!.Role); Assert.Equal(actor, repository.Command.ActorId);
        Assert.Equal(item, repository.Command.ItemId); Assert.Equal("AQIDBAUGBwg=", repository.Command.ExpectedVersion);
        Assert.Equal("Please reconsider the recorded result", repository.Command.Input.Reason);
    }
    [Theory]
    [InlineData(UserRoleCode.Supervisor)]
    [InlineData(UserRoleCode.Unknown)]
    public async Task RequestDoesNotInferAnotherRoleAuthority(UserRoleCode role)
    { Assert.Equal(403, (await Run(role)).Status); Assert.Null(repository.Command); }

    [Theory]
    [InlineData(null, "\"AQIDBAUGBwg=\"")]
    [InlineData("review-request-1", null)]
    public async Task KeyAndHeadVersionAreRequired(string? key, string? version)
    { Assert.Equal(428, (await Run(key: key, version: version)).Status); Assert.Null(repository.Command); }

    [Theory]
    [InlineData("bad key", "\"AQIDBAUGBwg=\"", "reason")]
    [InlineData("review-request-1", "\"AQIDBA==\"", "reason")]
    [InlineData("review-request-1", "\"AQIDBAUGBwg=\"", " ")]
    public async Task InvalidRequestNeverEntersReceiptProducer(string key, string version, string reason)
    { Assert.Equal(400, (await Run(key: key, version: version, reason: reason)).Status); Assert.Null(repository.Command); }

    private sealed class RecordingRepository : IRepairWorkflowRepository
    {
        public RepairReviewRequestCommand? Command { get; private set; }
        public Task<RepairWorkflowResult> RequestReviewAsync(RepairReviewRequestCommand command, CancellationToken cancellationToken)
        { Command = command; return Task.FromResult(new RepairWorkflowResult(201)); }
        public Task<RepairWorkflowResult> CorrectAsync(RepairCorrectionCommand command, CancellationToken cancellationToken)
            => throw new InvalidOperationException("A request must not invoke correction.");
        public Task<RepairWorkflowResult> ReadItemAsync(Guid actorId, UserRoleCode role, Guid projectId,
            Guid packageId, Guid itemId, bool history, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
