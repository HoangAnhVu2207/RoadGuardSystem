using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Implementations.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4RepairProducerAdmissionTests
{
    [Theory]
    [InlineData("CREATE")]
    [InlineData("PROPOSE")]
    [InlineData("APPROVE")]
    [InlineData("ASSIGN")]
    public async Task MissingProducingKeyRequiresPreconditionBeforeAnyRepositoryCall(string action)
    {
        var repository = new Recording(); var result = await Execute(new(repository), action, null, Token);
        Assert.Equal(428, result.Status); Assert.False(repository.Called);
    }
    [Theory]
    [InlineData("CREATE")]
    [InlineData("PROPOSE")]
    [InlineData("APPROVE")]
    [InlineData("ASSIGN")]
    public async Task ProducingCommandsRequireExactEightByteRowVersionNotHistoryOrArbitraryQuotedToken(string action)
    {
        var repository = new Recording(); var result = await Execute(new(repository), action, "k", "\"h4-history-v1-test\"");
        Assert.Equal(400, result.Status); Assert.False(repository.Called);
    }
    [Fact]
    public async Task CrewCannotCreateRepairPackageEvenWithSyntacticallyValidHeaders()
    {
        var repository = new Recording(); var result = await new RepairProducerService(repository).CreatePackageAsync(
            Actor, UserRoleCode.RepairCrew, Project, PackageInput, "k", Token, default);
        Assert.Equal(403, result.Status); Assert.False(repository.Called);
    }
    [Fact]
    public async Task ValidPackageDelegationPreservesSourceVersionAndOriginalTypedBody()
    {
        var repository = new Recording(); var result = await new RepairProducerService(repository).CreatePackageAsync(
            Actor, UserRoleCode.ProjectManager, Project, PackageInput, "k", Token, default);
        Assert.Same(repository.Result, result); var command = Assert.IsType<RepairPackageCreateCommand>(repository.Command);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(PackageInput), System.Text.Json.JsonSerializer.Serialize(command.Input)); Assert.Equal(Token[1..^1], command.ExpectedDefectVersion);
        Assert.Equal("k", command.Key); Assert.Equal(Actor, command.ActorId);
    }
    private static readonly Guid Actor = Guid.NewGuid(), Project = Guid.NewGuid(), Package = Guid.NewGuid(), Item = Guid.NewGuid();
    private static readonly string Token = "\"" + Convert.ToBase64String(new byte[8]) + "\"";
    private static readonly RepairPackageCreateInput PackageInput = new(Guid.NewGuid(), Token[1..^1],
        [new("FORMAL_REPAIR", true, new(Guid.NewGuid(), Guid.NewGuid(), null, null, null, 1, 2, 0, 1), "mandatory")], "package reason");
    private static Task<RepairWorkflowResult> Execute(RepairProducerService service, string action, string? key, string? version)
        => action switch
        {
            "CREATE" => service.CreatePackageAsync(Actor, UserRoleCode.ProjectManager, Project, PackageInput, key, version, default),
            "PROPOSE" => service.ProposeItemAsync(Actor, UserRoleCode.ProjectManager, Project, Package,
                new(Guid.NewGuid(), "NORMAL", "plan", "checklist", "proposal"), key, version, default),
            "APPROVE" => service.ApproveItemAsync(Actor, UserRoleCode.Supervisor, Project, Package, Item, new("approval"), key, version, default),
            "ASSIGN" => service.AssignItemAsync(Actor, UserRoleCode.ProjectManager, Project, Package, Item,
                new(new FieldTaskCreateInput(Guid.NewGuid(), Token[1..^1], null, "REPORTER", Guid.NewGuid(), null,
                    null, null, "POST_REPAIR", 1, "{}", null, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1)), null, "assign"), key, version, default),
            _ => throw new ArgumentException("Unknown test action.")
        };
    private sealed class Recording : IRepairProducerRepository
    {
        public bool Called { get; private set; }
        public object? Command { get; private set; }
        public RepairWorkflowResult Result { get; } = new(201);
        private Task<RepairWorkflowResult> Capture(object command) { Called = true; Command = command; return Task.FromResult(Result); }
        public Task<RepairWorkflowResult> CreatePackageAsync(RepairPackageCreateCommand command, CancellationToken cancellationToken) => Capture(command);
        public Task<RepairWorkflowResult> ProposeItemAsync(RepairItemProposeCommand command, CancellationToken cancellationToken) => Capture(command);
        public Task<RepairWorkflowResult> ApproveItemAsync(RepairItemApprovalCommand command, CancellationToken cancellationToken) => Capture(command);
        public Task<RepairWorkflowResult> AssignItemAsync(RepairItemAssignCommand command, CancellationToken cancellationToken) => Capture(command);
    }
}
