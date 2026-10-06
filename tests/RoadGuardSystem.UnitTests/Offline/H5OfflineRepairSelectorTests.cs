using System.Text.Json;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Services.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineRepairSelectorTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("REPAIR_ASSESSMENT", "assessment")]
    [InlineData("REPAIR_EXECUTION_START", "execution-start")]
    [InlineData("REPAIR_EXECUTION_FINISH", "execution-finish")]
    public void MatchedRepairBodyCannotAcquireAdministrativeActionOrAnotherBody(string kind, string action)
    {
        var origin = Guid.NewGuid(); var device = Guid.NewGuid(); var item = Guid.NewGuid();
        var start = new RepairExecutionStartInput(origin, Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), device);
        var finish = new RepairExecutionFinishInput(origin, Guid.NewGuid(), DateTimeOffset.UtcNow, device);
        var assessment = new RepairMeasurementAssessmentInput(origin, Guid.NewGuid(), null, null, null, device);
        var payload = new OfflineRepairPayload(item, action, Start: kind == "REPAIR_EXECUTION_START" ? start : null,
            Finish: kind == "REPAIR_EXECUTION_FINISH" ? finish : null,
            Assessment: kind == "REPAIR_ASSESSMENT" ? assessment : null);
        var operation = new OfflineOperationInput(1, origin, origin, kind, Guid.NewGuid(), device, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Convert.ToBase64String(new byte[8]), new string('a', 64), [], null, Repair: payload);
        Assert.Equal(item, OfflineWorkflowEngine.Describe(operation).RepairResourceId);
        Assert.Throws<ArgumentException>(() => OfflineWorkflowEngine.Describe(operation with
            { Repair = payload with { Action = "cancel" } }));
        Assert.Throws<ArgumentException>(() => OfflineWorkflowEngine.Describe(operation with
            { Repair = payload with { Start = start, Finish = finish, Assessment = assessment } }));
        Assert.Throws<ArgumentException>(() => OfflineWorkflowEngine.Describe(operation with
            { Repair = new OfflineRepairPayload(item, action) }));
        Assert.Throws<ArgumentException>(() => OfflineWorkflowEngine.Describe(operation with { OriginId = Guid.NewGuid() }));
        Assert.Throws<ArgumentException>(() => OfflineWorkflowEngine.Describe(operation with { SourceDeviceId = Guid.NewGuid() }));
    }

    [Fact]
    public void NewOfflineWrapperRejectsAdministrativeBodyAtJsonBoundary()
    {
        var json = "{\"resourceId\":\"" + Guid.NewGuid().ToString("D") +
            "\",\"action\":\"cancel\",\"decision\":{\"reason\":\"not an offline execution operation\"}}";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<OfflineRepairPayload>(json, Json));
    }
}
