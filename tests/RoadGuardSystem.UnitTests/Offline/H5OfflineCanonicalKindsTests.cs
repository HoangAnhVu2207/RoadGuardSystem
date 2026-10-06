using RoadGuardSystem.BusinessObjects.Inspections;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineCanonicalKindsTests
{
    [Theory]
    [InlineData("FIELD_ACCEPT")]
    [InlineData("REPAIR_ASSESSMENT")]
    [InlineData("REPAIR_EXECUTION_START")]
    [InlineData("REPAIR_EXECUTION_FINISH")]
    public void FiniteOfflineKindsUseTheExistingCanonicalOriginWithoutRewritingHashes(string kind)
    {
        var origin = Guid.NewGuid(); var effect = Guid.NewGuid(); var hash = new string('a', 64);
        var row = FieldInspectionOperationOrigin.Create(Guid.NewGuid(), Guid.NewGuid(), origin, kind, hash,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), effect, DateTimeOffset.UtcNow);
        Assert.Equal(kind, row.Kind); Assert.Equal(hash, row.ContentHash);
        Assert.Equal(origin, row.OriginId); Assert.Equal(effect, row.EffectId);
    }

    [Fact]
    public void ArbitraryAdministrativeOperationCannotEnterTheFiniteCanonicalRegistry()
        => Assert.Throws<ArgumentException>(() => FieldInspectionOperationOrigin.Create(Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), "REPAIR_POLICY_PUBLISH", new string('a', 64), Guid.NewGuid(), null,
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow));
}
