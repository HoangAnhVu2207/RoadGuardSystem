using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Projects;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Projects;

public sealed class P220ProjectMembershipModelTests
{
    [Fact(DisplayName = "P2-20: The project membership model rejects a non-PM primary member")]
    public void PrimaryMembershipModel_RequiresProjectManagerRole()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(ProjectMember));
        var constraints = entity!.GetCheckConstraints().Select(constraint => constraint.Sql).ToArray();

        constraints.Should().Contain("[IsPrimary] = 0 OR [RoleCode] = 'PM'");
    }

    private static RoadGuardDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=RoadGuard_P220_ModelOnly;Integrated Security=true;TrustServerCertificate=true",
                sql => sql.UseNetTopologySuite())
            .Options;
        return new RoadGuardDbContext(options);
    }
}
