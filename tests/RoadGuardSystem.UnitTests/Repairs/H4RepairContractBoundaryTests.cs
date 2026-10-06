using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Implementations.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4RepairContractBoundaryTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task AssessmentMappingPreservesNullEmptyAndDeclaredCaptureCoreHash(int shape)
    {
        var actor = Guid.NewGuid(); var project = Guid.NewGuid(); var package = Guid.NewGuid();
        var item = Guid.NewGuid(); var task = Guid.NewGuid();
        FieldMeasurementInput[]? measurements = shape switch
        {
            0 => null,
            1 => [],
            3 => [null!],
            _ => [new("m1", "WIDTH", 12.340m, "MEASURED", null, "LENGTH", "mm", 106.7, 10.8,
                "observed", "gauge", "manual", "retained note")]
        };
        FieldEvidenceDeclaration[]? evidence = shape switch
        {
            0 => null,
            1 => [],
            3 => [null!],
            _ => [new(Guid.NewGuid(), null, "BEFORE", new string('b', 64), "image/jpeg",
                new DateTimeOffset(2026, 10, 6, 7, 30, 0, TimeSpan.FromHours(7)), "checklist")]
        };
        var input = new RepairMeasurementAssessmentInput(Guid.NewGuid(), Guid.NewGuid(), measurements, evidence,
            shape == 2 ? new("GPS_CAPTURE", "checklist", 106.7, 10.8, "slab", Guid.NewGuid(), Guid.NewGuid(), 12.34) : null);
        var repository = new Recording();
        var service = new RepairExecutionService(repository);
        await service.AssessAsync(actor, UserRoleCode.RepairCrew, project, package, item, task, input,
            "assessment", "\"AQIDBAUGBwg=\"", default);
        var command = Assert.IsType<RepairAssessmentCommand>(repository.Command);
        Assert.Equal("AQIDBAUGBwg=", command.ExpectedItemVersion);
        Assert.Equal(JsonSerializer.Serialize(input, WebJson),
            JsonSerializer.Serialize(command.Input, WebJson));
        Assert.Equal(RepairCoreHash.Assessment(task, item, actor, input),
            RepairCommandCoreHash.Assessment(task, item, actor, command.Input));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecutionMappingRetainsOriginalClaimAndCanonicalHash(bool finish)
    {
        var actor = Guid.NewGuid(); var project = Guid.NewGuid(); var item = Guid.NewGuid(); var task = Guid.NewGuid();
        var at = new DateTimeOffset(2026, 10, 6, 7, 30, 0, TimeSpan.FromHours(7));
        var repository = new Recording(); var service = new RepairExecutionService(repository);
        if (finish)
        {
            var input = new RepairExecutionFinishInput(Guid.NewGuid(), Guid.NewGuid(), at, Guid.NewGuid(), 1234, "boot-id");
            await service.FinishExecutionAsync(actor, UserRoleCode.RepairCrew, project, Guid.NewGuid(), item, task,
                input, "finish", "\"AQIDBAUGBwg=\"", default);
            var command = Assert.IsType<RepairExecutionFinishCommand>(repository.Command);
            Assert.Equal(RepairCoreHash.ExecutionFinish(task, item, actor, input),
                RepairCommandCoreHash.ExecutionFinish(task, item, actor, command.Input));
        }
        else
        {
            var input = new RepairExecutionStartInput(Guid.NewGuid(), Guid.NewGuid(), at, Guid.NewGuid(), null, null, null);
            await service.StartExecutionAsync(actor, UserRoleCode.RepairCrew, project, Guid.NewGuid(), item, task,
                input, "start", "\"AQIDBAUGBwg=\"", default);
            var command = Assert.IsType<RepairExecutionStartCommand>(repository.Command);
            Assert.Equal(RepairCoreHash.ExecutionStart(task, item, actor, input),
                RepairCommandCoreHash.ExecutionStart(task, item, actor, command.Input));
        }
    }

    [Fact]
    public void CorrectionOutboxRetainsTheExistingCatalogWireShape()
    {
        var id = Guid.NewGuid(); var project = Guid.NewGuid(); var item = Guid.NewGuid();
        var obligation = Guid.NewGuid(); var previous = Guid.NewGuid();
        var at = new DateTimeOffset(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
        var actual = new RepairCorrectionOutboxEvent(1, id, id, project, "CORRECTED", "RepairWork", item,
            id, at, obligation, previous, "UNREPAIRED");
        var expected = new H6RepairCorrectionAuditDto(1, id, id, project, "CORRECTED", "RepairWork", item,
            id, at, obligation, previous, "UNREPAIRED");
        Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(expected, WebJson), JsonSerializer.SerializeToUtf8Bytes(actual, WebJson));
    }

    [Fact]
    public async Task RepositoryOutcomeMapsToExistingWireTypeWithoutChangingReceiptMetadata()
    {
        var item = Guid.NewGuid(); var task = Guid.NewGuid(); var id = Guid.NewGuid(); var first = Guid.NewGuid();
        var session = Guid.NewGuid();
        var fact = new RepairAssessmentFact(id, item, task, first, session, "PRE_EXECUTION", "INCOMPLETE",
            ["LOCATION_UNKNOWN"], "UNKNOWN", new string('a', 64));
        var repository = new Recording { Result = new(200, Value: fact, Version: "AQIDBAUGBwg=", Replayed: true) };
        var result = await new RepairExecutionService(repository).AssessAsync(Guid.NewGuid(), UserRoleCode.RepairCrew,
            Guid.NewGuid(), Guid.NewGuid(), item, task, new(Guid.NewGuid(), first, null, null, null),
            "assessment", "\"AQIDBAUGBwg=\"", default);
        Assert.Equal(200, result.Status); Assert.True(result.Replayed); Assert.Equal("AQIDBAUGBwg=", result.Version);
        var view = Assert.IsType<RepairAssessmentView>(result.Value);
        Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(fact, WebJson), JsonSerializer.SerializeToUtf8Bytes(view, WebJson));
    }

    private sealed class Recording : IRepairExecutionRepository
    {
        public object? Command { get; private set; }
        public RepairWorkflowResult Result { get; init; } = new(201);
        private Task<RepairWorkflowResult> Capture(object command) { Command = command; return Task.FromResult(Result); }
        public Task<RepairWorkflowResult> AssessAsync(RepairAssessmentCommand command, CancellationToken token) => Capture(command);
        public Task<RepairWorkflowResult> StartExecutionAsync(RepairExecutionStartCommand command, CancellationToken token) => Capture(command);
        public Task<RepairWorkflowResult> FinishExecutionAsync(RepairExecutionFinishCommand command, CancellationToken token) => Capture(command);
        public Task<RepairWorkflowResult> SubmitAttemptAsync(RepairAttemptSubmitCommand command, CancellationToken token) => Capture(command);
        public Task<RepairWorkflowResult> SupplementAttemptAsync(RepairAttemptSupplementCommand command, CancellationToken token) => Capture(command);
        public Task<RepairWorkflowResult> ReviewAttemptAsync(RepairAttemptReviewCommand command, CancellationToken token) => Capture(command);
        public Task<RepairWorkflowResult> ConfirmFinalAsync(RepairFinalConfirmCommand command, CancellationToken token) => Capture(command);
    }
}
