using RoadGuardSystem.BusinessObjects.Identity;
using Xunit;

namespace RoadGuardSystem.UnitTests.Authentication;

[Trait("Package", "HUY-01")]
public sealed class Huy01SessionBoundaryTests
{
    [Fact]
    public void WebIdleAndAbsoluteExpiry_UseInclusiveBoundaryAndTouchCannotRevive()
    {
        var issued = new DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.Zero);
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            IssuedAt = issued,
            ExpiresAt = issued.AddHours(12),
            Transport = SessionTransport.Web,
            LastActivityAt = issued
        };

        Assert.True(session.IsActiveAt(issued.AddMinutes(29).AddSeconds(59)));
        Assert.False(session.IsActiveAt(issued.AddMinutes(30)));
        Assert.Throws<InvalidOperationException>(() => session.Touch(issued.AddMinutes(30)));
        session.Touch(issued.AddMinutes(29).AddSeconds(59));
        Assert.False(session.IsActiveAt(issued.AddHours(11).AddMinutes(59).AddSeconds(59)));
        session.LastActivityAt = issued.AddHours(11).AddMinutes(59);
        Assert.True(session.IsActiveAt(issued.AddHours(11).AddMinutes(59).AddSeconds(59)));
        Assert.False(session.IsActiveAt(issued.AddHours(12)));
        Assert.Throws<InvalidOperationException>(() => session.Touch(issued.AddHours(12)));
        session.RevokedAt = issued.AddHours(1);
        Assert.Throws<InvalidOperationException>(() => session.Touch(issued.AddHours(1)));
    }
}
