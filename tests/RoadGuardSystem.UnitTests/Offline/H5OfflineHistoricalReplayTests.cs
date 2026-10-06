using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Offline;
using System.Text.Json;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.Services.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineHistoricalReplayTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [Fact]
    public void RetainedHistoricalBindingUsesActualTypedEffectWithoutNewIdentityOrCaptureDevice()
    {
        var (operation, canonical, start) = ExistingStart();
        var binding = OfflineOperationBinding.BindHistoricalStart(Guid.NewGuid(), canonical, start,
            Guid.NewGuid(), operation.SnapshotId, OfflineWorkflowEngine.EnvelopeHash(operation),
            JsonSerializer.Serialize(operation, Json), start.ServerReceivedAt.AddHours(1));
        Assert.Equal(start.Id, binding.EffectId);
        Assert.Equal(canonical.OriginId, binding.OriginId);
        Assert.NotEqual(binding.OriginId, binding.EffectId);
        Assert.Equal(canonical.ContentHash, binding.CorePayloadHash);
        Assert.Null(start.DeviceId);
        Assert.Null(JsonSerializer.Deserialize<OfflineOperationInput>(binding.EnvelopeJson, Json)!.FieldStart!.DeviceId);
    }

    [Fact]
    public void HistoricalBindingRejectsUnrelatedTypedEffectEvenWithTheOldCanonicalClaim()
    {
        var (operation, canonical, start) = ExistingStart();
        var unrelated = FieldTaskStartOrigin.Create(Guid.NewGuid(), start.ProjectId, start.TaskId,
            start.AssignmentId, start.OriginId, start.ContentHash, start.OriginalActorId, null,
            start.ClaimedAt, null, null, start.ServerReceivedAt, start.RouteVersionId, null, null, true);
        Assert.Throws<ArgumentException>(() => OfflineOperationBinding.BindHistoricalStart(Guid.NewGuid(),
            canonical, unrelated, Guid.NewGuid(), operation.SnapshotId, OfflineWorkflowEngine.EnvelopeHash(operation),
            JsonSerializer.Serialize(operation, Json), start.ServerReceivedAt.AddHours(1)));
    }

    [Fact]
    public void ExistingDirectStartRetainsItsActualEffectAndUnknownCaptureDevice()
    {
        var (operation, canonical, start) = ExistingStart();
        var descriptor = OfflineWorkflowEngine.DescribeHistoricalStart(operation, canonical, start);
        Assert.Equal(start.Id, descriptor.EffectId);
        Assert.NotEqual(operation.OriginId, descriptor.EffectId);
        Assert.Null(operation.FieldStart!.DeviceId);
        Assert.Null(start.DeviceId);
        Assert.Equal(canonical.ContentHash, descriptor.CorePayloadHash);
        Assert.Throws<ArgumentException>(() => OfflineWorkflowEngine.Describe(operation));
    }

    [Fact]
    public void HistoricalReplayCannotRelabelOriginalBodyOrCaptureDevice()
    {
        var (operation, canonical, start) = ExistingStart();
        Assert.Throws<ArgumentException>(() => OfflineWorkflowEngine.DescribeHistoricalStart(
            operation with { FieldStart = operation.FieldStart! with { DeviceId = operation.SourceDeviceId } }, canonical, start));
        Assert.Throws<ArgumentException>(() => OfflineWorkflowEngine.DescribeHistoricalStart(
            operation with { CorePayloadHash = new string('f', 64) }, canonical, start));
    }

    [Fact]
    public void CanonicalClaimWithoutTheExactTypedEffectCannotAuthorizeHistoricalReplay()
    {
        var (operation, canonical, start) = ExistingStart();
        var unrelated = FieldTaskStartOrigin.Create(Guid.NewGuid(), start.ProjectId, start.TaskId,
            start.AssignmentId, start.OriginId, start.ContentHash, start.OriginalActorId, null,
            start.ClaimedAt, null, null, start.ServerReceivedAt, start.RouteVersionId, null, null, true);
        Assert.Throws<ArgumentException>(() => OfflineWorkflowEngine.DescribeHistoricalStart(operation, canonical, unrelated));
        Assert.Throws<ArgumentException>(() => OfflineWorkflowEngine.DescribeHistoricalStart(
            operation with { EffectId = operation.OriginId }, canonical, start));
    }

    private static (OfflineOperationInput Operation, FieldInspectionOperationOrigin Canonical, FieldTaskStartOrigin Start) ExistingStart()
    {
        var project = Guid.NewGuid(); var task = Guid.NewGuid(); var actor = Guid.NewGuid();
        var origin = Guid.NewGuid(); var effect = Guid.NewGuid(); var assignment = Guid.NewGuid();
        var at = new DateTimeOffset(2026, 10, 6, 1, 0, 0, TimeSpan.Zero);
        var operation = new OfflineOperationInput(1, origin, effect, "FIELD_START", actor, Guid.NewGuid(),
            Guid.NewGuid(), task, assignment, Convert.ToBase64String(new byte[8]), new string('0', 64), [], null,
            new FieldStartInput(origin, at));
        operation = operation with { CorePayloadHash = OfflineWorkflowEngine.FieldCoreHash(operation) };
        var canonical = FieldInspectionOperationOrigin.Create(effect, project, origin, "FIELD_START",
            operation.CorePayloadHash, actor, null, task, effect, at);
        var start = FieldTaskStartOrigin.Create(effect, project, task, assignment, origin,
            operation.CorePayloadHash, actor, null, at, null, null, at, Guid.NewGuid(), null, null, true);
        return (operation, canonical, start);
    }
}
