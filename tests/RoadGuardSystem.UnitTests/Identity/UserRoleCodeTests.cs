using FluentAssertions;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Identity;

[Trait("TaskId", "P2-10")]
public sealed class UserRoleCodeTests
{
    [Fact(DisplayName = "P2-10 UserRoleCode has stable byte values")]
    public void UserRoleCode_HasStableByteValues()
    {
        var enumType = typeof(BaseEntity).Assembly.GetType(
            "RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode");

        enumType.Should().NotBeNull("P2-10 defines the authoritative domain role enum");
        enumType!.IsEnum.Should().BeTrue();
        Enum.GetUnderlyingType(enumType).Should().Be(typeof(byte));
        Enum.GetNames(enumType).Should().Equal(
            "Unknown",
            "Supervisor",
            "ProjectManager",
            "DroneOperator",
            "RepairCrew");
        Enum.GetValues(enumType).Cast<byte>().Should().Equal(0, 1, 2, 3, 4);
    }
}
