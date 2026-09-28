using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Surveys;

[Trait("TaskId", "P2-054/P2-055/P2-011/P2-012")]
public sealed class P2V2SurveyScopeConcurrencyTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P2V2SurveyScopeConcurrencyTests(IdentitySqlServerFixture fixture) => _fixture = fixture;

    [Fact(DisplayName = "P2 V2: plan persists every route-version scope row and SQL rowversion")]
    public async Task SurveyPlan_MultiRouteScopeAndRowVersion_RoundTrips()
    {
        await using var context = _fixture.CreateDbContext();
        var fixture = await CreateFixtureAsync(context, 2);
        var plan = SurveyPlan.Create(
            Guid.NewGuid(),
            fixture.ProjectId,
            fixture.RoadSectionIds[0],
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            SurveyType.Original,
            SurveyPlanStatus.Planned,
            "[]",
            fixture.RouteVersionIds[0]);
        context.SurveyPlans.Add(plan);
        context.SurveyPlanScopes.AddRange(
            fixture.RouteVersionIds.Select((routeVersionId, index) => SurveyPlanScope.Create(
                Guid.NewGuid(),
                plan.Id,
                routeVersionId,
                Guid.NewGuid(),
                $"[\"{Guid.NewGuid()}\"]",
                index == 0 ? "SURFACE" : "LEFT_EDGE")));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persisted = await context.SurveyPlans.AsNoTracking().SingleAsync(item => item.Id == plan.Id);
        var scopes = await context.SurveyPlanScopes.AsNoTracking().Where(item => item.SurveyPlanId == plan.Id).ToListAsync();

        persisted.RowVersion.Should().NotBeEmpty();
        scopes.Should().HaveCount(2);
        scopes.Select(item => item.RouteSectionVersionId).Should().BeEquivalentTo(fixture.RouteVersionIds);
    }

    [Fact(DisplayName = "P2 V2: stale survey plan rowversion rejects concurrent postpone")]
    public async Task SurveyPlan_StaleRowVersion_IsRejected()
    {
        await using var setupContext = _fixture.CreateDbContext();
        var fixture = await CreateFixtureAsync(setupContext, 1);
        var plan = SurveyPlan.Create(
            Guid.NewGuid(),
            fixture.ProjectId,
            fixture.RoadSectionIds[0],
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddHours(1),
            SurveyType.Original,
            SurveyPlanStatus.Planned,
            "[]",
            fixture.RouteVersionIds[0]);
        setupContext.SurveyPlans.Add(plan);
        await setupContext.SaveChangesAsync();
        setupContext.ChangeTracker.Clear();

        await using var firstContext = _fixture.CreateDbContext();
        await using var secondContext = _fixture.CreateDbContext();
        var first = await firstContext.SurveyPlans.SingleAsync(item => item.Id == plan.Id);
        var second = await secondContext.SurveyPlans.SingleAsync(item => item.Id == plan.Id);
        var firstVersion = first.RowVersion.ToArray();
        second.RowVersion.Should().Equal(firstVersion);

        first.Postpone(null);
        firstContext.SurveyPlanPostponements.Add(SurveyPlanPostponement.Create(Guid.NewGuid(), first.Id, DateTimeOffset.UtcNow, "First update", null));
        await firstContext.SaveChangesAsync();

        second.Postpone(null);
        secondContext.SurveyPlanPostponements.Add(SurveyPlanPostponement.Create(Guid.NewGuid(), second.Id, DateTimeOffset.UtcNow, "Stale update", null));
        var staleSave = () => secondContext.SaveChangesAsync();

        await staleSave.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    private async Task<FixtureData> CreateFixtureAsync(RoadGuardDbContext context, int routeCount)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"P2V2-{Guid.NewGuid():N}",
            Name = "P2 V2 scope fixture",
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Projects.Add(project);
        var sections = Enumerable.Range(0, routeCount)
            .Select(index => RoadSection.Create(Guid.NewGuid(), project.Id, $"P2V2-ROAD-{index}-{Guid.NewGuid():N}"))
            .ToArray();
        context.RoadSections.AddRange(sections);
        await context.SaveChangesAsync();

        var geometryFactory = new GeometryFactory(new PrecisionModel(), 32648);
        var versions = sections.Select((section, index) => RoadSectionVersion.Create(
            Guid.NewGuid(),
            section.Id,
            1,
            true,
            geometryFactory.CreateLineString([
                new Coordinate(500000 + index * 100, 1200000),
                new Coordinate(500100 + index * 100, 1200000)]),
            DateTimeOffset.UtcNow,
            "P2 V2 scope fixture")).ToArray();
        context.RoadSectionVersions.AddRange(versions);
        await context.SaveChangesAsync();
        return new FixtureData(project.Id, sections.Select(item => item.Id).ToArray(), versions.Select(item => item.Id).ToArray());
    }

    private sealed record FixtureData(Guid ProjectId, IReadOnlyList<Guid> RoadSectionIds, IReadOnlyList<Guid> RouteVersionIds);
}
