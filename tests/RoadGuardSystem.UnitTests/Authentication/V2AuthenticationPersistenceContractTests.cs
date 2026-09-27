using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.UnitTests.Authentication;

[Trait("TaskId", "V2-P1-004")]
public sealed class V2AuthenticationPersistenceContractTests
{
    [Fact]
    public void PasswordRecovery_ModelAndMigrationAreDiscoverable()
    {
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(
                "Server=invalid;Database=RoadGuardModelOnly;User Id=invalid;Password=invalid;TrustServerCertificate=True",
                sql => sql.UseNetTopologySuite())
            .Options;
        using var context = new RoadGuardDbContext(options);

        context.Model.FindEntityType(typeof(PasswordRecoveryRequest))!
            .GetTableName().Should().Be("PasswordRecoveryRequests");
        context.Database.GetMigrations()
            .Should().Contain("20260927153000_AddPasswordRecoveryRequests");
    }
}
