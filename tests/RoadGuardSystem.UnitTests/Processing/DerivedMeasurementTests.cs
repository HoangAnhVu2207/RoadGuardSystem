using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class DerivedMeasurementTests
{
    [Fact]
    public void Create_PublishedMeasurement_PreservesResearchProvenance()
    {
        var computedAt = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

        var measurement = DerivedMeasurement.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "sample-001",
            MeasurementType.DepressionDepth,
            12.5m,
            "mm",
            0.5m,
            DerivedMeasurementSourceType.SurfaceModel,
            "surface-model-v1",
            computedAt,
            DerivedMeasurementStatus.Published);

        Assert.Equal("sample-001", measurement.SampleId);
        Assert.Equal(MeasurementType.DepressionDepth, measurement.MeasurementType);
        Assert.Equal(DerivedMeasurementStatus.Published, measurement.Status);
    }
}
