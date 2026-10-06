using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Projects;
using Xunit;

namespace RoadGuardSystem.UnitTests.Projects;

public sealed class H6ProjectLifecycleBoundaryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadAndRenewPreserveHistorySerializationAndReceiptFingerprintInputs(bool webJson)
    {
        var actor = Guid.NewGuid(); var project = Guid.NewGuid(); var closure = Guid.NewGuid();
        var historyId = Guid.NewGuid(); var obligation = Guid.NewGuid(); var grant = Guid.NewGuid();
        var receiver = Guid.NewGuid(); var defect = Guid.NewGuid(); var linkedDefect = Guid.NewGuid();
        var at = new DateTimeOffset(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
        var version = new string('a', 64);
        var expected = new ProjectLifecycleViewDto(project, "CONFIRMED", "BASIS_INVALIDATED", true, true,
            "VERIFIED", "BLOCKED", [obligation], ["retained-source-reason"],
            [new(historyId, "RenewedHandlingScope", actor, at, obligation, grant, receiver, closure, "reason",
                "basis", "retained-authority", "TARGET_CONFIRMED", defect, linkedDefect, "handling scope")], version);
        var facts = new ProjectLifecycleFacts(project, "CONFIRMED", "BASIS_INVALIDATED", true, true,
            "VERIFIED", "BLOCKED", [obligation], ["retained-source-reason"],
            [new(historyId, "RenewedHandlingScope", actor, at, obligation, grant, receiver, closure, "reason",
                "basis", "retained-authority", "TARGET_CONFIRMED", defect, linkedDefect, "handling scope")], version);
        var input = new RenewedHandlingScopeInput(closure, "new reason", "new handling scope", "new basis");
        var options = webJson ? new JsonSerializerOptions(JsonSerializerDefaults.Web) : new JsonSerializerOptions();
        using var cancellation = new CancellationTokenSource();
        var repository = new FixtureRepository(facts, (command, token) =>
        {
            Assert.Equal(actor, command.ActorId); Assert.Equal(project, command.ProjectId);
            Assert.Equal(UserRoleCode.Supervisor, command.Role); Assert.Equal("receipt-key", command.Key);
            Assert.Equal(version, command.ExpectedProjectionVersion); Assert.Equal(cancellation.Token, token);
            Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(input, options),
                JsonSerializer.SerializeToUtf8Bytes(command.Input, options));
            return new(201, Value: facts);
        });
        var service = new ProjectLifecycleService(repository);
        var read = await service.ReadAsync(actor, UserRoleCode.Supervisor, project, cancellation.Token);
        var renewed = await service.RenewAsync(actor, UserRoleCode.Supervisor, project, input,
            "receipt-key", '"' + version + '"', cancellation.Token);
        Assert.Equal(200, read.Status); Assert.Equal(201, renewed.Status);
        var expectedBytes = JsonSerializer.SerializeToUtf8Bytes(expected, options);
        Assert.Equal(expectedBytes, JsonSerializer.SerializeToUtf8Bytes(read.Value, options));
        Assert.Equal(expectedBytes, JsonSerializer.SerializeToUtf8Bytes(renewed.Value, options));
        Assert.Equal(expectedBytes, JsonSerializer.SerializeToUtf8Bytes(facts, options));
    }

    [Fact]
    public async Task RepositoryRejectionRemainsARejectionWithoutManufacturedView()
    {
        var repository = new FixtureRepository(null, (_, _) => new(409, "operational_closure_source_unavailable"));
        var service = new ProjectLifecycleService(repository);
        var actor = Guid.NewGuid(); var project = Guid.NewGuid();
        Assert.Equal(404, (await service.ReadAsync(actor, UserRoleCode.Supervisor, project, default)).Status);
        var result = await service.RenewAsync(actor, UserRoleCode.Supervisor, project,
            new(Guid.NewGuid(), "reason", "scope", "basis"), "key", '"' + new string('b', 64) + '"', default);
        Assert.Equal(409, result.Status); Assert.Equal("operational_closure_source_unavailable", result.Code);
        Assert.Null(result.Value);
    }

    private sealed class FixtureRepository(ProjectLifecycleFacts? facts,
        Func<ProjectRenewedHandlingCommand, CancellationToken, ProjectLifecycleWriteResult> renew)
        : IProjectLifecycleRepository
    {
        public Task<ProjectLifecycleFacts?> ReadAsync(Guid actor, Guid project, CancellationToken cancellationToken)
            => Task.FromResult(facts);
        public Task<ProjectLifecycleWriteResult> RenewAsync(ProjectRenewedHandlingCommand command,
            CancellationToken cancellationToken) => Task.FromResult(renew(command, cancellationToken));
    }
}
