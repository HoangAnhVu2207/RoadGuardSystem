using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Spatial;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Surveys;

[Trait("TaskId", "P2-30")]
public sealed class P230SupplementarySurveyRequestSchemaTests : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly GeometryFactory Srid32648Factory = new(new PrecisionModel(), SpatialConstants.UtmZone48NSrid);
    private readonly IdentitySqlServerFixture _fixture;

    public P230SupplementarySurveyRequestSchemaTests(IdentitySqlServerFixture fixture) => _fixture = fixture;

    [Fact(DisplayName = "P2-30: supplementary request is independent and round-trips through SQL Server")]
    public async Task SupplementarySurveyRequest_ValidRound_RoundTrips()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var request = SupplementarySurveyRequest.Create(
            Guid.NewGuid(), scope.SurveyId, null, scope.ProjectManagerUserId, "Coverage gap", "{\"segment\":\"north\"}",
            1, SupplementarySurveyRequestStatus.Requested, null, null, "Source files remain immutable.");

        context.SupplementarySurveyRequests.Add(request);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        (await context.SupplementarySurveyRequests.AsNoTracking().SingleAsync(item => item.Id == request.Id))
            .RoundNo.Should().Be(1);
    }

    [Fact(DisplayName = "P2-30: SQL Server rejects duplicate supplementary round and invalid requested scope JSON")]
    public async Task SupplementarySurveyRequest_InvalidRoundOrScope_IsRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var first = SupplementarySurveyRequest.Create(
            Guid.NewGuid(), scope.SurveyId, null, scope.ProjectManagerUserId, "Coverage gap", "{\"segment\":\"north\"}",
            1, SupplementarySurveyRequestStatus.Requested, null, null, null);
        context.SupplementarySurveyRequests.Add(first);
        await context.SaveChangesAsync();

        var duplicateRound = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [SupplementarySurveyRequests]
                ([Id], [SurveyId], [RequestedByUserId], [Reason], [RequestedScope], [RoundNo], [Status])
            VALUES ({Guid.NewGuid()}, {scope.SurveyId}, {scope.ProjectManagerUserId}, {"Retry coverage"},
                {"{\"segment\":\"south\"}"}, {1}, {(byte)SupplementarySurveyRequestStatus.Requested})
            """);
        var invalidJson = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [SupplementarySurveyRequests]
                ([Id], [SurveyId], [RequestedByUserId], [Reason], [RequestedScope], [RoundNo], [Status])
            VALUES ({Guid.NewGuid()}, {scope.SurveyId}, {scope.ProjectManagerUserId}, {"Invalid scope"},
                {"not-json"}, {2}, {(byte)SupplementarySurveyRequestStatus.Requested})
            """);

        await duplicateRound.Should().ThrowAsync<SqlException>();
        await invalidJson.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-30: supplementary request migration downgrades and reapplies")]
    public async Task MigrationLifecycle_DowngradesToDatasetSchemaAndReapplies()
    {
        await using var context = _fixture.CreateDbContext();
        (await CountSupplementaryTablesAsync(context)).Should().Be(1);

        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20260921131520_AddP230DataVersionQualityCheckSchema");
        (await CountSupplementaryTablesAsync(context)).Should().Be(0);

        await context.Database.MigrateAsync();
        (await CountSupplementaryTablesAsync(context)).Should().Be(1);
    }

    private async Task<SupplementaryScope> CreateScopeAsync(RoadGuardDbContext context)
    {
        await _fixture.SeedRolesAsync(context);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"P230-SUP-{Guid.NewGuid():N}",
            Name = "P2-30 supplementary fixture",
            EngineeringUtmSrid = SpatialConstants.UtmZone48NSrid,
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var manager = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"p230_pm_{Guid.NewGuid():N}",
            DisplayName = "P2-30 manager",
            PasswordHash = "fixture-password-hash",
            RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"P230-SUP-ROAD-{Guid.NewGuid():N}");
        var version = RoadSectionVersion.Create(
            Guid.NewGuid(), road.Id, 1, true,
            Srid32648Factory.CreateLineString(new[] { new Coordinate(588500, 2325000), new Coordinate(588600, 2325100) }),
            DateTimeOffset.UtcNow, "P2-30 supplementary fixture version");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        context.AddRange(project, manager, road, version, survey);
        await context.SaveChangesAsync();
        return new SupplementaryScope(survey.Id, manager.Id);
    }

    private static Task<int> CountSupplementaryTablesAsync(RoadGuardDbContext context)
        => context.Database.SqlQueryRaw<int>("""
            SELECT CAST(COUNT(*) AS int) AS [Value]
            FROM sys.tables WHERE [name] = 'SupplementarySurveyRequests'
            """).SingleAsync();

    private sealed record SupplementaryScope(Guid SurveyId, Guid ProjectManagerUserId);
}
