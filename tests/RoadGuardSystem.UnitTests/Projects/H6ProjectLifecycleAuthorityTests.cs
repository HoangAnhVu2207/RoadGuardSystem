using FluentAssertions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Projects;
using Xunit;

namespace RoadGuardSystem.UnitTests.Projects;

public sealed class H6ProjectLifecycleAuthorityTests
{
    [Theory]
    [InlineData(ProjectLifecycleCommandKind.DefectClose)]
    [InlineData(ProjectLifecycleCommandKind.LinkedRecurrenceCreate)]
    [InlineData(ProjectLifecycleCommandKind.ConstructionComplete)]
    [InlineData(ProjectLifecycleCommandKind.OperationalClose)]
    [InlineData(ProjectLifecycleCommandKind.ObligationTransferGrant)]
    [InlineData(ProjectLifecycleCommandKind.ObligationTransferAccept)]
    public void PendingCommands_DoNotInheritSupervisorAuthority(ProjectLifecycleCommandKind command)
        => ProjectLifecycleAuthority.Evaluate(command, UserRoleCode.Supervisor, true, true)
            .Should().Be("lifecycle_authority_pending");

    [Theory]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.RepairCrew)]
    [InlineData(UserRoleCode.Reporter)]
    public void RenewedScope_OnlyConfirmedSupervisorRole(UserRoleCode role)
        => ProjectLifecycleAuthority.Evaluate(ProjectLifecycleCommandKind.RenewedHandlingScope, role, true, true)
            .Should().Be("access_forbidden");

    [Fact]
    public void CurrentSupervisor_WithActualClosureMayDecideRenewedScope()
        => ProjectLifecycleAuthority.Evaluate(ProjectLifecycleCommandKind.RenewedHandlingScope, UserRoleCode.Supervisor, true, true)
            .Should().Be("success");

    [Fact]
    public void RevokedSupervisor_CannotUseHistoricalPermission()
        => ProjectLifecycleAuthority.Evaluate(ProjectLifecycleCommandKind.RenewedHandlingScope, UserRoleCode.Supervisor, false, true)
            .Should().Be("access_forbidden");

    [Fact]
    public void LegacyClosedStatus_DoesNotProveOperationalClosure()
        => ProjectLifecycleAuthority.Evaluate(ProjectLifecycleCommandKind.RenewedHandlingScope, UserRoleCode.Supervisor, true, false)
            .Should().Be("operational_closure_source_unavailable");

    [Fact]
    public void UndefinedCommand_FailsClosed()
        => ProjectLifecycleAuthority.Evaluate((ProjectLifecycleCommandKind)255, UserRoleCode.Supervisor, true, true)
            .Should().Be("lifecycle_command_unregistered");
}
