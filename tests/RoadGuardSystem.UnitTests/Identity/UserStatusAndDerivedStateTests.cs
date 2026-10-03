using FluentAssertions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using Xunit;

namespace RoadGuardSystem.UnitTests.Identity;

[Trait("TaskId", "P2-10")]
public sealed class UserStatusAndDerivedStateTests
{
    [Fact]
    public void WebSession_UsesIdleTimeoutWithoutExtendingAbsoluteExpiry()
    {
        var issued = new DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.Zero);
        var session = new UserSession
        {
            Id = Guid.NewGuid(), UserId = Guid.NewGuid(), IssuedAt = issued,
            ExpiresAt = issued.AddHours(12), Transport = SessionTransport.Web,
            LastActivityAt = issued
        };

        session.IsActiveAt(issued.AddMinutes(29)).Should().BeTrue();
        session.IsActiveAt(issued.AddMinutes(30)).Should().BeFalse();
        session.Touch(issued.AddMinutes(10));
        session.LastActivityAt.Should().Be(issued.AddMinutes(10));
        session.ExpiresAt.Should().Be(issued.AddHours(12));
    }

    [Fact]
    public void Touch_DoesNotReviveRevokedOrExpiredSession()
    {
        var issued = DateTimeOffset.UtcNow.AddHours(-1);
        var session = new UserSession
        {
            Id = Guid.NewGuid(), UserId = Guid.NewGuid(), IssuedAt = issued,
            ExpiresAt = issued.AddMinutes(30), Transport = SessionTransport.Web,
            RevokedAt = issued.AddMinutes(5)
        };

        var touch = () => session.Touch(DateTimeOffset.UtcNow);
        touch.Should().Throw<InvalidOperationException>();
        session.LastActivityAt.Should().BeNull();
    }
    [Fact(DisplayName = "P2-10 UserStatus has stable byte values")]
    public void UserStatus_HasStableByteValues()
    {
        var enumType = typeof(UserStatus);
        Enum.GetUnderlyingType(enumType).Should().Be(typeof(byte));
        Enum.GetNames(enumType).Should().Equal("Unknown", "Active", "Suspended", "Pending");
        Enum.GetValues(enumType).Cast<byte>().Should().Equal(0, 1, 2, 3);
    }

    [Fact(DisplayName = "P2-10 PasswordResetResult has stable byte values")]
    public void PasswordResetResult_HasStableByteValues()
    {
        var enumType = typeof(PasswordResetResult);
        Enum.GetUnderlyingType(enumType).Should().Be(typeof(byte));
        Enum.GetNames(enumType).Should().Equal("Unknown", "Success", "Failed", "Rejected");
        Enum.GetValues(enumType).Cast<byte>().Should().Equal(0, 1, 2, 3);
    }

    [Fact(DisplayName = "P2-10 UserSession derived state: Active")]
    public void UserSession_ActiveState_CalculatedCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            IssuedAt = now.AddMinutes(-5),
            ExpiresAt = now.AddMinutes(30),
            RevokedAt = null
        };

        session.IsActiveAt(DateTimeOffset.UtcNow).Should().BeTrue();
        session.IsRevoked.Should().BeFalse();
        session.IsExpiredAt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact(DisplayName = "P2-10 UserSession derived state: Revoked")]
    public void UserSession_RevokedState_CalculatedCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            IssuedAt = now.AddMinutes(-10),
            ExpiresAt = now.AddMinutes(20),
            RevokedAt = now.AddMinutes(-2)
        };

        session.IsActiveAt(DateTimeOffset.UtcNow).Should().BeFalse();
        session.IsRevoked.Should().BeTrue();
        session.IsExpiredAt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact(DisplayName = "P2-10 UserSession derived state: Expired")]
    public void UserSession_ExpiredState_CalculatedCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            IssuedAt = now.AddHours(-2),
            ExpiresAt = now.AddHours(-1),
            RevokedAt = null
        };

        session.IsActiveAt(DateTimeOffset.UtcNow).Should().BeFalse();
        session.IsRevoked.Should().BeFalse();
        session.IsExpiredAt(DateTimeOffset.UtcNow).Should().BeTrue();
    }
}
