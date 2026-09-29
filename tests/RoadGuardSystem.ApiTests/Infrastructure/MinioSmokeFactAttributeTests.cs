using FluentAssertions;
using Xunit;

namespace RoadGuardSystem.ApiTests.Infrastructure;

public sealed class MinioSmokeFactAttributeTests
{
    [Fact]
    public void AttributeSkipsOnlyWhenMinioSmokeConfigurationIsMissing()
    {
        var attribute = new MinioSmokeFactAttribute();

        if (MinioSmokeFactAttribute.IsConfigured())
        {
            attribute.Skip.Should().BeNull();
        }
        else
        {
            attribute.Skip.Should().NotBeNullOrWhiteSpace();
        }
    }
}
