using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using Xunit;

namespace RoadGuardSystem.UnitTests.Clocks;

public sealed class BusinessReceivingRequestTests
{
    [Fact]
    public void Review_appointment_preserves_origin_due_and_breach_history()
    {
        var at = new DateTimeOffset(2026, 10, 7, 2, 0, 0, TimeSpan.Zero);
        var clock = DeadlineClock.Create(Guid.NewGuid(), Guid.NewGuid(), DeadlineClockKind.ProjectManagerReview,
            Guid.NewGuid(), Guid.NewGuid(), at);
        clock.ObserveBreach(Guid.NewGuid(), at.AddHours(25));
        clock.Appoint(Guid.NewGuid(), Guid.NewGuid(), UserRoleCode.ProjectManager, Guid.NewGuid(),
            "Current substitute", at.AddHours(25));
        Assert.Equal(at.AddHours(24), clock.OriginalDueAt);
        Assert.Equal(clock.OriginalDueAt, clock.CurrentDueAt);
        Assert.Single(clock.Breaches); Assert.Single(clock.Appointments);
        Assert.NotNull(clock.AppointedActorId);
        Assert.Throws<ArgumentException>(() => clock.Appoint(Guid.NewGuid(), Guid.NewGuid(), UserRoleCode.RepairCrew,
            Guid.NewGuid(), "Wrong role", at.AddHours(26)));
    }
    [Theory]
    [InlineData(DeadlineClockKind.CrewSupplement, UserRoleCode.RepairCrew, 48)]
    [InlineData(DeadlineClockKind.SupervisorEscalation, UserRoleCode.Supervisor, 24)]
    public void First_server_ack_survives_reclick_and_replacement(DeadlineClockKind kind, UserRoleCode role, int hours)
    {
        var at = new DateTimeOffset(2026, 10, 7, 2, 0, 0, TimeSpan.Zero);
        var first = Guid.NewGuid(); var next = Guid.NewGuid();
        var request = BusinessReceivingRequest.Create(Guid.NewGuid(), Guid.NewGuid(), kind,
            "FieldReview", Guid.NewGuid(), "source-v1", Guid.NewGuid(), first, role, at);
        Assert.Null(request.AcknowledgedAt);
        request.Appoint(next, Guid.NewGuid(), "current replacement", at.AddMinutes(1));
        Assert.Throws<InvalidOperationException>(() => request.Acknowledge(first, Guid.NewGuid(), at.AddMinutes(2), at.AddDays(-1)));
        var clock = request.Acknowledge(next, Guid.NewGuid(), at.AddMinutes(3), at.AddDays(-1));
        Assert.Equal(at.AddMinutes(3), clock.OriginAt);
        Assert.Equal(clock.OriginAt.AddHours(hours), clock.OriginalDueAt);
        Assert.Equal(at.AddDays(-1), request.ClaimedDeviceAt);
        Assert.Equal(clock.Id, request.Acknowledge(next, Guid.NewGuid(), at.AddMinutes(4), null).Id);
        request.Appoint(first, Guid.NewGuid(), "subsequent replacement", at.AddMinutes(5));
        Assert.Equal(clock.Id, request.Acknowledge(first, Guid.NewGuid(), at.AddMinutes(6), null).Id);
        Assert.Equal(at.AddMinutes(3), request.AcknowledgedAt);
        Assert.Equal(2, request.Appointments.Count);
        Assert.Throws<InvalidOperationException>(() => request.Acknowledge(next, Guid.NewGuid(), at.AddMinutes(7), null));
    }
}
