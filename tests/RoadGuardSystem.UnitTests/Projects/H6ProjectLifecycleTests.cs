using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Projects;
using Xunit;

namespace RoadGuardSystem.UnitTests.Projects;

public sealed class H6ProjectLifecycleTests
{
    [Fact]
    public void ConstructionCompletion_DoesNotResolveObligationOrCloseOperations()
    {
        var result = ProjectLifecycleProjection.Evaluate(true, false, false, [new(Guid.NewGuid(), true, false, null)]);
        result.ConstructionCompleted.Should().BeTrue();
        result.OperationallyClosed.Should().BeFalse();
        result.CanOperationallyClose.Should().BeFalse();
        result.OutstandingMandatoryObligationIds.Should().HaveCount(1);
        result.AcceptsNewReports.Should().BeTrue();
    }

    [Fact]
    public void ResolvedObligations_DoNotInventClosureDecisionOrWarranty()
    {
        var result = ProjectLifecycleProjection.Evaluate(false, false, false, [new(Guid.NewGuid(), true, true, null)]);
        result.CanOperationallyClose.Should().BeTrue();
        result.OperationallyClosed.Should().BeFalse();
        result.ConstructionCompleted.Should().BeFalse();
        result.WarrantyExists.Should().BeFalse();
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void TransferWithoutEveryConfirmedFact_DoesNotSatisfyClosure(bool authorized, bool accepted, bool matchingScope)
    {
        var transfer = new ProjectObligationTransferFact(Guid.NewGuid(), Guid.NewGuid(), authorized, accepted, matchingScope);
        var result = ProjectLifecycleProjection.Evaluate(true, true, true, [new(Guid.NewGuid(), true, false, transfer)]);
        result.CanOperationallyClose.Should().BeFalse();
        result.OperationallyClosed.Should().BeFalse();
        result.ClosureBasisInvalidated.Should().BeTrue();
        result.WarrantyExists.Should().BeTrue();
    }

    [Fact]
    public void ControlledAcceptedTransferFacts_KeepObligationUnresolvedButPermitClosureGate()
    {
        // Controlled facts test the invariant; they do not activate a production transfer command.
        var transfer = new ProjectObligationTransferFact(Guid.NewGuid(), Guid.NewGuid(), true, true, true);
        var result = ProjectLifecycleProjection.Evaluate(false, true, false, [new(Guid.NewGuid(), true, false, transfer)]);
        result.OperationallyClosed.Should().BeTrue();
        result.OutstandingMandatoryObligationIds.Should().BeEmpty();
        result.AcceptsNewReports.Should().BeTrue();
    }

    [Fact]
    public void ReopenedMandatoryObligation_InvalidatesLiveClosureWithoutRewritingSourceHistory()
    {
        var id = Guid.NewGuid();
        var original = ProjectLifecycleProjection.Evaluate(true, true, true, [new(id, true, true, null)]);
        var corrected = ProjectLifecycleProjection.Evaluate(true, true, true, [new(id, true, false, null)]);
        original.OperationallyClosed.Should().BeTrue();
        corrected.OperationallyClosed.Should().BeFalse();
        corrected.ClosureBasisInvalidated.Should().BeTrue();
        corrected.OutstandingMandatoryObligationIds.Should().ContainSingle().Which.Should().Be(id);
        original.OperationallyClosed.Should().BeTrue();
    }

    [Fact]
    public void DuplicateConflictingObligationFacts_AreRejected()
    {
        var id = Guid.NewGuid();
        var act = () => ProjectLifecycleProjection.Evaluate(false, false, false, [new(id, true, true, null), new(id, true, false, null)]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NonMandatoryObligation_DoesNotBlockButIsNotResolved()
    {
        var fact = new ProjectLifecycleObligationFact(Guid.NewGuid(), false, false, null);
        var result = ProjectLifecycleProjection.Evaluate(false, false, false, [fact]);
        result.CanOperationallyClose.Should().BeTrue();
        fact.Resolved.Should().BeFalse();
    }

    [Fact]
    public void ConfirmedTransferRequiresActualGrantAndReceiverIdentities()
    {
        var act = () => ProjectLifecycleProjection.Evaluate(false, true, false,
            [new(Guid.NewGuid(), true, false, new(Guid.Empty, Guid.Empty, true, true, true))]);
        act.Should().Throw<ArgumentException>();
    }
}
