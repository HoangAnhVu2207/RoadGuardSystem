using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.SqlClient;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Models.Huy01;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Huy01;

public sealed class Huy01SharedSchemaTests
{
    [Fact]
    public void Domain_report_is_mapped_without_competing_entity()
    {
        using var db = new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer("Server=localhost;Database=model-only;Integrated Security=true", o => o.UseNetTopologySuite()).Options);
        db.Model.FindEntityType(typeof(Report)).Should().NotBeNull();
        db.Model.FindEntityType(typeof(Report))!.GetTableName().Should().Be("Reports");
    }

}
