using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Surveys;

[Trait("TaskId", "P2-22")]
public sealed class P222SurveyPlanningSchemaTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P222SurveyPlanningSchemaTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-22: a plan and append-only postponement persist")]
    public async Task SurveyPlan_WithPostponement_RoundTrips()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var plan = CreatePlan(scope);
        context.SurveyPlans.Add(plan);
        await context.SaveChangesAsync();

        var postponement = SurveyPlanPostponement.Create(
            Guid.NewGuid(),
            plan.Id,
            DateTimeOffset.UtcNow,
            "Heavy rain",
            DateTimeOffset.UtcNow.AddDays(1));
        context.SurveyPlanPostponements.Add(postponement);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        (await context.SurveyPlanPostponements.AsNoTracking().SingleAsync(item => item.Id == postponement.Id))
            .Reason.Should().Be("Heavy rain");
    }

    [Fact(DisplayName = "P2-22: SQL Server rejects invalid plan dates, status, and active duplicates")]
    public async Task SurveyPlan_InvalidValuesAndActiveDuplicate_AreRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var validPlan = CreatePlan(scope);
        context.SurveyPlans.Add(validPlan);
        await context.SaveChangesAsync();
        var startAt = DateTimeOffset.UtcNow;

        var invalidDate = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [SurveyPlans]
                ([Id], [ProjectId], [RoadSectionId], [PlannedStartAt], [PlannedEndAt], [SurveyType], [Status])
            VALUES ({Guid.NewGuid()}, {scope.ProjectId}, {scope.RoadSectionId}, {startAt},
                {startAt.AddMinutes(-1)}, {1}, {1})
            """);
        var invalidStatus = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [SurveyPlans]
                ([Id], [ProjectId], [RoadSectionId], [PlannedStartAt], [PlannedEndAt], [SurveyType], [Status])
            VALUES ({Guid.NewGuid()}, {scope.ProjectId}, {scope.RoadSectionId}, {startAt},
                {startAt.AddHours(1)}, {1}, {99})
            """);
        var duplicateActive = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [SurveyPlans]
                ([Id], [ProjectId], [RoadSectionId], [PlannedStartAt], [PlannedEndAt], [SurveyType], [Status])
            VALUES ({Guid.NewGuid()}, {scope.ProjectId}, {scope.RoadSectionId}, {startAt},
                {startAt.AddHours(1)}, {(byte)validPlan.SurveyType}, {(byte)SurveyPlanStatus.Planned})
            """);

        await invalidDate.Should().ThrowAsync<SqlException>();
        await invalidStatus.Should().ThrowAsync<SqlException>();
        await duplicateActive.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-22: request requires a valid source plan and one active request per plan")]
    public async Task SurveyRequest_MissingPlanAndActiveDuplicate_AreRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var plan = CreatePlan(scope);
        context.SurveyPlans.Add(plan);
        await context.SaveChangesAsync();
        var firstRequest = CreateRequest(scope, plan.Id);
        context.SurveyRequests.Add(firstRequest);
        await context.SaveChangesAsync();

        context.SurveyRequests.Add(CreateRequest(scope, Guid.NewGuid()));
        var missingPlan = () => context.SaveChangesAsync();

        await missingPlan.Should().ThrowAsync<DbUpdateException>();
        context.ChangeTracker.Clear();
        context.SurveyRequests.Add(CreateRequest(scope, plan.Id));
        var duplicateActive = () => context.SaveChangesAsync();
        await duplicateActive.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-22: SQL Server rejects cross-project road and source-plan scope")]
    public async Task SurveyPlanAndRequest_CrossProjectScope_AreRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var firstScope = await CreateScopeAsync(context);
        var secondScope = await CreateScopeAsync(context);
        var startAt = DateTimeOffset.UtcNow.AddDays(3);
        var mismatchedRoadPlan = SurveyPlan.Create(
            Guid.NewGuid(),
            firstScope.ProjectId,
            secondScope.RoadSectionId,
            startAt,
            startAt.AddHours(2),
            SurveyType.Periodic,
            SurveyPlanStatus.Planned);
        context.SurveyPlans.Add(mismatchedRoadPlan);

        var saveMismatchedRoad = () => context.SaveChangesAsync();

        await saveMismatchedRoad.Should().ThrowAsync<DbUpdateException>();
        context.ChangeTracker.Clear();
        var validPlan = CreatePlan(firstScope);
        context.SurveyPlans.Add(validPlan);
        await context.SaveChangesAsync();
        context.SurveyRequests.Add(SurveyRequest.Create(
            Guid.NewGuid(),
            secondScope.ProjectId,
            secondScope.RoadSectionId,
            validPlan.Id,
            secondScope.RequestedByUserId,
            SurveyType.Periodic,
            SurveyRequestStatus.NewAssigned,
            DateTimeOffset.UtcNow));

        var saveMismatchedPlan = () => context.SaveChangesAsync();

        await saveMismatchedPlan.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-22: SQL Server prevents postponement mutation and deletion")]
    public async Task SurveyPlanPostponement_MutationAndDelete_AreRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var plan = CreatePlan(scope);
        context.SurveyPlans.Add(plan);
        await context.SaveChangesAsync();
        var postponement = SurveyPlanPostponement.Create(Guid.NewGuid(), plan.Id, DateTimeOffset.UtcNow, "Rain", null);
        context.SurveyPlanPostponements.Add(postponement);
        await context.SaveChangesAsync();

        var update = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [SurveyPlanPostponements] SET [Reason] = {"Changed"} WHERE [Id] = {postponement.Id}");
        var delete = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM [SurveyPlanPostponements] WHERE [Id] = {postponement.Id}");

        await update.Should().ThrowAsync<SqlException>();
        await delete.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-22: migration downgrades to P2-21 and reapplies survey planning schema")]
    public async Task MigrationLifecycle_DowngradesToP221AndReapplies()
    {
        await using var context = _fixture.CreateDbContext();
        (await CountP222TablesAsync(context)).Should().Be(3);

        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20260920154542_AddRoadSectionVersionAndWarrantySchema");
        (await CountP222TablesAsync(context)).Should().Be(0);

        await context.Database.MigrateAsync();
        (await CountP222TablesAsync(context)).Should().Be(3);
    }

    private async Task<SurveyScope> CreateScopeAsync(RoadGuardDbContext context)
    {
        await _fixture.SeedRolesAsync(context);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"P222-{Guid.NewGuid():N}",
            Name = "P2-22 fixture project",
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"p222_user_{Guid.NewGuid():N}",
            DisplayName = "P2-22 fixture PM",
            PasswordHash = "fixture-password-hash",
            RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var roadSection = RoadSection.Create(Guid.NewGuid(), project.Id, $"P222-ROAD-{Guid.NewGuid():N}");
        context.AddRange(project, user, roadSection);
        await context.SaveChangesAsync();
        return new SurveyScope(project.Id, roadSection.Id, user.Id);
    }

    private static SurveyPlan CreatePlan(SurveyScope scope)
    {
        var plannedStartAt = DateTimeOffset.UtcNow.AddDays(3);
        return SurveyPlan.Create(
            Guid.NewGuid(),
            scope.ProjectId,
            scope.RoadSectionId,
            plannedStartAt,
            plannedStartAt.AddHours(2),
            SurveyType.Periodic,
            SurveyPlanStatus.Planned);
    }

    private static SurveyRequest CreateRequest(SurveyScope scope, Guid surveyPlanId)
        => SurveyRequest.Create(
            Guid.NewGuid(),
            scope.ProjectId,
            scope.RoadSectionId,
            surveyPlanId,
            scope.RequestedByUserId,
            SurveyType.Periodic,
            SurveyRequestStatus.NewAssigned,
            DateTimeOffset.UtcNow);

    private static Task<int> CountP222TablesAsync(RoadGuardDbContext context)
        => context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.tables
                WHERE [name] IN ('SurveyPlans', 'SurveyPlanPostponements', 'SurveyRequests')
                """)
            .SingleAsync();

    private sealed record SurveyScope(Guid ProjectId, Guid RoadSectionId, Guid RequestedByUserId);
}
