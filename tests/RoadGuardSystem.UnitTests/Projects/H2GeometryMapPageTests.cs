using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Services.Projects;
using Xunit;

namespace RoadGuardSystem.UnitTests.Projects;

public sealed class H2GeometryMapPageTests
{
    private static GeometryMapSnapshot Snapshot() => new(new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        Guid.NewGuid(), null, 32648, "LEGACY_UNKNOWN", true, 0.01, "hash-v1", [new("slabs", "READY", "Polygon", 3, [])]),
        [new("c", "slabs", new("Polygon", Array.Empty<double>()), [20, 0, 30, 10], new { sequence = 3 }),
         new("a", "slabs", new("Polygon", Array.Empty<double>()), [0, 0, 10, 10], new { sequence = 1 }),
         new("b", "slabs", new("Polygon", Array.Empty<double>()), [10, 0, 20, 10], new { sequence = 2 })]);
    [Fact]
    public void StablePagingUsesPublicationHashAndPreservesAllFeatures()
    {
        var snapshot = Snapshot(); var first = GeometryMapPageEngine.Page(snapshot, "slabs", "hash-v1", 2);
        Assert.Equal(["a", "b"], first.Features.Select(x => x.Id)); Assert.NotNull(first.NextCursor);
        var second = GeometryMapPageEngine.Page(snapshot, "slabs", "hash-v1", 2, cursor: first.NextCursor);
        Assert.Equal("c", Assert.Single(second.Features).Id); Assert.Null(second.NextCursor);
    }
    [Fact]
    public void BboxIsNativeAndBoundToCursor()
    {
        var snapshot = Snapshot(); var first = GeometryMapPageEngine.Page(snapshot, "slabs", "hash-v1", 1, [0, 0, 19, 10]);
        Assert.Equal("a", Assert.Single(first.Features).Id);
        Assert.Throws<GeometryValidationException>(() => GeometryMapPageEngine.Page(snapshot, "slabs", "hash-v1", 1, [0, 0, 30, 10], first.NextCursor));
    }
    [Fact]
    public void MixedPublicationHashAndCursorAreRejected()
    {
        var snapshot = Snapshot(); var page = GeometryMapPageEngine.Page(snapshot, "slabs", "hash-v1", 1);
        Assert.Throws<GeometryValidationException>(() => GeometryMapPageEngine.Page(snapshot, "slabs", "stale-hash", 1));
        var other = snapshot with { Manifest = snapshot.Manifest with { PublicationId = Guid.NewGuid() } };
        Assert.Throws<GeometryValidationException>(() => GeometryMapPageEngine.Page(other, "slabs", "hash-v1", 1, cursor: page.NextCursor));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public void PageSizeIsBounded(int limit)
        => Assert.Throws<GeometryValidationException>(() => GeometryMapPageEngine.Page(Snapshot(), "slabs", "hash-v1", limit));
    [Fact]
    public void MalformedCursorBboxAndUnknownLayerFailClosed()
    {
        var snapshot = Snapshot();
        Assert.Throws<GeometryValidationException>(() => GeometryMapPageEngine.Page(snapshot, "slabs", "hash-v1", 1, cursor: "not-base64"));
        Assert.Throws<GeometryValidationException>(() => GeometryMapPageEngine.Page(snapshot, "slabs", "hash-v1", 1, [0, 1, double.NaN, 2]));
        Assert.Throws<GeometryValidationException>(() => GeometryMapPageEngine.Page(snapshot, "other", "hash-v1", 1));
    }
    [Fact]
    public void UnavailableLayerDoesNotPretendEmptyReadyData()
    {
        var snapshot = Snapshot(); snapshot = snapshot with { Manifest = snapshot.Manifest with { Layers = [new("wgs84", "UNAVAILABLE", "LineString", 0, ["CRS_GATE_PENDING"])] } };
        var error = Assert.Throws<GeometryValidationException>(() => GeometryMapPageEngine.Page(snapshot, "wgs84", "hash-v1", 1));
        Assert.Equal("geometry_layer_unavailable", error.Code);
    }
}
