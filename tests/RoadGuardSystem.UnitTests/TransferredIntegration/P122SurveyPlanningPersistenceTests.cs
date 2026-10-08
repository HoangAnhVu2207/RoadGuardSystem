using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.Repositories.Spatial;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Surveys;

public sealed class P122SurveyPlanningPersistenceTests
{
    [Fact(DisplayName = "P1-22: survey plan and request retain output requirements and request due date")]
    public void SurveyPlanningEntities_RetainNewPersistenceFields()
    {
        var projectId = Guid.NewGuid();
        var roadSectionId = Guid.NewGuid();
        var requestedAt = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
        var dueAt = requestedAt.AddDays(3);
        const string outputRequirements = "{\"formats\":[\"video\",\"srt\"]}";

        var plan = SurveyPlan.Create(
            Guid.NewGuid(),
            projectId,
            roadSectionId,
            requestedAt,
            requestedAt.AddHours(4),
            SurveyType.Periodic,
            SurveyPlanStatus.Planned,
            outputRequirements);
        var request = SurveyRequest.Create(
            Guid.NewGuid(),
            projectId,
            roadSectionId,
            plan.Id,
            Guid.NewGuid(),
            SurveyType.Periodic,
            SurveyRequestStatus.NewAssigned,
            requestedAt,
            dueAt,
            outputRequirements);

        plan.OutputRequirements.Should().Be(outputRequirements);
        request.DueAt.Should().Be(dueAt);
        request.OutputRequirements.Should().Be(outputRequirements);
        request.Status.Should().Be(SurveyRequestStatus.NewAssigned);
    }
}
