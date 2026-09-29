using RoadGuardSystem.BusinessObjects.Idempotency;
using Xunit;

namespace RoadGuardSystem.UnitTests.Idempotency;

public sealed class IdempotencyRecordTests
{
    [Fact]
    public void Create_UsesReceiptIdentitySeparateFromBusinessOperation()
    {
        var operationId = Guid.NewGuid();

        var record = IdempotencyRecord.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "processing.retry",
            "retry-001",
            new string('a', 64),
            operationId,
            "{\"status\":\"queued\"}",
            DateTimeOffset.UtcNow);

        Assert.NotEqual(operationId, record.Id);
        Assert.Equal(operationId, record.OperationId);
    }
}
