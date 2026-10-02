using RoadGuardSystem.DTOs.Surveys;
using RoadGuardSystem.Services.Surveys;
using FluentAssertions;
using Xunit;

namespace RoadGuardSystem.UnitTests.Surveys;

public sealed class Anh01AssessmentPolicyTests
{
    [Theory]
    [InlineData("PASS", false, false)]
    [InlineData("PASS", true, true)]
    [InlineData("UNKNOWN", false, true)]
    [InlineData("FAIL", false, true)]
    [InlineData("", true, false)]
    [InlineData("pass", true, false)]
    public void EveryDimensionIsExplicitAndPassNeedsEvidence(string status, bool hasEvidence, bool expected)
    {
        var item = new AssessmentItemDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "SURFACE", status, status, status,
            "Manual review rationale", hasEvidence ? [new(Guid.NewGuid())] : []);
        SurveyAssessmentService.ValidItems([item]).Should().Be(expected);
    }
    [Theory]
    [InlineData(null, null, true)]
    [InlineData(0L, 1L, true)]
    [InlineData(-1L, 1L, false)]
    [InlineData(1L, 1L, false)]
    [InlineData(2L, 1L, false)]
    [InlineData(null, 1L, false)]
    [InlineData(0L, null, false)]
    public void VideoIntervalsRequireAnOrderedNonnegativePair(long? from, long? to, bool expected)
    {
        var item = new AssessmentItemDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "SURFACE", "PASS", "PASS", "PASS",
            "Manual review rationale", [new(Guid.NewGuid(), from, to)]);
        SurveyAssessmentService.ValidItems([item]).Should().Be(expected);
    }
    [Fact]
    public void DuplicateScopeAndMissingReasonAreRejected()
    {
        var item = new AssessmentItemDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "SURFACE", "UNKNOWN", "UNKNOWN", "UNKNOWN", "Missing video", []);
        SurveyAssessmentService.ValidItems([item, item]).Should().BeFalse();
        SurveyAssessmentService.ValidItems([item with { Reason = " " }]).Should().BeFalse();
    }
}
