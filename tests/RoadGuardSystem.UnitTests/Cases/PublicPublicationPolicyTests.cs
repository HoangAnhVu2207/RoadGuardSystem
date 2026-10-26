using RoadGuardSystem.BusinessObjects.Cases;
using Xunit;
namespace RoadGuardSystem.UnitTests.Cases;

public sealed class PublicPublicationPolicyTests
{
    [Theory]
    [InlineData("CRACK", true, "ROAD_SURFACE_DAMAGE")]
    [InlineData("POTHOLE", true, "ROAD_SURFACE_DAMAGE")]
    [InlineData("RG_POSTMAN_POTHOLE", true, null)]
    [InlineData("OTHER", true, null)]
    [InlineData("crack", true, null)]
    [InlineData("CRACK", false, null)]
    public void MappingIsExactAndFailClosed(string code, bool active, string? expected)
        => Assert.Equal(expected, PublicPublicationPolicy.Category(code, active));
}
