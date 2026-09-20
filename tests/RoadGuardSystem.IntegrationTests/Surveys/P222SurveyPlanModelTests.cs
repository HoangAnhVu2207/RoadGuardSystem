using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Surveys;

public sealed class P222SurveyPlanModelTests
{
    [Fact]
    public void Model_MapsSurveyPlanAndAppendOnlyPostponement()
    {
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=RoadGuard_ModelOnly;Trusted_Connection=True", sql => sql.UseNetTopologySuite())
            .Options;
        using var context = new RoadGuardDbContext(options);

        var plan = context.Model.FindEntityType(typeof(SurveyPlan));
        var postponement = context.Model.FindEntityType(typeof(SurveyPlanPostponement));
        var request = context.Model.FindEntityType(typeof(SurveyRequest));

        plan.Should().NotBeNull();
        var mappedPlan = plan!;
        mappedPlan.GetTableName().Should().Be("SurveyPlans");
        mappedPlan.GetForeignKeys().Should().HaveCount(2);
        postponement.Should().NotBeNull();
        var mappedPostponement = postponement!;
        mappedPostponement.GetTableName().Should().Be("SurveyPlanPostponements");
        mappedPostponement.GetForeignKeys().Should().ContainSingle(foreignKey => foreignKey.IsRequired);
        request.Should().NotBeNull();
        var mappedRequest = request!;
        mappedRequest.GetTableName().Should().Be("SurveyRequests");
        mappedRequest.GetForeignKeys().Should().HaveCount(4);
        mappedRequest.GetForeignKeys().Should().ContainSingle(foreignKey =>
            foreignKey.Properties.Single().Name == nameof(SurveyRequest.SurveyPlanId) && !foreignKey.IsRequired);
    }

    [Fact]
    public async Task SaveChanges_WithChangedPostponement_IsRejectedBeforeSql()
    {
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer("Server=invalid;Database=RoadGuard_ModelOnly;Trusted_Connection=True", sql => sql.UseNetTopologySuite())
            .Options;
        await using var context = new RoadGuardDbContext(options);
        var postponement = SurveyPlanPostponement.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "Severe weather",
            null);
        context.Attach(postponement);
        context.Entry(postponement).State = EntityState.Deleted;

        var save = () => context.SaveChangesAsync();

        await save.Should().ThrowAsync<InvalidOperationException>();
    }
}
