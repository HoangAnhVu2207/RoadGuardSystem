using RoadGuardSystem.BusinessObjects.Retention;
using Xunit;

namespace RoadGuardSystem.UnitTests.Retention;

public sealed class Anh02RetentionPreparationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2032-10-02T17:00:00Z");

    [Theory]
    [InlineData(false, true, true, "INVENTORY_INCOMPLETE")]
    [InlineData(true, false, true, "BASIS_UNCONFIRMED")]
    [InlineData(true, true, false, "BASIS_STALE")]
    public void Any_unproven_precondition_keeps_evaluation_waiting(bool complete, bool confirmed, bool current, string reason)
    {
        var result = RetentionEligibilityEvaluator.Evaluate(new(complete, confirmed, current, 0, [Now.AddDays(-1)]), Now);
        Assert.Equal("WAITING_RETENTION_BASIS", result.Eligibility);
        Assert.Contains(reason, result.ReasonCodes);
        Assert.Null(result.EligibleAfter);
    }

    [Fact]
    public void Maximum_of_known_subset_cannot_override_missing_obligation()
    {
        var result = RetentionEligibilityEvaluator.Evaluate(new(true, true, true, 0, [Now.AddDays(-1), null]), Now);
        Assert.Equal("WAITING_RETENTION_BASIS", result.Eligibility);
        Assert.Contains("OBLIGATION_UNRESOLVED", result.ReasonCodes);
        Assert.Null(result.EligibleAfter);
    }

    [Fact]
    public void Empty_inventory_has_no_evidence_of_eligibility()
    {
        var result = RetentionEligibilityEvaluator.Evaluate(new(true, true, true, 0, []), Now);
        Assert.Equal("WAITING_RETENTION_BASIS", result.Eligibility);
        Assert.Contains("OBLIGATION_UNRESOLVED", result.ReasonCodes);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Holds_take_precedence_but_do_not_hide_missing_basis(int holds)
    {
        var result = RetentionEligibilityEvaluator.Evaluate(new(false, false, false, holds, [null]), Now);
        Assert.Equal("BLOCKED_HOLD", result.Eligibility);
        Assert.Contains("ACTIVE_HOLD", result.ReasonCodes);
        Assert.Contains("INVENTORY_INCOMPLETE", result.ReasonCodes);
        Assert.Contains("BASIS_UNCONFIRMED", result.ReasonCodes);
        Assert.Contains("BASIS_STALE", result.ReasonCodes);
        Assert.Contains("OBLIGATION_UNRESOLVED", result.ReasonCodes);
    }

    [Fact]
    public void Exact_verified_boundary_is_review_eligibility_never_delete_approval()
    {
        var input = new RetentionEligibilityInput(true, true, true, 0, [Now.AddDays(-10), Now]);
        Assert.Equal("RETAIN_UNTIL", RetentionEligibilityEvaluator.Evaluate(input, Now.AddTicks(-1)).Eligibility);
        var result = RetentionEligibilityEvaluator.Evaluate(input, Now);
        Assert.Equal("ELIGIBLE_FOR_REVIEW", result.Eligibility);
        Assert.Equal(Now, result.EligibleAfter);
        Assert.Empty(result.ReasonCodes);
    }

    [Fact]
    public void Negative_hold_count_is_not_treated_as_unheld()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RetentionEligibilityEvaluator.Evaluate(new(true, true, true, -1, [Now]), Now));
    }
}
