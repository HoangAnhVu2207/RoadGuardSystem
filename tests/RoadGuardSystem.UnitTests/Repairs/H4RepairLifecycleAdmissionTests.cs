using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Implementations.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4RepairLifecycleAdmissionTests
{
    [Theory]
    [InlineData(UserRoleCode.RepairCrew, "key", "\"AAAAAAAAAAA=\"", 403)]
    [InlineData(UserRoleCode.Supervisor, "key", "\"AAAAAAAAAAA=\"", 403)]
    [InlineData(UserRoleCode.ProjectManager, null, "\"AAAAAAAAAAA=\"", 428)]
    [InlineData(UserRoleCode.ProjectManager, "key", null, 428)]
    [InlineData(UserRoleCode.ProjectManager, "key", "W/\"AAAAAAAAAAA=\"", 400)]
    public async Task CancellationRejectsBeforeRepository(UserRoleCode role, string? key, string? version, int status)
    {
        var repo = new Capture(); var service = new RepairLifecycleService(repo);
        var result = await service.CancelItemAsync(Guid.NewGuid(), role, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new("reason", null), key, version, default);
        Assert.Equal(status, result.Status); Assert.Null(repo.Command);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ContinuationPreservesWireDataAndReceiptMetadata(bool withHandover)
    {
        var repo = new Capture(); var service = new RepairLifecycleService(repo);
        var input = new RepairNormalContinuationInput("new plan", "checklist", "normal continuation",
            withHandover ? new(Guid.NewGuid(), Guid.NewGuid(), "barrier retained", "area safe") : null);
        var result = await service.ContinueNormallyAsync(Guid.NewGuid(), UserRoleCode.ProjectManager,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), input, "key", "\"AAAAAAAAAAA=\"", default);
        var command = Assert.IsType<RepairNormalContinuationCommand>(repo.Command);
        Assert.Equal(JsonSerializer.Serialize(input), JsonSerializer.Serialize(command.Input));
        Assert.Equal("AAAAAAAAAAA=", command.ExpectedVersion);
        Assert.IsType<RepairLifecycleView>(result.Value); Assert.True(result.Replayed);
        Assert.Equal(200, result.Status); Assert.Equal("source-version", result.Version);
    }
    private sealed class Capture : IRepairLifecycleRepository
    {
        public object? Command { get; private set; }
        private Task<RepairWorkflowResult> Save(object command)
        {
            Command = command;
            return Task.FromResult(new RepairWorkflowResult(200, Value: new RepairLifecycleFact(Guid.NewGuid(),
                Guid.NewGuid(), Guid.NewGuid(), null, null, Guid.NewGuid(), "Cancelled", "AwaitingApproval",
                "source-version", "successor-version"), Version: "source-version", Replayed: true));
        }
        public Task<RepairWorkflowResult> CancelItemAsync(RepairCancellationCommand command, CancellationToken token) => Save(command);
        public Task<RepairWorkflowResult> ContinueNormallyAsync(RepairNormalContinuationCommand command, CancellationToken token) => Save(command);
    }
}
