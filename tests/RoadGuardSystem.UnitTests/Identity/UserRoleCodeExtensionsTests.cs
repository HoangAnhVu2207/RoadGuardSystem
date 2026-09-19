using FluentAssertions;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Identity;

[Trait("TaskId", "P2-10")]
public sealed class UserRoleCodeExtensionsTests
{
    [Theory(DisplayName = "P2-10 Positive: Valid UserRoleCode maps to canonical string")]
    [InlineData(UserRoleCode.Supervisor, "SUPERVISOR")]
    [InlineData(UserRoleCode.ProjectManager, "PM")]
    [InlineData(UserRoleCode.DroneOperator, "DRONE_OPERATOR")]
    [InlineData(UserRoleCode.RepairCrew, "REPAIR_CREW")]
    public void ToDbCode_ValidCode_ReturnsCanonicalString(UserRoleCode role, string expectedDbCode)
    {
        role.ToDbCode().Should().Be(expectedDbCode);
    }

    [Fact(DisplayName = "P2-10 Negative: Unknown UserRoleCode throws on ToDbCode")]
    public void ToDbCode_Unknown_ThrowsArgumentOutOfRangeException()
    {
        var act = () => UserRoleCode.Unknown.ToDbCode();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "P2-10 Negative: Undefined byte value throws on ToDbCode")]
    public void ToDbCode_Undefined_ThrowsArgumentOutOfRangeException()
    {
        var act = () => ((UserRoleCode)99).ToDbCode();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory(DisplayName = "P2-10 Positive: Canonical string maps to UserRoleCode")]
    [InlineData("SUPERVISOR", UserRoleCode.Supervisor)]
    [InlineData("PM", UserRoleCode.ProjectManager)]
    [InlineData("DRONE_OPERATOR", UserRoleCode.DroneOperator)]
    [InlineData("REPAIR_CREW", UserRoleCode.RepairCrew)]
    public void FromDbCode_ValidString_ReturnsEnum(string dbCode, UserRoleCode expectedRole)
    {
        UserRoleCodeExtensions.FromDbCode(dbCode).Should().Be(expectedRole);
    }

    [Theory(DisplayName = "P2-10 Negative: Invalid string throws on FromDbCode")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("supervisor")]
    [InlineData("UNKNOWN")]
    [InlineData("ADMIN")]
    public void FromDbCode_InvalidString_ThrowsArgumentOutOfRangeException(string invalidDbCode)
    {
        var act = () => UserRoleCodeExtensions.FromDbCode(invalidDbCode);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
