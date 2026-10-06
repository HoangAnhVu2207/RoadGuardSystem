using RoadGuardSystem.BusinessObjects.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineRepairResourcePinTests
{
    [Theory]
    [InlineData("REPAIR_ASSESSMENT")]
    [InlineData("REPAIR_EXECUTION_START")]
    [InlineData("REPAIR_EXECUTION_FINISH")]
    public void ActualRepairItemPinIsPersistedWithoutChangingOriginalHashes(string kind)
    {
        var item = Guid.NewGuid(); var row = Bind(kind, item);
        Assert.Equal(item, row.RepairResourceId); Assert.Equal(new string('a', 64), row.CorePayloadHash);
        Assert.Equal(new string('b', 64), row.EnvelopeHash);
    }

    [Theory]
    [InlineData("REPAIR_ASSESSMENT")]
    [InlineData("REPAIR_EXECUTION_START")]
    [InlineData("REPAIR_EXECUTION_FINISH")]
    public void RepairKindCannotBeBoundWithoutAnActualItemSelector(string kind)
        => Assert.Throws<ArgumentException>(() => Bind(kind, null));

    [Fact]
    public void FieldKindCannotAcquireAnUnrelatedRepairItemPin()
        => Assert.Throws<ArgumentException>(() => Bind("FIELD_START", Guid.NewGuid()));

    private static OfflineOperationBinding Bind(string kind, Guid? item)
    {
        var origin = Guid.NewGuid();
        return OfflineOperationBinding.Bind(Guid.NewGuid(), Guid.NewGuid(), origin, origin, kind, new string('a', 64),
            new string('b', 64), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "{}", DateTimeOffset.UtcNow, item);
    }
}
