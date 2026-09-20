using FluentAssertions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Surveys;
using Xunit;

namespace RoadGuardSystem.UnitTests.Surveys;

public sealed class P222SurveyRequestTests
{
    [Fact]
    public void Create_WithPlanSource_PreservesRequestScope()
    {
        var requestId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var roadSectionId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var requestedAt = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

        var request = SurveyRequest.Create(
            requestId,
            projectId,
            roadSectionId,
            planId,
            requesterId,
            SurveyType.Periodic,
            SurveyRequestStatus.NewAssigned,
            requestedAt);

        request.Id.Should().Be(requestId);
        request.ProjectId.Should().Be(projectId);
        request.RoadSectionId.Should().Be(roadSectionId);
        request.SurveyPlanId.Should().Be(planId);
        request.RequestedByUserId.Should().Be(requesterId);
        request.Status.Should().Be(SurveyRequestStatus.NewAssigned);
        request.RequestedAt.Should().Be(requestedAt);
        request.CancelledAt.Should().BeNull();
        request.CancellationReason.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyScopeOrUnknownStatus_IsRejected()
    {
        var requestedAt = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
        var emptyRequester = () => SurveyRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            Guid.Empty,
            SurveyType.Original,
            SurveyRequestStatus.NewAssigned,
            requestedAt);
        var unknownStatus = () => SurveyRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            SurveyType.Original,
            SurveyRequestStatus.Unknown,
            requestedAt);

        emptyRequester.Should().Throw<ArgumentException>();
        unknownStatus.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Status_UsesTheCanonicalSurveyRequestWorkflowValues()
    {
        var expected = new[]
        {
            SurveyRequestStatus.Unknown,
            SurveyRequestStatus.NewAssigned,
            SurveyRequestStatus.Accepted,
            SurveyRequestStatus.Rejected,
            SurveyRequestStatus.Reassigned,
            SurveyRequestStatus.InProgress,
            SurveyRequestStatus.Submitted,
            SurveyRequestStatus.SupplementRequired,
            SurveyRequestStatus.Completed,
            SurveyRequestStatus.Cancelled,
            SurveyRequestStatus.Postponed
        };

        Enum.GetValues<SurveyRequestStatus>().Should().BeEquivalentTo(expected);
    }
}
