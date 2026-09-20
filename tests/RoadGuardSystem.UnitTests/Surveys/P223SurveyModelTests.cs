using FluentAssertions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Surveys;
using Xunit;

namespace RoadGuardSystem.UnitTests.Surveys;

public sealed class P223SurveyModelTests
{
    [Fact]
    public void CreateSurvey_PreservesVersionAnchorAndRequiresConsistentBaselineMetadata()
    {
        var surveyId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var roadSectionVersionId = Guid.NewGuid();
        var confirmedByUserId = Guid.NewGuid();
        var confirmedAt = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

        var survey = Survey.Create(
            surveyId,
            requestId,
            projectId,
            roadSectionVersionId,
            SurveyType.Original,
            SurveyStatus.Draft,
            isBaselineConfirmed: true,
            baselineConfirmedByUserId: confirmedByUserId,
            baselineConfirmedAt: confirmedAt);

        survey.Id.Should().Be(surveyId);
        survey.SurveyRequestId.Should().Be(requestId);
        survey.ProjectId.Should().Be(projectId);
        survey.RoadSectionVersionId.Should().Be(roadSectionVersionId);
        survey.IsBaselineConfirmed.Should().BeTrue();
        survey.BaselineConfirmedByUserId.Should().Be(confirmedByUserId);
        survey.BaselineConfirmedAt.Should().Be(confirmedAt);

        var inconsistentBaseline = () => Survey.Create(
            Guid.NewGuid(),
            requestId,
            projectId,
            roadSectionVersionId,
            SurveyType.Original,
            SurveyStatus.Draft,
            isBaselineConfirmed: true,
            baselineConfirmedByUserId: null,
            baselineConfirmedAt: null);

        inconsistentBaseline.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateAssignment_RequiresRejectionReasonWhenRejected()
    {
        var assignmentId = Guid.NewGuid();
        var assignedAt = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

        var assignment = SurveyAssignment.Create(
            assignmentId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            assignedAt,
            acceptedAt: null,
            rejectedAt: assignedAt.AddMinutes(5),
            rejectionReason: "Weather restriction",
            reassignmentReason: null,
            endedAt: assignedAt.AddMinutes(5));

        assignment.Id.Should().Be(assignmentId);
        assignment.RejectionReason.Should().Be("Weather restriction");
        assignment.EndedAt.Should().Be(assignedAt.AddMinutes(5));

        var missingReason = () => SurveyAssignment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            assignedAt,
            acceptedAt: null,
            rejectedAt: assignedAt.AddMinutes(5),
            rejectionReason: null,
            reassignmentReason: null,
            endedAt: assignedAt.AddMinutes(5));

        missingReason.Should().Throw<ArgumentException>();

        var rejectedWithReassignmentReason = () => SurveyAssignment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            assignedAt,
            acceptedAt: null,
            rejectedAt: assignedAt.AddMinutes(5),
            rejectionReason: "Weather restriction",
            reassignmentReason: "Replacement operator assigned",
            endedAt: assignedAt.AddMinutes(5));

        rejectedWithReassignmentReason.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Assignment_AcceptAndReject_EnforcePersistableHistoryTransitions()
    {
        var assignedAt = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
        var accepted = SurveyAssignment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            assignedAt,
            null,
            null,
            null,
            null,
            null);

        accepted.Accept(assignedAt.AddMinutes(5));
        accepted.AcceptedAt.Should().Be(assignedAt.AddMinutes(5));

        var rejected = SurveyAssignment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            assignedAt,
            null,
            null,
            null,
            null,
            null);
        rejected.Reject(assignedAt.AddMinutes(5), "Equipment unavailable");

        rejected.RejectedAt.Should().Be(assignedAt.AddMinutes(5));
        rejected.RejectionReason.Should().Be("Equipment unavailable");
        rejected.EndedAt.Should().Be(assignedAt.AddMinutes(5));
        var rejectAfterAccept = () => accepted.Reject(assignedAt.AddMinutes(6), "Late refusal");
        rejectAfterAccept.Should().Throw<InvalidOperationException>();
    }
}
