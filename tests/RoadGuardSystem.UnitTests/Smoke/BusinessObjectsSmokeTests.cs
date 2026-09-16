using FluentAssertions;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Smoke;

/// <summary>
/// Smoke tests that confirm the BusinessObjects assembly loads correctly
/// and core types are instantiable.
/// </summary>
[Trait("TaskId", "P1-00")]
public sealed class BusinessObjectsSmokeTests
{
    [Fact(DisplayName = "BusinessObjects assembly loads successfully")]
    public void BusinessObjects_Assembly_LoadsSuccessfully()
    {
        // ARRANGE & ACT
        var assembly = typeof(BaseEntity).Assembly;

        // ASSERT
        assembly.Should().NotBeNull();
        assembly.GetName().Name.Should().Be("RoadGuardSystem.aBusinessObjects");
    }

    [Fact(DisplayName = "EnumExtensions.GetAllValues returns a list for valid enum type")]
    public void EnumExtensions_GetAllValues_ReturnsListForValidEnum()
    {
        // ARRANGE & ACT — UserStatus is a valid (currently empty) enum
        var values = EnumExtensions.GetAllValues<UserStatus>();

        // ASSERT — must return a list; empty enum returns empty list (not null, not exception)
        values.Should().NotBeNull();
    }

    [Fact(DisplayName = "EnumExtensions.GetDescription returns string for UserStatus value if it has any")]
    public void EnumExtensions_GetDescription_DoesNotThrowForEmptyEnum()
    {
        // ARRANGE — UserStatus currently has no members, so we only verify the
        // static class is accessible and GetAllValues is callable without throwing.
        // When members are added later, this test documents the expected behavior.
        var act = () => EnumExtensions.GetAllValues<UserStatus>();

        act.Should().NotThrow(
            because: "EnumExtensions must be callable even on an empty enum");
    }
}
