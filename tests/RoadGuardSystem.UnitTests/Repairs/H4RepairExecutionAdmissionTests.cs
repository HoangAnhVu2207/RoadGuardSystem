using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Implementations.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4RepairExecutionAdmissionTests
{
    [Theory]
    [InlineData("ASSESS")]
    [InlineData("START")]
    [InlineData("FINISH")]
    public async Task MissingExecutionKeyRequiresPreconditionWithoutRepositoryMutation(string action)
    {
        var repository = new Recording(); var result = await Execute(new(repository), action, null, Token);
        Assert.Equal(428, result.Status); Assert.False(repository.Called);
    }
    [Theory]
    [InlineData("ASSESS")]
    [InlineData("START")]
    [InlineData("FINISH")]
    public async Task HistoryHashCannotSubstituteForExecutionItemRowVersion(string action)
    {
        var repository = new Recording(); var result = await Execute(new(repository), action, "k", "\"h4-history-v1-not-a-rowversion\"");
        Assert.Equal(400, result.Status); Assert.False(repository.Called);
    }
    [Theory]
    [InlineData("ASSESS")]
    [InlineData("START")]
    [InlineData("FINISH")]
    public async Task ProjectManagerCannotCallDirectCrewExecutionPath(string action)
    {
        var repository = new Recording(); var result = await Execute(new(repository), action, "k", Token, UserRoleCode.ProjectManager);
        Assert.Equal(403, result.Status); Assert.False(repository.Called);
    }
    [Fact]
    public async Task DirectAssessmentPreservesTypedOriginalCaptureAndExpectedItemVersion()
    {
        var repository = new Recording(); var result = await Execute(new(repository), "ASSESS", "k", Token);
        Assert.Same(repository.Result, result); var command = Assert.IsType<RepairAssessmentCommand>(repository.Command);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(Assessment), System.Text.Json.JsonSerializer.Serialize(command.Input)); Assert.Equal(Token[1..^1], command.ExpectedItemVersion);
        Assert.Equal(Actor, command.ActorId); Assert.Equal(TaskId, command.TaskId);
    }
    [Theory]
    [InlineData("SUBMIT", UserRoleCode.RepairCrew)]
    [InlineData("SUPPLEMENT", UserRoleCode.RepairCrew)]
    [InlineData("REVIEW", UserRoleCode.ProjectManager)]
    [InlineData("FINAL", UserRoleCode.Supervisor)]
    public async Task NormalContinuationRequiresKeyVersionAndItsCommandRole(string action, UserRoleCode role)
    {
        var repository = new Recording(); var service = new RepairExecutionService(repository);
        Assert.Equal(428, (await ExecuteContinuation(service, action, role, null, Token)).Status);
        Assert.Equal(400, (await ExecuteContinuation(service, action, role, "key", "\"history-not-rowversion\"")).Status);
        Assert.Equal(403, (await ExecuteContinuation(service, action, UserRoleCode.Reporter, "key", Token)).Status);
        Assert.False(repository.Called);
        var result = await ExecuteContinuation(service, action, role, "key", Token);
        Assert.Same(repository.Result, result);
        Assert.Equal(Token[1..^1], action switch
        {
            "SUBMIT" => Assert.IsType<RepairAttemptSubmitCommand>(repository.Command).ExpectedItemVersion,
            "SUPPLEMENT" => Assert.IsType<RepairAttemptSupplementCommand>(repository.Command).ExpectedItemVersion,
            "REVIEW" => Assert.IsType<RepairAttemptReviewCommand>(repository.Command).ExpectedItemVersion,
            _ => Assert.IsType<RepairFinalConfirmCommand>(repository.Command).ExpectedItemVersion
        });
    }
    private static readonly Guid Actor = Guid.NewGuid(), Project = Guid.NewGuid(), Package = Guid.NewGuid(), Item = Guid.NewGuid(), TaskId = Guid.NewGuid();
    private static readonly string Token = "\"" + Convert.ToBase64String(new byte[8]) + "\"";
    private static readonly RepairMeasurementAssessmentInput Assessment = new(Guid.NewGuid(), Guid.NewGuid(), null, null, null);
    private static Task<RepairWorkflowResult> Execute(RepairExecutionService service, string action, string? key, string? version,
        UserRoleCode role = UserRoleCode.RepairCrew) => action switch
        {
            "ASSESS" => service.AssessAsync(Actor, role, Project, Package, Item, TaskId, Assessment, key, version, default),
            "START" => service.StartExecutionAsync(Actor, role, Project, Package, Item, TaskId,
                new(Guid.NewGuid(), Assessment.FieldFirstStartId, DateTimeOffset.UtcNow, Guid.NewGuid()), key, version, default),
            "FINISH" => service.FinishExecutionAsync(Actor, role, Project, Package, Item, TaskId,
                new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow), key, version, default),
            _ => throw new ArgumentException("Unknown execution action.")
        };
    private static Task<RepairWorkflowResult> ExecuteContinuation(RepairExecutionService service, string action,
        UserRoleCode role, string? key, string? version) => action switch
        {
            "SUBMIT" => service.SubmitAttemptAsync(Actor, role, Project, Package, Item, TaskId,
                new(Guid.NewGuid(), new string('a', 64), Guid.NewGuid()), key, version, default),
            "SUPPLEMENT" => service.SupplementAttemptAsync(Actor, role, Project, Package, Item, TaskId,
                new(Guid.NewGuid(), new string('a', 64)), key, version, default),
            "REVIEW" => service.ReviewAttemptAsync(Actor, role, Project, Package, Item, TaskId,
                new(Guid.NewGuid(), "SUPPLEMENT", "evidence required"), key, version, default),
            "FINAL" => service.ConfirmFinalAsync(Actor, role, Project, Package, Item,
                new("verified"), key, version, default),
            _ => throw new ArgumentException("Unknown continuation action.")
        };
    private sealed class Recording : IRepairExecutionRepository
    {
        public bool Called { get; private set; }
        public object? Command { get; private set; }
        public RepairWorkflowResult Result { get; } = new(201);
        private Task<RepairWorkflowResult> Capture(object command) { Called = true; Command = command; return Task.FromResult(Result); }
        public Task<RepairWorkflowResult> AssessAsync(RepairAssessmentCommand command, CancellationToken cancellationToken) => Capture(command);
        public Task<RepairWorkflowResult> StartExecutionAsync(RepairExecutionStartCommand command, CancellationToken cancellationToken) => Capture(command);
        public Task<RepairWorkflowResult> FinishExecutionAsync(RepairExecutionFinishCommand command, CancellationToken cancellationToken) => Capture(command);
        public Task<RepairWorkflowResult> SubmitAttemptAsync(RepairAttemptSubmitCommand command, CancellationToken cancellationToken) => Capture(command);
        public Task<RepairWorkflowResult> SupplementAttemptAsync(RepairAttemptSupplementCommand command, CancellationToken cancellationToken) => Capture(command);
        public Task<RepairWorkflowResult> ReviewAttemptAsync(RepairAttemptReviewCommand command, CancellationToken cancellationToken) => Capture(command);
        public Task<RepairWorkflowResult> ConfirmFinalAsync(RepairFinalConfirmCommand command, CancellationToken cancellationToken) => Capture(command);
    }
}
