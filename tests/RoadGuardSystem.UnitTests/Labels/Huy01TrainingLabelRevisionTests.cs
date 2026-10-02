using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Labels;
using Xunit;

namespace RoadGuardSystem.UnitTests.Labels;

[Trait("Package", "HUY-01")]
public sealed class Huy01TrainingLabelRevisionTests
{
    [Fact]
    public void Create_BboxExceedsImageBounds_IsRejected()
    {
        var create = () => TrainingLabelRevision.Create(
            Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), "source-v1", Guid.NewGuid(),
            0.8m, 0.2m, 0.3m, 0.4m, "CRACK", "invalid annotation");

        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Review_ApprovedRevision_PreservesApprovalProofAndRejectsSecondDecision()
    {
        var label = TrainingLabelRevision.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            "source-v1",
            Guid.NewGuid(),
            0.1m,
            0.2m,
            0.3m,
            0.4m,
            "CRACK",
            "manual PM annotation");
        var reviewerId = Guid.NewGuid();
        var reviewedAt = new DateTimeOffset(2026, 10, 2, 4, 0, 0, TimeSpan.Zero);

        label.Review(reviewerId, TrainingLabelReviewStatus.Approved, "verified evidence", reviewedAt);

        label.Status.Should().Be(TrainingLabelReviewStatus.Approved);
        label.ReviewedByUserId.Should().Be(reviewerId);
        label.ReviewedAt.Should().Be(reviewedAt);
        var repeat = () => label.Review(Guid.NewGuid(), TrainingLabelReviewStatus.Rejected, "late decision", reviewedAt.AddMinutes(1));
        repeat.Should().Throw<InvalidOperationException>();
    }
}
