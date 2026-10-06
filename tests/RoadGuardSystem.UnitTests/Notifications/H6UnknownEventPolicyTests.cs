using RoadGuardSystem.DTOs.Messaging;
using Xunit;

namespace RoadGuardSystem.UnitTests.Notifications;

public sealed class H6UnknownEventPolicyTests
{
    [Theory]
    [InlineData("field.task.future_action.v99", true)]
    [InlineData("field.task.assigned.v1 ", true)]
    [InlineData("field.task.assigned.v1", false)]
    [InlineData("processing_job.dispatch", false)]
    [InlineData("project.unrelated_action.v1", false)]
    public void DiagnosticOwnershipDoesNotRegisterOrLeaseUnknownActions(string type, bool diagnosticOwned)
    {
        Assert.Equal(diagnosticOwned, H6NotificationProtocolTypes.IsOwnedUnregistered(type));
        if (diagnosticOwned) Assert.False(H6NotificationProtocolTypes.Owns(type));
    }
}
