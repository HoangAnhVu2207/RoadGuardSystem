using RoadGuardSystem.BusinessObjects.Retention;
using Xunit;
namespace RoadGuardSystem.UnitTests.Retention;

public sealed class Anh02RetentionCalendarTests
{
    [Theory]
    [InlineData("2027-10-02", "2032-10-02T17:00:00Z")]
    [InlineData("2028-02-29", "2033-02-28T17:00:00Z")]
    [InlineData("2023-12-31", "2028-12-31T17:00:00Z")]
    public void Latest_warranty_keeps_entire_Vietnam_calendar_day(string end, string boundary)
    {
        var actual = RetentionEligibilityEvaluator.WarrantyEligibleAfter(DateOnly.Parse(end));
        Assert.Equal(DateTimeOffset.Parse(boundary), actual);
        var input = new RetentionEligibilityInput(true, true, true, 0, [actual.AddYears(-1), actual]);
        Assert.Equal("RETAIN_UNTIL", RetentionEligibilityEvaluator.Evaluate(input, actual.AddTicks(-1)).Eligibility);
        Assert.Equal("ELIGIBLE_FOR_REVIEW", RetentionEligibilityEvaluator.Evaluate(input, actual).Eligibility);
    }
    [Fact]
    public void Release_one_of_two_independent_holds_still_blocks()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Equal("BLOCKED_HOLD", RetentionEligibilityEvaluator.Evaluate(new(true, true, true, 2, [now.AddYears(-1)]), now).Eligibility);
        Assert.Equal("BLOCKED_HOLD", RetentionEligibilityEvaluator.Evaluate(new(true, true, true, 1, [now.AddYears(-1)]), now).Eligibility);
        Assert.Equal("ELIGIBLE_FOR_REVIEW", RetentionEligibilityEvaluator.Evaluate(new(true, true, true, 0, [now.AddYears(-1)]), now).Eligibility);
    }
}
