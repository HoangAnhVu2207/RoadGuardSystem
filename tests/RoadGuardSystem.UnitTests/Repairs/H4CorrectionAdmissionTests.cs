using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Implementations.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4CorrectionAdmissionTests
{
    private readonly RecordingRepository repository = new();
    private readonly Guid actor = Guid.NewGuid(), project = Guid.NewGuid(), package = Guid.NewGuid(), item = Guid.NewGuid();
    private static RepairCorrectionInput Input() => new(Guid.NewGuid(), "UNREPAIRED", "actual correction reason", new("observed basis", []));
    private Task<RepairWorkflowResult> Run(UserRoleCode role = UserRoleCode.ProjectManager, string? key = "correction-1",
        string? version = "\"AQIDBAUGBwg=\"", RepairCorrectionInput? input = null) => new RepairWorkflowService(repository)
        .CorrectAsync(actor, role, project, package, item, input ?? Input(), key, version, CancellationToken.None);

    [Theory]
    [InlineData(UserRoleCode.RepairCrew)]
    [InlineData(UserRoleCode.Unknown)]
    public async Task UnsupportedRolesNeverReachProtectedRepository(UserRoleCode role)
    { Assert.Equal(403, (await Run(role)).Status); Assert.Null(repository.Command); }

    [Theory]
    [InlineData(null, "\"AQIDBA==\"")]
    [InlineData("correction-1", null)]
    public async Task MutationRequiresBothReceiptKeyAndVersion(string? key, string? version)
    { Assert.Equal(428, (await Run(key: key, version: version)).Status); Assert.Null(repository.Command); }

    [Theory]
    [InlineData("bad key", "\"AQIDBA==\"")]
    [InlineData("correction-1", "*")]
    [InlineData("correction-1", "\"AQIDBA==\",\"BQ==\"")]
    public async Task MalformedKeyOrMultiVersionDoesNotEnterTransaction(string key, string version)
    { Assert.Equal(400, (await Run(key: key, version: version)).Status); Assert.Null(repository.Command); }

    [Fact]
    public async Task AdmissionPreservesActualScopeAndPassesUnquotedExactVersion()
    {
        var result = await Run(); Assert.Equal(201, result.Status);
        Assert.Equal(actor, repository.Command!.ActorId); Assert.Equal(project, repository.Command.ProjectId);
        Assert.Equal(package, repository.Command.PackageId); Assert.Equal(item, repository.Command.ItemId);
        Assert.Equal("AQIDBAUGBwg=", repository.Command.ExpectedVersion);
    }

    [Fact]
    public async Task EmptyCorrectionHeadDoesNotBecomeLatestHeadWildcard()
    { Assert.Equal(400, (await Run(input: Input() with { SupersedesDecisionId = Guid.Empty })).Status); Assert.Null(repository.Command); }

    [Fact]
    public async Task UnknownEffectiveResultCannotBeStoredAsOwnerDecision()
    { Assert.Equal(400, (await Run(input: Input() with { Result = "RESOLVED" })).Status); Assert.Null(repository.Command); }

    [Theory]
    [InlineData("\"not-base64\"")]
    [InlineData("\"AQIDBA==\"")]
    [InlineData("\" AQIDBAUGBwg=\"")]
    [InlineData("\"AQIDBAUGBwgJ\"")]
    public async Task QuotedVersionMustBeCanonicalEightByteRowVersion(string version)
    { Assert.Equal(400, (await Run(version: version)).Status); Assert.Null(repository.Command); }

    private sealed class RecordingRepository : IRepairWorkflowRepository
    {
        public RepairCorrectionCommand? Command { get; private set; }
        public Task<RepairWorkflowResult> RequestReviewAsync(RepairReviewRequestCommand command,
            CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<RepairWorkflowResult> CorrectAsync(RepairCorrectionCommand command, CancellationToken cancellationToken)
        { Command = command; return Task.FromResult(new RepairWorkflowResult(201)); }
        public Task<RepairWorkflowResult> ReadItemAsync(Guid actorId, UserRoleCode role, Guid projectId,
            Guid packageId, Guid itemId, bool history, CancellationToken cancellationToken) => Task.FromResult(new RepairWorkflowResult(200));
    }
}
