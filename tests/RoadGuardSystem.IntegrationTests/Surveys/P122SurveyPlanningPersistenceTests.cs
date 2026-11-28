using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.Warranties;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.Repositories.Spatial;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Surveys;

[Trait("TaskId", "P1-22")]

[Trait("TaskId", "P1-22")]
public sealed class P122SurveyPlanningPersistenceSqlTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P122SurveyPlanningPersistenceSqlTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P1-22: plan, request, and postpone commit audit and idempotent replay")]
    public async Task PlanningCommands_ReplayWithoutDuplicateEffects()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var repository = new SurveyPlanningPersistenceService(context, new IdempotencyOperationService(context));
        var startAt = DateTimeOffset.UtcNow.AddDays(2);
        var planRequest = new SurveyPlanCreationPersistenceRequest(
            scope.UserId,
            scope.ProjectId,
            scope.RoadSectionId,
            scope.RoadSectionVersionId,
            startAt,
            startAt.AddHours(2),
            SurveyType.Periodic,
            "{\"formats\":[\"video\"]}",
            "p122-plan-key",
            new string('a', 64),
            Guid.NewGuid(),
            Guid.NewGuid());

        var createdPlan = await repository.CreatePlanAsync(planRequest);
        var replayedPlan = await repository.CreatePlanAsync(planRequest);

        createdPlan.Status.Should().Be(SurveyPlanPersistenceStatus.Success);
        replayedPlan.Status.Should().Be(SurveyPlanPersistenceStatus.Replayed);
        replayedPlan.Plan!.PlanId.Should().Be(createdPlan.Plan!.PlanId);
        (await context.SurveyPlans.Where(plan => plan.Id == createdPlan.Plan.PlanId)
            .Select(plan => plan.WarrantyId).SingleAsync()).Should().Be(scope.WarrantyId);
        (await context.AuditLogs.CountAsync(log => log.EntityId == createdPlan.Plan.PlanId)).Should().Be(1);

        var request = new SurveyRequestCreationPersistenceRequest(
            scope.UserId,
            scope.ProjectId,
            scope.RoadSectionId,
            scope.RoadSectionVersionId,
            createdPlan.Plan.PlanId,
            SurveyType.Periodic,
            DateTimeOffset.UtcNow.AddDays(4),
            "{\"formats\":[\"video\",\"srt\"]}",
            "p122-request-key",
            new string('b', 64),
            Guid.NewGuid(),
            Guid.NewGuid());
        var createdRequest = await repository.CreateRequestAsync(request);
        createdRequest.Status.Should().Be(SurveyRequestPersistenceStatus.Success);
        createdRequest.Request!.Status.Should().Be(SurveyRequestStatus.NewAssigned);

        var postpone = new SurveyPlanPostponementPersistenceRequest(
            scope.UserId,
            scope.ProjectId,
            createdPlan.Plan.PlanId,
            startAt.AddHours(1),
            "Weather delay",
            "p122-postpone-key",
            new string('c', 64),
            Guid.NewGuid(),
            Guid.NewGuid());
        var postponed = await repository.PostponePlanAsync(postpone);
        var postponedReplay = await repository.PostponePlanAsync(postpone);

        postponed.Status.Should().Be(SurveyPlanPostponementPersistenceStatus.Success);
        postponed.Postponement!.Status.Should().Be(SurveyPlanStatus.Postponed);
        postponedReplay.Status.Should().Be(SurveyPlanPostponementPersistenceStatus.Replayed);
        (await context.SurveyPlanPostponements.CountAsync(item => item.SurveyPlanId == createdPlan.Plan.PlanId)).Should().Be(1);

        var historicalPlan = SurveyPlan.Create(Guid.NewGuid(), scope.ProjectId, scope.RoadSectionId,
            startAt, startAt, SurveyType.Supplementary, SurveyPlanStatus.Planned, roadSectionVersionId: scope.RoadSectionVersionId);
        context.SurveyPlans.Add(historicalPlan);
        await context.SaveChangesAsync();
        var historicalPostpone = postpone with
        {
            SurveyPlanId = historicalPlan.Id,
            IdempotencyKey = "p122-historical-postpone-key",
            RequestFingerprint = new string('d', 64),
            OperationId = Guid.NewGuid(),
            NewPlannedStartAt = startAt
        };

        var datedHistoricalPostpone = await repository.PostponePlanAsync(historicalPostpone);
        datedHistoricalPostpone.Status.Should().Be(SurveyPlanPostponementPersistenceStatus.InvalidInput);
        datedHistoricalPostpone.ScheduleError!.Code.Should().Be("survey_time_range_invalid");
        var undatedHistoricalPostpone = await repository.PostponePlanAsync(historicalPostpone with
        {
            NewPlannedStartAt = null,
            RequestFingerprint = new string('e', 64)
        });
        undatedHistoricalPostpone.Status.Should().Be(SurveyPlanPostponementPersistenceStatus.Success);
        (await context.SurveyPlanPostponements.CountAsync(item => item.SurveyPlanId == historicalPlan.Id)).Should().Be(1);
    }

    [Fact(DisplayName = "P2-22: planning commands reject a road section version outside the scope")]
    public async Task PlanningCommands_WrongRoadSectionVersion_ReturnScopeConflict()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var repository = new SurveyPlanningPersistenceService(context, new IdempotencyOperationService(context));
        var startAt = DateTimeOffset.UtcNow.AddDays(2);

        var result = await repository.CreatePlanAsync(new SurveyPlanCreationPersistenceRequest(
            scope.UserId,
            scope.ProjectId,
            scope.RoadSectionId,
            Guid.NewGuid(),
            startAt,
            startAt.AddHours(2),
            SurveyType.Periodic,
            "{\"formats\":[\"video\"]}",
            "p122-wrong-version",
            new string('d', 64),
            Guid.NewGuid(),
            Guid.NewGuid()));

        result.Status.Should().Be(SurveyPlanPersistenceStatus.ScopeConflict);
    }

    private async Task<PlanningScope> CreateScopeAsync(RoadGuardDbContext context)
    {
        await _fixture.SeedRolesAsync(context);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"P122-{Guid.NewGuid():N}",
            Name = "P1-22 fixture project",
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"p122_user_{Guid.NewGuid():N}",
            DisplayName = "P1-22 fixture PM",
            PasswordHash = "fixture-password-hash",
            RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var roadSection = RoadSection.Create(Guid.NewGuid(), project.Id, $"P122-ROAD-{Guid.NewGuid():N}");
        var version = RoadSectionVersion.Create(
            Guid.NewGuid(), roadSection.Id, 1, true,
            new GeometryFactory(new PrecisionModel(), SpatialConstants.UtmZone48NSrid)
                .CreateLineString(new[] { new Coordinate(588500, 2325000), new Coordinate(588600, 2325100) }),
            DateTimeOffset.UtcNow, "P1-22 planning version");
        var firstDay = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        var warranty = Warranty.Create(
            Guid.NewGuid(), project.Id, null, null, firstDay, firstDay, firstDay.AddYears(1),
            null, WarrantyScope.Project, null, null, WarrantyStatus.Active);
        context.AddRange(project, user, roadSection, version, warranty);
        await context.SaveChangesAsync();
        return new(project.Id, roadSection.Id, version.Id, user.Id, warranty.Id);
    }

    private sealed record PlanningScope(Guid ProjectId, Guid RoadSectionId, Guid RoadSectionVersionId, Guid UserId, Guid WarrantyId);
}
