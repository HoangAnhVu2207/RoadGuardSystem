using FluentAssertions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Surveys;
using Xunit;

namespace RoadGuardSystem.UnitTests.Surveys;

public sealed class P222SurveyPlanTests
{
    [Fact]
    public void Create_WithValidSchedule_PreservesSurveyScope()
    {
        var planId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var roadSectionId = Guid.NewGuid();
        var plannedStartAt = new DateTimeOffset(2026, 10, 1, 7, 0, 0, TimeSpan.Zero);
        var plannedEndAt = plannedStartAt.AddHours(2);

        var plan = SurveyPlan.Create(
            planId,
            projectId,
            roadSectionId,
            plannedStartAt,
            plannedEndAt,
            SurveyType.Periodic,
            SurveyPlanStatus.Planned);

        plan.Id.Should().Be(planId);
        plan.ProjectId.Should().Be(projectId);
        plan.RoadSectionId.Should().Be(roadSectionId);
        plan.PlannedStartAt.Should().Be(plannedStartAt);
        plan.PlannedEndAt.Should().Be(plannedEndAt);
        plan.SurveyType.Should().Be(SurveyType.Periodic);
        plan.Status.Should().Be(SurveyPlanStatus.Planned);
    }

    [Fact]
    public void Create_WithInvalidScheduleOrStatus_IsRejected()
    {
        var startAt = new DateTimeOffset(2026, 10, 1, 7, 0, 0, TimeSpan.Zero);

        var invalidDateRange = () => SurveyPlan.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            startAt,
            startAt.AddMinutes(-1),
            SurveyType.Original,
            SurveyPlanStatus.Planned);
        var invalidStatus = () => SurveyPlan.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            startAt,
            startAt.AddHours(1),
            SurveyType.Original,
            SurveyPlanStatus.Unknown);

        invalidDateRange.Should().Throw<ArgumentException>();
        invalidStatus.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreatePostponement_WithReasonAndOptionalNewStart_PreservesAppendOnlyRecord()
    {
        var postponementId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var postponedAt = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
        var newPlannedStartAt = postponedAt.AddDays(1);

        var postponement = SurveyPlanPostponement.Create(
            postponementId,
            planId,
            postponedAt,
            "Severe weather",
            newPlannedStartAt);

        postponement.Id.Should().Be(postponementId);
        postponement.SurveyPlanId.Should().Be(planId);
        postponement.PostponedAt.Should().Be(postponedAt);
        postponement.Reason.Should().Be("Severe weather");
        postponement.NewPlannedStartAt.Should().Be(newPlannedStartAt);
    }
}
