using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Surveys;

[Trait("TaskId", "P1-22")]
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
        (await context.AuditLogs.CountAsync(log => log.EntityId == createdPlan.Plan.PlanId)).Should().Be(1);

        var request = new SurveyRequestCreationPersistenceRequest(
            scope.UserId,
            scope.ProjectId,
            scope.RoadSectionId,
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
    }

    [Fact(DisplayName = "P1-22: planning contract migration downgrades and reapplies")]
    public async Task MigrationLifecycle_DowngradesAndReapplies()
    {
        await using var context = _fixture.CreateDbContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20260921134719_AddP230FlightSurveyIdentityImmutability");

        await context.Database.MigrateAsync();
        var columns = await context.Database.SqlQueryRaw<int>(
            """
            SELECT CAST(COUNT(*) AS int) AS [Value]
            FROM sys.columns
            WHERE ([object_id] = OBJECT_ID(N'[dbo].[SurveyPlans]') AND [name] = N'OutputRequirements')
               OR ([object_id] = OBJECT_ID(N'[dbo].[SurveyRequests]') AND [name] IN (N'DueAt', N'OutputRequirements'))
            """).SingleAsync();
        columns.Should().Be(3);
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
        context.AddRange(project, user, roadSection);
        await context.SaveChangesAsync();
        return new(project.Id, roadSection.Id, user.Id);
    }

    private sealed record PlanningScope(Guid ProjectId, Guid RoadSectionId, Guid UserId);
}
