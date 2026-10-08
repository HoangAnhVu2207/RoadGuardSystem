using RoadGuardSystem.BusinessObjects.Processing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Processing;

public sealed class P234ValidationRunContractTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P234ValidationRunContractTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProcessingFoundationMigration_CreatesValidationRunAndJobRowVersion()
    {
        await using var context = _fixture.CreateDbContext();
        var tableCount = await context.Database.SqlQueryRaw<int>(
            "SELECT CAST(COUNT(*) AS int) AS [Value] FROM sys.tables WHERE [name] = 'ValidationRuns'").SingleAsync();
        var rowVersionCount = await context.Database.SqlQueryRaw<int>(
            "SELECT CAST(COUNT(*) AS int) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID('ProcessingJobs') AND [name] = 'RowVersion' AND system_type_id = 189").SingleAsync();

        tableCount.Should().Be(1);
        rowVersionCount.Should().Be(1);
    }

    [Fact]
    public async Task ValidationProvenanceMigration_CreatesResearchTablesAndConcurrencyToken()
    {
        await using var context = _fixture.CreateDbContext();
        var tableCount = await context.Database.SqlQueryRaw<int>(
            "SELECT CAST(COUNT(*) AS int) AS [Value] FROM sys.tables WHERE [name] IN ('DerivedMeasurements', 'MeasurementValidationSamples')")
            .SingleAsync();
        var rowVersionCount = await context.Database.SqlQueryRaw<int>(
            "SELECT CAST(COUNT(*) AS int) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID('ValidationRuns') AND [name] = 'RowVersion' AND system_type_id = 189")
            .SingleAsync();

        tableCount.Should().Be(2);
        rowVersionCount.Should().Be(1);
    }
}
